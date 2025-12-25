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

using MediaBoom.Basolia.Exceptions;
using MediaBoom.Basolia.File;
using MediaBoom.Basolia.Languages;
using MediaBoom.Basolia.Playback;
using MediaBoom.Native;
using MediaBoom.Native.Exceptions;
using MediaBoom.Native.Interop.Enumerations;
using MediaBoom.Native.Interop.Init;
using System;
using System.Diagnostics;
using Textify.General;

namespace MediaBoom.Basolia
{
    /// <summary>
    /// Basolia instance for media manipulation
    /// </summary>
    public unsafe class BasoliaMedia
    {
        internal bool bufferPlaying = false;
        internal bool holding = false;
        internal string radioIcy = "";
        internal PlaybackState state = PlaybackState.Stopped;
        internal bool isOpened = false;
        internal bool isRadioStation = false;
        internal bool isOutputOpen = false;
        internal FileType? currentFile;

        internal MpvHandle* _libmpvHandle;

        /// <summary>
        /// Makes a new Basolia instance and initializes the library, if necessary.
        /// </summary>
        /// <param name="root">Root directory that contains native library files</param>
        /// <exception cref="BasoliaNativeLibraryException"></exception>
        public BasoliaMedia(string root = "")
        {
            if (!InitBasolia.BasoliaInitialized)
                InitBasolia.Init(root);

            // Verify that we've actually loaded the library!
            try
            {
                var @delegate = NativeInitializer.GetDelegate<NativeInit.mpv_create>(NativeInitializer.libManagerMpv, nameof(NativeInit.mpv_create));
                var handle = @delegate.Invoke();
                Debug.WriteLine($"Verifying libmpv version: {NativeInitializer.NativeLibVersion}");

                var initDelegate = NativeInitializer.GetDelegate<NativeInit.mpv_initialize>(NativeInitializer.libManagerMpv, nameof(NativeInit.mpv_initialize));
                MpvError initResult = (MpvError)initDelegate.Invoke(handle);
                if (initResult < MpvError.MPV_ERROR_SUCCESS)
                    throw new BasoliaException("Can't initialize MPV core", initResult);
                _libmpvHandle = handle;
            }
            catch (Exception ex)
            {
                throw new BasoliaNativeLibraryException(LanguageTools.GetLocalized("MEDIABOOM_BASOLIA_EXCEPTION_LIBMPVLIBINVALID").FormatString(NativeInitializer.libmpvLibPath) + $" {ex.Message}");
            }
        }
    }
}
