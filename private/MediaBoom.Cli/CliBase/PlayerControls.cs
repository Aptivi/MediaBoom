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
using System.Threading;
using BassBoom.Basolia.Enumerations;
using MediaBoom.Basolia.File;
using MediaBoom.Basolia.Helpers;
using MediaBoom.Basolia.Lyrics;
using MediaBoom.Basolia.Playback;
using MediaBoom.Basolia.Playback.Playlists;
using MediaBoom.Basolia.Playback.Playlists.Enumerations;
using MediaBoom.Cli.Languages;
using MediaBoom.Cli.Tools;
using Terminaux.Base.Buffered;
using Terminaux.Inputs.Styles;
using Terminaux.Inputs.Styles.Infobox;
using Textify.General;

namespace MediaBoom.Cli.CliBase
{
    internal static class PlayerControls
    {
        internal static long seekRate = 3;

        internal static void SeekForward()
        {
            // In case we have no songs in the playlist...
            if (Common.cachedInfos.Count == 0)
                return;
            if (Common.CurrentCachedInfo is null)
                return;

            Player.position += (int)seekRate;
            if (Player.position > Common.CurrentCachedInfo.Duration)
                Player.position = Common.CurrentCachedInfo.Duration;
            PlaybackPositioningTools.SeekTo(MediaBoomCli.basolia, Player.position);
        }

        internal static void SeekBackward()
        {
            // In case we have no songs in the playlist...
            if (Common.cachedInfos.Count == 0)
                return;
            if (Common.CurrentCachedInfo is null)
                return;

            Player.position -= (int)seekRate;
            if (Player.position < 0)
                Player.position = 0;
            PlaybackPositioningTools.SeekTo(MediaBoomCli.basolia, Player.position);
        }

        internal static void SeekBeginning()
        {
            // In case we have no songs in the playlist...
            if (Common.cachedInfos.Count == 0)
                return;

            PlaybackPositioningTools.SeekToTheBeginning(MediaBoomCli.basolia);
            Player.position = 0;
        }

        internal static void SeekPreviousLyric()
        {
            // In case we have no songs in the playlist, or we have no lyrics...
            if (Common.cachedInfos.Count == 0)
                return;
            if (Common.CurrentCachedInfo is null)
                return;
            if (Common.CurrentCachedInfo.LyricInstance is null)
                return;

            var lyrics = Common.CurrentCachedInfo.LyricInstance.GetLinesCurrent(MediaBoomCli.basolia);
            if (lyrics.Length == 0)
                return;
            var lyric = lyrics.Length == 1 ? lyrics[0] : lyrics[lyrics.Length - 2];
            PlaybackPositioningTools.SeekLyric(MediaBoomCli.basolia, lyric);
        }

        internal static void SeekCurrentLyric()
        {
            // In case we have no songs in the playlist, or we have no lyrics...
            if (Common.cachedInfos.Count == 0)
                return;
            if (Common.CurrentCachedInfo is null)
                return;
            if (Common.CurrentCachedInfo.LyricInstance is null)
                return;

            var lyrics = Common.CurrentCachedInfo.LyricInstance.GetLinesCurrent(MediaBoomCli.basolia);
            if (lyrics.Length == 0)
                return;
            var lyric = lyrics[lyrics.Length - 1];
            PlaybackPositioningTools.SeekLyric(MediaBoomCli.basolia, lyric);
        }

        internal static void SeekNextLyric()
        {
            // In case we have no songs in the playlist, or we have no lyrics...
            if (Common.cachedInfos.Count == 0)
                return;
            if (Common.CurrentCachedInfo is null)
                return;
            if (Common.CurrentCachedInfo.LyricInstance is null)
                return;

            var lyrics = Common.CurrentCachedInfo.LyricInstance.GetLinesUpcoming(MediaBoomCli.basolia);
            if (lyrics.Length == 0)
            {
                SeekCurrentLyric();
                return;
            }
            var lyric = lyrics[0];
            PlaybackPositioningTools.SeekLyric(MediaBoomCli.basolia, lyric);
        }

        internal static void SeekWhichLyric()
        {
            // In case we have no songs in the playlist, or we have no lyrics...
            if (Common.cachedInfos.Count == 0)
                return;
            if (Common.CurrentCachedInfo is null)
                return;
            if (Common.CurrentCachedInfo.LyricInstance is null)
                return;

            var lyrics = Common.CurrentCachedInfo.LyricInstance.Lines;
            var choices = lyrics.Select((line) => new InputChoiceInfo($"{line.LineSpan}", line.Line)).ToArray();
            int index = InfoBoxSelectionColor.WriteInfoBoxSelection(choices, LanguageTools.GetLocalized("MEDIABOOM_APP_PLAYER_SELECTLYRICSEEK"));
            if (index == -1)
                return;
            var lyric = lyrics[index];
            PlaybackPositioningTools.SeekLyric(MediaBoomCli.basolia, lyric);
        }

        internal static void SeekTo(TimeSpan target)
        {
            // In case we have no songs in the playlist...
            if (Common.cachedInfos.Count == 0)
                return;
            if (Common.CurrentCachedInfo is null)
                return;

            Player.position = (int)target.TotalSeconds;
            if (Player.position > Common.CurrentCachedInfo.Duration)
                Player.position = 0;
            PlaybackPositioningTools.SeekTo(MediaBoomCli.basolia, Player.position);
        }

        internal static void Play()
        {
            // In case we have no songs in the playlist...
            if (Common.cachedInfos.Count == 0)
                return;
            if (Player.playerThread is null)
                return;

            if (PlaybackTools.GetState(MediaBoomCli.basolia) == PlaybackState.Stopped)
                // There could be a chance that the music has fully stopped without any user interaction.
                PlaybackPositioningTools.SeekToTheBeginning(MediaBoomCli.basolia);
            Common.advance = true;
            Player.playerThread.Start();
            SpinWait.SpinUntil(() => PlaybackTools.IsPlaying(MediaBoomCli.basolia) || Common.failedToPlay);
            Common.failedToPlay = false;
        }

        internal static void Pause()
        {
            Common.advance = false;
            Common.paused = true;
            PlaybackTools.Pause(MediaBoomCli.basolia);
        }

        internal static void Stop(bool resetCurrentSong = true)
        {
            Common.advance = false;
            Common.paused = false;
            if (resetCurrentSong)
                Common.currentPos = 1;
            PlaybackTools.Stop(MediaBoomCli.basolia);
        }

        internal static void NextSong()
        {
            // In case we have no songs in the playlist...
            if (Common.cachedInfos.Count == 0)
                return;

            Common.currentPos++;
            if (Common.currentPos > Common.cachedInfos.Count)
                Common.currentPos = 1;
        }

        internal static void PreviousSong()
        {
            // In case we have no songs in the playlist...
            if (Common.cachedInfos.Count == 0)
                return;

            Common.currentPos--;
            if (Common.currentPos <= 0)
                Common.currentPos = Common.cachedInfos.Count;
        }

        internal static void PromptForAddSong()
        {
            string path = InfoBoxInputColor.WriteInfoBoxInput(LanguageTools.GetLocalized("MEDIABOOM_APP_PLAYER_MUSICFILEPROMPT"));
            ScreenTools.CurrentScreen?.RequireRefresh();
            if (File.Exists(path))
            {
                long currentPos = Player.position;
                Common.populate = true;
                PopulateMusicFileInfo(path);
                Common.populate = true;
                PopulateMusicFileInfo(Common.CurrentCachedInfo?.MusicPath ?? "");
                PlaybackPositioningTools.SeekTo(MediaBoomCli.basolia, currentPos);
            }
            else
                InfoBoxModalColor.WriteInfoBoxModal(LanguageTools.GetLocalized("MEDIABOOM_APP_PLAYER_MUSICFILENOTFOUND"), path);
        }

        internal static void PromptForAddSongs()
        {
            string path = InfoBoxInputColor.WriteInfoBoxInput(LanguageTools.GetLocalized("MEDIABOOM_APP_PLAYER_MUSICPLAYLISTPROMPT"));
            string extension = Path.GetExtension(path);
            ScreenTools.CurrentScreen?.RequireRefresh();
            if (File.Exists(path) && (extension == ".m3u" || extension == ".m3u8"))
            {
                long currentPos = Player.position;
                var playlist = PlaylistParser.ParsePlaylist(path);
                if (playlist.Tracks.Length > 0)
                {
                    foreach (var track in playlist.Tracks)
                    {
                        if (track.Type == SongType.File)
                        {
                            Common.populate = true;
                            PopulateMusicFileInfo(track.Path);
                        }
                    }
                    Common.populate = true;
                    PopulateMusicFileInfo(Common.CurrentCachedInfo?.MusicPath ?? "");
                    PlaybackPositioningTools.SeekTo(MediaBoomCli.basolia, currentPos);
                }
            }
            else
                InfoBoxModalColor.WriteInfoBoxModal(LanguageTools.GetLocalized("MEDIABOOM_APP_PLAYER_MUSICPLAYLISTNOTFOUND"));
        }

        internal static void PromptForAddDirectory()
        {
            string path = InfoBoxInputColor.WriteInfoBoxInput(LanguageTools.GetLocalized("MEDIABOOM_APP_PLAYER_MUSICLIBRARYPROMPT"));
            ScreenTools.CurrentScreen?.RequireRefresh();
            if (Directory.Exists(path))
            {
                long currentPos = Player.position;
                var cachedInfos = Directory.EnumerateFiles(path).Where((pathStr) => FileTools.SupportedExtensions.Contains(Path.GetExtension(pathStr))).ToArray();
                if (cachedInfos.Length > 0)
                {
                    foreach (string musicFile in cachedInfos)
                    {
                        Common.populate = true;
                        PopulateMusicFileInfo(musicFile);
                    }
                    Common.populate = true;
                    PopulateMusicFileInfo(Common.CurrentCachedInfo?.MusicPath ?? "");
                    PlaybackPositioningTools.SeekTo(MediaBoomCli.basolia, currentPos);
                }
            }
            else
                InfoBoxModalColor.WriteInfoBoxModal(LanguageTools.GetLocalized("MEDIABOOM_APP_PLAYER_MUSICLIBRARYNOTFOUND"));
        }

        internal static void PopulateMusicFileInfo(string musicPath)
        {
            // Try to open the file after loading the library
            if (PlaybackTools.IsPlaying(MediaBoomCli.basolia) || !Common.populate)
                return;
            Common.populate = false;
            Common.Switch(musicPath);
            if (!Common.cachedInfos.Any((csi) => csi.MusicPath == musicPath))
            {
                ScreenTools.CurrentScreen?.RequireRefresh();
                InfoBoxNonModalColor.WriteInfoBox($"Opening {musicPath}...", false);
                var total = AudioInfoTools.GetDuration(MediaBoomCli.basolia);
                string title = MpvPropertyHandler.GetStringProperty(MediaBoomCli.basolia, "media-title");
                string count = MpvPropertyHandler.GetStringProperty(MediaBoomCli.basolia, "metadata/list/count");

                // Try to open the lyrics
                var lyric = OpenLyrics(musicPath);
                var instance = new CachedSongInfo(musicPath, total, lyric, "", false, new(title, ""));
                Common.cachedInfos.Add(instance);
            }
        }

        internal static string RenderSongName(string musicPath)
        {
            // Render the song name
            var (musicName, musicArtist) = GetMusicNameArtist(musicPath);

            // Print the music name
            return $"Now playing: {musicArtist} - {musicName}";
        }

        internal static (string musicName, string musicArtist) GetMusicNameArtist(string musicPath)
        {
            if (Common.CurrentCachedInfo is null)
                return ("", "");
            var metadata = Common.CurrentCachedInfo.Metadata;
            string musicName =
                (!string.IsNullOrEmpty(metadata?.Title) ? metadata?.Title :
                 Path.GetFileNameWithoutExtension(musicPath)) ?? "";
            string musicArtist =
                (!string.IsNullOrEmpty(metadata?.Artist) ? metadata?.Artist :
                 LanguageTools.GetLocalized("MEDIABOOM_APP_PLAYER_UNKNOWNARTIST")) ?? "";
            return (musicName, musicArtist);
        }

        internal static (string musicName, string musicArtist) GetMusicNameArtist(int cachedInfoIdx)
        {
            var cachedInfo = Common.cachedInfos[cachedInfoIdx];
            var metadata = cachedInfo.Metadata;
            var path = cachedInfo.MusicPath;
            string musicName =
                (!string.IsNullOrEmpty(metadata?.Title) ? metadata?.Title :
                 Path.GetFileNameWithoutExtension(path)) ?? "";
            string musicArtist =
                (!string.IsNullOrEmpty(metadata?.Artist) ? metadata?.Artist :
                 LanguageTools.GetLocalized("MEDIABOOM_APP_PLAYER_UNKNOWNARTIST")) ?? "";
            return (musicName, musicArtist);
        }

        internal static Lyric? OpenLyrics(string musicPath)
        {
            string lyricsPath = Path.GetDirectoryName(musicPath) + "/" + Path.GetFileNameWithoutExtension(musicPath) + ".lrc";
            try
            {
                InfoBoxNonModalColor.WriteInfoBox(LanguageTools.GetLocalized("MEDIABOOM_APP_PLAYER_OPENINGMUSICLYRICFILE"), lyricsPath);
                if (File.Exists(lyricsPath))
                    return LyricReader.GetLyrics(lyricsPath);
                else
                    return null;
            }
            catch (Exception ex)
            {
                InfoBoxModalColor.WriteInfoBoxModal(LanguageTools.GetLocalized("MEDIABOOM_APP_PLAYER_OPENINGMUSICLYRICFILEFAILED") + $" {ex.Message}", lyricsPath);
            }
            return null;
        }

        internal static void RemoveCurrentSong()
        {
            // In case we have no songs in the playlist...
            if (Common.cachedInfos.Count == 0)
                return;
            if (Common.CurrentCachedInfo is null)
                return;

            Common.cachedInfos.RemoveAt(Common.currentPos - 1);
            if (Common.cachedInfos.Count > 0)
            {
                Common.currentPos--;
                if (Common.currentPos == 0)
                    Common.currentPos = 1;
                Common.populate = true;
                PopulateMusicFileInfo(Common.CurrentCachedInfo.MusicPath);
            }
        }

        internal static void RemoveAllSongs()
        {
            // In case we have no songs in the playlist...
            if (Common.cachedInfos.Count == 0)
                return;

            for (int i = Common.cachedInfos.Count; i > 0; i--)
                RemoveCurrentSong();
        }

        internal static void PromptSeek()
        {
            // In case we have no songs in the playlist...
            if (Common.cachedInfos.Count == 0)
                return;
            if (Common.CurrentCachedInfo is null)
                return;

            // Prompt the user to set the current position to the specified time
            string time = InfoBoxInputColor.WriteInfoBoxInput(LanguageTools.GetLocalized("MEDIABOOM_APP_PLAYER_TARGETPOSPROMPT") + " HH:MM:SS");
            if (TimeSpan.TryParse(time, out TimeSpan duration))
            {
                Player.position = (int)duration.TotalSeconds;
                if (Player.position > Common.CurrentCachedInfo.Duration)
                    Player.position = Common.CurrentCachedInfo.Duration;
                PlaybackPositioningTools.SeekTo(MediaBoomCli.basolia, Player.position);
            }
        }

        internal static void ShowSongInfo()
        {
            if (Common.CurrentCachedInfo is null)
                return;
            var metadata = Common.CurrentCachedInfo.Metadata;
            if (metadata is null)
                return;
            InfoBoxModalColor.WriteInfoBoxModal(
                LanguageTools.GetLocalized("BASSBOOM_APP_PLAYER_INFO_SONGINFO") + "\n\n" +
                LanguageTools.GetLocalized("BASSBOOM_APP_PLAYER_INFO_SONGINFO_ARTIST") + $" {(!string.IsNullOrEmpty(metadata.Artist) ? metadata.Artist : LanguageTools.GetLocalized("MEDIABOOM_APP_PLAYER_INFO_UNKNOWN"))}" + "\n" +
                LanguageTools.GetLocalized("BASSBOOM_APP_PLAYER_INFO_SONGINFO_TITLE") + $" {(!string.IsNullOrEmpty(metadata.Title) ? metadata.Title : "")}" + "\n" +
                LanguageTools.GetLocalized("BASSBOOM_APP_PLAYER_INFO_SONGINFO_DURATION") + $" {Common.CurrentCachedInfo.DurationSpan}" + "\n" +
                LanguageTools.GetLocalized("BASSBOOM_APP_PLAYER_INFO_SONGINFO_LYRICS") + $" {(Common.CurrentCachedInfo.LyricInstance is not null ? LanguageTools.GetLocalized("MEDIABOOM_APP_PLAYER_INFO_SONGINFO_LYRICS_LINES").FormatString(Common.CurrentCachedInfo.LyricInstance.Lines.Count) : LanguageTools.GetLocalized("MEDIABOOM_APP_PLAYER_INFO_SONGINFO_LYRICS_NOLYRICS"))}"
            );
        }
    }
}
