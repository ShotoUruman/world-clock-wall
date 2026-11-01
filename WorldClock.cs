using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Xml.Serialization;
using System.Xml;

[assembly: System.Reflection.AssemblyVersion("1.2.1.0")]
[assembly: System.Reflection.AssemblyFileVersion("1.2.1.0")]

namespace WorldClock {
    public class Station {
        public string Label { get; set; }
        public string Zone { get; set; }
        public Station() { }
        public Station(string label, string zone) { Label = label; Zone = zone; }
    }
    public class Preferences {
        public string Theme { get; set; }
        public string Accent { get; set; }
        public string Ink { get; set; }
        public string LabelFont { get; set; }
        public string DigitalFont { get; set; }
        public string BackgroundColor { get; set; }
        public string CardColor { get; set; }
        public string FaceColor { get; set; }
        public string HourColor { get; set; }
        public string MinuteColor { get; set; }
        public string SecondColor { get; set; }
        public int LabelSize { get; set; }
        public int DigitalSize { get; set; }
        public int Opacity { get; set; }
        public int CardWidth { get; set; }
        public bool TwentyFour { get; set; }
        public bool Seconds { get; set; }
        public bool Numerals { get; set; }
        public bool Topmost { get; set; }
        public bool SortZones { get; set; }
        public bool ShowSolar { get; set; }
        public bool SolarTwilight { get; set; }
        public bool SolarCities { get; set; }
        public int SolarHeight { get; set; }
        public List<Station> Stations { get; set; }
        public Preferences() {
            Theme = "Midnight"; Accent = "#77D9CC"; Ink = "#E8EDF4";
            LabelFont = "Segoe UI"; DigitalFont = "Consolas";
            LabelSize = 17; DigitalSize = 30;
            Opacity = 100; CardWidth = 260; TwentyFour = true; Seconds = true;
            Numerals = true; SortZones = true;
            ShowSolar = true; SolarTwilight = true; SolarCities = true; SolarHeight = 320;
            Stations = new List<Station>();
        }
        public static Preferences Defaults() {
            Preferences p = new Preferences();
            p.Stations.AddRange(new[] {
                new Station("HONOLULU", "Hawaiian Standard Time"),
                new Station("ANCHORAGE", "Alaskan Standard Time"),
                new Station("PACIFIC / REDMOND", "Pacific Standard Time"),
                new Station("DENVER", "Mountain Standard Time"),
                new Station("CHICAGO", "Central Standard Time"),
                new Station("WASHINGTON, DC", "Eastern Standard Time"),
                new Station("LOCAL TIME", "LOCAL"),
                new Station("ZULU / UTC", "UTC"),
                new Station("LONDON", "GMT Standard Time"),
                new Station("PARIS", "Romance Standard Time"),
                new Station("MOSCOW", "Russian Standard Time"),
                new Station("BEIJING", "China Standard Time"),
                new Station("TOKYO", "Tokyo Standard Time")
            });
            return p;
        }
        public Preferences Clone() {
            Preferences p = (Preferences)MemberwiseClone();
            p.Stations = Stations.Select(s => new Station(s.Label, s.Zone)).ToList();
            return p;
        }
        public void ApplyTheme(string name) {
            Theme = name;
            BackgroundColor = CardColor = FaceColor = HourColor = MinuteColor = SecondColor = null;
            Ink = name == "Ivory" ? "#1D2B3A" : "#E8EDF4";
            Accent = name == "Ivory" ? "#006B64" : "#77D9CC";
            if (name == "Amber") {
                BackgroundColor = "#15120D"; CardColor = "#241F15"; FaceColor = "#19160F";
                Ink = "#F5E8C8"; Accent = "#E9B75D";
            }
            if (name == "Arctic") {
                BackgroundColor = "#0E1626"; CardColor = "#18283C"; FaceColor = "#101F32";
                Ink = "#EDF5FF"; Accent = "#82B9FF";
            }
        }
        public void Validate() {
            Opacity = Math.Max(30, Math.Min(100, Opacity));
            CardWidth = Math.Max(210, Math.Min(400, CardWidth));
            LabelSize = Math.Max(12, Math.Min(26, LabelSize));
            DigitalSize = Math.Max(18, Math.Min(42, DigitalSize));
            SolarHeight = Math.Max(180, Math.Min(480, SolarHeight));
            if (!new[] { "Midnight", "Ivory", "Amber", "Arctic" }.Contains(Theme)) throw new InvalidDataException("Unknown theme preset.");
            if (Stations == null || Stations.Count == 0 || Stations.Count > 60)
                throw new InvalidDataException("Choose between 1 and 60 clocks.");
            foreach (string required in new[] { Accent, Ink })
                if (String.IsNullOrWhiteSpace(required) || ColorTranslator.FromHtml(required).A != 255) throw new InvalidDataException("Choose a solid text and accent color.");
            foreach (string value in new[] { BackgroundColor, CardColor, FaceColor, HourColor, MinuteColor, SecondColor })
                if (!String.IsNullOrWhiteSpace(value)) { Color color = ColorTranslator.FromHtml(value); if (color.A != 255) throw new InvalidDataException("Use solid colors; the opacity slider controls transparency."); }
            foreach (Station s in Stations) {
                if (String.IsNullOrWhiteSpace(s.Label) || s.Label.Length > 40)
                    throw new InvalidDataException("Clock labels must contain 1 to 40 characters.");
                Time.Zone(s.Zone);
            }
        }
    }
    public static class Time {
        static Dictionary<string, TimeZoneInfo> zones = new Dictionary<string, TimeZoneInfo>();
        public static TimeZoneInfo Zone(string id) {
            if (id == "LOCAL") return TimeZoneInfo.Local;
            if (!zones.ContainsKey(id)) zones[id] = TimeZoneInfo.FindSystemTimeZoneById(id);
            return zones[id];
        }
        public static DateTimeOffset At(DateTimeOffset utc, string zone) {
            return TimeZoneInfo.ConvertTime(utc, Zone(zone));
        }
        public static string Offset(TimeSpan span) {
            if (span == TimeSpan.Zero) return "UTC +00:00";
            return "UTC " + (span < TimeSpan.Zero ? "-" : "+") + span.Duration().ToString(@"hh\:mm");
        }
        public static string Abbreviation(string id, DateTimeOffset utc) {
            var zone = Zone(id); bool dst = zone.IsDaylightSavingTime(At(utc, id));
            switch (zone.Id) {
                case "Hawaiian Standard Time": return "HST";
                case "Alaskan Standard Time": return dst ? "AKDT" : "AKST";
                case "Pacific Standard Time": return dst ? "PDT" : "PST";
                case "Mountain Standard Time": return dst ? "MDT" : "MST";
                case "Central Standard Time": return dst ? "CDT" : "CST";
                case "Eastern Standard Time": return dst ? "EDT" : "EST";
                case "UTC": return "UTC";
                case "GMT Standard Time": return dst ? "BST" : "GMT";
                case "Romance Standard Time": return dst ? "CEST" : "CET";
                case "Russian Standard Time": return "MSK";
                case "China Standard Time": return "CST";
                case "Tokyo Standard Time": return "JST";
                default: return "";
            }
        }
        public static string ZoneCaption(string id, DateTimeOffset utc) {
            string abbreviation = Abbreviation(id, utc);
            string offset = Offset(At(utc, id).Offset);
            return abbreviation == "" || abbreviation == "UTC" ? offset : abbreviation + "  /  " + offset;
        }
        public static IEnumerable<Station> Ordered(Preferences p, DateTimeOffset utc) {
            if (p.SortZones) return p.Stations.OrderBy(s => At(utc, s.Zone).Offset); return p.Stations;
        }
    }
    public static class Store {
        public static readonly string DirectoryPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "WorldClockWall");
        public static readonly string FilePath = Path.Combine(DirectoryPath, "settings.xml");
        public static Preferences Load(out string warning) {
            warning = null;
            if (!File.Exists(FilePath)) return Preferences.Defaults();
            try { return ReadProfile(FilePath); }
            catch (Exception ex) { warning = "Saved settings could not be read. Defaults are shown; the original file has been kept. " + ex.Message; return Preferences.Defaults(); }
        }
        public static Preferences ReadProfile(string path) {
            var options = new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null, MaxCharactersInDocument = 1048576 };
            using (var reader = XmlReader.Create(path, options)) {
                var p = (Preferences)new XmlSerializer(typeof(Preferences)).Deserialize(reader); p.Validate(); return p;
            }
        }
        public static void Save(Preferences p) {
            Directory.CreateDirectory(DirectoryPath); WriteProfile(p, FilePath);
        }
        public static void WriteProfile(Preferences p, string path) {
            p.Validate(); string temp = path + ".tmp";
            using (var f = File.Create(temp)) new XmlSerializer(typeof(Preferences)).Serialize(f, p);
            if (File.Exists(path)) File.Replace(temp, path, path + ".bak");
            else File.Move(temp, path);
        }
    }
    public class Palette {
        public Color Background, Card, Edge, Muted, Accent, Ink, Dial, Hour, Minute, Second;
        public Palette(Preferences p) {
            bool light = p.Theme == "Ivory";
            Background = ColorTranslator.FromHtml(light ? "#E6E4DE" : "#0D131C");
            Card = ColorTranslator.FromHtml(light ? "#F7F5EF" : "#161F2B");
            Dial = ColorTranslator.FromHtml(light ? "#FDFCF8" : "#111923");
            Edge = ColorTranslator.FromHtml(light ? "#C5C3BB" : "#303D4D");
            Muted = ColorTranslator.FromHtml(light ? "#5B6571" : "#9DAABC");
            Accent = ColorTranslator.FromHtml(p.Accent); Ink = ColorTranslator.FromHtml(p.Ink);
            Background = Override(p.BackgroundColor, Background); Card = Override(p.CardColor, Card); Dial = Override(p.FaceColor, Dial);
            Hour = Override(p.HourColor, Ink); Minute = Override(p.MinuteColor, Ink); Second = Override(p.SecondColor, Accent);
        }
        static Color Override(string value, Color fallback) { return String.IsNullOrWhiteSpace(value) ? fallback : ColorTranslator.FromHtml(value); }
    }
    public class Wall : ScrollableControl {
        public Preferences Config;
        public DateTimeOffset? FixedUtc;
        public Wall(Preferences p) { Config = p; DoubleBuffered = true; AutoScroll = true; ResizeRedraw = true; }
        public void Reflow() {
            int cols = Math.Max(1, (ClientSize.Width - 24) / (Config.CardWidth + 14));
            int rows = (Config.Stations.Count + cols - 1) / cols;
            AutoScrollMinSize = new Size(0, rows * (Config.CardWidth + 132) + 14);
            Invalidate();
        }
        protected override void OnResize(EventArgs e) { base.OnResize(e); Reflow(); }
        static Font FontOf(string family, float size, FontStyle style) {
            try { return new Font(family, size, style, GraphicsUnit.Pixel); }
            catch { return new Font("Segoe UI", size, style, GraphicsUnit.Pixel); }
        }
        internal static void DrawText(Graphics g, string value, RectangleF r, string family, float size, Color color, FontStyle style) {
            float fitted = size;
            using (Font probe = FontOf(family, size, style)) {
                float measured = g.MeasureString(value, probe).Width;
                if (measured > r.Width - 4) fitted = Math.Max(10, size * (r.Width - 4) / measured);
            }
            using (Font f = FontOf(family, fitted, style))
            using (Brush b = new SolidBrush(color))
            using (StringFormat sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center, Trimming = StringTrimming.EllipsisCharacter, FormatFlags = StringFormatFlags.NoWrap })
                g.DrawString(value, f, b, r, sf);
        }
        protected override void OnPaint(PaintEventArgs e) {
            Palette c = new Palette(Config); BackColor = c.Background;
            Graphics g = e.Graphics; g.Clear(c.Background); g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
            g.TranslateTransform(AutoScrollPosition.X, AutoScrollPosition.Y);
            int width = ClientSize.Width - 24;
            int cols = Math.Max(1, width / (Config.CardWidth + 14));
            int cw = Math.Min(Config.CardWidth + 32, Math.Max(180, (width - (cols - 1) * 14) / cols));
            int ch = Config.CardWidth + 118;
            int left = Math.Max(12, (ClientSize.Width - (cols * cw + (cols - 1) * 14)) / 2);
            DateTimeOffset utc = FixedUtc ?? DateTimeOffset.UtcNow;
            int i = 0;
            foreach (Station s in Time.Ordered(Config, utc)) {
                Rectangle r = new Rectangle(left + (i % cols) * (cw + 14), 14 + (i / cols) * (ch + 14), cw, ch);
                i++;
                if (r.Bottom + AutoScrollPosition.Y < 0 || r.Top + AutoScrollPosition.Y > ClientSize.Height) continue;
                DrawStation(g, r, s, utc, c);
            }
        }
        void DrawStation(Graphics g, Rectangle r, Station s, DateTimeOffset utc, Palette c) {
            DateTimeOffset t = Time.At(utc, s.Zone);
            bool zulu = s.Zone == "UTC";
            using (Brush b = new SolidBrush(c.Card)) g.FillRectangle(b, r);
            using (Pen p = new Pen(zulu ? c.Accent : c.Edge, zulu ? 2 : 1)) g.DrawRectangle(p, r);
            using (Brush b = new SolidBrush(c.Accent)) g.FillRectangle(b, r.X + 1, r.Y + 1, r.Width - 2, zulu ? 4 : 2);
            DrawText(g, s.Label, new RectangleF(r.X + 8, r.Y + 14, r.Width - 16, 32), Config.LabelFont, Config.LabelSize, zulu ? c.Accent : c.Ink, FontStyle.Bold);
            DrawText(g, Time.ZoneCaption(s.Zone, utc), new RectangleF(r.X, r.Y + 47, r.Width, 20), "Consolas", 12, c.Muted, FontStyle.Regular);
            float radius = Math.Min(r.Width - 38, r.Height - 172) / 2f;
            float cx = r.X + r.Width / 2f, cy = r.Y + 77 + radius;
            using (Brush b = new SolidBrush(c.Dial)) g.FillEllipse(b, cx - radius, cy - radius, radius * 2, radius * 2);
            using (Pen p = new Pen(c.Edge, 2)) g.DrawEllipse(p, cx - radius, cy - radius, radius * 2, radius * 2);
            for (int n = 0; n < 60; n++) {
                double a = n * Math.PI / 30 - Math.PI / 2;
                float outer = radius - 8, inner = outer - (n % 5 == 0 ? 10 : 4);
                using (Pen p = new Pen(n % 5 == 0 ? c.Ink : c.Muted, n % 5 == 0 ? 2 : 1))
                    g.DrawLine(p, cx + (float)Math.Cos(a) * inner, cy + (float)Math.Sin(a) * inner, cx + (float)Math.Cos(a) * outer, cy + (float)Math.Sin(a) * outer);
            }
            if (Config.Numerals) for (int n = 1; n <= 12; n++) {
                double a = n * Math.PI / 6 - Math.PI / 2; float rad = radius - 32;
                DrawText(g, n.ToString(), new RectangleF(cx + (float)Math.Cos(a) * rad - 14, cy + (float)Math.Sin(a) * rad - 12, 28, 24), "Segoe UI", 15, c.Ink, FontStyle.Regular);
            }
            Hand(g, cx, cy, (t.Hour % 12 + t.Minute / 60.0 + t.Second / 3600.0) * 30, radius * .48f, 5, c.Hour);
            Hand(g, cx, cy, (t.Minute + t.Second / 60.0) * 6, radius * .72f, 3, c.Minute);
            if (Config.Seconds) Hand(g, cx, cy, t.Second * 6, radius * .83f, 1.6f, c.Second);
            using (Brush b = new SolidBrush(c.Accent)) g.FillEllipse(b, cx - 4, cy - 4, 8, 8);
            string fmt = Config.TwentyFour ? (Config.Seconds ? "HH:mm:ss" : "HH:mm") : (Config.Seconds ? "hh:mm:ss tt" : "hh:mm tt");
            float digitalY = cy + radius + 14;
            DrawText(g, t.ToString(fmt, CultureInfo.InvariantCulture), new RectangleF(r.X + 5, digitalY, r.Width - 10, 48), Config.DigitalFont, Config.DigitalSize, c.Ink, FontStyle.Regular);
            DrawText(g, t.ToString("ddd, dd MMM yyyy", CultureInfo.InvariantCulture), new RectangleF(r.X, digitalY + 41, r.Width, 22), "Segoe UI", 12, c.Muted, FontStyle.Regular);
            string state = zulu ? "ZULU  /  NO DAYLIGHT SAVING" : (Time.Zone(s.Zone).IsDaylightSavingTime(t) ? "DAYLIGHT TIME" : "STANDARD TIME");
            DrawText(g, state, new RectangleF(r.X, digitalY + 65, r.Width, 18), "Segoe UI", 10, zulu ? c.Accent : c.Muted, FontStyle.Regular);
        }
        static void Hand(Graphics g, float x, float y, double degrees, float length, float width, Color color) {
            double a = degrees * Math.PI / 180 - Math.PI / 2;
            using (Pen p = new Pen(color, width) { StartCap = LineCap.Round, EndCap = LineCap.Round })
                g.DrawLine(p, x, y, x + (float)Math.Cos(a) * length, y + (float)Math.Sin(a) * length);
        }
    }
    public class ClockForm : Form {
        Preferences config;
        Wall wall;
        SolarMap solar;
        Panel header, footer;
        Label title, subtitle, status;
        Button customize, full;
        Timer ticker;
        long lastPaintSecond;
        bool presentation;
        Rectangle savedBounds;
        FormWindowState savedState;
        readonly bool quietLaunch;
        bool suppressInitialTopmost;
        protected override bool ShowWithoutActivation { get { return quietLaunch; } }
        public ClockForm(Preferences p, bool withoutActivation = false) {
            quietLaunch = withoutActivation;
            suppressInitialTopmost = withoutActivation;
            Shown += delegate { suppressInitialTopmost = false; };
            config = p; Text = "World Clock Wall"; StartPosition = FormStartPosition.CenterScreen;
            Size = new Size(1220, 980); MinimumSize = new Size(740, 500);
            Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
            AutoScaleMode = AutoScaleMode.Dpi; Font = new Font("Segoe UI", 10);
            header = new Panel { Dock = DockStyle.Top, Height = 90, Width = ClientSize.Width };
            title = new Label { Text = "WORLD CLOCK WALL", Location = new Point(24, 15), AutoSize = true, Font = new Font("Segoe UI", 18, FontStyle.Bold) };
            subtitle = new Label { Text = "LOCAL  /  NATIONAL  /  ZULU  /  INTERNATIONAL", Location = new Point(26, 52), AutoSize = true, Font = new Font("Segoe UI", 8) };
            customize = new Button { Text = "Customize", Size = new Size(110, 36), Anchor = AnchorStyles.Right | AnchorStyles.Top, Location = new Point(Width - 266, 25), FlatStyle = FlatStyle.Flat };
            full = new Button { Text = "Full screen", Size = new Size(110, 36), Anchor = AnchorStyles.Right | AnchorStyles.Top, Location = new Point(Width - 146, 25), FlatStyle = FlatStyle.Flat };
            header.Controls.AddRange(new Control[] { title, subtitle, customize, full });
            footer = new Panel { Dock = DockStyle.Bottom, Height = 32 };
            status = new Label { Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(24, 0, 0, 0), Font = new Font("Segoe UI", 8) };
            footer.Controls.Add(status);
            wall = new Wall(config) { Dock = DockStyle.Fill };
            solar = new SolarMap(config) { Dock = DockStyle.Top };
            Controls.Add(wall); Controls.Add(footer); Controls.Add(solar); Controls.Add(header);
            customize.Click += delegate { using (SettingsForm dialog = new SettingsForm(config)) {
                if (dialog.ShowDialog(this) == DialogResult.OK) {
                    config = dialog.Result; wall.Config = config; solar.Config = config; Apply(); wall.Reflow();
                }
            }};
            full.Click += delegate { TogglePresentation(); };
            KeyPreview = true;
            KeyDown += delegate(object sender, KeyEventArgs e) { if (e.KeyCode == Keys.F11 || (e.KeyCode == Keys.Escape && presentation)) { TogglePresentation(); e.Handled = true; } };
            ticker = new Timer { Interval = 250 };
            ticker.Tick += delegate {
                long second = DateTime.UtcNow.Ticks / TimeSpan.TicksPerSecond;
                if (WindowState != FormWindowState.Minimized && second != lastPaintSecond) { lastPaintSecond = second; wall.Invalidate(); if (solar.Visible) solar.Invalidate(); UpdateStatus(); }
            };
            ticker.Start(); Apply();
        }
        protected override void Dispose(bool disposing) {
            if (disposing && ticker != null) { ticker.Stop(); ticker.Dispose(); ticker = null; }
            base.Dispose(disposing);
        }
        void Apply() {
            Palette c = new Palette(config); BackColor = header.BackColor = footer.BackColor = c.Background;
            title.ForeColor = c.Ink; subtitle.ForeColor = status.ForeColor = c.Muted;
            customize.BackColor = full.BackColor = c.Card; customize.ForeColor = full.ForeColor = c.Ink;
            solar.Visible = config.ShowSolar; solar.Height = config.SolarHeight; solar.RefreshMap();
            Opacity = config.Opacity / 100.0; TopMost = config.Topmost && !suppressInitialTopmost; wall.Reflow(); UpdateStatus();
        }
        void UpdateStatus() {
            string direction = config.SortZones ? "Time-zone order" : "Custom order";
            status.Text = config.Stations.Count + " clocks  /  " + direction + "  /  Windows system time  /  " + config.Opacity + "% opaque  /  F11 full screen";
        }
        void TogglePresentation() {
            if (!presentation) {
                savedBounds = Bounds; savedState = WindowState; WindowState = FormWindowState.Normal;
                FormBorderStyle = FormBorderStyle.None; Bounds = Screen.FromControl(this).Bounds;
                presentation = true; full.Text = "Exit full screen";
            } else {
                presentation = false; FormBorderStyle = FormBorderStyle.Sizable; Bounds = savedBounds; WindowState = savedState; full.Text = "Full screen";
            }
        }
    }
    static class Program {
        [STAThread]
        static int Main(string[] args) {
            Application.EnableVisualStyles(); Application.SetCompatibleTextRenderingDefault(false);
            if (args.Length > 0 && args[0] == "--self-test") return SelfTest();
            if (args.Length > 0 && args[0] == "--audit-times") { TimeAudit.Export(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "verification", "time-audit.tsv")); return 0; }
            try {
                string warning; Preferences p = Store.Load(out warning);
                bool quiet = args.Contains("--no-activate");
                using (var form = new ClockForm(p, quiet)) {
                    int pos = Array.IndexOf(args, "--bounds");
                    if (pos >= 0 && args.Length >= pos + 5) {
                        form.StartPosition = FormStartPosition.Manual;
                        form.Bounds = new Rectangle(Int32.Parse(args[pos + 1]), Int32.Parse(args[pos + 2]), Int32.Parse(args[pos + 3]), Int32.Parse(args[pos + 4]));
                    }
                    if (warning != null && !quiet) form.Shown += delegate { MessageBox.Show(form, warning, "Settings", MessageBoxButtons.OK, MessageBoxIcon.Information); };
                    Application.Run(form);
                }
                return 0;
            } catch (Exception ex) { MessageBox.Show(ex.Message, "World Clock Wall", MessageBoxButtons.OK, MessageBoxIcon.Error); return 1; }
        }
        static void Require(bool ok, string message) { if (!ok) throw new Exception(message); }
        static void PrepareOffscreen(Control c) {
            // Create HWNDs for drawing only; no Show, Activate, or foreground call.
            typeof(Control).GetMethod("CreateControl", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic, null, new[] { typeof(bool) }, null).Invoke(c, new object[] { true });
            foreach (Control child in c.Controls) PrepareOffscreen(child);
            c.PerformLayout();
        }
        static int SelfTest() {
            string folder = AppDomain.CurrentDomain.BaseDirectory;
            try {
                Preferences p = Preferences.Defaults(); p.Validate();
                DateTimeOffset before = new DateTimeOffset(2026, 11, 1, 5, 59, 59, TimeSpan.Zero), after = before.AddSeconds(1);
                var a = Time.At(before, "Eastern Standard Time"); var b = Time.At(after, "Eastern Standard Time");
                Require(a.Hour == 1 && a.Minute == 59 && a.Second == 59 && a.Offset.TotalHours == -4, "Before fallback");
                Require(b.Hour == 1 && b.Minute == 0 && b.Offset.TotalHours == -5, "After fallback");
                Require(Time.At(after, "UTC").Hour == 6, "Zulu must stay continuous");
                Require(Time.At(after, "Central Standard Time").Hour == Time.At(after, "Eastern Standard Time").Hour && Time.Abbreviation("Central Standard Time", after) == "CDT" && Time.Abbreviation("Eastern Standard Time", after) == "EST", "Staggered fallback must show matching hours with distinct DST labels");
                Require(Time.Abbreviation("Central Standard Time", after.AddHours(1)) == "CST", "Central label must change at its own fallback");
                var spring = Time.At(new DateTimeOffset(2027, 3, 14, 7, 0, 0, TimeSpan.Zero), "Eastern Standard Time");
                Require(spring.Hour == 3 && spring.Offset.TotalHours == -4, "Spring transition");
                Require(Time.At(after, "India Standard Time").Offset.TotalMinutes == 330, "Half-hour zone");
                Require(Time.At(after, "Nepal Standard Time").Offset.TotalMinutes == 345, "Quarter-hour zone");
                var offsets = Time.Ordered(p, after).Select(s => Time.At(after, s.Zone).Offset).ToArray();
                Require(offsets.SequenceEqual(offsets.OrderBy(x => x)), "Timezone sorting");
                using (var ms = new MemoryStream()) {
                    var serializer = new XmlSerializer(typeof(Preferences)); serializer.Serialize(ms, p); ms.Position = 0;
                    var copy = (Preferences)serializer.Deserialize(ms); copy.Validate(); Require(copy.Stations.Count == 13 && copy.Stations[7].Label == "ZULU / UTC", "Settings round trip");
                }
                string verification = Path.Combine(folder, "verification"); Directory.CreateDirectory(verification);
                string profilePath = Path.Combine(verification, "profile.xml");
                Preferences custom = p.Clone(); custom.ApplyTheme("Arctic"); custom.HourColor = "#FFA500";
                custom.MinuteColor = "#00BFFF"; custom.LabelSize = 23; custom.DigitalSize = 38; custom.Opacity = 75;
                custom.ShowSolar = false; custom.SolarTwilight = false; custom.SolarCities = false; custom.SolarHeight = 260;
                custom.Stations[0].Label = "MY PACIFIC CLOCK"; custom.SortZones = false;
                Store.WriteProfile(custom, profilePath);
                Preferences imported = Store.ReadProfile(profilePath);
                Require(imported.HourColor == custom.HourColor && imported.LabelSize == 23 && imported.Opacity == 75 && imported.Stations[0].Label == "MY PACIFIC CLOCK" && !imported.SortZones, "Profile round trip must preserve customizations");
                Require(!imported.ShowSolar && !imported.SolarTwilight && !imported.SolarCities && imported.SolarHeight == 260, "Solar settings round trip");
                custom.Opacity = 80; Store.WriteProfile(custom, profilePath);
                Require(Store.ReadProfile(profilePath).Opacity == 80 && Store.ReadProfile(profilePath + ".bak").Opacity == 75, "Atomic profile save and backup");
                var oldXml = new XmlDocument { XmlResolver = null }; oldXml.Load(profilePath);
                foreach (string name in new[] { "BackgroundColor", "CardColor", "FaceColor", "HourColor", "MinuteColor", "SecondColor", "LabelSize", "DigitalSize", "ShowSolar", "SolarTwilight", "SolarCities", "SolarHeight" }) {
                    XmlNode node = oldXml.DocumentElement.SelectSingleNode(name); if (node != null) oldXml.DocumentElement.RemoveChild(node);
                }
                oldXml.DocumentElement.SelectSingleNode("Theme").InnerText = "Midnight";
                string legacyPath = Path.Combine(verification, "legacy-profile.xml"); oldXml.Save(legacyPath);
                Preferences legacy = Store.ReadProfile(legacyPath);
                Require(legacy.LabelSize == 17 && legacy.DigitalSize == 30 && legacy.Stations[0].Label == "MY PACIFIC CLOCK", "Legacy settings migration");
                Require(legacy.ShowSolar && legacy.SolarTwilight && legacy.SolarCities && legacy.SolarHeight == 320, "Legacy solar defaults");
                string invalidPath = Path.Combine(verification, "invalid-profile.xml");
                File.WriteAllText(invalidPath, "<!DOCTYPE Preferences []><Preferences />");
                bool rejected = false; try { Store.ReadProfile(invalidPath); } catch { rejected = true; }
                Require(rejected, "Profiles must reject DTDs");
                SolarMap.Verify(folder);
                using (Wall wall = new Wall(p)) {
                    wall.FixedUtc = after; wall.Size = new Size(1200, 1660); wall.CreateControl(); wall.Reflow();
                    using (Bitmap bmp = new Bitmap(wall.Width, wall.Height)) { wall.DrawToBitmap(bmp, new Rectangle(Point.Empty, bmp.Size)); bmp.Save(Path.Combine(folder, "preview.png")); }
                    wall.Size = new Size(500, 900); wall.Reflow();
                    using (Bitmap bmp = new Bitmap(wall.Width, wall.Height)) { wall.DrawToBitmap(bmp, new Rectangle(Point.Empty, bmp.Size)); bmp.Save(Path.Combine(folder, "preview-narrow.png")); }
                    p.Theme = "Ivory"; p.Ink = "#1D2B3A"; p.Accent = "#006B64"; p.LabelFont = "Segoe Print"; p.CardWidth = 210; p.TwentyFour = false;
                    wall.Size = new Size(1200, 1300); wall.Reflow();
                    using (Bitmap bmp = new Bitmap(wall.Width, wall.Height)) { wall.DrawToBitmap(bmp, new Rectangle(Point.Empty, bmp.Size)); bmp.Save(Path.Combine(folder, "preview-ivory.png")); }
                }
                using (var form = new ClockForm(Preferences.Defaults())) {
                    form.Controls.OfType<Wall>().Single().FixedUtc = after;
                    form.Controls.OfType<SolarMap>().Single().FixedUtc = after;
                    PrepareOffscreen(form); form.PerformLayout();
                    using (Bitmap bmp = new Bitmap(form.Width, form.Height)) { form.DrawToBitmap(bmp, new Rectangle(Point.Empty, bmp.Size)); bmp.Save(Path.Combine(folder, "preview-app.png")); }
                }
                using (var settings = new SettingsForm(Preferences.Defaults())) {
                    PrepareOffscreen(settings); settings.PerformLayout();
                    using (Bitmap bmp = new Bitmap(settings.Width, settings.Height)) { settings.DrawToBitmap(bmp, new Rectangle(Point.Empty, bmp.Size)); bmp.Save(Path.Combine(folder, "preview-settings.png")); }
                    settings.VerifyEditor();
                    settings.SelectClockTab();
                    settings.PerformLayout();
                    using (Bitmap bmp = new Bitmap(settings.Width, settings.Height)) { settings.DrawToBitmap(bmp, new Rectangle(Point.Empty, bmp.Size)); bmp.Save(Path.Combine(folder, "preview-order.png")); }
                    settings.SelectColorsTab(); settings.PerformLayout();
                    using (Bitmap bmp = new Bitmap(settings.Width, settings.Height)) { settings.DrawToBitmap(bmp, new Rectangle(Point.Empty, bmp.Size)); bmp.Save(Path.Combine(folder, "preview-colors.png")); }
                }
                File.WriteAllText(Path.Combine(folder, "test-results.txt"), "PASS: 13 zones, fall-back boundary, spring-forward boundary, continuous Zulu, fractional offsets, sorted order, XML round trip, offscreen renders, appearance selections, manual reorder, unsaved edit isolation, live color/size preview, presets preserving clocks, profile round trip, atomic save and backup, legacy migration, invalid profile rejection. Solar: seasons, polar day/night, subsolar/antisolar elevations, westward motion, twilight bands, 127 embedded land polygons, dark/ivory maps. No windows shown; production settings untouched.\r\n");
                return 0;
            } catch (Exception ex) { File.WriteAllText(Path.Combine(folder, "test-results.txt"), ex.ToString()); return 1; }
        }
    }
}
