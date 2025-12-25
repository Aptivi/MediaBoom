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
using MediaBoom.Native;
using MediaBoom.Native.Interop.Analysis;
using MediaBoom.Native.Interop.Enumerations;
using System;
using System.Runtime.InteropServices;
using Textify.General;

namespace MediaBoom.Basolia.Helpers
{
    /// <summary>
    /// MPV property handler
    /// </summary>
    public static class MpvPropertyHandler
    {
        /// <summary>
        /// Sets an MPV string property
        /// </summary>
        /// <param name="basolia">Basolia instance that contains a valid handle</param>
        /// <param name="propertyName">Property name to set</param>
        /// <param name="propertyValue">Property value to set</param>
        /// <exception cref="BasoliaException"></exception>
        public static void SetStringProperty(BasoliaMedia? basolia, string propertyName, string propertyValue)
        {
            InitBasolia.CheckInited();
            if (basolia is null)
                throw new BasoliaException(LanguageTools.GetLocalized("MEDIABOOM_BASOLIA_EXCEPTION_BASOLIAMEDIA"), MpvError.MPV_ERROR_INVALID_PARAMETER);

            // We're now entering the dangerous zone
            unsafe
            {
                // Set the string property
                var handle = basolia._libmpvHandle;
                var propertyValuePointer = NativeArrayBuilder.GetUtf8BytesPointer(propertyValue);
                MpvError propertyResult = (MpvError)NativeInitializer.GetDelegate<NativeParameters.mpv_set_property>(NativeInitializer.libManagerMpv, nameof(NativeParameters.mpv_set_property)).Invoke(handle, propertyName, MpvValueFormat.MPV_FORMAT_STRING, ref propertyValue);

                // TODO: MEDIABOOM_BASOLIA_EXCEPTION_SETSTRINGPROPERTYFAILED -> Failed to set string property {0} to {1}
                if (propertyResult < MpvError.MPV_ERROR_SUCCESS)
                    throw new BasoliaException(LanguageTools.GetLocalized("MEDIABOOM_BASOLIA_EXCEPTION_SETSTRINGPROPERTYFAILED").FormatString(propertyName, propertyValue), propertyResult);
            }
        }
        
        /// <summary>
        /// Sets an MPV integer property
        /// </summary>
        /// <param name="basolia">Basolia instance that contains a valid handle</param>
        /// <param name="propertyName">Property name to set</param>
        /// <param name="propertyValue">Property value to set</param>
        /// <exception cref="BasoliaException"></exception>
        public static void SetIntegerProperty(BasoliaMedia? basolia, string propertyName, long propertyValue)
        {
            InitBasolia.CheckInited();
            if (basolia is null)
                throw new BasoliaException(LanguageTools.GetLocalized("MEDIABOOM_BASOLIA_EXCEPTION_BASOLIAMEDIA"), MpvError.MPV_ERROR_INVALID_PARAMETER);

            // We're now entering the dangerous zone
            unsafe
            {
                // Set the string property
                var handle = basolia._libmpvHandle;
                MpvError propertyResult = (MpvError)NativeInitializer.GetDelegate<NativeParameters.mpv_set_property_int>(NativeInitializer.libManagerMpv, nameof(NativeParameters.mpv_set_property)).Invoke(handle, propertyName, MpvValueFormat.MPV_FORMAT_INT64, ref propertyValue);

                // TODO: MEDIABOOM_BASOLIA_EXCEPTION_SETINTEGERPROPERTYFAILED -> Failed to set integer property {0} to {1}
                if (propertyResult < MpvError.MPV_ERROR_SUCCESS)
                    throw new BasoliaException(LanguageTools.GetLocalized("MEDIABOOM_BASOLIA_EXCEPTION_SETINTEGERPROPERTYFAILED").FormatString(propertyName, propertyValue), propertyResult);
            }
        }
        
        /// <summary>
        /// Sets an MPV double property
        /// </summary>
        /// <param name="basolia">Basolia instance that contains a valid handle</param>
        /// <param name="propertyName">Property name to set</param>
        /// <param name="propertyValue">Property value to set</param>
        /// <exception cref="BasoliaException"></exception>
        public static void SetDoubleProperty(BasoliaMedia? basolia, string propertyName, double propertyValue)
        {
            InitBasolia.CheckInited();
            if (basolia is null)
                throw new BasoliaException(LanguageTools.GetLocalized("MEDIABOOM_BASOLIA_EXCEPTION_BASOLIAMEDIA"), MpvError.MPV_ERROR_INVALID_PARAMETER);

            // We're now entering the dangerous zone
            unsafe
            {
                // Set the string property
                var handle = basolia._libmpvHandle;
                MpvError propertyResult = (MpvError)NativeInitializer.GetDelegate<NativeParameters.mpv_set_property_double>(NativeInitializer.libManagerMpv, nameof(NativeParameters.mpv_set_property)).Invoke(handle, propertyName, MpvValueFormat.MPV_FORMAT_DOUBLE, ref propertyValue);

                // TODO: MEDIABOOM_BASOLIA_EXCEPTION_SETDOUBLEPROPERTYFAILED -> Failed to set double property {0} to {1}
                if (propertyResult < MpvError.MPV_ERROR_SUCCESS)
                    throw new BasoliaException(LanguageTools.GetLocalized("MEDIABOOM_BASOLIA_EXCEPTION_SETDOUBLEPROPERTYFAILED").FormatString(propertyName, propertyValue), propertyResult);
            }
        }

        /// <summary>
        /// Gets an MPV string property
        /// </summary>
        /// <param name="basolia">Basolia instance that contains a valid handle</param>
        /// <param name="propertyName">Property name to set</param>
        /// <exception cref="BasoliaException"></exception>
        public static string GetStringProperty(BasoliaMedia? basolia, string propertyName)
        {
            InitBasolia.CheckInited();
            if (basolia is null)
                throw new BasoliaException(LanguageTools.GetLocalized("MEDIABOOM_BASOLIA_EXCEPTION_BASOLIAMEDIA"), MpvError.MPV_ERROR_INVALID_PARAMETER);

            // We're now entering the dangerous zone
            string value = "";
            unsafe
            {
                // Get the string property
                var handle = basolia._libmpvHandle;
                var buffer = IntPtr.Zero;
                MpvError propertyResult = (MpvError)NativeInitializer.GetDelegate<NativeParameters.mpv_get_property>(NativeInitializer.libManagerMpv, nameof(NativeParameters.mpv_get_property)).Invoke(handle, propertyName, MpvValueFormat.MPV_FORMAT_STRING, out buffer);

                // TODO: MEDIABOOM_BASOLIA_EXCEPTION_GETSTRINGPROPERTYFAILED -> Failed to get string property {0}
                if (propertyResult < MpvError.MPV_ERROR_SUCCESS)
                    throw new BasoliaException(LanguageTools.GetLocalized("MEDIABOOM_BASOLIA_EXCEPTION_GETSTRINGPROPERTYFAILED").FormatString(propertyName), propertyResult);

                // Convert the integer pointer to the string
                value = Marshal.PtrToStringAnsi(buffer);
            }

            // Return the property value
            return value;
        }

        /// <summary>
        /// Gets an MPV integer number property
        /// </summary>
        /// <param name="basolia">Basolia instance that contains a valid handle</param>
        /// <param name="propertyName">Property name to set</param>
        /// <exception cref="BasoliaException"></exception>
        public static long GetIntegerProperty(BasoliaMedia? basolia, string propertyName)
        {
            InitBasolia.CheckInited();
            if (basolia is null)
                throw new BasoliaException(LanguageTools.GetLocalized("MEDIABOOM_BASOLIA_EXCEPTION_BASOLIAMEDIA"), MpvError.MPV_ERROR_INVALID_PARAMETER);

            // We're now entering the dangerous zone
            long value = 0;
            unsafe
            {
                // Get the string property
                var handle = basolia._libmpvHandle;
                MpvError propertyResult = (MpvError)NativeInitializer.GetDelegate<NativeParameters.mpv_get_property_int>(NativeInitializer.libManagerMpv, nameof(NativeParameters.mpv_get_property)).Invoke(handle, propertyName, MpvValueFormat.MPV_FORMAT_INT64, out value);

                // TODO: MEDIABOOM_BASOLIA_EXCEPTION_GETINTEGERPROPERTYFAILED -> Failed to get integer property {0}
                if (propertyResult < MpvError.MPV_ERROR_SUCCESS)
                    throw new BasoliaException(LanguageTools.GetLocalized("MEDIABOOM_BASOLIA_EXCEPTION_GETINTEGERPROPERTYFAILED").FormatString(propertyName), propertyResult);
            }

            // Return the property value
            return value;
        }

        /// <summary>
        /// Gets an MPV double number property
        /// </summary>
        /// <param name="basolia">Basolia instance that contains a valid handle</param>
        /// <param name="propertyName">Property name to set</param>
        /// <exception cref="BasoliaException"></exception>
        public static double GetDoubleProperty(BasoliaMedia? basolia, string propertyName)
        {
            InitBasolia.CheckInited();
            if (basolia is null)
                throw new BasoliaException(LanguageTools.GetLocalized("MEDIABOOM_BASOLIA_EXCEPTION_BASOLIAMEDIA"), MpvError.MPV_ERROR_INVALID_PARAMETER);

            // We're now entering the dangerous zone
            double value = 0;
            unsafe
            {
                // Get the string property
                var handle = basolia._libmpvHandle;
                MpvError propertyResult = (MpvError)NativeInitializer.GetDelegate<NativeParameters.mpv_get_property_double>(NativeInitializer.libManagerMpv, nameof(NativeParameters.mpv_get_property)).Invoke(handle, propertyName, MpvValueFormat.MPV_FORMAT_INT64, out value);

                // TODO: MEDIABOOM_BASOLIA_EXCEPTION_GETDOUBLEPROPERTYFAILED -> Failed to get double property {0}
                if (propertyResult < MpvError.MPV_ERROR_SUCCESS)
                    throw new BasoliaException(LanguageTools.GetLocalized("MEDIABOOM_BASOLIA_EXCEPTION_GETDOUBLEPROPERTYFAILED").FormatString(propertyName), propertyResult);
            }

            // Return the property value
            return value;
        }
    }
}
