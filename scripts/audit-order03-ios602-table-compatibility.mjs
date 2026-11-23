import fs from 'node:fs';
import path from 'node:path';
import crypto from 'node:crypto';
import {fileURLToPath} from 'node:url';

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
const read = p => fs.readFileSync(path.isAbsolute(p) ? p : path.join(root, p));
const json = p => JSON.parse(read(p).toString('utf8').replace(/^\uFEFF/, ''));
const sha = p => crypto.createHash('sha256').update(read(p)).digest('hex');
const metadataEvidence = 'evidence/ios-package-search/order03-ios602-enums.json';
const metadata = json(metadataEvidence);
if (!metadata.success || !metadata.independentCsharpAgreement) throw Error('Verified metadata extraction required');
const candidatePath = 'test-output/order03-candidate/DO-NOT-DEPLOY-CombatTables.json';
const candidateEvidence = json('evidence/ios-package-search/order03-candidate-snapshot.json');
if (sha(candidatePath) !== candidateEvidence.snapshotSha256) throw Error('Candidate hash changed');
const candidate = json(candidatePath);
const projectionPath = 'test-output/order03-independent-audit/projected-definitions.json';
const comparison = json('evidence/ios-package-search/order03-enum-version-differences.json');
if (sha(projectionPath) !== comparison.projectionSha256) throw Error('Reviewed projection hash changed');
const d = json(projectionPath), uses = new Map();
function observe(field, family, value) {
  if (typeof value !== 'string') throw Error('Expected projected string: ' + field);
  const token = value.trim(); if (!token) return;
  if (!uses.has(family)) uses.set(family, new Map());
  const labels = uses.get(family);
  if (!labels.has(token)) labels.set(token, {name: token, observations: 0, fields: {}});
  const item = labels.get(token); item.observations++; item.fields[field] = (item.fields[field] || 0) + 1;
}
for (const card of Object.values(d.Cards)) observe('Cards.Rarity', 'RARITY', card.Rarity);
for (const variants of Object.values(d.Skills)) for (const s of variants)
  for (const [field, family] of Object.entries({Type: 'SKILL_TYPE', Kind: 'SKILL_KIND', Attribute: 'ATTR', Job: 'JOB_TYPE', ArthurJob: 'ARTHUR_TYPE', DamageKind: 'SKILL_PHYSICS_TYPE', Target: 'SKILL_TARGET', HandAttribute: 'ATTR', HandKind: 'SKILL_KIND', AppendTrigger: 'SKILL_EXEC_TIMING', AppendCondition: 'SKILL_EXTEND_EXEC_ORDER', BranchCondition: 'SKILL_EXTEND_EXEC_ORDER', BranchCondition2: 'SKILL_EXTEND_EXEC_ORDER'})) observe('Skills.' + field, family, s[field]);
for (const roles of Object.values(d.SkillRoles)) for (const r of roles) { observe('SkillRoles.Function', 'SKILL_ROLE', r.Function); observe('SkillRoles.Target', 'SKILL_ROLE_TARGET', r.Target); }
for (const e of Object.values(d.Enemies)) observe('Enemies.Attribute', 'ATTR', e.Attribute);
for (const s of Object.values(d.Spheres)) { observe('Spheres.Type', 'SPHR_TYPE', s.Type); observe('Spheres.PlayCondition', 'SPHR_PLAY_CONDITION', s.PlayCondition); }
for (const ai of Object.values(d.EnemyAIOrders)) for (const [index, family] of [[1, 'ENEMY_AI_ORDER_PARTY'], [2, 'ENEMY_AI_ORDER_TRANCE_GAUGE'], [29, 'ENEMY_AI_ORDER_TRIGGER']]) observe('EnemyAIOrders.' + index, family, ai.Fields[index] || '');
const roleEvidence = json('evidence/ios-package-search/order03-role-enum-token-audit.json');
if (sha(roleEvidence.inputAudit) !== roleEvidence.inputAuditSha256) throw Error('Role token audit hash changed');
const rawRoles = json(roleEvidence.inputAudit), aliases = {ATTR_BIT: 'ATTR', SKILL_KIND_BIT: 'SKILL_KIND', DOT: 'SKILL_ROLE_DOT', TRANCE_GAUGE_STATE: 'ENEMY_TRANCE_GAUGE_STATE'};
const roleParameters = [];
for (const [rule, tokens] of Object.entries(rawRoles.symbolic_token_frequencies)) {
  const family = aliases[rule] || rule;
  const values = new Set(metadata.families.find(x => x.name === family)?.values.map(x => x.name) || []);
  const missing = [], matched = [], special = [];
  for (const [name, observations] of Object.entries(tokens)) {
    const item = {name, observations};
    if (!name || name === 'NULL' || (name === 'ALL' && (rule === 'ATTR_BIT' || rule === 'SKILL_KIND_BIT'))) special.push(item);
    else if (values.has(name)) matched.push(item); else missing.push(item);
  }
  roleParameters.push({rule, family, matched, missing, special});
}
const families = metadata.families.map(e => {
  const old = new Map(e.values.map(x => [x.name, x.value]));
  const modern = candidate.enums.find(x => x.name === e.name);
  if (!modern) throw Error('Missing candidate family');
  const changed = modern.values.filter(x => old.has(x.name) && old.get(x.name) !== x.value)
    .map(x => ({name: x.name, ios602: old.get(x.name), modernCandidate: x.value, observations: uses.get(e.name)?.get(x.name)?.observations || 0}));
  const missingObserved = [...(uses.get(e.name)?.values() || [])].filter(x => !old.has(x.name));
  const legacyPath = 'legacy-unity/ExportedProject/Assets/Scripts/Assembly-CSharp/' + e.name + '.cs';
  const legacyValues = new Map([...read(legacyPath).toString('utf8').matchAll(/^\s*([A-Za-z_][A-Za-z0-9_]*)\s*=\s*(-?\d+)\s*,?\s*$/gm)]
    .filter(x => !x[1].startsWith('sizeof_')).map(x => [x[1], Number(x[2])]));
  const legacyEqual = legacyValues.size === old.size && [...old].every(([name, value]) => legacyValues.get(name) === value);
  return {family: e.name, ios602Members: old.size, modernCandidateMembers: modern.values.length, changed,
    missingObserved, legacy140Sha256: sha(legacyPath), identicalToLegacy140: legacyEqual};
});
const incompatibleFamilies = families.filter(x => x.missingObserved.length);
const missingProjectedObservations = incompatibleFamilies.reduce((n, f) => n + f.missingObserved.reduce((m, x) => m + x.observations, 0), 0);
const missingRoleObservations = roleParameters.reduce((n, f) => n + f.missing.reduce((m, x) => m + x.observations, 0), 0);
const report = {
  checkedAt: new Date().toISOString(), analysisCompleted: true,
  metadataEvidenceSha256: sha(metadataEvidence), candidateSha256: sha(candidatePath), projectionSha256: sha(projectionPath), rawRoleAuditSha256: sha(roleEvidence.inputAudit), scriptSha256: sha(fileURLToPath(import.meta.url)),
  directReuseOfIos602EnumsForDeliveredTables: incompatibleFamilies.length === 0 && missingRoleObservations === 0 && families.every(x => x.changed.length === 0),
  missingProjectedObservations, missingRoleObservations, families, roleParameters,
  scope: 'Static name/value comparison of independently recovered original 6.0.2 enum constants against 23 modern candidate families and selected delivered-table Go projection fields, plus supplied raw-role token frequencies. Counts are field/variant occurrences, not unique skills or reachable executions. Empty/NULL/bitmask sentinels remain context-dependent. Missing names establish that the old enum set cannot directly represent all checked data; this does not authorize truncation, invented replacements, changed CSV values or legacy scope reduction. Runtime semantics, correct modern original version and service contracts remain unverified.'
};
fs.writeFileSync(path.join(root, 'evidence/ios-package-search/order03-ios602-table-compatibility.json'), JSON.stringify(report, null, 2) + '\n');
console.log(JSON.stringify({analysisCompleted: true, directReuseOfIos602EnumsForDeliveredTables: report.directReuseOfIos602EnumsForDeliveredTables, missingProjectedObservations, missingRoleObservations,
  identicalToLegacy140: families.filter(x => x.identicalToLegacy140).length,
  incompatibleFamilies: incompatibleFamilies.map(x => ({family: x.family, names: x.missingObserved.length, observations: x.missingObserved.reduce((n, v) => n + v.observations, 0)})),
  changed: families.filter(x => x.changed.length).map(x => ({family: x.family, values: x.changed}))}, null, 2));
