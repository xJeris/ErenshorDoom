using System;
using ManagedDoom;
using ManagedDoom.Video;
using UnityEngine;

namespace ErenshorDoom.Video
{
    public sealed class UnityDoomVideo : IVideo, IDisposable
    {
        private ManagedDoom.Video.Renderer renderer;
        private byte[] columnMajorData; // RGBA in column-major order from Doom renderer
        private byte[] rowMajorData;    // RGBA in row-major order for Unity Texture2D
        private Texture2D texture;
        private bool hasFocus;

        public UnityDoomVideo(Config config, GameContent content)
        {
            renderer = new ManagedDoom.Video.Renderer(config, content);

            int pixelCount = renderer.Width * renderer.Height;
            columnMajorData = new byte[4 * pixelCount];
            rowMajorData = new byte[4 * pixelCount];
            // Unity Texture2D: width x height, row-major, bottom row first
            texture = new Texture2D(renderer.Width, renderer.Height, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Point;
            texture.wrapMode = TextureWrapMode.Clamp;

            hasFocus = true;
        }

        public void Render(Doom doom, Fixed frameFrac)
        {
            renderer.Render(doom, columnMajorData, frameFrac);

            // Transpose from column-major (Doom: data[height * x + y]) to
            // row-major bottom-up (Unity: data[(height-1-y) * width + x])
            int w = renderer.Width;
            int h = renderer.Height;
            for (int x = 0; x < w; x++)
            {
                int srcCol = h * x; // start of this column in source
                for (int y = 0; y < h; y++)
                {
                    int srcIdx = (srcCol + y) * 4;
                    // Unity textures are bottom-up: row 0 is the bottom of the screen
                    int dstIdx = ((h - 1 - y) * w + x) * 4;
                    rowMajorData[dstIdx]     = columnMajorData[srcIdx];
                    rowMajorData[dstIdx + 1] = columnMajorData[srcIdx + 1];
                    rowMajorData[dstIdx + 2] = columnMajorData[srcIdx + 2];
                    rowMajorData[dstIdx + 3] = columnMajorData[srcIdx + 3];
                }
            }

            texture.LoadRawTextureData(rowMajorData);
            texture.Apply(false);
        }

        public void InitializeWipe()
        {
            renderer.InitializeWipe();
        }

        public bool HasFocus()
        {
            return hasFocus;
        }

        public void SetFocus(bool focused)
        {
            hasFocus = focused;
        }

        public void Dispose()
        {
            if (texture != null)
            {
                UnityEngine.Object.Destroy(texture);
                texture = null;
            }
        }

        public Texture2D Texture => texture;
        public int RenderWidth => renderer.Width;
        public int RenderHeight => renderer.Height;

        public int WipeBandCount => renderer.WipeBandCount;
        public int WipeHeight => renderer.WipeHeight;

        public int MaxWindowSize => renderer.MaxWindowSize;

        public int WindowSize
        {
            get => renderer.WindowSize;
            set => renderer.WindowSize = value;
        }

        public bool DisplayMessage
        {
            get => renderer.DisplayMessage;
            set => renderer.DisplayMessage = value;
        }

        public int MaxGammaCorrectionLevel => renderer.MaxGammaCorrectionLevel;

        public int GammaCorrectionLevel
        {
            get => renderer.GammaCorrectionLevel;
            set => renderer.GammaCorrectionLevel = value;
        }
    }
}
