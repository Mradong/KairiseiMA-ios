import fs from 'node:fs';
import path from 'node:path';
import crypto from 'node:crypto';
import assert from 'node:assert/strict';
import {spawnSync} from 'node:child_process';
import {fileURLToPath} from 'node:url';
import {labelVerificationFiles} from './server-verification-names.mjs';
const root=path.resolve(path.dirname(fileURLToPath(import.meta.url)),'..');
const sha=file=>crypto.createHash('sha256').update(fs.readFileSync(file)).digest('hex');
const tests=JSON.parse(fs.readFileSync(path.join(root,'evidence/ios-package-search/order03-startup-pin-tests-lyd.json')));
assert(tests.success);assert.equal(tests.scriptSha256,sha(path.join(root,'scripts/Sync-Order03StartupPin.ps1')));
const stamp=new Date().toISOString().replace(/[-:.]/g,'').replace('T','-');
const directory=path.join(root,'dist/order03-startup-pin-repair-'+stamp+'-lyd');fs.mkdirSync(directory);
for(const name of ['Sync-Order03StartupPin.ps1','Collect-Order03RuntimeSelection.ps1'])fs.writeFileSync(path.join(directory,name),fs.readFileSync(path.join(root,'scripts',name),'utf8').replace(/\r?\n/g,'\r\n'));
fs.writeFileSync(path.join(directory,'Repair-Order03-Startup-Pin.cmd'),'@echo off\r\npowershell.exe -NoLogo -NoProfile -ExecutionPolicy Bypass -File "%~dp0Sync-Order03StartupPin.ps1" -TestRoot "%~dp0."\r\npause\r\n');
const readme=`# 修复已安装程序的启动校验值

新程序已替换后，原 Start-IOS-Test.ps1 仍根据 isolated-config.json 的 expected_test_binary_sha256 校验旧程序，因此显示 File SHA-256 differs。本包先验证程序和原启动脚本的已知哈希，再把这一项同步为实际已验证的程序哈希。保留原配置的其他字段、BOM 和换行，原版服务/资源/iOS平台/内存检查照常执行。

1. 把本包内全部文件复制到原 IOS-Isolated-Test，与 Start-Phone-Test.cmd 同层，替换同名文件。
2. 双击 Repair-Order03-Startup-Pin.cmd。应显示 pin_synchronized 或 already_synchronized。
3. 运行原 Start-Phone-Test.cmd，保留服务窗口；再运行已有 Verify-Order03-Restart-lyd.cmd。按验证提示在服务窗口 Ctrl+C，再启动原 Start-Phone-Test.cmd，验证窗口保持打开。
4. 返回 Order03-Startup-Pin-…-lyd.json 和新生成的 Order03-Restart-…-lyd.zip；也请保留并返回已生成的安装回执。若同步失败，只返回同步结果，不继续入场。

本包不包含或替换服务器程序，不读取账号/存档，不调用游戏接口，不创建身份或扣费。后续重启验证仍最多1个专用账号/角色、1次15BP入场后放弃，无通关奖励。原启动脚本通过已收到的原文核验；实际服务器启动与恢复还需返回结果。

原程序哈希：7704963e90c587aa4e8167fc7e74299f231e8b22f45a10012fbbe44301727918
修复程序哈希：1c1488558e2ed859cb8a9f36b8720b125c0dbceb3ac3c83e3bec2603e1fdf2c1
原启动脚本哈希：9aef0e3c2ed167d476b4fc9b6135a3ae26229a574ba19ab8d82440ebd48ce6a6
`;
fs.writeFileSync(path.join(directory,'先读说明.md'),'\ufeff'+readme.replace(/\n/g,'\r\n'));
labelVerificationFiles(directory);
const files=fs.readdirSync(directory).map(name=>({name,bytes:fs.statSync(path.join(directory,name)).size,sha256:sha(path.join(directory,name))}));
for(const file of files)assert(path.basename(file.name,path.extname(file.name)).endsWith('-lyd'));
fs.writeFileSync(path.join(directory,'package-manifest-lyd.json'),JSON.stringify({files},null,2)+'\n');
const archive=directory+'.zip',quote=value=>"'"+value.replaceAll("'","''")+"'";
const result=spawnSync('powershell.exe',['-NoProfile','-Command','Add-Type -AssemblyName System.IO.Compression.FileSystem; [IO.Compression.ZipFile]::CreateFromDirectory('+quote(directory)+','+quote(archive)+')'],{encoding:'utf8',windowsHide:true});assert.equal(result.status,0,result.stdout+result.stderr);
const report={preparedAt:new Date().toISOString(),directory,archive,archiveBytes:fs.statSync(archive).size,archiveSha256:sha(archive),files,serverExecuted:false,scope:'Targeted startup pin repair for the already installed reviewed binary; original startup/resource/production checks retained. No binary/account/state replacement.'};
fs.writeFileSync(path.join(root,'evidence/ios-package-search/order03-startup-pin-package-lyd.json'),JSON.stringify(report,null,2)+'\n');console.log(JSON.stringify(report,null,2));
