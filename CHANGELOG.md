# Changes

## 1.2.1 — Time-zone audit and clearer labels

- Verified all 13 default clock labels against independent IANA time-zone data: 624 conversions across live time, seasonal dates, and US/European DST transition boundaries, with zero mismatches (tzdata 2026.6).
- Show daylight/standard abbreviations alongside numeric UTC offsets: for example, `CDT / UTC -05:00` versus `EST / UTC -05:00` during the staggered fall-back period.
- Added `--audit-times` and the development-only `audit-timezones.py` cross-check. The application still works entirely offline.
- Added regression checks for the temporary matching Chicago/Eastern hours and Central's later transition to CST. No clock values were shifted to force an artificial difference.
- Prepared a source-only Git repository; local settings, screenshots, binaries, and machine-specific helpers are excluded.

## 1.2 — Solar world map

- Offline daylight/night and twilight shading, overhead-sun marker, reference-city markers, and map controls.
- Embedded public-domain Natural Earth land data and NOAA approximate solar equations.

## 1.1 — Expanded customization

- Live preview, separate colors for clock faces/hands/backgrounds, font sizes, theme presets, and local profile import/export.

## 1.0 — Clock wall

- Labeled analog and digital clocks, Windows time-zone rules, Zulu/UTC, configurable appearance, and window controls.
