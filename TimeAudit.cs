using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace WorldClock {
    // Exports actual application conversions for comparison with independent IANA data.
    public static class TimeAudit {
        public static void Export(string file) {
            var instants = new List<DateTimeOffset> { DateTimeOffset.UtcNow };
            foreach (int month in new[] { 1, 4, 7, 11, 12 }) instants.Add(new DateTimeOffset(2026, month, 15, 12, 0, 0, TimeSpan.Zero));
            foreach (int hour in new[] { 5, 6, 7, 8, 9, 10, 11 }) {
                var t = new DateTimeOffset(2026, 11, 1, hour, 0, 0, TimeSpan.Zero);
                instants.Add(t.AddSeconds(-1)); instants.Add(t); instants.Add(t.AddSeconds(1));
            }
            foreach (int hour in new[] { 7, 8, 9, 10, 11 }) {
                var t = new DateTimeOffset(2027, 3, 14, hour, 0, 0, TimeSpan.Zero);
                instants.Add(t.AddSeconds(-1)); instants.Add(t); instants.Add(t.AddSeconds(1));
            }
            foreach (var date in new[] { new DateTimeOffset(2026, 10, 25, 1, 0, 0, TimeSpan.Zero), new DateTimeOffset(2027, 3, 28, 1, 0, 0, TimeSpan.Zero) }) {
                instants.Add(date.AddSeconds(-1)); instants.Add(date); instants.Add(date.AddSeconds(1));
            }
            Directory.CreateDirectory(Path.GetDirectoryName(file));
            using (var output = new StreamWriter(file)) {
                output.WriteLine("utc\tlabel\twindows_zone\tlocal_time\toffset_minutes\tdst");
                foreach (var utc in instants) foreach (var station in Preferences.Defaults().Stations) {
                    var tz = Time.Zone(station.Zone); var local = Time.At(utc, station.Zone);
                    output.WriteLine(utc.ToString("o", CultureInfo.InvariantCulture) + "\t" + station.Label + "\t" + tz.Id + "\t" + local.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture) + "\t" + local.Offset.TotalMinutes.ToString(CultureInfo.InvariantCulture) + "\t" + tz.IsDaylightSavingTime(local));
                }
            }
        }
    }
}
