using System;
using System.Diagnostics;
using System.Windows.Forms;

namespace DiscordChatGrabber
{
    public partial class About : Form
    {
        public About()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Clipboard.SetText("Wujek_Foliarz#9541");
        }

        private void button2_Click(object sender, EventArgs e)
        {
            Process.Start("https://github.com/WujekFoliarz/DiscordChatGrabber");
        }

        private void button3_Click(object sender, EventArgs e)
        {
            Process.Start("https://steamcommunity.com/id/AppleProfil/");
        }

        private void button4_Click(object sender, EventArgs e)
        {
            Process.Start("https://www.youtube.com/channel/UCFlsfz96PRkVuZ7uFImXfyA");
        }
    }
}
