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
using MediaBoom.Basolia.Media.Lyrics;
using MediaBoom.Basolia.Media.Playback;
using MediaBoom.Native.Interop.Enumerations;

namespace MediaBoom.Basolia.Media
{
    /// <summary>
    /// Basolia instance for media manipulation
    /// </summary>
    public partial class BasoliaMedia
    {
        #region Positioning tools
        internal object PositionLock = new();

        /// <summary>
        /// Gets the current duration of the file (samples)
        /// </summary>
        /// <returns>Current duration in samples</returns>
        public long GetCurrentDuration()
        {
            InitBasolia.CheckInited();

            // Check to see if the file is open
            if (!IsOpened())
                throw new BasoliaException(LanguageTools.GetLocalized("MEDIABOOM_BASOLIA_PLAYBACK_EXCEPTION_FILENOTOPEN_PLAY"), MpvError.MPV_ERROR_INVALID_PARAMETER);

            // We're now entering the dangerous zone
            long length;
            unsafe
            {
                length = MpvPropertyHandler.GetIntegerProperty(this, "time-pos/full");
            }

            // We're now entering the safe zone
            return length;
        }

        /// <summary>
        /// Gets the current duration of the file (time span)
        /// </summary>
        /// <returns>A time span instance that describes the current duration of the file</returns>
        public TimeSpan GetCurrentDurationSpan()
        {
            InitBasolia.CheckInited();

            // Get the duration
            long duration = GetCurrentDuration();
            return TimeSpan.FromSeconds(duration);
        }

        /// <summary>
        /// Seeks to the beginning of the music
        /// </summary>
        public void SeekToTheBeginning()
        {
            lock (PositionLock)
            {
                InitBasolia.CheckInited();

                // Check to see if the file is open
                if (!IsOpened())
                    throw new BasoliaException(LanguageTools.GetLocalized("MEDIABOOM_BASOLIA_PLAYBACK_EXCEPTION_FILENOTOPEN_SEEK"), MpvError.MPV_ERROR_INVALID_PARAMETER);

                // Seek to 0 sec
                SeekTo(0);
            }
        }

        /// <summary>
        /// Seeks to a specific frame
        /// </summary>
        /// <param name="seconds">Duration in seconds in absolute form</param>
        public void SeekTo(long seconds)
        {
            lock (PositionLock)
            {
                InitBasolia.CheckInited();

                // Check to see if the file is open
                if (!IsOpened())
                    throw new BasoliaException(LanguageTools.GetLocalized("MEDIABOOM_BASOLIA_PLAYBACK_EXCEPTION_FILENOTOPEN_SEEK"), MpvError.MPV_ERROR_INVALID_PARAMETER);

                // Seek the file
                MpvCommandHandler.RunCommand(this, "seek", seconds.ToString(), "absolute");
            }
        }
        #endregion

        #region Playback tools
        /// <summary>
        /// Checks to see whether the music is playing
        /// </summary>
        public bool IsPlaying() =>
            state == PlaybackState.Playing;

        /// <summary>
        /// The current state of the playback
        /// </summary>
        public PlaybackState GetState() =>
            state;

        /// <summary>
        /// Current radio ICY metadata
        /// </summary>
        public string GetRadioIcy() =>
            radioIcy;

        /// <summary>
        /// Current radio ICY metadata
        /// </summary>
        public string GetRadioNowPlaying()
        {
            string icy = GetRadioIcy();
            if (icy.Length == 0 || !IsRadioStation())
                return "";
            icy = Regex.Match(icy, @"StreamTitle='(.+?(?=\';))'").Groups[1].Value.Trim().Replace("\\'", "'");
            return icy;
        }

        /// <summary>
        /// Plays the currently open file (synchronous)
        /// </summary>
        /// <exception cref="BasoliaException"></exception>
        public void Play()
        {
            InitBasolia.CheckInited();

            // Check to see if the file is open
            if (!IsOpened())
                throw new BasoliaException(LanguageTools.GetLocalized("MEDIABOOM_BASOLIA_PLAYBACK_EXCEPTION_FILENOTOPEN_PLAY"), MpvError.MPV_ERROR_INVALID_PARAMETER);

            // Helper function to observe pause
            string pausing = "";
            void ObservePause((string name, string value) property)
            {
                if (property.name == "pause")
                    pausing = property.value;
            }

            // We're now entering the dangerous zone
            unsafe
            {
                // Now, buffer the entire music file and create an empty array based on its size
                var bufferSize = GetBufferSize();
                Debug.WriteLine($"Buffer size is {bufferSize}");
                MpvPropertyHandler.SetStringProperty(this, "pause", "no");
                MpvPropertyHandler.ObserveStringProperty(this, "pause");
                StringEventPropertyChanged += ObservePause;
                state = PlaybackState.Playing;

                // First, let Basolia "hold on" until hold is released
                while (holding)
                    Thread.Sleep(1);

                // Wait until pause is requested
                SpinWait.SpinUntil(() => pausing == "yes" || !IsPlaying());
                StringEventPropertyChanged -= ObservePause;
                if (state == PlaybackState.Pausing)
                    state = PlaybackState.Paused;
                if (IsPlaying() || state == PlaybackState.Stopping)
                    state = PlaybackState.Stopped;
            }
        }

        /// <summary>
        /// Plays the currently open file (asynchronous)
        /// </summary>
        public async Task PlayAsync() =>
            await Task.Run(Play);

        /// <summary>
        /// Pauses the currently open file
        /// </summary>
        /// <exception cref="BasoliaException"></exception>
        public void Pause()
        {
            InitBasolia.CheckInited();

            // Check to see if the file is open
            if (!IsOpened())
                throw new BasoliaException(LanguageTools.GetLocalized("MEDIABOOM_BASOLIA_PLAYBACK_EXCEPTION_FILENOTOPEN_PAUSE"), MpvError.MPV_ERROR_INVALID_PARAMETER);
            if (state == PlaybackState.Playing)
            {
                state = PlaybackState.Pausing;
                MpvPropertyHandler.SetStringProperty(this, "pause", "yes");
                SpinWait.SpinUntil(() => state == PlaybackState.Paused);
            }
            else
                state = PlaybackState.Stopped;
        }

        /// <summary>
        /// Stops the playback
        /// </summary>
        /// <exception cref="BasoliaException"></exception>
        public void Stop()
        {
            InitBasolia.CheckInited();

            // Check to see if the file is open
            if (!IsOpened())
                throw new BasoliaException(LanguageTools.GetLocalized("MEDIABOOM_BASOLIA_PLAYBACK_EXCEPTION_FILENOTOPEN_STOP"), MpvError.MPV_ERROR_INVALID_PARAMETER);

            // Stop the music and seek to the beginning
            state = state == PlaybackState.Playing ? PlaybackState.Stopping : PlaybackState.Stopped;
            MpvPropertyHandler.SetStringProperty(this, "pause", "yes");
            SpinWait.SpinUntil(() => state == PlaybackState.Stopped);
            if (!IsRadioStation())
                SeekToTheBeginning();
        }

        /// <summary>
        /// Sets the volume of this application
        /// </summary>
        /// <param name="volume">Volume from 0 to 100, inclusive</param>
        /// <exception cref="BasoliaException"></exception>
        public void SetVolume(double volume)
        {
            InitBasolia.CheckInited();

            // Check the volume
            if (volume < 0)
                volume = 0;
            if (volume > 100)
                volume = 100;

            try
            {
                MpvPropertyHandler.SetDoubleProperty(this, "ao-volume", volume);
            }
            catch
            {
                // TODO: Add debug later
            }
        }

        /// <summary>
        /// Gets the volume information
        /// </summary>
        /// <returns>A base linear volume from 0 to 100</returns>
        /// <exception cref="BasoliaException"></exception>
        public double GetVolume()
        {
            InitBasolia.CheckInited();

            try
            {
                double baseLinearAddr = MpvPropertyHandler.GetDoubleProperty(this, "ao-volume");
                return baseLinearAddr;
            }
            catch
            {
                return 0;
            }
        }
        #endregion
    }
}
