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
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using MediaBoom.Basolia.Exceptions;
using MediaBoom.Basolia.Languages;
using MediaBoom.Basolia.Media.File;
using MediaBoom.Basolia.Media.Helpers;
using MediaBoom.Basolia.Media.Playback;
using MediaBoom.Basolia.Media.Video;
using MediaBoom.Native;
using MediaBoom.Native.Exceptions;
using MediaBoom.Native.Interop.Analysis;
using MediaBoom.Native.Interop.Enumerations;
using MediaBoom.Native.Interop.Event;
using MediaBoom.Native.Interop.Init;
using MediaBoom.Native.Interop.Rendering;
using Textify.General;
using Threadify.Manager;

namespace MediaBoom.Basolia.Media
{
    /// <summary>
    /// Basolia instance for media manipulation
    /// </summary>
    public unsafe partial class BasoliaMedia
    {
        internal string radioIcy = "";
        internal PlaybackState state = PlaybackState.Stopped;
        internal bool isOpened = false;
        internal bool isRadioStation = false;
        internal bool isOutputOpen = false;
        internal bool isShuttingDown = false;
        internal FileType? currentFile;
        internal MpvRenderContext* renderContext;
        internal ManualResetEventSlim loadEvent = new(false);
        internal MpvEventId lastEventId = MpvEventId.MPV_EVENT_NONE;
        internal MpvError lastError = MpvError.MPV_ERROR_SUCCESS;
        internal ThreadInstance? renderThread;
        internal MpvHandle* _libmpvHandle;
        internal int cachedWidth;
        internal int cachedHeight;

        /// <summary>
        /// String event property has changed
        /// </summary>
        public event Action<(string name, string value)>? StringEventPropertyChanged;

        /// <summary>
        /// Integer event property has changed
        /// </summary>
        public event Action<(string name, long value)>? IntegerEventPropertyChanged;

        /// <summary>
        /// Double event property has changed
        /// </summary>
        public event Action<(string name, double value)>? DoubleEventPropertyChanged;

        /// <summary>
        /// Flag event property has changed
        /// </summary>
        public event Action<(string name, bool value)>? FlagEventPropertyChanged;

        /// <summary>
        /// Node map event property has changed
        /// </summary>
        public event Action<(string name, Dictionary<string, string> value)>? NodeMapEventPropertyChanged;

        /// <summary>
        /// Video frame data available
        /// </summary>
        public event EventHandler<VideoFrameEventArgs>? FrameAvailable;

        /// <summary>
        /// Closes the libmpv instance
        /// </summary>
        public void CloseInstance()
        {
            // Verify that we've actually loaded the library!
            try
            {
                VideoRenderingTools.ShutdownVideoRenderer();
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
                lastEventId = mpvEvent.event_id;
                switch (mpvEvent.event_id)
                {
                    case MpvEventId.MPV_EVENT_FILE_LOADED:
                        isOpened = true;
                        lastError = MpvError.MPV_ERROR_SUCCESS;
                        loadEvent.Set();
                        break;
                    case MpvEventId.MPV_EVENT_END_FILE:
                        var endFile = Marshal.PtrToStructure<MpvEventEndFile>(mpvEvent.data);
                        Debug.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] END_FILE reason={endFile.reason} error={endFile.error}");
                        if (!loadEvent.IsSet)
                        {
                            lastError =
                                endFile.reason == MpvEofReason.MPV_END_FILE_REASON_ERROR ?
                                (MpvError)endFile.error :
                                MpvError.MPV_ERROR_GENERIC;
                            if (endFile.reason == MpvEofReason.MPV_END_FILE_REASON_ERROR)
                                loadEvent.Set();
                        }
                        else
                        {
                            if (endFile.reason == MpvEofReason.MPV_END_FILE_REASON_EOF)
                            {
                                if (isOpened)
                                    isOpened = false;
                                state = PlaybackState.Stopped;
                            }
                        }
                        break;
                    case MpvEventId.MPV_EVENT_SHUTDOWN:
                        isShuttingDown = true;
                        break;
                    case MpvEventId.MPV_EVENT_LOG_MESSAGE:
                        var logMsg = Marshal.PtrToStructure<MpvEventLogMessage>(mpvEvent.data);
                        Debug.WriteLine($"[{logMsg.prefix}] {logMsg.text?.TrimEnd()}");
                        break;
                    case MpvEventId.MPV_EVENT_PROPERTY_CHANGE:
                        var observedProperty = Marshal.PtrToStructure<MpvEventProperty>(mpvEvent.data);
                        if (observedProperty.format == MpvValueFormat.MPV_FORMAT_NONE)
                            continue;
                        var propertyNode = Marshal.PtrToStructure<MpvNode>(observedProperty.data);
                        switch (propertyNode.format)
                        {
                            case MpvValueFormat.MPV_FORMAT_STRING:
                                {
                                    IntPtr valuePtr = Marshal.ReadIntPtr(propertyNode.u.@string);
                                    string value = Marshal.PtrToStringAnsi(valuePtr);
                                    StringEventPropertyChanged?.Invoke((observedProperty.name, value));
                                    break;
                                }
                            case MpvValueFormat.MPV_FORMAT_INT64:
                                {
                                    long value = propertyNode.u.int64;
                                    IntegerEventPropertyChanged?.Invoke((observedProperty.name, value));
                                    break;
                                }
                            case MpvValueFormat.MPV_FORMAT_DOUBLE:
                                {
                                    double value = propertyNode.u.double_;
                                    DoubleEventPropertyChanged?.Invoke((observedProperty.name, value));
                                    break;
                                }
                            case MpvValueFormat.MPV_FORMAT_FLAG:
                                {
                                    bool value = propertyNode.u.flag > 0;
                                    FlagEventPropertyChanged?.Invoke((observedProperty.name, value));
                                    break;
                                }
                            case MpvValueFormat.MPV_FORMAT_NODE_MAP:
                                {
                                    Debug.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] metadata event fired, observedProperty.format={observedProperty.format}");
                                    var nodeMap = Marshal.PtrToStructure<MpvNodeList>(propertyNode.u.list);
                                    int num = nodeMap.num;
                                    Debug.WriteLine($"  num={num}");
                                    int nodeSize = Marshal.SizeOf<MpvNode>();
                                    var strings = new Dictionary<string, string>();
                                    for (int i = 0; i < num; i++)
                                    {
                                        // Get the key and the value
                                        IntPtr keyPtr = Marshal.ReadIntPtr(nodeMap.keys, i * IntPtr.Size);
                                        IntPtr valueNodePtr = IntPtr.Add(nodeMap.values, i * nodeSize);
                                        string key = Marshal.PtrToStringAnsi(keyPtr);
                                        var valueNode = Marshal.PtrToStructure<MpvNode>(valueNodePtr);

                                        // Convert the value to the string
                                        string value = valueNode.format == MpvValueFormat.MPV_FORMAT_STRING ? Marshal.PtrToStringAnsi(valueNode.u.@string) : "";
                                        Debug.WriteLine($"  {key} = {value}");
                                        strings[key] = value;
                                    }
                                    NodeMapEventPropertyChanged?.Invoke((observedProperty.name, strings));
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

                NativeInitializer.GetDelegate<NativeLogging.mpv_request_log_messages>(NativeInitializer.libManagerMpv, nameof(NativeLogging.mpv_request_log_messages)).Invoke(_libmpvHandle, "v");
                MpvPropertyHandler.ObserveProperty(this, "pause");
                FlagEventPropertyChanged += (evt) =>
                    Debug.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] {evt.name} = {evt.value}");
                renderThread ??= new("Video renderer", true, () => VideoRenderingTools.VideoRendererLoop(this));
                renderThread.Start();
                SpinWait.SpinUntil(() => !VideoRenderingTools.switching);
                StartEventLoop();
            }
            catch (Exception ex)
            {
                throw new BasoliaNativeLibraryException(LanguageTools.GetLocalized("MEDIABOOM_BASOLIA_EXCEPTION_LIBMPVLIBINVALID").FormatString(NativeInitializer.libmpvLibPath) + $" {ex.Message}");
            }
        }
    }
}
