from pathlib import Path
import json, hashlib
root=Path(__file__).resolve().parents[2]
out=root/'Plugin/Resources/Maps/Icebreaker'
layers=out/'Layers'; layers.mkdir(parents=True,exist_ok=True)
names=['Bridge Roof','Bridge','Stairs (Blocked)','Officers Deck','Accommodation 6','Accommodation 5','Accommodation 4','Gym & Canteen','Helipad','Infirmary','Storage & Security','Fuel Pumps','Engine Room','Lower Automation']
levels=list(range(10,-4,-1))
missing=[level for level in levels if not (layers/f'Icebreaker_L{level}.png').exists()]
if missing: raise RuntimeError('Run crop-dynamic-map.ps1 first; missing '+str(missing))

# Calibration 2026-09-05: PlayerPositionText already prints (X, Z, height).
# Compare its FIRST TWO numbers to Cursor, not the first and third.
# Translate the image by player_xy - cursor_xy in map-local coordinates;
# the map container applies its existing rotation to both positions.
# Previous offset (-0.69, -0.44), cursor (-2.61, 68.18), player (-2.69, 61.51).
image_offset_x=-0.77
image_offset_y=-7.11
image_bounds={
 'Min':{'x':round(-19+image_offset_x,2),'y':round(-90+image_offset_y,2)},
 'Max':{'x':round(19+image_offset_x,2),'y':round(94+image_offset_y,2)}
}

# Estimated deck elevations from map spawn data and the retail ~2.7m deck cadence.
centres={-3:9.3,-2:10.3,-1:13.0,0:15.75,1:18.4,2:21.1,3:23.8,4:26.5,5:29.2,6:31.9,7:34.65,8:37.35,9:40.05,10:42.75}
ordered=sorted(centres)
vertical={}
for i,lvl in enumerate(ordered):
    lo=-100 if i==0 else (centres[lvl]+centres[ordered[i-1]])/2
    hi=100 if i==len(ordered)-1 else (centres[lvl]+centres[ordered[i+1]])/2
    vertical[lvl]=(round(lo,3),round(hi,3))
definition={
 'DisplayName':'Icebreaker / Ледокол','Author':'RE3MR (DynamicMaps adaptation by OpenAI Codex)',
 'AuthorLink':'https://reemr.se/Icebreaker/','MapInternalNames':['Suburbs','suburbs'],
 'CoordinateRotation':180,'Bounds':{'Min':{'x':-19,'y':-90},'Max':{'x':19,'y':94}},
 'DefaultLevel':1,'Layers':{},'Labels':[],'StaticMarkers':[]
}
for name,level in zip(names,levels):
    lo,hi=vertical[level]
    definition['Layers'][name]={
      'Level':level,'ImagePath':f'Maps/Icebreaker/Layers/Icebreaker_L{level}.png','TesselationIndex':1,
      'ImageBounds':image_bounds,
      'GameBounds':[{'Min':{'x':-25,'y':-100,'z':lo},'Max':{'x':25,'y':105,'z':hi}}]
    }
(out/'Icebreaker.jsonc').write_text(json.dumps(definition,indent=2,ensure_ascii=False)+'\n',encoding='utf-8')
(out/'CREDITS_AND_LICENSE.txt').write_text('''Icebreaker map artwork version 1.5 by RE3MR.COM\nSource: https://reemr.se/Icebreaker/\nLicense: Creative Commons Attribution-NonCommercial-ShareAlike 4.0 International\nhttps://creativecommons.org/licenses/by-nc-sa/4.0/\n\nModification: the single 7680x4320 overview was cropped into 14 individual deck images for noncommercial use in SPT DynamicMaps. No claim of authorship over the map artwork.\n''',encoding='utf-8')
manifest=[]
for p in sorted(out.rglob('*')):
    if p.is_file():manifest.append({'path':str(p.relative_to(out)),'size':p.stat().st_size,'sha256':hashlib.sha256(p.read_bytes()).hexdigest()})
(Path(__file__).resolve().parent/'manifest.json').write_text(json.dumps(manifest,indent=2),encoding='utf-8')
print('Built',len(levels),'layers; package bytes',sum(x['size'] for x in manifest))
