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
using SpecProbe.Loader;
using SpecProbe.Software.Platform;

namespace MediaBoom.Basolia.Media.Video
{
    internal class OpenGLRenderer : IVideoRenderer
    {
        internal BasoliaMedia media;
        private NativeRender.mpv_render_update_fn callback = (_) => VideoRenderingTools.needsRedraw = true;
        private mpv_opengl_init_params_get_proc_address procAddressDelegate = (_, _) => IntPtr.Zero;
        private IntPtr _opengl32Module;
        private IntPtr _openGLFrameworkHandle;
        private IntPtr _libGLHandle;
        private uint ownedFbo;
        private uint colorTexture;
        private int texWidth, texHeight;

        public bool NeedsRedraw =>
            VideoRenderingTools.needsRedraw;

        public void Attach()
        {
            lock (media)
            {
                // Create OpenGL init parameters
                procAddressDelegate = OpenGLGetProcAddress;
                MpvOpenGLInitParams glParams = new() { get_proc_address = procAddressDelegate, get_proc_address_ctx = IntPtr.Zero };
                IntPtr glParamsMemory = Marshal.AllocHGlobal(Marshal.SizeOf<MpvOpenGLInitParams>());
                Marshal.StructureToPtr(glParams, glParamsMemory, false);

                // Initialize parameters in the managed context
                IntPtr advancedControlValue = Marshal.AllocHGlobal(sizeof(int));
                Marshal.WriteInt32(advancedControlValue, 1);
                IntPtr apiTypeString = Marshal.StringToHGlobalAnsi("opengl");
                MpvRenderParam[] parameters =
                [
                    new() { type = MpvRenderParamType.MPV_RENDER_PARAM_ADVANCED_CONTROL, data = advancedControlValue },
                    new() { type = MpvRenderParamType.MPV_RENDER_PARAM_API_TYPE, data = apiTypeString },
                    new() { type = MpvRenderParamType.MPV_RENDER_PARAM_OPENGL_INIT_PARAMS, data = glParamsMemory },
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
                GLFunctions.Load(name => OpenGLGetProcAddress(IntPtr.Zero, name));
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

                // Initialize parameters in the managed context
                EnsureFbo((int)width, (int)height);
                MpvOpenGLFbo fboParameters = new() { fbo = (int)ownedFbo, w = (int)width, h = (int)height };
                int fboSize = Marshal.SizeOf<MpvOpenGLFbo>();
                IntPtr fboMemory = Marshal.AllocHGlobal(fboSize);
                Marshal.StructureToPtr(fboParameters, fboMemory, false);
                IntPtr flipYPtr = Marshal.AllocHGlobal(sizeof(int));
                Marshal.WriteInt32(flipYPtr, 1);
                MpvRenderParam[] parameters =
                [
                    new() { type = MpvRenderParamType.MPV_RENDER_PARAM_OPENGL_FBO, data = fboMemory },
                    new() { type = MpvRenderParamType.MPV_RENDER_PARAM_FLIP_Y, data = flipYPtr },
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
                    Marshal.FreeHGlobal(flipYPtr);
                    Marshal.FreeHGlobal(fboMemory);
                    Marshal.FreeHGlobal(parametersMemory);
                    if (result < MpvError.MPV_ERROR_SUCCESS)
                        throw new BasoliaException("Can't render", result);
                }

                // Fire the updated frame event
                media.FireFrameAvailableEvent(new VideoFrameEventArgs
                {
                    GLTexturePointer = colorTexture,
                    Width = (int)width,
                    Height = (int)height,
                    Format = "rgb24"
                });
            }
        }

        private void EnsureFbo(int width, int height)
        {
            if (ownedFbo != 0 && texWidth == width && texHeight == height) return;

            if (ownedFbo != 0)
            {
                uint fbo = ownedFbo, tex = colorTexture;
                GLFunctions.DeleteFramebuffers(1, ref fbo);
                GLFunctions.DeleteTextures(1, ref tex);
            }

            GLFunctions.GenTextures(1, out colorTexture);
            GLFunctions.BindTexture(GLConstants.GL_TEXTURE_2D, colorTexture);
            GLFunctions.TexImage2D(GLConstants.GL_TEXTURE_2D, 0, (int)GLConstants.GL_RGBA8,
                width, height, 0, GLConstants.GL_RGBA, GLConstants.GL_UNSIGNED_BYTE, IntPtr.Zero);
            GLFunctions.TexParameteri(GLConstants.GL_TEXTURE_2D, GLConstants.GL_TEXTURE_MIN_FILTER, (int)GLConstants.GL_LINEAR);
            GLFunctions.TexParameteri(GLConstants.GL_TEXTURE_2D, GLConstants.GL_TEXTURE_MAG_FILTER, (int)GLConstants.GL_LINEAR);

            GLFunctions.GenFramebuffers(1, out ownedFbo);
            GLFunctions.BindFramebuffer(GLConstants.GL_FRAMEBUFFER, ownedFbo);
            GLFunctions.FramebufferTexture2D(GLConstants.GL_FRAMEBUFFER, GLConstants.GL_COLOR_ATTACHMENT0,
                GLConstants.GL_TEXTURE_2D, colorTexture, 0);

            uint status = GLFunctions.CheckFramebufferStatus(GLConstants.GL_FRAMEBUFFER);
            if (status != GLConstants.GL_FRAMEBUFFER_COMPLETE)
                throw new BasoliaException($"Framebuffer incomplete: 0x{status:X}", MpvError.MPV_ERROR_GENERIC);

            texWidth = width;
            texHeight = height;
        }

        private nint OpenGLGetProcAddress(nint ctx, string name)
        {
            // If we're running on Windows
            // TODO: This is only a referernce implementation. Remove it once we make a pubilic property for handle.
            if (PlatformHelper.IsOnWindows())
            {
                IntPtr addr = MpvOpenGLHelpers.wglGetProcAddress(name);
                if (addr == IntPtr.Zero || addr == (IntPtr)1 || addr == (IntPtr)2 || addr == (IntPtr)3 || addr == (IntPtr)(-1))
                {
                    if (_opengl32Module == IntPtr.Zero)
                        _opengl32Module = MpvOpenGLHelpers.LoadLibrary("opengl32.dll");
                    addr = MpvOpenGLHelpers.GetProcAddress(_opengl32Module, name);
                }
                return addr;
            }
            else if (PlatformHelper.IsOnMacOS())
            {
                if (_openGLFrameworkHandle == IntPtr.Zero)
                    _openGLFrameworkHandle = MpvOpenGLHelpers.dlopen(
                        "/System/Library/Frameworks/OpenGL.framework/OpenGL", 2);
                return MpvOpenGLHelpers.dlsym(_openGLFrameworkHandle, name);
            }
            else if (PlatformHelper.IsOnUnix())
            {
                if (_libGLHandle == IntPtr.Zero)
                    _libGLHandle = MpvOpenGLHelpers.dlopen("libGL.so.1", 2);
                IntPtr addr = MpvOpenGLHelpers.glXGetProcAddressARB(name);
                return addr != IntPtr.Zero ? addr : MpvOpenGLHelpers.dlsym(_libGLHandle, name);
            }
            return IntPtr.Zero;
        }

        public OpenGLRenderer(BasoliaMedia media)
        {
            this.media = media;
        }
    }
}
