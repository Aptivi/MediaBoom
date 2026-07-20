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
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace MediaBoom.Basolia.Media
{
    /// <summary>
    /// Basolia instance for media manipulation
    /// </summary>
    public partial class BasoliaMedia
    {
        internal void FeedRadio()
        {
            if (!IsOpened() || !IsRadioStation())
                return;
            var currentRadio = CurrentFile();
            if (currentRadio is null)
                return;
            if (currentRadio.Headers is null)
                return;
            if (currentRadio.Stream is null)
                return;

            unsafe
            {
                // Get the MP3 frame length first
                string metaIntStr = currentRadio.Headers.GetValues("icy-metaint").First();
                int metaInt = int.Parse(metaIntStr);

                // Now, get the MP3 frame
                byte[] buffer = new byte[metaInt];
                int numBytesRead = 0;
                int numBytesToRead = metaInt;
                do
                {
                    int n = currentRadio.Stream.Read(buffer, numBytesRead, 1);
                    numBytesRead += n;
                    numBytesToRead -= n;
                } while (numBytesToRead > 0);

                // Fetch the metadata.
                int lengthOfMetaData = currentRadio.Stream.ReadByte();
                int metaBytesToRead = lengthOfMetaData * 16;
                Debug.WriteLine($"Buffer: {lengthOfMetaData} [{metaBytesToRead}]");
                byte[] metadataBytes = new byte[metaBytesToRead];
                currentRadio.Stream.Read(metadataBytes, 0, metaBytesToRead);
                string icy = Encoding.UTF8.GetString(metadataBytes).Replace("\0", "").Trim();
                if (!string.IsNullOrEmpty(icy))
                    radioIcy = icy;
                Debug.WriteLine($"{radioIcy}");

                // Copy the data to libmpv
                CopyBuffer(buffer);
            }
        }

        internal void CopyBuffer(byte[]? buffer)
        {
            if (buffer is null)
                return;
            unsafe
            {
                var handle = _libmpvHandle;

                // Copy the data to libmpv
                IntPtr data = Marshal.AllocHGlobal(buffer.Length);
                Marshal.Copy(buffer, 0, data, buffer.Length);

                // TODO: Unstub this function
            }
        }

        internal int PlayBuffer(byte[]? buffer)
        {
            if (buffer is null)
                return 0;

            // TODO: Unstub this function
            return 0;
        }
    }
}
