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
using System.Runtime.InteropServices;
using MediaBoom.Basolia.Exceptions;
using MediaBoom.Basolia.Media.Helpers;
using MediaBoom.Native;
using MediaBoom.Native.Interop.Enumerations;
using MediaBoom.Native.Interop.Rendering;

namespace MediaBoom.Basolia.Media.Video
{
    internal class SoftwareRenderer : IVideoRenderer
    {
        internal BasoliaMedia media;
        private IntPtr[] buffers = new IntPtr[2];
        private int frontIndex = 0;
        private long bufferSize = 0;
        private readonly object frameLock = new();
        private NativeRender.mpv_render_update_fn callback = (_) => VideoRenderingTools.needsRedraw = true;

        public bool NeedsRedraw =>
            VideoRenderingTools.needsRedraw;

        public void Attach()
        {
            lock (media)
            {
                // Initialize parameters in the managed context
                IntPtr advancedControlValue = Marshal.AllocHGlobal(sizeof(int));
                Marshal.WriteInt32(advancedControlValue, 1);
                IntPtr apiTypeString = Marshal.StringToHGlobalAnsi("sw");
                MpvRenderParam[] parameters =
                [
                    new() { type = MpvRenderParamType.MPV_RENDER_PARAM_ADVANCED_CONTROL, data = advancedControlValue },
                    new() { type = MpvRenderParamType.MPV_RENDER_PARAM_API_TYPE, data = apiTypeString },
                    new() { type = MpvRenderParamType.MPV_RENDER_PARAM_INVALID, data = IntPtr.Zero },
                ];

                // Add the parameters and open the render context
                unsafe
                {
                    int paramSize = Marshal.SizeOf<MpvRenderParam>();
                    var parametersMemory = Marshal.AllocHGlobal(paramSize * parameters.Length);
                    for (int i = 0; i < parameters.Length; i++)
                        Marshal.StructureToPtr(parameters[i], parametersMemory + (i * paramSize), false);
                    var renderContextCreateDelegate = NativeInitializer.GetDelegate<NativeRender.mpv_render_context_create>(NativeInitializer.libManagerMpv, nameof(NativeRender.mpv_render_context_create));
                    MpvError initRenderResult = (MpvError)renderContextCreateDelegate.Invoke(out media.renderContext, media._libmpvHandle, parametersMemory);
                    Marshal.FreeHGlobal(parametersMemory);
                    Marshal.FreeHGlobal(advancedControlValue);
                    Marshal.FreeHGlobal(apiTypeString);
                    if (initRenderResult < MpvError.MPV_ERROR_SUCCESS)
                        throw new BasoliaException("Can't initialize MPV render context", initRenderResult);
                }

                // Add the callback
                unsafe
                {
                    var setUpdateCallbackDelegate = NativeInitializer.GetDelegate<NativeRender.mpv_render_context_set_update_callback>(NativeInitializer.libManagerMpv, nameof(NativeRender.mpv_render_context_set_update_callback));
                    setUpdateCallbackDelegate.Invoke(media.renderContext, callback, IntPtr.Zero);
                }
            }
        }

        public void Detach()
        {
            lock (media)
            {
                unsafe
                {
                    var renderContextFreeDelegate = NativeInitializer.GetDelegate<NativeRender.mpv_render_context_free>(NativeInitializer.libManagerMpv, nameof(NativeRender.mpv_render_context_free));
                    renderContextFreeDelegate.Invoke(media.renderContext);
                }
            }
        }

        public void Dispose() =>
            Detach();

        public void RenderFrame()
        {
            lock (media)
            {
                if (!NeedsRedraw)
                    return;
                VideoRenderingTools.needsRedraw = false;

                // Get the size
                long width = 0, height = 0;
                try
                {
                    width = MpvPropertyHandler.GetIntegerProperty(media, "dwidth");
                    height = MpvPropertyHandler.GetIntegerProperty(media, "dheight");
                }
                catch
                {
                    return;
                }
                if (width <= 0 || height <= 0)
                    return;

                // Get the stride and the size
                int bytesPerPixel = 3;
                long stride = width * bytesPerPixel;
                long neededSize = stride * height;

                int backIndex = 1 - frontIndex;
                lock (frameLock)
                {
                    if (buffers[backIndex] == IntPtr.Zero || bufferSize != neededSize)
                    {
                        if (buffers[backIndex] != IntPtr.Zero)
                            Marshal.FreeHGlobal(buffers[backIndex]);
                        buffers[backIndex] = Marshal.AllocHGlobal((IntPtr)neededSize);
                        bufferSize = neededSize;
                    }
                }

                // Initialize parameters in the managed context
                IntPtr sizePtr = Marshal.AllocHGlobal(sizeof(int) * 2);
                Marshal.WriteInt32(sizePtr, 0, (int)width);
                Marshal.WriteInt32(sizePtr, sizeof(int), (int)height);
                IntPtr formatString = Marshal.StringToHGlobalAnsi("rgb24");
                IntPtr stridePtr = Marshal.AllocHGlobal(IntPtr.Size);
                Marshal.WriteIntPtr(stridePtr, (IntPtr)stride);
                MpvRenderParam[] parameters =
                [
                    new() { type = MpvRenderParamType.MPV_RENDER_PARAM_SW_SIZE, data = sizePtr },
                    new() { type = MpvRenderParamType.MPV_RENDER_PARAM_SW_FORMAT, data = formatString },
                    new() { type = MpvRenderParamType.MPV_RENDER_PARAM_SW_STRIDE, data = stridePtr },
                    new() { type = MpvRenderParamType.MPV_RENDER_PARAM_SW_POINTER, data = buffers[backIndex] },
                    new() { type = MpvRenderParamType.MPV_RENDER_PARAM_INVALID, data = IntPtr.Zero },
                ];

                // Add the parameters and render
                unsafe
                {
                    int paramSize = Marshal.SizeOf<MpvRenderParam>();
                    IntPtr parametersMemory = Marshal.AllocHGlobal(paramSize * parameters.Length);
                    for (int i = 0; i < parameters.Length; i++)
                        Marshal.StructureToPtr(parameters[i], parametersMemory + (i * paramSize), false);
                    var renderDelegate = NativeInitializer.GetDelegate<NativeRender.mpv_render_context_render>(NativeInitializer.libManagerMpv, nameof(NativeRender.mpv_render_context_render));
                    MpvError result = (MpvError)renderDelegate.Invoke(media.renderContext, parametersMemory);
                    Marshal.FreeHGlobal(parametersMemory);
                    Marshal.FreeHGlobal(formatString);
                    Marshal.FreeHGlobal(sizePtr);
                    Marshal.FreeHGlobal(stridePtr);
                    if (result < MpvError.MPV_ERROR_SUCCESS)
                        throw new BasoliaException("Can't render", result);
                }

                // Fire the updated frame event
                lock (frameLock)
                {
                    frontIndex = backIndex;
                }
                media.FireFrameAvailableEvent(new VideoFrameEventArgs
                {
                    SWFramePointer = buffers[frontIndex],
                    Width = (int)width,
                    Height = (int)height,
                    Stride = stride,
                    Format = "rgb24"
                });
            }
        }

        public SoftwareRenderer(BasoliaMedia media)
        {
            this.media = media;
        }
    }
}
