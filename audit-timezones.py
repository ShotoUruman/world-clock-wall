"""Development-only independent audit. No package installation or app networking.

Reads IANA TZif data from the PSF tzdata wheel, verifies its published SHA256,
then compares every exported app time to Python's zoneinfo implementation.
"""
import csv
import datetime as dt
import hashlib
import io
import json
import pathlib
import re
import urllib.request
import zipfile
from zoneinfo import ZoneInfo

root = pathlib.Path(__file__).resolve().parent
metadata = json.load(urllib.request.urlopen('https://pypi.org/pypi/tzdata/json', timeout=30))
wheel = next(f for f in metadata['urls'] if f['filename'].endswith('.whl'))
payload = urllib.request.urlopen(wheel['url'], timeout=30).read()
assert hashlib.sha256(payload).hexdigest() == wheel['digests']['sha256'], 'tzdata checksum mismatch'
archive = zipfile.ZipFile(io.BytesIO(payload))
mapping = {
    'Hawaiian Standard Time': 'Pacific/Honolulu',
    'Alaskan Standard Time': 'America/Anchorage',
    'Pacific Standard Time': 'America/Los_Angeles',
    'Mountain Standard Time': 'America/Denver',
    'Central Standard Time': 'America/Chicago',
    'Eastern Standard Time': 'America/New_York',
    'UTC': 'Etc/UTC',
    'GMT Standard Time': 'Europe/London',
    'Romance Standard Time': 'Europe/Paris',
    'Russian Standard Time': 'Europe/Moscow',
    'China Standard Time': 'Asia/Shanghai',
    'Tokyo Standard Time': 'Asia/Tokyo',
}
zones = {win: ZoneInfo.from_file(io.BytesIO(archive.read('tzdata/zoneinfo/' + iana)), key=iana) for win, iana in mapping.items()}
rows = list(csv.DictReader((root / 'verification' / 'time-audit.tsv').open(encoding='utf-8-sig'), delimiter='\t'))
failures = []
for row in rows:
    utc = dt.datetime.fromisoformat(re.sub(r'(\.\d{6})\d+', r'\1', row['utc']))
    expected = utc.astimezone(zones[row['windows_zone']])
    actual = (row['local_time'], int(row['offset_minutes']), row['dst'].lower() == 'true')
    reference = (expected.strftime('%Y-%m-%d %H:%M:%S'), int(expected.utcoffset().total_seconds() / 60), bool(expected.dst()))
    if actual != reference:
        failures.append({'row': row, 'iana_expected': reference})
summary = {'tzdata_version': metadata['info']['version'], 'cases': len(rows), 'clock_labels': len(set(r['label'] for r in rows)), 'mismatches': failures, 'result': 'PASS' if not failures else 'FAIL'}
(root / 'verification' / 'independent-time-audit.json').write_text(json.dumps(summary, indent=2))
print(json.dumps(summary, indent=2))
raise SystemExit(bool(failures))
