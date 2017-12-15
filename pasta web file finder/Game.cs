using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Windows.Forms;

namespace pasta_web_file_finder
{
    public class Game
    {
        private string url = String.Empty;
        private String nome = "";
        public string Nome { get { return nome; } set { nome = WebUtility.HtmlDecode(value); } }
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
            wc.Proxy = new WebProxy("192.168.113.245:3128") { UseDefaultCredentials = true, BypassProxyOnLocal = true };
            wc.DownloadFileAsync(new Uri(track), Path.Combine(Application.StartupPath, "OST", Nome, WebUtility.UrlDecode(track.Split('/')[track.Split('/').Length - 1].Replace('-', ' '))));
        }

    }
}
