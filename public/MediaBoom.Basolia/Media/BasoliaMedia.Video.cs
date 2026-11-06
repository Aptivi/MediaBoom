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
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using MediaBoom.Basolia.Exceptions;
using MediaBoom.Basolia.Languages;
using MediaBoom.Basolia.Media.Helpers;
using MediaBoom.Basolia.Media.Playback;
using MediaBoom.Basolia.Media.Video;
using MediaBoom.Native;
using MediaBoom.Native.Interop.Analysis;
using MediaBoom.Native.Interop.Enumerations;

namespace MediaBoom.Basolia.Media
{
    /// <summary>
    /// Basolia instance for media manipulation
    /// </summary>
    public partial class BasoliaMedia
    {
        internal IVideoRenderer? videoRenderer;
        internal VideoRendererBackend backend = VideoRendererBackend.Software;
        internal bool needsRedraw = false;
        internal bool customVoSet = false;
        internal bool renderLooping = true;
        internal bool switching = true;
        internal readonly AutoResetEvent redrawSignal = new(false);
        internal readonly ManualResetEventSlim switchDone = new(false);

        /// <summary>
        /// Current video rendering backend
        /// </summary>
        public VideoRendererBackend Backend =>
            backend;

        /// <summary>
        /// Switches between backends
        /// </summary>
        /// <param name="target">The target video backend</param>
        public void SwitchBackend(VideoRendererBackend target)
        {
            // Remember the selected video track and tear down the chain if there is one
            string previousVid = MpvPropertyHandler.GetStringProperty(this, "vid");
            bool hadVideo = previousVid != "no";
            if (hadVideo)
                MpvPropertyHandler.SetStringProperty(this, "vid", "no");

            // Wait for the renderer to swap context
            switchDone.Reset();
            backend = target;
            switching = true;
            redrawSignal.Set();
            switchDone.Wait();

            // Go back to the previous vid
            if (hadVideo)
                MpvPropertyHandler.SetStringProperty(this, "vid", previousVid);
        }

        internal void PrepareVideoRenderer()
        {
            SetCustomVo();

            // Determine which video renderer to use, and attach
            videoRenderer = Backend == VideoRendererBackend.OpenGL ? new OpenGLRenderer(this) : new SoftwareRenderer(this);
        }

        internal void InitializeVideoRenderer()
        {
            if (videoRenderer is null)
                return;
            renderLooping = true;
            videoRenderer.Attach();
        }

        internal void ShutdownVideoRenderer(bool signal = true)
        {
            if (videoRenderer is null)
                return;
            if (!signal)
                videoRenderer.Detach();
            else
                renderLooping = false;
        }

        internal void VideoRendererLoop()
        {
            MpvPropertyHandler.ObserveProperty(this, "dwidth");
            MpvPropertyHandler.ObserveProperty(this, "dheight");

            IntegerEventPropertyChanged += (evt) =>
            {
                if (evt.name == "dwidth")
                    cachedWidth = (int)evt.value;
                else if (evt.name == "dheight")
                    cachedHeight = (int)evt.value;
            };
            while (renderLooping)
            {
                try
                {
                    if (switching)
                    {
                        ShutdownVideoRenderer(false);
                        PrepareVideoRenderer();
                        InitializeVideoRenderer();
                        switching = false;
                        switchDone.Set();
                    }
                    videoRenderer?.RenderFrame();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[RENDER THREAD EXCEPTION] {ex}");
                }
                redrawSignal.WaitOne(TimeSpan.FromMilliseconds(16));
            }
            ShutdownVideoRenderer(false);
        }

        internal void SetCustomVo()
        {
            unsafe
            {
                // Set to custom VO
                if (!customVoSet)
                {
                    var setOptionDelegate = NativeInitializer.GetDelegate<NativeParameters.mpv_set_option_string>(NativeInitializer.libManagerMpv, nameof(NativeParameters.mpv_set_option_string));
                    MpvError initResult = (MpvError)setOptionDelegate.Invoke(_libmpvHandle, "vo", "libmpv");
                    if (initResult < MpvError.MPV_ERROR_SUCCESS)
                        throw new BasoliaException(LanguageTools.GetLocalized("MEDIABOOM_BASOLIA_VIDEO_EXCEPTION_VOINITFAILED"), initResult);
                    customVoSet = true;
                }
            }
        }
    }
}
