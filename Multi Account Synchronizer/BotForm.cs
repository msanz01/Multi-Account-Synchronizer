using IniParser.Model;
using IniParser;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Multi_Account_Synchronizer
{
    internal partial class BotForm : Form
    {
        public Bot Bot;
        public Player player;
        public Scene scene;
        PhoenixApi api;
        public BotForm(Bot b, Player p, Scene s, PhoenixApi api)
        {
            Bot = b;
            player = p;
            this.api = api;
            scene = s;
            InitializeComponent();
            Bot.richTextBox1 = this.richTextBox1;
            Run();
        }

        private async void Run()
        {
            while (player.Name == "")
                await Task.Delay(1);
            while (true)
            {
                this.Text = $"{player.Name} | Working time: {Bot.WorkingTimeSw.Elapsed.ToString(@"hh\:mm\:ss")}";
                await Task.Delay(500);
            }
            
        }


        private async void BotForm_Load(object sender, EventArgs e)
        {
            richTextBox1.ScrollToCaret();
        }



        private void radioButton1_CheckedChanged(object sender, EventArgs e)
        {
            Bot.DPS = radioButton1.Checked;
        }

        private void radioButton2_CheckedChanged(object sender, EventArgs e)
        {
            Bot.Buffer = radioButton2.Checked;
        }

        private void radioButton3_CheckedChanged(object sender, EventArgs e)
        {
            Bot.MinilandOwner = radioButton3.Checked;
        }

        private async void roundedButton1_Click(object sender, EventArgs e)
        {
            UpdateComboBox();
        }
        private async void UpdateComboBox()
        {
            player.updated = false;
            
            api.query_skills_info();
            comboBox1.Items.Clear();
            while (!player.updated)
                await Task.Delay(1);
            foreach (var item in player.Skills)
            {
                comboBox1.Items.Add(item.Item1);
            }
            comboBox1.Items.Add("Partner Skill Q");
            comboBox1.Items.Add("Partner Skill W");
            comboBox1.Items.Add("Partner Skill E");
            comboBox1.SelectedIndex = comboBox1.Items.Count > 0 ? 0 : -1;
        }
        private void roundedButton2_Click(object sender, EventArgs e)
        {
            if (comboBox1.SelectedIndex < 0)
                return;
            if (list.Items.Contains(comboBox1.SelectedItem))
                return;
            list.Items.Add(comboBox1.SelectedItem);
            Bot.Buffs.Clear();
            Bot.PartnerBuffs.Clear();
            foreach (string item in list.Items)
            {
                if (item.Contains("Partner"))
                {
                    char skill = item[14];
                    switch (skill)
                    {
                        case 'Q':
                            Bot.PartnerBuffs.Add(0);
                            break;
                        case 'W':
                            Bot.PartnerBuffs.Add(1);
                            break;
                        case 'E':
                            Bot.PartnerBuffs.Add(2);
                            break;
                        default:
                            break;

                    }
                }
                else
                {
                    var id = player.Skills.Where(t => t.Item1 == item).FirstOrDefault();
                    Bot.Buffs.Add(id);
                }
                
            }
        }

        private async void removeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (list.SelectedIndex != -1)
            {
                list.Items.RemoveAt(list.SelectedIndex);

            }
            player.updated = false;
            api.query_skills_info();
            Bot.Buffs.Clear();
            Bot.PartnerBuffs.Clear();
            while (!player.updated)
                await Task.Delay(1);
            foreach (string item in list.Items)
            {
                if (item.Contains("Partner"))
                {
                    char skill = item[14];
                    switch (skill)
                    {
                        case 'Q':
                            Bot.PartnerBuffs.Add(0);
                            break;
                        case 'W':
                            Bot.PartnerBuffs.Add(1);
                            break;
                        case 'E':
                            Bot.PartnerBuffs.Add(2);
                            break;
                        default:
                            break;

                    }
                }
                else
                {
                    Tuple<string, int> tup = player.Skills.Where(t => t.Item1 == item).FirstOrDefault();
                    if (tup == null)
                        continue;
                    Bot.Buffs.Add(tup);
                }
               
            }
        }

        private void roundedButton3_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Choose the phoenix bot profile that you use and want to sync", "INFO", MessageBoxButtons.OK, MessageBoxIcon.Information);
            OpenFileDialog o = new OpenFileDialog();
            o.Title = "Open file";
            o.Filter = "INI files|*.ini";
            DialogResult result = o.ShowDialog();
            if (result == DialogResult.Cancel)
                return;
            textBox2.Text = o.FileName;
            Bot.ReadIni(textBox2.Text);
            Bot.CreateNewIni(textBox2.Text);
            MessageBox.Show("The Phoenix Bot profile has changed(deleted path in current settings, made combat and loot whitelist and empty). DO NOT SAVE THE CHANGED PROFILE OVER YOUR ORIGINAL ONE", "CAUTION", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        private void roundedButton4_Click(object sender, EventArgs e)
        {
            if (Bot.run)
            {
                MessageBox.Show("You cannot use that function while running the MAS","Profile",MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            var res = MessageBox.Show("This option rolls back to your original Phoenix Bot profile. Are you sure about that?", "Profile", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (res == DialogResult.No)
                return;
            string path = textBox2.Text;
            string newpath = path.Replace(".ini", "");
            newpath = $"{newpath} Sync.ini";
            ReloadSettings(newpath);
        }
        private async void ReloadSettings(string path)
        {
            if (!File.Exists(path))
            {
                MessageBox.Show("Couldn't reload the Phoenix Bot profile", "Profile", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            var parser = new FileIniDataParser();
            IniData data = parser.ReadFile(path);

            data["Attack"]["blacklist"] = Bot.AttackBlacklist.ToString().ToLower();
            data["Attack"]["whitelist"] = Bot.AttackWhitelist.ToString().ToLower();
            data["Attack"]["monsters\\size"] = $"{Bot.MonsterList.Count}";

            data["Loot"]["blacklist"] = Bot.LootBlacklist.ToString().ToLower();
            data["Loot"]["whitelist"] = Bot.LootWhiteList.ToString().ToLower();
            data["Loot"]["loot_items\\size"] = $"{Bot.LootList.Count}";

            data["Walking"]["path\\size"] = $"{Bot.Path.Count}";

            data["Miniland"]["enabled"] = Bot.MiniEnabled.ToString().ToLower();
            data["Miniland"]["miniland_on_waypoint"] = Bot.MiniOnWaypoint.ToString().ToLower();
            data["Miniland"]["miniland_waypoint_index"] = Bot.MiniWaypointIndex.ToString();
            string newpath = path.Replace(".ini", "");
            newpath = $"{newpath} Reloaded.ini";
            if (File.Exists(newpath))
                File.Delete(newpath);
            File.WriteAllText(newpath, data.ToString());
            Stopwatch sw = Stopwatch.StartNew();
            while (!File.Exists(newpath) && sw.Elapsed.TotalSeconds < 5)
                await Task.Delay(500);
            if (!File.Exists(newpath))
            {
                MessageBox.Show("Couldn't reload the Phoenix Bot profile", "Profile", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
                

            Bot.AttackWhitelist = true;
            Bot.AttackBlacklist = false;
            Bot.MonsterList.Clear();

            Bot.LootBlacklist = false;
            Bot.LootWhiteList = true;
            Bot.LootList.Clear();

            Bot.Path.Clear();

            Bot.MiniEnabled = false;

            api.stop_bot();
            api.load_settings(newpath);
            MessageBox.Show("Reloaded the original Phoenix Bot profile. DO NOT FORGET TO LOAD PROFILE TO MAS AGAIN", "Profile", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        private void SantaClawscheckBox_CheckedChanged(object sender, EventArgs e)
        {
            if (SantaClawscheckBox.Checked)
            {
                if (PandaCheckBox.Checked)
                    PandaCheckBox.Checked = false;
                if (!player.Pet.Skills.ContainsKey(1890))
                {
                    player.Pet.Skills.Clear();
                    player.Pet.Skills[1890] = true;
                }
            }

            Bot.SantaClaws = SantaClawscheckBox.Checked;
        }

        private void PandaCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            if (PandaCheckBox.Checked)
            {
                if (SantaClawscheckBox.Checked)
                    SantaClawscheckBox.Checked = false;
                if (!player.Pet.Skills.ContainsKey(1714))
                {
                    player.Pet.Skills.Clear();
                    player.Pet.Skills[1714] = true;
                }


            }

            Bot.Panda = PandaCheckBox.Checked;
        }

        private void numericUpDown1_ValueChanged(object sender, EventArgs e)
        {
            Bot.DelayMultipler = (double)numericUpDown1.Value;
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            Bot.SwordsmanSP1 = checkBox1.Checked;
        }

    }
}
