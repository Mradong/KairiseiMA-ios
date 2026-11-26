import assert from 'node:assert/strict';
import fs from 'node:fs';
import path from 'node:path';
import crypto from 'node:crypto';
import { fileURLToPath } from 'node:url';

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
const recovery = 'ios-client/Assets/Recovery';
const read = name => fs.readFileSync(path.join(root, name), 'utf8');
const layout = JSON.parse(read(recovery + '/OriginalSceneContent/OriginalScenes.json'));
const base = 'recovered-unity/ExportedProject/Assets';
const sources = new Set([base + '/AnimationClip/Title_TS_Loop_00.anim', base + '/AnimationClip/Title_Connect_Loop_00.anim']);
for (const option of ['01', '02', '03', '04']) sources.add(base + '/AnimationClip/Cmn_Std_Out' + option + '_00.anim');
for (const scene of layout.definitions) {
  for (const node of scene.nodes) {
    if (node.enterAnimation) sources.add(node.enterAnimation.source);
  }
}
const sha = text => crypto.createHash('sha256').update(text).digest('hex');
const vector = '\\{x: ([^,]+), y: ([^,]+), z: ([^}]+)\\}';
const vectorKey = new RegExp('^        time:\\s*(\\S+)\\r?\\n        value: ' + vector + '\\r?\\n        inSlope: ' + vector + '\\r?\\n        outSlope: ' + vector, 'gm');
const alphaKey = /^        time:\s*(\S+)\r?\n        value:\s*(\S+)\r?\n        inSlope:\s*(\S+)\r?\n        outSlope:\s*(\S+)/gm;
const section = (yaml, name) => yaml.split(new RegExp('^  ' + name + ':\\r?$', 'm'))[1]?.split(/^  m_/m)[0] ?? '';
const targetPath = data => data.match(/^    path:([^\r\n]*)/m)?.[1].trim() ?? '';
const clips = {};
for (const source of [...sources].sort()) {
  const yaml = read(source);
  const name = path.posix.basename(source, '.anim');
  const tracks = [];
  for (const [field, property] of [['m_PositionCurves', 'position'], ['m_ScaleCurves', 'scale']]) {
    for (const curve of section(yaml, field).split(/^  - curve:/m).slice(1)) {
      const keys = [...curve.matchAll(vectorKey)].map(match => match.slice(1).map(Number));
      assert(keys.length > 0 && keys.flat().every(Number.isFinite), source + ' ' + property);
      tracks.push({ property, path: targetPath(curve), keys });
    }
  }
  for (const curve of section(yaml, 'm_FloatCurves').split(/^  - serializedVersion:/m).slice(1)) {
    if (!/^    attribute: color\.a\r?$/m.test(curve)) continue;
    const keys = [...curve.matchAll(alphaKey)].map(match => match.slice(1).map(Number));
    assert(keys.length > 0 && keys.flat().every(Number.isFinite), source + ' alpha');
    tracks.push({ property: 'alpha', path: targetPath(curve), keys });
  }
  const loop = /^  m_WrapMode: 2\r?$/m.test(yaml);
  const duration = Math.max(0, ...tracks.flatMap(track => track.keys.map(key => key[0])));
  const stop = Number(yaml.match(/^    m_StopTime: ([^\r\n]+)/m)?.[1]);
  clips[name] = { source, sha256: sha(yaml), duration: loop ? Math.max(duration, stop) : duration, loop, tracks };
}

const title = layout.definitions.find(scene => scene.name === 'Title');
const start = title.nodes.find(node => node.scripts.some(script => script.method === 'OnStartLogin'));
const titleSource = read(title.source);
const collider = [...titleSource.matchAll(/^--- !u!65 &\d+\r?\n([\s\S]*?)(?=^--- |(?![\s\S]))/gm)]
  .find(match => match[1].includes('m_GameObject: {fileID: ' + start.id + '}'));
const boxVector = field => collider[1].match(new RegExp('^  ' + field + ': ' + vector, 'm')).slice(1).map(Number);
assert(clips.Title_TS_Loop_00.loop && clips.Title_Connect_Loop_00.loop);
assert(clips.Home_Live2D_In_00.duration > 0 && clips.Cmn_Std_In04_00.duration > 0);
const titleHitBox = { node: start.id, center: boxVector('m_Center'), size: boxVector('m_Size'), source: title.source, sha256: sha(titleSource) };
function atlasFrames(prefix, suffixes) {
  const widget = layout.definitions.flatMap(scene => scene.nodes.flatMap(node => node.widgets))
    .find(widget => widget.frame === prefix + suffixes[0] && widget.resolved);
  return readFrames(widget.atlasSource, suffixes.map(suffix => prefix + suffix));
}
function readFrames(source, names) {
  const texture = layout.textures.find(texture => texture.source === source.replace(/\/([^/]+)\.prefab$/, '/material/$1.png'));
  assert(texture, 'Missing packaged atlas texture for ' + source);
  const atlas = read(source);
  const frames = {};
  for (const name of names) {
    const raw = [...atlas.matchAll(/^  - name: ([^\r\n]+)\r?\n([\s\S]*?)(?=^  - name:|^  mCoordinates:|(?![\s\S]))/gm)]
      .find(match => match[1] === name)?.[2];
    const outer = raw?.match(/^    outer:\r?\n([\s\S]*?)(?=^    inner:)/m)?.[1];
    assert(outer, 'Missing atlas frame ' + name);
    const [x, y, width, height] = ['x','y','width','height'].map(key => Number(outer.match(new RegExp('^      ' + key + ': ([^\\r\\n]+)', 'm'))?.[1]));
    assert([x, y, width, height].every(Number.isFinite), name);
    frames[name] = [x / texture.width, 1 - (y + height) / texture.height, width / texture.width, height / texture.height];
  }
  return { source, sha256: sha(atlas), texture: texture.key, frames };
}
const atlases = {
  level: atlasFrames('Num_600_', Array.from({length: 10}, (_, index) => String(index))),
  roleName: atlasFrames('IntroSlPl_Name', ['01','02','03','04']),
  homeAp: atlasFrames('Menu_Point_', ['00', '01'])
};
const menu = layout.definitions.find(scene => scene.name === 'Menu');
const menuFrames = new Map();
const menuAtlas = path.posix.join(path.posix.dirname(menu.source), '../03_ui_assets/menu/atlas');
menuFrames.set(menuAtlas + '/Menu.prefab', ['Menu_BattleAlchemistBtn01_00']);
menuFrames.set(menuAtlas + '/Menu2.prefab', ['Menu_FriendBtn01_01']);
const menuOwners = new Set(menu.menuSubgroups.map(group => group.owner));
for (const node of menu.nodes) {
  if (!menuOwners.has(node.parent)) continue;
  for (const widget of node.widgets) {
    if (!widget.resolved || !/^Menu_MainBtn_.+_00$/.test(widget.frame)) continue;
    if (!menuFrames.has(widget.atlasSource)) menuFrames.set(widget.atlasSource, []);
    menuFrames.get(widget.atlasSource).push(widget.frame.replace(/00$/, '01'));
  }
}
const menuAtlases = [...menuFrames].map(([source, names]) => readFrames(source, names));
const menuSprites = Object.fromEntries(menuAtlases.flatMap(atlas => Object.entries(atlas.frames)
  .map(([name, uv]) => [name, { texture: atlas.texture, uv, source: atlas.source, sha256: atlas.sha256 }])));
const output = path.join(root, recovery + '/Resources/OriginalSceneMotion.json');
fs.writeFileSync(output, JSON.stringify({ schemaVersion: 1, titleHitBox, clips, atlases, menuSprites }, null, 2) + '\n');
console.log(JSON.stringify({ clips: Object.keys(clips).length, tracks: Object.values(clips).reduce((total, clip) => total + clip.tracks.length, 0), titleHitBox }));
