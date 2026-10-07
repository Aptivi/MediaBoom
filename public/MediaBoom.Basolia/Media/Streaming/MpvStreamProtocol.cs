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
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using MediaBoom.Basolia.Exceptions;
using MediaBoom.Native;
using MediaBoom.Native.Interop.Enumerations;
using MediaBoom.Native.Interop.Init;

namespace MediaBoom.Basolia.Media.Streaming
{
    internal static class MpvStreamProtocol
    {
        private const string Protocol = "basolia";
        private static readonly ConcurrentDictionary<string, MpvStreamSource> sources = new();
        private static readonly HashSet<nint> registered = [];
        private static readonly mpv_stream_cb_open_ro_fn openFn = Open;
        private static readonly mpv_stream_cb_read_fn readFn = Read;
        private static readonly mpv_stream_cb_seek_fn seekFn = Seek;
        private static readonly mpv_stream_cb_size_fn sizeFn = Size;
        private static readonly mpv_stream_cb_close_fn closeFn = Close;
        private static readonly mpv_stream_cb_cancel_fn cancelFn = Cancel;

        internal static unsafe void EnsureRegistered(BasoliaMedia media)
        {
            lock (registered)
            {
                if (!registered.Add((nint)media._libmpvHandle))
                    return;
                var add = NativeInitializer.GetDelegate<NativeStreamProtocol.mpv_stream_cb_add_ro>(NativeInitializer.libManagerMpv, nameof(NativeStreamProtocol.mpv_stream_cb_add_ro));
                var result = (MpvError)add.Invoke(media._libmpvHandle, Protocol, IntPtr.Zero, openFn);
                // TODO: MEDIABOOM_BASOLIA_MEDIA_EXCEPTION_STREAMPROTOCOLUNREGISTERABLE -> Can't register the stream protocol
                if (result < MpvError.MPV_ERROR_SUCCESS)
                    throw new BasoliaException("Can't register the stream protocol", result);
            }
        }

        internal static string Add(Stream stream, bool leaveOpen)
        {
            string id = Guid.NewGuid().ToString("N");
            sources[id] = new MpvStreamSource(id, stream, leaveOpen);
            return $"{Protocol}://{id}";
        }

        private static MpvStreamSource Get(IntPtr cookie) =>
            (MpvStreamSource)GCHandle.FromIntPtr(cookie).Target;

        private static int Open(IntPtr userData, IntPtr uriPtr, IntPtr infoPtr)
        {
            string uri = Marshal.PtrToStringAnsi(uriPtr) ?? "";
            string prefix = Protocol + "://";
            if (!uri.StartsWith(prefix, StringComparison.Ordinal) || !sources.TryGetValue(uri.Substring(prefix.Length), out var source))
                return (int)MpvError.MPV_ERROR_LOADING_FAILED;

            var info = new MpvStreamCallbackInfo
            {
                cookie = GCHandle.ToIntPtr(GCHandle.Alloc(source)),
                read_fn = Marshal.GetFunctionPointerForDelegate(readFn),
                seek_fn = source.stream.CanSeek ? Marshal.GetFunctionPointerForDelegate(seekFn) : IntPtr.Zero,
                size_fn = Marshal.GetFunctionPointerForDelegate(sizeFn),
                close_fn = Marshal.GetFunctionPointerForDelegate(closeFn),
                cancel_fn = Marshal.GetFunctionPointerForDelegate(cancelFn),
            };
            Marshal.StructureToPtr(info, infoPtr, false);
            return 0;
        }

        private static long Read(IntPtr cookie, IntPtr buf, ulong nbytes)
        {
            try
            {
                var source = Get(cookie);
                int count = (int)Math.Min(nbytes, (ulong)source.scratch.Length);
                int read = source.stream.Read(source.scratch, 0, count);
                if (read > 0)
                    Marshal.Copy(source.scratch, 0, buf, read);
                return read;
            }
            catch
            {
                return -1;
            }
        }

        private static long Seek(IntPtr cookie, long offset)
        {
            try
            {
                var stream = Get(cookie).stream;
                return stream.Seek(offset, SeekOrigin.Begin);
            }
            catch
            {
                return (long)MpvError.MPV_ERROR_GENERIC;
            }
        }

        private static long Size(IntPtr cookie)
        {
            try
            {
                var stream = Get(cookie).stream;
                return stream.CanSeek ? stream.Length : (long)MpvError.MPV_ERROR_UNSUPPORTED;
            }
            catch
            {
                return (long)MpvError.MPV_ERROR_UNSUPPORTED;
            }
        }

        private static void Cancel(IntPtr cookie)
        {
            try
            {
                var stream = Get(cookie).stream;
                if (stream is FeedStream feedStream)
                    feedStream.Cancel();
            }
            catch
            { }
        }

        private static void Close(IntPtr cookie)
        {
            try
            {
                var handle = GCHandle.FromIntPtr(cookie);
                var source = (MpvStreamSource)handle.Target;
                handle.Free();
                sources.TryRemove(source.id, out _);
                if (!source.leaveOpen)
                    source.stream.Dispose();
            }
            catch
            { }
        }
    }
}
