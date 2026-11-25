import fs from 'node:fs';
import path from 'node:path';
import { spawnSync } from 'node:child_process';
import { fileURLToPath } from 'node:url';

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
const output = path.join(root,'test-output/order03-live-engine');
fs.mkdirSync(output,{recursive:true});
fs.copyFileSync(path.join(root,'tests/Order03LiveEngine.go'),path.join(output,'main.go'));
fs.copyFileSync(path.join(root,'native/bridge/input_journal.go'),path.join(output,'input_journal.go'));
const reference = path.join(root,'source-review/kairisei-ma-ch/server').replaceAll('\\','/');
fs.writeFileSync(path.join(output,'go.mod'),'module kairisei.local/server/order03liveengine\n\ngo 1.25.0\n\nrequire kairisei.local/server v0.0.0\n\nreplace kairisei.local/server => '+JSON.stringify(reference)+'\n');
const run=spawnSync(path.join(root,'tools/go/bin/go.exe'),['build','-mod=mod','-buildvcs=false','-trimpath','-o',path.join(output,'engine.exe'),'.'],{
  cwd:output,env:{...process.env,GOROOT:path.join(root,'tools/go'),GOPATH:path.join(root,'tools/go-work'),GOMODCACHE:path.join(root,'tools/go-module-cache'),GOCACHE:path.join(root,'tools/go-build-cache'),GOTOOLCHAIN:'local',CGO_ENABLED:'0'},
  encoding:'utf8',windowsHide:true,timeout:120000
});
if(run.status!==0)throw Error(run.stdout+run.stderr);
console.log(path.join(output,'engine.exe'));
