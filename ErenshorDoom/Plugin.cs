using System;
using System.IO;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace ErenshorDoom
{
    [BepInPlugin("com.erenshordoom.plugin", "ErenshorDoom", "0.1.0")]
    public class Plugin : BaseUnityPlugin
    {
        internal static Plugin Instance;
        internal static ManualLogSource Log;
        internal static string PluginDirectory;

        private Harmony harmony;
        private DoomRunner doomRunner;
        private bool doomActive;

        private void Awake()
        {
            Instance = this;
            Log = Logger;
            PluginDirectory = Path.GetDirectoryName(Info.Location);
            PluginConfig.Init(Config);

            harmony = new Harmony("com.erenshordoom.plugin");
            harmony.PatchAll();

            Logger.LogInfo("ErenshorDoom loaded. Type /doom in chat or press " + PluginConfig.ToggleKey.Value + " to play Doom!");
        }

        private void Update()
        {
            if (UnityEngine.Input.GetKeyDown(PluginConfig.ToggleKey.Value))
            {
                ToggleDoom();
            }

            if (doomActive && doomRunner != null)
            {
                doomRunner.DoUpdate();
            }
        }

        private void OnDestroy()
        {
            if (doomRunner != null)
            {
                doomRunner.Shutdown();
                doomRunner = null;
            }

            if (harmony != null)
            {
                harmony.UnpatchSelf();
                harmony = null;
            }
        }

        public void ToggleDoom()
        {
            if (doomActive)
            {
                CloseDoom();
            }
            else
            {
                OpenDoom();
            }
        }

        private void OpenDoom()
        {
            try
            {
                if (doomRunner == null)
                {
                    doomRunner = new DoomRunner();
                    doomRunner.Initialize();
                }

                doomRunner.Show();
                doomActive = true;
            }
            catch (Exception ex)
            {
                Log.LogError("Failed to open Doom: " + ex.Message);
                Log.LogError(ex.StackTrace);
            }
        }

        private void CloseDoom()
        {
            if (doomRunner != null)
            {
                doomRunner.Hide();
            }
            doomActive = false;
        }
    }
}
