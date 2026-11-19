import fs from 'node:fs';
import path from 'node:path';
import crypto from 'node:crypto';
import assert from 'node:assert/strict';
import {fileURLToPath} from 'node:url';
const root=path.resolve(path.dirname(fileURLToPath(import.meta.url)),'..');
const read=p=>fs.readFileSync(path.join(root,p),'utf8');
const sha=p=>crypto.createHash('sha256').update(fs.readFileSync(path.join(root,p))).digest('hex');
const patch='inputs/ios/cn602/Data/Raw/patch.txt';
const legacy='legacy-unity/ExportedProject/Assets/Scripts/Assembly-CSharp/Home.cs';
const navi='ios602-unity/ExportedProject/Assets/Resources/container/navi/navi.csv.csv';
const lines=read(patch).split(/\r?\n/);
const matches=lines.map((line,index)=>({line:index+1,value:line})).filter(x=>/^02_Map_Assets_Home\.bytes,/i.test(x.value));
assert.equal(matches.length,1,'expected a single legacy Home resource manifest entry');
const old=read(legacy);
assert.ok(old.includes('02_Map_Assets/Home/Home_{0:d5}/') && old.includes('m_home_receive_data.background') && old.includes('naviCsvData.background_id'));
const names=read(navi).split(/\r?\n/);assert.ok(names[0].includes('Live2D'));
const isolated=JSON.parse(read('evidence/ios-package-search/ios-home-isolated-fixture.json').split(/\r?\n/)[1]);
assert.equal(isolated.background,1);assert.equal(isolated.user.navi_type,0);
const report={generatedAt:new Date().toISOString(),sourceIpaSha256:JSON.parse(read('inputs/ios/cn602/import-manifest.json').replace(/^\uFEFF/,'')).sourceSha256,
 legacy140:{path:legacy,sha256:sha(legacy),backgroundLogic:'OnInitializedNavi: use nonzero navi background_id when present, otherwise HomeShow.background; failed resource load tries id 1000.',compatibility:'Reference behavior from 1.4.0, not proven for 6.0.2.'},
 ipa602:{patch:{path:patch,sha256:sha(patch),entries:matches},naviTable:{path:navi,sha256:sha(navi),header:names[0],rows:names.filter(x=>x&&!x.startsWith('#')).length},
 observedLimitation:'The manifest entry alone does not prove a matching bundle is included, accessible, complete, or compatible with Unity 6.'},
 isolatedHome:{source:'evidence/ios-package-search/ios-home-isolated-fixture.json',background:isolated.background,naviType:isolated.user.navi_type,scope:'Disposable isolated service fixture; not a real account or live deployment.'},
 scope:'Static source comparison only; no 6.0.2 method bodies, version compatibility, resource load, Unity execution or phone behavior established.'};
fs.writeFileSync(path.join(root,'evidence/ios-package-search/ios602-home-resource-audit.json'),JSON.stringify(report,null,2)+'\n');
console.log(JSON.stringify(report,null,2));
