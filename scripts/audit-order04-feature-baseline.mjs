import fs from 'node:fs';
import path from 'node:path';
import crypto from 'node:crypto';
import assert from 'node:assert/strict';
import { execFileSync } from 'node:child_process';
import { fileURLToPath } from 'node:url';

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
const check = process.argv.includes('--check');
assert(process.argv.slice(2).every(x => x === '--check'), 'Only --check is supported');
const specPath = 'scripts/order04-feature-specs.json';
const reportPath = 'evidence/ios-package-search/order04-feature-baseline.json';
const validationPath = 'evidence/ios-package-search/order04-baseline-validation.json';
const coveragePath = 'evidence/feature-coverage.md';
const historyMarker = '<!-- order04-preserved-history -->';
const hash = bytes => crypto.createHash('sha256').update(bytes).digest('hex');
function absolute(relative) {
  assert(!path.isAbsolute(relative), `Absolute source path refused: ${relative}`);
  const resolved = path.resolve(root, relative);
  assert(resolved.startsWith(root + path.sep), `Source escapes recovery directory: ${relative}`);
  assert(!fs.lstatSync(resolved).isSymbolicLink(), `Symlink refused: ${relative}`);
  return resolved;
}
const read = relative => fs.readFileSync(absolute(relative));
const json = relative => JSON.parse(read(relative).toString('utf8').replace(/^\uFEFF/, ''));
const refs = new Map();
function reference(relative, version, kind) {
  if (!refs.has(relative)) {
    const bytes = read(relative);
    refs.set(relative, { path: relative, bytes: bytes.length, sha256: hash(bytes), version, kind });
  }
  return refs.get(relative);
}

// Mask comments and C#/Go strings without changing character or line coordinates.
// The result is used only to locate declared method bodies, never to infer semantics.
function mask(text) {
  return text.replace(/\/\*[\s\S]*?\*\/|\/\/[^\r\n]*|@"(?:""|[^"])*"|"(?:\\[\s\S]|[^"\\])*"|'(?:\\[\s\S]|[^'\\])*'|`[^`]*`/g,
    token => token.replace(/[^\r\n]/g, ' '));
}
function methodBodies(relative, language) {
  const text = read(relative).toString('utf8');
  const masked = mask(text);
  const pattern = language === 'go'
    ? /^func\s+\(a\s+\*API\)\s+(\w+)\s*\([^{}]*?\)[^{}]*?\{/gm
    : /^\s*(?:public|private|protected|internal)\s+(?:(?:static|override|virtual|sealed|new|async)\s+)*[\w<>,.\[\]?]+\s+(\w+)\s*\([^;{}]*?\)\s*\{/gm;
  return [...masked.matchAll(pattern)].map(match => {
    const start = match.index + match[0].lastIndexOf('{');
    let end = start + 1, depth = 1;
    for (; end < masked.length && depth; end++) {
      if (masked[end] === '{') depth++;
      else if (masked[end] === '}') depth--;
    }
    assert.equal(depth, 0, `Unbalanced method body: ${relative} ${match[1]}`);
    const result = { symbol: match[1], line: text.slice(0, match.index + match[0].search(/\S/)).split('\n').length,
      bodySha256: hash(text.slice(start, end)), nonEmptyBody: /\S/.test(masked.slice(start + 1, end - 1)) };
    Object.defineProperty(result, 'bodyText', { value: text.slice(start, end), enumerable: false });
    return result;
  });
}

const spec = json(specPath);
reference(specPath, '04 baseline specification', 'curated_scope');
const sourceEvidence = json('evidence/source-repository.json');
assert.equal(sourceEvidence.commit, spec.referenceCommit);
const repo = path.join(root, 'source-review/kairisei-ma-ch');
const commit = execFileSync('git', ['-C', repo, 'rev-parse', 'HEAD'], { encoding: 'utf8' }).trim();
assert.equal(commit, spec.referenceCommit, 'Modern reference commit changed');
assert.equal(execFileSync('git', ['-C', repo, 'status', '--porcelain'], { encoding: 'utf8' }).trim(), '',
  'Modern reference is modified; do not audit a drifting reference');
reference('evidence/source-repository.json', commit, 'reference_identity');
const imported = json('evidence/ios-package-search/cn602-import.json');
const resourceAnalysis = json('evidence/ios-package-search/cn602-resource-analysis.json');
const sceneAudit = json('evidence/ios-package-search/ios602-scene-audit.json');
assert.equal(resourceAnalysis.sourceSha256, imported.sourceSha256);
assert.equal(sceneAudit.sourceIpaSha256, imported.sourceSha256);
for (const p of ['cn602-import.json', 'cn602-resource-analysis.json', 'ios602-scene-audit.json'])
  reference('evidence/ios-package-search/' + p, 'CN iOS 6.0.2 / 60200', 'existing_export_evidence');
const completion03 = json('evidence/ios-package-search/order03-completion-audit-lyd.json');
assert.equal(completion03.completionProven, true, '03 dependency not accepted');
const legacyIdentity = json('evidence/rea/legacy-native-boundaries.json');
const legacyDll = reference('legacy-managed/Assembly-CSharp.dll', '1.4.0 managed input', 'managed_source_identity');
assert.equal(legacyDll.sha256, legacyIdentity.subject.digest.sha256);
reference('evidence/rea/legacy-native-boundaries.json', '1.4.0 managed input', 'existing_managed_evidence');
const compatibility = json('evidence/ios-package-search/order03-ios602-table-compatibility.json');
assert.equal(compatibility.directReuseOfIos602EnumsForDeliveredTables, false);
reference('evidence/ios-package-search/order03-ios602-table-compatibility.json', '6.0.2 versus reconstruction 2.0.4', 'observed_incompatibility');

const sceneNames = resourceAnalysis.scenes.map(p => path.posix.basename(p, '.unity'));
assert.equal(new Set(sceneNames).size, sceneNames.length);
const actualSceneNames = fs.readdirSync(path.join(root, 'ios602-unity/ExportedProject/Assets/Scene'))
  .filter(p => p.endsWith('.unity')).map(p => p.slice(0, -6)).sort();
assert.deepEqual(actualSceneNames, [...sceneNames].sort(), 'Existing scene inventory changed');
const sceneOwners = new Map();
for (const feature of spec.features) {
  for (const scene of feature.scenes) {
    assert(sceneNames.includes(scene), `Unknown scene ${scene}`);
    assert(!sceneOwners.has(scene), `Scene assigned twice: ${scene}`);
    sceneOwners.set(scene, feature.id);
  }
}
assert.deepEqual([...sceneOwners.keys()].sort(), [...sceneNames].sort(), 'Scene omitted from baseline');
const scenes = sceneNames.map(name => {
  const relative = `ios602-unity/ExportedProject/Assets/Scene/${name}.unity`;
  const ref = reference(relative, 'CN iOS 6.0.2 / 60200', 'exported_scene');
  const existing = sceneAudit.scenes.find(s => s.scene === name);
  if (existing) assert.equal(ref.sha256, existing.sha256, `Previously audited scene changed: ${name}`);
  return { name, feature: sceneOwners.get(name), source: relative,
    previousDetailedAudit: existing ? { evidence: 'evidence/ios-package-search/ios602-scene-audit.json',
      components: existing.monoBehaviours, customFieldComponents: existing.customFieldComponents,
      missingScriptGuids: existing.missingScriptGuids } : null,
    bindingStatus: 'UNKNOWN: scene presence does not prove fields, behavior or runtime compatibility' };
});

const legacyCache = new Map();
function legacySource(name) {
  const relative = `legacy-unity/ExportedProject/Assets/Scripts/Assembly-CSharp/${name}.cs`;
  if (legacyCache.has(relative)) return legacyCache.get(relative);
  if (!fs.existsSync(path.join(root, relative))) {
    const missing = { source: relative, state: 'MISSING: use recorded scene/modern evidence; locate component in a later increment' };
    legacyCache.set(relative, missing);
    return missing;
  }
  const ref = reference(relative, '1.4.0 decompiled managed source', 'legacy_code');
  const methods = methodBodies(relative, 'csharp');
  const text = read(relative).toString('utf8');
  assert(!text.includes('Dummy class.'), `Legacy code is a stub: ${relative}`);
  const modernExport = relative.replace('legacy-unity/', 'ios602-unity/');
  let exportState = 'UNKNOWN: no same-path export';
  if (fs.existsSync(path.join(root, modernExport))) {
    reference(modernExport, 'CN iOS 6.0.2 / 60200', 'exported_script');
    exportState = read(modernExport).toString('utf8').includes('Dummy class.')
      ? 'STUB: same path has no recovered business implementation'
      : 'UNKNOWN: exported text requires separate validation';
  }
  const result = { source: relative, sha256: ref.sha256,
    state: methods.some(m => m.nonEmptyBody) ? 'METHOD_BODIES_PRESENT: static evidence only' : 'DECLARATIONS_ONLY',
    methods, ios602SamePath: { source: fs.existsSync(path.join(root, modernExport)) ? modernExport : null, state: exportState } };
  legacyCache.set(relative, result);
  return result;
}
// Exact anchors for the five priority flows; source existence alone is insufficient.
const requiredAnchors = {
  Home: ['PostStart', 'OnHomeShowReceive', 'OnDestroy'],
  Deck: ['OnCardDeckSet'], DeckChecker: ['IsSameCardIDInCards', 'IsCharaCardIDInCards', 'IsSphereDeckCompatible', 'IsBuddyDeckThereLeader'],
  CardMgr: ['CardDeckSetSend'], TeamStOne: ['StartSend'], StageSt: ['StartSend'],
  'battle/BattleSoloFunction': ['initialize', 'setReport', 'SetupBattlePluginNative'],
  Result: ['PreStart', 'PostStart'], 'battle/BattleResultCmdSoloFunction': ['execDirectionEnd']
};
for (const [name, anchors] of Object.entries(requiredAnchors)) {
  const source = legacySource(name);
  for (const anchor of anchors) assert(source.methods?.some(m => m.symbol === anchor && m.nonEmptyBody), `Missing behavior anchor ${name}.${anchor}`);
}

const httpapi = 'source-review/kairisei-ma-ch/server/internal/httpapi';
const handlers = new Map();
const modernBodyTexts = new Map();
for (const name of fs.readdirSync(path.join(root, httpapi)).sort().filter(n => n.endsWith('.go') && !n.endsWith('_test.go'))) {
  const relative = `${httpapi}/${name}`;
  for (const method of methodBodies(relative, 'go')) {
    assert(!handlers.has(method.symbol), `Duplicate API method: ${method.symbol}`);
    handlers.set(method.symbol, { source: relative, ...method });
    modernBodyTexts.set(method.symbol, method.bodyText);
  }
}
const routeSource = `${httpapi}/routes.go`;
reference(routeSource, commit, 'modern_route_registration');
const routeText = read(routeSource).toString('utf8');
const endpointOwners = new Map();
for (const feature of spec.features) {
  for (const endpoint of feature.routes) {
    assert(!endpointOwners.has(endpoint), `Endpoint assigned twice: ${endpoint}`);
    endpointOwners.set(endpoint, feature.id);
  }
}
const cnRouteSource = 'source-review/kairisei-ma-ch/server/internal/cnbootstrap/game_routes.go';
reference(cnRouteSource, commit, 'CN_wire_adapter_registration');
const cnText = read(cnRouteSource).toString('utf8');
const routes = [...routeText.matchAll(/router\.(Get|Post)\("([^"]+)",\s*a\.(\w+)\)/g)].map(match => {
  const handler = handlers.get(match[3]);
  assert(handler, `Route has no located handler body: ${match[2]}`);
  reference(handler.source, commit, 'modern_handler_body');
  return { method: match[1].toUpperCase(), endpoint: match[2],
    feature: endpointOwners.get(match[2]) ?? 'modern-unclassified', source: routeSource,
    line: routeText.slice(0, match.index).split('\n').length, handler,
    cnAdapterRegistered: cnText.includes(`router.Post("${match[2]}"`),
    behaviorStatus: 'UNKNOWN: registration and body location are static evidence; policy, empty/fixed responses and device behavior need validation' };
});
assert(routes.length > 0);
for (const endpoint of endpointOwners.keys()) assert(routes.some(r => r.endpoint === endpoint), `Curated endpoint not registered: ${endpoint}`);
const cnAdapters = [...cnText.matchAll(/router\.(Get|Post)\("([^"]+)"/g)].map(match => ({
  method: match[1].toUpperCase(), endpoint: match[2], feature: endpointOwners.get(match[2]) ?? spec.cnOnlyRoutes[match[2]] ?? 'modern-unclassified',
  source: cnRouteSource, line: cnText.slice(0, match.index).split('\n').length,
  status: 'STATIC_REGISTRATION_ONLY: aliases/adapters do not establish original-client behavior or runtime compatibility'
}));
for (const [endpoint, owner] of Object.entries(spec.cnOnlyRoutes)) {
  assert(cnAdapters.some(r => r.endpoint === endpoint), `CN-only endpoint not registered: ${endpoint}`);
  assert(spec.features.some(f => f.id === owner), `Unknown CN-only endpoint owner: ${owner}`);
}
const observations = spec.modernObservations.map(observation => {
  const handler = handlers.get(observation.handler);
  assert(handler, `Observed modern handler missing: ${observation.handler}`);
  assert(spec.features.some(f => f.id === observation.feature));
  for (const token of observation.tokens) assert(modernBodyTexts.get(observation.handler).includes(token), `Observation anchor missing: ${observation.handler} ${token}`);
  reference(handler.source, commit, 'modern_observed_call_path');
  return { ...observation, source: handler.source, symbol: handler.symbol, line: handler.line, bodySha256: handler.bodySha256,
    status: 'STATIC_CALL_PATH_OBSERVED: runtime rules and side effects remain scoped to separately referenced evidence' };
});
const unclassified = [...routes, ...cnAdapters].filter(r => r.feature === 'modern-unclassified');
const features = spec.features.map(feature => {
  for (const field of ['id', 'name', 'behavior', 'state', 'dependencies', 'next', 'order']) assert(feature[field]?.trim(), `${feature.id} lacks ${field}`);
  return { ...feature, runtimeAcceptance: 'NOT_PASSED', modernBehavior: 'UNKNOWN unless the precisely scoped evidence below states otherwise',
    legacySources: feature.legacy.map(legacySource),
    sceneSources: scenes.filter(s => s.feature === feature.id).map(s => s.source),
    modernRoutes: [...new Set(routes.filter(r => r.feature === feature.id).map(r => r.endpoint))],
    cnAdapters: cnAdapters.filter(r => r.feature === feature.id).map(r => r.endpoint),
    modernObservations: observations.filter(o => o.feature === feature.id),
    evidence: feature.evidence.map(name => {
      const relative = `evidence/ios-package-search/${name}`;
      reference(relative, 'Version and scope retained in the referenced report', 'existing_behavior_evidence');
      return relative;
    }) };
});
assert.equal(new Set(features.map(f => f.id)).size, features.length);
if (unclassified.length) features.push({ id: 'modern-unclassified', name: '现代尚未归类协议', scenes: [], legacySources: [], sceneSources: [],
    modernRoutes: [...new Set(unclassified.map(r => r.endpoint))], cnAdapters: [], modernObservations: [], evidence: [], order: '11',
  behavior: '现代账号路由存在，功能归属和业务规则尚未确认。', state: '待核实：保留全部未归类端点，不据此推断功能已支持。',
  dependencies: '逐处理器、调用方、实际发布策略与设备入口证据。', next: '11按未归类清单逐项调查并增量移入已确认功能族。',
  runtimeAcceptance: 'NOT_PASSED', modernBehavior: 'UNKNOWN' });

const current = read(coveragePath).toString('utf8');
const historical = current.includes(historyMarker) ? current.split(historyMarker)[1].replace(/^\r?\n/, '') : current;
const legacyLabel = f => f.legacySources.filter(x => x.sha256).slice(0, 3).map(s => {
  const name = path.posix.basename(s.source);
  const preferred = (requiredAnchors[s.source.split('Assembly-CSharp/')[1].slice(0, -3)] || [])[0];
  const anchor = s.methods?.find(m => m.symbol === preferred) || s.methods?.find(m => m.nonEmptyBody);
  return `[${name}${anchor ? ':' + anchor.symbol : ''}](../${s.source})`;
}).join('、') || '无方法体来源，保留未知';
const sourceLabel = f => {
  const endpointList = [...new Set([...f.modernRoutes, ...f.cnAdapters.filter(p => spec.cnOnlyRoutes[p])])];
  const handler = f.modernObservations[0] || routes.find(r => r.feature === f.id)?.handler;
  const handlerLabel = handler ? `；[${handler.symbol}](../${handler.source})` : '';
  return `${f.scenes.length ? '6.0.2：' + f.scenes.join('、') + '；' : '6.0.2独立场景/绑定待核实；'}1.4.0：${legacyLabel(f)}；现代：${endpointList.length ? endpointList.map(s => '`' + s + '`').join('、') + handlerLabel : '未由账号HTTP清单证明，继续核实其他传输/平台依赖'}`;
};
const rows = group => group.map(f => `| ${f.name} (${f.id}) | ${sourceLabel(f)}<br>${f.behavior} | ${f.state} | ${f.dependencies}<br>下一步 ${f.order}：${f.next} |`).join('\n');
const priority = new Set(['startup', 'resources', 'identity', 'home', 'deck', 'quests', 'battle', 'results', 'persistence']);
const evidenceRows = features.filter(f => f.evidence.length).map(f => `| ${f.name} | ${f.evidence.map(p => `[${path.posix.basename(p)}](../${p})`).join('、')} |`).join('\n');
const observationRows = observations.map(o => `| ${features.find(f => f.id === o.feature).name} | [${o.handler}:${o.line}](../${o.source})（固定提交${commit.slice(0, 7)}） | ${o.observation} |`).join('\n');
const markdown = `# 原工程功能和依赖覆盖（持续核实）

2026-11-25 顺序04当前基线。以下当前表覆盖下方历史快照的过期状态；按本行范围建立可追溯清单，不代表完整SCOPE-01、场景可复用或游戏已恢复。

来源与版本：国服iOS **6.0.2 / 60200** 的现有导出（IPA SHA-256 \`${imported.sourceSha256}\`）；旧 **1.4.0** 有方法体源码（Assembly-CSharp.dll SHA-256 \`${legacyDll.sha256}\`）；现代公开恢复项目 **2.0.4** 固定参考提交 \`${commit}\`。现代恢复版本不是已证明的官方原始提取版本。来源文件、方法行号/方法体哈希、场景与账号路由归属见 [order04-feature-baseline.json](ios-package-search/order04-feature-baseline.json)。03的数据及服务契约已按范围完成；官方提取/转换仍UNKNOWN。

状态定义：**局部支持**仅指所引证据范围；**缺失**指当前恢复客户端未实现/验收的行为；**待核实**保留原版或现代线索，不能按旧版缺项删减；**不兼容**只在有对应证据时确定。所有功能的完整运行验收均为NOT_PASSED，名称、方法体、路由或资源存在不证明功能可用。

已知不兼容：6.0.2枚举不能直接用于已接受2.0.4主表，7族/40个名称、8769次观察缺项，SELF_ENEMY_AI_VAR编号也不同，见 [原有跨版本审计](ios-package-search/order03-ios602-table-compatibility.json)。旧Android原生插件不能作为iOS资源交付。Live2D/CRI/WebView及其他SDK的具体版本兼容仍待核实。

复用现有导出与六场景审计，没有重导出或重跑静态场景调查。Home/Deck/StageSt/TeamStOne/Battle/Result均未恢复业务字段，Result另有1个空脚本引用；后续采用现有Unity iOS工程重建业务绑定，并复用已确认的资源与旧版行为线索，不能直接搬存根场景冒充原逻辑。

## 主流程的当前行为与依赖

| 功能 | 来源、版本及原行为线索 | 当前状态与边界 | 依赖和下一步 |
| --- | --- | --- | --- |
${rows(features.filter(f => priority.has(f.id)))}

## 现代参考已定位的调用边界（静态证据）

这里只确认所列固定源码中的调用，现代运行/设备行为继续待核实。尤其不能把主页、关卡列表或恢复请求默认当作无副作用。

| 功能 | 方法来源/版本 | 已定位行为与限制 |
| --- | --- | --- |
${observationRows}

## 其他功能和平台依赖

以下每项继续属于恢复/核实范围。53个导出场景均已归属；球、仓库、爬塔、称号和PetitRaid等没有独立同名场景的线索也保留。现代HTTP账号协议清单只覆盖固定参考的注册层，不穷尽多人传输、配置驱动、平台SDK或加密客户端中的功能。

| 功能 | 来源、版本及原行为线索 | 当前状态与边界 | 依赖和下一步 |
| --- | --- | --- | --- |
${rows(features.filter(f => !priority.has(f.id)))}

## 已有运行证据的精确引用

此轮只做本地范围核验，没有新账号、入场、消耗、领奖、服务写入、Unity/iOS构建或设备运行。真实服务证据来自先前专用身份，Windows/Go引擎、旧DLL差分和各版设备反馈各自保留范围。

| 功能 | 证据（版本与限制以各报告为准） |
| --- | --- |
${evidenceRows}

## 核验与后续顺序

执行 \`node scripts/audit-order04-feature-baseline.mjs --check\` 核对来源哈希、方法体位置、场景/路由归属、旧证据与固定参考工作树，并比对当前覆盖表。结果见 [order04-baseline-validation.json](ios-package-search/order04-baseline-validation.json)。清单覆盖是静态材料边界，不宣称原功能范围穷尽；后续发现新功能时追加来源、依赖、状态和下一步。

下一项为详细计划05「登录到主页」：先核对身份/角色分流、HomeShow与登录奖励/未完成战斗副作用，再接通用户可见主页、错误与重试，最后同版本设备验证。06—11按各行依赖继续。

## 历史记录（保留原版本，不作为当前状态）

${historyMarker}
${historical}`;

const report = {
  schemaVersion: 1, auditedOn: spec.auditedOn, planItem: '04',
  scope: 'Incremental static inventory from existing 6.0.2 exports, 1.4.0 method bodies and pinned modern reconstruction reference. Runtime acceptance is separate. No exports, accounts, game APIs, service mutations or builds executed.',
  versions: { ios602IpaSha256: imported.sourceSha256, legacy140AssemblySha256: legacyDll.sha256,
    modernReconstructionVersion: '2.0.4', modernReferenceCommit: commit,
    officialExtractionProvenance: 'UNKNOWN', legacyVersionLabel: '1.4.0 as recorded in prior recovery provenance; not asserted to be 6.0.2-compatible' },
  existingSceneAuditReused: true, sceneBindingRestored: false,
  knownIncompatibility: { evidence: 'evidence/ios-package-search/order03-ios602-table-compatibility.json',
    directIos602EnumReuse: false, missingProjectedObservations: compatibility.missingProjectedObservations },
  scenes, routes, cnAdapters, observations, features,
  sources: [...refs.values()].sort((a, b) => a.path.localeCompare(b.path)),
  historySha256: hash(historical),
  limitations: [
    'All features remain NOT_PASSED for complete runtime acceptance. Item 04 establishes the baseline only.',
    'Same-name scenes/classes do not prove matching behavior, recovered fields or compatibility.',
    'Modern route handler bodies may contain fixed/empty compatibility responses; registration is not implementation acceptance.',
    'Account HTTP route coverage does not exhaust multiplayer protocols, configuration-driven features, SDKs or unavailable original code.',
    'No new Unity/iOS build, ARM64 or device evidence. Current-turn/HP recovery remains unverified.'
  ]
};
if (check) {
  assert.deepEqual(json(reportPath), report, 'Baseline sources changed: review and regenerate the inventory');
  assert.equal(current, markdown, 'Current coverage document differs from reviewed specification');
} else {
  fs.writeFileSync(path.join(root, reportPath), JSON.stringify(report, null, 2) + '\n');
  fs.writeFileSync(path.join(root, coveragePath), markdown);
}
const validation = {
  checkedAt: new Date().toISOString(), planItem: '04', checkMode: check,
  completionProven: check, requirement: 'Every recorded feature has source/version, dependencies, state and next step; modern unknowns are retained; priority flows are anchored in method bodies; existing exports are reused.',
  counts: { features: features.length, exportedScenes: scenes.length, assignedScenes: sceneOwners.size,
    registeredRoutes: routes.length, distinctEndpoints: new Set(routes.map(r => r.endpoint)).size,
    cnAdapterRegistrations: cnAdapters.length, modernCallPathObservations: observations.length,
    unclassifiedEndpoints: new Set(unclassified.map(r => r.endpoint)).size,
    sourceFiles: refs.size, priorityMethodAnchors: Object.values(requiredAnchors).flat().length },
  checks: { allScenesAssigned: true, allRoutesAssignedOrExplicitUnknown: true, allFeatureFieldsPresent: true,
    allEvidencePathsAndHashesVerified: true, priorityMethodBodiesPresent: true,
    detailedSceneAuditReusedAndHashesMatch: true, historicalEntriesPreserved: true,
    pinnedReferenceUnmodified: true, modernUnverifiedBehaviorRetained: true,
    baselineAndMarkdownMatch: check },
  sourceFiles: [reference(reportPath, '04 baseline', 'generated_report'), reference(coveragePath, '04 current and historical coverage', 'coverage_document')],
  scriptSha256: hash(read('scripts/audit-order04-feature-baseline.mjs')),
  noNewGameSideEffects: true, noNewBuildOrDeviceValidation: true,
  wholeScopeAcceptance: false, wholeGameAcceptance: false, nextPlanItem: '05'
};
fs.writeFileSync(path.join(root, validationPath), JSON.stringify(validation, null, 2) + '\n');
console.log(JSON.stringify({ planItem: '04', checkMode: check, completionProven: check, ...validation.counts, wholeGameAcceptance: false }));
