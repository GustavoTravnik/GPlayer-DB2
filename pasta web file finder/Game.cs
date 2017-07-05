using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace pasta_web_file_finder
{
    public class Game
    {
        private string url = String.Empty;
        public string Nome { get; set; } = string.Empty;
        public Dictionary<string, string> Tracks { get; set; } = new Dictionary<string, string>();
        public String OST_LIST_DUMP = (Application.StartupPath);

        public Game(String Nome, String musicName, String url)
        {
            this.Nome = Nome;
            Tracks.Add(musicName, url);
        }

        public Game()
        {

        }        

        public void LoadMusics(WebClient wc)
        {
           
        }

        public void Download(ref WebClient wc, String track)
        {
            Directory.CreateDirectory(Path.Combine(Application.StartupPath, "OST", Nome));
            wc.DownloadFileAsync(new Uri(track), Path.Combine(Application.StartupPath, "OST", Nome, track.Split('/')[track.Split('/').Length - 1].Replace('-', ' ')));
        }

    }
}
