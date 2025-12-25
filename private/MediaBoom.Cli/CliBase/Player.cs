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
using System.Linq;
using System.Text;
using System.Threading;
using MediaBoom.Basolia.Exceptions;
using MediaBoom.Basolia.File;
using MediaBoom.Basolia.Playback;
using Terminaux.Base;
using Terminaux.Base.Buffered;
using Terminaux.Base.Extensions;
using Terminaux.Colors;
using Terminaux.Colors.Data;
using Terminaux.Colors.Transformation;
using Terminaux.Inputs;
using Terminaux.Inputs.Styles;
using Terminaux.Inputs.Styles.Infobox;
using Terminaux.Writer.ConsoleWriters;
using Terminaux.Writer.CyclicWriters.Graphical;
using Terminaux.Writer.CyclicWriters.Renderer;
using Terminaux.Writer.CyclicWriters.Renderer.Tools;
using Terminaux.Writer.CyclicWriters.Simple;

namespace MediaBoom.Cli.CliBase
{
    internal static class Player
    {
        internal static Thread? playerThread;
        internal static long position = 0;
        internal static readonly List<string> passedMusicPaths = [];
        internal static readonly Keybinding[] showBindings =
        [
            new("Play/Pause", ConsoleKey.Spacebar),
            new("Stop", ConsoleKey.Escape),
            new("Exit", ConsoleKey.Q),
            new("Help", ConsoleKey.H),
        ];
        internal static readonly Keybinding[] allBindings =
        [
            new("Play/Pause", ConsoleKey.Spacebar),
            new("Stop", ConsoleKey.Escape),
            new("Exit", ConsoleKey.Q),
            new("Increase volume", ConsoleKey.UpArrow),
            new("Decrease volume", ConsoleKey.DownArrow),
            new("Seek backwards", ConsoleKey.LeftArrow),
            new("Seek forwards", ConsoleKey.RightArrow),
            new("Decrease seek duration", ConsoleKey.LeftArrow, ConsoleModifiers.Control),
            new("Increase seek duration", ConsoleKey.RightArrow, ConsoleModifiers.Control),
            new("Song information", ConsoleKey.I),
            new("Add a music file", ConsoleKey.A),
            new("Add a music group from playlist", ConsoleKey.A, ConsoleModifiers.Shift),
            new("Add a music directory to the list (when idle)", ConsoleKey.S),
            new("Previous song", ConsoleKey.B),
            new("Next song", ConsoleKey.N),
            new("Remove current song", ConsoleKey.R),
            new("Remove all songs", ConsoleKey.R, ConsoleModifiers.Control),
            new("Selectively seek (when playing)", ConsoleKey.S),
            new("Seek to previous lyric (when playing)", ConsoleKey.F),
            new("Seek to next lyric (when playing)", ConsoleKey.G),
            new("Seek to current lyric (when playing)", ConsoleKey.J),
            new("Seek to which lyric (when playing)", ConsoleKey.K),
            new("Set repeat checkpoint", ConsoleKey.C),
            new("Seek to repeat checkpoint", ConsoleKey.C, ConsoleModifiers.Shift),
            new("Disco Mode!", ConsoleKey.L),
            new("Save to playlist", ConsoleKey.F1),
            new("System information", ConsoleKey.Z),
        ];

        public static void PlayerLoop()
        {
            // Populate the screen
            Screen playerScreen = new();
            ScreenTools.SetCurrent(playerScreen);

            // Make a screen part to draw our TUI
            ScreenPart screenPart = new();

            // Handle drawing
            screenPart.AddDynamicText(HandleDraw);

            // Current duration
            int hue = 0;
            screenPart.AddDynamicText(() =>
            {
                if (Common.CurrentCachedInfo is null)
                    return "";

                // Get the song name
                var buffer = new StringBuilder();
                string name = PlayerControls.RenderSongName(Common.CurrentCachedInfo.MusicPath);

                // Get the positions and the amount of songs per page
                int startPos = 4;
                int endPos = ConsoleWrapper.WindowHeight - 4;
                int songsPerPage = endPos - startPos;

                // Get the position
                position = FileTools.IsOpened(MediaBoomCli.basolia) ? PlaybackPositioningTools.GetCurrentDuration(MediaBoomCli.basolia) : 0;
                var posSpan = FileTools.IsOpened(MediaBoomCli.basolia) ? PlaybackPositioningTools.GetCurrentDurationSpan(MediaBoomCli.basolia) : new();

                // Disco effect!
                var disco = PlaybackTools.IsPlaying(MediaBoomCli.basolia) && Common.enableDisco ? new Color($"hsl:{hue};50;50") : MediaBoomCli.white;
                if (PlaybackTools.IsPlaying(MediaBoomCli.basolia))
                {
                    hue++;
                    if (hue >= 360)
                        hue = 0;
                }

                // Render the song list box frame and the duration bar
                var listBoxFrame = new BoxFrame()
                {
                    Text = name,
                    Left = 2,
                    Top = 1,
                    Width = ConsoleWrapper.WindowWidth - 6,
                    Height = songsPerPage,
                    FrameColor = disco,
                    TitleColor = disco,
                };
                var durationBar = new SimpleProgress((int)(100 * (position / (double)Common.CurrentCachedInfo.Duration)), 100)
                {
                    Width = ConsoleWrapper.WindowWidth - 4,
                    ShowPercentage = false,
                    ProgressForegroundColor = TransformationTools.GetDarkBackground(disco),
                    ProgressActiveForegroundColor = disco,
                };
                buffer.Append(
                    listBoxFrame.Render() +
                    RendererTools.RenderRenderable(durationBar, new(2, ConsoleWrapper.WindowHeight - 3))
                );

                // Render the indicator
                string indicator =
                    $"Seek: {PlayerControls.seekRate:0.00} | " +
                    $"Volume: {Common.volume:0}%{disco.VTSequenceForeground}";

                // Render the lyric
                string lyric = Common.CurrentCachedInfo.LyricInstance is not null ? Common.CurrentCachedInfo.LyricInstance.GetLastLineCurrent(MediaBoomCli.basolia) : "";
                string finalLyric = string.IsNullOrWhiteSpace(lyric) ? "..." : lyric;

                // Render the results
                string indicatorTextStr = $"{posSpan} / {Common.CurrentCachedInfo.DurationSpan} | {indicator}";
                string lyricTextStr = Common.CurrentCachedInfo.LyricInstance is not null && PlaybackTools.IsPlaying(MediaBoomCli.basolia) ? $"{finalLyric}" : "";
                int indicatorWidth = ConsoleChar.EstimateCellWidth(indicatorTextStr);
                int lyricTextWidth = ConsoleChar.EstimateCellWidth(lyricTextStr);
                var eraser = new Eraser()
                {
                    Left = 2,
                    Top = ConsoleWrapper.WindowHeight - 4,
                    Width = ConsoleWrapper.WindowWidth - 4,
                    Height = 1,
                };
                var indicatorText = new BoundedText()
                {
                    Left = 2,
                    Top = ConsoleWrapper.WindowHeight - 4,
                    ForegroundColor = disco,
                    Width = ConsoleWrapper.WindowWidth - 7 - lyricTextWidth,
                    Height = 1,
                    Text = indicatorTextStr
                };
                var lyricText = new BoundedText()
                {
                    Left = 2,
                    Top = ConsoleWrapper.WindowHeight - 4,
                    ForegroundColor = disco,
                    Width = ConsoleWrapper.WindowWidth - 4,
                    Height = 1,
                    Text = lyricTextStr,
                    Settings = new()
                    {
                        Alignment = TextAlignment.Right,
                    }
                };
                buffer.Append(
                    eraser.Render() +
                    indicatorText.Render() +
                    lyricText.Render()
                );
                return buffer.ToString();
            });

            // Render the buffer
            playerScreen.AddBufferedPart("MediaBoom Player", screenPart);

            // Then, the main loop
            while (!Common.exiting)
            {
                Thread.Sleep(1);
                try
                {
                    if (!playerScreen.CheckBufferedPart("MediaBoom Player"))
                        playerScreen.AddBufferedPart("MediaBoom Player", screenPart);
                    ScreenTools.Render();

                    // Handle the keystroke
                    if (ConsoleWrapper.KeyAvailable)
                    {
                        var keystroke = Input.ReadKey();
                        if (PlaybackTools.IsPlaying(MediaBoomCli.basolia))
                            HandleKeypressPlayMode(keystroke, playerScreen);
                        else
                            HandleKeypressIdleMode(keystroke, playerScreen);
                    }
                }
                catch (BasoliaException bex)
                {
                    if (PlaybackTools.IsPlaying(MediaBoomCli.basolia))
                        PlaybackTools.Stop(MediaBoomCli.basolia);
                    InfoBoxModalColor.WriteInfoBoxModal("There's an error with Basolia when trying to process the music file.\n\n" + bex.Message);
                    playerScreen.RequireRefresh();
                }
                catch (Exception ex)
                {
                    if (PlaybackTools.IsPlaying(MediaBoomCli.basolia))
                        PlaybackTools.Stop(MediaBoomCli.basolia);
                    InfoBoxModalColor.WriteInfoBoxModal("There's an unknown error when trying to process the music file.\n\n" + ex.Message);
                    playerScreen.RequireRefresh();
                }
            }

            // Close the file if open
            if (FileTools.IsOpened(MediaBoomCli.basolia))
                FileTools.CloseFile(MediaBoomCli.basolia);

            // Restore state
            ConsoleWrapper.CursorVisible = true;
            ColorTools.LoadBack();
            playerScreen.RemoveBufferedParts();
            ScreenTools.UnsetCurrent(playerScreen);
        }

        private static void HandleKeypressIdleMode(ConsoleKeyInfo keystroke, Screen playerScreen)
        {
            switch (keystroke.Key)
            {
                case ConsoleKey.Spacebar:
                    playerThread = new(HandlePlay);
                    PlayerControls.Play();
                    break;
                case ConsoleKey.B:
                    PlayerControls.SeekBeginning();
                    PlayerControls.PreviousSong();
                    playerScreen.RequireRefresh();
                    break;
                case ConsoleKey.N:
                    PlayerControls.SeekBeginning();
                    PlayerControls.NextSong();
                    playerScreen.RequireRefresh();
                    break;
                case ConsoleKey.I:
                    PlayerControls.ShowSongInfo();
                    playerScreen.RequireRefresh();
                    break;
                case ConsoleKey.A:
                    if (keystroke.Modifiers == ConsoleModifiers.Shift)
                        PlayerControls.PromptForAddSongs();
                    else
                        PlayerControls.PromptForAddSong();
                    playerScreen.RequireRefresh();
                    break;
                case ConsoleKey.S:
                    PlayerControls.PromptForAddDirectory();
                    playerScreen.RequireRefresh();
                    break;
                case ConsoleKey.R:
                    PlayerControls.Stop(false);
                    PlayerControls.SeekBeginning();
                    if (keystroke.Modifiers == ConsoleModifiers.Control)
                        PlayerControls.RemoveAllSongs();
                    else
                        PlayerControls.RemoveCurrentSong();
                    playerScreen.RequireRefresh();
                    break;
                case ConsoleKey.C:
                    if (Common.CurrentCachedInfo is null)
                        return;
                    if (keystroke.Modifiers == ConsoleModifiers.Shift)
                        PlayerControls.SeekTo(Common.CurrentCachedInfo.RepeatCheckpoint);
                    else
                        Common.CurrentCachedInfo.RepeatCheckpoint = PlaybackPositioningTools.GetCurrentDurationSpan(MediaBoomCli.basolia);
                    break;
                default:
                    Common.HandleKeypressCommon(keystroke, playerScreen, false);
                    break;
            }
        }

        private static void HandleKeypressPlayMode(ConsoleKeyInfo keystroke, Screen playerScreen)
        {
            switch (keystroke.Key)
            {
                case ConsoleKey.RightArrow:
                    if (keystroke.Modifiers == ConsoleModifiers.Control)
                        PlayerControls.seekRate += 50;
                    else
                        PlayerControls.SeekForward();
                    break;
                case ConsoleKey.LeftArrow:
                    if (keystroke.Modifiers == ConsoleModifiers.Control)
                        PlayerControls.seekRate -= 50;
                    else
                        PlayerControls.SeekBackward();
                    break;
                case ConsoleKey.B:
                    PlayerControls.Stop(false);
                    PlayerControls.SeekBeginning();
                    PlayerControls.PreviousSong();
                    playerThread = new(HandlePlay);
                    PlayerControls.Play();
                    playerScreen.RequireRefresh();
                    break;
                case ConsoleKey.F:
                    PlayerControls.SeekPreviousLyric();
                    break;
                case ConsoleKey.G:
                    PlayerControls.SeekNextLyric();
                    break;
                case ConsoleKey.J:
                    PlayerControls.SeekCurrentLyric();
                    break;
                case ConsoleKey.K:
                    PlayerControls.SeekWhichLyric();
                    playerScreen.RequireRefresh();
                    break;
                case ConsoleKey.N:
                    PlayerControls.Stop(false);
                    PlayerControls.SeekBeginning();
                    PlayerControls.NextSong();
                    playerThread = new(HandlePlay);
                    PlayerControls.Play();
                    playerScreen.RequireRefresh();
                    break;
                case ConsoleKey.Spacebar:
                    PlayerControls.Pause();
                    playerScreen.RequireRefresh();
                    break;
                case ConsoleKey.R:
                    PlayerControls.Stop(false);
                    PlayerControls.SeekBeginning();
                    if (keystroke.Modifiers == ConsoleModifiers.Control)
                        PlayerControls.RemoveAllSongs();
                    else
                        PlayerControls.RemoveCurrentSong();
                    playerScreen.RequireRefresh();
                    break;
                case ConsoleKey.Escape:
                    PlayerControls.Stop();
                    break;
                case ConsoleKey.I:
                    PlayerControls.ShowSongInfo();
                    playerScreen.RequireRefresh();
                    break;
                case ConsoleKey.S:
                    PlayerControls.PromptSeek();
                    playerScreen.RequireRefresh();
                    break;
                case ConsoleKey.D:
                    PlayerControls.Pause();
                    Common.HandleKeypressCommon(keystroke, playerScreen, false);
                    playerThread = new(HandlePlay);
                    PlayerControls.Play();
                    playerScreen.RequireRefresh();
                    break;
                case ConsoleKey.C:
                    if (Common.CurrentCachedInfo is null)
                        return;
                    if (keystroke.Modifiers == ConsoleModifiers.Shift)
                        PlayerControls.SeekTo(Common.CurrentCachedInfo.RepeatCheckpoint);
                    else
                        Common.CurrentCachedInfo.RepeatCheckpoint = PlaybackPositioningTools.GetCurrentDurationSpan(MediaBoomCli.basolia);
                    break;
                default:
                    Common.HandleKeypressCommon(keystroke, playerScreen, false);
                    break;
            }
        }

        private static void HandlePlay()
        {
            try
            {
                foreach (var musicFile in Common.cachedInfos.Skip(Common.currentPos - 1))
                {
                    if (!Common.advance || Common.exiting)
                        return;
                    else
                    {
                        ScreenTools.CurrentScreen?.RequireRefresh();
                        Common.populate = true;
                    }
                    Common.currentPos = Common.cachedInfos.IndexOf(musicFile) + 1;
                    PlayerControls.PopulateMusicFileInfo(musicFile.MusicPath);
                    if (Common.paused)
                    {
                        Common.paused = false;
                        PlaybackPositioningTools.SeekTo(MediaBoomCli.basolia, position);
                    }
                    PlaybackTools.Play(MediaBoomCli.basolia);
                }
            }
            catch (Exception ex)
            {
                InfoBoxModalColor.WriteInfoBoxModal($"Playback failure: {ex.Message}");
                Common.failedToPlay = true;
            }
        }

        private static string HandleDraw()
        {
            if (!ScreenTools.CurrentScreen?.RefreshWasDone ?? false)
                return "";

            // Prepare things
            var drawn = new StringBuilder();
            ConsoleWrapper.CursorVisible = false;

            // First, print the keystrokes
            var keybindings = new Keybindings()
            {
                KeybindingList = showBindings,
                Width = ConsoleWrapper.WindowWidth - 1,
            };
            drawn.Append(RendererTools.RenderRenderable(keybindings, new(0, ConsoleWrapper.WindowHeight - 1)));

            // In case we have no songs in the playlist...
            if (Common.cachedInfos.Count == 0)
            {
                if (passedMusicPaths.Count > 0)
                {
                    foreach (string path in passedMusicPaths)
                    {
                        PlayerControls.PopulateMusicFileInfo(path);
                        Common.populate = true;
                    }
                    passedMusicPaths.Clear();
                }
                else
                {
                    int height = (ConsoleWrapper.WindowHeight - 2) / 2;
                    var message = new AlignedText()
                    {
                        Top = height,
                        Text = "Press 'A' to insert a single song to the playlist, or 'S' to insert the whole music library.",
                        Settings = new()
                        {
                            Alignment = TextAlignment.Middle
                        }
                    };
                    drawn.Append(message.Render());
                    return drawn.ToString();
                }
            }

            // Populate music file info, as necessary
            string name = "";
            if (Common.CurrentCachedInfo is not null)
            {
                if (Common.populate)
                    PlayerControls.PopulateMusicFileInfo(Common.CurrentCachedInfo.MusicPath);
                name = PlayerControls.RenderSongName(Common.CurrentCachedInfo.MusicPath);
            }

            // Now, populate the input choice information instances that represent songs
            var choices = new List<InputChoiceInfo>();
            int startPos = 4;
            int endPos = ConsoleWrapper.WindowHeight - 4;
            int songsPerPage = endPos - startPos;
            int max = Common.cachedInfos.Select((_, idx) => idx).Max((idx) => $"  {idx + 1}) ".Length);
            for (int i = 0; i < Common.cachedInfos.Count; i++)
            {
                // Populate the first pane
                var (musicName, musicArtist) = PlayerControls.GetMusicNameArtist(i);
                string duration = Common.cachedInfos[i].DurationSpan;
                string songPreview = $"[{duration}] {musicArtist} - {musicName}";
                choices.Add(new($"{i + 1}", songPreview));
            }

            // Render the selections inside the box
            var playlistBoxFrame = new BoxFrame()
            {
                Text = name,
                Left = 2,
                Top = 1,
                Width = ConsoleWrapper.WindowWidth - 6,
                Height = songsPerPage,
            };
            var playlistSelections = new Selection([.. choices])
            {
                Left = 3,
                Top = 2,
                CurrentSelection = Common.currentPos - 1,
                Height = songsPerPage,
                Width = ConsoleWrapper.WindowWidth - 6,
                Settings = new()
                {
                    SelectedOptionColor = ConsoleColors.Green,
                    OptionColor = ConsoleColors.Silver,
                }
            };
            drawn.Append(
                playlistBoxFrame.Render() +
                playlistSelections.Render()
            );
            return drawn.ToString();
        }
    }
}
