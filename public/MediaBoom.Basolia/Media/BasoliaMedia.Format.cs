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
    }
}
