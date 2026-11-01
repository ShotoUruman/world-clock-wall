# World Clock Wall

Build with `build.ps1`, then open **WorldClockWall.exe**. Requires Windows with Microsoft .NET Framework 4.x (included with supported Windows desktop installations). No package installation is needed.

A local, offline Windows clock wall with government-office styling: labels above analog faces, digital times below. It is not affiliated with a government agency.

Open source under the [MIT license](LICENSE). The included Natural Earth map data is public domain; see `assets/SOURCE.txt` for its provenance.

## Controls

- **Customize → Appearance**: Midnight, Ivory, Amber, or Arctic preset; installed label and digital fonts with separate sizes; 30–100% window opacity; clock size; 12/24-hour digital time; seconds; analog numerals; optional always-on-top. The handwriting shortcut selects Segoe Print.
- **Customize → Colors**: independently choose text, accent, window background, card background, analog face, and hour/minute/second hand colors. Different hand colors can help with learning analog.
- **Live preview**: appearance changes update the sample clock immediately. Select a row in Clocks & order to preview that location. Actual window opacity applies after Save so the settings remain readable.
- **Customize → Clocks & order**: edit labels, select any installed Windows time zone, add/remove clocks, automatically order by current UTC offset, or arrange manually with Move up/down. Moving a row switches to manual order.
- **Save changes** persists preferences; **Cancel** leaves them unchanged.
- **Customize → Profiles**: export/import a complete local XML profile, including all colors, fonts, labels, zones, and order. Imported profiles are previewed before Save; Cancel leaves active settings alone. Reset appearance preserves clocks and their order. Changing a color preset also preserves the clock list.
- **Customize → Solar map**: show/hide the world map, resize it from 180–480 px tall, toggle twilight bands, and toggle reference-city dots. Hover a dot for the city's time and solar phase. The dots represent the original reference cities, not inferred positions for newly added clocks.
- **F11** toggles full screen on the current monitor. **Escape** exits full screen.
- Resize or maximize for more clocks per row; scroll to see additional rows.
- For readable handwriting, try **Segoe Print** for labels; for cursive, **Segoe Script**. Digital numbers default to **Consolas**.

The default 13 clocks are Honolulu, Anchorage, Redmond/Pacific, Denver, Chicago, Washington DC, local time, **ZULU / UTC**, London, Paris, Moscow, Beijing, and Tokyo. Zulu is highlighted. Default opacity is 100%.

## Timekeeping

Uses the PC's system clock and installed Windows time-zone rules. Regional daylight saving transitions happen automatically; UTC never falls back. No independent internet time synchronization is performed. Each analog dial uses 12 hours, with the digital display and date clarifying the day and AM/PM. Under automatic order, clocks with equal UTC offsets keep their configured relative order.

The default locations show abbreviations alongside their current numeric UTC offsets. Abbreviations are not unique worldwide (for example, CST can mean US Central or China time), so the UTC offset is always displayed. Additional Windows zones without an explicit abbreviation use the unambiguous numeric offset.

During the US fall-back night, neighboring zones can temporarily show the same hour: Eastern changes at 06:00 UTC and Central at 07:00 UTC in 2026. In between, Chicago is CDT (UTC−05:00) and Washington is EST (UTC−05:00). This is expected: the switch happens at 2 a.m. in each local daylight time zone. See [NIST's DST rules](https://www.nist.gov/pml/time-and-frequency-division/popular-links/daylight-saving-time-dst).

## Solar world map

Version 1.2 adds an offline world map above the clocks. Bright areas are in daylight; shaded bands show civil (0 to -6 degrees), nautical (-6 to -12), and astronomical (-12 to -18) twilight; dark areas are night. The accent line is the geometric day/night boundary. The sun symbol marks the subsolar point, where the sun is overhead. The map updates each UTC minute and does not jump when a local clock changes for DST.

Solar position uses NOAA's approximate fractional-year equations: https://gml.noaa.gov/grad/solcalc/solareqns.PDF . It is a visual guide, not an exact sunrise/sunset timetable; terrain and atmospheric refraction are not modeled.

Coastlines are public-domain Natural Earth 1:110m land data: https://www.naturalearthdata.com/about/terms-of-use/ . Source GeoJSON, provenance/hash, and the compact embedded resource are in `assets`. `fetch-map-data.py` was used once during development to fetch and convert the data; the app and normal build never run it or access the network.

## Trust and files

Built locally with the Microsoft .NET Framework compiler already installed on this PC. No downloaded software packages, browser runtime, runtime network calls, analytics, advertisements, account, admin permission, service, or startup entry. Source (`WorldClock.cs`, `SettingsForm.cs`, `SolarMap.cs`), embedded map data, manifest, and rebuild script are included. This is a custom unsigned executable, not a third-party audited or signed commercial release.

Settings: `%LOCALAPPDATA%\WorldClockWall\settings.xml`. The previous saved version is preserved as `settings.xml.bak`. Nothing is written to the existing Windows Clock app. Close the app and remove the app folder, desktop shortcut, and this settings folder to uninstall.

## Build and verification

Run `build.ps1` in PowerShell to rebuild using the installed Microsoft compiler. No internet connection is required.

`WorldClockWall.exe --self-test` runs without showing windows. It checks DST boundaries, UTC continuity, fractional offsets, ordering, XML profiles and backups, compatibility with earlier settings, live preview controls, and isolated draft edits. It renders preview images and writes test-only XML fixtures under `verification`. Read `test-results.txt` afterward. Screenshots are illustrative test previews, not live clock readings. Tests do not write production preferences.

Version 1.1 adds the expanded customization above. The pre-update source and executable are preserved under `backups/before-customization-v2`. Existing saved settings load with defaults for the new fields.

Version 1.2's pre-update backup is `backups/before-solar-map`. The solar calculation/rendering tests also cover solstices, equinox, polar day/night, subsolar/antisolar points, westward movement, twilight bands, and embedded coastlines. A candidate can be built without touching the running executable using `build.ps1 -OutputPath .\WorldClockWall.next.exe`.

The app supports `--no-activate --bounds X Y WIDTH HEIGHT` to open in a specified position without taking keyboard focus. It can be used to place the app on a secondary monitor without interrupting the active window.

## Independent time-zone audit

Run `WorldClockWall.exe --audit-times` to export application conversions under `verification/time-audit.tsv`, then run `python audit-timezones.py`. This optional development script downloads the Python Software Foundation's IANA `tzdata` wheel as data, verifies its published SHA256, and reads its timezone files using Python `zoneinfo`; it does not install or execute the wheel. Requires Python 3.9+ and internet access for this audit only. The app itself has no dependency on Python or downloaded timezone data.

Version 1.2.1 passed 624 comparisons covering all 13 default clocks, seasonal offsets, and US/European fall/spring transition boundaries against tzdata 2026.6. Audit outputs and build artifacts are intentionally excluded from Git.
