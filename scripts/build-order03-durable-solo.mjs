import fs from 'node:fs';
import path from 'node:path';
import crypto from 'node:crypto';
import assert from 'node:assert/strict';
import {spawnSync} from 'node:child_process';
import {fileURLToPath} from 'node:url';
const root=path.resolve(path.dirname(fileURLToPath(import.meta.url)),'..');
const read=file=>fs.readFileSync(path.join(root,file));
const sha=file=>crypto.createHash('sha256').update(read(file)).digest('hex');
const load=file=>JSON.parse(read(file));
const reproduction=load('evidence/ios-package-search/order03-reviewed-patch-reproduction.json');
const tests=load('evidence/ios-package-search/order03-durable-solo-tests-lyd.json');
assert(tests.success&&tests.passed.includes('TestOrder03DurableSoloOpening/primary')&&tests.passed.includes('TestOrder03DurableSoloOpening/named'));
assert.equal(tests.testSha256,sha('tests/Order03DurableSolo_test.go'));
for(const source of tests.sourceFiles)assert.equal(source.sha256,sha(source.file));
const directory='test-output/order03-durable-solo-source-lyd',module=path.join(root,directory,'server');
const allowed=['server/internal/accountstore/accounts.go','server/internal/accountstore/account_snapshot.go','server/internal/admin/admin_maintenance.go'];
const baseModule=path.join(root,reproduction.logs.directory,'server');
const files=[];
function walk(base,relative='') {
  for(const entry of fs.readdirSync(base,{withFileTypes:true})) {
    const rel=path.posix.join(relative,entry.name),file=path.join(base,entry.name);
    assert(!entry.isSymbolicLink());
    if(entry.isDirectory())walk(file,rel);
    else if(!rel.endsWith('_test.go'))files.push(rel);
  }
}
walk(module);
for(const file of files.filter(file=>!allowed.includes('server/'+file)))assert.deepEqual(fs.readFileSync(path.join(module,file)),fs.readFileSync(path.join(baseModule,file)),'Unexpected source change: '+file);
const diffRoot=path.join(root,'test-output/order03-durable-solo-diff-lyd');fs.mkdirSync(diffRoot,{recursive:true});
let patch='';
for(const file of allowed) {
  const base=spawnSync('git',['-c','safe.directory='+path.join(root,'source-review/kairisei-ma-ch').replaceAll('\\','/'),'-C',path.join(root,'source-review/kairisei-ma-ch'),'show',reproduction.baseCommit+':'+file],{windowsHide:true});
  assert.equal(base.status,0);
  const changed=read(directory+'/'+file);
  const normalized=Buffer.from(changed.toString('utf8').replaceAll('\r\n','\n'));assert.deepEqual(changed,normalized,'Tested source must be LF canonical');
  for(const [prefix,data] of [['base',base.stdout],['patched',changed]]) {
    const destination=path.join(diffRoot,prefix,file);fs.mkdirSync(path.dirname(destination),{recursive:true});fs.writeFileSync(destination,data);
  }
  const diff=spawnSync('git',['-c','core.autocrlf=false','diff','--no-index','--','base/'+file,'patched/'+file],{cwd:diffRoot,encoding:'utf8',windowsHide:true});
  assert.equal(diff.status,1);
  patch+=diff.stdout.replaceAll('a/base/','a/').replaceAll('b/patched/','b/');
}
const patchFile='patches/order03-durable-solo-lyd.patch';fs.writeFileSync(path.join(root,patchFile),patch);
const applied=spawnSync('git',['-c','core.autocrlf=false','apply','--',path.join(root,patchFile)],{cwd:path.join(diffRoot,'base'),encoding:'utf8',windowsHide:true});
assert.equal(applied.status,0,applied.stdout+applied.stderr);
for(const file of allowed)assert.deepEqual(fs.readFileSync(path.join(diffRoot,'base',file)),read(directory+'/'+file));
const go=path.join(root,'tools/go/bin/go.exe');
const version=spawnSync(go,['version'],{encoding:'utf8',windowsHide:true});assert.equal(version.status,0);
assert(version.stdout.includes('go1.25.1'));
const executable=directory+'/kairi-server-durable-solo-lyd.exe';
const flags=['-buildvcs=false','-trimpath','-ldflags=-s -w'];
const build=spawnSync(go,['build',...flags,'-o',path.join(root,executable),'./cmd/kairi-server'],{cwd:module,encoding:'utf8',windowsHide:true,timeout:180000,env:{...process.env,GOOS:'windows',GOARCH:'amd64',CGO_ENABLED:'0',GOTOOLCHAIN:'local',GOMODCACHE:path.join(root,'tools/go-module-cache'),GOCACHE:path.join(root,'test-output/go-cache')}});
assert.equal(build.status,0,build.stdout+build.stderr);
const report={builtAt:new Date().toISOString(),success:true,scriptSha256:sha('scripts/build-order03-durable-solo.mjs'),base:{commit:reproduction.baseCommit,iosPatchSha256:reproduction.inputPatch.sha256,observedExecutableSha256:reproduction.build.sha256},patch:{file:patchFile,sha256:sha(patchFile),allowedProductionFiles:allowed,exactPatchedGitSourceBytesReproduced:true},sourceFiles:tests.sourceFiles,tests:{file:'evidence/ios-package-search/order03-durable-solo-tests-lyd.json',sha256:sha('evidence/ios-package-search/order03-durable-solo-tests-lyd.json')},build:{executable,bytes:read(executable).length,sha256:sha(executable),goVersion:version.stdout.trim(),GOOS:'windows',GOARCH:'amd64',CGO_ENABLED:'0',flags},serverDeployed:false,scope:'Compiled independently patched Windows iOS test-service executable. Two account persistence files and the Admin interruption call change; iOS patch and other inputs are retained. Local SQLite/HTTP/maintenance tests passed. Actual server deployment and process restart remain to be verified.'};
fs.writeFileSync(path.join(root,'evidence/ios-package-search/order03-durable-solo-build-lyd.json'),JSON.stringify(report,null,2)+'\n');
console.log(JSON.stringify(report,null,2));
