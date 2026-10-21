// =====================================================================
//  IT Help Desk Lite  -  free IT ticket app for Windows employees
//  Copyright (c) 2026 Rehan Jamal  -  https://github.com/Rehanjamal-IT
//
//  The user fills who they are (once), picks a problem type, describes it,
//  attaches photos / screenshots and presses Send. It e-mails the IT team
//  a structured ticket with subject [IT-TICKET] ...
//  Computer name + hardware are detected automatically and added.
//  No server needed - a single small .exe.
//  Build with BUILD.bat (uses the C# compiler already inside Windows).
//
//  Free to use. Do NOT remove the author credits or publish this app
//  under another name - see LICENSE.
// =====================================================================
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Management;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;

[assembly: AssemblyTitle("IT Help Desk Lite")]
[assembly: AssemblyDescription("Free IT ticket app for Windows - by Rehan Jamal")]
[assembly: AssemblyProduct("IT Help Desk Lite")]
[assembly: AssemblyCompany("Rehan Jamal")]
[assembly: AssemblyCopyright("Copyright (c) 2026 Rehan Jamal")]
[assembly: AssemblyVersion("1.1.0.0")]
[assembly: AssemblyFileVersion("1.1.0.0")]

namespace ITHelpDeskLite
{
    // ---------------------------------------------------------------- author (please keep)
    static class Author
    {
        public const string App      = "IT Help Desk Lite";
        public const string Version  = "1.1.0";
        public const string Name     = "Rehan Jamal";
        public const string Title    = "IT Manager / Senior Systems Administrator";
        public const string Years    = "16+ years in IT  ·  IT for 450+ users";
        public const string LinkedIn = "https://www.linkedin.com/in/rehan-jamal/";
        public const string GitHub   = "https://github.com/Rehanjamal-IT";
        public const string Repo     = "https://github.com/Rehanjamal-IT/it-helpdesk-lite";
        public const string Credit   = "IT Help Desk Lite - free app by Rehan Jamal";
    }

    static class Theme
    {
        public static readonly Color Navy = Color.FromArgb(11, 37, 69);
        public static readonly Color Gold = Color.FromArgb(245, 180, 0);
        public static readonly Color Soft = Color.FromArgb(120, 128, 140);
        public static readonly Color Back = Color.FromArgb(246, 248, 251);
    }

    static class Paths
    {
        public static string DataDir()
        {
            string dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "IT Help Desk");
            try { Directory.CreateDirectory(dir); } catch { }
            return dir;
        }
    }

    // ---------------------------------------------------------------- settings
    //  1) built-in defaults  2) ithelp.ini next to the .exe (IT can pre-fill it)
    //  3) settings.ini in %AppData%\IT Help Desk (saved from the Settings tab)
    static class Settings
    {
        public static string Company = "Your Company";
        public static string Team    = "IT Support";
        public static string ItPhone = "";
        public static string L1Name  = "Level 1 - Help Desk";
        public static string L1Email = "helpdesk@yourcompany.com";
        public static string L2Name  = "Level 2 - IT Manager";
        public static string L2Email = "it-manager@yourcompany.com";
        public static string PinHash = "";

        public static bool IsConfigured()
        {
            return !L1Email.ToLower().EndsWith("@yourcompany.com") && L1Email.Contains("@");
        }

        static string UserFile() { return Path.Combine(Paths.DataDir(), "settings.ini"); }

        public static void Load()
        {
            try { Read(Path.Combine(Path.GetDirectoryName(Application.ExecutablePath), "ithelp.ini")); } catch { }
            try { Read(UserFile()); } catch { }
        }

        static void Read(string file)
        {
            if (!File.Exists(file)) return;
            foreach (string raw in File.ReadAllLines(file))
            {
                string line = raw.Trim();
                int eq = line.IndexOf('=');
                if (eq <= 0 || line.StartsWith(";")) continue;
                string k = line.Substring(0, eq).Trim().ToLower();
                string v = line.Substring(eq + 1).Trim();
                if (k == "pin_hash") { PinHash = v; continue; }
                if (v.Length == 0) continue;
                if (k == "company") Company = v;
                else if (k == "team") Team = v;
                else if (k == "it_phone") ItPhone = v;
                else if (k == "level1_name") L1Name = v;
                else if (k == "level1_email") L1Email = v;
                else if (k == "level2_name") L2Name = v;
                else if (k == "level2_email") L2Email = v;
                else if (k == "it_email" && L2Email.EndsWith("@yourcompany.com")) L2Email = v;   // old files
            }
        }

        public static bool Save()
        {
            try
            {
                StringBuilder b = new StringBuilder();
                b.AppendLine("; " + Author.Credit + " - saved settings");
                b.AppendLine("company=" + Company);
                b.AppendLine("team=" + Team);
                b.AppendLine("it_phone=" + ItPhone);
                b.AppendLine("level1_name=" + L1Name);
                b.AppendLine("level1_email=" + L1Email);
                b.AppendLine("level2_name=" + L2Name);
                b.AppendLine("level2_email=" + L2Email);
                b.AppendLine("pin_hash=" + PinHash);
                File.WriteAllText(UserFile(), b.ToString());
                return true;
            }
            catch { return false; }
        }

        public static string Hash(string pin)
        {
            using (SHA256 s = SHA256.Create())
            {
                byte[] h = s.ComputeHash(Encoding.UTF8.GetBytes("ithelp-lite:" + pin));
                return BitConverter.ToString(h).Replace("-", "").ToLower();
            }
        }
    }

    // ---------------------------------------------------------------- automatic system info
    static class SysInfo
    {
        public static string ComputerName = "", WinUser = "", OS = "", Maker = "", Model = "",
                             Serial = "", Cpu = "", RamGb = "", Ip = "";
        static bool loaded;

        public static void Collect()
        {
            if (loaded) return;
            loaded = true;
            try { ComputerName = Environment.MachineName; } catch { }
            try { WinUser = Environment.UserName; } catch { }
            OS = WmiOne("SELECT Caption FROM Win32_OperatingSystem", "Caption");
            if (OS.Length == 0) { try { OS = Environment.OSVersion.VersionString; } catch { } }
            Maker = WmiOne("SELECT Manufacturer FROM Win32_ComputerSystem", "Manufacturer");
            Model = WmiOne("SELECT Model FROM Win32_ComputerSystem", "Model");
            Serial = WmiOne("SELECT SerialNumber FROM Win32_BIOS", "SerialNumber");
            Cpu = WmiOne("SELECT Name FROM Win32_Processor", "Name");
            string ramBytes = WmiOne("SELECT TotalPhysicalMemory FROM Win32_ComputerSystem", "TotalPhysicalMemory");
            try { if (ramBytes.Length > 0) RamGb = Math.Round(Convert.ToUInt64(ramBytes) / 1073741824.0) + " GB"; } catch { }
            Ip = LocalIp();
        }

        static string WmiOne(string query, string prop)
        {
            try
            {
                using (ManagementObjectSearcher s = new ManagementObjectSearcher(query))
                    foreach (ManagementObject o in s.Get())
                    {
                        object v = o[prop];
                        if (v != null) return v.ToString().Trim();
                    }
            }
            catch { }
            return "";
        }

        static string LocalIp()
        {
            try
            {
                foreach (NetworkInterface ni in NetworkInterface.GetAllNetworkInterfaces())
                {
                    if (ni.OperationalStatus != OperationalStatus.Up) continue;
                    if (ni.NetworkInterfaceType == NetworkInterfaceType.Loopback) continue;
                    foreach (UnicastIPAddressInformation a in ni.GetIPProperties().UnicastAddresses)
                        if (a.Address.AddressFamily == AddressFamily.InterNetwork)
                            return a.Address.ToString();
                }
            }
            catch { }
            return "";
        }

        public static string Block()
        {
            StringBuilder b = new StringBuilder();
            b.AppendLine("Computer: " + ComputerName + "   (Windows user: " + WinUser + ")");
            if (Maker.Length > 0 || Model.Length > 0) b.AppendLine("Make / model: " + (Maker + " " + Model).Trim());
            if (Serial.Length > 0) b.AppendLine("Serial / service tag: " + Serial);
            if (OS.Length > 0) b.AppendLine("Windows: " + OS);
            if (Cpu.Length > 0) b.AppendLine("CPU: " + Cpu);
            if (RamGb.Length > 0) b.AppendLine("RAM: " + RamGb);
            if (Ip.Length > 0) b.AppendLine("IP address: " + Ip);
            return b.ToString();
        }
    }

    // ---------------------------------------------------------------- profile
    class Profile
    {
        public string First = "", Last = "", Dept = "", Phone = "", Email = "", Location = "";

        public string FullName() { return (First.Trim() + " " + Last.Trim()).Trim(); }

        public bool EmailOk() { return UI.EmailOk(Email); }
        public bool PhoneOk()
        {
            int digits = 0;
            foreach (char c in Phone) if (char.IsDigit(c)) digits++;
            return digits >= 7 && digits <= 15;
        }
        public bool Ok() { return First.Trim().Length > 0 && Dept.Trim().Length > 0 && EmailOk() && PhoneOk(); }

        static string FilePath() { return Path.Combine(Paths.DataDir(), "profile.ini"); }

        public static Profile Load()
        {
            Profile p = new Profile();
            try
            {
                string f = FilePath();
                if (File.Exists(f))
                    foreach (string line in File.ReadAllLines(f))
                    {
                        int eq = line.IndexOf('=');
                        if (eq <= 0) continue;
                        string k = line.Substring(0, eq).Trim(), v = line.Substring(eq + 1).Trim();
                        if (k == "first") p.First = v; else if (k == "last") p.Last = v;
                        else if (k == "dept") p.Dept = v; else if (k == "phone") p.Phone = v;
                        else if (k == "email") p.Email = v; else if (k == "location") p.Location = v;
                    }
            }
            catch { }
            return p;
        }

        public void Save()
        {
            try
            {
                File.WriteAllText(FilePath(),
                    "first=" + First + "\r\nlast=" + Last + "\r\ndept=" + Dept + "\r\nphone=" + Phone +
                    "\r\nemail=" + Email + "\r\nlocation=" + Location + "\r\n");
            }
            catch { }
        }
    }

    // ---------------------------------------------------------------- problem types
    class Kind
    {
        public string Label, Key;
        public Kind(string l, string k) { Label = l; Key = k; }
        public override string ToString() { return Label; }
        public static Kind[] All = new Kind[] {
            new Kind("💻 Computer / Laptop", "Hardware"),
            new Kind("🔑 Password / Login", "Password / Account"),
            new Kind("📧 Email / Outlook", "Email / Outlook"),
            new Kind("🌐 Internet / Wi-Fi", "Network / Internet"),
            new Kind("🖨 Printer / Scanner", "Printer / Scanner"),
            new Kind("📦 Software / App", "Software / Install"),
            new Kind("📁 Shared folder / Access", "Access / Permissions"),
            new Kind("👥 Teams / Office 365", "Microsoft 365 / Teams"),
            new Kind("🔒 VPN / Remote", "VPN / Remote"),
            new Kind("🖥 Other / New user", "Other"),
            new Kind("⚠ Suspicious email / Virus", "Security"),
        };
    }

    // ---------------------------------------------------------------- small UI helpers
    //  Everything is stacked with FlowLayoutPanels, so Windows measures the real text size.
    //  Nothing is cut off, at any screen size or Windows zoom level (100% - 250%).
    static class UI
    {
        public static bool EmailOk(string e)
        {
            return System.Text.RegularExpressions.Regex.IsMatch(
                (e ?? "").Trim(), @"^[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}$");
        }

        public static FlowLayoutPanel Stack(Color back)
        {
            FlowLayoutPanel f = new FlowLayoutPanel();
            f.FlowDirection = FlowDirection.TopDown; f.WrapContents = false;
            f.AutoSize = true; f.AutoSizeMode = AutoSizeMode.GrowAndShrink; f.BackColor = back;
            return f;
        }

        public static FlowLayoutPanel Row()
        {
            FlowLayoutPanel f = new FlowLayoutPanel();
            f.FlowDirection = FlowDirection.LeftToRight; f.WrapContents = false;
            f.AutoSize = true; f.AutoSizeMode = AutoSizeMode.GrowAndShrink; f.Margin = new Padding(0);
            return f;
        }

        // Scrolling content (fills the window) + a button bar at the bottom that is ALWAYS visible.
        public static FlowLayoutPanel ScrollArea(Control parent, out FlowLayoutPanel bar)
        {
            bar = new FlowLayoutPanel();
            bar.Dock = DockStyle.Bottom; bar.FlowDirection = FlowDirection.RightToLeft; bar.WrapContents = false;
            bar.AutoSize = true; bar.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            bar.BackColor = Theme.Back; bar.Padding = new Padding(12, 10, 12, 10);
            FlowLayoutPanel area = new FlowLayoutPanel();
            area.Dock = DockStyle.Fill; area.FlowDirection = FlowDirection.TopDown; area.WrapContents = false;
            area.AutoScroll = true; area.BackColor = Color.White; area.Padding = new Padding(16, 12, 16, 16);
            parent.Controls.Add(bar);
            parent.Controls.Add(area);
            area.BringToFront();
            return area;
        }

        public static Label Text(Control parent, string text, int width, bool bold, Color color, int topGap)
        {
            Label l = new Label(); l.AutoSize = true; l.MaximumSize = new Size(width, 0); l.Text = text;
            l.ForeColor = color; l.Margin = new Padding(3, topGap, 3, 3);
            if (bold) l.Font = new Font(parent.Font, FontStyle.Bold);
            parent.Controls.Add(l);
            return l;
        }

        public static Label Heading(Control parent, string text, int width)
        {
            return Text(parent, text, width, true, Color.Black, 12);
        }

        public static TextBox Field(Control parent, string label, string val, int width)
        {
            Text(parent, label, width, false, Color.Black, 6);
            TextBox t = new TextBox(); t.Width = width; t.Text = val; t.Margin = new Padding(3, 0, 3, 6);
            parent.Controls.Add(t);
            return t;
        }

        public static Button Btn(string text, Color back, Color fore)
        {
            Button b = new Button(); b.Text = text; b.BackColor = back; b.ForeColor = fore;
            b.FlatStyle = FlatStyle.Flat; b.FlatAppearance.BorderColor = back;
            b.Font = new Font("Segoe UI", 10.5F, FontStyle.Bold); b.Cursor = Cursors.Hand;
            b.AutoSize = true; b.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            b.MinimumSize = new Size(130, 40); b.Padding = new Padding(10, 2, 10, 2); b.Margin = new Padding(6, 0, 0, 0);
            return b;
        }

        public static Button Plain(string text)
        {
            Button b = new Button(); b.Text = text; b.AutoSize = true; b.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            b.MinimumSize = new Size(110, 40); b.Padding = new Padding(8, 2, 8, 2); b.Margin = new Padding(6, 0, 0, 0);
            return b;
        }

        // Keep a window inside the visible screen (fixes cut-off windows on small / zoomed screens)
        public static void FitToScreen(Form f)
        {
            Rectangle wa = Screen.FromControl(f).WorkingArea;
            int w = Math.Min(f.Width, wa.Width - 20), h = Math.Min(f.Height, wa.Height - 20);
            f.Size = new Size(w, h);
            f.Location = new Point(wa.Left + (wa.Width - w) / 2, wa.Top + (wa.Height - h) / 2);
        }

        public static string AskPin(IWin32Window owner, string title)
        {
            using (Form f = new Form())
            {
                f.Text = title; f.Font = new Font("Segoe UI", 10F);
                f.AutoScaleDimensions = new SizeF(96F, 96F); f.AutoScaleMode = AutoScaleMode.Dpi;
                f.FormBorderStyle = FormBorderStyle.FixedDialog; f.MaximizeBox = false; f.MinimizeBox = false;
                f.StartPosition = FormStartPosition.CenterParent; f.ShowInTaskbar = false;
                f.AutoSize = true; f.AutoSizeMode = AutoSizeMode.GrowAndShrink;
                FlowLayoutPanel s = Stack(Color.White); s.Padding = new Padding(14);
                Text(s, "Enter the IT settings PIN:", 300, false, Color.Black, 0);
                TextBox t = new TextBox(); t.UseSystemPasswordChar = true; t.Width = 300; s.Controls.Add(t);
                FlowLayoutPanel r = Row(); r.Margin = new Padding(0, 12, 0, 0);
                Button ok = Plain("OK"); ok.DialogResult = DialogResult.OK;
                Button c = Plain("Cancel"); c.DialogResult = DialogResult.Cancel;
                r.Controls.Add(ok); r.Controls.Add(c); s.Controls.Add(r);
                f.Controls.Add(s); f.AcceptButton = ok; f.CancelButton = c;
                return f.ShowDialog(owner) == DialogResult.OK ? t.Text : null;
            }
        }
    }

    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Settings.Load();
            SysInfo.Collect();
            Profile p = Profile.Load();
            if (!p.Ok())
            {
                using (ProfileForm pf = new ProfileForm(p))
                    if (pf.ShowDialog() != DialogResult.OK) return;
            }
            Application.Run(new MainForm(p));
        }
    }

    // =============================================================== profile form
    class ProfileForm : Form
    {
        Profile p;
        TextBox tFirst, tLast, tDept, tPhone, tEmail, tLoc;

        public ProfileForm(Profile prof)
        {
            p = prof;
            Text = Author.App + " - Your details";
            Font = new Font("Segoe UI", 10F);
            AutoScaleDimensions = new SizeF(96F, 96F);
            AutoScaleMode = AutoScaleMode.Dpi;
            ClientSize = new Size(480, 600);
            MinimumSize = new Size(420, 320);
            MaximizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Color.White;
            try { Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath); } catch { }

            Panel band = new Panel(); band.Dock = DockStyle.Top; band.Height = 60; band.BackColor = Theme.Navy;
            Label h = new Label(); h.Text = "  Tell us who you are (only once)"; h.ForeColor = Color.White;
            h.Font = new Font("Segoe UI", 13F, FontStyle.Bold); h.Dock = DockStyle.Fill; h.TextAlign = ContentAlignment.MiddleLeft;
            band.Controls.Add(h);

            FlowLayoutPanel bar;
            FlowLayoutPanel area = UI.ScrollArea(this, out bar);
            Controls.Add(band);

            int w = 420;
            tFirst = UI.Field(area, "First name *", p.First, w);
            tLast  = UI.Field(area, "Last name", p.Last, w);
            tDept  = UI.Field(area, "Department / project *", p.Dept, w);
            tPhone = UI.Field(area, "Mobile number *", p.Phone, w);
            tEmail = UI.Field(area, "Work e-mail *", p.Email, w);
            tLoc   = UI.Field(area, "Office / site / floor", p.Location, w);
            UI.Text(area, "This computer (added automatically): " + SysInfo.ComputerName +
                          (SysInfo.Model.Length > 0 ? "  ·  " + SysInfo.Model : ""), w, false, Theme.Soft, 10);

            Button cancel = UI.Plain("Cancel");
            Button save = UI.Btn("💾  Save", Theme.Navy, Color.White);
            bar.Controls.Add(cancel); bar.Controls.Add(save);      // right-to-left: Cancel at far right
            save.Click += delegate { Confirm(); };
            cancel.Click += delegate { DialogResult = DialogResult.Cancel; Close(); };
            AcceptButton = save;
            Load += delegate { UI.FitToScreen(this); };
        }

        void Confirm()
        {
            p.First = tFirst.Text.Trim(); p.Last = tLast.Text.Trim(); p.Dept = tDept.Text.Trim();
            p.Phone = tPhone.Text.Trim(); p.Email = tEmail.Text.Trim(); p.Location = tLoc.Text.Trim();
            if (p.First.Length == 0 || p.Dept.Length == 0) { MessageBox.Show(this, "Please fill first name and department."); return; }
            if (!p.PhoneOk()) { MessageBox.Show(this, "Please enter a valid mobile number (7-15 digits)."); return; }
            if (!p.EmailOk()) { MessageBox.Show(this, "Please check your e-mail, e.g. name@company.com"); return; }
            string msg = "Please check your details:\n\nName:  " + p.FullName() + "\nPhone:  " + p.Phone +
                         "\nE-mail:  " + p.Email + "\n\nIT will reply to this e-mail. If it is wrong, your ticket " +
                         "updates will not reach you.\n\nAre the e-mail and number correct?";
            if (MessageBox.Show(this, msg, "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                return;
            p.Save();
            DialogResult = DialogResult.OK; Close();
        }
    }

    // =============================================================== main form (3 tabs)
    class MainForm : Form
    {
        Profile p;
        TabControl tabs;
        TabPage tabTicket, tabDetails, tabSettings, tabAbout;
        Label lblHeader, lblWho, lblHint;
        // my details
        TextBox dFirst, dLast, dDept, dPhone, dEmail, dLoc;
        // ticket
        ComboBox cboKind;
        TextBox tTitle, tText;
        ListBox lstFiles;
        RadioButton rbL1, rbL2;
        List<string> files = new List<string>();
        // settings
        TextBox sCompany, sTeam, sPhone, sL1Name, sL1Email, sL2Name, sL2Email, sPin;
        bool settingsUnlocked;

        public MainForm(Profile prof)
        {
            p = prof;
            Text = Author.App + "  -  by " + Author.Name;
            Font = new Font("Segoe UI", 10F);
            AutoScaleDimensions = new SizeF(96F, 96F);
            AutoScaleMode = AutoScaleMode.Dpi;
            ClientSize = new Size(680, 760);
            MinimumSize = new Size(560, 420);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Color.White;
            try { Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath); } catch { }

            Panel band = new Panel(); band.Dock = DockStyle.Top; band.Height = 58; band.BackColor = Theme.Navy;
            lblHeader = new Label(); lblHeader.ForeColor = Color.White;
            lblHeader.Font = new Font("Segoe UI", 15F, FontStyle.Bold); lblHeader.Dock = DockStyle.Fill;
            lblHeader.TextAlign = ContentAlignment.MiddleLeft;
            band.Controls.Add(lblHeader);

            tabs = new TabControl(); tabs.Dock = DockStyle.Fill; tabs.Padding = new Point(14, 6);
            tabTicket   = new TabPage("📝  New ticket");
            tabDetails  = new TabPage("👤  My details");
            tabSettings = new TabPage("⚙  Settings");
            tabAbout    = new TabPage("ℹ  About");
            foreach (TabPage tp in new TabPage[] { tabTicket, tabDetails, tabSettings, tabAbout })
            { tp.BackColor = Color.White; tabs.TabPages.Add(tp); }
            Controls.Add(tabs);
            Controls.Add(band);

            BuildTicketTab();
            BuildDetailsTab();
            BuildSettingsTab();
            BuildAboutTab();
            RefreshTexts();

            tabs.Selecting += OnTabSelecting;
            Load += delegate { UI.FitToScreen(this); };
            Shown += delegate
            {
                if (!Settings.IsConfigured())
                {
                    tabs.SelectedTab = tabSettings;
                    MessageBox.Show(this, "Welcome!\n\nFirst, IT should enter where tickets are sent:\n" +
                                          "Level 1 and Level 2 e-mail addresses.\n\nFill them in the Settings tab and press Save.",
                                    Author.App, MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            };
        }

        void RefreshTexts()
        {
            lblHeader.Text = "  " + Settings.Company + " IT Help";
            rbL1.Text = Settings.L1Name; rbL2.Text = Settings.L2Name;
            lblHint.Text = "Default: goes to " + Settings.L1Name + " with a copy to " + Settings.L2Name + ".";
            lblWho.Text = "Name:   " + p.FullName() + "\n" +
                          "Department:   " + p.Dept + (p.Location.Length > 0 ? "   ·   " + p.Location : "") + "\n" +
                          "Mobile:   " + p.Phone + "\n" +
                          "E-mail:   " + p.Email;
        }

        // ------------------------------------------------------------- tab 1: ticket
        void BuildTicketTab()
        {
            FlowLayoutPanel bar;
            FlowLayoutPanel a = UI.ScrollArea(tabTicket, out bar);
            int w = 590;

            // ---- who is creating this ticket (always visible, goes into the ticket)
            FlowLayoutPanel card = UI.Stack(Theme.Back); card.Padding = new Padding(12, 10, 12, 10);
            card.Margin = new Padding(3, 0, 3, 6); card.MinimumSize = new Size(w + 6, 0);
            a.Controls.Add(card);
            UI.Text(card, "👤  Ticket from (your details)", w - 24, true, Color.Black, 0);
            lblWho = UI.Text(card, "", w - 24, false, Color.Black, 6);
            UI.Text(card, "🖥 " + SysInfo.ComputerName + (SysInfo.Model.Length > 0 ? "  ·  " + SysInfo.Model : "") +
                          (SysInfo.Ip.Length > 0 ? "  ·  " + SysInfo.Ip : "") + "   (computer details added automatically)",
                    w - 24, false, Theme.Soft, 6);
            Button bEdit = UI.Plain("✏  Change my details"); bEdit.Margin = new Padding(3, 8, 3, 0);
            bEdit.Click += delegate { tabs.SelectedTab = tabDetails; }; card.Controls.Add(bEdit);

            UI.Heading(a, "What is the problem about? *", w);
            cboKind = new ComboBox(); cboKind.DropDownStyle = ComboBoxStyle.DropDownList; cboKind.Width = w;
            cboKind.Items.AddRange(Kind.All); cboKind.SelectedIndex = 0; a.Controls.Add(cboKind);

            UI.Heading(a, "Short title (optional)", w);
            tTitle = new TextBox(); tTitle.Width = w; tTitle.MaxLength = 90; a.Controls.Add(tTitle);

            UI.Heading(a, "Describe the problem *   (do NOT write your password)", w);
            tText = new TextBox(); tText.Multiline = true; tText.ScrollBars = ScrollBars.Vertical;
            tText.Size = new Size(w, 130); a.Controls.Add(tText);

            UI.Heading(a, "Photos / screenshots (optional)", w);
            FlowLayoutPanel fr = UI.Row(); a.Controls.Add(fr);
            lstFiles = new ListBox(); lstFiles.Size = new Size(w - 190, 110); fr.Controls.Add(lstFiles);
            FlowLayoutPanel fb = UI.Stack(Color.White); fb.Margin = new Padding(8, 0, 0, 0); fr.Controls.Add(fb);
            Button bAdd = UI.Plain("📎 Add file"); Button bShot = UI.Plain("📷 Screenshot"); Button bDel = UI.Plain("🗑 Remove");
            foreach (Button b in new Button[] { bAdd, bShot, bDel })
            { b.MinimumSize = new Size(170, 34); b.Margin = new Padding(0, 0, 0, 4); fb.Controls.Add(b); }
            bAdd.Click += delegate { AddFiles(); };
            bShot.Click += delegate { Shot(); };
            bDel.Click += delegate { RemoveFile(); };

            UI.Heading(a, "Who should get this?", w);
            FlowLayoutPanel rr = UI.Row(); a.Controls.Add(rr);
            rbL1 = new RadioButton(); rbL1.Checked = true; rbL1.AutoSize = true; rbL1.Margin = new Padding(3, 3, 30, 3);
            rbL2 = new RadioButton(); rbL2.AutoSize = true;
            rr.Controls.Add(rbL1); rr.Controls.Add(rbL2);
            lblHint = UI.Text(a, "", w, false, Theme.Soft, 2);

            // bottom bar: big Send button (always visible)
            Button send = UI.Btn("📨   Send to IT", Theme.Gold, Color.Black);
            send.Font = new Font("Segoe UI", 12F, FontStyle.Bold); send.MinimumSize = new Size(260, 46);
            send.Click += delegate { Send(); };
            bar.Controls.Add(send);
        }

        void AddFiles()
        {
            using (OpenFileDialog d = new OpenFileDialog())
            {
                d.Multiselect = true;
                d.Filter = "Images and files|*.png;*.jpg;*.jpeg;*.gif;*.pdf;*.txt;*.log;*.docx;*.xlsx|All files|*.*";
                if (d.ShowDialog(this) == DialogResult.OK)
                    foreach (string f in d.FileNames) if (!files.Contains(f)) { files.Add(f); lstFiles.Items.Add(Path.GetFileName(f)); }
            }
        }

        void Shot()
        {
            try
            {
                Rectangle b = Screen.PrimaryScreen.Bounds;
                using (Bitmap bmp = new Bitmap(b.Width, b.Height))
                {
                    using (Graphics g = Graphics.FromImage(bmp)) g.CopyFromScreen(b.Location, Point.Empty, b.Size);
                    string f = Path.Combine(Path.GetTempPath(), "ITHelp_screen_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".png");
                    bmp.Save(f, ImageFormat.Png);
                    files.Add(f); lstFiles.Items.Add(Path.GetFileName(f));
                }
                MessageBox.Show(this, "Screenshot of your screen added.");
            }
            catch (Exception ex) { MessageBox.Show(this, "Could not take screenshot: " + ex.Message); }
        }

        void RemoveFile()
        {
            int i = lstFiles.SelectedIndex;
            if (i >= 0) { files.RemoveAt(i); lstFiles.Items.RemoveAt(i); }
        }

        // ------------------------------------------------------------- tab: my details
        void BuildDetailsTab()
        {
            FlowLayoutPanel bar;
            FlowLayoutPanel a = UI.ScrollArea(tabDetails, out bar);
            int w = 590;
            UI.Text(a, "These details are added to every ticket, so IT knows who you are and how to reach you.",
                    w, false, Theme.Soft, 0);
            dFirst = UI.Field(a, "First name *", p.First, w);
            dLast  = UI.Field(a, "Last name", p.Last, w);
            dDept  = UI.Field(a, "Department / project *", p.Dept, w);
            dPhone = UI.Field(a, "Mobile number *", p.Phone, w);
            dEmail = UI.Field(a, "Work e-mail *   (IT replies to this e-mail)", p.Email, w);
            dLoc   = UI.Field(a, "Office / site / floor", p.Location, w);

            Button undo = UI.Plain("Undo changes");
            Button save = UI.Btn("💾  Save my details", Theme.Navy, Color.White);
            bar.Controls.Add(undo); bar.Controls.Add(save);
            save.Click += delegate { SaveDetails(); };
            undo.Click += delegate { FillDetails(); };
        }

        void FillDetails()
        {
            dFirst.Text = p.First; dLast.Text = p.Last; dDept.Text = p.Dept;
            dPhone.Text = p.Phone; dEmail.Text = p.Email; dLoc.Text = p.Location;
        }

        void SaveDetails()
        {
            Profile n = new Profile();
            n.First = dFirst.Text.Trim(); n.Last = dLast.Text.Trim(); n.Dept = dDept.Text.Trim();
            n.Phone = dPhone.Text.Trim(); n.Email = dEmail.Text.Trim(); n.Location = dLoc.Text.Trim();
            if (n.First.Length == 0 || n.Dept.Length == 0) { MessageBox.Show(this, "Please fill first name and department."); return; }
            if (!n.PhoneOk()) { MessageBox.Show(this, "Please enter a valid mobile number (7-15 digits)."); return; }
            if (!n.EmailOk()) { MessageBox.Show(this, "Please check your e-mail, e.g. name@company.com"); return; }
            p.First = n.First; p.Last = n.Last; p.Dept = n.Dept; p.Phone = n.Phone; p.Email = n.Email; p.Location = n.Location;
            p.Save();
            RefreshTexts();
            MessageBox.Show(this, "Your details are saved.", Author.App, MessageBoxButtons.OK, MessageBoxIcon.Information);
            tabs.SelectedTab = tabTicket;
        }

        // ------------------------------------------------------------- tab 2: settings
        void BuildSettingsTab()
        {
            FlowLayoutPanel bar;
            FlowLayoutPanel a = UI.ScrollArea(tabSettings, out bar);
            int w = 590;

            UI.Text(a, "Where should tickets be e-mailed? Fill this once and press Save.\n" +
                       "Default: a ticket goes to Level 1, with a copy (CC) to Level 2.", w, false, Theme.Soft, 0);

            UI.Heading(a, "LEVEL 1  -  first-line support", w);
            sL1Name  = UI.Field(a, "Name shown to users", Settings.L1Name, w);
            sL1Email = UI.Field(a, "E-mail address *", Settings.L1Email, w);
            UI.Heading(a, "LEVEL 2  -  IT Manager", w);
            sL2Name  = UI.Field(a, "Name shown to users", Settings.L2Name, w);
            sL2Email = UI.Field(a, "E-mail address *", Settings.L2Email, w);
            UI.Heading(a, "COMPANY", w);
            sCompany = UI.Field(a, "Company name", Settings.Company, w);
            sTeam    = UI.Field(a, "IT team name", Settings.Team, w);
            sPhone   = UI.Field(a, "IT phone (optional)", Settings.ItPhone, w);
            UI.Heading(a, "PROTECT SETTINGS (optional)", w);
            sPin = UI.Field(a, "Settings PIN - so normal users cannot change the e-mails (leave empty = no PIN)", "", w);
            sPin.UseSystemPasswordChar = true;
            if (Settings.PinHash.Length > 0) sPin.Text = "********";

            Button reset = UI.Plain("Undo changes");
            Button save = UI.Btn("💾  Save settings", Theme.Navy, Color.White);
            bar.Controls.Add(reset); bar.Controls.Add(save);
            save.Click += delegate { SaveSettings(); };
            reset.Click += delegate { FillSettings(); };
        }

        void FillSettings()
        {
            sL1Name.Text = Settings.L1Name; sL1Email.Text = Settings.L1Email;
            sL2Name.Text = Settings.L2Name; sL2Email.Text = Settings.L2Email;
            sCompany.Text = Settings.Company; sTeam.Text = Settings.Team; sPhone.Text = Settings.ItPhone;
            sPin.Text = Settings.PinHash.Length > 0 ? "********" : "";
        }

        void SaveSettings()
        {
            if (!UI.EmailOk(sL1Email.Text)) { MessageBox.Show(this, "Please enter a valid Level 1 e-mail address."); sL1Email.Focus(); return; }
            if (!UI.EmailOk(sL2Email.Text)) { MessageBox.Show(this, "Please enter a valid Level 2 e-mail address."); sL2Email.Focus(); return; }
            Settings.L1Name  = sL1Name.Text.Trim().Length > 0 ? sL1Name.Text.Trim() : "Level 1";
            Settings.L1Email = sL1Email.Text.Trim();
            Settings.L2Name  = sL2Name.Text.Trim().Length > 0 ? sL2Name.Text.Trim() : "Level 2";
            Settings.L2Email = sL2Email.Text.Trim();
            if (sCompany.Text.Trim().Length > 0) Settings.Company = sCompany.Text.Trim();
            if (sTeam.Text.Trim().Length > 0) Settings.Team = sTeam.Text.Trim();
            Settings.ItPhone = sPhone.Text.Trim();
            string pin = sPin.Text;
            if (pin.Length == 0) Settings.PinHash = "";
            else if (pin != "********") Settings.PinHash = Settings.Hash(pin);

            if (Settings.Save())
            {
                RefreshTexts();
                MessageBox.Show(this, "Settings saved.\n\nTickets now go to:\n" +
                                      Settings.L1Name + "  <" + Settings.L1Email + ">\n" +
                                      "copy to " + Settings.L2Name + "  <" + Settings.L2Email + ">",
                                Author.App, MessageBoxButtons.OK, MessageBoxIcon.Information);
                tabs.SelectedTab = tabTicket;
            }
            else MessageBox.Show(this, "Could not save settings on this computer.", Author.App);
        }

        void OnTabSelecting(object sender, TabControlCancelEventArgs e)
        {
            if (e.TabPage != tabSettings || settingsUnlocked || Settings.PinHash.Length == 0) return;
            string pin = UI.AskPin(this, "Settings");
            if (pin != null && Settings.Hash(pin) == Settings.PinHash) { settingsUnlocked = true; return; }
            if (pin != null) MessageBox.Show(this, "Wrong PIN. Only IT can change these settings.", Author.App);
            e.Cancel = true;
        }

        // ------------------------------------------------------------- tab 3: about
        void BuildAboutTab()
        {
            FlowLayoutPanel bar;
            FlowLayoutPanel a = UI.ScrollArea(tabAbout, out bar);
            int w = 590;

            Label app = UI.Text(a, Author.App, w, false, Theme.Navy, 0);
            app.Font = new Font("Segoe UI", 18F, FontStyle.Bold);
            UI.Text(a, "Version " + Author.Version + "  ·  Free IT ticket app for Windows", w, false, Theme.Soft, 0);

            FlowLayoutPanel card = UI.Stack(Theme.Back); card.Padding = new Padding(14, 12, 14, 12);
            card.Margin = new Padding(3, 14, 3, 6); card.MinimumSize = new Size(w + 6, 0);
            a.Controls.Add(card);
            int cw = w - 28;
            UI.Text(card, "Designed and built by", cw, false, Theme.Soft, 0);
            Label nm = UI.Text(card, Author.Name, cw, false, Color.Black, 2);
            nm.Font = new Font("Segoe UI", 16F, FontStyle.Bold);
            UI.Text(card, Author.Title, cw, false, Color.Black, 4);
            UI.Text(card, Author.Years, cw, false, Color.Black, 2);
            Link(card, "LinkedIn:  " + Author.LinkedIn.Replace("https://www.", "").TrimEnd('/'), Author.LinkedIn, 10);
            Link(card, "GitHub:  " + Author.GitHub.Replace("https://", ""), Author.GitHub, 4);
            UI.Text(card, "Open to IT jobs and freelance IT automation projects.", cw, false, Theme.Soft, 10);

            Label sup = UI.Text(a, "❤  Do you like this app?", w, true, Color.Black, 18);
            sup.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
            UI.Text(a, "This app is free. If it helps you, please support my work:", w, false, Color.Black, 6);
            UI.Text(a, "⭐   Give it a star on GitHub", w, false, Color.Black, 4);
            UI.Text(a, "📤   Share it with your friends and colleagues who need a free IT help desk", w, false, Color.Black, 4);
            UI.Text(a, "💬   Send me your feedback or ideas on LinkedIn or GitHub", w, false, Color.Black, 4);

            UI.Text(a, "© 2026 " + Author.Name + ". Free to use in any company. Please do not remove the author's name " +
                       "or publish this app under another name. See LICENSE on GitHub.", w, false, Theme.Soft, 18);

            Button share = UI.Btn("📤  Share", Theme.Gold, Color.Black);
            share.Click += delegate { ShareApp(); };
            Button star = UI.Btn("⭐  GitHub", Theme.Navy, Color.White);
            star.Click += delegate { OpenUrl(Author.Repo); };
            Button lin = UI.Btn("LinkedIn", Color.FromArgb(10, 102, 194), Color.White);
            lin.Click += delegate { OpenUrl(Author.LinkedIn); };
            bar.Controls.Add(share); bar.Controls.Add(star); bar.Controls.Add(lin);
        }

        void Link(Control parent, string text, string url, int topGap)
        {
            LinkLabel l = new LinkLabel(); l.Text = text; l.AutoSize = true; l.Margin = new Padding(3, topGap, 3, 3);
            l.LinkColor = Theme.Navy; l.LinkClicked += delegate { OpenUrl(url); };
            parent.Controls.Add(l);
        }

        void OpenUrl(string url)
        {
            try { System.Diagnostics.Process.Start(url); }
            catch { MessageBox.Show(this, url, Author.App); }
        }

        void ShareApp()
        {
            string text = "Free IT ticket app for Windows - employees report IT problems with screenshots in 1 click.\n" +
                          "IT Help Desk Lite by " + Author.Name + ":\n" + Author.Repo;
            try { Clipboard.SetText(text); } catch { }
            MessageBox.Show(this, "Share text copied! Paste it in WhatsApp, LinkedIn, Teams or e-mail:\n\n" + text,
                            "Thank you for sharing ❤", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        // ------------------------------------------------------------- send
        void Send()
        {
            if (!Settings.IsConfigured())
            {
                MessageBox.Show(this, "IT has not set the ticket e-mail addresses yet.\nPlease fill them in the Settings tab.", Author.App);
                tabs.SelectedTab = tabSettings; return;
            }
            if (tText.Text.Trim().Length < 3) { MessageBox.Show(this, "Please describe the problem."); return; }
            Kind k = (Kind)cboKind.SelectedItem;
            string title = tTitle.Text.Trim();
            string subject = "[IT-TICKET] " + k.Key + (title.Length > 0 ? ": " + title : ": " + tText.Text.Trim());
            if (subject.Length > 150) subject = subject.Substring(0, 150);

            string to, cc, toName;
            if (rbL2.Checked) { to = Settings.L2Email; cc = ""; toName = Settings.L2Name; }
            else { to = Settings.L1Email; cc = Settings.L2Email; toName = Settings.L1Name; }

            StringBuilder body = new StringBuilder();
            body.AppendLine("Sent to: " + toName);
            body.AppendLine("Name: " + p.FullName());
            body.AppendLine("Department: " + p.Dept);
            body.AppendLine("Phone: " + p.Phone);
            body.AppendLine("Email: " + p.Email);
            if (p.Location.Length > 0) body.AppendLine("Location: " + p.Location);
            body.AppendLine("Category: " + k.Key);
            body.AppendLine();
            body.AppendLine("Problem:");
            body.AppendLine(tText.Text.Trim());
            body.AppendLine();
            body.AppendLine("---- Computer details (automatic) ----");
            body.Append(SysInfo.Block());
            body.AppendLine();
            body.AppendLine("-- sent with " + Author.Credit + " (" + Author.Repo.Replace("https://", "") + ")");

            string choice = AskMailApp();
            if (choice == null) return;

            bool ok;
            if (choice == "outlook")
            {
                ok = SendViaOutlook(to, cc, subject, body.ToString());
                if (!ok)
                {
                    if (MessageBox.Show(this, "Outlook is not installed or could not open on this computer.\n\n" +
                                              "Do you want to send with Gmail instead?", Author.App,
                                        MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
                    choice = "gmail";
                }
                else
                {
                    MessageBox.Show(this, "Your e-mail is ready in Outlook.\n\nJust press SEND in Outlook.\n" +
                                          "You will get a ticket number by e-mail.", Author.App);
                }
            }
            if (choice == "gmail")
            {
                ok = SendViaGmail(to, cc, subject, body.ToString());
                if (!ok) return;
            }
            tText.Clear(); tTitle.Clear(); files.Clear(); lstFiles.Items.Clear();
        }

        string AskMailApp()
        {
            string result = null;
            using (Form f = new Form())
            {
                f.Text = Author.App + " - Send with";
                f.Font = new Font("Segoe UI", 10F);
                f.AutoScaleDimensions = new SizeF(96F, 96F); f.AutoScaleMode = AutoScaleMode.Dpi;
                f.FormBorderStyle = FormBorderStyle.FixedDialog;
                f.MaximizeBox = false; f.MinimizeBox = false; f.ShowInTaskbar = false;
                f.StartPosition = FormStartPosition.CenterParent;
                f.ClientSize = new Size(420, 230);
                f.BackColor = Color.White;

                Label q = new Label(); q.Text = "Which e-mail do you want to use?";
                q.Font = new Font("Segoe UI", 12F, FontStyle.Bold); q.AutoSize = true; q.Location = new Point(20, 18);
                f.Controls.Add(q);
                Label q2 = new Label(); q2.Text = "The e-mail will open ready. You only press SEND.";
                q2.ForeColor = Theme.Soft; q2.AutoSize = true; q2.Location = new Point(20, 50);
                f.Controls.Add(q2);

                Button bOut = UI.Btn("📧  Outlook", Theme.Navy, Color.White); bOut.SetBounds(20, 90, 185, 60);
                bOut.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
                bOut.Click += delegate { result = "outlook"; f.Close(); };
                f.Controls.Add(bOut);
                Button bGm = UI.Btn("✉  Gmail", Color.FromArgb(219, 68, 55), Color.White); bGm.SetBounds(215, 90, 185, 60);
                bGm.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
                bGm.Click += delegate { result = "gmail"; f.Close(); };
                f.Controls.Add(bGm);
                Button bCancel = new Button(); bCancel.Text = "Cancel"; bCancel.SetBounds(310, 175, 90, 34);
                bCancel.Click += delegate { result = null; f.Close(); };
                f.Controls.Add(bCancel);
                f.CancelButton = bCancel;
                f.ShowDialog(this);
            }
            return result;
        }

        bool SendViaGmail(string to, string cc, string subject, string body)
        {
            try
            {
                string b = body;
                string url = BuildGmailUrl(to, cc, subject, b);
                while (url.Length > 7500 && b.Length > 200)
                { b = b.Substring(0, b.Length - 200); url = BuildGmailUrl(to, cc, subject, b + "\n...(cut)"); }
                System.Diagnostics.Process.Start(url);

                string msg = "Gmail is opening in your browser with the e-mail ready.\n\n" +
                             "Just press SEND in Gmail.\n(If Gmail asks, sign in to your Gmail account first.)";
                if (files.Count > 0)
                {
                    string dir = Path.Combine(Path.GetTempPath(), "IT Help Desk - attach");
                    try { if (Directory.Exists(dir)) foreach (string old in Directory.GetFiles(dir)) File.Delete(old); } catch { }
                    Directory.CreateDirectory(dir);
                    foreach (string fl in files) { try { File.Copy(fl, Path.Combine(dir, Path.GetFileName(fl)), true); } catch { } }
                    System.Diagnostics.Process.Start("explorer.exe", "\"" + dir + "\"");
                    msg = "Gmail is opening in your browser with the e-mail ready.\n\n" +
                          "Your photos/screenshots are in the folder that just opened.\n" +
                          "Drag them into the Gmail window, then press SEND.";
                }
                MessageBox.Show(this, msg, Author.App);
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Could not open Gmail: " + ex.Message, Author.App);
                return false;
            }
        }

        static string BuildGmailUrl(string to, string cc, string subject, string body)
        {
            return "https://mail.google.com/mail/?view=cm&fs=1" +
                   "&to=" + Uri.EscapeDataString(to) +
                   (cc != null && cc.Length > 0 ? "&cc=" + Uri.EscapeDataString(cc) : "") +
                   "&su=" + Uri.EscapeDataString(subject) +
                   "&body=" + Uri.EscapeDataString(body);
        }

        static object Call(object o, string m, params object[] a)
        { return o.GetType().InvokeMember(m, BindingFlags.InvokeMethod, null, o, a); }
        static object Get(object o, string pr)
        { return o.GetType().InvokeMember(pr, BindingFlags.GetProperty, null, o, null); }
        static void Set(object o, string pr, object v)
        { o.GetType().InvokeMember(pr, BindingFlags.SetProperty, null, o, new object[] { v }); }

        bool SendViaOutlook(string to, string cc, string subject, string body)
        {
            try
            {
                Type t = Type.GetTypeFromProgID("Outlook.Application");
                if (t == null) return false;
                object ol = Activator.CreateInstance(t);
                object mail = Call(ol, "CreateItem", 0);
                Set(mail, "To", to);
                if (cc != null && cc.Length > 0) Set(mail, "CC", cc);
                Set(mail, "Subject", subject);
                Set(mail, "Body", body);
                object atts = Get(mail, "Attachments");
                foreach (string f in files) { try { Call(atts, "Add", f); } catch { } }
                Call(mail, "Display", false);
                return true;
            }
            catch { return false; }
        }
    }
}
