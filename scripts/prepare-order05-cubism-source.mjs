import fs from 'node:fs';
import path from 'node:path';
import crypto from 'node:crypto';
import assert from 'node:assert/strict';
import {fileURLToPath} from 'node:url';
const root=path.resolve(path.dirname(fileURLToPath(import.meta.url)),'..');
const reference='test-output/order05-live2d-reference/Live2DUnity-reference.cs';
const receipt=JSON.parse(fs.readFileSync(path.join(root,'evidence/ios-package-search/order05-live2d-reference-decompilation.json'),'utf8'));
const hash=p=>crypto.createHash('sha256').update(fs.readFileSync(path.join(root,p))).digest('hex');
assert(receipt.success&&receipt.inputUnchanged);assert.equal(hash(reference),receipt.referenceSha256);
let code=fs.readFileSync(path.join(root,reference),'utf8').replace(/^\[assembly:[^\r\n]*\]\r?\n/gm,'').replace(/^\[module:[^\r\n]*\]\r?\n/gm,'');
code=code.replace(/\blive2d\b/g,'Kairisei.Recovery.Cubism2');
const unusedBlob=code.match(/\[CompilerGenerated\]\s*internal sealed class (CubismType_\d+)\s*\{/);
if(unusedBlob && code.split(unusedBlob[1]).length===2){
 const start=unusedBlob.index,open=code.indexOf('{',start);let cursor=open+1,depth=1;
 while(depth&&cursor<code.length){if(code[cursor]==='{')depth++;else if(code[cursor]==='}')depth--;cursor++;}
 assert.equal(depth,0);code=code.slice(0,start)+code.slice(cursor);
}
let shaderLookups=0;
code=code.replace(/Shader\.Find\((\w+)\)/g,(_,name)=>{shaderLookups++;return 'Kairisei.Recovery.OriginalLive2DShaders.Resolve('+name+')';});
assert.equal(shaderLookups,1,'Expected one explicit SDK shader factory');
code=code.replace(/(\w+)\.SetVector\(vector: (.+), nameID: (\w+)\);/g,'$1.SetVector($3, $2);');
const output='ios-client/Assets/Recovery/Runtime/RecoveredCubism2.cs';
fs.writeFileSync(path.join(root,output),`// Reconstructed Cubism 2.1 managed core for this recovery project.
// Original shipped managed reference: legacy-managed/Live2DUnity.dll, SHA-256 ${receipt.inputSha256}.
// Original SDK copyright: Live2D, 2013. Existing game-owned input; no original DLL is shipped.
// Invalid identifiers normalized by ILSpy; assembly attributes removed; namespace isolated.
// Shader lookup is explicit through the recovery resource bundle and fails for missing shaders.
// This source has not yet been validated by Unity 6000/IL2CPP, iOS or a device.
${code}`);
const report={preparedAt:new Date().toISOString(),reference,referenceSha256:hash(reference),originalAssemblySha256:receipt.inputSha256,
  source:output,sourceSha256:hash(output),changes:['invalid identifier normalization without renaming virtual methods','remove original assembly-level attributes','isolate Kairisei.Recovery.Cubism2 namespace','remove unreferenced compiler-generated blob holder; numeric initializers retained','explicit recovery shader resolver instead of silently missing original shader names'],
  originalDllCopiedIntoClient:false,originalDllExecuted:false,unity6000Validated:false,iOSValidated:false,deviceValidated:false};
fs.writeFileSync(path.join(root,'evidence/ios-package-search/order05-cubism-source-preparation.json'),JSON.stringify(report,null,2)+'\n');console.log(JSON.stringify(report,null,2));
