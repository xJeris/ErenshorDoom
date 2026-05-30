using System;
using ManagedDoom;
using ManagedDoom.Audio;
using UnityEngine;

namespace ErenshorDoom.Audio
{
    public sealed class UnityDoomSound : ISound, IDisposable
    {
        private static readonly int ChannelCount = 8;

        private static readonly float FastDecay = (float)Math.Pow(0.5, 1.0 / (35.0 / 5.0));
        private static readonly float SlowDecay = (float)Math.Pow(0.5, 1.0 / 35.0);

        private static readonly float ClipDist = 1200;
        private static readonly float CloseDist = 160;
        private static readonly float Attenuator = ClipDist - CloseDist;

        private Config config;
        private AudioClip[] clips;
        private float[] amplitudes;

        private DoomRandom random;

        private GameObject audioRoot;
        private AudioSource[] channels;
        private ChannelInfo[] infos;

        private AudioSource uiChannel;
        private Sfx uiReserved;

        private Mobj listener;
        private float masterVolumeDecay;

        private float lastUpdateTime;

        public UnityDoomSound(Config config, GameContent content)
        {
            this.config = config;

            config.audio_soundvolume = MathCompat.Clamp(config.audio_soundvolume, 0, MaxVolume);

            clips = new AudioClip[DoomInfo.SfxNames.Length];
            amplitudes = new float[DoomInfo.SfxNames.Length];

            if (config.audio_randompitch)
            {
                random = new DoomRandom();
            }

            // Extract sound lumps from WAD and create Unity AudioClips
            for (var i = 0; i < DoomInfo.SfxNames.Length; i++)
            {
                var name = "DS" + DoomInfo.SfxNames[i].ToString().ToUpper();
                var lump = content.Wad.GetLumpNumber(name);
                if (lump == -1)
                    continue;

                var data = content.Wad.ReadLump(name);
                if (data.Length < 8)
                    continue;

                int sampleRate = BitConverter.ToUInt16(data, 2);
                int sampleCount = BitConverter.ToInt32(data, 4);

                var offset = 8;
                if (ContainsDmxPadding(data, sampleCount))
                {
                    offset += 16;
                    sampleCount -= 32;
                }

                if (sampleCount <= 0)
                    continue;

                // Convert 8-bit unsigned PCM to float PCM for Unity
                var floatSamples = new float[sampleCount];
                float maxAmp = 0;
                int ampCheckCount = Math.Min(sampleRate / 5, sampleCount);
                for (var s = 0; s < sampleCount; s++)
                {
                    floatSamples[s] = (data[offset + s] - 128) / 128f;
                    if (s < ampCheckCount)
                    {
                        float a = Math.Abs(floatSamples[s]);
                        if (a > maxAmp) maxAmp = a;
                    }
                }

                var clip = AudioClip.Create("doom_sfx_" + i, sampleCount, 1, sampleRate, false);
                clip.SetData(floatSamples, 0);
                clips[i] = clip;
                amplitudes[i] = maxAmp;
            }

            // Create audio source pool
            audioRoot = new GameObject("ErenshorDoom_Audio");
            UnityEngine.Object.DontDestroyOnLoad(audioRoot);

            channels = new AudioSource[ChannelCount];
            infos = new ChannelInfo[ChannelCount];
            for (var i = 0; i < ChannelCount; i++)
            {
                var go = new GameObject("DoomSfx_" + i);
                go.transform.SetParent(audioRoot.transform);
                channels[i] = go.AddComponent<AudioSource>();
                channels[i].playOnAwake = false;
                channels[i].spatialBlend = 0f; // 2D audio, we handle panning ourselves
                infos[i] = new ChannelInfo();
            }

            var uiGo = new GameObject("DoomSfx_UI");
            uiGo.transform.SetParent(audioRoot.transform);
            uiChannel = uiGo.AddComponent<AudioSource>();
            uiChannel.playOnAwake = false;
            uiChannel.spatialBlend = 0f;
            uiReserved = Sfx.NONE;

            masterVolumeDecay = (float)config.audio_soundvolume / MaxVolume;
            lastUpdateTime = 0;
        }

        private static bool ContainsDmxPadding(byte[] data, int sampleCount)
        {
            if (sampleCount < 32)
                return false;

            var first = data[8];
            for (var i = 1; i < 16; i++)
            {
                if (data[8 + i] != first)
                    return false;
            }

            var last = data[8 + sampleCount - 1];
            for (var i = 1; i < 16; i++)
            {
                if (data[8 + sampleCount - i - 1] != last)
                    return false;
            }

            return true;
        }

        public void SetListener(Mobj listener)
        {
            this.listener = listener;
        }

        public void Update()
        {
            var now = Time.unscaledTime;
            if (now - lastUpdateTime < 0.01f)
                return;

            for (var i = 0; i < infos.Length; i++)
            {
                var info = infos[i];
                var channel = channels[i];

                if (info.Playing != Sfx.NONE)
                {
                    if (channel.isPlaying)
                    {
                        if (info.Type == SfxType.Diffuse)
                            info.Priority *= SlowDecay;
                        else
                            info.Priority *= FastDecay;
                        SetParam(channel, info);
                    }
                    else
                    {
                        info.Playing = Sfx.NONE;
                        if (info.Reserved == Sfx.NONE)
                            info.Source = null;
                    }
                }

                if (info.Reserved != Sfx.NONE)
                {
                    if (info.Playing != Sfx.NONE)
                        channel.Stop();

                    channel.clip = clips[(int)info.Reserved];
                    SetParam(channel, info);
                    channel.pitch = GetPitch(info.Type, info.Reserved);
                    channel.Play();
                    info.Playing = info.Reserved;
                    info.Reserved = Sfx.NONE;
                }
            }

            if (uiReserved != Sfx.NONE)
            {
                if (uiChannel.isPlaying)
                    uiChannel.Stop();
                uiChannel.volume = masterVolumeDecay;
                uiChannel.panStereo = 0f;
                uiChannel.clip = clips[(int)uiReserved];
                uiChannel.Play();
                uiReserved = Sfx.NONE;
            }

            lastUpdateTime = now;
        }

        public void StartSound(Sfx sfx)
        {
            if (clips[(int)sfx] == null)
                return;
            uiReserved = sfx;
        }

        public void StartSound(Mobj mobj, Sfx sfx, SfxType type)
        {
            StartSound(mobj, sfx, type, 100);
        }

        public void StartSound(Mobj mobj, Sfx sfx, SfxType type, int volume)
        {
            if (clips[(int)sfx] == null)
                return;

            var x = (mobj.X - listener.X).ToFloat();
            var y = (mobj.Y - listener.Y).ToFloat();
            var dist = MathF.Sqrt(x * x + y * y);

            float priority;
            if (type == SfxType.Diffuse)
                priority = volume;
            else
                priority = amplitudes[(int)sfx] * GetDistanceDecay(dist) * volume;

            // Try to find existing channel for this source
            for (var i = 0; i < infos.Length; i++)
            {
                var info = infos[i];
                if (info.Source == mobj && info.Type == type)
                {
                    info.Reserved = sfx;
                    info.Priority = priority;
                    info.Volume = volume;
                    return;
                }
            }

            // Try to find a free channel
            for (var i = 0; i < infos.Length; i++)
            {
                var info = infos[i];
                if (info.Reserved == Sfx.NONE && info.Playing == Sfx.NONE)
                {
                    info.Reserved = sfx;
                    info.Priority = priority;
                    info.Source = mobj;
                    info.Type = type;
                    info.Volume = volume;
                    return;
                }
            }

            // Steal lowest priority channel
            var minPriority = float.MaxValue;
            var minChannel = -1;
            for (var i = 0; i < infos.Length; i++)
            {
                if (infos[i].Priority < minPriority)
                {
                    minPriority = infos[i].Priority;
                    minChannel = i;
                }
            }
            if (priority >= minPriority && minChannel >= 0)
            {
                var info = infos[minChannel];
                info.Reserved = sfx;
                info.Priority = priority;
                info.Source = mobj;
                info.Type = type;
                info.Volume = volume;
            }
        }

        public void StopSound(Mobj mobj)
        {
            for (var i = 0; i < infos.Length; i++)
            {
                var info = infos[i];
                if (info.Source == mobj)
                {
                    info.LastX = info.Source.X;
                    info.LastY = info.Source.Y;
                    info.Source = null;
                    info.Volume /= 5;
                }
            }
        }

        public void Reset()
        {
            if (random != null)
                random.Clear();

            for (var i = 0; i < infos.Length; i++)
            {
                channels[i].Stop();
                infos[i].Clear();
            }

            listener = null;
        }

        public void Pause()
        {
            for (var i = 0; i < channels.Length; i++)
            {
                if (channels[i].isPlaying)
                    channels[i].Pause();
            }
        }

        public void Resume()
        {
            for (var i = 0; i < channels.Length; i++)
            {
                if (!channels[i].isPlaying && channels[i].clip != null && channels[i].time > 0)
                    channels[i].UnPause();
            }
        }

        private void SetParam(AudioSource source, ChannelInfo info)
        {
            if (info.Type == SfxType.Diffuse)
            {
                source.panStereo = 0f;
                source.volume = 0.01f * masterVolumeDecay * info.Volume;
            }
            else
            {
                Fixed sourceX;
                Fixed sourceY;
                if (info.Source == null)
                {
                    sourceX = info.LastX;
                    sourceY = info.LastY;
                }
                else
                {
                    sourceX = info.Source.X;
                    sourceY = info.Source.Y;
                }

                var x = (sourceX - listener.X).ToFloat();
                var y = (sourceY - listener.Y).ToFloat();

                if (Math.Abs(x) < 16 && Math.Abs(y) < 16)
                {
                    source.panStereo = 0f;
                    source.volume = 0.01f * masterVolumeDecay * info.Volume;
                }
                else
                {
                    var dist = MathF.Sqrt(x * x + y * y);
                    var angle = MathF.Atan2(y, x) - (float)listener.Angle.ToRadian();
                    source.panStereo = -MathF.Sin(angle);
                    source.volume = 0.01f * masterVolumeDecay * GetDistanceDecay(dist) * info.Volume;
                }
            }
        }

        private float GetDistanceDecay(float dist)
        {
            if (dist < CloseDist)
                return 1f;
            return Math.Max((ClipDist - dist) / Attenuator, 0f);
        }

        private float GetPitch(SfxType type, Sfx sfx)
        {
            if (random != null)
            {
                if (sfx == Sfx.ITEMUP || sfx == Sfx.TINK || sfx == Sfx.RADIO)
                    return 1.0f;
                if (type == SfxType.Voice)
                    return 1.0f + 0.075f * (random.Next() - 128) / 128f;
                return 1.0f + 0.025f * (random.Next() - 128) / 128f;
            }
            return 1.0f;
        }

        public void Dispose()
        {
            if (audioRoot != null)
            {
                UnityEngine.Object.Destroy(audioRoot);
                audioRoot = null;
            }

            if (clips != null)
            {
                for (var i = 0; i < clips.Length; i++)
                {
                    if (clips[i] != null)
                    {
                        UnityEngine.Object.Destroy(clips[i]);
                        clips[i] = null;
                    }
                }
            }
        }

        public int MaxVolume => 15;

        public int Volume
        {
            get => config.audio_soundvolume;
            set
            {
                config.audio_soundvolume = value;
                masterVolumeDecay = (float)config.audio_soundvolume / MaxVolume;
            }
        }

        private class ChannelInfo
        {
            public Sfx Reserved;
            public Sfx Playing;
            public float Priority;
            public Mobj Source;
            public SfxType Type;
            public int Volume;
            public Fixed LastX;
            public Fixed LastY;

            public void Clear()
            {
                Reserved = Sfx.NONE;
                Playing = Sfx.NONE;
                Priority = 0;
                Source = null;
                Type = 0;
                Volume = 0;
                LastX = Fixed.Zero;
                LastY = Fixed.Zero;
            }
        }
    }
}
