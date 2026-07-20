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

using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using MediaBoom.Basolia.Exceptions;
using MediaBoom.Basolia.Media.Playback;
using MediaBoom.Basolia.Media.Playback.Playlists;
using MediaBoom.Basolia.Media.Playback.Playlists.Enumerations;
using MediaBoom.Basolia.Media.Radio;
using MediaBoom.Cli.Languages;
using MediaBoom.Cli.Tools;
using MediaBoom.Native.Interop.Enumerations;
using Terminaux.Base.Buffered;
using Terminaux.Inputs.Styles.Infobox;
using Textify.General;

namespace MediaBoom.Cli.CliBase
{
    internal static class RadioControls
    {
        internal static void Play()
        {
            // In case we have no stations in the playlist...
            if (Common.cachedInfos.Count == 0)
                return;
            if (Radio.playerThread is null)
                return;

            // There could be a chance that the music has fully stopped without any user interaction, but since we're on
            // a radio station, we should seek nothing.
            if (MediaBoomCli.basolia is null)
                throw new BasoliaException(LanguageTools.GetLocalized("MEDIABOOM_BASOLIA_EXCEPTION_BASOLIAMEDIA"), MpvError.MPV_ERROR_GENERIC);

            // Start the player thread
            Common.advance = true;
            if (!Radio.playerThread.IsAlive)
                Radio.playerThread.Regen();
            Radio.playerThread.Start();

            // Wait until radio is really playing
            SpinWait.SpinUntil(() => MediaBoomCli.basolia.IsPlaying() || Common.failedToPlay);
            Common.failedToPlay = false;
        }

        internal static void Pause()
        {
            if (MediaBoomCli.basolia is null)
                throw new BasoliaException(LanguageTools.GetLocalized("MEDIABOOM_BASOLIA_EXCEPTION_BASOLIAMEDIA"), MpvError.MPV_ERROR_GENERIC);
            Common.advance = false;
            Common.paused = true;
            MediaBoomCli.basolia.Pause();
        }

        internal static void Stop(bool resetCurrentStation = true)
        {
            if (MediaBoomCli.basolia is null)
                throw new BasoliaException(LanguageTools.GetLocalized("MEDIABOOM_BASOLIA_EXCEPTION_BASOLIAMEDIA"), MpvError.MPV_ERROR_GENERIC);
            Common.advance = false;
            Common.paused = false;
            if (resetCurrentStation)
                Common.currentPos = 1;
            MediaBoomCli.basolia.Stop();
        }

        internal static void NextStation()
        {
            // In case we have no stations in the playlist...
            if (Common.cachedInfos.Count == 0)
                return;

            if (MediaBoomCli.basolia is null)
                throw new BasoliaException(LanguageTools.GetLocalized("MEDIABOOM_BASOLIA_EXCEPTION_BASOLIAMEDIA"), MpvError.MPV_ERROR_GENERIC);
            MediaBoomCli.basolia.Stop();
            Common.currentPos++;
            if (Common.currentPos > Common.cachedInfos.Count)
                Common.currentPos = 1;
        }

        internal static void PreviousStation()
        {
            // In case we have no stations in the playlist...
            if (Common.cachedInfos.Count == 0)
                return;

            if (MediaBoomCli.basolia is null)
                throw new BasoliaException(LanguageTools.GetLocalized("MEDIABOOM_BASOLIA_EXCEPTION_BASOLIAMEDIA"), MpvError.MPV_ERROR_GENERIC);
            MediaBoomCli.basolia.Stop();
            Common.currentPos--;
            if (Common.currentPos <= 0)
                Common.currentPos = Common.cachedInfos.Count;
        }

        internal static void PromptForAddStation()
        {
            string path = InfoBoxInputColor.WriteInfoBoxInput(LanguageTools.GetLocalized("MEDIABOOM_APP_RADIO_STATIONPROMPT"));
            ScreenTools.CurrentScreen?.RequireRefresh();
            if (string.IsNullOrEmpty(path))
                return;
            Common.populate = true;
            PopulateRadioStationInfo(path);
            Common.populate = true;
            PopulateRadioStationInfo(Common.CurrentCachedInfo?.MusicPath ?? "");
        }

        internal static void PromptForAddStations()
        {
            string path = InfoBoxInputColor.WriteInfoBoxInput(LanguageTools.GetLocalized("MEDIABOOM_APP_RADIO_STATIONGROUPPROMPT"));
            ScreenTools.CurrentScreen?.RequireRefresh();
            if (string.IsNullOrEmpty(path))
                return;
            string extension = Path.GetExtension(path);
            if (File.Exists(path) && (extension == ".m3u" || extension == ".m3u8"))
            {
                var playlist = PlaylistParser.ParsePlaylist(path);
                if (playlist.Tracks.Length > 0)
                {
                    foreach (var track in playlist.Tracks)
                    {
                        if (track.Type == SongType.Radio)
                        {
                            Common.populate = true;
                            PopulateRadioStationInfo(track.Path);
                        }
                    }
                    Common.populate = true;
                    PopulateRadioStationInfo(Common.CurrentCachedInfo?.MusicPath ?? "");
                }
            }
            else
                InfoBoxModalColor.WriteInfoBoxModal(LanguageTools.GetLocalized("MEDIABOOM_APP_RADIO_STATIONGROUPNOTFOUND"));
        }

        internal static void PopulateRadioStationInfo(string musicPath)
        {
            // Try to open the file after loading the library
            if (MediaBoomCli.basolia is null)
                throw new BasoliaException(LanguageTools.GetLocalized("MEDIABOOM_BASOLIA_EXCEPTION_BASOLIAMEDIA"), MpvError.MPV_ERROR_GENERIC);
            if (MediaBoomCli.basolia.IsPlaying() || !Common.populate)
                return;
            Common.Switch(musicPath);
            Common.populate = false;
            if (!Common.cachedInfos.Any((csi) => csi.MusicPath == musicPath))
            {
                InfoBoxNonModalColor.WriteInfoBox(LanguageTools.GetLocalized("MEDIABOOM_APP_PLAYER_OPENINGMUSICFILE"), musicPath);

                // Try to open the lyrics
                var instance = new CachedSongInfo(musicPath, -1, null, MediaBoomCli.basolia.CurrentFile()?.StationName ?? "", true, null);
                Common.cachedInfos.Add(instance);
            }
        }

        internal static string RenderStationName()
        {
            if (MediaBoomCli.basolia is null)
                throw new BasoliaException(LanguageTools.GetLocalized("MEDIABOOM_BASOLIA_EXCEPTION_BASOLIAMEDIA"), MpvError.MPV_ERROR_GENERIC);

            // Render the station name
            string icy = MediaBoomCli.basolia.GetRadioNowPlaying();

            // Print the music name
            return LanguageTools.GetLocalized("MEDIABOOM_APP_PLAYER_NOWPLAYING") + $" {icy}";
        }

        internal static void RemoveCurrentStation()
        {
            // In case we have no stations in the playlist...
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
                PopulateRadioStationInfo(Common.CurrentCachedInfo.MusicPath);
            }
        }

        internal static void RemoveAllStations()
        {
            // In case we have no stations in the playlist...
            if (Common.cachedInfos.Count == 0)
                return;

            for (int i = Common.cachedInfos.Count; i > 0; i--)
                RemoveCurrentStation();
        }

        internal static void ShowStationInfo()
        {
            if (Common.CurrentCachedInfo is null)
                return;
            if (MediaBoomCli.basolia is null)
                throw new BasoliaException(LanguageTools.GetLocalized("MEDIABOOM_BASOLIA_EXCEPTION_BASOLIAMEDIA"), MpvError.MPV_ERROR_GENERIC);
            InfoBoxModalColor.WriteInfoBoxModal(
                LanguageTools.GetLocalized("MEDIABOOM_APP_RADIO_INFO_STATIONINFO") + "\n\n" +
                LanguageTools.GetLocalized("MEDIABOOM_APP_RADIO_INFO_STATIONINFO_URL") + $" {Common.CurrentCachedInfo.MusicPath}" + "\n" +
                LanguageTools.GetLocalized("MEDIABOOM_APP_RADIO_INFO_STATIONINFO_NAME") + $" {Common.CurrentCachedInfo.StationName}" + "\n" +
                LanguageTools.GetLocalized("MEDIABOOM_APP_RADIO_INFO_STATIONINFO_CURRSONG") + $" {MediaBoomCli.basolia.GetRadioNowPlaying()}"
            );
        }

        internal static void ShowExtendedStationInfo()
        {
            if (Common.CurrentCachedInfo is null)
                return;
            var station = RadioTools.GetRadioInfo(Common.CurrentCachedInfo.MusicPath);
            var streamBuilder = new StringBuilder();
            if (station is not null)
            {
                foreach (var stream in station.Streams)
                {
                    streamBuilder.AppendLine(LanguageTools.GetLocalized("MEDIABOOM_APP_RADIO_INFOEXT_STREAMINFO_NAME") + $" {stream.StreamTitle}");
                    streamBuilder.AppendLine("    " + LanguageTools.GetLocalized("MEDIABOOM_APP_RADIO_INFOEXT_STREAMINFO_HOMEPAGE") + $" {stream.StreamHomepage}");
                    streamBuilder.AppendLine("    " + LanguageTools.GetLocalized("MEDIABOOM_APP_PLAYER_INFO_SONGINFO_GENRE") + $" {stream.StreamGenre}");
                    streamBuilder.AppendLine("    " + LanguageTools.GetLocalized("MEDIABOOM_APP_PLAYER_NOWPLAYING") + $" {stream.SongTitle}");
                    streamBuilder.AppendLine("    " + LanguageTools.GetLocalized("MEDIABOOM_APP_RADIO_INFOEXT_STREAMINFO_STREAMPATH") + $" {stream.StreamPath}");
                    streamBuilder.AppendLine("    " + LanguageTools.GetLocalized("MEDIABOOM_APP_RADIO_INFOEXT_STREAMINFO_LISTENERS").FormatString(stream.CurrentListeners, stream.PeakListeners));
                    streamBuilder.AppendLine("    " + LanguageTools.GetLocalized("MEDIABOOM_APP_RADIO_INFOEXT_STREAMINFO_BITRATE") + $" {stream.BitRate} kbps");
                    streamBuilder.AppendLine("    " + LanguageTools.GetLocalized("MEDIABOOM_APP_RADIO_INFOEXT_STREAMINFO_MEDIATYPE") + $" {stream.MimeInfo}");
                    streamBuilder.AppendLine();
                }
                InfoBoxModalColor.WriteInfoBoxModal(
                    LanguageTools.GetLocalized("MEDIABOOM_APP_RADIO_INFOEXT_SERVERINFO") + "\n\n" +
                    LanguageTools.GetLocalized("MEDIABOOM_APP_RADIO_INFO_STATIONINFO_URL") + $" {station.ServerHostFull}" + "\n" +
                    LanguageTools.GetLocalized("MEDIABOOM_APP_RADIO_INFOEXT_SERVERINFO_HTTPS") + $" {station.ServerHttps}" + "\n" +
                    LanguageTools.GetLocalized("MEDIABOOM_APP_RADIO_INFOEXT_SERVERINFO_TYPE") + $" {station.ServerType}" + "\n" +
                    LanguageTools.GetLocalized("MEDIABOOM_APP_RADIO_INFOEXT_SERVERINFO_STREAMS").FormatString(station.TotalStreams, station.ActiveStreams) + "\n" +
                    LanguageTools.GetLocalized("MEDIABOOM_APP_RADIO_INFOEXT_SERVERINFO_LISTENERS").FormatString(station.CurrentListeners, station.PeakListeners) + "\n\n" +

                    LanguageTools.GetLocalized("MEDIABOOM_APP_RADIO_INFOEXT_STREAMINFO") + "\n\n" +
                    streamBuilder.ToString()
                );
            }
            else
                InfoBoxModalColor.WriteInfoBoxModal(LanguageTools.GetLocalized("MEDIABOOM_APP_RADIO_INFOEXT_UNABLETOOBTAIN").FormatString(Common.CurrentCachedInfo.MusicPath));
        }
    }
}
