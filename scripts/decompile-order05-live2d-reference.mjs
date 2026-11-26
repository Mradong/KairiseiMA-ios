import fs from 'node:fs';
import path from 'node:path';
import crypto from 'node:crypto';
import assert from 'node:assert/strict';
import {spawnSync} from 'node:child_process';
import {fileURLToPath} from 'node:url';
const root=path.resolve(path.dirname(fileURLToPath(import.meta.url)),'..');
const out=path.join(root,'test-output/order05-live2d-reference');fs.mkdirSync(out,{recursive:true});
const runtime=path.join(root,'tools/dotnet/shared/Microsoft.NETCore.App/10.0.15');
const decompiler=path.join(root,'tools/csharp/decompiler/ICSharpCode.Decompiler.dll');
const cecil=path.join(root,'tools/csharp/decompiler/Mono.Cecil.dll');
const source=path.join(out,'ReferenceDecompiler.cs');
fs.writeFileSync(source,`using System; using System.IO; using ICSharpCode.Decompiler; using ICSharpCode.Decompiler.CSharp; using ICSharpCode.Decompiler.CSharp.Transforms;
public static class ReferenceDecompiler { public static int Main(string[] args) { var d = new CSharpDecompiler(args[0],new DecompilerSettings { ThrowOnAssemblyResolveErrors = false }); d.AstTransforms.Add(new EscapeInvalidIdentifiers()); File.WriteAllText(args[1],d.DecompileWholeModuleAsString()); Console.WriteLine("Reference-only decompilation completed with invalid identifiers normalized; source assembly was not executed."); return 0; } }
`);
const exe=path.join(out,'ReferenceDecompiler.dll');
const runtimeReferences=['System.Private.CoreLib.dll','System.Runtime.dll','System.Console.dll','System.IO.FileSystem.dll','System.Collections.dll','System.Collections.Immutable.dll','System.Reflection.Metadata.dll','netstandard.dll','mscorlib.dll'];
const normalizer=path.join(out,'NormalizeCubismMetadata.dll');
const normalizationBuild=spawnSync(path.join(root,'tools/csharp/toolset/tasks/net472/csc.exe'),['/nologo','/utf8output','/nostdlib','/target:exe','/out:'+normalizer,'/reference:'+cecil,...runtimeReferences.map(n=>'/reference:'+path.join(runtime,n)),path.join(root,'tests/NormalizeCubismMetadata.cs')],{encoding:'utf8',windowsHide:true});
assert.equal(normalizationBuild.status,0,normalizationBuild.stdout+normalizationBuild.stderr);
fs.copyFileSync(cecil,path.join(out,'Mono.Cecil.dll'));
fs.writeFileSync(path.join(out,'NormalizeCubismMetadata.runtimeconfig.json'),JSON.stringify({runtimeOptions:{tfm:'net10.0',framework:{name:'Microsoft.NETCore.App',version:'10.0.15'}}}));
const normalizedAssembly=path.join(out,'Live2DUnity-normalized.dll');
for(const name of ['UnityEngine.dll','mscorlib.dll','System.dll'])fs.copyFileSync(path.join(root,'legacy-managed',name),path.join(out,name));
const normalization=spawnSync(path.join(root,'tools/dotnet/dotnet.exe'),[normalizer,path.join(root,'legacy-managed/Live2DUnity.dll'),normalizedAssembly],{encoding:'utf8',windowsHide:true});
assert.equal(normalization.status,0,normalization.stdout+normalization.stderr);
const build=spawnSync(path.join(root,'tools/csharp/toolset/tasks/net472/csc.exe'),['/nologo','/utf8output','/nostdlib','/target:exe','/out:'+exe,'/reference:'+decompiler,...runtimeReferences.map(n=>'/reference:'+path.join(runtime,n)),source],{encoding:'utf8',windowsHide:true});
assert.equal(build.status,0,build.stdout+build.stderr);
fs.copyFileSync(decompiler,path.join(out,'ICSharpCode.Decompiler.dll'));
fs.writeFileSync(path.join(out,'ReferenceDecompiler.runtimeconfig.json'),JSON.stringify({runtimeOptions:{tfm:'net10.0',framework:{name:'Microsoft.NETCore.App',version:'10.0.15'}}}));
const input=path.join(root,'legacy-managed/Live2DUnity.dll'),output=path.join(out,'Live2DUnity-reference.cs');
const hash=p=>crypto.createHash('sha256').update(fs.readFileSync(p)).digest('hex');const before=hash(input);
const run=spawnSync(path.join(root,'tools/dotnet/dotnet.exe'),[exe,normalizedAssembly,output],{encoding:'utf8',windowsHide:true});
const report={checkedAt:new Date().toISOString(),success:run.status===0,normalization:normalization.stdout.trim(),normalizedWorkingCopySha256:hash(normalizedAssembly),stdout:run.stdout.trim(),stderr:run.stderr.trim(),input:'legacy-managed/Live2DUnity.dll',inputSha256:before,inputUnchanged:hash(input)===before,
  referenceOutput:path.relative(root,output).replaceAll('\\','/'),referenceSha256:fs.existsSync(output)?hash(output):null,
  scope:'Only existing managed Live2D assembly metadata/IL decompiled into an ignored local reference. No SDK method, original game, account or Unity license executed/read. Raw reference output is not a production source or upload artifact.'};
fs.writeFileSync(path.join(root,'evidence/ios-package-search/order05-live2d-reference-decompilation.json'),JSON.stringify(report,null,2)+'\n');console.log(JSON.stringify(report,null,2));if(!report.success)process.exitCode=1;
