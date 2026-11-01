using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace WorldClock {
    // NOAA's fractional-year approximation; display-grade geometric solar position.
    // https://gml.noaa.gov/grad/solcalc/solareqns.PDF
    public struct SunPosition {
        public double Latitude, Longitude;
        public static double Radians(double degrees) { return degrees * Math.PI / 180; }
        public static double Wrap(double degrees) { return ((degrees + 180) % 360 + 360) % 360 - 180; }
        public static SunPosition At(DateTimeOffset instant) {
            DateTime utc = instant.UtcDateTime;
            double hour = utc.TimeOfDay.TotalHours;
            double gamma = 2 * Math.PI / (DateTime.IsLeapYear(utc.Year) ? 366 : 365) * (utc.DayOfYear - 1 + (hour - 12) / 24);
            double eq = 229.18 * (0.000075 + 0.001868 * Math.Cos(gamma) - 0.032077 * Math.Sin(gamma) - 0.014615 * Math.Cos(2 * gamma) - 0.040849 * Math.Sin(2 * gamma));
            double decl = 0.006918 - 0.399912 * Math.Cos(gamma) + 0.070257 * Math.Sin(gamma) - 0.006758 * Math.Cos(2 * gamma) + 0.000907 * Math.Sin(2 * gamma) - 0.002697 * Math.Cos(3 * gamma) + 0.00148 * Math.Sin(3 * gamma);
            return new SunPosition { Latitude = decl * 180 / Math.PI, Longitude = Wrap((720 - utc.TimeOfDay.TotalMinutes - eq) / 4) };
        }
        public double SineElevation(double latitude, double longitude) {
            double lat = Radians(latitude), dec = Radians(Latitude);
            return Math.Sin(lat) * Math.Sin(dec) + Math.Cos(lat) * Math.Cos(dec) * Math.Cos(Radians(longitude - Longitude));
        }
        public double Elevation(double lat, double lon) { return Math.Asin(Math.Max(-1, Math.Min(1, SineElevation(lat, lon)))) * 180 / Math.PI; }
        public string Phase(double lat, double lon) {
            double e = Elevation(lat, lon);
            return e >= 0 ? "Daylight" : e >= -6 ? "Civil twilight" : e >= -12 ? "Nautical twilight" : e >= -18 ? "Astronomical twilight" : "Night";
        }
    }
    public class SolarMap : Control {
        public Preferences Config;
        public DateTimeOffset? FixedUtc;
        Bitmap cached;
        string cacheKey;
        Rectangle mapBounds;
        ToolTip tip = new ToolTip();
        string lastTip;
        static readonly List<PointF[][]> land = LoadLand();
        class City {
            public string Name, Zone;
            public double Lat, Lon;
            public City(string name, double lat, double lon, string zone) { Name = name; Lat = lat; Lon = lon; Zone = zone; }
        }
        static readonly City[] cities = {
            new City("Honolulu",21.307,-157.858,"Hawaiian Standard Time"), new City("Anchorage",61.218,-149.900,"Alaskan Standard Time"),
            new City("Redmond",47.674,-122.122,"Pacific Standard Time"), new City("Denver",39.739,-104.990,"Mountain Standard Time"),
            new City("Chicago",41.878,-87.630,"Central Standard Time"), new City("Washington, DC",38.907,-77.037,"Eastern Standard Time"),
            new City("London",51.507,-0.128,"GMT Standard Time"), new City("Paris",48.857,2.352,"Romance Standard Time"),
            new City("Moscow",55.756,37.617,"Russian Standard Time"), new City("Beijing",39.904,116.407,"China Standard Time"),
            new City("Tokyo",35.676,139.650,"Tokyo Standard Time")
        };
        public SolarMap(Preferences p) { Config = p; DoubleBuffered = true; ResizeRedraw = true; Height = p.SolarHeight; }
        protected override void Dispose(bool disposing) {
            if (disposing) { if (cached != null) cached.Dispose(); tip.Dispose(); }
            base.Dispose(disposing);
        }
        static List<PointF[][]> LoadLand() {
            var result = new List<PointF[][]>();
            using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("WorldClock.Land"))
            using (var reader = new BinaryReader(stream)) {
                int count = reader.ReadInt32();
                for (int i = 0; i < count; i++) {
                    var rings = new PointF[reader.ReadInt32()][];
                    for (int r = 0; r < rings.Length; r++) {
                        rings[r] = new PointF[reader.ReadInt32()];
                        for (int n = 0; n < rings[r].Length; n++) rings[r][n] = new PointF(reader.ReadSingle(), reader.ReadSingle());
                    }
                    result.Add(rings);
                }
            }
            return result;
        }
        static PointF Project(double lat, double lon, Rectangle r) {
            return new PointF(r.Left + (float)((lon + 180) / 360 * r.Width), r.Top + (float)((90 - lat) / 180 * r.Height));
        }
        public void RefreshMap() { cacheKey = null; Invalidate(); }
        protected override void OnPaint(PaintEventArgs e) {
            if (Width < 50 || Height < 80) return;
            DateTimeOffset now = FixedUtc ?? DateTimeOffset.UtcNow;
            Palette c = new Palette(Config);
            string key = now.UtcDateTime.ToString("yyyyMMddHHmm", CultureInfo.InvariantCulture) + ":" + Width + ":" + Height + ":" + c.Background.ToArgb() + ":" + c.Card.ToArgb() + ":" + c.Accent.ToArgb() + ":" + c.Ink.ToArgb() + ":" + Config.SolarCities + ":" + Config.SolarTwilight;
            if (cached == null || cacheKey != key) {
                if (cached != null) cached.Dispose(); cached = new Bitmap(Width, Height);
                using (Graphics g = Graphics.FromImage(cached)) Render(g, now, c);
                cacheKey = key;
            }
            e.Graphics.DrawImageUnscaled(cached, 0, 0);
        }
        void Render(Graphics g, DateTimeOffset now, Palette c) {
            g.Clear(c.Background); g.SmoothingMode = SmoothingMode.AntiAlias;
            SunPosition sun = SunPosition.At(now);
            Wall.DrawText(g, "SOLAR WORLD MAP", new RectangleF(16, 3, 220, 26), "Segoe UI", 13, c.Accent, FontStyle.Bold);
            Wall.DrawText(g, now.UtcDateTime.ToString("dd MMM  HH:mm 'Z'", CultureInfo.InvariantCulture), new RectangleF(Width - 220, 3, 200, 26), "Consolas", 13, c.Muted, FontStyle.Regular);
            int availableH = Height - 68;
            int mapW = Math.Min(Width - 36, availableH * 2);
            mapBounds = new Rectangle((Width - mapW) / 2, 34, mapW, mapW / 2);
            bool light = Config.Theme == "Ivory";
            Color ocean = light ? Color.FromArgb(190, 209, 219) : Color.FromArgb(24, 49, 66);
            Color earth = light ? Color.FromArgb(222, 218, 190) : Color.FromArgb(113, 139, 143);
            using (Brush b = new SolidBrush(ocean)) g.FillRectangle(b, mapBounds);
            using (Brush b = new SolidBrush(earth))
            using (Pen p = new Pen(Color.FromArgb(85, c.Muted), .6f)) {
                foreach (PointF[][] polygon in land) using (var path = new GraphicsPath(FillMode.Alternate)) {
                    foreach (PointF[] ring in polygon) {
                        var points = new PointF[ring.Length];
                        for (int n = 0; n < ring.Length; n++) points[n] = Project(ring[n].Y, ring[n].X, mapBounds);
                        if (points.Length >= 3) path.AddPolygon(points);
                    }
                    g.FillPath(b, path); g.DrawPath(p, path);
                }
            }
            using (Pen grid = new Pen(Color.FromArgb(35, c.Ink), 1)) {
                for (int lat = -60; lat <= 60; lat += 30) { var y = Project(lat, 0, mapBounds).Y; g.DrawLine(grid, mapBounds.Left, y, mapBounds.Right, y); }
                for (int lon = -150; lon <= 150; lon += 30) { var x = Project(0, lon, mapBounds).X; g.DrawLine(grid, x, mapBounds.Top, x, mapBounds.Bottom); }
            }
            using (Bitmap shade = NightShade(mapBounds.Width, mapBounds.Height, sun, Config.SolarTwilight)) g.DrawImageUnscaled(shade, mapBounds.Location);
            DrawTerminator(g, sun, mapBounds, c.Accent);
            if (Config.SolarCities) foreach (City city in cities) {
                PointF pt = Project(city.Lat, city.Lon, mapBounds);
                using (Brush b = new SolidBrush(c.Ink)) g.FillEllipse(b, pt.X - 2.5f, pt.Y - 2.5f, 5, 5);
                using (Pen p = new Pen(c.Background, 1)) g.DrawEllipse(p, pt.X - 2.5f, pt.Y - 2.5f, 5, 5);
            }
            PointF solar = Project(sun.Latitude, sun.Longitude, mapBounds);
            using (Pen p = new Pen(Color.FromArgb(255, 212, 105), 1.5f)) {
                g.DrawEllipse(p, solar.X - 6, solar.Y - 6, 12, 12);
                for (int n = 0; n < 8; n++) { double a = n * Math.PI / 4; g.DrawLine(p, solar.X + (float)Math.Cos(a) * 9, solar.Y + (float)Math.Sin(a) * 9, solar.X + (float)Math.Cos(a) * 12, solar.Y + (float)Math.Sin(a) * 12); }
            }
            using (Brush b = new SolidBrush(Color.FromArgb(255, 212, 105))) g.FillEllipse(b, solar.X - 3, solar.Y - 3, 6, 6);
            using (Pen p = new Pen(c.Edge, 1)) g.DrawRectangle(p, mapBounds);
            if (mapBounds.Left >= 170) {
                var left = new RectangleF(18, 60, mapBounds.Left - 34, 34);
                Wall.DrawText(g, "DAY / NIGHT", left, "Segoe UI", 15, c.Ink, FontStyle.Bold);
                Wall.DrawText(g, "Sun overhead", new RectangleF(left.X, 104, left.Width, 22), "Segoe UI", 12, c.Muted, FontStyle.Regular);
                string latText = Math.Abs(sun.Latitude).ToString("0.0", CultureInfo.InvariantCulture) + "° " + (sun.Latitude >= 0 ? "N" : "S");
                string lonText = Math.Abs(sun.Longitude).ToString("0.0", CultureInfo.InvariantCulture) + "° " + (sun.Longitude >= 0 ? "E" : "W");
                Wall.DrawText(g, latText + "  /  " + lonText, new RectangleF(left.X, 128, left.Width, 24), "Consolas", 13, c.Ink, FontStyle.Regular);
                var right = new RectangleF(mapBounds.Right + 16, 80, Width - mapBounds.Right - 34, 25);
                Wall.DrawText(g, "DAYLIGHT", right, "Segoe UI", 13, earth, FontStyle.Bold);
                Wall.DrawText(g, Config.SolarTwilight ? "TWILIGHT  /  0° to -18°" : "DAY / NIGHT BOUNDARY", new RectangleF(right.X, 112, right.Width, 24), "Segoe UI", 12, c.Muted, FontStyle.Regular);
                Wall.DrawText(g, "NIGHT", new RectangleF(right.X, 144, right.Width, 24), "Segoe UI", 13, c.Muted, FontStyle.Bold);
                Wall.DrawText(g, "Updates each minute", new RectangleF(right.X, 185, right.Width, 22), "Segoe UI", 11, c.Muted, FontStyle.Regular);
            }
            string legend = Config.SolarTwilight ? "Bright = day   /   shaded bands = twilight   /   dark = night" : "Bright = day   /   dark = night";
            Wall.DrawText(g, legend + (Config.SolarCities ? "   •   Hover dots for reference cities" : ""), new RectangleF(12, Height - 29, Width - 24, 23), "Segoe UI", 11, c.Muted, FontStyle.Regular);
        }
        static Bitmap NightShade(int width, int height, SunPosition sun, bool twilight) {
            var bitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb);
            var data = bitmap.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
            byte[] pixels = new byte[data.Stride * height];
            double dec = SunPosition.Radians(sun.Latitude); double sd = Math.Sin(dec), cd = Math.Cos(dec);
            double[] cosLon = new double[width];
            for (int x = 0; x < width; x++) cosLon[x] = Math.Cos(SunPosition.Radians((x + .5) / width * 360 - 180 - sun.Longitude));
            double civil = Math.Sin(SunPosition.Radians(-6)), nautical = Math.Sin(SunPosition.Radians(-12)), astro = Math.Sin(SunPosition.Radians(-18));
            for (int y = 0; y < height; y++) {
                double lat = SunPosition.Radians(90 - (y + .5) / height * 180); double sl = Math.Sin(lat) * sd, cl = Math.Cos(lat) * cd;
                for (int x = 0; x < width; x++) {
                    double e = sl + cl * cosLon[x];
                    byte alpha = e >= 0 ? (byte)0 : !twilight ? (byte)185 : e >= civil ? (byte)55 : e >= nautical ? (byte)105 : e >= astro ? (byte)150 : (byte)185;
                    int at = y * data.Stride + x * 4; pixels[at] = 24; pixels[at + 1] = 12; pixels[at + 2] = 7; pixels[at + 3] = alpha;
                }
            }
            Marshal.Copy(pixels, 0, data.Scan0, pixels.Length); bitmap.UnlockBits(data); return bitmap;
        }
        static void DrawTerminator(Graphics g, SunPosition sun, Rectangle r, Color color) {
            double d = SunPosition.Radians(sun.Latitude), l = SunPosition.Radians(sun.Longitude);
            PointF? previous = null;
            using (Pen p = new Pen(Color.FromArgb(165, color), 1)) for (int n = 0; n <= 720; n++) {
                double a = n * Math.PI / 360, ca = Math.Cos(a), sa = Math.Sin(a);
                double x = -Math.Sin(l) * ca - Math.Sin(d) * Math.Cos(l) * sa;
                double y = Math.Cos(l) * ca - Math.Sin(d) * Math.Sin(l) * sa;
                double z = Math.Cos(d) * sa;
                PointF pt = Project(Math.Asin(z) * 180 / Math.PI, Math.Atan2(y, x) * 180 / Math.PI, r);
                if (previous.HasValue && Math.Abs(pt.X - previous.Value.X) < r.Width / 2) g.DrawLine(p, previous.Value, pt);
                previous = pt;
            }
        }
        protected override void OnMouseMove(MouseEventArgs e) {
            base.OnMouseMove(e);
            if (!mapBounds.Contains(e.Location)) { SetTip(""); return; }
            DateTimeOffset now = FixedUtc ?? DateTimeOffset.UtcNow; SunPosition sun = SunPosition.At(now);
            if (Config.SolarCities) foreach (City city in cities) {
                PointF pt = Project(city.Lat, city.Lon, mapBounds);
                if (Math.Abs(pt.X - e.X) < 6 && Math.Abs(pt.Y - e.Y) < 6) {
                    SetTip(city.Name + " • " + Time.At(now, city.Zone).ToString("HH:mm", CultureInfo.InvariantCulture) + "\n" + sun.Phase(city.Lat, city.Lon) + " (reference city)"); return;
                }
            }
            double lon = (e.X - mapBounds.Left) / (double)mapBounds.Width * 360 - 180, lat = 90 - (e.Y - mapBounds.Top) / (double)mapBounds.Height * 180;
            SetTip(sun.Phase(lat, lon) + " • approximate solar elevation " + sun.Elevation(lat, lon).ToString("0.0", CultureInfo.InvariantCulture) + "°");
        }
        void SetTip(string text) { if (text != lastTip) { lastTip = text; tip.SetToolTip(this, text); } }
        internal static void Verify(string folder) {
            var june = SunPosition.At(new DateTimeOffset(2026, 6, 21, 12, 0, 0, TimeSpan.Zero));
            var december = SunPosition.At(new DateTimeOffset(2026, 12, 21, 12, 0, 0, TimeSpan.Zero));
            var march = SunPosition.At(new DateTimeOffset(2026, 3, 20, 12, 0, 0, TimeSpan.Zero));
            if (june.Latitude < 23 || june.Latitude > 24 || december.Latitude > -23 || december.Latitude < -24 || Math.Abs(march.Latitude) > 1) throw new Exception("Solar seasons incorrect.");
            if (june.Elevation(90, 0) < 0 || june.Elevation(-90, 0) > 0) throw new Exception("Polar day/night incorrect.");
            if (Math.Abs(june.Elevation(june.Latitude, june.Longitude) - 90) > .001 || Math.Abs(june.Elevation(-june.Latitude, SunPosition.Wrap(june.Longitude + 180)) + 90) > .001) throw new Exception("Subsolar or antisolar position incorrect.");
            var evening = SunPosition.At(new DateTimeOffset(2026, 6, 21, 18, 0, 0, TimeSpan.Zero));
            if (Math.Abs(SunPosition.Wrap(evening.Longitude - june.Longitude) + 90) > 1) throw new Exception("Solar longitude motion incorrect.");
            var equator = new SunPosition { Latitude = 0, Longitude = 0 };
            if (equator.Phase(0, 0) != "Daylight" || equator.Phase(0, 93) != "Civil twilight" || equator.Phase(0, 99) != "Nautical twilight" || equator.Phase(0, 105) != "Astronomical twilight" || equator.Phase(0, 120) != "Night") throw new Exception("Twilight bands incorrect.");
            if (land.Count != 127) throw new Exception("Embedded coastlines missing.");
            using (var control = new SolarMap(Preferences.Defaults())) {
                control.Size = new Size(1260, 320); control.FixedUtc = new DateTimeOffset(2026, 11, 1, 6, 0, 0, TimeSpan.Zero); control.CreateControl();
                using (var bmp = new Bitmap(control.Width, control.Height)) { control.DrawToBitmap(bmp, new Rectangle(Point.Empty, bmp.Size)); bmp.Save(Path.Combine(folder, "preview-solar.png")); }
                control.Config.ApplyTheme("Ivory"); control.Config.SolarTwilight = false; control.Size = new Size(740, 240); control.RefreshMap();
                using (var bmp = new Bitmap(control.Width, control.Height)) { control.DrawToBitmap(bmp, new Rectangle(Point.Empty, bmp.Size)); bmp.Save(Path.Combine(folder, "preview-solar-ivory.png")); }
            }
        }
    }
}
