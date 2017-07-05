using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using WMPLib;
using System.Threading;
using System.Drawing;
using System.Text;

namespace pasta_web_file_finder
{
    public partial class Form1 : Form
    {
        WebClient wc = new WebClient();
        List<Game> games = new List<Game>();
        Game currentGame;
        List<String> currentSoundTracks = new List<string>();
        List<String> listMusicOst = new List<string>();
        List<String> sourceListAll = new List<string>();
        volatile int activeThreads = 0;
        private static readonly Object LOCKER = new object();
        Thread filterThread;
        public static String OST_LIST_DUMP = Path.Combine(Application.StartupPath, "OSTDUMP.dmp");
        Control selectedControl;

        public Form1()
        {
            InitializeComponent();
            Control.CheckForIllegalCrossThreadCalls = false;
            txtF1.ReadOnly = true;
            CreateLoadOn(lstSiteOSTS1);
            Hide();

            if (File.Exists(OST_LIST_DUMP))
            {
                foreach (String s in File.ReadAllLines(OST_LIST_DUMP))
                {
                    String gameName = s.Split('¬')[0];

                    Game existentGame = new Game();
                    existentGame.Nome = gameName;

                    List<String> parts = s.Split('¬').ToList().Where(k=> !String.IsNullOrWhiteSpace(k)).ToList();

                    String aux1="", aux2="";

                    for(int i = 0; i < parts.Count; i++)
                    {
                        if (i == 0) continue;

                        if (String.IsNullOrWhiteSpace(aux1))
                        {
                            aux1 = parts[i];
                        }
                        else if (String.IsNullOrWhiteSpace(aux2))
                        {
                            aux2 = parts[i];
                            existentGame.Tracks.Add(aux1, aux2);
                            aux1 = null;
                            aux2 = null;
                        }
                    }
                    games.Add(existentGame);
                }

                foreach(Game game in games)
                {
                    AddItemToListBox1(game.Nome);
                }

                DestroyLoadOn(lstSiteOSTS1);
                txtF1.ReadOnly = false;
            }
            else
            {
                //https://ocremix.org/remix/OCR03523
                for (int i = 3501; i <= 4000; i++)
                {
                    String sulfix = i.ToString("00000");
                    FillListBySimbol(sulfix);
                }
                DestroyLoadOn(lstSiteOSTS1);
                txtF1.ReadOnly = false;

                List<String> sb = new List<String>();

                foreach (Game game in games)
                {
                    String line = "";
                    line += game.Nome + "¬";

                    foreach (KeyValuePair<String, String> kvp in game.Tracks)
                    {
                        line += kvp.Key + "¬" + kvp.Value + "¬";
                    }

                    sb.Add(line);
                }

                File.WriteAllLines(OST_LIST_DUMP, sb.ToArray());
                Application.Restart();
            }
            AdjustScreen();
        }

        private void CreateLoadOn(Control control)
        {
            selectedControl = control;
            lstMusicsOST2.Enabled = false;
            lstSiteOSTS1.Enabled = false;
            control.Visible = false;            
            loadPicture.BackColor = System.Drawing.Color.Black;
            loadPicture.Size = control.Size;
            loadPicture.Location = control.Location;
            loadPicture.Visible = true;
            loadPicture.BringToFront();
        }

        private void DestroyLoadOn(Control control)
        {
            selectedControl = control;
            loadPicture.Visible = false;
            lstSiteOSTS1.Enabled = true;
            lstMusicsOST2.Enabled = true;
            control.Visible = true;
        }

        public void FillListBySimbol(String args)
        {

            String simbol = args;
            WebClient wc = new WebClient();
            String gameUrl = "https://ocremix.org/remix/OCR" + simbol;
            try
            {
                String source = (wc.DownloadString(gameUrl));
                String gameName = Regex.Split(source, "<em>")[1].Split('<')[0];
                String musicName = Regex.Split(source, "href=\"/song/")[1].Split('>')[1].Split('<')[0];
                Game existentGame = games.Find(k => k.Nome == gameName);
                if (existentGame == null)
                {
                    existentGame = new Game(gameName, musicName, GetMusicLink(gameUrl));
                    games.Add(existentGame);
                }
                else
                {
                    existentGame.Tracks.Add(musicName, GetMusicLink(gameUrl));
                }
            }
            catch
            {
            }

        }

        public String GetMusicLink(String link)
        {
            String content = wc.DownloadString(link);
            content = Regex.Split(content, ".mp3").ToList().Where(k => k.Contains("Download from")).ToList()[0];
            content = Regex.Split(content, "://")[1] + ".mp3";
            content = "http://" + content;
            return content;
        }

        private void AddItemToListBox1(String nome)
        {
            lstSiteOSTS1.Items.Add(nome);
            currentSoundTracks.Add(nome);
        }

        private void listBox1_DoubleClick(object sender, EventArgs e)
        {
            axWindowsMediaPlayer1.Ctlcontrols.pause();
            ThreadPool.QueueUserWorkItem(new WaitCallback(LoadMusicsBySelectedGame), new Object());
        }

        private void LoadMusicsBySelectedGame(Object args)
        {
            Invoke(new MethodInvoker(() => CreateLoadOn(lstMusicsOST2)));
            currentGame = games.Find(k => k.Nome.Equals(lstSiteOSTS1.SelectedItem.ToString()));
            lstMusicsOST2.Items.Clear();
            listMusicOst.Clear();
            foreach (KeyValuePair<string, string> kvp in currentGame.Tracks)
            {
                lstMusicsOST2.Items.Add(kvp.Key);
            }
            Invoke(new MethodInvoker(() => DestroyLoadOn(lstMusicsOST2)));
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            filterThread?.Abort();
            filterThread = new Thread(() => ApplyFilter());
            filterThread.IsBackground = true;
            filterThread.Start();
        }

        private void ApplyFilter()
        {
            lock (LOCKER)
            {
               
                CreateLoadOn(lstSiteOSTS1);
                lstSiteOSTS1.Visible = false;
                lstSiteOSTS1.Items.Clear();

                foreach (string s in currentSoundTracks.Where(k => k.ToLower().Contains(txtF1.Text.ToLower())))
                {
                    lstSiteOSTS1.Items.Add(s);
                }
                lstSiteOSTS1.Visible = true;
                DestroyLoadOn(lstSiteOSTS1);
            }
        }

        private void DownloadCurrentTrack()
        {
            Downloader download = new Downloader(new List<string>(){ lstMusicsOST2.SelectedItem.ToString() }, currentGame);
            download.ShowDialog();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            DownloadCurrentTrack();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            Process.Start("\"" + Path.Combine(Application.StartupPath, "OST") + "\"");
        }

        private void listBox2_DoubleClick(object sender, EventArgs e)
        {
            if (lstMusicsOST2.Items.Count > 0)
            {
                axWindowsMediaPlayer1.URL = currentGame.Tracks[lstMusicsOST2.SelectedItem.ToString()];
                axWindowsMediaPlayer1.Ctlcontrols.play();
            }
        }


        private void timer1_Tick(object sender, EventArgs e)
        {
            if (axWindowsMediaPlayer1.playState == WMPPlayState.wmppsStopped && checkBox1.Checked)
            {
                if (lstMusicsOST2.SelectedIndex < lstMusicsOST2.Items.Count - 1)
                {
                    lstMusicsOST2.SelectedIndex = lstMusicsOST2.SelectedIndex + 1;
                    listBox2_DoubleClick(sender, e);
                }
            }
        }

        private void axWindowsMediaPlayer1_MediaChange(object sender, AxWMPLib._WMPOCXEvents_MediaChangeEvent e)
        {
            Text = "KHInsider Music Player - " + currentGame.Nome + " / " + axWindowsMediaPlayer1.Ctlcontrols.currentItem.name;
        }

        private void button3_Click(object sender, EventArgs e)
        {
            List<String> lista = new List<string>();
            foreach(String s in lstMusicsOST2.Items)
            {
                lista.Add(s);
            }
            Downloader download = new Downloader(lista, currentGame);
            download.ShowDialog();
        }

        private void axWindowsMediaPlayer1_Enter(object sender, EventArgs e)
        {

        }

        private void lstSiteOSTS_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void textBox2_TextChanged(object sender, EventArgs e)
        {
            lstMusicsOST2.Items.Clear();
            foreach (string s in listMusicOst)
            {
                if (s.Contains(txtF2.Text))
                {
                    lstMusicsOST2.Items.Add(s);
                }
            }
        }

        private void AdjustScreen()
        {
            int textHeight = 26;
            int playerHeight = 50;
            int buttonHeight = 30;

            picLogo.Location = new Point(0, 0);
            picLogo.Size = new Size(Width, 50);

            txtF1.Location = new Point(0, 50);
            txtF1.Size = new Size(Width / 2, textHeight);

            txtF2.Location = new Point(Width / 2, 50);
            txtF2.Size = new Size(Width / 2, textHeight);

            lstSiteOSTS1.Location = new Point(0, textHeight + 50);
            lstSiteOSTS1.Size = new Size(Width / 2, Height - (buttonHeight * 2) - playerHeight - (textHeight + 50));

            lstMusicsOST2.Location = new Point(Width / 2, textHeight + 50);
            lstMusicsOST2.Size = lstSiteOSTS1.Size;

            btnDownloadOst1.Size = new Size(lstSiteOSTS1.Width, buttonHeight);
            btnDownloadOst1.Location = new Point(0, lstSiteOSTS1.Bottom);

            btnDownloadMusica2.Location = new Point(lstMusicsOST2.Left, lstMusicsOST2.Bottom);
            btnDownloadMusica2.Size = new Size(lstMusicsOST2.Width / 2, buttonHeight);

            btnDownloadAlbum3.Location = new Point(lstMusicsOST2.Left + lstMusicsOST2.Width / 2, lstMusicsOST2.Bottom);
            btnDownloadAlbum3.Size = new Size(lstMusicsOST2.Width / 2, buttonHeight);

            if (selectedControl != null)
            {
                loadPicture.Size = selectedControl.Size;
                loadPicture.Location = selectedControl.Location;
            }
        }

        private void Form1_SizeChanged(object sender, EventArgs e)
        {
            AdjustScreen();

        }
    }
}
