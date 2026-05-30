// .NET Framework 4.7.2 compatibility shims for APIs available in .NET Core/.NET 5+ but not net472.

using System;
using System.Collections.Generic;
using System.IO;

namespace ManagedDoom
{
    internal static class MathCompat
    {
        public static int Clamp(int value, int min, int max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }

        public static float Clamp(float value, float min, float max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }

        public static double Clamp(double value, double min, double max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }
    }

    internal static class MathF
    {
        public const float PI = (float)Math.PI;

        public static float Sin(float x) => (float)Math.Sin(x);
        public static float Cos(float x) => (float)Math.Cos(x);
        public static float Tan(float x) => (float)Math.Tan(x);
        public static float Atan2(float y, float x) => (float)Math.Atan2(y, x);
        public static float Sqrt(float x) => (float)Math.Sqrt(x);
        public static float Abs(float x) => Math.Abs(x);
        public static float Round(float x) => (float)Math.Round(x);
        public static float Floor(float x) => (float)Math.Floor(x);
        public static float Ceiling(float x) => (float)Math.Ceiling(x);
        public static float Pow(float x, float y) => (float)Math.Pow(x, y);
        public static float Log(float x) => (float)Math.Log(x);
        public static float Max(float x, float y) => Math.Max(x, y);
        public static float Min(float x, float y) => Math.Min(x, y);
    }

    internal static class DictionaryExtensions
    {
        public static bool TryAdd<TKey, TValue>(this Dictionary<TKey, TValue> dict, TKey key, TValue value)
        {
            if (dict.ContainsKey(key))
                return false;
            dict.Add(key, value);
            return true;
        }
    }

    internal static class StreamExtensions
    {
        public static void ReadExactly(this Stream stream, byte[] buffer, int offset, int count)
        {
            int totalRead = 0;
            while (totalRead < count)
            {
                int bytesRead = stream.Read(buffer, offset + totalRead, count - totalRead);
                if (bytesRead == 0)
                    throw new EndOfStreamException();
                totalRead += bytesRead;
            }
        }
    }
}
