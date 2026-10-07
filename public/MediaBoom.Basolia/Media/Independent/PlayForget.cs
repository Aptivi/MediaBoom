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

using System.IO;
using System.Threading.Tasks;

namespace MediaBoom.Basolia.Media.Independent
{
    /// <summary>
    /// Play and Forget class (to initialize playback and to forget)
    /// </summary>
    public static class PlayForget
    {
        /// <summary>
        /// Plays the file (synchronous)
        /// </summary>
        /// <param name="path">Path to a music file</param>
        /// <param name="settings">Settings of the play/forget technique</param>
        public static void PlayFile(string path, PlayForgetSettings? settings = null)
        {
            settings ??= new();

            // Make a Basolia media instance and open it with a file
            var media = new BasoliaMedia(settings.RootLibPath);
            media.OpenFile(path);

            // Set the volume
            media.SetVolume(settings.Volume);

            // Play the file
            media.Play();
        }

        /// <summary>
        /// Plays the file (asynchronous)
        /// </summary>
        /// <param name="path">Path to a music file</param>
        /// <param name="settings">Settings of the play/forget technique</param>
        public static async Task PlayFileAsync(string path, PlayForgetSettings? settings = null) =>
            await Task.Run(() => PlayFile(path, settings));

        /// <summary>
        /// Plays the stream (synchronous)
        /// </summary>
        /// <param name="stream">Stream that contains valid MPEG audio stream</param>
        /// <param name="settings">Settings of the play/forget technique</param>
        public static void PlayStream(Stream stream, PlayForgetSettings? settings = null)
        {
            settings ??= new();

            // Make a Basolia media instance and open it with a file
            var media = new BasoliaMedia(settings.RootLibPath);
            media.OpenStream(stream);

            // Set the volume
            media.SetVolume(settings.Volume);

            // Play the file
            media.Play();
        }

        /// <summary>
        /// Plays the stream (asynchronous)
        /// </summary>
        /// <param name="stream">Stream that contains valid MPEG audio stream</param>
        /// <param name="settings">Settings of the play/forget technique</param>
        public static async Task PlayStreamAsync(Stream stream, PlayForgetSettings? settings = null) =>
            await Task.Run(() => PlayStream(stream, settings));
    }
}
