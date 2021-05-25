using System;
using DiscordChatGrabber.Properties;
using System.Windows.Forms;

namespace DiscordChatGrabber
{
    public partial class Settings : Form
    {
        public Settings()
        {
            InitializeComponent();
        }

        private void Settings_Load(object sender, EventArgs e)
        {
            if(Properties.Settings.Default.DiscordRPC == "true"){ rpcCheckBox.Checked = true; }
            else if(Properties.Settings.Default.DiscordRPC == "false") { rpcCheckBox.Checked = false; }
            tokenBox.Text = Properties.Settings.Default.defaultToken;

            textBox1.Text = Properties.Settings.Default.defaultDirectory;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog f = new FolderBrowserDialog();
            f.Description = "Please select an output directory";
            f.ShowDialog();
            textBox1.Text = f.SelectedPath + @"\DiscordChatGrabber";
            Properties.Settings.Default.defaultDirectory = f.SelectedPath + @"\DiscordChatGrabber";
            Properties.Settings.Default.Save();
        }

        private void tokenBox_TextChanged(object sender, EventArgs e)
        {
            Properties.Settings.Default.defaultToken = tokenBox.Text;
            Properties.Settings.Default.Save();
        }

        private void rpcCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            if(rpcCheckBox.Checked == true){Properties.Settings.Default.DiscordRPC = "true";}
            else{Properties.Settings.Default.DiscordRPC = "false";}
            Properties.Settings.Default.Save();
        }
    }
}
