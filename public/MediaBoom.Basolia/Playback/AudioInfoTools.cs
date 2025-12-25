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

using MediaBoom.Basolia.File;
using System;
using System.Diagnostics;
using MediaBoom.Basolia.Exceptions;
using MediaBoom.Native.Interop.Enumerations;
using MediaBoom.Basolia.Helpers;
using MediaBoom.Basolia.Languages;

namespace MediaBoom.Basolia.Playback
{
    /// <summary>
    /// Audio information tools
    /// </summary>
    public static class AudioInfoTools
    {
        /// <summary>
        /// Gets the duration of the file in seconds
        /// </summary>
        /// <param name="basolia">Basolia instance that contains a valid handle</param>
        /// <returns>Duration in seconds.</returns>
        public static long GetDuration(BasoliaMedia? basolia)
        {
            InitBasolia.CheckInited();
            if (basolia is null)
                throw new BasoliaException(LanguageTools.GetLocalized("MEDIABOOM_BASOLIA_EXCEPTION_BASOLIAMEDIA"), MpvError.MPV_ERROR_INVALID_PARAMETER);

            // Check to see if the file is open
            if (!FileTools.IsOpened(basolia))
                throw new BasoliaException(LanguageTools.GetLocalized("MEDIABOOM_BASOLIA_FORMAT_EXCEPTION_FILENOTOPEN_QUERY"), MpvError.MPV_ERROR_INVALID_PARAMETER);

            // Check to see if we're playing
            if (PlaybackTools.IsPlaying(basolia))
                throw new BasoliaException(LanguageTools.GetLocalized("MEDIABOOM_BASOLIA_FORMAT_EXCEPTION_DURATIONONPLAYBACK"), MpvError.MPV_ERROR_INVALID_PARAMETER);

            // Always zero for radio stations
            if (FileTools.IsRadioStation(basolia))
                return 0;

            // We're now entering the dangerous zone
            long length;
            unsafe
            {
                // Get the actual length
                length = MpvPropertyHandler.GetIntegerProperty(basolia, "duration/full");
            }

            // We're now entering the safe zone
            return length;
        }

        /// <summary>
        /// Gets the duration of the file in the time span
        /// </summary>
        /// <param name="basolia">Basolia instance that contains a valid handle</param>
        /// <returns>A <see cref="TimeSpan"/> instance containing the duration in human-readable format</returns>
        public static TimeSpan GetDurationSpan(BasoliaMedia? basolia)
        {
            if (basolia is null)
                throw new BasoliaException(LanguageTools.GetLocalized("MEDIABOOM_BASOLIA_EXCEPTION_BASOLIAMEDIA"), MpvError.MPV_ERROR_INVALID_PARAMETER);

            // Get the duration and return the time span
            long durationSeconds = GetDuration(basolia);
            return TimeSpan.FromSeconds(durationSeconds);
        }

        /// <summary>
        /// Gets the frame size from the currently open music file
        /// </summary>
        /// <param name="basolia">Basolia instance that contains a valid handle</param>
        /// <returns>The MPEG frame size</returns>
        /// <exception cref="BasoliaException"></exception>
        /// <exception cref="BasoliaException"></exception>
        public static int GetFrameSize(BasoliaMedia? basolia)
        {
            InitBasolia.CheckInited();
            if (basolia is null)
                throw new BasoliaException(LanguageTools.GetLocalized("MEDIABOOM_BASOLIA_EXCEPTION_BASOLIAMEDIA"), MpvError.MPV_ERROR_INVALID_PARAMETER);

            // Check to see if the file is open
            if (!FileTools.IsOpened(basolia))
                throw new BasoliaException(LanguageTools.GetLocalized("MEDIABOOM_BASOLIA_FORMAT_EXCEPTION_FILENOTOPEN_QUERY"), MpvError.MPV_ERROR_INVALID_PARAMETER);

            // TODO: Unstub this function
            return 0;
        }

        /// <summary>
        /// Gets the frame length
        /// </summary>
        /// <param name="basolia">Basolia instance that contains a valid handle</param>
        /// <returns>Frame length in samples</returns>
        /// <exception cref="BasoliaException"></exception>
        public static int GetFrameLength(BasoliaMedia? basolia)
        {
            int getStatus = 0;
            InitBasolia.CheckInited();
            if (basolia is null)
                throw new BasoliaException(LanguageTools.GetLocalized("MEDIABOOM_BASOLIA_EXCEPTION_BASOLIAMEDIA"), MpvError.MPV_ERROR_INVALID_PARAMETER);

            // Check to see if the file is open
            if (!FileTools.IsOpened(basolia))
                throw new BasoliaException(LanguageTools.GetLocalized("MEDIABOOM_BASOLIA_FORMAT_EXCEPTION_FILENOTOPEN_QUERY"), MpvError.MPV_ERROR_INVALID_PARAMETER);

            unsafe
            {
                var handle = basolia._libmpvHandle;

                // Get the frame length

                // TODO: Unstub this function
                Debug.WriteLine($"Got frame length {getStatus}");
            }
            return getStatus;
        }

        /// <summary>
        /// Gets the number of samples per frame
        /// </summary>
        /// <param name="basolia">Basolia instance that contains a valid handle</param>
        /// <returns>Number of samples per frame</returns>
        /// <exception cref="BasoliaException"></exception>
        public static int GetSamplesPerFrame(BasoliaMedia? basolia)
        {
            int getStatus = 0;
            InitBasolia.CheckInited();
            if (basolia is null)
                throw new BasoliaException(LanguageTools.GetLocalized("MEDIABOOM_BASOLIA_EXCEPTION_BASOLIAMEDIA"), MpvError.MPV_ERROR_INVALID_PARAMETER);

            // Check to see if the file is open
            if (!FileTools.IsOpened(basolia))
                throw new BasoliaException(LanguageTools.GetLocalized("MEDIABOOM_BASOLIA_FORMAT_EXCEPTION_FILENOTOPEN_QUERY"), MpvError.MPV_ERROR_INVALID_PARAMETER);

            unsafe
            {
                var handle = basolia._libmpvHandle;

                // Get the samples per frame

                // TODO: Unstub this function
                Debug.WriteLine($"Got frame spf {getStatus}");
            }
            return getStatus;
        }

        /// <summary>
        /// Gets the number of seconds per frame
        /// </summary>
        /// <param name="basolia">Basolia instance that contains a valid handle</param>
        /// <returns>Number of seconds per frame</returns>
        /// <exception cref="BasoliaException"></exception>
        public static double GetSecondsPerFrame(BasoliaMedia? basolia)
        {
            double getStatus = 0;
            InitBasolia.CheckInited();
            if (basolia is null)
                throw new BasoliaException(LanguageTools.GetLocalized("MEDIABOOM_BASOLIA_EXCEPTION_BASOLIAMEDIA"), MpvError.MPV_ERROR_INVALID_PARAMETER);

            // Check to see if the file is open
            if (!FileTools.IsOpened(basolia))
                throw new BasoliaException(LanguageTools.GetLocalized("MEDIABOOM_BASOLIA_FORMAT_EXCEPTION_FILENOTOPEN_QUERY"), MpvError.MPV_ERROR_INVALID_PARAMETER);

            unsafe
            {
                var handle = basolia._libmpvHandle;

                // Get the seconds per frame

                // TODO: Unstub this function
                Debug.WriteLine($"Got frame tpf {getStatus}");
            }
            return getStatus;
        }

        /// <summary>
        /// Gets the buffer size from the currently open music file.
        /// </summary>
        /// <param name="basolia">Basolia instance that contains a valid handle</param>
        /// <returns>Buffer size</returns>
        /// <exception cref="BasoliaException"></exception>
        public static int GetBufferSize(BasoliaMedia? basolia)
        {
            int bufferSize = 0;
            InitBasolia.CheckInited();
            if (basolia is null)
                throw new BasoliaException(LanguageTools.GetLocalized("MEDIABOOM_BASOLIA_EXCEPTION_BASOLIAMEDIA"), MpvError.MPV_ERROR_INVALID_PARAMETER);

            // Check to see if the file is open
            if (!FileTools.IsOpened(basolia))
                throw new BasoliaException(LanguageTools.GetLocalized("MEDIABOOM_BASOLIA_FORMAT_EXCEPTION_FILENOTOPEN_QUERY"), MpvError.MPV_ERROR_INVALID_PARAMETER);

            unsafe
            {
                var handle = basolia._libmpvHandle;

                // Now, buffer the entire music file and create an empty array based on its size

                // TODO: Unstub this function
                Debug.WriteLine($"Buffer size is {bufferSize}");
            }
            return bufferSize;
        }

        /// <summary>
        /// Gets the generic buffer size that is suitable in most cases
        /// </summary>
        /// <returns>Buffer size</returns>
        /// <exception cref="BasoliaException"></exception>
        public static int GetGenericBufferSize()
        {
            InitBasolia.CheckInited();
            int bufferSize = 0;

            unsafe
            {
                // Get the generic buffer size

                // TODO: Unstub this function
                Debug.WriteLine($"Got buffsize {bufferSize}");
            }
            return bufferSize;
        }
    }
}
