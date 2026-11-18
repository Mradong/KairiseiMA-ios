import fs from 'node:fs';
import path from 'node:path';
import crypto from 'node:crypto';
import {fileURLToPath} from 'node:url';
const root=path.resolve(path.dirname(fileURLToPath(import.meta.url)),'..');
const ci=path.join(root,'ci-upload');
const hash=bytes=>crypto.createHash('sha256').update(bytes).digest('hex');
const file=path.join(ci,'ci-source-manifest.json');
const manifest=JSON.parse(fs.readFileSync(file,'utf8'));
for(const record of manifest.files)if(hash(fs.readFileSync(path.join(ci,record.path)))!==record.sha256)throw new Error('Unreviewed CI source change: '+record.path);
const reports=['registered-battle-differential-tests.json','session-differential-tests.json','registered-skill-query-differential-tests.json','registration-state-tests.json'];
const dllHash=hash(fs.readFileSync(path.join(root,'native/build/windows-amd64/kairisei_battlecore_rebuilt.dll')));
for(const name of reports) {
  const report=JSON.parse(fs.readFileSync(path.join(root,'evidence/native',name),'utf8'));
  if(!report.success||(report.rebuilt&&report.rebuilt.sha256!==dllHash))throw new Error('Current DLL lacks required evidence: '+name);
}
const wave=JSON.parse(fs.readFileSync(path.join(root,'evidence/native/registered-battle-differential-tests.json'),'utf8'));
if(wave.counts.waveCases!==12||wave.counts.managedScenarios!==3)throw new Error('Missing actual registered-wave and C# consumer comparisons.');
const selected=[
 ...fs.readdirSync(path.join(root,'native/bridge')).filter(n=>n.endsWith('.go')||['go.mod','go.sum'].includes(n)).map(n=>'native/bridge/'+n),
 'ios-client/Assets/Recovery/Runtime/NativeRegisteredBattle.cs',
 'tests/RegisteredBattleDifferential.cs','tests/RegisteredWaveCases.cs','tests/RegisteredManagedCases.cs',
 'scripts/test-registered-battle.mjs','scripts/prepare-registered-wave-checkpoint.mjs',
 ...reports.map(n=>'evidence/native/'+n),
 'STATUS.md','evidence/acceptance.json','evidence/feature-coverage.md'
];
const approved=new Set(manifest.files.map(f=>f.path));
for(const relative of selected) {
 const bytes=fs.readFileSync(path.join(root,relative));
 fs.mkdirSync(path.dirname(path.join(ci,relative)),{recursive:true});fs.writeFileSync(path.join(ci,relative),bytes);approved.add(relative);
}
manifest.files=[...approved].sort().map(relative=>{const bytes=fs.readFileSync(path.join(ci,relative));return {path:relative,bytes:bytes.length,sha256:hash(bytes)};});
manifest.generatedAt=new Date().toISOString();manifest.bytes=manifest.files.reduce((n,f)=>n+f.bytes,0);
manifest.execution='Local source checkpoint after Windows registered-wave and actual C# consumer comparisons. Not uploaded or cloud built. Run 44293950030 is an earlier commit and cannot certify this source.';
manifest.gameplayReady=false;
const checkpoint={generatedAt:manifest.generatedAt,stage:'partial-registered-wave-consumer',sourcePrepared:true,uploaded:false,cloudBuilt:false,deviceValidated:false,gameplayReady:false,
 windowsDLLSHA256:dllHash,comparisons:wave.counts,evidence:reports.map(n=>'evidence/native/'+n),selectedSources:selected,
 limitations:'Synthetic ordinary mode only. Full registered game inputs, other modes, awakening, enemy programs/gauges, NPC/sphere/buddy content, real UI, ARM64 and iPhone execution remain incomplete.',
 cloudRunForEarlierSource:44293950030};
const checkpointName='evidence/native/registered-wave-checkpoint.json';
const checkpointBytes=Buffer.from(JSON.stringify(checkpoint,null,2)+'\n');
fs.writeFileSync(path.join(root,checkpointName),checkpointBytes);fs.writeFileSync(path.join(ci,checkpointName),checkpointBytes);
manifest.files=manifest.files.filter(f=>f.path!==checkpointName);
manifest.files.push({path:checkpointName,bytes:checkpointBytes.length,sha256:hash(checkpointBytes)});manifest.files.sort((a,b)=>a.path.localeCompare(b.path));
manifest.bytes=manifest.files.reduce((n,f)=>n+f.bytes,0);
fs.writeFileSync(file,JSON.stringify(manifest,null,2)+'\n');
console.log(JSON.stringify({files:manifest.files.length,bytes:manifest.bytes,windowsDLLSHA256:dllHash,uploaded:false,cloudBuilt:false},null,2));
