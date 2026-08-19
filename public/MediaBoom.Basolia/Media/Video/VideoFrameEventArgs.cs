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
using System.Text;

namespace MediaBoom.Basolia.Media.Video
{
    /// <summary>
    /// A new video frame
    /// </summary>
    public class VideoFrameEventArgs
    {
        /// <summary>
        /// Frame pointer (software renderer)
        /// </summary>
        public IntPtr SWFramePointer { get; internal set; }

        /// <summary>
        /// Stride (software renderer)
        /// </summary>
        public long Stride { get; internal set; }
        
        /// <summary>
        /// OpenGL texture pointer
        /// </summary>
        public uint GLTexturePointer { get; internal set; }

        /// <summary>
        /// Buffer width
        /// </summary>
        public int Width { get; internal set; }

        /// <summary>
        /// Buffer height
        /// </summary>
        public int Height { get; internal set; }

        /// <summary>
        /// Video pixel format
        /// </summary>
        public string Format { get; internal set; } = "";
    }
}
