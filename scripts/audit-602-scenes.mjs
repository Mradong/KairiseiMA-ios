import fs from 'node:fs';
import path from 'node:path';
import crypto from 'node:crypto';
import {fileURLToPath} from 'node:url';

const root=path.resolve(path.dirname(fileURLToPath(import.meta.url)),'..');
const assets=path.join(root,'ios602-unity/ExportedProject/Assets');
const guidIndex=new Map();
function visit(dir) {
  for(const item of fs.readdirSync(dir,{withFileTypes:true})) {
    const p=path.join(dir,item.name);
    if(item.isDirectory()) visit(p);
    else if(item.isFile() && item.name.endsWith('.meta')) {
      const match=fs.readFileSync(p,'utf8').match(/^guid:\s*([a-f0-9]{32})\s*$/m);
      if(match) {
        const rel=path.relative(assets,p.slice(0,-5)).replaceAll('\\','/');
        if(!guidIndex.has(match[1])) guidIndex.set(match[1],[]);
        guidIndex.get(match[1]).push(rel);
      }
    }
  }
}
visit(assets);
const sceneNames=['Home','Deck','Battle','Result','TeamStOne','StageSt'];
const internal=new Set(['m_ObjectHideFlags','m_PrefabParentObject','m_PrefabInternal','m_GameObject','m_Enabled','m_EditorHideFlags','m_Script','m_Name','m_EditorClassIdentifier','m_CorrespondingSourceObject','m_PrefabInstance','m_PrefabAsset']);
const scenes=[];
for(const name of sceneNames) {
  const file=path.join(assets,'Scene',name+'.unity');
  const text=fs.readFileSync(file,'utf8');
  const parts=text.split(/^--- !u!/m).slice(1);
  const blocks=parts.map(s=>{const m=s.match(/^(\d+) &(-?\d+)[^\n]*\r?\n([^\n]+):/);if(!m)throw new Error('Invalid block in '+name);return {type:+m[1],id:m[2],kind:m[3],text:s};});
  const objects=new Map(blocks.filter(b=>b.type===1).map(b=>[b.id,(b.text.match(/^  m_Name:\s*(.*)\r?$/m)||[])[1]?.trim()||'']));
  const scripts=[];
  for(const b of blocks.filter(b=>b.type===114)) {
    const owner=(b.text.match(/^  m_GameObject: \{fileID: (-?\d+)\}/m)||[])[1];
    const guid=(b.text.match(/^  m_Script: \{fileID: -?\d+, guid: ([a-f0-9]{32}), type: \d+\}/m)||[])[1]||null;
    const fields=[...b.text.matchAll(/^  ([A-Za-z_][\w]*):/gm)].map(x=>x[1]).filter(x=>!internal.has(x));
    const paths=guidIndex.get(guid)||[];
    const legacyPaths=paths.filter(p=>p.startsWith('Scripts/') && fs.existsSync(path.join(root,'legacy-unity/ExportedProject/Assets',p)));
    scripts.push({id:b.id,objectId:owner||null,objectName:objects.get(owner)||null,scriptGuid:guid,scriptPaths:paths,legacy140SamePath:legacyPaths,customFields:fields});
  }
  const external=[...new Set([...text.matchAll(/guid: ([a-f0-9]{32})/g)].map(m=>m[1]))].sort().map(guid=>({guid,paths:guidIndex.get(guid)||[],builtin:/^0{16}/.test(guid)}));
  scenes.push({scene:name,source:'ios602-unity/ExportedProject/Assets/Scene/'+name+'.unity',sha256:crypto.createHash('sha256').update(text).digest('hex'),gameObjects:objects.size,monoBehaviours:scripts.length,customFieldComponents:scripts.filter(s=>s.customFields.length).length,missingScriptGuids:scripts.filter(s=>!s.scriptPaths.length).length,externalReferences:external,scripts});
}
const report={generatedAt:new Date().toISOString(),sourceIpaSha256:JSON.parse(fs.readFileSync(path.join(root,'evidence/ios-package-search/ios-content-source.json'),'utf8')).sourceIpaSha256,scope:'Static analysis of exported Unity YAML and .meta GUIDs only. Resolved GUIDs do not prove assets load, fields are correct, logic exists, runtime compatibility or completeness. Built-in GUIDs are separately labelled.',scenes};
fs.writeFileSync(path.join(root,'evidence/ios-package-search/ios602-scene-audit.json'),JSON.stringify(report,null,2)+'\n');
console.log(JSON.stringify(scenes.map(({scene,gameObjects,monoBehaviours,customFieldComponents,missingScriptGuids,externalReferences})=>({scene,gameObjects,monoBehaviours,customFieldComponents,missingScriptGuids,externalGuids:externalReferences.length,unresolvedExternal:externalReferences.filter(x=>!x.builtin&&!x.paths.length).length})),null,2));
