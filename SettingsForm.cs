using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace WorldClock {
    public class SettingsForm : Form {
        public Preferences Result;
        Preferences draft;
        bool loading = true;
        ComboBox theme, labelFont, digitalFont;
        NumericUpDown labelSize, digitalSize;
        NumericUpDown solarHeight;
        TrackBar opacity, size;
        CheckBox twentyfour, seconds, numerals, topmost, sort;
        CheckBox showSolar, solarTwilight, solarCities;
        Button accent, ink, background, card, face, hour, minute, second;
        DataGridView grid;
        Label opacityLabel, sizeLabel, previewStatus;
        Wall preview;
        TabControl tabs;
        Timer previewTimer;

        public SettingsForm(Preferences original) {
            draft = original.Clone(); Text = "Customize World Clock Wall";
            Size = new Size(1180, 800); MinimumSize = new Size(1040, 700);
            StartPosition = FormStartPosition.CenterParent;
            Font = new Font("Segoe UI", 10); BackColor = Color.FromArgb(240, 242, 245);
            tabs = new TabControl { Dock = DockStyle.Fill };
            var appearance = new TabPage("Appearance"); var colors = new TabPage("Colors");
            var locations = new TabPage("Clocks & order"); var solarTab = new TabPage("Solar map"); var profiles = new TabPage("Profiles"); var about = new TabPage("About");
            tabs.TabPages.AddRange(new[] { appearance, colors, locations, solarTab, profiles, about });
            var solarFields = Fields(solarTab);
            showSolar = Check("Show solar world map above the clocks", draft.ShowSolar);
            solarTwilight = Check("Show civil, nautical and astronomical twilight", draft.SolarTwilight);
            solarCities = Check("Show reference-city dots (hover for details)", draft.SolarCities);
            solarHeight = Number(180, 480, draft.SolarHeight);
            AddField(solarFields, "Map", showSolar, 0); AddField(solarFields, "Height (px)", solarHeight, 1);
            AddField(solarFields, "Twilight", solarTwilight, 2); AddField(solarFields, "Reference cities", solarCities, 3);
            AddField(solarFields, "How it works", new Label { Text = "The map updates each minute from UTC, independent of daylight saving time. The sun symbol marks where the sun is directly overhead. Shading shows geometric day, twilight and night.\r\n\r\nReference dots mark the original cities, not inferred locations for new clocks.\r\n\r\nApproximate visualization using NOAA solar equations and public-domain Natural Earth coastlines; not a precise sunrise timetable.", AutoSize = true, MaximumSize = new Size(330, 0) }, 4);

            var fields = Fields(appearance);
            theme = Combo(new[] { "Midnight", "Ivory", "Amber", "Arctic" }, draft.Theme);
            var fontNames = FontFamily.Families.Select(f => f.Name).OrderBy(n => n).ToArray();
            labelFont = Combo(fontNames, draft.LabelFont); digitalFont = Combo(fontNames, draft.DigitalFont);
            labelSize = Number(12, 26, draft.LabelSize); digitalSize = Number(18, 42, draft.DigitalSize);
            opacity = new TrackBar { Minimum = 30, Maximum = 100, Value = draft.Opacity, TickFrequency = 10, Width = 210 };
            size = new TrackBar { Minimum = 210, Maximum = 400, Value = draft.CardWidth, TickFrequency = 30, Width = 210 };
            opacityLabel = new Label { AutoSize = true }; sizeLabel = new Label { AutoSize = true };
            var op = Flow(opacity, opacityLabel); var sz = Flow(size, sizeLabel);
            twentyfour = Check("24-hour digital time", draft.TwentyFour); seconds = Check("Seconds and second hand", draft.Seconds);
            numerals = Check("Numbers 1-12 on analog faces", draft.Numerals); topmost = Check("Keep above other windows", draft.Topmost);
            AddField(fields, "Color preset", theme, 0);
            AddField(fields, "Label font", labelFont, 1); AddField(fields, "Label size (px)", labelSize, 2);
            AddField(fields, "Digital font", digitalFont, 3); AddField(fields, "Digital size (px)", digitalSize, 4);
            AddField(fields, "Opacity (100% = solid)", op, 5); AddField(fields, "Clock size", sz, 6);
            AddField(fields, "Time format", twentyfour, 7); AddField(fields, "Clock detail", seconds, 8);
            AddField(fields, "Learning analog", numerals, 9); AddField(fields, "Window", topmost, 10);
            var handwriting = new Button { Text = "Try readable handwriting", AutoSize = true };
            handwriting.Click += delegate { labelFont.SelectedItem = "Segoe Print"; };
            AddField(fields, "Font shortcut", handwriting, 11);

            var colorFields = Fields(colors); Palette palette = new Palette(draft);
            accent = ColorButton(palette.Accent); ink = ColorButton(palette.Ink);
            background = ColorButton(palette.Background); card = ColorButton(palette.Card); face = ColorButton(palette.Dial);
            hour = ColorButton(palette.Hour); minute = ColorButton(palette.Minute); second = ColorButton(palette.Second);
            AddField(colorFields, "Accent / Zulu label", accent, 0); AddField(colorFields, "Text / dial numbers", ink, 1);
            AddField(colorFields, "Window background", background, 2); AddField(colorFields, "Clock card", card, 3);
            AddField(colorFields, "Analog face", face, 4); AddField(colorFields, "Hour hand (short)", hour, 5);
            AddField(colorFields, "Minute hand (long)", minute, 6); AddField(colorFields, "Second hand", second, 7);
            AddField(colorFields, "Tip", new Label { Text = "Different hand colors can help while learning analog. Choose colors with enough contrast against the face.", AutoSize = true, MaximumSize = new Size(330, 0) }, 8);

            var locFooter = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 92, Padding = new Padding(8) };
            sort = Check("Automatically sort by time zone (west to east)", draft.SortZones);
            locFooter.Controls.Add(sort); locFooter.SetFlowBreak(sort, true);
            var add = new Button { Text = "Add clock", AutoSize = true }; var remove = new Button { Text = "Remove", AutoSize = true };
            var up = new Button { Text = "Move up", AutoSize = true }; var down = new Button { Text = "Move down", AutoSize = true };
            locFooter.Controls.AddRange(new Control[] { add, remove, up, down });
            grid = new DataGridView { Dock = DockStyle.Fill, AllowUserToAddRows = false, AllowUserToDeleteRows = false, RowHeadersVisible = false, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, SelectionMode = DataGridViewSelectionMode.FullRowSelect, MultiSelect = false, BackgroundColor = Color.White };
            grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Label above clock", FillWeight = 38, MaxInputLength = 40 });
            var zoneCol = new DataGridViewComboBoxColumn { HeaderText = "Windows time zone", FillWeight = 62, DisplayMember = "Name", ValueMember = "Id", FlatStyle = FlatStyle.Flat };
            var zones = new List<ZoneChoice> { new ZoneChoice("LOCAL", "Local time (follows this PC)") };
            zones.AddRange(TimeZoneInfo.GetSystemTimeZones().Select(z => new ZoneChoice(z.Id, z.DisplayName)));
            zoneCol.DataSource = zones; grid.Columns.Add(zoneCol);
            foreach (Station s in draft.Stations) grid.Rows.Add(s.Label, s.Zone);
            locations.Controls.Add(grid); locations.Controls.Add(locFooter);
            add.Click += delegate { if (grid.Rows.Count < 60) { int n = grid.Rows.Add("NEW CLOCK", "UTC"); grid.CurrentCell = grid.Rows[n].Cells[0]; grid.BeginEdit(true); RefreshPreview(); } };
            remove.Click += delegate { if (grid.CurrentRow != null && grid.Rows.Count > 1) { grid.Rows.RemoveAt(grid.CurrentRow.Index); RefreshPreview(); } };
            up.Click += delegate { MoveRow(-1); }; down.Click += delegate { MoveRow(1); };

            var profileFields = Fields(profiles);
            AddField(profileFields, "Your profiles", new Label { Text = "Export a complete setup: colors, fonts, clock labels, time zones and order. Import loads it into this preview; nothing changes until you click Save changes.", AutoSize = true, MaximumSize = new Size(330, 0) }, 0);
            var export = new Button { Text = "Export profile...", AutoSize = true }; var import = new Button { Text = "Import profile...", AutoSize = true };
            var reset = new Button { Text = "Reset appearance", AutoSize = true };
            AddField(profileFields, "Save a copy", export, 1); AddField(profileFields, "Load a copy", import, 2);
            AddField(profileFields, "Restore defaults", reset, 3);
            AddField(profileFields, "Reset keeps", new Label { Text = "Your clocks, labels, time zones and their order are kept. Appearance changes stay in the preview until saved.", AutoSize = true, MaximumSize = new Size(330, 0) }, 4);
            export.Click += delegate { ExportProfile(); }; import.Click += delegate { ImportProfile(); };
            reset.Click += delegate {
                try {
                    grid.EndEdit(); Preferences current = ReadControls(); Preferences fresh = Preferences.Defaults();
                    fresh.Stations = current.Stations; fresh.SortZones = current.SortZones; fresh.Topmost = current.Topmost; LoadControls(fresh);
                } catch (Exception ex) { MessageBox.Show(this, ex.Message, "Finish editing your clocks first", MessageBoxButtons.OK, MessageBoxIcon.Information); }
            };
            about.Controls.Add(new Label { Dock = DockStyle.Fill, Padding = new Padding(24), Text = "WORLD CLOCK WALL  /  CUSTOMIZATION EDITION\r\n\r\nAn offline clock wall, built locally for you.\r\n\r\nNo accounts, advertising, tracking, network requests, or startup tasks. It uses your PC clock and Windows time-zone rules.\r\n\r\nZulu / UTC never changes for daylight saving time. Other clocks follow their regional rules.\r\n\r\nTry Segoe Print for handwriting or Segoe Script for cursive. Labels and digital numbers shrink to fit long text when needed.\r\n\r\nF11: full screen. Escape: exit full screen.\r\n\r\nSettings: " + Store.FilePath + "\r\n\r\nSource and rebuild script are included. This is a locally built unsigned app, not an official government product.", AutoSize = false });

            var previewPanel = new Panel { Dock = DockStyle.Right, Width = 352, Padding = new Padding(8) };
            var previewTitle = new Label { Text = "LIVE PREVIEW", Dock = DockStyle.Top, Height = 38, TextAlign = ContentAlignment.MiddleCenter, Font = new Font("Segoe UI", 11, FontStyle.Bold) };
            previewStatus = new Label { Dock = DockStyle.Bottom, Height = 58, TextAlign = ContentAlignment.MiddleCenter };
            preview = new Wall(draft.Clone()) { Dock = DockStyle.Fill };
            previewPanel.Controls.Add(preview); previewPanel.Controls.Add(previewTitle); previewPanel.Controls.Add(previewStatus);
            var bottom = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 54, FlowDirection = FlowDirection.RightToLeft, Padding = new Padding(8) };
            var cancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, AutoSize = true };
            var save = new Button { Text = "Save changes", AutoSize = true };
            bottom.Controls.Add(cancel); bottom.Controls.Add(save); CancelButton = cancel; AcceptButton = save;
            save.Click += delegate { SaveDraft(); };
            Controls.Add(tabs); Controls.Add(previewPanel); Controls.Add(bottom);

            theme.SelectedIndexChanged += delegate {
                if (loading) return;
                Preferences p = ReadControls(); p.ApplyTheme((string)theme.SelectedItem); LoadColors(p); RefreshPreview();
            };
            labelFont.SelectedIndexChanged += delegate { RefreshPreview(); }; digitalFont.SelectedIndexChanged += delegate { RefreshPreview(); };
            labelSize.ValueChanged += delegate { RefreshPreview(); }; digitalSize.ValueChanged += delegate { RefreshPreview(); };
            opacity.ValueChanged += delegate { RefreshPreview(); }; size.ValueChanged += delegate { RefreshPreview(); };
            foreach (CheckBox c in new[] { twentyfour, seconds, numerals, topmost, sort }) c.CheckedChanged += delegate { RefreshPreview(); };
            foreach (CheckBox c in new[] { showSolar, solarTwilight, solarCities }) c.CheckedChanged += delegate { RefreshPreview(); };
            solarHeight.ValueChanged += delegate { RefreshPreview(); };
            grid.CellValueChanged += delegate { RefreshPreview(); }; grid.SelectionChanged += delegate { RefreshPreview(); };
            previewTimer = new Timer { Interval = 1000 }; previewTimer.Tick += delegate { preview.Invalidate(); }; previewTimer.Start();
            loading = false; RefreshPreview();
        }
        protected override void Dispose(bool disposing) {
            if (disposing && previewTimer != null) { previewTimer.Stop(); previewTimer.Dispose(); previewTimer = null; }
            base.Dispose(disposing);
        }
        static TableLayoutPanel Fields(TabPage page) {
            var p = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(18), ColumnCount = 2, AutoScroll = true };
            p.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 184)); p.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100)); page.Controls.Add(p); return p;
        }
        static FlowLayoutPanel Flow(params Control[] controls) { var p = new FlowLayoutPanel { AutoSize = true }; p.Controls.AddRange(controls); return p; }
        static NumericUpDown Number(int min, int max, int value) { return new NumericUpDown { Minimum = min, Maximum = max, Value = value, Width = 90 }; }
        static ComboBox Combo(string[] values, string selected) {
            var c = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 290 }; c.Items.AddRange(values); c.SelectedItem = selected;
            if (c.SelectedIndex < 0 && c.Items.Count > 0) c.SelectedItem = "Segoe UI"; return c;
        }
        static CheckBox Check(string text, bool value) { return new CheckBox { Text = text, Checked = value, AutoSize = true }; }
        static void AddField(TableLayoutPanel p, string caption, Control value, int row) {
            p.RowCount = Math.Max(p.RowCount, row + 1); p.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            p.Controls.Add(new Label { Text = caption, AutoSize = true, Margin = new Padding(0, 9, 8, 12) }, 0, row);
            value.Margin = new Padding(0, 4, 0, 7); p.Controls.Add(value, 1, row);
        }
        Button ColorButton(Color color) {
            var b = new Button { Width = 260, Height = 32, BackColor = color, UseVisualStyleBackColor = false };
            Action caption = delegate { b.Text = ColorTranslator.ToHtml(b.BackColor) + "  -  Choose..."; b.ForeColor = b.BackColor.GetBrightness() > .5 ? Color.Black : Color.White; };
            b.BackColorChanged += delegate { caption(); RefreshPreview(); }; caption();
            b.Click += delegate { using (var d = new ColorDialog { Color = b.BackColor, FullOpen = true }) if (d.ShowDialog(this) == DialogResult.OK) b.BackColor = d.Color; };
            return b;
        }
        void LoadColors(Preferences p) {
            bool wasLoading = loading; loading = true; Palette c = new Palette(p);
            accent.BackColor = c.Accent; ink.BackColor = c.Ink; background.BackColor = c.Background; card.BackColor = c.Card;
            face.BackColor = c.Dial; hour.BackColor = c.Hour; minute.BackColor = c.Minute; second.BackColor = c.Second; loading = wasLoading;
        }
        Preferences ReadControls() {
            Preferences p = draft.Clone(); p.Theme = (string)theme.SelectedItem;
            p.LabelFont = (string)labelFont.SelectedItem; p.DigitalFont = (string)digitalFont.SelectedItem;
            p.LabelSize = (int)labelSize.Value; p.DigitalSize = (int)digitalSize.Value;
            p.Accent = ColorTranslator.ToHtml(accent.BackColor); p.Ink = ColorTranslator.ToHtml(ink.BackColor);
            p.BackgroundColor = ColorTranslator.ToHtml(background.BackColor); p.CardColor = ColorTranslator.ToHtml(card.BackColor); p.FaceColor = ColorTranslator.ToHtml(face.BackColor);
            p.HourColor = ColorTranslator.ToHtml(hour.BackColor); p.MinuteColor = ColorTranslator.ToHtml(minute.BackColor); p.SecondColor = ColorTranslator.ToHtml(second.BackColor);
            p.Opacity = opacity.Value; p.CardWidth = size.Value; p.TwentyFour = twentyfour.Checked;
            p.Seconds = seconds.Checked; p.Numerals = numerals.Checked; p.Topmost = topmost.Checked; p.SortZones = sort.Checked;
            p.ShowSolar = showSolar.Checked; p.SolarTwilight = solarTwilight.Checked; p.SolarCities = solarCities.Checked; p.SolarHeight = (int)solarHeight.Value;
            p.Stations = grid.Rows.Cast<DataGridViewRow>().Select(r => new Station(Convert.ToString(r.Cells[0].Value).Trim(), Convert.ToString(r.Cells[1].Value))).ToList();
            return p;
        }
        void RefreshPreview() {
            if (loading || preview == null) return;
            Preferences p = ReadControls();
            // Keep partially edited grid values out of the renderer until valid.
            try { p.Validate(); } catch { previewStatus.Text = "Finish editing the clock label or time zone\nto update the preview."; return; }
            var selected = grid.CurrentRow;
            int index = selected == null ? 0 : selected.Index;
            Station sample = p.Stations[Math.Min(index, p.Stations.Count - 1)];
            p.Stations = new List<Station> { sample };
            preview.Config = p; preview.Reflow();
            opacityLabel.Text = opacity.Value + "%"; sizeLabel.Text = size.Value + " px";
            previewStatus.Text = "Preview: " + sample.Label + "\n" + opacity.Value + "% opacity applies after Save";
        }
        void LoadControls(Preferences p) {
            p.Validate(); loading = true; draft = p.Clone();
            theme.SelectedItem = p.Theme; labelFont.SelectedItem = p.LabelFont; digitalFont.SelectedItem = p.DigitalFont;
            if (labelFont.SelectedIndex < 0) labelFont.SelectedItem = "Segoe UI";
            if (digitalFont.SelectedIndex < 0) digitalFont.SelectedItem = "Consolas";
            labelSize.Value = p.LabelSize; digitalSize.Value = p.DigitalSize; opacity.Value = p.Opacity; size.Value = p.CardWidth;
            twentyfour.Checked = p.TwentyFour; seconds.Checked = p.Seconds; numerals.Checked = p.Numerals; topmost.Checked = p.Topmost; sort.Checked = p.SortZones;
            showSolar.Checked = p.ShowSolar; solarTwilight.Checked = p.SolarTwilight; solarCities.Checked = p.SolarCities; solarHeight.Value = p.SolarHeight;
            LoadColors(p); grid.Rows.Clear(); foreach (Station s in p.Stations) grid.Rows.Add(s.Label, s.Zone);
            loading = false; RefreshPreview();
        }
        void MoveRow(int direction) {
            if (grid.CurrentRow == null) return;
            grid.EndEdit(); int from = grid.CurrentRow.Index, to = from + direction;
            if (to < 0 || to >= grid.Rows.Count) return;
            loading = true;
            object a = grid.Rows[from].Cells[0].Value, b = grid.Rows[from].Cells[1].Value;
            grid.Rows[from].Cells[0].Value = grid.Rows[to].Cells[0].Value; grid.Rows[from].Cells[1].Value = grid.Rows[to].Cells[1].Value;
            grid.Rows[to].Cells[0].Value = a; grid.Rows[to].Cells[1].Value = b; grid.CurrentCell = grid.Rows[to].Cells[0]; sort.Checked = false;
            loading = false; RefreshPreview();
        }
        void SaveDraft() {
            try { grid.EndEdit(); Preferences p = ReadControls(); p.Validate(); Store.Save(p); Result = p; DialogResult = DialogResult.OK; Close(); }
            catch (Exception ex) { MessageBox.Show(this, ex.Message, "Could not save settings", MessageBoxButtons.OK, MessageBoxIcon.Information); }
        }
        void ExportProfile() {
            try {
                grid.EndEdit(); Preferences p = ReadControls(); p.Validate();
                using (var dialog = new SaveFileDialog { Title = "Export clock profile", Filter = "World Clock profile (*.xml)|*.xml", FileName = "My Clock Profile.xml", DefaultExt = "xml", AddExtension = true })
                    if (dialog.ShowDialog(this) == DialogResult.OK) { Store.WriteProfile(p, dialog.FileName); previewStatus.Text = "Profile exported.\nYour active settings have not changed."; }
            } catch (Exception ex) { MessageBox.Show(this, ex.Message, "Could not export profile", MessageBoxButtons.OK, MessageBoxIcon.Information); }
        }
        void ImportProfile() {
            try {
                using (var dialog = new OpenFileDialog { Title = "Import clock profile", Filter = "World Clock profile (*.xml)|*.xml", CheckFileExists = true })
                    if (dialog.ShowDialog(this) == DialogResult.OK) { LoadControls(Store.ReadProfile(dialog.FileName)); previewStatus.Text = "Profile loaded into preview.\nClick Save changes to apply it."; }
            } catch (Exception ex) { MessageBox.Show(this, ex.Message, "Could not import profile", MessageBoxButtons.OK, MessageBoxIcon.Information); }
        }
        internal void SelectClockTab() { tabs.SelectedIndex = 2; }
        internal void SelectColorsTab() { tabs.SelectedIndex = 1; }
        internal void VerifyEditor() {
            if ((string)theme.SelectedItem != draft.Theme || (string)labelFont.SelectedItem != draft.LabelFont || (string)digitalFont.SelectedItem != draft.DigitalFont) throw new Exception("Appearance selection mismatch.");
            if (grid.Rows.Count != draft.Stations.Count) throw new Exception("Clock editor rows mismatch.");
            grid.CurrentCell = grid.Rows[0].Cells[0]; string first = Convert.ToString(grid.Rows[0].Cells[0].Value); MoveRow(1);
            if (sort.Checked || Convert.ToString(grid.Rows[1].Cells[0].Value) != first) throw new Exception("Manual reorder failed.");
            if (draft.Stations[0].Label != first) throw new Exception("Unsaved edits changed source settings.");
            hour.BackColor = Color.Orange; minute.BackColor = Color.DeepSkyBlue; labelSize.Value = 22; digitalSize.Value = 38;
            if (new Palette(preview.Config).Hour != Color.Orange || preview.Config.LabelSize != 22 || preview.Config.DigitalSize != 38) throw new Exception("Live preview failed.");
            theme.SelectedItem = "Amber";
            if (preview.Config.Theme != "Amber" || new Palette(preview.Config).Background.ToArgb() != ColorTranslator.FromHtml("#15120D").ToArgb()) throw new Exception("Preset preview failed.");
            if (ReadControls().Stations.Count != draft.Stations.Count || sort.Checked) throw new Exception("Preset changed clocks or order.");
        }
        public class ZoneChoice {
            public string Id { get; set; } public string Name { get; set; }
            public ZoneChoice(string id, string name) { Id = id; Name = name; }
        }
    }
}
