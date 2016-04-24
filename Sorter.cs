using System;
using System.IO;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Windows.Forms;

namespace MusicSort
{
    public partial class Sorter : Form
    {
        private string[] extensions = new string[] { ".mp3", ".flac" };
        private List<string> CDs = new string[] { "cd1", "cd2", "cd3", "cd4", "cd 1", "cd 2", "cd 3", "cd 4", "disc1", "disc2", "disc3", "disc4", "disc 1", "disc 2", "disc 3", "disc 4",
                                "cd01", "cd02", "cd03", "cd04", "cd 01", "cd 02", "cd 03", "cd 04", "disc01", "disc02", "disc03", "disc04", "disc 01", "disc 02", "disc 03", "disc 04"}.ToList();
        private string[] CDreplace = new string[] { "cd 1", "cd 2", "cd 3", "cd 4", "cd 1", "cd 2", "cd 3", "cd 4", "cd 1", "cd 2", "cd 3", "cd 4", "cd 1", "cd 2", "cd 3", "cd 4",
                                "cd 1", "cd 2", "cd 3", "cd 4", "cd 1", "cd 2", "cd 3", "cd 4", "cd 1", "cd 2", "cd 3", "cd 4", "cd 1", "cd 2", "cd 3", "cd 4"};
        private string[] commonFolders = new string[] { "albums", "compilations", "remixes", "singles", "tracks", "demos", "doodles", "wip", "work in progress", "originals", "remakes", "eps", "downloads", "bonus disc", "bonus track", "discs", "cds" };
        private string[] subFolders = new string[] { "albums", "compilations", "remixes", "singles", "tracks", "demos", "doodles", "wip", "work in progress", "originals", "remakes", "eps", "downloads" };
        private string[] commonFoldersAsExtra = new string[] { "bonus" };
        private List<string> years = new string[] { "[1", "[2", "(1", "(2" }.ToList();
        private string[] yearsEnd = new string[] { "]", "]", ")", ")" };
        private string[] nums = new string[] { "1", "2", "3", "4", "5", "6", "7", "8", "9", "0" };
        private string[] trackNumEnd = new string[] { ".", "-", ")", "]" };
        private string[] replaceBadStart = new string[] { ".", "-", ")", "]", "}" };
        private string[] replaceBadEnd = new string[] { ".", "-", "(", "[", "{" };
        private string[] replaceBadStr = new string[] { "[]", "()", "{}", "[ ]", "( )", "{ }", "flac", "mp3" };
        private string[] subStart = new string[] { "(", "[", "{" };
        private string[] subEnd = new string[] { ")", "]", "}" };

        public Sorter()
        {
            InitializeComponent();
        }

        private void btnBrowse_Click(object sender, EventArgs e)
        {
            folderDialog.ShowDialog();
            txtFolder.Text = folderDialog.SelectedPath;
        }

        private void btnSearch_Click(object sender, EventArgs e)
        {
            //intelligently extract music details from folder & file structure
            progress.Maximum = 100;
            progress.Value = 0;
            progress.Visible = true;
            txtFolder.Visible = false;
            btnBrowse.Visible = false;
            btnSearch.Visible = false;
            Refresh();

            var files = GetFiles(txtFolder.Text, extensions.ToList());
            string[] p;
            var x = 0;
            var i = 0;
            var a = 0;
            var y = new int[4] { 0, 0, 0, 0 };
            var skip = 0;
            var inc = 0;
            var num = 0.0;
            var pathlen = txtFolder.Text.Split('\\').Length;
            var tempTrackNums = new int[] { 0, 0, 0 };
            var str = new string[] { };
            var orig = "";
            var yr = "";
            var dotspace = "";
            var clean = "";
            FileDetails file;
            FileDetails lastFile;
            var details = new List<FileDetails>();

            progress.Maximum = files.Count;

            foreach (string f in files)
            {
                p = f.ToLower().Replace("_", " ").Split('\\');
                x = p.Length;
                file = new FileDetails();
                file.extra = "";
                file.CD = "";
                file.year = "";
                file.subfolder = "";
                skip = 0;
                inc = 0;
                
                //get album folder ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
                skip = 0;
                while(skip < 3)
                {
                    a = x - 2 - skip;
                    dotspace = p[a].Replace(".", " ");
                    clean = dotspace.Replace("(", "").Replace("[", "").Replace(")", "").Replace("]","");
                    if (CDs.Count(c => dotspace.IndexOf(c) >= 0 && dotspace.IndexOf(c) <= 2) > 0)
                    {
                        //found folder for CD
                        file.CD = CDreplace[CDs.FindIndex(c => dotspace.IndexOf(c) >= 0)].ToUpper().Replace(" ","").Replace("DISC","CD").Trim();
                    }
                    else if (commonFolders.Count(c => clean.IndexOf(c) == 0) > 0)
                    {
                        //found a common folder
                        if(commonFoldersAsExtra.Count(c => clean.IndexOf(c) == 0) > 0)
                        {
                            file.extra = clean.Trim();
                        }
                        if (subFolders.Count(c => clean.IndexOf(c) == 0) > 0)
                        {
                            file.subfolder = clean.Trim();
                        }
                    }
                    else
                    {
                        //check if album is already found
                        if(file.album.Length == 0 && a > pathlen) {
                            //this must be an album folder
                            file.album = p[a].Trim();

                            //get year if it exists
                            yr = GetYear(file.album);
                            if (yr.Length > 0)
                            {
                                file.year = yr;
                                file.album = RemoveBadStrings(file.album.Replace(yr, ""));
                            }
                        }
                    }
                    skip++;
                }

                //get artist folder ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
                a = pathlen;
                while (a < p.Length - 1)
                {
                    if (commonFolders.Count(c => p[a].IndexOf(c) == 0) == 0)
                    {
                        //found artist folder
                        str = new string[] { "" };
                        str[0] = "";
                        for(i = 0; i <= a; i++)
                        {
                            str[0] += p[i] + "\\";
                        }
                        if(str[0].Length - txtFolder.Text.Length <= 2)
                        {
                            //went too far back, the previous folder was the artist folder
                            if(skip >= 1)
                            {
                                file.artist = p[a + 1];
                                if(file.album.IndexOf(file.artist) == 0)
                                {
                                    file.album = p[a + 2];

                                    //get year if it exists
                                    yr = GetYear(file.album);
                                    if (yr.Length > 0)
                                    {
                                        file.year = yr;
                                        file.album = RemoveBadStrings(file.album.Replace(yr, ""));
                                    }
                                }
                            }
                        }
                        else
                        {
                            file.artist = p[a];

                            //get year if it exists
                            yr = GetYear(file.artist);
                            if (yr.Length > 0 && file.year == "")
                            {
                                file.year = yr;
                                file.artist = RemoveBadStrings(file.artist.Replace(yr, ""));
                            }
                        }
                        
                        break;
                    }
                    a++;
                }
                file.artist = RemoveSubContent(file.artist);

                if(file.album.IndexOf(file.artist) == 0 && file.album.Length > file.artist.Length)
                {
                    //remove artist name from album
                    file.album = file.album.Substring(file.artist.Length);
                }
                file.album = RemoveBadStrings(file.album);

                //get track name /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
                file.filename = p[x - 1].Trim();
                file.folder = f.ToLower().Replace(file.filename, "");
                file.trackName = file.filename.Replace("_", " ");
                str = file.filename.Split('.');
                file.extension = str[str.Length - 1];

                //remove file extension from track name
                foreach (var ext in extensions)
                {
                    file.trackName = file.trackName.Replace(ext, "").Trim();
                }
                orig = file.trackName;

                //check for track number
                while (inc < 3)
                {
                    skip = 0;
                    a = 0;
                    y[3] = 0;
                    while (a <= 5)
                    {
                        if(file.trackName.Length <= a) { break; }
                        if (double.TryParse(file.trackName.Substring(a, 1), out num) == true)
                        {
                            i = file.trackName.ToList().FindIndex(a, c => nums.Count(n => n == c.ToString()) == 0);
                            if (i > 0)
                            {
                                //found set of numbers
                                if (skip == 1 && file.trackNumber > 0 && file.trackNumber <= 10)
                                {
                                    //found 2nd set of numbers, so 1st set must be CD #
                                    file.CD = "CD" + file.trackNumber.ToString();
                                }
                                file.trackNumber = int.Parse(file.trackName.Substring(a, i - a));
                                y[3] = i; //set up beginning of track name
                                if (skip == 1)
                                {
                                    //check to see if 2nd set of numbers is actually the beginning of the track name
                                    if (trackNumEnd.Count(c => file.trackName.IndexOf(c, i + 1) < i + 3) == 0 || file.trackNumber > 36 || i < file.trackName.Length - 3)
                                    {
                                        file.trackNumber = tempTrackNums[0];
                                        y[3] = tempTrackNums[2]; //rewind the beginning of track name
                                    }
                                    break;
                                }
                                else
                                {
                                    //remember track numbers just in case CD (2nd set of numbers) doesn't work out
                                    tempTrackNums[1] = tempTrackNums[0];
                                    tempTrackNums[0] = file.trackNumber;
                                }
                                //don't break just yet, look for a 2nd set of numbers
                                //because the first set might just be the CD #
                                a = i;
                                y[0] = i;
                                tempTrackNums[2] = i; //temp end index for 1st number set
                                skip++;
                            }
                        }
                        a++;
                    }

                    //get track name
                    if (y[3] > 0)
                    {
                        file.trackName = file.trackName.Substring(y[3]).Trim();
                    }
                    else
                    {
                        file.trackName = file.trackName.Trim();
                    }

                    //get year if it exists
                    yr = GetYear(file.trackName);
                    if (yr.Length > 0 && file.year == "")
                    {
                        file.year = yr;
                        file.trackName = RemoveBadStrings(file.trackName.Replace(yr, ""));
                    }

                    file.trackName = RemoveBadStrings(file.trackName);

                    //remove extra content from track name
                    if (file.trackName.IndexOf(" - ") > 0)
                    {
                        str = file.trackName.Split(new string[] { " - " }, StringSplitOptions.RemoveEmptyEntries);
                        if (str[0].IndexOf(file.artist) >= 0 || str[0].IndexOf(file.album) >= 0)
                        {
                            file.trackName = string.Join(" - ", str.Skip(1).ToArray());
                        }
                    }
                    //remove artist from trackname
                    if ((file.trackName.IndexOf(file.artist) == 0 || file.trackName.IndexOf(file.artist) == 1) && 
                        file.artist.Length > 0 && file.trackName.IndexOf("feat") < 0 && file.trackName.IndexOf("vs") < 0)
                    {
                        file.trackName = file.trackName.Split(new string[] { file.artist }, 2, StringSplitOptions.None)[1];
                    }
                    file.trackName = RemoveBadStrings(file.trackName);

                    //track name is empty, use original track name
                    if (file.trackName == "" && file.trackNumber > 0)
                    {
                        file.trackName = RemoveBadStrings(orig);
                        file.trackNumber = 0;
                    }else if(file.trackName == "")
                    {
                        file.trackName = RemoveBadStrings(orig);
                    }

                    //if there is no number at the beginning of the track, then exit loop
                    if (double.TryParse(file.trackName.Substring(0, 1), out num) == false) {
                        break;
                    }
                    else
                    {
                        if (trackNumEnd.Count(c => file.trackName.IndexOf(c) > 0 && file.trackName.IndexOf(c) < 4) == 0)
                        {
                            break;
                        }
                    }
                    inc++;
                }

                //check previous file for similar values
                if (details.Count > 0)
                {
                    lastFile = details[details.Count - 1];
                    if(lastFile.artist == file.artist && lastFile.album == file.album)
                    {
                        //previous file was part of the same artist / album
                        if(lastFile.CD == "" && file.CD != "")
                        {
                            //remove CD value since previous file doesn't use it
                            file.trackNumber = tempTrackNums[0];
                            file.CD = "";
                        }
                        else if(lastFile.CD != "" && file.CD == "")
                        {
                            //remove CD value for previous file since this file doesn't use it
                            lastFile.trackNumber = tempTrackNums[1];
                            lastFile.CD = "";
                            details[details.Count - 1] = lastFile;
                        }
                        else if(lastFile.CD != "" && file.CD != "")
                        {
                            //check for large skip in CD #
                            if(int.Parse(file.CD.Replace("CD","")) - int.Parse(lastFile.CD.Replace("CD", "")) > 1)
                            {
                                file.CD = lastFile.CD;
                            }
                        }
                    }
                }

                //build restructured file path
                file.renamed = GetCapitalized(file.artist + "\\" + (file.subfolder.Length > 0 ? file.subfolder + "\\" : "") +  // artist/sub-folder/
                                (file.album.Length > 0 ? (file.year != "" ? "(" + file.year + ") " : "") + file.album + (file.extra.Length > 0 ? " (" + file.extra + ")" : "") + "\\": "") + // (year) album (extra)
                                (file.trackNumber > 0 ? file.trackNumber.ToString("00") + " - " : "") + file.trackName) + "." + file.extension; // # track name.ext
                
                //finally, add file to list
                details.Add(file);

                progress.Value++;
                progress.Refresh();
            }

            dataFiles.AutoGenerateColumns = false;
            dataFiles.DataSource = details;

            progress.Visible = false;
            txtFolder.Visible = true;
            btnBrowse.Visible = true;
            btnSearch.Visible = true;
            panelMove.Visible = true;
        }

        private List<string> GetFiles(string folder, List<string> extensions)
        {
            List<string> files = new List<string>();
            try
            {
                foreach (string f in Directory.GetFiles(folder).Where(d => extensions.Count(e =>d.IndexOf(e) > 0) > 0).ToList())
                {
                    files.Add(f);
                }
                foreach (string d in Directory.GetDirectories(folder))
                {
                    files.AddRange(GetFiles(d, extensions));
                }
            }
            catch (Exception excpt)
            {
                MessageBox.Show(excpt.Message);
            }

            return files;
        }

        private string GetYear(string str)
        {
            var i = -1;
            var o = 0;
            var s = "";
            var result = "";
            for(var x = 0; x < str.Length; x++)
            {
                s = str.Substring(x, 1);
                if (int.TryParse(s, out o) == true)
                {
                    if(i == -1)
                    {
                        //found beginning of number
                        i = x;
                    }
                }
                else
                {
                    if(i >= 0)
                    {
                        //found end of number
                        o = int.Parse(str.Substring(i, x - i));
                        if(o < 1900 || o > DateTime.Now.Year + 1)
                        {
                            //not a believable date, keep searching
                            i = -1;
                        }
                        else
                        {
                            //found year, exit
                            result = o.ToString();
                            break;
                        }
                    }
                }
            }
            return result;
        }

        private string GetCapitalized(string str)
        {
            var s = "";
            var trig = true;
            char[] symbols = (" !@#$%^&*()[]{}\\|;:\",.<>/?~`-+=").ToCharArray();

            foreach (var c in str)
            {
                if(trig == true)
                {
                    if (!symbols.Contains(c))
                    {
                        //capitalize
                        s += c.ToString().ToUpper();
                    }
                    else
                    {
                        s += c.ToString();
                    }
                }
                else
                {
                    s += c.ToString();
                }
                trig = symbols.Contains(c);
            }
            return s;
        }

        private string RemoveBadStrings(string str)
        {
            var s = str;
            if (str.Length > 0)
            {
                foreach (var r in replaceBadStr)
                {
                    s = s.Replace(r, "");
                }
                s = s.Trim();
                if(s.Length > 0)
                {
                    //no brackets allowed
                    s = s.Replace("[", " (").Replace("]", ") ");

                    if (replaceBadStart.Count(c => s.IndexOf(c) == 0) >= 1)
                    {
                        s = s.Substring(1).Trim();
                    }
                }
                if (s.Length > 0)
                {
                    if (replaceBadEnd.Count(c => s.IndexOf(c) == s.Length - 1) >= 1)
                    {
                        s = s.Substring(0, s.Length - 2).Trim();
                    }
                }
            }
            if(s.Length > 0)
            {
                //replace double spaces
                s = s.Replace("  ", " ").Replace("  ", " ");
            }
            
            return s;
        }

        private string RemoveSubContent(string str)
        {
            var s = str.Trim();
            var i = 0;
            var e = 0;
            var changed = false;
            do
            {
                changed = false;
                for (var x = 0; x < subStart.Length; x++)
                {
                    i = s.IndexOf(subStart[x]);
                    if (i >= 0)
                    {
                        e = s.IndexOf(subEnd[x]);
                        if (e > 0)
                        {
                            s = s.Remove(i, e - i + 1);
                            changed = true;
                        }
                    }
                }
            } while (changed == true);

            return s;
        }
    }

    public class FileDetails
    {
        public string folder { get; set; }
        public string filename { get; set; }
        public string artist { get; set; }
        public string album { get; set; }
        public string CD { get; set; }
        public int trackNumber { get; set; }
        public string trackName { get; set; }
        public string year { get; set; }
        public string renamed { get; set; } //AI creates this name
        public string extension { get; set; }
        public string extra { get; set; }
        public string subfolder { get; set; }

        public FileDetails(string folder = "", string filename = "", string artist = "", string album = "", string CD = "", int trackNumber = 0, string trackName = "", string year = "")
        {
            this.folder = folder;
            this.filename = filename;
            this.artist = artist;
            this.album = album;
            this.CD = CD;
            this.trackNumber = trackNumber;
            this.trackName = trackName;
            this.year = year;
            this.renamed = "";
        }
    }
}
