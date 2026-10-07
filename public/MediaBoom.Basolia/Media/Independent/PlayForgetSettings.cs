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

using MediaBoom.Basolia.Exceptions;
using MediaBoom.Basolia.Languages;

namespace MediaBoom.Basolia.Media.Independent
{
    /// <summary>
    /// Settings for the Play and Forget technique
    /// </summary>
    public class PlayForgetSettings
    {
        private double volume = 100;
        private readonly string rootLibPath = "";

        /// <summary>
        /// Volume in integer (50 resembles 50%)
        /// </summary>
        public double Volume
        {
            get => volume;
            set => volume = value < 0 ? 0 : value > 100 ? 100 : value;
        }

        /// <summary>
        /// Root path to the library
        /// </summary>
        public string RootLibPath =>
            rootLibPath;

        /// <summary>
        /// Makes a new instance of the Play/Forget technique settings
        /// </summary>
        /// <exception cref="BasoliaMiscException"></exception>
        public PlayForgetSettings()
        { }

        /// <summary>
        /// Makes a new instance of the Play/Forget technique settings
        /// </summary>
        /// <param name="volume">Volume boost</param>
        /// <param name="rootLibPath">Root path to the library</param>
        /// <exception cref="BasoliaMiscException"></exception>
        public PlayForgetSettings(double volume, string rootLibPath)
        {
            this.volume = volume;
            this.rootLibPath = rootLibPath;
        }
    }
}
