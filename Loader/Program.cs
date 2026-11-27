using System;
using System.Threading;
using System.Windows.Forms;

namespace REPOLoader
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            bool created;
            using (Mutex m = new Mutex(true, "REPOMenuLoader_single", out created))
            {
                if (!created)
                {
                    MessageBox.Show("Loader уже запущен (смотри значок в трее).", "R.E.P.O. Loader");
                    return;
                }
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new MainForm());
            }
        }
    }
}
