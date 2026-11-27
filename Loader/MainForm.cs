using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using SharpMonoInjector;

namespace REPOLoader
{
    internal sealed class MainForm : Form
    {
        private static readonly Color Bg = Color.FromArgb(12, 12, 14);
        private static readonly Color Panel = Color.FromArgb(22, 22, 26);
        private static readonly Color Border = Color.FromArgb(46, 46, 52);
        private static readonly Color Accent = Color.FromArgb(237, 79, 61);
        private static readonly Color TextC = Color.FromArgb(225, 225, 230);
        private static readonly Color Dim = Color.FromArgb(128, 128, 142);
        private static readonly Color Green = Color.FromArgb(77, 217, 140);

        private const string Ns = "r.e.p.o_cheat";
        private const string Cls = "Loader";

        private readonly Label lblTitle = new Label();
        private readonly Label lblStatus = new Label();
        private readonly Label lblDetail = new Label();
        private readonly ProgressBar bar = new ProgressBar();
        private readonly Button btnRun = new Button();
        private readonly Button btnUnload = new Button();
        private readonly CheckBox chkSteam = new CheckBox();
        private readonly CheckBox chkTray = new CheckBox();
        private readonly TextBox log = new TextBox();
        private readonly NotifyIcon tray = new NotifyIcon();

        private CancellationTokenSource cts;
        private bool running;
        private bool injected;
        private bool reallyExit;

        public MainForm()
        {
            Text = "R.E.P.O. Menu Loader";
            ClientSize = new Size(560, 430);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Bg;
            ForeColor = TextC;
            Font = new Font("Segoe UI", 9f);

            lblTitle.Text = "/  R.E.P.O MENU LOADER";
            lblTitle.Font = new Font("Segoe UI", 14f, FontStyle.Bold);
            lblTitle.ForeColor = TextC;
            lblTitle.SetBounds(20, 14, 500, 30);

            lblStatus.Text = "Нажми «Запустить»";
            lblStatus.Font = new Font("Segoe UI", 12f, FontStyle.Bold);
            lblStatus.ForeColor = TextC;
            lblStatus.SetBounds(20, 58, 520, 28);

            lblDetail.ForeColor = Dim;
            lblDetail.SetBounds(20, 88, 520, 36);

            bar.Style = ProgressBarStyle.Marquee;
            bar.MarqueeAnimationSpeed = 0;
            bar.SetBounds(20, 128, 520, 6);

            StyleButton(btnRun, "ЗАПУСТИТЬ", true);
            btnRun.Font = new Font("Segoe UI", 13f, FontStyle.Bold);
            btnRun.SetBounds(20, 146, 340, 56);
            btnRun.Click += delegate { if (running) Cancel(); else Start(); };

            StyleButton(btnUnload, "Выгрузить", false);
            btnUnload.SetBounds(372, 146, 168, 56);
            btnUnload.Click += delegate { Unload(); };

            chkSteam.Text = "Запускать игру через Steam автоматически";
            chkSteam.Checked = true;
            chkSteam.ForeColor = TextC;
            chkSteam.SetBounds(20, 212, 330, 22);
            chkTray.Text = "Сворачивать в трей после загрузки";
            chkTray.Checked = true;
            chkTray.ForeColor = TextC;
            chkTray.SetBounds(20, 236, 330, 22);

            log.Multiline = true;
            log.ReadOnly = true;
            log.ScrollBars = ScrollBars.Vertical;
            log.BackColor = Panel;
            log.ForeColor = Dim;
            log.BorderStyle = BorderStyle.FixedSingle;
            log.Font = new Font("Consolas", 8.5f);
            log.SetBounds(20, 268, 520, 142);

            Controls.AddRange(new Control[] { lblTitle, lblStatus, lblDetail, bar, btnRun, btnUnload, chkSteam, chkTray, log });

            tray.Icon = SystemIcons.Application;
            tray.Text = "R.E.P.O. Menu Loader";
            ContextMenuStrip cm = new ContextMenuStrip();
            cm.Items.Add("Открыть", null, delegate { ShowFromTray(); });
            cm.Items.Add("Выгрузить меню", null, delegate { Unload(); });
            cm.Items.Add("Выход", null, delegate { reallyExit = true; Close(); });
            tray.ContextMenuStrip = cm;
            tray.DoubleClick += delegate { ShowFromTray(); };

            FormClosing += OnClosing;
            Shown += delegate { Preflight(); };
        }

        private static void StyleButton(Button b, string text, bool accent)
        {
            b.Text = text;
            b.FlatStyle = FlatStyle.Flat;
            b.FlatAppearance.BorderColor = accent ? Accent : Border;
            b.FlatAppearance.MouseOverBackColor = accent ? Color.FromArgb(255, 100, 82) : Color.FromArgb(40, 40, 46);
            b.FlatAppearance.MouseDownBackColor = accent ? Color.FromArgb(180, 50, 38) : Accent;
            b.BackColor = accent ? Accent : Panel;
            b.ForeColor = Color.White;
            b.Cursor = Cursors.Hand;
            b.UseVisualStyleBackColor = false;
        }

        // ---------- UI-хелперы ----------
        private void Ui(Action a)
        {
            if (IsDisposed) return;
            try { if (InvokeRequired) BeginInvoke(a); else a(); } catch { }
        }

        private void Status(string s, Color c)
        {
            Ui(delegate { lblStatus.Text = s; lblStatus.ForeColor = c; });
            Log(s);
        }

        private void Detail(string s) { Ui(delegate { lblDetail.Text = s; }); }

        private void Log(string s)
        {
            Ui(delegate { log.AppendText(DateTime.Now.ToString("HH:mm:ss") + "  " + s + "\r\n"); });
        }

        private void Busy(bool on)
        {
            Ui(delegate
            {
                running = on;
                bar.MarqueeAnimationSpeed = on ? 30 : 0;
                btnRun.Text = on ? "ОТМЕНА" : "ЗАПУСТИТЬ";
                btnUnload.Enabled = !on;
            });
        }

        // ---------- проверка при старте ----------
        private void Preflight()
        {
            string dll = GameFinder.FindDll();
            string game = GameFinder.Find();
            Log(dll != null ? "REPOMenu.dll: " + dll : "REPOMenu.dll не найден рядом с Loader.exe");
            Log(game != null ? "Папка игры: " + game : "Папка игры не найдена автоматически");
            Detail((game != null ? "Игра: " + game : "Папка игры не найдена (не страшно, если игра запустится)") + (dll == null ? "\r\nНет REPOMenu.dll: запусти build.bat" : ""));
            if (dll == null) Status("Не найден REPOMenu.dll", Accent);
        }

        // ---------- запуск ----------
        private void Cancel()
        {
            if (cts != null) cts.Cancel();
        }

        private bool Wait(int ms)
        {
            return !cts.Token.WaitHandle.WaitOne(ms);
        }

        private void Start()
        {
            cts = new CancellationTokenSource();
            bool steam = chkSteam.Checked;
            Busy(true);
            Task.Run(delegate
            {
                try { Flow(steam); }
                catch (Exception e) { Status("Ошибка: " + e.Message, Accent); }
                finally { Busy(false); }
            });
        }

        private void Flow(bool steam)
        {
            Status("Проверяю файлы…", TextC);
            string dll = GameFinder.FindDll();
            if (dll == null) { Status("Не найден REPOMenu.dll рядом с Loader.exe (запусти build.bat)", Accent); return; }

            Process game = GameFinder.FindProcess();
            if (game == null)
            {
                if (steam)
                {
                    Status("Запускаю R.E.P.O. через Steam…", TextC);
                    try { Process.Start(new ProcessStartInfo("steam://rungameid/3241660") { UseShellExecute = true }); }
                    catch (Exception e) { Log("Steam: " + e.Message); }
                }
                else Status("Запусти R.E.P.O. сам, я подожду…", TextC);

                for (int i = 0; i < 300; i++)
                {
                    game = GameFinder.FindProcess();
                    if (game != null) break;
                    if (!Wait(1000)) { Status("Отменено", Dim); return; }
                    if (i == 20 && steam) Detail("Игра ещё не запустилась. Проверь, что Steam открыт.");
                }
                if (game == null) { Status("Игра не запустилась за 5 минут", Accent); return; }
            }

            Status("Игра запущена. Жду загрузки Mono…", TextC);
            bool mono = false;
            for (int i = 0; i < 240; i++)
            {
                game.Refresh();
                if (game.HasExited) { Status("Игра закрылась", Accent); return; }
                if (GameFinder.MonoLoaded(game)) { mono = true; break; }
                if (!Wait(500)) { Status("Отменено", Dim); return; }
            }
            if (!mono) { Status("Mono не загрузился за 2 минуты", Accent); return; }

            for (int s = 12; s > 0; s--)
            {
                Status("Mono загружен. Даю игре загрузиться: " + s + " с…", TextC);
                if (!Wait(1000)) { Status("Отменено", Dim); return; }
            }

            byte[] bytes = File.ReadAllBytes(dll);
            string marker = Path.Combine(Path.GetTempPath(), "REPOMenu_loaded.txt");
            const int attempts = 6;
            for (int a = 1; a <= attempts; a++)
            {
                game.Refresh();
                if (game.HasExited) { Status("Игра закрылась", Accent); return; }
                Status("Внедряю меню… попытка " + a + " из " + attempts, TextC);
                DateTime before = DateTime.Now.AddSeconds(-1);
                try
                {
                    using (Injector inj = new Injector(game.Id))
                    {
                        inj.Inject(bytes, Ns, Cls, "Init");
                    }
                    bool ok = false;
                    for (int w = 0; w < 12; w++)
                    {
                        if (File.Exists(marker) && File.GetLastWriteTime(marker) >= before) { ok = true; break; }
                        Thread.Sleep(500);
                    }
                    injected = true;
                    if (ok) Status("Готово! Меню загружено. В игре: Insert открыть, F10 выгрузить", Green);
                    else Status("Инжект выполнен. Нажми Insert в игре (если меню уже было загружено, всё в порядке)", Green);
                    AfterSuccess();
                    return;
                }
                catch (Exception e)
                {
                    Log("Попытка " + a + ": " + e.Message);
                    if (!Wait(4000)) { Status("Отменено", Dim); return; }
                }
            }
            Status("Не удалось внедрить меню. Подробности в журнале ниже", Accent);
        }

        private void AfterSuccess()
        {
            if (!chkTray.Checked) return;
            Thread.Sleep(3000);
            Ui(delegate { HideToTray(); });
        }

        // ---------- выгрузка ----------
        private void Unload()
        {
            Process game = GameFinder.FindProcess();
            string dll = GameFinder.FindDll();
            if (game == null) { Status("Игра не запущена", Dim); return; }
            if (dll == null) { Status("Не найден REPOMenu.dll", Accent); return; }
            Task.Run(delegate
            {
                try
                {
                    Status("Выгружаю меню…", TextC);
                    using (Injector inj = new Injector(game.Id))
                    {
                        inj.Inject(File.ReadAllBytes(dll), Ns, Cls, "UnloadCheat");
                    }
                    injected = false;
                    Status("Меню выгружено. Кнопка «Запустить» загрузит его снова", Green);
                }
                catch (Exception e) { Status("Ошибка выгрузки: " + e.Message, Accent); }
            });
        }

        // ---------- трей ----------
        private void HideToTray()
        {
            tray.Visible = true;
            Hide();
            tray.ShowBalloonTip(2500, "R.E.P.O. Menu", "Меню загружено. Insert в игре.", ToolTipIcon.Info);
        }

        private void ShowFromTray()
        {
            Show();
            WindowState = FormWindowState.Normal;
            Activate();
            tray.Visible = false;
        }

        private void OnClosing(object sender, FormClosingEventArgs e)
        {
            if (!reallyExit && injected && chkTray.Checked && e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                HideToTray();
                return;
            }
            if (cts != null) cts.Cancel();
            tray.Visible = false;
            tray.Dispose();
        }
    }
}
