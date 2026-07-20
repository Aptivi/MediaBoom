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
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using Colorimetry.Data;
using Colorimetry.Transformation;
using MediaBoom.Basolia.Media;
using MediaBoom.Basolia.Media.Helpers;
using MediaBoom.QuickPlay.Arguments;
using MediaBoom.QuickPlay.Languages;
using Terminaux.Base;
using Terminaux.Base.Extensions;
using Terminaux.Inputs;
using Terminaux.Shell.Arguments.Base;
using Terminaux.Themes.Colors;
using Terminaux.Writer.ConsoleWriters;
using Terminaux.Writer.CyclicWriters.Simple;
using Threadify.Manager;

namespace MediaBoom.QuickPlay
{
    internal class QuickPlayer
    {
        internal static bool quiet;
        internal static string musicPath = "";
        private static ThreadInstance playerThread = new("Player thread", false, (basolia) => ((BasoliaMedia?)basolia)?.Play());
        private static readonly Dictionary<string, ArgumentInfo> arguments = new()
        {
            { "quiet", new("quiet", "Quiet mode", new QuietArgument()) },
            { "path", new("path", "Path to music file", new PathArgument()) },
        };

        static int Main(string[] args)
        {
            // Parse the arguments
            ArgumentParse.ParseArguments(args, arguments);

            // Check to see if a music file has been specified and is found
            if (string.IsNullOrEmpty(musicPath))
            {
                TextWriterColor.Write(LanguageTools.GetLocalized("MEDIABOOM_QUICKPLAYER_NOTSPECIFIED"), ThemeColorType.Error);
                return 1;
            }
            if (!File.Exists(musicPath))
            {
                TextWriterColor.Write(LanguageTools.GetLocalized("MEDIABOOM_QUICKPLAYER_NOTFOUND"), ThemeColorType.Error, musicPath);
                return 2;
            }
            if (!quiet)
                TextWriterColor.Write(LanguageTools.GetLocalized("MEDIABOOM_QUICKPLAYER_OPENING"), ThemeColorType.Progress, musicPath);

            // Open the music file
            var basoliaMedia = new BasoliaMedia();
            basoliaMedia.OpenFile(musicPath);

            // Get metadata info and display them
            string musicName = MpvPropertyHandler.GetStringProperty(basoliaMedia, "media-title");
            string musicArtist = LanguageTools.GetLocalized("MEDIABOOM_QUICKPLAYER_UNKNOWNARTIST");
            if (!quiet)
            {
                TextWriterColor.Write($" >> {musicArtist} - {musicName}", ThemeColorType.Success);
                TextWriterColor.Write(LanguageTools.GetLocalized("MEDIABOOM_QUICKPLAYER_EXITTIP") + "\n", ThemeColorType.Tip);
            }

            // Play the music file, and display live status
            try
            {
                long duration = basoliaMedia.GetDuration();
                var durationProgress = new SimpleProgress(0, (int)duration)
                {
                    ShowPercentage = false,
                    Accurate = true,
                    ProgressActiveForegroundColor = ConsoleColors.Lime,
                    ProgressForegroundColor = TransformationTools.GetDarkBackground(ConsoleColors.Lime),
                };
                var builder = new StringBuilder();
                playerThread.Start(basoliaMedia);
                SpinWait.SpinUntil(basoliaMedia.IsPlaying);
                while (basoliaMedia.IsPlaying())
                {
                    if (!quiet)
                    {
                        // Get duration information
                        long currentDuration = basoliaMedia.GetCurrentDuration();
                        string durationSpan = TimeSpan.FromSeconds(duration).ToString();
                        string currentDurationSpan = TimeSpan.FromSeconds(currentDuration).ToString();
                        string durationIndicator = $"{currentDurationSpan} / {durationSpan}";

                        // Get progress width
                        int durationIndicatorTextWidth = ConsoleChar.EstimateCellWidth(durationIndicator);
                        int totalWidth = ConsoleWrapper.WindowWidth - durationIndicatorTextWidth - 1;
                        durationProgress.Width = totalWidth;
                        durationProgress.Position = (int)currentDuration;

                        // Display duration and progress
                        builder.Clear();
                        builder.Append('\r');
                        builder.Append($"{durationIndicator} {durationProgress.Render()}");
                        builder.Append(ConsoleClearing.GetClearLineToRightSequence());
                        TextWriterRaw.WriteRaw(builder.ToString());
                    }

                    // Check to see if a user pressed a key
                    var keypress = Input.ReadPointerOrKeyNoBlock(InputEventType.Keyboard);
                    if (keypress.ConsoleKeyInfo is ConsoleKeyInfo cki && cki.Key == ConsoleKey.Q)
                        basoliaMedia.Stop();
                }
                if (!quiet)
                    TextWriterRaw.Write();
            }
            catch (Exception ex)
            {
                TextWriterColor.Write(LanguageTools.GetLocalized("MEDIABOOM_QUICKPLAYER_FAILURE") + $": {ex.Message}", ThemeColorType.Error);
            }

            // Return success to the OS
            return 0;
        }
    }
}
