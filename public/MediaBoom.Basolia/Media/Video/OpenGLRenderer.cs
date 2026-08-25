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
using System.Diagnostics;
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
        private NativeRender.mpv_render_update_fn callback = (_) =>
        {
            VideoRenderingTools.needsRedraw = true;
            VideoRenderingTools.redrawSignal.Set();
        };
        private mpv_opengl_init_params_get_proc_address procAddressDelegate = (_, _) => IntPtr.Zero;
        private LibraryManager? libGLLibManager;
        private uint ownedFbo;
        private uint colorTexture;
        private int texWidth, texHeight;
        private IntPtr glfwWindow;

        public bool NeedsRedraw
        {
            get
            {
                unsafe
                {
                    ulong needed = NativeInitializer.GetDelegate<NativeRender.mpv_render_context_update>(NativeInitializer.libManagerMpv, nameof(NativeRender.mpv_render_context_update)).Invoke(media.renderContext);
                    bool isNeeded = needed == 1 << 0;
                    return isNeeded;
                }
            }
        }

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

                // Open the GL context
                GLFunctions.LoadEssentials(name => OpenGLGetProcAddress(IntPtr.Zero, name));
                CreateGLContext();

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
                if (ownedFbo != 0)
                {
                    uint fbo = ownedFbo, tex = colorTexture;
                    GLFunctions.DeleteFramebuffers(1, ref fbo);
                    GLFunctions.DeleteTextures(1, ref tex);
                    ownedFbo = 0;
                    colorTexture = 0;
                }
                unsafe
                {
                    var renderContextFreeDelegate = NativeInitializer.GetDelegate<NativeRender.mpv_render_context_free>(NativeInitializer.libManagerMpv, nameof(NativeRender.mpv_render_context_free));
                    renderContextFreeDelegate.Invoke(media.renderContext);
                }
                DestroyGLContext();
            }
        }

        public void Dispose() =>
            Detach();

        public void RenderFrame()
        {
            unsafe
            {
                if (!NeedsRedraw)
                    return;
                VideoRenderingTools.needsRedraw = false;

                // Get the size
                long width = media.cachedWidth > 0 ? media.cachedWidth : 640;
                long height = media.cachedHeight > 0 ? media.cachedHeight : 480;

                // Initialize parameters in the managed context
                EnsureFbo((int)width, (int)height);
                MpvOpenGLFbo fboParameters = new() { fbo = (int)ownedFbo, w = (int)width, h = (int)height };
                int fboSize = Marshal.SizeOf<MpvOpenGLFbo>();
                IntPtr fboMemory = Marshal.AllocHGlobal(fboSize);
                Marshal.StructureToPtr(fboParameters, fboMemory, false);
                IntPtr flipYPtr = Marshal.AllocHGlobal(sizeof(int));
                Marshal.WriteInt32(flipYPtr, 1);
                IntPtr blockForTargetTimePtr = Marshal.AllocHGlobal(sizeof(int));
                Marshal.WriteInt32(blockForTargetTimePtr, 0);
                MpvRenderParam[] parameters =
                [
                    new() { type = MpvRenderParamType.MPV_RENDER_PARAM_OPENGL_FBO, data = fboMemory },
                    new() { type = MpvRenderParamType.MPV_RENDER_PARAM_FLIP_Y, data = flipYPtr },
                    new() { type = MpvRenderParamType.MPV_RENDER_PARAM_BLOCK_FOR_TARGET_TIME, data = blockForTargetTimePtr },
                    new() { type = MpvRenderParamType.MPV_RENDER_PARAM_INVALID, data = IntPtr.Zero },
                ];

                // Add the parameters and render
                int paramSize = Marshal.SizeOf<MpvRenderParam>();
                IntPtr parametersMemory = Marshal.AllocHGlobal(paramSize * parameters.Length);
                for (int i = 0; i < parameters.Length; i++)
                    Marshal.StructureToPtr(parameters[i], parametersMemory + (i * paramSize), false);
                var renderDelegate = NativeInitializer.GetDelegate<NativeRender.mpv_render_context_render>(NativeInitializer.libManagerMpv, nameof(NativeRender.mpv_render_context_render));
                MpvError result = (MpvError)renderDelegate.Invoke(media.renderContext, parametersMemory);
                Marshal.FreeHGlobal(flipYPtr);
                Marshal.FreeHGlobal(blockForTargetTimePtr);
                Marshal.FreeHGlobal(fboMemory);
                Marshal.FreeHGlobal(parametersMemory);
                if (result < MpvError.MPV_ERROR_SUCCESS)
                    throw new BasoliaException("Can't render before swap", result);
                var reportSwapDelegate = NativeInitializer.GetDelegate<NativeRender.mpv_render_context_report_swap>(NativeInitializer.libManagerMpv, nameof(NativeRender.mpv_render_context_report_swap));
                reportSwapDelegate.Invoke(media.renderContext);

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
            Debug.WriteLine($"framebuffer status returned: {status:x} = 0x8CD5");
            if (status != GLConstants.GL_FRAMEBUFFER_COMPLETE)
                throw new BasoliaException($"Framebuffer incomplete: 0x{status:X}", MpvError.MPV_ERROR_GENERIC);

            texWidth = width;
            texHeight = height;
        }

        private nint OpenGLGetProcAddress(nint ctx, string name)
        {
            // If we're running on Windows
            if (PlatformHelper.IsOnWindows())
            {
                IntPtr addr = MpvOpenGLHelpers.wglGetProcAddress(name);
                if (addr == IntPtr.Zero || addr == (IntPtr)1 || addr == (IntPtr)2 || addr == (IntPtr)3 || addr == (IntPtr)(-1))
                {
                    if (libGLLibManager is null)
                    {
                        libGLLibManager = new(new LibraryFile(["opengl32.dll"]));
                        libGLLibManager.LoadNativeLibrary();
                    }
                    addr = libGLLibManager.GetNativeMethodAddress(name);
                }
                return addr;
            }
            else if (PlatformHelper.IsOnMacOS())
            {
                if (libGLLibManager is null)
                {
                    libGLLibManager = new(new LibraryFile(["/System/Library/Frameworks/OpenGL.framework/OpenGL"]));
                    libGLLibManager.LoadNativeLibrary();
                }
                return libGLLibManager.GetNativeMethodAddress(name);
            }
            else if (PlatformHelper.IsOnUnix())
            {
                if (libGLLibManager is null)
                {
                    libGLLibManager = new(new LibraryFile(["libGL.so.1"]));
                    libGLLibManager.LoadNativeLibrary();
                }
                IntPtr addr = MpvOpenGLHelpers.glXGetProcAddressARB(name);
                return addr != IntPtr.Zero ? addr : libGLLibManager.GetNativeMethodAddress(name);
            }
            return IntPtr.Zero;
        }

        public void CreateGLContext()
        {
            GLFW.Load();
            var glfwInit = NativeInitializer.GetDelegate<GLFW.glfwInit>(GLFW.glfwLibManager, nameof(GLFW.glfwInit));
            var glfwWindowHint = NativeInitializer.GetDelegate<GLFW.glfwWindowHint>(GLFW.glfwLibManager, nameof(GLFW.glfwWindowHint));
            var glfwCreateWindow = NativeInitializer.GetDelegate<GLFW.glfwCreateWindow>(GLFW.glfwLibManager, nameof(GLFW.glfwCreateWindow));
            var glfwMakeContextCurrent = NativeInitializer.GetDelegate<GLFW.glfwMakeContextCurrent>(GLFW.glfwLibManager, nameof(GLFW.glfwMakeContextCurrent));

            if (glfwInit() == GLFW.GLFW_FALSE)
                throw new BasoliaException("Failed to initialize GLFW", MpvError.MPV_ERROR_GENERIC);

            glfwWindowHint(GLFW.GLFW_VISIBLE, GLFW.GLFW_FALSE);
            glfwWindowHint(GLFW.GLFW_CONTEXT_VERSION_MAJOR, 3);
            glfwWindowHint(GLFW.GLFW_CONTEXT_VERSION_MINOR, 3);
            glfwWindowHint(GLFW.GLFW_OPENGL_PROFILE, GLFW.GLFW_OPENGL_CORE_PROFILE);

            glfwWindow = glfwCreateWindow(1, 1, "BasoliaMedia (offscreen)", IntPtr.Zero, IntPtr.Zero);
            if (glfwWindow == IntPtr.Zero)
                throw new BasoliaException("Failed to create GL context", MpvError.MPV_ERROR_GENERIC);

            glfwMakeContextCurrent(glfwWindow);
        }

        public void DestroyGLContext()
        {
            var glfwDestroyWindow = NativeInitializer.GetDelegate<GLFW.glfwDestroyWindow>(GLFW.glfwLibManager, nameof(GLFW.glfwDestroyWindow));
            var glfwTerminate = NativeInitializer.GetDelegate<GLFW.glfwTerminate>(GLFW.glfwLibManager, nameof(GLFW.glfwTerminate));
            if (glfwWindow != IntPtr.Zero)
            {
                glfwDestroyWindow(glfwWindow);
                glfwWindow = IntPtr.Zero;
            }
            glfwTerminate();
        }

        public OpenGLRenderer(BasoliaMedia media)
        {
            this.media = media;
        }
    }
}
