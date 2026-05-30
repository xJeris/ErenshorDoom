using System;
using System.IO;
using ManagedDoom;
using ManagedDoom.Audio;
using ManagedDoom.Video;
using ManagedDoom.UserInput;
using ErenshorDoom.Video;
using ErenshorDoom.Audio;
using ErenshorDoom.Input;
using ErenshorDoom.UI;
using UnityEngine;

namespace ErenshorDoom
{
    /// <summary>
    /// Orchestrates the Doom engine lifecycle: initialization, tick loop, rendering, and cleanup.
    /// Driven by Plugin.Update() each Unity frame.
    /// </summary>
    public class DoomRunner
    {
        private Config config;
        private GameContent content;
        private Doom doom;

        private UnityDoomVideo video;
        private UnityDoomSound sound;
        private UnityDoomMusic music;
        private UnityDoomInput input;
        private DoomScreen screen;

        private float tickAccumulator;
        private const float TickDuration = 1f / 35f; // Doom runs at 35 Hz
        private const int MaxTicksPerFrame = 3; // Prevent spiral of death

        private bool initialized;
        private bool visible;

        public void Initialize()
        {
            if (initialized)
                return;

            try
            {
                Plugin.Log.LogInfo("Initializing Doom engine...");

                // Find WAD file
                var wadPath = FindWadPath();
                if (wadPath == null)
                {
                    Plugin.Log.LogError("No WAD file found! Place DOOM1.WAD (or DOOM.WAD / DOOM2.WAD) in " + Plugin.PluginDirectory);
                    throw new FileNotFoundException("No DOOM WAD file found in plugin directory.");
                }
                Plugin.Log.LogInfo("Loading WAD: " + wadPath);

                // Create Doom config with defaults
                config = new Config();
                config.video_highresolution = false; // 320x200 for performance
                config.video_gamescreensize = 7;
                config.video_displaymessage = true;
                config.video_gammacorrection = 2;
                config.audio_soundvolume = (int)(PluginConfig.SfxVolume.Value / 100f * 15);
                config.audio_musicvolume = (int)(PluginConfig.MusicVolume.Value / 100f * 15);

                // Create game content from WAD
                var args = new CommandLineArgs(new[] { "-iwad", wadPath });
                content = new GameContent(args);

                // Create Unity implementations
                video = new UnityDoomVideo(config, content);
                sound = new UnityDoomSound(config, content);
                music = new UnityDoomMusic(config, content);
                input = new UnityDoomInput(config, this);

                // Create display (windowed, with close callback)
                screen = new DoomScreen(() => Plugin.Instance.ToggleDoom());
                screen.SetTexture(video.Texture);

                // Create Doom instance
                doom = new Doom(args, config, content, video, sound, music, input);

                tickAccumulator = 0f;
                initialized = true;

                Plugin.Log.LogInfo("Doom engine initialized successfully! Renderer: " + video.RenderWidth + "x" + video.RenderHeight);
            }
            catch (Exception ex)
            {
                Plugin.Log.LogError("Failed to initialize Doom: " + ex.Message);
                Plugin.Log.LogError(ex.StackTrace);
                Shutdown();
                throw;
            }
        }

        public void DoUpdate()
        {
            if (!initialized || !visible)
                return;

            try
            {
                // Focus-based input routing: only send input to Doom when mouse is over the window
                bool doomHasFocus = screen.IsMouseOverWindow();
                input.SetFocused(doomHasFocus);
                video.SetFocus(doomHasFocus);

                // Process input
                input.ProcessInput();

                // Accumulate time and tick
                tickAccumulator += Time.unscaledDeltaTime;
                int ticks = 0;
                while (tickAccumulator >= TickDuration && ticks < MaxTicksPerFrame)
                {
                    var result = doom.Update();
                    if (result == UpdateResult.Completed)
                    {
                        // Doom quit (e.g., F10 -> Yes)
                        Plugin.Log.LogInfo("Doom requested quit.");
                        Plugin.Instance.ToggleDoom();
                        return;
                    }
                    tickAccumulator -= TickDuration;
                    ticks++;
                }

                // Render
                var frameFrac = Fixed.FromFloat(tickAccumulator / TickDuration);
                if (frameFrac > Fixed.One) frameFrac = Fixed.One;
                video.Render(doom, frameFrac);
            }
            catch (Exception ex)
            {
                Plugin.Log.LogError("Doom tick error: " + ex.Message);
                Plugin.Log.LogError(ex.StackTrace);
            }
        }

        public void PostDoomEvent(DoomEvent e)
        {
            if (doom != null)
            {
                doom.PostEvent(e);
            }
        }

        public void Show()
        {
            if (!initialized) return;
            screen.Show();
            video.SetFocus(true);
            input.GrabMouse();
            visible = true;
        }

        public void Hide()
        {
            if (!initialized) return;
            screen.Hide();
            video.SetFocus(false);
            input.ReleaseMouse();
            visible = false;
        }

        public void Shutdown()
        {
            visible = false;
            initialized = false;

            if (screen != null)
            {
                screen.Dispose();
                screen = null;
            }

            if (sound is IDisposable soundDisposable)
            {
                soundDisposable.Dispose();
            }
            sound = null;

            if (video != null)
            {
                video.Dispose();
                video = null;
            }

            if (content != null)
            {
                content.Dispose();
                content = null;
            }

            doom = null;
            input = null;
            music = null;
            config = null;
        }

        private string FindWadPath()
        {
            string[] wadNames = { "DOOM.WAD", "DOOM1.WAD", "DOOM2.WAD", "PLUTONIA.WAD", "TNT.WAD", "FREEDOOM1.WAD", "FREEDOOM2.WAD" };

            // Check configured name first
            var configuredName = PluginConfig.WadFileName.Value;
            if (!string.IsNullOrEmpty(configuredName))
            {
                var configuredPath = Path.Combine(Plugin.PluginDirectory, configuredName);
                if (File.Exists(configuredPath))
                    return configuredPath;
            }

            // Search plugin directory
            foreach (var name in wadNames)
            {
                var path = Path.Combine(Plugin.PluginDirectory, name);
                if (File.Exists(path))
                    return path;

                // Case-insensitive check
                var lower = Path.Combine(Plugin.PluginDirectory, name.ToLower());
                if (File.Exists(lower))
                    return lower;
            }

            return null;
        }

        public bool IsVisible => visible;
    }
}
