using Newtonsoft.Json.Linq;
using Multi_Account_Synchronizer;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO.Compression;
using System.Diagnostics;
using IniParser.Model;
using System.Drawing.Drawing2D;

namespace Multi_Account_Synchronizer
{
    public partial class Form1 : Form
    {
        string part;
        string realport;
        List<Tuple<PhoenixApi, JsonHandler, Player, Scene, Bot, BotForm>> apis = new List<Tuple<PhoenixApi, JsonHandler, Player, Scene, Bot, BotForm>>();
        PhoenixApi phoenixapi = new PhoenixApi();
        PortFinder portfinder = new PortFinder();
        public Form2 form2;
        public bool IsConnected = false;
        public Form1()
        {
            InitializeComponent();
        }
        public bool Connect(int port, string nickname)
        {
            IsConnected = phoenixapi.ConnectPort(port);
            part = port.ToString();
            realport = port.ToString();
            if (nickname != "")
                part = nickname;
            return IsConnected;
        }
        private async void Form1_Load(object sender, EventArgs e)
        {
            try
            {

                form2.ShowDialog();
                List<string> zurna = portfinder.find_ports();
                if (!IsConnected)
                    this.Close();
                foreach (RoundedButton button in tableLayoutPanel2.Controls.OfType<RoundedButton>())
                {
                    button.Enabled = false;
                }
                if (IsConnected)
                {

                    phoenixapi.Close();
                    await CreatePort(int.Parse(realport), part);
                }
                else
                    this.Close();

                await CheckPorts();
                timer1.Start();
                foreach (RoundedButton button in tableLayoutPanel2.Controls.OfType<RoundedButton>())
                {
                    button.Enabled = true;
                }
                AskDisconnectSecurity();
            }
            catch (Exception a)
            {
                MessageBox.Show(a.Message);
                this.Close();
            }
        }
        private async void AskDisconnectSecurity()
        {
            var result = MessageBox.Show("Would you like to enable disconnect security for buff accounts?\nOptions will be enabled:Sound, flash, message box, discord notification.", "Disconnect Secuirty", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (result == DialogResult.No)
                return;
            IniData data = new IniData();
            data["disconnect_security"]["make_sound"] = "true";
            data["disconnect_security"]["flash"] = "true";
            data["disconnect_security"]["message_box"] = "true";
            data["disconnect_security"]["discord"] = "true";
            data["Security"]["security_enabled"] = "true";
            string path = $"{Application.StartupPath}\\disconnect.ini";
            File.WriteAllText(path, data.ToString());
            foreach (var api in apis)
            {
                if (!api.Item5.MinilandOwner && !api.Item5.Buffer)
                    continue;
                api.Item1.load_settings(path);
            }
            await Task.Delay(5000);
            if (File.Exists(path))
                File.Delete(path);
        }
        private async Task CheckPorts()
        {
            if (!File.Exists("Members.json"))
                return;
            JObject Members = new JObject();
            using (StreamReader file = new StreamReader("Members.json"))
            {
                string json = file.ReadToEnd();
                Members = JObject.Parse(json);
            }
            List<string> members = new List<string>();
            var ports = portfinder.find_ports();
            foreach (var member in Members["Members"])
            {
                foreach (var port in ports)
                {
                    if (port.Split(' ').Length != 3)
                        continue;
                    if (port.Split(' ')[0] == member["name"].ToString())
                    {
                        members.Add(port);
                    }
                }
            }
            foreach (Label l in tableLayoutPanel1.Controls.OfType<Label>())
            {
                string s = members.Where(x => x.Contains(l.Text)).FirstOrDefault();
                if (s == null)
                    continue;
                members.Remove(s);
            }
            if (members.Count == 0)
                return;
            string accs = "";

            for (int i = 0; i < members.Count; i++)
            {
                string[] splitted_port = members[i].Split(' ');
                accs += splitted_port[0];
                if (i == members.Count - 1)
                    continue;
                accs += ", ";
            }
            var result = MessageBox.Show($"Accounts {accs} found in settings would you like to add these accounts?", "Multi Account Synchronizer", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (result == DialogResult.No)
                return;
            foreach (string port in members)
            {
                int pord = int.Parse(port.Split(' ')[1]);
                string name = port.Split(' ')[0];
                await CreatePort(pord, name);
            }
        }

        private int FindLowestEmptyRowIndex(TableLayoutPanel tableLayoutPanel)
        {
            for (int row = tableLayoutPanel.RowStyles.Count - 1; row >= 0; row--)
            {
                bool rowIsEmpty = true;

                for (int col = 0; col < tableLayoutPanel.ColumnStyles.Count; col++)
                {
                    if (tableLayoutPanel.GetControlFromPosition(col, row) != null)
                    {
                        rowIsEmpty = false;
                        break;
                    }
                }

                if (rowIsEmpty)
                {
                    return row;
                }
            }

            return -1;
        }

        private void UpdateRows()
        {
            int ba = -1;
            foreach (RowStyle row in tableLayoutPanel1.RowStyles)
            {
                row.Height = 100.0f / tableLayoutPanel1.RowStyles.Count;
            }
            for (int i = 0; i < apis.Count; i++)
            {
                apis[i].Item5.delayer = i + 1;
            }
            List<Control> controls = new List<Control>();
            foreach (Control c in tableLayoutPanel1.Controls)
            {
                int index = tableLayoutPanel1.GetRow(c);
                if (index >= tableLayoutPanel1.RowStyles.Count)
                {
                    controls.Add(c);
                }
            }
            if (controls.Count > 0)
                ba = FindLowestEmptyRowIndex(tableLayoutPanel1);
            foreach (Control c in controls)
            {

                if (c.GetType().Name == "Label")
                    tableLayoutPanel1.Controls.Add(c, 0, ba);
                else if (c.Text == "Settings")
                    tableLayoutPanel1.Controls.Add(c, 1, ba);
                else if (c.Text == "Remove account")
                    tableLayoutPanel1.Controls.Add(c, 2, ba);
            }
        }

        private async Task CreatePort(int port, string nickname)
        {
            int i = FindLowestEmptyRowIndex(tableLayoutPanel1);
            if (i == -1)
            {
                RowStyle r = new RowStyle();
                r.SizeType = SizeType.Percent;
                tableLayoutPanel1.RowStyles.Add(r);
                //updaterows();
                i = FindLowestEmptyRowIndex(tableLayoutPanel1);
            }
            PhoenixApi api = new PhoenixApi();
            JsonHandler handler = new JsonHandler();
            Player player = new Player();
            Scene scene = new Scene();
            Bot bot = new Bot();
            api.ConnectPort(port);
            handler.phoenixapi = api;
            handler.player = player;
            handler.scene = scene;
            handler.bot = bot;
            scene.player = player;
            player.phoenixapi = api;
            bot.Player = player;
            bot.Scene = scene;
            bot.Api = api;
            api.receive_message();
            handler.start();
            api.query_player_info();
            api.query_map_entities();
            BotForm b = new BotForm(bot, player, scene, api);

            Label l1 = new Label();
            l1.Text = nickname;
            l1.Name = nickname;
            l1.AutoSize = true;
            l1.Anchor = AnchorStyles.None;


            RoundedButton r1 = new RoundedButton();
            r1.Text = "Settings";
            r1.Name = $"Settings {nickname}";
            r1.Size = new Size(r1.Size.Width, 25);
            r1.MinimumSize = new Size(r1.Size.Width, 25);
            r1.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            r1.Click += (sender, e) =>
            {
                b.ShowDialog();
            };

            RoundedButton r2 = new RoundedButton();
            r2.Text = "Remove account";
            r2.Name = $"Remove account {nickname}";
            r2.Size = new Size(r2.Size.Width, 25);
            r2.MinimumSize = new Size(r2.Size.Width, 25);
            r2.Anchor = AnchorStyles.Left | AnchorStyles.Right;


            Tuple<PhoenixApi, JsonHandler, Player, Scene, Bot, BotForm> tuple = new Tuple<PhoenixApi, JsonHandler, Player, Scene, Bot, BotForm>(api, handler, player, scene, bot, b);
            r2.Click += (sender, e) =>
            {
                handler.run = false;
                api.run = false;
                api.Close();
                bot.run = false;
                int index = tableLayoutPanel1.GetRow(l1);
                tableLayoutPanel1.Controls.Remove(r1);
                tableLayoutPanel1.Controls.Remove(l1);
                tableLayoutPanel1.Controls.Remove(r2);
                tableLayoutPanel1.RowStyles.RemoveAt(index);

                apis.Remove(tuple);
                UpdateRows();
                api = null;
                handler = null;
                player = null;
                scene = null;
                bot = null;
            };

            tableLayoutPanel1.Controls.Add(l1, 0, i);
            tableLayoutPanel1.Controls.Add(r1, 1, i);
            tableLayoutPanel1.Controls.Add(r2, 2, i);
            apis.Add(tuple);
            UpdateRows();
            //tabControl1.TabPages.Add(tabpage);
            await UpdateRoles(bot, player, handler, scene, api, b);
            Settings s = new Settings(apis, false);
        }
        private async Task UpdateRoles(Bot bot, Player player, JsonHandler jsonhandler, Scene scene, PhoenixApi api, BotForm b)
        {
            if (!File.Exists("Members.json"))
                return;

            while (player.Id <= 0 || player.Name == "")
                await Task.Delay(100);
            bot.Random = new Random(player.Id);
            JObject Members = new JObject();
            Random random = new Random(player.Id);
            RadioButton DPS = b.radioButton1;
            RadioButton Buffer = b.radioButton2;
            RadioButton MinilandOwner = b.radioButton3;
            NumericUpDown DelayMultipler = b.numericUpDown1;
            CheckBox SantaClaws = b.SantaClawscheckBox;
            CheckBox Panda = b.PandaCheckBox;
            CheckBox swordsmanSP1 = b.checkBox1;
            string path = "";
            using (StreamReader file = new StreamReader("Members.json"))
            {
                string json = file.ReadToEnd();
                Members = JObject.Parse(json);
            }
            int invitemin = Statics.JsonGetValueOrDefault(Members["General Settings"], "Accept Invite Min", 1000);
            int invitemax = Statics.JsonGetValueOrDefault(Members["General Settings"], "Accept Invite Max", 2000);
            int exitmin = Statics.JsonGetValueOrDefault(Members["General Settings"], "Exit Miniland Min", 750);
            int exitmax = Statics.JsonGetValueOrDefault(Members["General Settings"], "Exit Miniland Max", 2000);
            int useamuletmin = Statics.JsonGetValueOrDefault(Members["General Settings"], "Use Amulet Min", 750);
            int useamuletmax = Statics.JsonGetValueOrDefault(Members["General Settings"], "Use Amulet Max", 1450);
            int attackluremin = Statics.JsonGetValueOrDefault(Members["General Settings"], "Attack Lure Min", 1500);
            int attackluremax = Statics.JsonGetValueOrDefault(Members["General Settings"], "Attack Lure Max", 2100);
            int delayAfterKillMin = Statics.JsonGetValueOrDefault(Members["General Settings"], "After Kill Point Min", 400);
            int delayAfterKillMax = Statics.JsonGetValueOrDefault(Members["General Settings"], "After Kill Point Max", 850);
            int normalFlower = Statics.JsonGetValueOrDefault(Members["General Settings"], "Normal Flower", 420);
            int strongFlower = Statics.JsonGetValueOrDefault(Members["General Settings"], "Strong Flower", 40);
            string inviteCommand = Statics.JsonGetValueOrDefault(Members["General Settings"], "Invite Command", "");
            int vokeDelay = Statics.JsonGetValueOrDefault(Members["General Settings"], "Voke Delay", 750);
            int minMobCountVoke = Statics.JsonGetValueOrDefault(Members["General Settings"], "Min Monster Count For Voke", 6);
            bool trashItems = Statics.JsonGetValueOrDefault(Members["General Settings"], "Loot Trash Items", true);
            int trashItemsChance = Statics.JsonGetValueOrDefault(Members["General Settings"], "Trash Items Chance", 25);
            double ignoreVokeRadius = Statics.JsonGetValueOrDefault(Members["General Settings"], "Ignore Voke Radius", 1.0);
            if (inviteCommand != "" && !Statics.InviteCommands.ContainsKey(inviteCommand))
            {
                inviteCommand = "";
            }
            Tuple<int, int> invite = new Tuple<int, int>(invitemin, invitemax);
            Tuple<int, int> exit = new Tuple<int, int>(exitmin, exitmax);
            Tuple<int, int> amulet = new Tuple<int, int>(useamuletmin, useamuletmax);
            Tuple<int, int> attack = new Tuple<int, int>(attackluremin, attackluremax);
            Tuple<int, int> delayAfterKill = new Tuple<int, int>(delayAfterKillMin, delayAfterKillMax);
            bot.AcceptInviteDelay = invite;
            bot.MinilandExitDelay = exit;
            bot.AmuletUseDelay = amulet;
            bot.StartAttackDelay = attack;
            bot.DelayAfterKillPoint = delayAfterKill;
            bot.InviteCommand = inviteCommand;
            bot.NormalFlowerUsage = normalFlower;
            bot.StrongFlowerUsage = strongFlower;
            bot.VokeDelay = vokeDelay;
            bot.MinVokeMonsterCount = minMobCountVoke;
            bot.TrashItems = trashItems;
            bot.TrashItemsChance = trashItemsChance;
            bot.IgnoreVokeRadius = ignoreVokeRadius;
            foreach (var member in Members["Members"])
            {

                if (((int)member["id"]) == player.Id)
                {
                    DPS.Checked = Statics.JsonGetValueOrDefault(member, "DPS", false);
                    Buffer.Checked = Statics.JsonGetValueOrDefault(member, "Buffer", true);
                    MinilandOwner.Checked = Statics.JsonGetValueOrDefault(member, "Miniland Owner", false);
                    SantaClaws.Checked = Statics.JsonGetValueOrDefault(member, "SantaClaws", false);
                    Panda.Checked = Statics.JsonGetValueOrDefault(member, "Panda", false);
                    swordsmanSP1.Checked = Statics.JsonGetValueOrDefault(member, "Swordsman SP1", false);
                    DelayMultipler.Value = Convert.ToDecimal(Statics.JsonGetValueOrDefault(member, "Delay Multipler", 1.0));
                    path = Statics.JsonGetValueOrDefault(member, "Path", "");
                    if (path != "")
                    {
                        var result = MessageBox.Show($"Profile {path} found in settings for {player.Name} do you want to load it?", "QUESTION", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                        if (result == DialogResult.Yes)
                        {
                            b.textBox2.Text = path;
                            bot.ReadIni(path);
                            bot.CreateNewIni(path);
                        }

                    }
                    foreach (var item in member["buffs"])
                    {
                        Tuple<string, int> t = new Tuple<string, int>(item["name"].ToString(), ((int)item["id"]));
                        bot.Buffs.Add(t);
                        b.list.Items.Add(t.Item1);
                    }
                    if (member["Partner Buffs"] != null)
                    {
                        foreach (var item in member["Partner Buffs"])
                        {
                            b.list.Items.Add(item["Partner Skill"].ToString());
                            char skill = item["Partner Skill"].ToString()[14];
                            switch (skill)
                            {
                                case 'Q':
                                    bot.PartnerBuffs.Add(0);
                                    break;
                                case 'W':
                                    bot.PartnerBuffs.Add(1);
                                    break;
                                case 'E':
                                    bot.PartnerBuffs.Add(2);
                                    break;
                                default:
                                    break;

                            }
                        }
                    }
                    
                }
            }
        }



        private void roundedButton1_Click(object sender, EventArgs e)
        {
            int killpointcount = -1;
            var ab = apis.Where(x => x.Item5.DPS && x.Item5.Path.Count(y => y.Kill) > 0).FirstOrDefault();
            if (ab != null)
                killpointcount = ab.Item5.Path.Count(y => y.Kill);
            if (apis.Count(y => y.Item5.InviteCommand == "") > 0)
            {
                var allinvites = Statics.InviteCommands.ToList();
                string allinvitestext = "";
                foreach (var invite in allinvites)
                {
                    allinvitestext += $"{invite.Value}={invite.Key}, ";
                }
                allinvitestext = allinvitestext.Remove(allinvitestext.Length - 2);
                MessageBox.Show($"Invite command is empty. Go to settings and choose it.\nHere is the commands for every language:\n{allinvitestext}", "WARNING", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            else if (apis.Count(x => x.Item5.DPS && x.Item5.Path.Count == 0) > 0)
            {
                string accs = "";
                foreach (var a in apis.Where(x => x.Item5.DPS && x.Item5.Path.Count == 0))
                {
                    accs += $"{a.Item3.Name}, ";
                }
                accs = accs.Remove(accs.Length - 2);
                MessageBox.Show($"Accounts {accs} has 0 walking points in path. Check Phoenix Bot profiles.", "WARNING", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            else if (apis.Count(x => x.Item5.Path.Count(a => a.Kill) != killpointcount && x.Item5.DPS) > 0 && killpointcount != -1)
            {
                string points = "";
                foreach (var item in apis.Where(x => x.Item5.DPS))
                {
                    if (!points.Contains(item.Item3.Name))
                        points += $"{item.Item3.Name}:";
                    List<WalkPoint> wpoints = item.Item5.Path.Where(x => x.Kill).ToList();
                    foreach (WalkPoint p in wpoints)
                    {
                        points += $" X:{p.X} | Y:{p.Y},";
                    }
                    if (points.Last() == ',')
                    {
                        points = points.Remove(points.Length - 1);
                        points += $" points count {item.Item5.Path.Count(x => x.Kill)}";
                    }


                    points += "\n";
                }
                MessageBox.Show($"Kill point count is different on DPS accounts bot cannot work(original kill point count = {killpointcount}).\nCheck phoenix bot profiles. The kill points:\n{points}", "WARNING", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            else if (apis.Count(x => x.Item5.DPS && x.Item5.AttackWhitelist && x.Item5.MonsterList.Count == 0) > 0)
            {
                string accs = "";
                foreach (var a in apis.Where(x => x.Item5.DPS && x.Item5.AttackWhitelist && x.Item5.MonsterList.Count == 0))
                {
                    accs += $"{a.Item3.Name}, ";
                }
                accs = accs.Remove(accs.Length - 2);
                MessageBox.Show($"Accounts {accs} is on whitelist mode in combat and has 0 monsters in it. Check Phoenix Bot profiles.", "WARNING", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            else if (apis.Count > 0 && apis.Count(x => x.Item5.run) == 0)
            {
                var api = apis.FirstOrDefault();
                if (api == null)
                    return;
                var result = MessageBox.Show($"Miniland invite command is '{api.Item5.InviteCommand}' are you sure about starting the bot?", "QUESTION", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (result == DialogResult.No)
                    return;
            }
            else if (apis.Count(x => x.Item5.MinilandOwner) > 0 && apis.Count(x => x.Item5.MiniEnabled) <= 0)
            {
                var result = MessageBox.Show("There is miniland owner but no accs have miniland option enabled. Are you sure about starting the bot?","QUESTION",MessageBoxButtons.YesNo,MessageBoxIcon.Question);
                if (result == DialogResult.No)
                    return;
            }
            this.Text = "MAS by Diartios1881 | Running";
            apis.ForEach(x => {
                if (!x.Item5.run)
                {

                    x.Item5.AddLog("Bot started", "Information");
                }
                x.Item5.StopAllBots = false;
                x.Item5.LastPath = -1;
                x.Item5.run = true;
                x.Item5.Start();
            });
        }

        private void roundedButton2_Click(object sender, EventArgs e)
        {
            if (!this.Text.Contains("Idle"))
                this.Text = "MAS by Diartios1881 | Stopped";
            apis.ForEach(x => {
                if (x.Item5.run)
                {
                    x.Item5.AddLog("Bot stopped", "Information");
                }
                x.Item5.run = false;
                x.Item5.WorkingTimeSw.Stop();
            });
        }

        private void roundedButton3_Click(object sender, EventArgs e)
        {


            AddPortForm addPortForm = new AddPortForm();
            foreach (Label l in tableLayoutPanel1.Controls.OfType<Label>())
            {
                addPortForm.portList.Add(l.Text);
            }
            addPortForm.ShowDialog();
            if (addPortForm.IsConnected)
            {
                CreatePort(int.Parse(addPortForm.port), addPortForm.part);
                addPortForm = null;
            }
        }

        private void roundedButton4_Click(object sender, EventArgs e)
        {
            if (apis.Count == 0)
            {
                MessageBox.Show("There isn't any account", "Save Settings", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var a = apis.FirstOrDefault();
            JObject content = new JObject();
            JArray players = new JArray();
            JObject generalSettings = new JObject();
            generalSettings.Add("Accept Invite Min", a.Item5.AcceptInviteDelay.Item1);
            generalSettings.Add("Accept Invite Max", a.Item5.AcceptInviteDelay.Item2);
            generalSettings.Add("Exit Miniland Min", a.Item5.MinilandExitDelay.Item1);
            generalSettings.Add("Exit Miniland Max", a.Item5.MinilandExitDelay.Item2);
            generalSettings.Add("Use Amulet Min", a.Item5.AmuletUseDelay.Item1);
            generalSettings.Add("Use Amulet Max", a.Item5.AmuletUseDelay.Item2);
            generalSettings.Add("Attack Lure Min", a.Item5.StartAttackDelay.Item1);
            generalSettings.Add("Attack Lure Max", a.Item5.StartAttackDelay.Item2);
            generalSettings.Add("After Kill Point Min", a.Item5.DelayAfterKillPoint.Item1);
            generalSettings.Add("After Kill Point Max", a.Item5.DelayAfterKillPoint.Item2);
            generalSettings.Add("Normal Flower", a.Item5.NormalFlowerUsage);
            generalSettings.Add("Strong Flower", a.Item5.StrongFlowerUsage);
            generalSettings.Add("Invite Command", a.Item5.InviteCommand);
            generalSettings.Add("Voke Delay", a.Item5.VokeDelay);
            generalSettings.Add("Loot Trash Items", a.Item5.TrashItems);
            generalSettings.Add("Trash Items Chance", a.Item5.TrashItemsChance);
            generalSettings.Add("Min Monster Count For Voke", a.Item5.MinVokeMonsterCount);
            generalSettings.Add("Ignore Voke Radius", a.Item5.IgnoreVokeRadius);
            content["General Settings"] = generalSettings;
            foreach (var api in apis)
            {
                JArray buffs = new JArray();
                JArray partnerBuffs = new JArray();
                foreach (var item in api.Item5.Buffs)
                {
                    JObject i = new JObject();
                    int id = item.Item2;
                    string name = item.Item1;
                    i.Add("id", id);
                    i.Add("name", name);
                    buffs.Add(i);
                }
                foreach (var item in api.Item5.PartnerBuffs)
                {
                    JObject i = new JObject();
                    string name = "";
                    if (item == 0)
                        name = "Partner Skill Q";
                    else if (item == 1)
                        name = "Partner Skill W";
                    else
                        name = "Partner Skill E";
                    i.Add("Partner Skill", name);
                    partnerBuffs.Add(i);
                }
                JObject newItem = new JObject();
                newItem.Add("name", api.Item3.Name);
                newItem.Add("id", api.Item3.Id);
                newItem.Add("DPS", api.Item5.DPS);
                newItem.Add("Miniland Owner", api.Item5.MinilandOwner);
                newItem.Add("Buffer", api.Item5.Buffer);
                newItem.Add("Delay Multipler", ((double)api.Item6.numericUpDown1.Value));
                newItem.Add("SantaClaws", api.Item5.SantaClaws);
                newItem.Add("Panda", api.Item5.Panda);
                newItem.Add("Swordsman SP1", api.Item5.SwordsmanSP1);
                newItem.Add("Path", api.Item6.textBox2.Text);
                newItem["buffs"] = buffs;
                newItem["Partner Buffs"] = partnerBuffs;
                players.Add(newItem);
                content["Members"] = players;
            }
            File.WriteAllText("Members.json", content.ToString());
            MessageBox.Show("Settings saved!", "Settings", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private async void timer1_Tick(object sender, EventArgs e)
        {
            if (apis.Count <= 0)
                return;
            string s = "Idle";
            if (this.Text.Contains("Idle"))
                s = "Idle";
            else if (apis.Count(x => x.Item5.run) > 0)
                s = "Running";
            else if (apis.Count(x => !x.Item5.run) > 0)
                s = "Stopped";
            this.Text = $"MAS by Diartios1881 | {s}";
            bool stopbots = false;
            if (apis.Count(x => x.Item5.StopAllBots) > 0)
            {
                stopbots = true;
            }
            var MinilandOwner = apis.Where(x => x.Item5.MinilandOwner).FirstOrDefault();

            bool timetobuff = false;
            bool invite = false;
            bool leavemini = false;
            bool entermini = false;
            string minilandownernick = "";

            if (MinilandOwner != null)
            {
                MinilandOwner.Item3.updated = false;
                MinilandOwner.Item1.query_inventory();
                Stopwatch sw = Stopwatch.StartNew();
                while (!MinilandOwner.Item3.updated && sw.Elapsed.TotalSeconds <= 5)
                    await Task.Delay(1);
                int seedofpowercount = MinilandOwner.Item3.Inventory.Where(x => x.Vnum == 1012).Sum(x => x.Quantity);
                MinilandOwner.Item5.DPSAccounts = apis.Where(x => x.Item5.DPS && (x.Item5.MiniEnabled || x.Item5.MiniOnWaypoint)).Select(x => x.Item3.Name).ToList();
                timetobuff = apis.Count(x => x.Item3.MapId == 20001) == apis.Count();
                invite = apis.Count(x => x.Item5.WaitingForMiniland && x.Item5.DPS && (x.Item5.MiniEnabled || x.Item5.MiniOnWaypoint)) == apis.Count(x => x.Item5.DPS && (x.Item5.MiniEnabled || x.Item5.MiniOnWaypoint));
                leavemini = apis.Count(x => x.Item5.Buffing) == 0;
                minilandownernick = MinilandOwner.Item3.Name;
                entermini = (apis.Count(x => x.Item5.Minilandsw.Elapsed.TotalSeconds >= x.Item5.MinilandInterval || x.Item5.Minilandsw.Elapsed.TotalSeconds == 0) > 0 || apis.Count(x => x.Item5.MiniOnWaypoint && x.Item5.LastPath == x.Item5.MiniWaypointIndex && x.Item5.DPS) == apis.Count(x => x.Item5.DPS)) && seedofpowercount >= apis.Count(x => x.Item5.DPS && x.Item5.MiniEnabled);

            }

            bool updatemini = apis.Count(x => x.Item5.UpdateBuff && x.Item5.MiniEnabled) == apis.Count(x => x.Item5.DPS && x.Item5.MiniEnabled);
            bool shouldStop = apis.Count(x => x.Item5.DPS && x.Item5.StopAfterMin > 0 && x.Item5.WorkingTimeSw.Elapsed.TotalMinutes >= x.Item5.StopAfterMin) > 0 && entermini;
            bool gonextlure = apis.Count(x => x.Item5.Finished && x.Item5.DPS) == apis.Count(x => x.Item5.DPS);
            bool startkill = apis.Count(x => x.Item5.DPS && x.Item5.ReachedKillPoint) == apis.Count(x => x.Item5.DPS);
            List<int> luremobs = apis.Where(x => x.Item5.DPS).Select(x => x.Item4.CenterMob(x.Item5.AttackSearchRadius, x.Item5.AttackBlacklist, x.Item5.AttackWhitelist, x.Item5.MonsterList, x.Item5.Priority)).ToList();
            bool reset = apis.Count(x => x.Item5.DPS && x.Item5.ResetMinilandsw && x.Item5.MiniEnabled) == apis.Count(x => x.Item5.DPS && x.Item5.MiniEnabled) && apis.Count(x => x.Item5.DPS && x.Item5.MiniEnabled) > 0;
            int luremob = -1;
            if (luremobs.Count(x => x > 0) > 0)
                luremob = luremobs.Where(x => x > 0).FirstOrDefault();

            bool HelpAmulet = apis.Count(x => x.Item5.DPS && x.Item5.NeedHelp) > 0;
            List<int> defencemobs = apis.Where(x => x.Item5.DPS && x.Item5.NeedHelp).Select(x => x.Item5.HelpNeededMobId).ToList();
            int defencemob = -1;
            if (defencemobs.Count(x => x > 0) > 0)
                defencemob = defencemobs.Where(x => x > 0).FirstOrDefault();

            int samekeydelay = 200;
            int differentkeydelay = 2000;
            var a = apis.Where(x => x.Item5.DelaySameKey != 200 || x.Item5.DelayDifferentKey != 2000).FirstOrDefault();
            if (a != null)
            {
                samekeydelay = a.Item5.DelaySameKey;
                differentkeydelay = a.Item5.DelayDifferentKey;
            }
            bool updateVoke = apis.Count(x => x.Item5.UpdateVoke) == apis.Count(x => x.Item5.DPS);
            var vokeAcc = apis.Where(x => x.Item5.DPS && x.Item5.IsVokeReady()).FirstOrDefault();
            if (vokeAcc != null)
            {
            }
            if (updateVoke && vokeAcc != null)
            {

                vokeAcc.Item5.UpdateVoke = false;
                vokeAcc.Item5.UseVoke = true;
            }
            foreach (var api in apis)
            {
                if (stopbots)
                {   
                    api.Item5.StopAllBots = false;
                    api.Item5.run = false;
                    api.Item5.AddLog("Bot stopped due to security reasons", "Security");
                    api.Item5.WorkingTimeSw.Stop();
                }
                api.Item5.DefenceMob = defencemob;
                api.Item5.HelpAmulet = HelpAmulet;
                api.Item5.StartBuff = timetobuff;
                api.Item5.Invite = invite;
                api.Item5.GoNextLure = gonextlure;
                api.Item5.LeaveMiniland = leavemini;
                api.Item5.LureMob = luremob;
                api.Item5.StartAttack = startkill;
                api.Item5.OwnerName = minilandownernick;
                api.Item5.DelayDifferentKey = differentkeydelay;
                api.Item5.DelaySameKey = samekeydelay;
                api.Item5.ShouldStop = shouldStop;
                if (reset)
                {
                    api.Item5.Minilandsw.Restart();
                    api.Item5.ResetMinilandsw = false;
                }
                if (updatemini)
                {
                    api.Item5.EnterMini = entermini;
                    api.Item5.UpdateBuff = false;

                }
                if (updateVoke && vokeAcc != null)
                {
                    api.Item5.UpdateVoke = false;
                }
            }
        }

        private void roundedButton5_Click(object sender, EventArgs e)
        {
            Settings s = new Settings(apis);
            s.ShowDialog();
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (apis.Count(x => x.Item5.run) > 0)
            {
                var result = MessageBox.Show("Phoenix Bots might be still running. Would you like to stop them?", "QUESTION", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (result == DialogResult.Yes)
                {
                    apis.ForEach(x => x.Item1.stop_bot());
                }
            }


        }

        private void roundedButton6_Click(object sender, EventArgs e)
        {
            if (this.Text.Contains("Idle") || this.Text.Contains("Running"))
            {
                return;
            }
            this.Text = "MAS by Diartios1881 | Running";
            foreach (var api in apis)
            {
                if (!api.Item5.run)
                {

                    api.Item5.AddLog("Bot resumed", "Information");
                }
                api.Item5.StopAllBots = false;
                api.Item5.run = true;
                api.Item1.start_bot();
                api.Item5.WorkingTimeSw.Start();
            }
        }
    }
}
