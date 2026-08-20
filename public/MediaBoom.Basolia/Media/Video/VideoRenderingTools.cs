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
using System.Threading;
using MediaBoom.Basolia.Exceptions;
using MediaBoom.Native;
using MediaBoom.Native.Interop.Analysis;
using MediaBoom.Native.Interop.Enumerations;

namespace MediaBoom.Basolia.Media.Video
{
    internal static class VideoRenderingTools
    {
        internal static IVideoRenderer? videoRenderer;
        internal static VideoRendererBackend backend = VideoRendererBackend.Software;
        internal static bool needsRedraw = false;
        internal static bool customVoSet = false;
        internal static bool renderLooping = true;
        internal static bool switching = true;

        internal static VideoRendererBackend Backend
        {
            get => backend;
            set
            {
                backend = value;
                switching = true;
            }
        }

        internal static void PrepareVideoRenderer(BasoliaMedia media)
        {
            SetCustomVo(media);

            // Determine which video renderer to use, and attach
            videoRenderer = Backend == VideoRendererBackend.OpenGL ? new OpenGLRenderer(media) : new SoftwareRenderer(media);
        }

        internal static void InitializeVideoRenderer()
        {
            if (videoRenderer is null)
                return;
            videoRenderer.Attach();
        }

        internal static void ShutdownVideoRenderer()
        {
            if (videoRenderer is null)
                return;
            videoRenderer.Detach();
        }

        internal static void VideoRendererLoop(BasoliaMedia basoliaMedia)
        {
            while (renderLooping)
            {
                if (switching)
                {
                    PrepareVideoRenderer(basoliaMedia);
                    InitializeVideoRenderer();
                    switching = false;
                }
                videoRenderer?.RenderFrame();
                SpinWait.SpinUntil(() => (videoRenderer?.NeedsRedraw ?? false) || !renderLooping || switching);
            }
        }

        internal static void SetCustomVo(BasoliaMedia media)
        {
            unsafe
            {
                // Set to custom VO
                if (!customVoSet)
                {
                    var setOptionDelegate = NativeInitializer.GetDelegate<NativeParameters.mpv_set_option_string>(NativeInitializer.libManagerMpv, nameof(NativeParameters.mpv_set_option_string));
                    MpvError initResult = (MpvError)setOptionDelegate.Invoke(media._libmpvHandle, "vo", "libmpv");
                    if (initResult < MpvError.MPV_ERROR_SUCCESS)
                        throw new BasoliaException("Can't initialize libmpv VO", initResult);
                    customVoSet = true;
                }
            }
        }
    }
}
