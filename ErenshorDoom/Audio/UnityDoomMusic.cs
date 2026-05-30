using ManagedDoom;
using ManagedDoom.Audio;
using UnityEngine;

namespace ErenshorDoom.Audio
{
    /// <summary>
    /// Stub music implementation. MeltySynth integration is deferred to a later phase.
    /// When implemented, this will use MeltySynth to render MIDI to PCM and stream
    /// it through OnAudioFilterRead on a dedicated AudioSource.
    /// </summary>
    public sealed class UnityDoomMusic : IMusic
    {
        private Config config;

        public UnityDoomMusic(Config config, GameContent content)
        {
            this.config = config;
            config.audio_musicvolume = MathCompat.Clamp(config.audio_musicvolume, 0, MaxVolume);

            // TODO: Load soundfont, initialize MeltySynth synthesizer
            // var sfPath = Path.Combine(Plugin.PluginDirectory, config.audio_soundfont);
            // if (File.Exists(sfPath)) { ... }

            Plugin.Log.LogInfo("Music system initialized (stub - music playback not yet implemented)");
        }

        public void StartMusic(Bgm bgm, bool loop)
        {
            // TODO: Convert MUS lump to MIDI, feed to MeltySynth, stream to AudioSource
        }

        public int MaxVolume => 15;

        public int Volume
        {
            get => config.audio_musicvolume;
            set => config.audio_musicvolume = value;
        }
    }
}
