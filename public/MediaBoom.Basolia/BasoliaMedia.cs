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
using System.Threading;
using MediaBoom.Basolia.Exceptions;
using MediaBoom.Basolia.File;
using MediaBoom.Basolia.Languages;
using MediaBoom.Basolia.Playback;
using MediaBoom.Native;
using MediaBoom.Native.Exceptions;
using MediaBoom.Native.Interop.Analysis;
using MediaBoom.Native.Interop.Enumerations;
using MediaBoom.Native.Interop.Event;
using MediaBoom.Native.Interop.Init;
using Textify.General;
using Threadify.Manager;

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
        internal bool isShuttingDown = false;
        internal FileType? currentFile;
        internal ManualResetEventSlim loadEvent = new(false);
        internal MpvEventId lastEventId = MpvEventId.MPV_EVENT_NONE;
        internal MpvError lastError = MpvError.MPV_ERROR_SUCCESS;

        internal MpvHandle* _libmpvHandle;

        /// <summary>
        /// String event property has changed
        /// </summary>
        public event Action<(string name, string value)>? StringEventPropertyChanged;

        /// <summary>
        /// Integer event property has changed
        /// </summary>
        public event Action<(string name, long value)>? IntegerEventPropertyChanged;

        /// <summary>
        /// Closes the libmpv instance
        /// </summary>
        public void CloseInstance()
        {
            // Verify that we've actually loaded the library!
            try
            {
                var @delegate = NativeInitializer.GetDelegate<NativeInit.mpv_terminate_destroy>(NativeInitializer.libManagerMpv, nameof(NativeInit.mpv_terminate_destroy));
                @delegate.Invoke(_libmpvHandle);
            }
            catch (Exception ex)
            {
                // TODO: MEDIABOOM_BASOLIA_EXCEPTION_INSTANCECLOSEFAILED -> "Instance closure failed"
                throw new BasoliaNativeLibraryException(LanguageTools.GetLocalized("MEDIABOOM_BASOLIA_EXCEPTION_INSTANCECLOSEFAILED") + $" {ex.Message}");
            }
        }

        private void StartEventLoop()
        {
            // Start the event loop
            var thread = new ThreadInstance("libmpv event loop", true, () => EventLoopHandler());
            thread.Start();
        }

        private void EventLoopHandler()
        {
            while (!isShuttingDown)
            {
                // Wait for an event to come, then handle
                var eventDelegate = NativeInitializer.GetDelegate<NativeEvent.mpv_wait_event>(NativeInitializer.libManagerMpv, nameof(NativeEvent.mpv_wait_event));
                var mpvEventPtr = eventDelegate(_libmpvHandle, 0.5);
                if (mpvEventPtr == 0)
                    continue;
                var mpvEvent = Marshal.PtrToStructure<MpvEvent>(mpvEventPtr);
                Debug.WriteLine(mpvEvent.event_id);
                lastEventId = mpvEvent.event_id;
                switch (mpvEvent.event_id)
                {
                    case MpvEventId.MPV_EVENT_FILE_LOADED:
                        isOpened = true;
                        lastError = MpvError.MPV_ERROR_SUCCESS;
                        loadEvent.Set();
                        break;
                    case MpvEventId.MPV_EVENT_END_FILE:
                        if (!isOpened)
                            isOpened = false;
                        if (!loadEvent.IsSet)
                        {
                            var endFile = Marshal.PtrToStructure<MpvEventEndFile>(mpvEvent.data);
                            lastError =
                                endFile.reason == MpvEofReason.MPV_END_FILE_REASON_ERROR ?
                                (MpvError)endFile.error :
                                MpvError.MPV_ERROR_GENERIC;
                            if (endFile.reason == MpvEofReason.MPV_END_FILE_REASON_ERROR)
                                loadEvent.Set();
                        }
                        break;
                    case MpvEventId.MPV_EVENT_SHUTDOWN:
                        isShuttingDown = true;
                        break;
                    case MpvEventId.MPV_EVENT_PROPERTY_CHANGE:
                        var observedProperty = Marshal.PtrToStructure<MpvEventProperty>(mpvEvent.data);
                        switch (observedProperty.format)
                        {
                            case MpvValueFormat.MPV_FORMAT_STRING:
                                {
                                    IntPtr valuePtr = Marshal.ReadIntPtr(observedProperty.data);
                                    string value = Marshal.PtrToStringAnsi(valuePtr);
                                    StringEventPropertyChanged?.Invoke((observedProperty.name, value));
                                    break;
                                }
                            case MpvValueFormat.MPV_FORMAT_INT64:
                                {
                                    long value = Marshal.ReadInt64(observedProperty.data);
                                    IntegerEventPropertyChanged?.Invoke((observedProperty.name, value));
                                    break;
                                }
                        }
                        break;
                }
            }
        }

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
                StartEventLoop();
            }
            catch (Exception ex)
            {
                throw new BasoliaNativeLibraryException(LanguageTools.GetLocalized("MEDIABOOM_BASOLIA_EXCEPTION_LIBMPVLIBINVALID").FormatString(NativeInitializer.libmpvLibPath) + $" {ex.Message}");
            }
        }
    }
}
