import fs from 'node:fs';
import path from 'node:path';
import crypto from 'node:crypto';
import assert from 'node:assert/strict';
import {spawnSync} from 'node:child_process';
import {fileURLToPath} from 'node:url';
import {labelVerificationFiles} from './server-verification-names.mjs';
const root=path.resolve(path.dirname(fileURLToPath(import.meta.url)),'..');
const sha=file=>crypto.createHash('sha256').update(fs.readFileSync(file)).digest('hex');
const load=file=>JSON.parse(fs.readFileSync(path.join(root,file)));
const build=load('evidence/ios-package-search/order03-durable-solo-build-lyd.json');
const installer=load('evidence/ios-package-search/order03-solo-fix-installer-tests-lyd.json');
const probe=load('evidence/ios-package-search/order03-restart-probe-tests-lyd.json');
const startup=load('evidence/ios-package-search/order03-startup-pin-tests-lyd.json');
assert(build.success&&installer.success&&probe.success&&startup.success);
assert.equal(sha(path.join(root,build.build.executable)),build.build.sha256);
assert.equal(installer.scriptSha256,sha(path.join(root,'scripts/Install-Order03SoloFix.ps1')));
assert.equal(probe.scriptSha256,sha(path.join(root,'scripts/Verify-Order03Restart.ps1')));
assert.equal(startup.scriptSha256,sha(path.join(root,'scripts/Sync-Order03StartupPin.ps1')));
const stamp=new Date().toISOString().replace(/[-:.]/g,'').replace('T','-');
const directory=path.join(root,'dist/order03-solo-restart-fix-'+stamp+'-lyd');fs.mkdirSync(directory);
for(const name of ['Install-Order03SoloFix.ps1','Verify-Order03Restart.ps1','Collect-Order03RuntimeSelection.ps1','Sync-Order03StartupPin.ps1'])fs.writeFileSync(path.join(directory,name),fs.readFileSync(path.join(root,'scripts',name),'utf8').replace(/\r?\n/g,'\r\n'));
fs.copyFileSync(path.join(root,build.build.executable),path.join(directory,'kairi-server-durable-solo-lyd.exe'));
fs.copyFileSync(path.join(root,build.patch.file),path.join(directory,'order03-durable-solo-lyd.patch'));
fs.copyFileSync(path.join(root,'source-review/kairisei-ma-ch/LICENSE'),path.join(directory,'LICENSE-lyd.txt'));
fs.copyFileSync(path.join(root,'source-review/kairisei-ma-ch/NOTICE'),path.join(directory,'NOTICE-lyd.txt'));
const command=(script,args)=>'@echo off\r\npowershell.exe -NoLogo -NoProfile -ExecutionPolicy Bypass -File "%~dp0'+script+'" -TestRoot "%~dp0." '+args+'\r\npause\r\n';
fs.writeFileSync(path.join(directory,'Install-Order03-Solo-Fix.cmd'),command('Install-Order03SoloFix.ps1','-ExpectedPayloadSha256 '+build.build.sha256));
fs.writeFileSync(path.join(directory,'Rollback-Order03-Solo-Fix.cmd'),command('Install-Order03SoloFix.ps1','-ExpectedPayloadSha256 '+build.build.sha256+' -Rollback'));
fs.writeFileSync(path.join(directory,'Verify-Order03-Restart.cmd'),command('Verify-Order03Restart.ps1','-ExpectedExecutableSha256 '+build.build.sha256));
fs.writeFileSync(path.join(directory,'Repair-Order03-Startup-Pin.cmd'),command('Sync-Order03StartupPin.ps1',''));
const readme=`# 顺序 03：单人战斗重启恢复修复与验证

旧服务程序把单人开场保存在进程内存，重启后丢失，但已扣 BP 仍保存。本包给独立 iOS 测试服务保存已接受的单人开场，保留一小时有效期；放弃和后台主动中断会清除保存的开场。后台中断的保存失败会返回错误并回滚。安装/回滚会核验并同步 isolated-config.json 中的一项 expected_test_binary_sha256，原启动脚本及资源/原版服务校验保持执行。程序备份与已验证版本的程序替换均在原 bin 中完成，不读取账号数据库或存档。

1. 解压后，把包内所有文件复制到现有 IOS-Isolated-Test，与 Start-Phone-Test.cmd 同层，替换同名验证脚本。
2. 在原 iOS 服务窗口按 Ctrl+C 停止服务。不要停止其他服务。
3. 双击 Install-Order03-Solo-Fix.cmd。必须显示 State: installed 或 already_installed；失败时不要继续，返回 Order03-Solo-Fix-Install-…-lyd.json。
   若已经安装过本修复程序但原启动器显示 File SHA-256 differs，可直接运行 Repair-Order03-Startup-Pin.cmd，同步已验证的程序哈希后再启动，无需重装。
4. 双击原有 Start-Phone-Test.cmd，保留服务窗口。再双击 Verify-Order03-Restart.cmd，保留验证窗口。
5. 验证窗口出现 Now restart ONLY 提示后，在服务窗口按 Ctrl+C，再运行 Start-Phone-Test.cmd。验证窗口保持打开。
6. 返回新生成的 Order03-Restart-…-lyd.zip 和本次 Order03-Solo-Fix-Install-…-lyd.json。若原启动脚本没有启动成功，也返回它生成的预检结果；不要反复创建新测试身份。

验证会最多创建一个专用账号/角色，按 15 BP 规则入场一次，最后放弃，不领取通关奖励；专用身份可能保留，凭据仅在内存。源代码、真实 SQLite/HTTP 定向测试、程序编译和安装/回滚文件夹具已在本地完成。新程序尚未在你的服务器执行，原启动脚本与服务器部署仍须现场结果核对。

如需回滚，先在服务窗口 Ctrl+C 停服，再双击 Rollback-Order03-Solo-Fix.cmd，确认 rolled_back 后启动原 Start-Phone-Test.cmd。旧版仍不支持跨进程恢复未结束战斗。备份程序位于 bin/kairi-server-before-solo-fix-…-lyd.exe。

新程序 SHA-256：${build.build.sha256}
原程序 SHA-256：${build.base.observedExecutableSha256}
修复补丁 SHA-256：${build.patch.sha256}
`;
fs.writeFileSync(path.join(directory,'先读说明.md'),'\ufeff'+readme.replace(/\n/g,'\r\n'));
fs.writeFileSync(path.join(directory,'source-receipt.json'),JSON.stringify({base:build.base,patch:build.patch,build:build.build,localTestsPassed:true,serverDeployed:false},null,2)+'\n');
labelVerificationFiles(directory);
const files=fs.readdirSync(directory).map(name=>({name,bytes:fs.statSync(path.join(directory,name)).size,sha256:sha(path.join(directory,name))}));
for(const file of files)assert(path.basename(file.name,path.extname(file.name)).endsWith('-lyd'));
fs.writeFileSync(path.join(directory,'package-manifest-lyd.json'),JSON.stringify({files},null,2)+'\n');
const archive=directory+'.zip',quote=value=>"'"+value.replaceAll("'","''")+"'";
const zip=spawnSync('powershell.exe',['-NoProfile','-Command','Add-Type -AssemblyName System.IO.Compression.FileSystem; [IO.Compression.ZipFile]::CreateFromDirectory('+quote(directory)+','+quote(archive)+')'],{encoding:'utf8',windowsHide:true});assert.equal(zip.status,0,zip.stdout+zip.stderr);
const report={preparedAt:new Date().toISOString(),directory,archive,archiveBytes:fs.statSync(archive).size,archiveSha256:sha(archive),expectedExecutableSha256:build.build.sha256,files,serverExecuted:false,scope:'Prepared reversible binary install/rollback and one-entry manual restart verification for the independent iOS test service. Original startup scripts are retained and their现场 compatibility remains to be observed.'};
fs.writeFileSync(path.join(root,'evidence/ios-package-search/order03-solo-fix-package-lyd.json'),JSON.stringify(report,null,2)+'\n');console.log(JSON.stringify(report,null,2));
