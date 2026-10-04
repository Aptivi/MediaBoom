//
// MediaBoom  Copyright (C) 2023-2025  Aptivi
//
// This file is part of MediaBoom
//
// MediaBoom is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// MediaBoom is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY, without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <https://www.gnu.org/licenses/>.
//

using System;
using System.Text;
using System.Threading;
using MediaBoom.Basolia.Exceptions;
using MediaBoom.Basolia.Languages;
using MediaBoom.Basolia.Media;
using MediaBoom.Basolia.Media.Video;
using MediaBoom.Native.Interop.Enumerations;
using Terminaux.Base.Buffered;
using Terminaux.Base.Extensions;
using Terminaux.Inputs;
using Terminaux.Writer.ConsoleWriters;

namespace MediaBoom.Cli.CliBase.VideoDisplay
{
    internal static class VideoDisplayer
    {
        private static byte[] back = [], front = [];
        private static int gridW, gridH;
        private static bool sizeChanged = true;
        private static int[] xs = [];
        private static int xsSrcW, xsOutW;
        private readonly static object gridLock = new();
        private readonly static AutoResetEvent drawSignal = new(false);

        internal static void Display(BasoliaMedia? basolia, Screen playerScreen)
        {
            if (basolia is null)
                throw new BasoliaException(LanguageTools.GetLocalized("MEDIABOOM_BASOLIA_EXCEPTION_BASOLIAMEDIA"), MpvError.MPV_ERROR_GENERIC);
            if (!basolia.IsPlaying())
                return;

            // Make a resizable screen part that shows the video frame
            var displayerScreenBuffer = new ScreenPart();
            string renderedBuffer = "";
            displayerScreenBuffer.AddDynamicText(() => renderedBuffer);
            playerScreen.RemoveBufferedParts();
            playerScreen.AddBufferedPart("MediaBoom Player - Video displayer", displayerScreenBuffer);

            // Subscribe to the video frame change event and allow background
            basolia.GLReadbackEnabled = true;
            basolia.FrameAvailable += UpdateFrame;
            ConsoleColoring.AllowBackground = true;

            // Wait for user to bail
            sizeChanged = true;
            byte[] buf = [];
            while (true)
            {
                if (!basolia.IsPlaying())
                    break;
                if (!drawSignal.WaitOne(100))
                    continue;

                // Copy the video block for drawing
                int w, h;
                bool clear;
                lock (gridLock)
                {
                    w = gridW;
                    h = gridH;
                    clear = sizeChanged;
                    sizeChanged = false;
                    if (buf.Length != front.Length)
                        buf = new byte[front.Length];
                    Buffer.BlockCopy(front, 0, buf, 0, w * h * 3);
                }
                Draw(buf, w, h, clear);
                ScreenTools.Render();

                // Wait for keypress
                var keypress = Input.ReadPointerOrKeyNoBlock(InputEventType.Keyboard);
                if (keypress.ConsoleKeyInfo is ConsoleKeyInfo cki && cki.Key == ConsoleKey.Escape)
                    break;
            }

            // Unsubscribe from the video frame change event
            basolia.FrameAvailable -= UpdateFrame;
            basolia.GLReadbackEnabled = false;
            ConsoleColoring.AllowBackground = false;

            // Restore state
            playerScreen.RemoveBufferedParts();
            ConsoleColoring.LoadBack();
        }

        private static void UpdateFrame(object? sender, VideoFrameEventArgs e)
        {
            // Process the update frame event args
            IntPtr pixels = e.Backend switch
            {
                VideoRendererBackend.Software => e.SWFramePointer,
                VideoRendererBackend.OpenGL => e.GLPixelPointer,
                _ => IntPtr.Zero,
            };
            if ((e.Format != "rgb24" && e.Format != "rgba8") || pixels == IntPtr.Zero || e.Width <= 0 || e.Height <= 0)
                return;

            SampleToGrid(pixels, e.Width, e.Height, e.Stride);
        }

        private static void Draw(byte[] buf, int w, int h, bool clear)
        {
            var sb = new StringBuilder();
            if (clear)
                sb.Append("\u001b[2J");

            for (int row = 0; row < h / 2; row++)
            {
                sb.Append(ConsolePositioning.RenderChangePosition(0, row));
                int lastTop = -1, lastBot = -1;
                int ti = row * 2 * w * 3;

                for (int x = 0; x < w; x++, ti += 3)
                {
                    int bi = ti + w * 3;
                    int top = buf[ti] << 16 | buf[ti + 1] << 8 | buf[ti + 2];
                    int bot = buf[bi] << 16 | buf[bi + 1] << 8 | buf[bi + 2];

                    if (top != lastTop)
                    {
                        sb.Append(ConsoleColoring.RenderSetConsoleColor(new(buf[ti], buf[ti + 1], buf[ti + 2])));
                        lastTop = top;
                    }
                    if (bot != lastBot)
                    {
                        sb.Append(ConsoleColoring.RenderSetConsoleColor(new(buf[bi], buf[bi + 1], buf[bi + 2]), true));
                        lastBot = bot;
                    }
                    sb.Append('▀');
                }
                sb.Append("\u001b[0m");
            }
            TextWriterRaw.WriteRaw(sb.ToString());
        }

        private static void SampleToGrid(IntPtr pixels, int srcW, int srcH, long stride)
        {
            int gw = Math.Max(1, Console.WindowWidth);
            int gh = Math.Max(2, (Console.WindowHeight) * 2);

            double scale = Math.Min((double)gw / srcW, (double)gh / srcH);
            int ow = Math.Max(1, (int)(srcW * scale));
            int oh = Math.Max(2, (int)(srcH * scale) & ~1);
            int ox = (gw - ow) / 2;
            int oy = ((gh - oh) / 2) & ~1;

            int needed = gw * gh * 3;
            if (back.Length != needed)
                back = new byte[needed];
            else
                Array.Clear(back, 0, back.Length);

            if (xs.Length != ow + 1 || xsSrcW != srcW || xsOutW != ow)
            {
                xs = new int[ow + 1];
                for (int i = 0; i <= ow; i++)
                    xs[i] = (int)((long)i * srcW / ow);
                xsSrcW = srcW;
                xsOutW = ow;
            }

            unsafe
            {
                byte* src = (byte*)pixels;

                fixed (byte* dst = back)
                {
                    for (int y = 0; y < oh; y++)
                    {
                        int y0 = (int)((long)y * srcH / oh);
                        int y1 = Math.Max(y0 + 1, (int)((long)(y + 1) * srcH / oh));
                        int stepY = Math.Max(1, (y1 - y0) / 3);
                        byte* d = dst + (((oy + y) * gw) + ox) * 3;

                        for (int x = 0; x < ow; x++, d += 3)
                        {
                            int x0 = xs[x];
                            int x1 = Math.Max(x0 + 1, xs[x + 1]);
                            int stepX = Math.Max(1, (x1 - x0) / 3);

                            int r = 0, g = 0, b = 0, n = 0;
                            for (int yy = y0; yy < y1; yy += stepY)
                            {
                                byte* p = src + yy * stride + x0 * 3L;
                                for (int xx = x0; xx < x1; xx += stepX, p += 3 * stepX)
                                {
                                    r += p[0];
                                    g += p[1];
                                    b += p[2];
                                    n++;
                                }
                            }
                            d[0] = (byte)(r / n);
                            d[1] = (byte)(g / n);
                            d[2] = (byte)(b / n);
                        }
                    }
                }
            }

            lock (gridLock)
            {
                (front, back) = (back, front);
                if (gridW != gw || gridH != gh)
                    sizeChanged = true;
                gridW = gw;
                gridH = gh;
            }
            drawSignal.Set();
        }
    }
}
