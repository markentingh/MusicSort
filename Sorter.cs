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

            var extensions = new string[] { ".mp3", ".flac" };
            var files = GetFiles(txtFolder.Text, extensions.ToList());
            string[] p;
            var CDs = new string[] { "cd1", "cd2", "cd3", "cd4", "cd 1", "cd 2", "cd 3", "cd 4", "disc1", "disc2", "disc3", "disc4", "disc 1", "disc 2", "disc 3", "disc 4",
                                    "cd01", "cd02", "cd03", "cd04", "cd 01", "cd 02", "cd 03", "cd 04", "disc01", "disc02", "disc03", "disc04", "disc 01", "disc 02", "disc 03", "disc 04"};
            var commonFolders = new string[] { "albums", "compilations", "remixes", "singles", "tracks", "demos", "doodles", "wip", "work in progress", "originals", "remakes", "downloads" };
            var years = new string[] { "[1", "[2", "(1", "(2" }.ToList();
            var yearsEnd = new string[] { "]", "]", ")", ")" };
            var nums = new string[] { "1", "2", "3", "4", "5", "6", "7", "8", "9", "0" };
            var trackNumEnd = new string[] { ".", "-", ")", "]" };
            var trackNameStart = new string[] { ".", "-", "_", ")", "]", "}" };
            var x = 0;
            var i = 0;
            var a = 0;
            var y = new int[4] { 0, 0, 0, 0 };
            var skip = 0;
            var inc = 0;
            var num = 0.0;
            var tempTrackNums = new int[] { 0, 0, 0 };
            var str = new string[] { };
            var orig = "";
            FileDetails file;
            FileDetails lastFile;
            var details = new List<FileDetails>();

            progress.Maximum = files.Count;

            foreach (string f in files)
            {
                p = f.ToLower().Replace("_", " ").Split('\\');
                x = p.Length;
                file = new FileDetails();
                skip = 0;
                inc = 0;
                
                //get album folder ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
                skip = 0;
                while(skip < 3)
                {
                    a = x - 2 - skip;
                    if (p[a].Length <= 8 && CDs.Count(c => p[a].IndexOf(c) >= 0) > 0)
                    {
                        //found folder for CD
                        file.CD = CDs.Where(c => p[a].IndexOf(c) >= 0).ToList()[0].Replace(" ", "").ToUpper().Trim();
                    }
                    else if (commonFolders.Count(c => p[a].IndexOf(c) == 0) > 0)
                    {
                        //found a common folder
                    }
                    else {
                        //this must be an album folder
                        file.album = p[a].Trim();
                        i = years.FindIndex(0, 1, c => file.album.IndexOf(c) >= 0);
                        if (i >= 0)
                        {
                            //year exists in folder
                            y[0] = file.album.IndexOf(years[i], 0);
                            y[1] = file.album.IndexOf(yearsEnd[i], y[0] + 1);
                            if (y[1] > 0)
                            {
                                //found full year
                                file.year = file.album.Substring(y[0] + 1, y[1] - (y[0] + 1)).Trim();
                                file.album = file.album.Replace(file.album.Substring(y[0], (y[1] + 1) - y[0]), "").Replace("  ", " ").Trim();
                            }
                        }
                        break;
                    }
                    skip++;
                }
                

                //get artist folder ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
                while (skip < 6)
                {
                    a = x - 3 - skip;
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
                                }
                            }
                        }
                        else
                        {
                            file.artist = p[a];
                        }
                        
                        break;
                    }
                    skip++;
                }

                //get track name /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
                file.filename = p[x - 1].Trim();
                file.folder = f.ToLower().Replace(file.filename, "");
                file.trackName = file.filename.Replace("_", " ");
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
                                skip = 1;
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
                    
                    //remove lingering character symbols at the beginning of the track name
                    if (trackNameStart.Count(c => file.trackName.IndexOf(c) == 0) == 1)
                    {
                        file.trackName = file.trackName.Substring(1).Trim();
                    }

                    //remove extra content from track name
                    if (file.trackName.IndexOf(" - ") > 0)
                    {
                        str = file.trackName.Split(new string[] { " - " }, StringSplitOptions.RemoveEmptyEntries);
                        if (str[0].IndexOf(file.artist) >= 0 || str[0].IndexOf(file.album) >= 0)
                        {
                            file.trackName = str[str.Length - 1];
                        }
                    }
                    if (file.trackName.IndexOf(file.artist) == 0 && file.artist.Length > 0)
                    {
                        file.trackName = file.trackName.Split(new string[] { file.artist }, 2, StringSplitOptions.None)[1];
                    }
                    //remove lingering character symbols at the beginning of the track name
                    if (trackNameStart.Count(c => file.trackName.IndexOf(c) == 0) == 1)
                    {
                        file.trackName = file.trackName.Substring(1).Trim();
                    }
                    if(file.trackName == "" && file.trackNumber > 0)
                    {
                        file.trackName = orig;
                        file.trackNumber = 0;
                    }
                    if (double.TryParse(file.trackName.Substring(0, 1), out num) == false) { break; }
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
                    }
                }
                
                //finally, add file to list
                details.Add(file);

                progress.Value++;
                progress.Refresh();
            }

            dataFiles.DataSource = details;

            progress.Visible = false;
            txtFolder.Visible = true;
            btnBrowse.Visible = true;
            btnSearch.Visible = true;
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
