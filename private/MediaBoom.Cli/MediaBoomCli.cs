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
using System.Reflection;
using MediaBoom.Basolia;
using MediaBoom.Cli.CliBase;
using MediaBoom.Cli.Languages;
using Terminaux.Base;
using Terminaux.Base.Buffered;
using Terminaux.Base.Extensions;
using Terminaux.Colors;
using Terminaux.Colors.Data;
using Terminaux.Writer.ConsoleWriters;

namespace MediaBoom.Cli
{
    internal class MediaBoomCli
    {
        private static readonly Version? version = Assembly.GetAssembly(typeof(InitBasolia))?.GetName().Version;
        internal static Version? mpgVer;
        internal static BasoliaMedia? basolia;
        internal static Color white = new(ConsoleColors.White);

        static int Main(string[] args)
        {
            try
            {
                ConsoleMisc.SetTitle($"MediaBoom CLI - Basolia v{version?.ToString()}");

                // First, prompt for the music path if no arguments are provided.
                string[] arguments = args.Where((arg) => !arg.StartsWith("-")).ToArray();
                string[] switches = args.Where((arg) => arg.StartsWith("-")).ToArray();
                bool isRadio = switches.Contains("-r");
                if (arguments.Length != 0)
                {
                    string musicPath = args[0];

                    // Check for existence.
                    if (string.IsNullOrEmpty(musicPath) || (!isRadio && !File.Exists(musicPath)))
                    {
                        TextWriterColor.Write(LanguageTools.GetLocalized("MEDIABOOM_APP_NOTFOUND"), musicPath);
                        return 1;
                    }
                    if (!isRadio)
                        Player.passedMusicPaths.Add(musicPath);
                }

                // Initialize Basolia
                basolia = new();

                // Initialize versions
                mpgVer = InitBasolia.NativeLibVersion;

                // Now, open an interactive TUI
                ConsoleResizeHandler.StartResizeListener((_, _, _, _) => ScreenTools.CurrentScreen?.RequireRefresh());
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
