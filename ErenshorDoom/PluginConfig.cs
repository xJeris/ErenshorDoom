using BepInEx.Configuration;
using UnityEngine;

namespace ErenshorDoom
{
    public static class PluginConfig
    {
        public static ConfigEntry<KeyCode> ToggleKey;
        public static ConfigEntry<int> RenderScale;
        public static ConfigEntry<int> SfxVolume;
        public static ConfigEntry<int> MusicVolume;
        public static ConfigEntry<string> WadFileName;
        public static ConfigEntry<float> ControllerDeadzone;
        public static ConfigEntry<float> ControllerTurnSensitivity;

        public static void Init(ConfigFile config)
        {
            ToggleKey = config.Bind(
                "General", "ToggleKey", KeyCode.F9,
                "Key to open/close the Doom viewport");

            RenderScale = config.Bind(
                "Video", "RenderScale", 1,
                new ConfigDescription(
                    "Render scale multiplier (1 = 320x200, 2 = 640x400)",
                    new AcceptableValueRange<int>(1, 3)));

            SfxVolume = config.Bind(
                "Audio", "SfxVolume", 80,
                new ConfigDescription(
                    "Sound effects volume (0-100)",
                    new AcceptableValueRange<int>(0, 100)));

            MusicVolume = config.Bind(
                "Audio", "MusicVolume", 60,
                new ConfigDescription(
                    "Music volume (0-100)",
                    new AcceptableValueRange<int>(0, 100)));

            WadFileName = config.Bind(
                "General", "WadFileName", "DOOM1.WAD",
                "WAD file name to load (place in plugin folder)");

            ControllerDeadzone = config.Bind(
                "Controller", "Deadzone", 0.15f,
                new ConfigDescription(
                    "Controller stick deadzone",
                    new AcceptableValueRange<float>(0f, 0.5f)));

            ControllerTurnSensitivity = config.Bind(
                "Controller", "TurnSensitivity", 5f,
                new ConfigDescription(
                    "Controller turn speed",
                    new AcceptableValueRange<float>(1f, 10f)));
        }
    }
}
