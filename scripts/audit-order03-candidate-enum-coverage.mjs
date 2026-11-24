import fs from 'node:fs';import path from 'node:path';import crypto from 'node:crypto';import {fileURLToPath} from 'node:url';
const root=path.resolve(path.dirname(fileURLToPath(import.meta.url)),'..');
const candidate=path.join(root,'test-output/order03-candidate/DO-NOT-DEPLOY-CombatTables.json'),projection=path.join(root,'test-output/order03-independent-audit/projected-definitions.json');
const sha=p=>crypto.createHash('sha256').update(fs.readFileSync(p)).digest('hex');
const manifest=JSON.parse(fs.readFileSync(path.join(root,'evidence/ios-package-search/order03-candidate-snapshot.json'),'utf8'));
if(sha(candidate)!==manifest.snapshotSha256)throw Error('candidate differs from audited hash');
const enums=new Map(JSON.parse(fs.readFileSync(candidate,'utf8')).enums.map(e=>[e.name,new Set(e.values.map(v=>v.name))]));
const d=JSON.parse(fs.readFileSync(projection,'utf8')),metrics={};
// Explicitly mirror only the condition labels accepted by decodeSkillCondition.
// This is a source-level coverage estimate, not a claim of executed behavior.
const decodedConditions=new Set(['NONE','NULL','TARGET_ATTR','SELF_MAIN_DECK_ATTR','SELF_MAIN_DECK_SKILL_KIND','SELF_ARTHUR_TYPE','SELF_OTHER_PLAY_ATTR','SELF_OTHER_PLAY_SKILL_KIND','SELF_OTHER_PLAY_RARITY','RANDOM','TURN','DECK_COMBO_COUNT','SELF_ENEMY_AI_VAR','SELF_ENEMY_AI_FLAG','SELF_ENEMY_AI_FLAG_AND','SELF_HP_PER','SELF_OTHER_PLAY_NUM','SELF_PLAY_MOST_LOW_COST','SELF_PLAY_COST_TOTAL','ENEMY_TRIBAL','SELF_MAIN_DECK_TAG','SELF_SUPPORT_DECK_TAG','SELF_ALL_DECK_TAG']);
const conditionCoverage={};
function condition(field,value){let s=conditionCoverage[field];if(!s)s=conditionCoverage[field]={blank:0,declaredDecoder:0,other:0,otherNames:{}};const token=value.trim();if(!token){s.blank++;return;}if(decodedConditions.has(token))s.declaredDecoder++;else{s.other++;s.otherNames[token]=(s.otherNames[token]||0)+1;}}
function examine(label,family,value){
 const e=enums.get(family);if(!e)throw Error('missing candidate enum family '+family);
 const key=label+' -> '+family;let stat=metrics[key];if(!stat)stat=metrics[key]={field:label,enumFamily:family,observations:0,blank:0,matched:0,unmatched:0,unmatchedTokens:{}};
 stat.observations++;if(typeof value!=='string')throw Error('unexpected non-string field '+label);const token=value.trim();if(!token){stat.blank++;return;}
 if(e.has(token))stat.matched++;else{stat.unmatched++;stat.unmatchedTokens[token]=(stat.unmatchedTokens[token]||0)+1;}
}
for(const card of Object.values(d.Cards))examine('Cards.Rarity','RARITY',card.Rarity);
for(const variants of Object.values(d.Skills))for(const s of variants){
 for(const [field,family] of Object.entries({Type:'SKILL_TYPE',Kind:'SKILL_KIND',Attribute:'ATTR',Job:'JOB_TYPE',ArthurJob:'ARTHUR_TYPE',DamageKind:'SKILL_PHYSICS_TYPE',Target:'SKILL_TARGET',HandAttribute:'ATTR',HandKind:'SKILL_KIND',AppendTrigger:'SKILL_EXEC_TIMING',AppendCondition:'SKILL_EXTEND_EXEC_ORDER',BranchCondition:'SKILL_EXTEND_EXEC_ORDER',BranchCondition2:'SKILL_EXTEND_EXEC_ORDER'}))examine('Skills.'+field,family,s[field]);
 for(const field of ['AppendCondition','BranchCondition','BranchCondition2'])condition(field,s[field]);
}
for(const roles of Object.values(d.SkillRoles))for(const r of roles){examine('SkillRoles.Function','SKILL_ROLE',r.Function);examine('SkillRoles.Target','SKILL_ROLE_TARGET',r.Target);}
for(const e of Object.values(d.Enemies))examine('Enemies.Attribute','ATTR',e.Attribute);
for(const s of Object.values(d.Spheres)){examine('Spheres.Type','SPHR_TYPE',s.Type);examine('Spheres.PlayCondition','SPHR_PLAY_CONDITION',s.PlayCondition);}
for(const ai of Object.values(d.EnemyAIOrders)){examine('EnemyAIOrders.Party','ENEMY_AI_ORDER_PARTY',ai.Fields[1]||'');examine('EnemyAIOrders.TranceGauge','ENEMY_AI_ORDER_TRANCE_GAUGE',ai.Fields[2]||'');examine('EnemyAIOrders.Trigger','ENEMY_AI_ORDER_TRIGGER',ai.Fields[29]||'');}
const report={checkedAt:new Date().toISOString(),candidateSha256:sha(candidate),projectionSha256:sha(projection),bridgeConditionSourceSha256:sha(path.join(root,'native/bridge/skill_query.go')),fields:Object.values(metrics),conditionDecoderCoverage:conditionCoverage,scope:'Static name-only comparison of selected projected fields against 23 candidate enums. Projection is the server audit Go loader output, which normalizes NULL/empty and applies compatibility defaults; blanks are counted separately, never declared valid. Condition coverage is a manually transcribed decodeSkillCondition label list, not a guarantee the full skill executes. Missing names may reflect version/schema differences or source normalization. No original-client semantic, packed ABI, real combat, Unity or device validation.'};
fs.writeFileSync(path.join(root,'evidence/ios-package-search/order03-candidate-enum-coverage.json'),JSON.stringify(report,null,2)+'\n');
for(const m of report.fields)console.log(`${m.field}: observations=${m.observations} blank=${m.blank} matched=${m.matched} unmatched=${m.unmatched} tokens=${Object.keys(m.unmatchedTokens).join(',')}`);
for(const [name,s] of Object.entries(conditionCoverage))console.log(`${name} decoder: blank=${s.blank} declared=${s.declaredDecoder} other=${s.other}`);
