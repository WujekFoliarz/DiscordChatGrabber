using DiscordRPC;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;

namespace DiscordChatGrabber
{
    public partial class Main : Form
    {
        private List<Emoji> emojiList = new List<Emoji>();
        private Regex emojiregx = new Regex("(<:+[a-zA-Z0-9_]+:+[0-9]+>)");
        private WebClient wc = new WebClient();
        private string selectedPath;
        private Thread t;
        private DiscordRpcClient client;

        public Main()
        {
            InitializeComponent();
            wc.Headers.Set(HttpRequestHeader.UserAgent, "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/90.0.4430.93 Safari/537.36");
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (channel_IDBOX.Text != string.Empty && tokenBox.Text != string.Empty)
            {
                emojiList.Clear();
                richTextBox1.Clear();

                selectedPath = Properties.Settings.Default.defaultDirectory;

                if (Directory.Exists(selectedPath))
                {
                    if(Directory.Exists(selectedPath + @"\" + channel_IDBOX.Text))
                    {
                        DialogResult res = MessageBox.Show($"Folder named {channel_IDBOX.Text} already exists. Do you want to delete it?", "Confirmation", MessageBoxButtons.YesNo, MessageBoxIcon.Information);
                        if (res == DialogResult.Yes)
                        {
                            Directory.Delete(selectedPath + @"\" + channel_IDBOX.Text, true);
                        }
                    }

                    setPresence($"Currently downloading a channel:\n[{channel_IDBOX.Text}]");
                    Directory.CreateDirectory(selectedPath + @"\" + channel_IDBOX.Text);
                    selectedPath = selectedPath + @"\" + channel_IDBOX.Text;
                    richTextBox1.AppendText("Creating a directory - " + selectedPath + @"\Chunks" + "\n");
                    Directory.CreateDirectory(selectedPath + @"\Chunks");
                    richTextBox1.AppendText("Creating a directory - " + selectedPath + @"\Resources\Images" + "\n");
                    Directory.CreateDirectory(selectedPath + @"\Resources\Images" + "\n");
                    richTextBox1.AppendText("Creating a directory - " + selectedPath + @"\Resources\Avatars" + "\n");
                    Directory.CreateDirectory(selectedPath + @"\Resources\Avatars" + "\n");
                    richTextBox1.AppendText("Creating a directory - " + selectedPath + @"\Resources\Emojis" + "\n");
                    Directory.CreateDirectory(selectedPath + @"\Resources\Emojis" + "\n");
                    richTextBox1.AppendText("Creating a file - " + selectedPath + @"\" + channel_IDBOX.Text + ".html" + "\n");
                    File.Create(selectedPath + @"\" + channel_IDBOX.Text + ".html").Close();

                    t = new Thread(() => downloadChannelHistoryJson(channel_IDBOX.Text, tokenBox.Text));
                    t.IsBackground = true;

                    abortButton.Enabled = true;
                    button1.Enabled = false;
                    tokenBox.ReadOnly = true;
                    channel_IDBOX.ReadOnly = true;
                    t.Start();
                }
            }
        }

        private void downloadChannelHistoryJson(string channelID, string token)
        {
            richTextBox1.Invoke(new Action(delegate () { richTextBox1.AppendText($"Downloading channel [{channelID}] to JSON files...\n"); }));
            string url = "https://discord.com/api/v9/channels/" + channelID + "/messages?limit=100";
            wc.Headers.Set(HttpRequestHeader.Authorization, token);
            int chunkCount = 1;

            try
            {
                while (true)
                {
                    richTextBox1.Invoke(new Action(delegate () { richTextBox1.AppendText($"Downloading chunk{chunkCount}.json...\n"); }));
                    File.WriteAllText(selectedPath + @"\Chunks\chunk" + chunkCount + ".json", wc.DownloadString(url));
                    dynamic tempjson = JsonConvert.DeserializeObject(File.ReadAllText(selectedPath + @"\Chunks\chunk" + chunkCount + ".json"));
                    string lastID = string.Empty;
                    chunkCount++;

                    int i = 0;
                    while (true)
                    {
                        try
                        {
                            string temp = tempjson[i]["id"].ToString();
                            lastID = temp;
                            i++;
                        }
                        catch (ArgumentOutOfRangeException)
                        {
                            //?before=842472736348569651
                            url = "https://discord.com/api/v9/channels/" + channelID + "/messages" + $"?before={lastID}" + "&limit=100";
                            tempjson = null;
                            break;
                        }
                    }
                }
            }
            catch (WebException e)
            {
                HttpWebResponse respone = (HttpWebResponse)e.Response;
                //jsonChunks.RemoveAt(jsonChunks.Count-1);

                if (respone.StatusCode == HttpStatusCode.Unauthorized) { MessageBox.Show("Access Denied!"); Directory.Delete(Properties.Settings.Default.defaultDirectory + @"\" + channelID, true); reset(); }
                else if (respone.StatusCode == HttpStatusCode.NotFound) { MessageBox.Show("The channel doesn't exists!"); Directory.Delete(Properties.Settings.Default.defaultDirectory + @"\" + channelID, true); reset(); }
                else if (respone.StatusCode == HttpStatusCode.BadRequest){saveMessagesToList();}
                else { MessageBox.Show("Unknown error! - " + respone.StatusCode.ToString()); MessageBox.Show("The channel doesn't exists!"); Directory.Delete(Properties.Settings.Default.defaultDirectory + @"\" + channelID, true); reset(); }
            }
        }

        private void saveMessagesToList()
        {
            WebClient client = new WebClient();
            client.DownloadFile("https://discord.com/assets/322c936a8c8be1b803cd94861bdfa868.png", selectedPath + @"\Resources\Avatars\default.png");
            richTextBox1.Invoke(new Action(delegate () { richTextBox1.AppendText("Parsing messages...\n"); }));
            int i = 0;
            List<Message> msgList = new List<Message>();
            foreach (string chunk in Directory.GetFiles(selectedPath + @"\Chunks", "*.json"))
            {
                i = 0;
                richTextBox1.Invoke(new Action(delegate () { richTextBox1.AppendText($"Reading from {Path.GetFileName(chunk)}...\n"); }));
                while (true)
                {
                    try
                    {
                        dynamic json = JsonConvert.DeserializeObject(File.ReadAllText(chunk));
                        string id = json[i]["id"].ToString();
                        Message msg = new Message();

                        msg.messageID = id;
                        msg.Type = json[i]["type"].ToString();
                        msg.Content = json[i]["content"].ToString();
                        msg.authorID = json[i]["author"]["id"].ToString();
                        msg.authorUsername = json[i]["author"]["username"].ToString();
                        msg.authorAvatar = json[i]["author"]["avatar"].ToString();
                        msg.authorDiscriminator = json[i]["author"]["discriminator"].ToString();

                        MatchCollection emojiMatches = emojiregx.Matches(msg.Content);
                        // EXAMPLE - <:821369159970848808:829738103661002772>

                        foreach (Match match in emojiMatches)
                        {
                            string emoji_name = match.Value.Split(':')[1];
                            string emoji_ID = match.Value.Split(':')[2].Replace('>', ' ').Trim(' ');

                            if (!File.Exists(selectedPath + @"\Resources\Emojis\" + emoji_ID + ".png"))
                            {
                                richTextBox1.Invoke(new Action(delegate () { richTextBox1.AppendText($"CUSTOM EMOJI DETECTED! DOWNLOADING [{emoji_name}]\n"); }));
                                emojiList.Add(new Emoji() { emoji_id = emoji_ID, emoji_name = emoji_name, emoji_match = match.Value });
                                client.DownloadFile("https://cdn.discordapp.com/emojis/" + emoji_ID + ".png", selectedPath + @"\Resources\Emojis\" + emoji_ID + ".png");
                            }

                            //essageBox.Show(emoji_name + " | " + emoji_ID);
                        }

                        if (!File.Exists(selectedPath + @"\Resources\Avatars\" + msg.authorAvatar + ".png") && msg.authorAvatar != string.Empty)
                        {
                            richTextBox1.Invoke(new Action(delegate () { richTextBox1.AppendText($"AVATAR DETECTED! DOWNLOADING [{msg.authorAvatar}]\n"); }));
                            client.DownloadFile("https://cdn.discordapp.com/avatars/" + msg.authorID + "/" + msg.authorAvatar + ".png?size=128", selectedPath + @"\Resources\Avatars\" + msg.authorAvatar + ".png");
                        }

                        if (msg.authorAvatar == string.Empty)
                        {
                            msg.authorAvatar = "default.png";
                        }

                        if (!msg.authorAvatar.EndsWith(".png"))
                        {
                            msg.authorAvatar = msg.authorAvatar + ".png";
                        }

                        try
                        {
                            msg.attachmentURL = json[i]["attachments"][0]["url"].ToString();
                            msg.attachmentFileName = json[i]["attachments"][0]["filename"].ToString();
                            msg.attachmentContentType = json[i]["attachments"][0]["content_type"].ToString();
                            richTextBox1.Invoke(new Action(delegate () { richTextBox1.AppendText($"ATTACHMENT DETECTED! DOWNLOADING [{msg.attachmentFileName}]\n"); }));
                            if (File.Exists(selectedPath + @"\Resources\Images\" + msg.attachmentFileName))
                            {
                                Random rand = new Random();
                                while (true)
                                {
                                    msg.attachmentFileName = rand.Next(0, 9999999) + "_" + msg.attachmentFileName;
                                    if (!File.Exists(selectedPath + @"\Resources\Images\" + msg.attachmentFileName)) { break; }
                                }
                            }
                            client.DownloadFile(msg.attachmentURL, selectedPath + @"\Resources\Images\" + msg.attachmentFileName);
                        }
                        catch { }

                        if (json[i]["tts"].ToString() == "true") { msg.TTS = true; }
                        else { msg.TTS = false; }

                        DateTime posted = DateTime.Parse(json[i]["timestamp"].ToString().Replace('.', '/'));
                        msg.timeStamp = posted;

                        if (json[i]["edited_timestamp"].ToString() != string.Empty)
                        {
                            DateTime edited = DateTime.Parse(json[i]["edited_timestamp"].ToString().Replace('.', '/'));
                            msg.editedTimestamp = edited;
                        }

                        msgList.Add(msg);

                        richTextBox1.Invoke(new Action(delegate () { richTextBox1.AppendText($"Message parsed! ID [{msg.messageID}]\n"); }));
                        i++;
                        json = null;
                        msg = null;
                    }
                    catch (Exception e)
                    {
                        System.Diagnostics.Debug.WriteLine(e.ToString());
                        break;
                    }
                }
            }

            saveMessageListToHTML(msgList);
        }

        private void saveMessageListToHTML(List<Message> msgList)
        {
            msgList.Reverse();
            msgList.Sort((x, y) => DateTime.Compare(x.timeStamp, y.timeStamp));
            richTextBox1.Invoke(new Action(delegate () { richTextBox1.AppendText("Saving messages to HTML...\n"); }));
            string htmlFile = selectedPath + @"\" + channel_IDBOX.Text + ".html";

            File.AppendAllText(htmlFile, "<html>\n\n");
            File.AppendAllText(htmlFile, "  <head>\n");
            File.AppendAllText(htmlFile, "      <title>" + channel_IDBOX.Text + "</title>\n");
            File.AppendAllText(htmlFile, "  </head>\n\n");
            File.AppendAllText(htmlFile, "  <body style=\"background-color: black; color: white; font-family: arial;\">\n");

            // https://discord.com/api/v9/users/648910280482488321/profile

            foreach (Message msg in msgList)
            {
                if (msg.Content != string.Empty)
                {
                    File.AppendAllText(htmlFile, "		<div class=\"regular\">\n");
                    File.AppendAllText(htmlFile, "			<hr>\n");
                    File.AppendAllText(htmlFile, $"         <h3 style=\"color: red; float: right; \"> CREATED: {msg.timeStamp}</h3>\n");
                    if (msg.editedTimestamp.ToString() != "01.01.0001 00:00:00") { File.AppendAllText(htmlFile, $"        <h3 style=\"color: red; float: right; padding - right: 10px; \">EDITED: {msg.editedTimestamp} |‎‎‎‏‏‎ ‎</h3>\n"); }
                    File.AppendAllText(htmlFile, $"			<h1 style=\"color: blue; \"><img src=\"" + selectedPath + "\\Resources\\Avatars\\" + msg.authorAvatar + "\" alt=\"avatar\" style=\"height: 128; height: 128;\"> " + msg.authorUsername + ":</h1>\n");

                    foreach (Emoji em in emojiList)
                    {
                        if (msg.Content.Replace(em.emoji_match, "").Trim(' ') == string.Empty)
                        {
                            msg.Content = msg.Content.Replace(em.emoji_match, "<img src=\"" + selectedPath + @"\Resources\Emojis\" + em.emoji_id + ".png" + "\" style=\"height: 64px; width: 64px;\">");
                        }
                        else
                        {
                            msg.Content = msg.Content.Replace(em.emoji_match, "<img src=\"" + selectedPath + @"\Resources\Emojis\" + em.emoji_id + ".png" + "\" style=\"height: 36px; width: 36px;\">");
                        }
                    }

                    File.AppendAllText(htmlFile, $"         <h2>{msg.Content}</h2>\n");
                    if (msg.attachmentURL != null && msg.attachmentContentType != null)
                    {
                        if (msg.attachmentContentType.Contains("video"))
                        {
                            File.AppendAllText(htmlFile, "          <video width=\"1624\" height=\"750\" controls>");
                            File.AppendAllText(htmlFile, "            <source src=\"" + selectedPath + @"\Resources\Images\" + msg.attachmentFileName + "\" type=\"" + msg.attachmentContentType + "\">");
                            File.AppendAllText(htmlFile, "          Your browser does not support the video tag.");
                            File.AppendAllText(htmlFile, "          </video>");
                        }
                        else if (msg.attachmentContentType.Contains("image"))
                        {
                            File.AppendAllText(htmlFile, "          <img src=\"" + selectedPath + @"\Resources\Images\" + msg.attachmentFileName + "\" style=\"max-width: 100%; max-height: 100%;\"></img>");
                        }
                    }
                    File.AppendAllText(htmlFile, $"     </div>\n");
                }
            }

            File.AppendAllText(htmlFile, "  </body>\n\n");
            File.AppendAllText(htmlFile, "</html>");

            richTextBox1.Invoke(new Action(delegate () { richTextBox1.AppendText("Download complete!\n"); }));
            reset();
        }

        private void richTextBox1_TextChanged(object sender, EventArgs e)
        {
            richTextBox1.SelectionStart = richTextBox1.Text.Length;
            richTextBox1.ScrollToCaret();
        }

        private void abortButton_Click(object sender, EventArgs e)
        {
            Application.Restart();
        }

        private void reset()
        {
            setPresence("Idle");
            abortButton.Invoke(new Action(delegate () { abortButton.Enabled = false; }));
            button1.Invoke(new Action(delegate () { button1.Enabled = true; }));
            tokenBox.Invoke(new Action(delegate () { tokenBox.ReadOnly = false; }));
            channel_IDBOX.Invoke(new Action(delegate () { channel_IDBOX.ReadOnly = false; }));
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            FormCollection fc = Application.OpenForms;

            bool open = false;
            foreach (Form frm in fc)
            {
                //iterate through
                if (frm.Name == "About")
                {
                    open = true;
                }
            }

            if (open == false)
            {
                About abForm = new About();
                abForm.Show();
            }
            fc = null;
        }

        private void settingsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            FormCollection fc = Application.OpenForms;

            bool open = false;
            foreach (Form frm in fc)
            {
                //iterate through
                if (frm.Name == "Settings")
                {
                    open = true;
                }
            }

            if (open == false)
            {
                Settings stForm = new Settings();
                stForm.Show();
            }
            fc = null;
        }

        private void Main_Load(object sender, EventArgs e)
        {
            tokenBox.Text = Properties.Settings.Default.defaultToken;
            if (Properties.Settings.Default.defaultDirectory == string.Empty) { Properties.Settings.Default.defaultDirectory = $"{Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)}\\DiscordChatGrabber"; }
            if (Properties.Settings.Default.DiscordRPC == string.Empty) { Properties.Settings.Default.DiscordRPC = "true"; }
            if (Properties.Settings.Default.DiscordRPC != "true" && Properties.Settings.Default.DiscordRPC != "false") { Properties.Settings.Default.DiscordRPC = "true"; }

            if (Properties.Settings.Default.DiscordRPC == "true")
            {
                client = new DiscordRpcClient("846829297096458252");

                //Connect to the RPC
                client.Initialize();

                //Set the rich presence
                //Call this as many times as you want and anywhere in your code.
                setPresence("Idle");
            }

            Properties.Settings.Default.Save();
        }

        private void setPresence(string details)
        {
            if (client.IsInitialized == true)
            {
                client.SetPresence(new RichPresence()
                {
                    Details = details,
                    Assets = new Assets()
                    {
                        LargeImageKey = "ik",
                    }
                });
            }
        }
    }

    public class Emoji
    {
        public string emoji_name { get; set; }
        public string emoji_id { get; set; }
        public string emoji_match { get; set; }
    }

    public class Message
    {
        public string messageID { get; set; }
        public string Type { get; set; }
        public string Content { get; set; }
        public string authorID { get; set; }
        public string authorUsername { get; set; }
        public string authorAvatar { get; set; }
        public string authorDiscriminator { get; set; }
        public string attachmentURL { get; set; }
        public string attachmentFileName { get; set; }
        public string attachmentContentType { get; set; }
        public bool TTS { get; set; }
        public DateTime timeStamp { get; set; }
        public DateTime editedTimestamp { get; set; }
    }
}