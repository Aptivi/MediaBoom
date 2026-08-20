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
using MediaBoom.Native.Exceptions;
using MediaBoom.Native.Interop.Enumerations;

namespace MediaBoom.Native.Interop.Rendering
{
    internal delegate nint mpv_opengl_init_params_get_proc_address(nint ctx, [In][MarshalAs(UnmanagedType.LPStr)] string name);

    [UnmanagedFunctionPointer(CallingConvention.Winapi)]
    internal delegate void GenTexturesFn(int n, out uint textures);

    [UnmanagedFunctionPointer(CallingConvention.Winapi)]
    internal delegate void BindTextureFn(uint target, uint texture);

    [UnmanagedFunctionPointer(CallingConvention.Winapi)]
    internal delegate void TexImage2DFn(uint target, int level, int internalFormat, int width, int height, int border, uint format, uint type, IntPtr pixels);

    [UnmanagedFunctionPointer(CallingConvention.Winapi)]
    internal delegate void TexParameteriFn(uint target, uint pname, int param);

    [UnmanagedFunctionPointer(CallingConvention.Winapi)]
    internal delegate void DeleteTexturesFn(int n, ref uint textures);

    [UnmanagedFunctionPointer(CallingConvention.Winapi)]
    internal delegate void GenFramebuffersFn(int n, out uint framebuffers);

    [UnmanagedFunctionPointer(CallingConvention.Winapi)]
    internal delegate void BindFramebufferFn(uint target, uint framebuffer);

    [UnmanagedFunctionPointer(CallingConvention.Winapi)]
    internal delegate void FramebufferTexture2DFn(uint target, uint attachment, uint textarget, uint texture, int level);

    [UnmanagedFunctionPointer(CallingConvention.Winapi)]
    internal delegate void DeleteFramebuffersFn(int n, ref uint framebuffers);

    [UnmanagedFunctionPointer(CallingConvention.Winapi)]
    internal delegate uint CheckFramebufferStatusFn(uint target);

    [StructLayout(LayoutKind.Sequential)]
    internal struct MpvOpenGLInitParams
    {
        public mpv_opengl_init_params_get_proc_address get_proc_address;
        public nint get_proc_address_ctx;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct MpvOpenGLFbo
    {
        public int fbo;
        public int w;
        public int h;
        public int internal_format;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct MpvOpenGLDrmParams
    {
        public int fd;
        public int crtc_id;
        public int connector_id;
        public nint atomic_request_ptr;
        public int render_fd;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct MpvOpenGLDrmDrawSurfaceSize
    {
        public int width;
        public int height;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct MpvOpenGLDrmParamsV2
    {
        public int fd;
        public int crtc_id;
        public int connector_id;
        public nint atomic_request_ptr;
        public int render_fd;
    }

    internal static class GLConstants
    {
        public const uint GL_TEXTURE_2D = 0x0DE1;
        public const uint GL_RGBA8 = 0x8058;
        public const uint GL_RGBA = 0x1908;
        public const uint GL_UNSIGNED_BYTE = 0x1401;
        public const uint GL_TEXTURE_MIN_FILTER = 0x2801;
        public const uint GL_TEXTURE_MAG_FILTER = 0x2800;
        public const uint GL_LINEAR = 0x2601;
        public const uint GL_FRAMEBUFFER = 0x8D40;
        public const uint GL_COLOR_ATTACHMENT0 = 0x8CE0;
        public const uint GL_FRAMEBUFFER_COMPLETE = 0x8CD5;
    }

    internal static class GLFunctions
    {
        public static GenTexturesFn GenTextures = null!;
        public static BindTextureFn BindTexture = null!;
        public static TexImage2DFn TexImage2D = null!;
        public static TexParameteriFn TexParameteri = null!;
        public static DeleteTexturesFn DeleteTextures = null!;
        public static GenFramebuffersFn GenFramebuffers = null!;
        public static BindFramebufferFn BindFramebuffer = null!;
        public static FramebufferTexture2DFn FramebufferTexture2D = null!;
        public static DeleteFramebuffersFn DeleteFramebuffers = null!;
        public static CheckFramebufferStatusFn CheckFramebufferStatus = null!;
        private static bool loaded;

        public static void Load(Func<string, IntPtr> getProcAddress)
        {
            if (loaded) return;

            T Bind<T>(string name) where T : Delegate
            {
                IntPtr ptr = getProcAddress(name);
                if (ptr == IntPtr.Zero)
                    throw new BasoliaNativeLibraryException($"GL function not found: {name}");
                return Marshal.GetDelegateForFunctionPointer<T>(ptr);
            }

            GenTextures = Bind<GenTexturesFn>("glGenTextures");
            BindTexture = Bind<BindTextureFn>("glBindTexture");
            TexImage2D = Bind<TexImage2DFn>("glTexImage2D");
            TexParameteri = Bind<TexParameteriFn>("glTexParameteri");
            DeleteTextures = Bind<DeleteTexturesFn>("glDeleteTextures");
            GenFramebuffers = Bind<GenFramebuffersFn>("glGenFramebuffers");
            BindFramebuffer = Bind<BindFramebufferFn>("glBindFramebuffer");
            FramebufferTexture2D = Bind<FramebufferTexture2DFn>("glFramebufferTexture2D");
            DeleteFramebuffers = Bind<DeleteFramebuffersFn>("glDeleteFramebuffers");
            CheckFramebufferStatus = Bind<CheckFramebufferStatusFn>("glCheckFramebufferStatus");

            loaded = true;
        }
    }

    internal static class MpvOpenGLHelpers
    {
        [DllImport("opengl32.dll", CharSet = CharSet.Ansi)]
        internal static extern IntPtr wglGetProcAddress(string name);

        [DllImport("libGL.so.1")]
        internal static extern IntPtr glXGetProcAddressARB([MarshalAs(UnmanagedType.LPStr)] string procName);
    }
}
