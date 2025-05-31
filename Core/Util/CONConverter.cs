using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
//using System.Windows.Forms;
using Un4seen.Bass;
using Nautilus;
using Nautilus.x360;

namespace Chord
{
    public class CONConverter
    {
        private readonly PhaseShiftSong Song;
        private readonly DTAParser Parser;
        private readonly NemoTools Tools;
        private readonly Action<string> Status;
        public CONConverter(Action<string> status)
        {
            Song = new PhaseShiftSong();
            Tools = new NemoTools();
            Parser = new DTAParser();
            Status = status;
        }
        public void Log(string message)
        {
            Status.Invoke(message);
            Console.WriteLine(message);
        }
        public void Convert(string file, string outDir)
        {
            if (VariousFunctions.ReadFileType(file) != XboxFileType.STFS) return;
            Song.NewSong();
            Song.ReplaceGenreWithSub = false;
            if (!Directory.Exists(outDir))
            {
                Directory.CreateDirectory(outDir);
            }
            if (!outDir.EndsWith("\\")) outDir += "\\";

            Parser.ExtractDTA(file);
            Parser.ReadDTA(Parser.DTA);
            if (Parser.Songs.Count > 1)
            {
                Log("File " + Path.GetFileName(file) + " is a pack, try dePACKing first, skipping...");
                return;
            }
            if (!Parser.Songs.Any())
            {
                Log("There was an error processing the songs.dta file");
                return;
            }
            uint TitleID;
            var xCON = new STFSPackage(file);
            if (xCON.ParseSuccess)
            {
                TitleID = xCON.Header.TitleID;
            }
            else
            {
                TitleID = 0;
            }
            xCON.CloseIO();
            if (loadDTA(TitleID))
            {
                Log("Loaded and processed songs.dta file for song successfully");
                Log("Song is " + Song.Artist + " - " + Song.Name);
            }
            else
            {
                return;
            }

            var internal_name = Parser.Songs[0].InternalName;

            var xPackage = new STFSPackage(file);
            if (!xPackage.ParseSuccess)
            {
                Log("Failed to parse '" + Path.GetFileName(file) + "'");
                Log("Skipping this file");
                return;
            }
            var xArt = xPackage.GetFile("songs/" + internal_name + "/gen/" + internal_name + "_keep.png_xbox");
            if (xArt != null)
            {
                var newart = Path.Combine(outDir, "album.png_xbox");
                if (xArt.ExtractToFile(newart))
                {
                    Log("Extracted album art file " + internal_name + "_keep.png_xbox successfully");
                    fromXbox(newart);
                }
                else
                {
                    Log("There was a problem extracting the album art file");
                }
            }
            else
            {
                Log("WARNING: Did not find album art file in that CON file");
            }
            var xMIDI = xPackage.GetFile("songs/" + internal_name + "/" + internal_name + ".mid");
            if (xMIDI != null)
            {
                var newmidi = Path.Combine(outDir, "notes.mid");
                if (xMIDI.ExtractToFile(newmidi))
                {
                    Log("Extracted MIDI file " + internal_name + ".mid successfully");
                    ProcessMidi(newmidi);
                }
                else
                {
                    Log("There was a problem extracting the MIDI file");
                    Log("Skipping this song...");
                    xPackage.CloseIO();
                    return;
                }
            }
            else
            {
                Log("ERROR: Did not find a MIDI file in that CON file!");
                Log("Skipping this song...");
                xPackage.CloseIO();
                return;
            }
            var xMOGG = xPackage.GetFile("songs/" + internal_name + "/" + internal_name + ".mogg");
            if (xMOGG != null)
            {
                var newMogg = Path.Combine(outDir, internal_name + ".mogg");
                xPackage.CloseIO();
                SeparateAudio(file, newMogg, outDir);
            }
            else
            {
                Log("ERROR: Did not find an audio file in that CON file!");
                Log("Skipping this song...");
                xPackage.CloseIO();
                return;
            }
            xPackage.CloseIO();

            Song.WriteINIFile(outDir, false);
        }

        private void SeparateAudio(string CON, string mogg, string folder)
        {
            Log("Separating mogg file into its component files");
            var Splitter = new MoggSplitter();
            var split = Splitter.SplitMogg(CON, folder, "allstems|rhythm|song", MoggSplitter.MoggSplitFormat.OGG);
            if (!split)
            {
                foreach (var error in Splitter.ErrorLog)
                {
                    Log(error);
                }
                Log("Failed...will try to downmix");
                DownMixAudio(CON, folder);
                return;
            }
            FinishAudioSeparation(mogg, folder);
        }

        private void FinishAudioSeparation(string mogg, string folder)
        {
            var oggs = Directory.GetFiles(folder, "*.ogg");
            if (!oggs.Any())
            {
                Log("Failed");
                return;
            }
            Log("Success");
            Tools.DeleteFile(mogg);
            AnalyzeAudioFiles(oggs);
        }

        private void DownMixAudio(string CON, string folder)
        {
            var ogg = folder + "song.ogg";
            Log("Downmixing audio file to stereo file:");
            Log(ogg);
            var Splitter = new MoggSplitter();
            var mixed = Splitter.DownmixMogg(CON, ogg, MoggSplitter.MoggSplitFormat.OGG);
            foreach (var error in Splitter.ErrorLog)
            {
                Log(error);
            }
            Log(mixed && File.Exists(ogg) ? "Success" : "Failed");
        }

        private bool BassInit = false;
        private void AnalyzeAudioFiles(IEnumerable<string> audioFiles)
        {
            Log("Analyzing audio files and deleting silent files");
            if (!BassInit)
            {
                Bass.BASS_Init(-1, 44100, BASSInit.BASS_DEVICE_DEFAULT, IntPtr.Zero);
                BassInit = true;
            }

            foreach (var audioFile in audioFiles)
            {
                var BassStream = Bass.BASS_StreamCreateFile(audioFile, 0, 0, BASSFlag.BASS_STREAM_DECODE);
                if (BassStream == 0) break;
                var level = new float[1];
                while (Bass.BASS_ChannelGetLevel(BassStream, level, 1, BASSLevel.BASS_LEVEL_MONO))
                {
                    if (level[0] != 0) break;
                }
                Bass.BASS_StreamFree(BassStream);
                if (level[0] == 0)
                {
                    Tools.DeleteFile(audioFile);
                }
            }
            Bass.BASS_Free();
        }
        private void ProcessMidi(string midifile)
        {
            var songMidi = Tools.NemoLoadMIDI(midifile);
            if (songMidi == null)
            {
                Log("Error parsing MIDI file ... can't analyze contents");
                return;
            }
            for (var i = 0; i < songMidi.Events.Tracks; i++)
            {
                if (Tools.GetMidiTrackName(songMidi.Events[i][0].ToString()).ToLowerInvariant().Contains("real_guitar_22"))
                {
                    Song.ProGuitar22 = true;
                }
                else if (Tools.GetMidiTrackName(songMidi.Events[i][0].ToString()).ToLowerInvariant().Contains("real_bass_22"))
                {
                    Song.ProBass22 = true;
                }
            }
        }

        private void fromXbox(string image)
        {
            var pngfile = Path.GetDirectoryName(image) + "\\album.png";
            try
            {
                Log(Tools.ConvertRBImage(image, pngfile, "png", true) ? "Converted album art file to 'album.png' successfully" : "There was an error when converting the album art file");
            }
            catch (Exception ex)
            {
                Log("There was an error when converting the album art file");
                Log("The error says: " + ex.Message);
                throw new Exception("Something went wrong when trying to convert the album art file\nfrom the native Rock Band format\nSorry\n\nThe message says: " + ex.Message);
            }
        }
        public bool loadDTA(uint TitleID)
        {
            try
            {
                Song.Name = Parser.Songs[0].Name;
                Song.Artist = Parser.Songs[0].Artist;
                if (string.IsNullOrEmpty(Song.Artist) && (TitleID == 0x454108B1 || TitleID == (uint)4294838225)) //for TBRB customs
                {
                    Song.Artist = "The Beatles";
                }
                Song.Album = Parser.Songs[0].Album;
                Song.Year = Parser.Songs[0].YearReleased;
                Song.HopoThreshold = Parser.Songs[0].HopoThreshold;
                Song.DrumKit = Parser.GetDrumKit(Parser.Songs[0].DrumBank);
                Song.TrackNumber = Parser.Songs[0].TrackNumber;
                switch (Parser.Songs[0].VocalParts)
                {
                    case 0:
                        {
                            Song.DiffVocals = -1;
                            Song.HasHarmonies = false;
                        }
                        break;
                    case 1:
                        {
                            Song.HasHarmonies = false;
                        }
                        break;
                    case 2:
                        {
                            Song.HasHarmonies = true;
                        }
                        break;
                    case 3:
                        {
                            Song.HasHarmonies = true;
                        }
                        break;
                    default:
                        {
                            Song.DiffVocals = -1;
                            Song.HasHarmonies = false;
                        }
                        break;
                }
                Song.DiffDrums = Parser.Songs[0].DrumsDiff - 1;
                Song.DiffBand = Parser.Songs[0].BandDiff - 1;
                Song.DiffBass = Parser.Songs[0].BassDiff - 1;
                Song.DiffGuitar = Parser.Songs[0].GuitarDiff - 1;
                Song.DiffKeys = Parser.Songs[0].KeysDiff - 1;
                Song.DiffProKeys = Parser.Songs[0].DisableProKeys ? -1 : Parser.Songs[0].ProKeysDiff - 1;
                Song.DiffProBass = Parser.Songs[0].ProBassDiff - 1;
                Song.DiffProGuitar = Parser.Songs[0].ProGuitarDiff - 1;
                Song.DiffVocals = Parser.Songs[0].VocalsDiff - 1;
                Song.PreviewStart = Parser.Songs[0].PreviewStart;
                Song.Length = Parser.Songs[0].Length;
                Song.Genre = Parser.Songs[0].Genre;
                Song.SubGenre = Parser.Songs[0].SubGenre;
                Song.BassTuning = Parser.Songs[0].ProBassTuning;
                Song.GuitarTuning = Parser.Songs[0].ProGuitarTuning;
                Song.Charter = Parser.Songs[0].ChartAuthor;
                Song.RhythmOnBass = Parser.Songs[0].RhythmBass;
                Song.RhythmOnKeys = Parser.Songs[0].RhythmKeys;
                return true;
            }
            catch (Exception ex)
            {
                Log("There was an error processing that songs.dta file");
                Log("The error says: " + ex.Message);
                return false;
            }
        }

        public class PhaseShiftSong
        {
            public string Name { get; set; }
            public string Artist { get; set; }
            public string Album { get; set; }
            public string LoadingPhrase { get; set; }
            public string Genre { get; set; }
            public string SubGenre { get; set; }
            public int Year { get; set; }
            public string Charter { get; set; }

            public string GuitarTuning { get; set; }
            public string BassTuning { get; set; }

            public bool RhythmOnKeys { get; set; }
            public bool RhythmOnBass { get; set; }
            public bool ProGuitar22 { get; set; }
            public bool ProBass22 { get; set; }
            public bool HasHarmonies { get; set; }
            public bool ReplaceGenreWithSub { get; set; }
            public long Length { get; set; }
            public long PreviewStart { get; set; }

            public int DrumKit { get; set; }
            public int HopoThreshold { get; set; }

            //Valid values = 0-6
            //-1 is disabled
            public int DiffDrums { get; set; }
            public int DiffBass { get; set; }
            public int DiffProBass { get; set; }
            public int DiffGuitar { get; set; }
            public int DiffProGuitar { get; set; }
            public int DiffKeys { get; set; }
            public int DiffProKeys { get; set; }
            public int DiffVocals { get; set; }
            public int DiffBand { get; set; }
            public int TrackNumber { get; set; }

            public string icon { get; set; }


            /// <summary>
            /// Sets all the song's values to defaults
            /// </summary>
            public void NewSong()
            {
                Name = "";
                Artist = "";
                Album = "";
                LoadingPhrase = "";
                icon = "";
                Genre = "";
                Year = -1;
                Charter = "";
                DiffDrums = -1;
                DiffBass = -1;
                DiffProBass = -1;
                DiffGuitar = -1;
                DiffProGuitar = -1;
                DiffKeys = -1;
                DiffProKeys = -1;
                DiffVocals = -1;
                DiffBand = -1;
                RhythmOnBass = false;
                RhythmOnKeys = false;
                ProGuitar22 = false;
                ProBass22 = false;
                HasHarmonies = false;
                Length = -1;
                PreviewStart = -1;
                GuitarTuning = "";
                BassTuning = "";
                DrumKit = -1;
                HopoThreshold = -1;
                ReplaceGenreWithSub = false;
                TrackNumber = 0;
            }

            /// <summary>
            /// Outputs song.ini file with all the information currently attached to that song
            /// </summary>
            /// <param name="output_folder">Folder path where the song.ini file should be created</param>
            /// <returns></returns>
            public bool WriteINIFile(string output_folder, bool addHopoThreshold)
            {
                var ini = output_folder + "\\song.ini";
                var ps = "\\loading_phrase.txt";

                if (File.Exists(ps))
                {
                    try
                    {
                        var sr = new StreamReader(ps, Encoding.Default);
                        LoadingPhrase = sr.ReadLine();
                        sr.Dispose();
                    }
                    catch (Exception)
                    {
                        LoadingPhrase = "";
                    }
                }
                try
                {
                    if (File.Exists(ini))
                    {
                        File.Delete(ini);
                    }
                }
                catch (Exception)
                { }
                try
                {
                    var sw = new StreamWriter(ini, false, Encoding.Default);
                    sw.WriteLine("[song]");
                    sw.WriteLine("delay = 0"); //is this necessary?
                    sw.WriteLine("multiplier_note = 116"); //is this necessary?
                    sw.WriteLine("artist = " + Artist);
                    sw.WriteLine("name = " + Name);
                    if (Album != "")
                    {
                        sw.WriteLine("album = " + Album);
                    }
                    if (TrackNumber > 0)
                    {
                        sw.WriteLine("track = " + TrackNumber);
                    }
                    if (Year > -1)
                    {
                        sw.WriteLine("year = " + Year);
                    }
                    if (Genre != "")
                    {
                        sw.WriteLine("genre = " + ((ReplaceGenreWithSub && SubGenre != "") ? SubGenre : Genre));
                    }
                    if (DiffDrums > -1)
                    {
                        sw.WriteLine("pro_drums = True"); //always true for RB3 content

                        if (DrumKit > -1)
                        {
                            sw.WriteLine("kit_type = " + DrumKit);
                        }
                    }
                    sw.WriteLine("diff_drums = " + DiffDrums);
                    sw.WriteLine("diff_drums_real = " + DiffDrums);
                    var DiffRhythm = -1;
                    if (RhythmOnBass)
                    {
                        DiffRhythm = DiffBass;
                        DiffBass = -1;
                        DiffProBass = -1;
                        ProBass22 = false;
                    }
                    sw.WriteLine("diff_bass = " + DiffBass);
                    sw.WriteLine("diff_bass_real = " + DiffProBass);
                    if (ProBass22)
                    {
                        sw.WriteLine("diff_bass_real_22 = " + DiffProBass);
                    }
                    else
                    {
                        sw.WriteLine("diff_bass_real_22 = -1");
                    }
                    if (BassTuning != "")
                    {
                        sw.WriteLine("real_bass_tuning = " + BassTuning);
                    }
                    sw.WriteLine("diff_rhythm = " + DiffRhythm);
                    sw.WriteLine("diff_guitar = " + DiffGuitar);
                    sw.WriteLine("diff_guitar_real = " + DiffProGuitar);
                    if (ProGuitar22)
                    {
                        sw.WriteLine("diff_guitar_real_22 = " + DiffProGuitar);
                    }
                    else
                    {
                        sw.WriteLine("diff_guitar_real_22 = -1");
                    }
                    if (GuitarTuning != "")
                    {
                        sw.WriteLine("real_guitar_tuning = " + GuitarTuning);
                    }
                    var DiffCoop = -1;
                    if (RhythmOnKeys)
                    {
                        DiffCoop = DiffKeys;
                        DiffKeys = -1;
                        DiffProKeys = -1;
                    }
                    sw.WriteLine("diff_keys = " + DiffKeys);
                    sw.WriteLine("diff_keys_real = " + DiffProKeys);
                    sw.WriteLine("diff_guitar_coop = " + DiffCoop);
                    sw.WriteLine("diff_vocals = " + DiffVocals);
                    if (HasHarmonies)
                    {
                        sw.WriteLine("diff_vocals_harm = " + DiffVocals);
                    }
                    else
                    {
                        sw.WriteLine("diff_vocals_harm = -1");
                    }
                    sw.WriteLine("diff_band = " + DiffBand);
                    if (HopoThreshold > -1 && addHopoThreshold)
                    {
                        sw.WriteLine("hopo_frequency = " + HopoThreshold);
                    }
                    if (PreviewStart > 0)
                    {
                        sw.WriteLine("preview_start_time = " + PreviewStart);
                    }
                    if (Length > 0)
                    {
                        sw.WriteLine("song_length = " + Length);
                    }
                    sw.WriteLine("charter = " + Charter);
                    sw.WriteLine("loading_phrase = " + LoadingPhrase);
                    sw.WriteLine("icon = " + icon);
                    //these values will always be -1 for RB conversions but add them
                    //makes it clearer in the interface that these instruments are disabled
                    sw.WriteLine("diff_drums_real_ps = -1");
                    sw.WriteLine("diff_keys_real_ps = -1");
                    sw.WriteLine("diff_dance = -1");
                    sw.Dispose();
                    return true;
                }
                catch (Exception)
                {
                    return false;
                }
            }
        }
    }
}
