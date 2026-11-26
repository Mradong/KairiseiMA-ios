import fs from 'node:fs';
import path from 'node:path';
import crypto from 'node:crypto';
import assert from 'node:assert/strict';
import {fileURLToPath} from 'node:url';

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
const output = 'ios-client/Assets/Recovery/OriginalSceneContent';
const roots = ['recovered-unity/ExportedProject/Assets', 'test-output/order05-scene-export/ExportedProject/Assets', 'test-output/order05-home-pc-export/ExportedProject/Assets'];
const guidIndex = new Map(), guidCandidates = new Map(), files = [], origins = new Map();
const sha = bytes => crypto.createHash('sha256').update(bytes).digest('hex');
const text = relative => fs.readFileSync(path.join(root, relative), 'utf8');
function visit(relative) {
  for (const child of fs.readdirSync(path.join(root, relative), {withFileTypes: true})) {
    const file = relative + '/' + child.name;
    if (child.isDirectory()) visit(file);
    else if (child.name.endsWith('.meta')) {
      const guid = text(file).match(/^guid:\s*([a-f0-9]{32})\s*$/m)?.[1];
      if (guid) { guidIndex.set(guid, file.slice(0, -5)); if(!guidCandidates.has(guid))guidCandidates.set(guid,[]);guidCandidates.get(guid).push(file.slice(0,-5)); }
    } else files.push(file);
  }
}
roots.forEach(visit);
fs.mkdirSync(path.join(root, output), {recursive: true});
function record(relative) {
  const data = fs.readFileSync(path.join(root, relative));
  origins.set(relative, {file: relative, bytes: data.length, sha256: sha(data)});
}
function scalar(block, key, fallback = '') {
  const raw = block.match(new RegExp('^  ' + key + ':([^\\r\\n]*)', 'm'))?.[1].trim();
  if (!raw) return fallback;
  if (raw.startsWith('"')) return JSON.parse(raw);
  if (raw.startsWith("'")) return raw.slice(1, -1).replaceAll("''", "'");
  return raw;
}
function ref(block, key) {
  const raw = scalar(block, key);
  return {id: raw.match(/fileID:\s*(-?\d+)/)?.[1] ?? '0', guid: raw.match(/guid:\s*([a-f0-9]{32})/)?.[1] ?? null};
}
function vector(block, key, keys, fallback) {
  const raw = scalar(block, key);
  return keys.map((k, i) => {
    const match = raw.match(new RegExp('(?:\\{|,\\s*)' + k + ':\\s*([^,}]+)'));
    const value = match ? Number(match[1]) : fallback[i];
    assert(Number.isFinite(value), `${key}.${k} is not finite`);
    return value;
  });
}
function blocks(relative) {
  record(relative);
  return text(relative).split(/^--- !u!/m).slice(1).map(block => {
    const match = block.match(/^(\d+) &(-?\d+)\r?\n([^:\r\n]+):/);
    assert(match, `Invalid Unity block: ${relative}`);
    return {type: Number(match[1]), id: match[2], name: match[3], text: block};
  });
}
const atlasCache = new Map(), textures = new Map(), missing = [];
function animationPose(file) {
  record(file);const value=text(file);const result={position:[],scale:[],alpha:[]};
  for(const [kind,key] of [['m_PositionCurves','position'],['m_ScaleCurves','scale']]){
    const section=value.match(new RegExp('^  '+kind+':\\r?\\n([\\s\\S]*?)(?=^  m_|(?![\\s\\S]))','m'))?.[1];
    if(!section)continue;
    for(const match of section.matchAll(/^  - curve:\r?\n([\s\S]*?)(?=^  - curve:|(?![\s\S]))/gm)){
      const curve=match[1],pathValue=curve.match(/^    path:([^\r\n]*)/m)?.[1].trim()??'';
      const keys=[...curve.matchAll(/^        time:\s*([^\r\n]+)\r?\n        value:\s*\{x:\s*([^,]+), y:\s*([^,]+), z:\s*([^}]+)\}/gm)].map(m=>({time:Number(m[1]),value:[Number(m[2]),Number(m[3]),Number(m[4])]}));
      if(keys.length)result[key].push({path:pathValue,keys});
    }
  }
  const floats=value.match(/^  m_FloatCurves:\r?\n([\s\S]*?)(?=^  m_|(?![\s\S]))/m)?.[1];
  if(floats)for(const m of floats.matchAll(/^  - serializedVersion:[^\r\n]*\r?\n([\s\S]*?)(?=^  - serializedVersion:|(?![\s\S]))/gm)){
    if(!/^    attribute: color.a\s*$/m.test(m[1]))continue;
    const keys=[...m[1].matchAll(/^        time:\s*([^\r\n]+)\r?\n        value:\s*([^\r\n]+)/gm)].map(v=>({time:Number(v[1]),value:Number(v[2])}));
    if(keys.length)result.alpha.push({path:m[1].match(/^    path:([^\r\n]*)/m)?.[1].trim()??'',keys});
  }
  return result;
}
function copyTexture(relative) {
  record(relative);
  const bytes = fs.readFileSync(path.join(root, relative));
  assert(bytes.subarray(0, 8).equals(Buffer.from([137,80,78,71,13,10,26,10])));
  const key = 'original-' + sha(Buffer.from(relative)).slice(0, 16);
  if (!textures.has(key)) {
    fs.writeFileSync(path.join(root, output, key + '.png'), bytes);
    textures.set(key, {key, file: key + '.png', source: relative, sha256: sha(bytes), width: bytes.readUInt32BE(16), height: bytes.readUInt32BE(20)});
  }
  return textures.get(key);
}
function atlasFor(widget) {
  const direct = ref(widget, 'mAtlas');
  let source = direct.guid ? guidIndex.get(direct.guid) : null;
  if (!source) {
    const lookup = scalar(widget, 'mAtlasPath').toLowerCase();
    const name = scalar(widget, 'mAtlasName').toLowerCase();
    source = [...files].reverse().find(p => p.toLowerCase().endsWith('/resources/' + lookup + '.prefab'))
      ?? [...files].reverse().find(p => name && p.toLowerCase().endsWith('/atlas/' + name + '.prefab'));
  }
  if (!source || !source.endsWith('.prefab')) return null;
  if (atlasCache.has(source)) return atlasCache.get(source);
  const data = blocks(source).find(b => b.type === 114 && /^  sprites:/m.test(b.text))?.text;
  if (!data) return null;
  const name = path.posix.basename(source, '.prefab');
  const image = path.posix.dirname(source) + '/material/' + name + '.png';
  if (!fs.existsSync(path.join(root, image))) return null;
  const texture = copyTexture(image), sprites = {};
  for (const match of data.matchAll(/^  - name:([^\r\n]+)\r?\n([\s\S]*?)(?=^  - name:|^  mCoordinates:|(?![\s\S]))/gm)) {
    const name = match[1].trim(), body = match[2];
    const parseRect = kind => {
      const section = body.match(new RegExp('^    ' + kind + ':\\r?\\n([\\s\\S]*?)(?=^    [a-zA-Z]|(?![\\s\\S]))', 'm'))?.[1];
      if (!section) return null;
      return ['x','y','width','height'].map(key => Number(section.match(new RegExp('^      ' + key + ':\\s*([^\\r\\n]+)', 'm'))?.[1]));
    };
    const outer = parseRect('outer'), inner = parseRect('inner');
    assert(outer && outer.every(Number.isFinite), `Atlas rectangle missing ${source} ${name}`);
    assert(outer[0] >= 0 && outer[1] >= 0 && outer[2] > 0 && outer[3] > 0 && outer[0] + outer[2] <= texture.width && outer[1] + outer[3] <= texture.height, `Atlas frame outside texture ${source} ${name}`);
    sprites[name] = {texture: texture.key, uv: [outer[0]/texture.width, (texture.height-outer[1]-outer[3])/texture.height, outer[2]/texture.width, outer[3]/texture.height], outer, inner,
      rotated: /^    rotated:\s*1/m.test(body)};
  }
  const result = {source, texture, sprites};
  atlasCache.set(source, result);
  return result;
}
function scene(name, source) {
  const data = blocks(source), gameObjects = new Map(data.filter(b => b.type === 1).map(b => [b.id, b]));
  const transforms = new Map(data.filter(b => b.type === 4).map(b => [b.id, b]));
  const scripts = data.filter(b => b.type === 114);
  const nodes = [];
  for (const [id, object] of gameObjects) {
    const transform = [...transforms.values()].find(b => ref(b.text, 'm_GameObject').id === id);
    if (!transform) continue;
    const components = scripts.filter(b => ref(b.text, 'm_GameObject').id === id);
    const parentTransform = transforms.get(ref(transform.text, 'm_Father').id);
    const widgets = [];
    for (const component of components) {
      const type = path.posix.basename(guidIndex.get(ref(component.text, 'm_Script').guid) ?? '', '.cs');
      if (type !== 'UISprite' && type !== 'UILabel' && type !== 'UITexture') continue;
      const widget = {id: component.id, kind: type, pivot: Number(scalar(component.text, 'mPivot', '4')), depth: Number(scalar(component.text, 'mDepth', '0')),
        color: vector(component.text, 'mColor', ['r','g','b','a'], [1,1,1,1]), enabled: scalar(component.text, 'm_Enabled', '1') === '1'};
      if (type === 'UISprite') {
        const atlas = atlasFor(component.text), frame = scalar(component.text, 'mSpriteName');
        widget.frame = frame; widget.atlasSource = atlas?.source ?? null;
        widget.spriteType = Number(scalar(component.text, 'mType', '0'));
        widget.fillAmount = Number(scalar(component.text, 'mFillAmount', '1'));
        widget.resolved = atlas?.sprites[frame] ?? null;
        if (!widget.resolved && atlas) {
          const aliases = Object.keys(atlas.sprites).filter(n => n.toLowerCase() === frame.toLowerCase());
          if (aliases.length === 1) { widget.resolved = atlas.sprites[aliases[0]]; widget.resolvedFrame = aliases[0]; }
        }
        const cachedTexture = ref(component.text, 'mTex');
        const cachedSource = guidIndex.get(cachedTexture.guid);
        if (/^Cmn_White$/i.test(frame) && cachedSource?.endsWith('.png'))
          widget.resolved = {texture:copyTexture(cachedSource).key,uv:[0,0,1,1],authoredTextureOverride:true};
        if (!widget.resolved) missing.push({scene: name, node: scalar(object.text, 'm_Name'), widget: component.id, type, frame, atlasName: scalar(component.text, 'mAtlasName'), atlasPath: scalar(component.text, 'mAtlasPath')});
      } else if (type === 'UILabel') {
        widget.text = scalar(component.text, 'mText');
        widget.maxLineWidth = Number(scalar(component.text, 'mMaxLineWidth', '0'));
        widget.effect = Number(scalar(component.text, 'mEffectStyle', '0'));
        widget.effectColor = vector(component.text, 'mEffectColor', ['r','g','b','a'], [0,0,0,1]);
      } else {
        let reference = ref(component.text, 'mTexture');
        if (!reference.guid) reference = ref(component.text, 'mTex');
        let textureSource = guidIndex.get(reference.guid);
        if (!textureSource && name === 'IntroSlPl') {
          const thisNode = scalar(object.text,'m_Name');
          if (thisNode === 'Bg_IntroSlPl00') textureSource=roots[0]+'/Resources/05_image_assets/bg/Bg_Intro00.png';
          const arthurComponent = scripts.find(c=>path.posix.basename(guidIndex.get(ref(c.text,'m_Script').guid)??'','.cs')==='ArthurImage'&&ref(c.text,'m_img').id===component.id);
          if (arthurComponent) {
            const roleOwner = scripts.find(c=>ref(c.text,'m_arthur_image').id===arthurComponent.id && scalar(c.text,'m_arthur_type'));
            const role=roleOwner ? Number(scalar(roleOwner.text,'m_arthur_type')) : 0;
            if(role>=1&&role<=4){
              const base=role===4?'test-output/order05-arthur-portrait-export-verified':'test-output/order05-role-pc-cache-export';
              textureSource=base+'/ExportedProject/Assets/resources/05_image_assets/chr82/30/'+String(role).padStart(3,'0')+'/chr82_30'+String(role).padStart(3,'0')+'000.png';
              widget.boundArthurRole=role;
              widget.resourceVersionStatus=role===4?'Published 2.0.4 PC bytes verified':'Original PC cache texture, source byte hashes retained; canonical counterpart/version relationship UNKNOWN';
            }
          }
        }
        if (textureSource && !fs.existsSync(path.join(root,textureSource))) textureSource=null;
        if (textureSource?.endsWith('.png')) widget.resolved = {texture: copyTexture(textureSource).key, uv: [0,0,1,1]};
        else missing.push({scene: name, node: scalar(object.text, 'm_Name'), widget: component.id, type, textureReference: reference});
      }
      widgets.push(widget);
    }
    const anchor = components.find(c => path.posix.basename(guidIndex.get(ref(c.text, 'm_Script').guid) ?? '', '.cs') === 'UIAnchor');
    const uiRoot = components.find(c => path.posix.basename(guidIndex.get(ref(c.text, 'm_Script').guid) ?? '', '.cs') === 'UIRoot');
    const panel = components.find(c => path.posix.basename(guidIndex.get(ref(c.text, 'm_Script').guid) ?? '', '.cs') === 'UIPanel');
    const animation=data.find(b=>b.type===111&&ref(b.text,'m_GameObject').id===id);
    let enterAnimation=null;
    if(animation){
      const localRoot=roots.find(r=>source.startsWith(r+'/'));
      const candidates=[...animation.text.matchAll(/guid:\s*([a-f0-9]{32})/g)].map(m=>guidCandidates.get(m[1])?.find(p=>p.startsWith(localRoot+'/'))??guidIndex.get(m[1])).filter(Boolean);
      let file=candidates.find(p=>/_In(?:\d|_|\.)/.test(path.posix.basename(p)));
      if(!file){const out=candidates.find(p=>/_Out(?:\d|_|\.)/.test(path.posix.basename(p)));if(out){const counterpart=out.replace('_Out','_In');if(fs.existsSync(path.join(root,counterpart)))file=counterpart;}}
      if(file)enterAnimation={source:file,...animationPose(file)};
    }
    nodes.push({id, parent: parentTransform ? ref(parentTransform.text, 'm_GameObject').id : null, name: scalar(object.text, 'm_Name'), active: scalar(object.text, 'm_IsActive', '1') === '1',
      position: vector(transform.text, 'm_LocalPosition', ['x','y','z'], [0,0,0]), scale: vector(transform.text, 'm_LocalScale', ['x','y','z'], [1,1,1]), rotation: vector(transform.text, 'm_LocalRotation', ['x','y','z','w'], [0,0,0,1]),
      anchor: anchor ? {side: Number(scalar(anchor.text, 'side','4')), relative: vector(anchor.text,'relativeOffset',['x','y'],[0,0]), pixels: vector(anchor.text,'pixelOffset',['x','y'],[0,0])} : null,
      rootHeight: uiRoot ? Number(scalar(uiRoot.text,'manualHeight','640')) : null,
      panel: panel ? {clipping: Number(scalar(panel.text,'mClipping','0')), range: vector(panel.text,'mClipRange',['x','y','z','w'],[0,0,0,0])} : null,
      enterAnimation,scripts: components.map(c => ({id:c.id, type:path.posix.basename(guidIndex.get(ref(c.text,'m_Script').guid) ?? '', '.cs'),
        method:scalar(c.text,'functionName'),role:Number(scalar(c.text,'m_arthur_type','0')),
        label:ref(c.text,'label').id,maxChars:Number(scalar(c.text,'maxChars','0'))})), widgets});
  }
  const ids = new Set(nodes.map(n => n.id));
  assert(nodes.every(n => n.parent === null || ids.has(n.parent)), `Scene parent missing: ${name}`);
  const bindings={};
  for(const script of scripts){
    for(const key of ['m_ap_time_label','m_bp_time_label','m_bp_now_label','m_bp_max_label','m_name_label','m_coin_label','m_menu_crystal_label','m_ap_label','m_bp_label']){
      const reference=ref(script.text,key).id;if(reference!=='0')bindings[key]=reference;
    }
  }
  const menuSubgroups=[];
  if(name==='Menu')for(const script of scripts){
    const kind=path.posix.basename(guidIndex.get(ref(script.text,'m_Script').guid)??'','.cs');
    const section=script.text.match(/^  m_sub_menu:\r?\n([\s\S]*?)(?=^  [a-zA-Z_]|(?![\s\S]))/m)?.[1];
    if(!section)continue;
    const groups=[...section.matchAll(/fileID:\s*(-?\d+)/g)].map(m=>scripts.find(s=>s.id===m[1])).filter(Boolean).map(s=>ref(s.text,'m_GameObject').id);
    menuSubgroups.push({owner:ref(script.text,'m_GameObject').id,type:kind,groups});
  }
  return {name, source, sourceSha256: sha(Buffer.from(text(source))), nodes,bindings,menuSubgroups,authoredLayoutPreserved: true, runtimeAccepted: false};
}
const definitions = [
  scene('Title', roots[0] + '/Scene/Title.unity'),
  scene('IntroSlPl', roots[0] + '/Scene/IntroSlPl.unity'),
  scene('Home', roots[0] + '/Scene/Home.unity'),
  scene('Menu', roots[1] + '/resources/scenes/Menu.prefab'),
  scene('Message', roots[0] + '/Resources/03_ui_assets/cmn/Cmn_Window01.prefab'),
  scene('Confirm', roots[0] + '/Resources/03_ui_assets/cmn/Cmn_Window02.prefab'),
  scene('ServerList', roots[0] + '/Resources/03_ui_assets/cmn/Cmn_ServerListWindow.prefab'),
  scene('Download', roots[0] + '/Resources/03_ui_assets/cmn/Cmn_DownloadBar.prefab')
];
// Additional textures are real scene sources, never generated stand-ins.
const cover = roots[0] + '/Texture2D/TitleDefault.png';
const titleCover = fs.existsSync(path.join(root, cover)) ? copyTexture(cover).key : null;
const result = {schemaVersion:1, sourceVersions:{builtin:'Original Windows reconstruction 2.0.4 inputs; not official 6.0.2 method bodies', menu:'Read-only existing Windows resource cache; exact published 2.0.4 PC-platform counterparts verified in order05-scene-resource-inputs.json'},
  definitions, textures:[...textures.values()], titleCover, missing, requiredHomeDependencies:['default home_01000 background and corresponding original scene/materials','navi type 0 uathach model/textures/compatible Live2D rendering'],
  worldResourceDescriptor:'recovery/world.dat:originalworld',originalSceneResourcesPrepared:fs.existsSync(path.join(root,'ios-client/Assets/Recovery/OriginalWorldContent/OriginalWorld.json')),
  originalSceneRestorationComplete:false, deviceVerified:false};
fs.writeFileSync(path.join(root, output, 'OriginalScenes.json'), JSON.stringify(result,null,2)+'\n');
const report = {checkedAt:new Date().toISOString(), sourceFiles:[...origins.values()], scenes:definitions.map(d=>({name:d.name,nodes:d.nodes.length,widgets:d.nodes.flatMap(n=>n.widgets).length})),
  copiedTextures:textures.size, missingWidgetDependencies:missing, requiredHomeDependencies:result.requiredHomeDependencies,
  layoutAndAppearance:'Original transforms, hierarchy, pivot, color, depth, atlas rectangles and serialized text. No invented dashboard or diagnostic form.',
  runtimeAccepted:false, deviceVerified:false, ready:false, scriptSha256:sha(fs.readFileSync(fileURLToPath(import.meta.url)))};
fs.writeFileSync(path.join(root,'evidence/ios-package-search/order05-original-scene-preparation.json'),JSON.stringify(report,null,2)+'\n');
console.log(JSON.stringify({scenes:report.scenes,textures:report.copiedTextures,missingWidgets:missing.length,ready:false}));
