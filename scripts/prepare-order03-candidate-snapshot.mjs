import fs from 'node:fs';
import path from 'node:path';
import crypto from 'node:crypto';
import {fileURLToPath} from 'node:url';
const root=path.resolve(path.dirname(fileURLToPath(import.meta.url)),'..');
const pkg=path.join(root,'inputs/server-order03/Order03-Combat-Verified-20261123');
const source=path.join(root,'recovered-unity/ExportedProject/Assets/Scripts/Assembly-CSharp');
const out=path.join(root,'test-output/order03-candidate');fs.mkdirSync(out,{recursive:true});
const sha=b=>crypto.createHash('sha256').update(b).digest('hex');
const families=['SKILL_EXTEND_EXEC_ORDER','RARITY','SKILL_TYPE','SKILL_KIND','ATTR','JOB_TYPE','ARTHUR_TYPE','SKILL_PHYSICS_TYPE','SKILL_TARGET','SKILL_EXEC_TIMING','SKILL_ROLE','SKILL_ROLE_TARGET','SKILL_ROLE_BATTLE_PARAM','SKILL_ROLE_KIND_DEBUFF','SKILL_ROLE_KIND_BUFF','SKILL_ROLE_DOT','ENEMY_TRANCE_GAUGE_STATE','ENEMY_AI_TARGET','ENEMY_AI_ORDER_PARTY','ENEMY_AI_ORDER_TRANCE_GAUGE','ENEMY_AI_ORDER_TRIGGER','SPHR_TYPE','SPHR_PLAY_CONDITION'];
const sumsBytes=fs.readFileSync(path.join(pkg,'SHA256SUMS-tables.txt'));
if(sha(sumsBytes)!=='d6619cd8acc8f8a3d60931e7b6404d0164198ef43684d2cdb5986160217d3661')throw Error('delivered table checksum list differs');
const sums=sumsBytes.toString('utf8').replaceAll('\r\n','\n').trim().split('\n');
const utf8=new TextDecoder('utf-8',{fatal:true}), tables=[],tableManifest=[];
for(const line of sums){const m=line.match(/^([0-9a-f]{64})  tables\/([a-z][a-z0-9_]*\.csv)$/);if(!m)throw Error('invalid table checksum entry');const b=fs.readFileSync(path.join(pkg,'tables',m[2]));if(sha(b)!==m[1]||b.subarray(0,3).equals(Buffer.from([0xef,0xbb,0xbf])))throw Error('table hash or BOM mismatch: '+m[2]);const text=utf8.decode(b);if(!Buffer.from(text,'utf8').equals(b))throw Error('UTF-8 roundtrip mismatch');tables.push({name:m[2],text});tableManifest.push({name:m[2],bytes:b.length,sha256:m[1]});}
if(tables.length!==29||new Set(tables.map(t=>t.name)).size!==29)throw Error('table set incomplete');
const enums=[],enumManifest=[];
for(const name of families){const file=path.join(source,name+'.cs'),bytes=fs.readFileSync(file),text=bytes.toString('utf8');const values=[],seen=new Set();let memberLines=0;
 for(const line of text.split(/\r?\n/)){const t=line.trim();if(!t||t==='{'||t==='}'||t.startsWith('public enum '))continue;const m=t.match(/^([A-Za-z_][A-Za-z0-9_]*)\s*=\s*(-?\d+)\s*,?$/);if(!m)throw Error('unparsed enum member '+name+': '+t);if(m[1].startsWith('sizeof_'))continue;memberLines++;const value=Number(m[2]);if(!Number.isInteger(value)||value< -2147483648||value>2147483647||seen.has(value))throw Error('ambiguous enum value: '+name);seen.add(value);values.push({name:m[1],value});}
 if(!memberLines||memberLines>1024)throw Error('invalid enum '+name);enums.push({name,values});enumManifest.push({name,count:values.length,sourceSha256:sha(bytes)});
}
const bytes=Buffer.from(JSON.stringify({tables,enums}),'utf8');if(bytes.length>64*1024*1024)throw Error('snapshot exceeds transport limit');
const destination=path.join(out,'DO-NOT-DEPLOY-CombatTables.json');fs.writeFileSync(destination,bytes);
const report={checkedAt:new Date().toISOString(),snapshot:'test-output/order03-candidate/DO-NOT-DEPLOY-CombatTables.json',snapshotSha256:sha(bytes),snapshotBytes:bytes.length,tableChecksumListSha256:sha(sumsBytes),tableCount:tables.length,enumFamilyCount:enums.length,tables:tableManifest,enumFamilies:enumManifest,scope:'Isolated candidate containing unchanged server-delivered CSV text and selected modern exported enum declarations needed by the current partial bridge. Source client version, full ABI coverage, table-version compatibility, real registration, Unity/iOS and device are unverified. This is not a production asset; no automatic copying or upload.'};
fs.writeFileSync(path.join(root,'evidence/ios-package-search/order03-candidate-snapshot.json'),JSON.stringify(report,null,2)+'\n');console.log(JSON.stringify({snapshot:report.snapshot,bytes:report.snapshotBytes,sha256:report.snapshotSha256,tables:tables.length,enumFamilies:enums.length},null,2));
