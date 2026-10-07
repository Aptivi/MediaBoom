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
using System.Collections.Concurrent;
using System.IO;
using System.Threading;

namespace MediaBoom.Basolia.Media.Streaming
{
    /// <summary>
    /// Feed stream
    /// </summary>
    public sealed class FeedStream : Stream
    {
        private readonly BlockingCollection<byte[]> chunks = [];
        private readonly CancellationTokenSource cts = new();
        private byte[] current = [];
        private int offset;

        /// <summary>
        /// Feeds new data and places it to the collection of chunks
        /// </summary>
        /// <param name="data">Data to add to the buffer</param>
        /// <param name="offset">Offset from the source byte array</param>
        /// <param name="count">How many bytes to copy</param>
        public void Feed(byte[] data, int offset, int count)
        {
            var copy = new byte[count];
            Buffer.BlockCopy(data, offset, copy, 0, count);
            chunks.Add(copy);
        }

        /// <inheritdoc/>
        public void Complete() =>
            chunks.CompleteAdding();

        /// <inheritdoc/>
        public override int Read(byte[] buffer, int off, int count)
        {
            try
            {
                while (offset >= current.Length)
                {
                    if (!chunks.TryTake(out current!, Timeout.Infinite, cts.Token))
                        return 0;
                    offset = 0;
                }
            }
            catch (OperationCanceledException)
            {
                return 0;
            }

            int n = Math.Min(count, current.Length - offset);
            Buffer.BlockCopy(current, offset, buffer, off, n);
            offset += n;
            return n;
        }

        /// <inheritdoc/>
        public override bool CanRead =>
            true;

        /// <inheritdoc/>
        public override bool CanSeek =>
            false;

        /// <inheritdoc/>
        public override bool CanWrite =>
            false;

        /// <inheritdoc/>
        public override long Length =>
            throw new NotSupportedException();

        /// <inheritdoc/>
        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        /// <inheritdoc/>
        public override void Flush()
        { }

        /// <inheritdoc/>
        public override long Seek(long o, SeekOrigin so) =>
            throw new NotSupportedException();

        /// <inheritdoc/>
        public override void SetLength(long v) =>
            throw new NotSupportedException();

        /// <inheritdoc/>
        public override void Write(byte[] b, int o, int c) =>
            throw new NotSupportedException();

        internal void Cancel() =>
            cts.Cancel();
    }
}
