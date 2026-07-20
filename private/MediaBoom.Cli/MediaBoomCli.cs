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
using System.Reflection;
using Colorimetry;
using Colorimetry.Data;
using MediaBoom.Basolia;
using MediaBoom.Basolia.Media;
using MediaBoom.Cli.CliBase;
using MediaBoom.Cli.CliBase.Arguments;
using MediaBoom.Cli.Languages;
using Terminaux.Base;
using Terminaux.Base.Extensions;
using Terminaux.Shell.Arguments.Base;
using Terminaux.Writer.ConsoleWriters;

namespace MediaBoom.Cli
{
    internal class MediaBoomCli
    {
        internal static Version? mpgVer;
        internal static BasoliaMedia? basolia;
        internal static Color white = new(ConsoleColors.White);
        internal static bool isRadio;
        private static readonly Version? version = Assembly.GetAssembly(typeof(InitBasolia))?.GetName().Version;
        private static readonly Dictionary<string, ArgumentInfo> arguments = new()
        {
            { "radio", new("radio", "Radio mode", new RadioArgument()) },
            { "path", new("path", "Path to music or radio URL", new PathArgument()) },
        };

        static int Main(string[] args)
        {
            try
            {
                ConsoleMisc.SetTitle($"MediaBoom CLI - Basolia v{version?.ToString()}");

                // Parse arguments
                ArgumentParse.ParseArguments(args, arguments);

                // Initialize Basolia
                basolia = new();

                // Initialize versions
                mpgVer = InitBasolia.NativeLibVersion;

                // Now, open an interactive TUI
                ConsoleResizeHandler.StartResizeListener();
                if (isRadio)
                    Radio.RadioLoop();
                else
                    Player.PlayerLoop();
            }
            catch (Exception ex)
            {
                TextWriterColor.Write(LanguageTools.GetLocalized("MEDIABOOM_APP_FATALERROR") + "\n\n" + ex.ToString());
                return ex.HResult;
            }
            return 0;
        }
    }
}
