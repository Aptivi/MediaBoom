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
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using MediaBoom.Basolia;
using MediaBoom.Basolia.Exceptions;
using MediaBoom.Cli.Languages;
using MediaBoom.Cli.Tools;
using MediaBoom.Native.Interop.Enumerations;
using SpecProbe.Software.Platform;
using Terminaux.Base.Buffered;
using Terminaux.Inputs.Styles.Infobox;
using Terminaux.Inputs.Styles.Infobox.Tools;
using Terminaux.Writer.CyclicWriters.Renderer.Tools;
using Textify.General;

namespace MediaBoom.Cli.CliBase
{
    internal static class Common
    {
        internal static double volume = 1.0;
        internal static bool enableDisco = false;
        internal static int currentPos = 1;
        internal static bool exiting = false;
        internal static bool advance = false;
        internal static bool populate = true;
        internal static bool paused = false;
        internal static bool failedToPlay = false;
        internal static bool isRadioMode = false;
        internal static readonly List<CachedSongInfo> cachedInfos = [];

        internal static CachedSongInfo? CurrentCachedInfo =>
            cachedInfos.Count > 0 ? cachedInfos[currentPos - 1] : null;

        internal static void PopulatePassedPaths()
        {
            if (MediaBoomCli.isRadio)
            {
                if (Radio.passedRadioStationPaths.Count > 0)
                {
                    foreach (string path in Radio.passedRadioStationPaths)
                    {
                        try
                        {
                            RadioControls.PopulateRadioStationInfo(path);
                            populate = true;
                        }
                        catch (Exception ex)
                        {
                            InfoBoxModalColor.WriteInfoBoxModal(LanguageTools.GetLocalized("MEDIABOOM_BASOLIA_FILE_EXCEPTION_MUSICRADIOOPENFAILED") + $": {path}\n{ex.Message}");
                        }
                    }
                    Radio.passedRadioStationPaths.Clear();
                }
            }
            else
            {
                if (Player.passedMusicPaths.Count > 0)
                {
                    foreach (string path in Player.passedMusicPaths)
                    {
                        try
                        {
                            PlayerControls.PopulateMusicFileInfo(path);
                            populate = true;
                        }
                        catch (Exception ex)
                        {
                            InfoBoxModalColor.WriteInfoBoxModal(LanguageTools.GetLocalized("MEDIABOOM_APP_NOTFOUND").FormatString(path) + $"\n{ex.Message}");
                        }
                    }
                    Player.passedMusicPaths.Clear();
                }
            }
        }

        internal static void RaiseVolume()
        {
            if (MediaBoomCli.basolia is null)
                throw new BasoliaException(LanguageTools.GetLocalized("MEDIABOOM_BASOLIA_EXCEPTION_BASOLIAMEDIA"), MpvError.MPV_ERROR_GENERIC);
            volume += 5;
            if (volume > 100)
                volume = 100;
            MediaBoomCli.basolia.SetVolume(volume);
        }

        internal static void LowerVolume()
        {
            if (MediaBoomCli.basolia is null)
                throw new BasoliaException(LanguageTools.GetLocalized("MEDIABOOM_BASOLIA_EXCEPTION_BASOLIAMEDIA"), MpvError.MPV_ERROR_GENERIC);
            volume -= 5;
            if (volume < 0)
                volume = 0;
            MediaBoomCli.basolia.SetVolume(volume);
        }

        internal static void Exit()
        {
            if (MediaBoomCli.basolia is null)
                throw new BasoliaException(LanguageTools.GetLocalized("MEDIABOOM_BASOLIA_EXCEPTION_BASOLIAMEDIA"), MpvError.MPV_ERROR_GENERIC);
            exiting = true;
            advance = false;
            if (MediaBoomCli.basolia.IsOpened())
                MediaBoomCli.basolia.Stop();
        }

        internal static void Switch(string musicPath)
        {
            if (MediaBoomCli.basolia is null)
                throw new BasoliaException(LanguageTools.GetLocalized("MEDIABOOM_BASOLIA_EXCEPTION_BASOLIAMEDIA"), MpvError.MPV_ERROR_GENERIC);
            if (MediaBoomCli.basolia.IsOpened())
                MediaBoomCli.basolia.CloseFile();
            if (isRadioMode)
                MediaBoomCli.basolia.OpenUrl(musicPath);
            else
                MediaBoomCli.basolia.OpenFile(musicPath);
        }

        internal static void ShowSpecs()
        {
            InfoBoxModalColor.WriteInfoBoxModal(
                $$"""
                MediaBoom specifications
                =======================

                Basolia version: {{InitBasolia.BasoliaVersion}}
                libmpv version: {{InitBasolia.NativeLibVersion}}

                System specifications
                =====================

                System: {{(PlatformHelper.IsOnWindows() ? "Windows" : PlatformHelper.IsOnMacOS() ? "macOS" : "Unix/Linux")}}
                System Architecture: {{RuntimeInformation.OSArchitecture}}
                Process Architecture: {{RuntimeInformation.ProcessArchitecture}}
                System description: {{RuntimeInformation.OSDescription}}
                .NET description: {{RuntimeInformation.FrameworkDescription}}
                """
            );
        }

        internal static void ShowHelp()
        {
            InfoBoxModalColor.WriteInfoBoxModal(KeybindingTools.RenderKeybindingHelpText(Player.AllBindings), new InfoBoxSettings()
            {
                Title = LanguageTools.GetLocalized("MEDIABOOM_APP_COMMON_AVAILABLEKEYSTROKES"),
            });
        }

        internal static void ShowHelpRadio()
        {
            InfoBoxModalColor.WriteInfoBoxModal(KeybindingTools.RenderKeybindingHelpText(Radio.AllBindings), new InfoBoxSettings()
            {
                Title = LanguageTools.GetLocalized("MEDIABOOM_APP_COMMON_AVAILABLEKEYSTROKES"),
            });
        }

        internal static void HandleKeypressCommon(ConsoleKeyInfo keystroke, Screen playerScreen, bool radio)
        {
            switch (keystroke.Key)
            {
                case ConsoleKey.UpArrow:
                    RaiseVolume();
                    break;
                case ConsoleKey.DownArrow:
                    LowerVolume();
                    break;
                case ConsoleKey.H:
                    if (radio)
                        ShowHelpRadio();
                    else
                        ShowHelp();
                    playerScreen.RequireRefresh();
                    break;
                case ConsoleKey.Z:
                    ShowSpecs();
                    playerScreen.RequireRefresh();
                    break;
                case ConsoleKey.L:
                    enableDisco = !enableDisco;
                    break;
                case ConsoleKey.F1:
                    string path = InfoBoxInputColor.WriteInfoBoxInput(LanguageTools.GetLocalized("MEDIABOOM_APP_COMMON_PATHTOPLAYLIST"));
                    playerScreen.RequireRefresh();
                    if (string.IsNullOrEmpty(path))
                    {
                        playerScreen.RequireRefresh();
                        break;
                    }

                    // Check for extension
                    string extension = Path.GetExtension(path);
                    if (extension != ".m3u" && extension != ".m3u8")
                        path += ".m3u";

                    // Get a list of paths and write the file
                    string[] paths = cachedInfos.Select((csi) => csi.MusicPath).ToArray();
                    File.WriteAllLines(path, paths);
                    playerScreen.RequireRefresh();
                    break;
                case ConsoleKey.Q:
                    Exit();
                    break;
            }
        }
    }
}
