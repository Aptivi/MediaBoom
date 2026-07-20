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
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using MediaBoom.Basolia.Exceptions;
using MediaBoom.Basolia.Languages;
using MediaBoom.Basolia.Media.File;
using MediaBoom.Basolia.Media.Helpers;
using MediaBoom.Basolia.Media.Playback;
using MediaBoom.Basolia.Media.Radio;
using MediaBoom.Native.Interop.Enumerations;
using SpecProbe.Software.Platform;

namespace MediaBoom.Basolia.Media
{
    /// <summary>
    /// Basolia instance for media manipulation
    /// </summary>
    public partial class BasoliaMedia
    {
        /// <summary>
        /// Is the file open?
        /// </summary>
        public bool IsOpened()
        {
            InitBasolia.CheckInited();
            return isOpened;
        }

        /// <summary>
        /// Is the radio station open?
        /// </summary>
        public bool IsRadioStation()
        {
            InitBasolia.CheckInited();
            return isRadioStation;
        }

        /// <summary>
        /// Current file
        /// </summary>
        public FileType? CurrentFile()
        {
            InitBasolia.CheckInited();
            return currentFile;
        }

        /// <summary>
        /// Opens a media file
        /// </summary>
        /// <param name="path">Path to a valid media file</param>
        public void OpenFile(string path)
        {
            InitBasolia.CheckInited();

            // Check to see if the file is open
            if (IsOpened())
                throw new BasoliaException(LanguageTools.GetLocalized("MEDIABOOM_BASOLIA_FILE_EXCEPTION_FILEALREADYOPEN"), MpvError.MPV_ERROR_INVALID_PARAMETER);

            // Check to see if we provided a path
            if (string.IsNullOrEmpty(path))
                throw new BasoliaException(LanguageTools.GetLocalized("MEDIABOOM_BASOLIA_FILE_EXCEPTION_NEEDSMUSICFILEPATH"), MpvError.MPV_ERROR_INVALID_PARAMETER);
            path = Path.GetFullPath(path);

            // Check to see if the file exists
            if (!System.IO.File.Exists(path))
                throw new BasoliaException(LanguageTools.GetLocalized("MEDIABOOM_BASOLIA_FILE_EXCEPTION_MUSICFILENOTFOUND"), MpvError.MPV_ERROR_INVALID_PARAMETER);

            if (isOpened && currentFile?.Path == path)
                return;

            if (isOpened)
                CloseFile();

            // Open the file
            loadEvent.Reset();
            MpvPropertyHandler.SetStringProperty(this, "pause", "yes");
            MpvCommandHandler.RunCommand(this, "loadfile", path);
            if (!loadEvent.Wait(new TimeSpan(0, 0, 10)))
                throw new BasoliaException(LanguageTools.GetLocalized("MEDIABOOM_BASOLIA_EXCEPTION_OPERATIONTIMEOUT"), MpvError.MPV_ERROR_GENERIC);
            currentFile = new(false, path, null, null, "");
        }

        /// <summary>
        /// Opens a remote radio station
        /// </summary>
        /// <param name="path">URL Path to a valid media file</param>
        public void OpenUrl(string path) =>
            Task.Run(() => OpenUrlAsync(path)).Wait();

        /// <summary>
        /// Opens a remote radio station
        /// </summary>
        /// <param name="path">URL Path to a valid media file</param>
        public async Task OpenUrlAsync(string path)
        {
            InitBasolia.CheckInited();

            // Check to see if the file is open
            if (IsOpened())
                throw new BasoliaException(LanguageTools.GetLocalized("MEDIABOOM_BASOLIA_FILE_EXCEPTION_URLALREADYOPEN"), MpvError.MPV_ERROR_INVALID_PARAMETER);

            // Check to see if we provided a path
            if (string.IsNullOrEmpty(path))
                throw new BasoliaException(LanguageTools.GetLocalized("MEDIABOOM_BASOLIA_FILE_EXCEPTION_NEEDSMUSICURL"), MpvError.MPV_ERROR_INVALID_PARAMETER);

            // Check to see if the radio station exists
            if (PlatformHelper.IsDotNetFx())
                RadioTools.client = new();
            RadioTools.client.DefaultRequestHeaders.Add("Icy-MetaData", "1");
            var reply = await RadioTools.client.GetAsync(path, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false);
            RadioTools.client.DefaultRequestHeaders.Remove("Icy-MetaData");
            if (!reply.IsSuccessStatusCode)
                throw new BasoliaException(LanguageTools.GetLocalized("MEDIABOOM_BASOLIA_FILE_EXCEPTION_NORADIOSTATION") + $" {(int)reply.StatusCode} ({reply.StatusCode}).", MpvError.MPV_ERROR_INVALID_PARAMETER);

            // Check to see if there are any ICY headers
            if (!reply.Headers.Any((kvp) => kvp.Key.StartsWith("icy-")))
                throw new BasoliaException(LanguageTools.GetLocalized("MEDIABOOM_BASOLIA_FILE_EXCEPTION_NOTARADIOSTATION"), MpvError.MPV_ERROR_INVALID_PARAMETER);

            if (isOpened && currentFile?.Path == path)
                return;

            if (isOpened)
                CloseFile();

            // Open the radio station
            loadEvent.Reset();
            MpvPropertyHandler.SetStringProperty(this, "pause", "yes");
            MpvCommandHandler.RunCommand(this, "loadfile", path);
            if (!loadEvent.Wait(new TimeSpan(0, 0, 10)))
                throw new BasoliaException(LanguageTools.GetLocalized("MEDIABOOM_BASOLIA_EXCEPTION_OPERATIONTIMEOUT"), MpvError.MPV_ERROR_GENERIC);
            isRadioStation = true;
            currentFile = new(true, path, await reply.Content.ReadAsStreamAsync().ConfigureAwait(false), reply.Headers, reply.Headers.GetValues("icy-name").First());

            // If necessary, feed.
            FeedRadio();
        }

        /// <summary>
        /// Closes a currently opened media file
        /// </summary>
        public void CloseFile()
        {
            InitBasolia.CheckInited();

            // Check to see if the file is open
            if (!IsOpened())
                throw new BasoliaException(LanguageTools.GetLocalized("MEDIABOOM_BASOLIA_FILE_EXCEPTION_ALREADYCLOSED"), MpvError.MPV_ERROR_INVALID_PARAMETER);

            // First, stop the playing song
            var state = GetState();
            if (state == PlaybackState.Playing || state == PlaybackState.Paused)
                Stop();

            // We're now entering the dangerous zone
            unsafe
            {
                // Close the file
                MpvCommandHandler.RunCommand(this, "playlist-remove", "current");
                isOpened = false;
                isRadioStation = false;
                currentFile?.Stream?.Close();
                currentFile = null;
            }
        }
    }
}
