using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace r.e.p.o_cheat
{
    public sealed partial class Menu
    {
        private readonly Dictionary<string, float> orig = new Dictionary<string, float>();
        private readonly List<Collider> noclipCols = new List<Collider>();
        private bool noclipActive;
        private bool godApplied;
        private bool energyApplied;
        private bool fogSaved;
        private bool fogPrev;
        private bool zoomSaved;
        private float zoomPrev;

        /// <summary>Множитель поверх оригинального значения поля. При x1 оригинал возвращается.</summary>
        private void Scale(object o, string field, float mul)
        {
            if (o == null) return;
            string key = RuntimeHelpers.GetHashCode(o) + "." + field;
            float cur;
            bool has = orig.TryGetValue(key, out cur);
            if (Mathf.Abs(mul - 1f) < 0.001f)
            {
                if (has) { R.Set(o, field, cur); orig.Remove(key); }
                return;
            }
            if (!has) { cur = R.F(o, field); orig[key] = cur; }
            R.Set(o, field, cur * mul);
        }

        private void ApplySelf()
        {
            object pc = R.LocalController();
            object av = R.LocalAvatar();

            // --- god mode ---
            object ph = R.Get(av, "playerHealth");
            if (ph != null)
            {
                if (cfg.god)
                {
                    if (!R.B(av, "deadSet"))
                    {
                        R.Set(ph, "godMode", true);
                        R.Set(ph, "invincibleTimer", 1f);
                        int mh = R.I(ph, "maxHealth");
                        if (mh > 0 && R.I(ph, "health") < mh) R.Set(ph, "health", mh);
                    }
                    godApplied = true;
                }
                else if (godApplied)
                {
                    R.Set(ph, "godMode", false);
                    godApplied = false;
                }
            }

            if (pc != null)
            {
                // --- stamina ---
                if (cfg.stamina)
                {
                    R.Set(pc, "EnergyCurrent", R.F(pc, "EnergyStart", 40f));
                    R.Set(pc, "DebugEnergy", true);
                    energyApplied = true;
                }
                else if (energyApplied)
                {
                    R.Set(pc, "DebugEnergy", false);
                    energyApplied = false;
                }

                // --- движение ---
                Scale(pc, "MoveSpeed", cfg.speedMul);
                Scale(pc, "SprintSpeed", cfg.speedMul);
                Scale(pc, "CrouchSpeed", cfg.speedMul);
                Scale(pc, "JumpForce", cfg.jumpMul);
                Scale(pc, "CustomGravity", cfg.gravityMul);

                R.Set(pc, "DebugNoTumble", cfg.noTumble);
                Noclip(pc, cfg.noclip);
            }

            // --- магазин ---
            object shop = R.GetS("ShopManager", "instance");
            if (R.IsAlive(shop)) Scale(shop, "itemValueMultiplier", cfg.freeShop ? 0f : 1f);

            // --- хват ---
            object grabber = R.GetS("PhysGrabber", "instance");
            if (R.IsAlive(grabber))
            {
                Scale(grabber, "grabStrength", cfg.strengthMul);
                Scale(grabber, "throwStrength", cfg.strengthMul);
                Scale(grabber, "grabRange", cfg.grabRangeMul);
            }
        }

        private void Noclip(object pc, bool on)
        {
            Component comp = pc as Component;
            if (comp == null) return;

            if (!on)
            {
                if (noclipActive) RestoreNoclip();
                return;
            }

            if (!noclipActive)
            {
                noclipCols.Clear();
                foreach (Collider c in comp.GetComponentsInChildren<Collider>(true))
                {
                    if (c.enabled) { c.enabled = false; noclipCols.Add(c); }
                }
                noclipActive = true;
            }

            Rigidbody rb = R.Get(pc, "rb") as Rigidbody;
            GameObject camGo = R.Get(pc, "cameraGameObject") as GameObject;
            if (rb == null || camGo == null) return;
            Transform cam = camGo.transform;

            Vector3 d = Vector3.zero;
            if (!open)
            {
                if (Held(KeyCode.W)) d += cam.forward;
                if (Held(KeyCode.S)) d -= cam.forward;
                if (Held(KeyCode.D)) d += cam.right;
                if (Held(KeyCode.A)) d -= cam.right;
                if (Held(KeyCode.Space)) d += Vector3.up;
                if (Held(KeyCode.LeftControl)) d -= Vector3.up;
            }
            float sp = cfg.noclipSpeed * (Held(KeyCode.LeftShift) ? 2.5f : 1f);
            Vector3 vel = d.sqrMagnitude > 0.0001f ? d.normalized * sp : Vector3.zero;
            if (!R.Set(rb, "linearVelocity", vel)) R.Set(rb, "velocity", vel);
            R.Set(pc, "antiGravityTimer", 0.2f);
        }

        private void RestoreNoclip()
        {
            foreach (Collider c in noclipCols)
            {
                if (c != null) c.enabled = true;
            }
            noclipCols.Clear();
            noclipActive = false;
        }

        /// <summary>Туман и FOV: после игровых Update, поэтому в LateUpdate.</summary>
        private void ApplyVisuals()
        {
            ApplyNight();
            if (cfg.noFog)
            {
                if (!fogSaved) { fogPrev = RenderSettings.fog; fogSaved = true; }
                RenderSettings.fog = false;
            }
            else if (fogSaved)
            {
                RenderSettings.fog = fogPrev;
                fogSaved = false;
            }

            object zoom = R.GetS("CameraZoom", "Instance");
            if (R.IsAlive(zoom))
            {
                if (cfg.fovOn)
                {
                    if (!zoomSaved) { zoomPrev = R.F(zoom, "playerZoomDefault", 70f); zoomSaved = true; }
                    R.Set(zoom, "playerZoomDefault", cfg.fov);
                    foreach (object c in R.List(R.Get(zoom, "cams")))
                    {
                        Camera cam = c as Camera;
                        if (cam != null) cam.fieldOfView = cfg.fov;
                    }
                }
                else if (zoomSaved)
                {
                    R.Set(zoom, "playerZoomDefault", zoomPrev);
                    zoomSaved = false;
                }
            }
        }

        private void CleanupSelf()
        {
            RestoreNoclip();
            object pc = R.LocalController();
            object av = R.LocalAvatar();
            if (pc != null)
            {
                Scale(pc, "MoveSpeed", 1f);
                Scale(pc, "SprintSpeed", 1f);
                Scale(pc, "CrouchSpeed", 1f);
                Scale(pc, "JumpForce", 1f);
                Scale(pc, "CustomGravity", 1f);
                R.Set(pc, "DebugNoTumble", false);
                if (energyApplied) R.Set(pc, "DebugEnergy", false);
            }
            object shop2 = R.GetS("ShopManager", "instance");
            if (R.IsAlive(shop2)) Scale(shop2, "itemValueMultiplier", 1f);
            object grabber = R.GetS("PhysGrabber", "instance");
            if (R.IsAlive(grabber))
            {
                Scale(grabber, "grabStrength", 1f);
                Scale(grabber, "throwStrength", 1f);
                Scale(grabber, "grabRange", 1f);
            }
            object ph = R.Get(av, "playerHealth");
            if (ph != null && godApplied) R.Set(ph, "godMode", false);
            if (fogSaved) RenderSettings.fog = fogPrev;
            RestoreNight();
            freecam = false;
            object zoom = R.GetS("CameraZoom", "Instance");
            if (R.IsAlive(zoom) && zoomSaved) R.Set(zoom, "playerZoomDefault", zoomPrev);
            godApplied = false;
            energyApplied = false;
        }
    }
}
