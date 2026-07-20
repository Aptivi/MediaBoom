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
using MediaBoom.Basolia.Exceptions;
using MediaBoom.Basolia.Languages;
using MediaBoom.Basolia.Media.Helpers;
using MediaBoom.Native.Interop.Enumerations;

namespace MediaBoom.Basolia.Media
{
    /// <summary>
    /// Basolia instance for media manipulation
    /// </summary>
    public partial class BasoliaMedia
    {
        /// <summary>
        /// Gets the duration of the file in samples
        /// </summary>
        /// <returns>Number of seconds.</returns>
        public long GetDuration()
        {
            InitBasolia.CheckInited();

            // Check to see if the file is open
            if (!IsOpened())
                throw new BasoliaException(LanguageTools.GetLocalized("MEDIABOOM_BASOLIA_FORMAT_EXCEPTION_FILENOTOPEN_QUERY"), MpvError.MPV_ERROR_INVALID_PARAMETER);

            // Check to see if we're playing
            if (IsPlaying())
                throw new BasoliaException(LanguageTools.GetLocalized("MEDIABOOM_BASOLIA_FORMAT_EXCEPTION_DURATIONONPLAYBACK"), MpvError.MPV_ERROR_INVALID_PARAMETER);

            // Always zero for radio stations
            if (IsRadioStation())
                return 0;

            // We're now entering the dangerous zone
            long length;
            unsafe
            {
                // Get the actual length
                length = MpvPropertyHandler.GetIntegerProperty(this, "duration/full");
            }

            // We're now entering the safe zone
            return length;
        }

        /// <summary>
        /// Gets the duration of the file in the time span
        /// </summary>
        /// <returns>A <see cref="TimeSpan"/> instance containing the duration in human-readable format</returns>
        public TimeSpan GetDurationSpan()
        {
            // Get the duration and return the time span
            long durationSeconds = GetDuration();
            return TimeSpan.FromSeconds(durationSeconds);
        }

        /// <summary>
        /// Gets the frame size from the currently open music file
        /// </summary>
        /// <returns>The MPEG frame size</returns>
        /// <exception cref="BasoliaException"></exception>
        public int GetFrameSize()
        {
            InitBasolia.CheckInited();

            // TODO: Unstub this function
            return 0;
        }

        /// <summary>
        /// Gets the frame length
        /// </summary>
        /// <returns>Frame length in samples</returns>
        /// <exception cref="BasoliaException"></exception>
        public int GetFrameLength()
        {
            int getStatus = 0;
            InitBasolia.CheckInited();

            // Check to see if the file is open
            if (!IsOpened())
                throw new BasoliaException(LanguageTools.GetLocalized("MEDIABOOM_BASOLIA_FORMAT_EXCEPTION_FILENOTOPEN_QUERY"), MpvError.MPV_ERROR_INVALID_PARAMETER);

            unsafe
            {
                var handle = _libmpvHandle;

                // Get the frame length

                // TODO: Unstub this function
                Debug.WriteLine($"Got frame length {getStatus}");
            }
            return getStatus;
        }

        /// <summary>
        /// Gets the number of samples per frame
        /// </summary>
        /// <returns>Number of samples per frame</returns>
        /// <exception cref="BasoliaException"></exception>
        public int GetSamplesPerFrame()
        {
            int getStatus = 0;
            InitBasolia.CheckInited();

            // Check to see if the file is open
            if (!IsOpened())
                throw new BasoliaException(LanguageTools.GetLocalized("MEDIABOOM_BASOLIA_FORMAT_EXCEPTION_FILENOTOPEN_QUERY"), MpvError.MPV_ERROR_INVALID_PARAMETER);

            unsafe
            {
                var handle = _libmpvHandle;

                // Get the frame length

                // TODO: Unstub this function
                Debug.WriteLine($"Got frame spf {getStatus}");
            }
            return getStatus;
        }

        /// <summary>
        /// Gets the number of seconds per frame
        /// </summary>
        /// <returns>Number of seconds per frame</returns>
        /// <exception cref="BasoliaException"></exception>
        public double GetSecondsPerFrame()
        {
            double getStatus = 0;
            InitBasolia.CheckInited();

            // Check to see if the file is open
            if (!IsOpened())
                throw new BasoliaException(LanguageTools.GetLocalized("MEDIABOOM_BASOLIA_FORMAT_EXCEPTION_FILENOTOPEN_QUERY"), MpvError.MPV_ERROR_INVALID_PARAMETER);

            unsafe
            {
                var handle = _libmpvHandle;

                // Get the seconds per frame

                // TODO: Unstub this function
                Debug.WriteLine($"Got frame tpf {getStatus}");
            }
            return getStatus;
        }

        /// <summary>
        /// Gets the buffer size from the currently open music file.
        /// </summary>
        /// <returns>Buffer size</returns>
        /// <exception cref="BasoliaException"></exception>
        public int GetBufferSize()
        {
            int bufferSize = 0;
            InitBasolia.CheckInited();

            // Check to see if the file is open
            if (!IsOpened())
                throw new BasoliaException(LanguageTools.GetLocalized("MEDIABOOM_BASOLIA_FORMAT_EXCEPTION_FILENOTOPEN_QUERY"), MpvError.MPV_ERROR_INVALID_PARAMETER);

            unsafe
            {
                var handle = _libmpvHandle;

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
        public int GetGenericBufferSize()
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
