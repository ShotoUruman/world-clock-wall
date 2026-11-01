import json,pathlib,struct,urllib.request,hashlib
folder=pathlib.Path(__file__).resolve().parent/'assets';folder.mkdir(exist_ok=True)
url='https://raw.githubusercontent.com/nvkelso/natural-earth-vector/master/geojson/ne_110m_land.geojson'
data=urllib.request.urlopen(url,timeout=30).read();(folder/'ne_110m_land.geojson').write_bytes(data)
obj=json.loads(data);polys=[]
for f in obj['features']:
 g=f['geometry']
 if g['type']=='Polygon':polys.append(g['coordinates'])
 elif g['type']=='MultiPolygon':polys.extend(g['coordinates'])
with (folder/'land.bin').open('wb') as out:
 out.write(struct.pack('<i',len(polys)))
 for poly in polys:
  out.write(struct.pack('<i',len(poly)))
  for ring in poly:
   out.write(struct.pack('<i',len(ring)))
   for lon,lat,*_ in ring:out.write(struct.pack('<ff',lon,lat))
(folder/'SOURCE.txt').write_text('Natural Earth 1:110m land, public domain.\nSource: '+url+'\nLicense: https://www.naturalearthdata.com/about/terms-of-use/\nSHA256 GeoJSON: '+hashlib.sha256(data).hexdigest()+'\nland.bin stores polygon/ring counts and float32 longitude/latitude, generated locally.\n')
print('Packaged',len(polys),'land polygons;',len(data),'source bytes')
