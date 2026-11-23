import fs from 'node:fs';
import path from 'node:path';
import crypto from 'node:crypto';
import {fileURLToPath} from 'node:url';
const root=path.resolve(path.dirname(fileURLToPath(import.meta.url)),'..');
const audit='inputs/server-order03/Order03-Combat-Verified-20261123/audit/raw-role-parameters.json';
const source='recovered-unity/ExportedProject/Assets/Scripts/Assembly-CSharp';
const sha=p=>crypto.createHash('sha256').update(fs.readFileSync(p)).digest('hex');
const input=JSON.parse(fs.readFileSync(path.join(root,audit),'utf8'));
// Aliases reflect the 1.4.0 getRoleParam method, not proof that the current
// table export came from that version or that modern runtime behavior matches.
const aliases={ATTR_BIT:'ATTR',SKILL_KIND_BIT:'SKILL_KIND',DOT:'SKILL_ROLE_DOT',TRANCE_GAUGE_STATE:'ENEMY_TRANCE_GAUGE_STATE'};
const families=[];
for(const [rule,frequencies] of Object.entries(input.symbolic_token_frequencies)){
 const enumName=aliases[rule]||rule, file=path.join(root,source,enumName+'.cs');
 if(!fs.existsSync(file)){families.push({rule,enumName,sourceAvailable:false,uniqueTokens:Object.keys(frequencies).length});continue;}
 const text=fs.readFileSync(file,'utf8');const values=new Map();
 for(const m of text.matchAll(/^\s*([A-Za-z_][A-Za-z0-9_]*)\s*=\s*(-?\d+)\s*,?\s*$/gm))if(!m[1].startsWith('sizeof_'))values.set(m[1],Number(m[2]));
 const missing=[],special=[],matched=[];
 for(const [token,count] of Object.entries(frequencies)){
  if(token===''||token==='NULL'){special.push({token,count,kind:'empty_or_null_requires_context'});continue;}
  if(token==='ALL'&&(rule==='ATTR_BIT'||rule==='SKILL_KIND_BIT')){special.push({token,count,kind:'legacy_bitmask_sentinel'});continue;}
  if(values.has(token))matched.push({token,count,value:values.get(token)});else missing.push({token,count});
 }
 families.push({rule,enumName,sourceAvailable:true,sourceSha256:sha(file),uniqueTokens:Object.keys(frequencies).length,matched,special,missing});
}
const result={checkedAt:new Date().toISOString(),inputAudit:audit,inputAuditSha256:sha(path.join(root,audit)),sourceDirectory:source,aliasBasis:'1.4.0 SkillDataContainer.getRoleParam; modern implementation is an exported stub',families,scope:'Static token-name comparison against modern exported enum declarations for role parameter types observed in the supplied server audit. Empty/NULL and bitmask ALL are separated because semantics depend on field type. This does not prove these client enums match the supplied table version, that values fit the registered ABI, or that skill/AI behavior is implemented; no production schema generated.'};
const output=path.join(root,'evidence/ios-package-search/order03-role-enum-token-audit.json');fs.writeFileSync(output,JSON.stringify(result,null,2)+'\n');
for(const x of families)console.log(`${x.rule} => ${x.enumName}: matched=${x.matched?.length??0} special=${x.special?.length??0} missing=${x.missing?.length??'source missing'}`);
