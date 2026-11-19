import fs from 'node:fs';
import assert from 'node:assert/strict';
import {spawnSync} from 'node:child_process';
import path from 'node:path';
import {fileURLToPath} from 'node:url';
const root=path.resolve(path.dirname(fileURLToPath(import.meta.url)),'..');
const run=spawnSync(process.execPath,[path.join(root,'scripts/audit-602-scenes.mjs')],{encoding:'utf8'});
assert.equal(run.status,0,run.stderr);
const r=JSON.parse(fs.readFileSync(path.join(root,'evidence/ios-package-search/ios602-scene-audit.json'),'utf8'));
assert.equal(r.scenes.length,6);
const home=r.scenes.find(s=>s.scene==='Home');
assert.equal(home.gameObjects,21);assert.equal(home.monoBehaviours,27);
const script=home.scripts.find(s=>s.objectName==='Instance' && s.scriptPaths.includes('Scripts/Assembly-CSharp/Home.cs'));
assert.ok(script,'Home scene must map Instance to exported Home script');
assert.ok(script.legacy140SamePath.includes('Scripts/Assembly-CSharp/Home.cs'));
assert.equal(home.customFieldComponents,0);
const result=r.scenes.find(s=>s.scene==='Result');
assert.ok(result.scripts.some(s=>s.objectName==='Result_PvpCNRank' && s.scriptGuid===null && s.scriptPaths.length===0));
for(const s of r.scenes) {
  assert.match(s.sha256,/^[a-f0-9]{64}$/);
  assert.equal(s.monoBehaviours,s.scripts.length);
  assert.equal(s.missingScriptGuids,s.scripts.filter(x=>!x.scriptPaths.length).length);
}
console.log('PASS: 6 exported scenes, Home object-to-script mapping, 1.4.0 path presence, missing script and audit consistency; static only.');
