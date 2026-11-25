import fs from 'node:fs';
import path from 'node:path';
import crypto from 'node:crypto';
import {spawnSync} from 'node:child_process';
import {fileURLToPath} from 'node:url';
import {labelVerificationFiles} from './server-verification-names.mjs';
const root=path.resolve(path.dirname(fileURLToPath(import.meta.url)),'..');
const sha=file=>crypto.createHash('sha256').update(fs.readFileSync(file)).digest('hex');
const tests=JSON.parse(fs.readFileSync(path.join(root,'evidence/ios-package-search/order03-restart-probe-tests-lyd.json')));
if(!tests.success||tests.scriptSha256!==sha(path.join(root,'scripts/Verify-Order03Restart.ps1'))||tests.helpersSha256!==sha(path.join(root,'scripts/Collect-Order03RuntimeSelection.ps1')))throw new Error('Restart fixture tests missing or stale');
const stamp=new Date().toISOString().replace(/[-:.]/g,'').replace('T','-');
const directory=path.join(root,'dist/order03-restart-probe-'+stamp+'-lyd');fs.mkdirSync(directory);
for(const name of ['Verify-Order03Restart.ps1','Verify-Order03-Restart.cmd','Collect-Order03RuntimeSelection.ps1'])fs.writeFileSync(path.join(directory,name),fs.readFileSync(path.join(root,'scripts',name),'utf8').replace(/\r?\n/g,'\r\n'));
const readme=`# 顺序 03 独立服务进程重启恢复验证

本步骤验证单人入场后，真正重启 iOS 测试服务时账号、同一战斗开场和扣费能否恢复。验证脚本会创建一个专用测试账号/角色，入场一次按 15 BP 规则扣费，最终以未通关方式放弃。

1. 把包内三个脚本文件放到现有 IOS-Isolated-Test，与 isolated-config.json 同层，替换同名脚本。
2. 先双击 Start-Phone-Test.cmd 启动 iOS 测试服务，保留“服务窗口”；再双击 Verify-Order03-Restart.cmd，保持这个“验证窗口”打开。新脚本在首次登录和重启后均等待本机服务健康检查通过，并核对程序哈希；本机请求不使用系统代理。
3. 当验证窗口显示 Now restart ONLY the iOS test service window for port 27040 时，在原来运行 iOS 服务的窗口按 Ctrl+C，再双击 Start-Phone-Test.cmd。
4. 服务重新启动后，验证窗口会自动重新登录、恢复同一 Start、核对 seed/start_time/完整开场响应、放弃并核对单次扣费。
5. 返回新生成的 Order03-Restart-日期时间-lyd.zip。若失败也会保存阶段、请求路径与 HTTP/网络状态。首次服务就绪检查未通过时，不发起登录、创建角色或战斗入场。

此步骤会短暂中断 27040/27041 的独立 iOS 测试服务。服务的停止/启动由你操作，验证脚本保留专用账号和会话在内存，结果只输出状态与程序身份。新建测试身份可能继续保存在服务端。本地已用模拟 HTTP/进程更换验证完整脚本，实际服务器重启尚待本次操作。
`;
fs.writeFileSync(path.join(directory,'先读说明.md'),'\ufeff'+readme.replace(/\n/g,'\r\n'));
labelVerificationFiles(directory);
const files=fs.readdirSync(directory).map(name=>({name,bytes:fs.statSync(path.join(directory,name)).size,sha256:sha(path.join(directory,name))}));
fs.writeFileSync(path.join(directory,'package-manifest-lyd.json'),JSON.stringify({files},null,2)+'\n');
const quote=value=>"'"+value.replaceAll("'","''")+"'";
const archive=directory+'.zip';
const zip=spawnSync('powershell.exe',['-NoProfile','-Command','Add-Type -AssemblyName System.IO.Compression.FileSystem; [IO.Compression.ZipFile]::CreateFromDirectory('+quote(directory)+','+quote(archive)+')'],{encoding:'utf8',windowsHide:true});
if(zip.status!==0)throw new Error(zip.stdout+zip.stderr);
const report={preparedAt:new Date().toISOString(),archive,archiveBytes:fs.statSync(archive).size,archiveSha256:sha(archive),files,serverExecuted:false,scope:'Prepared one-entry dedicated-account manual iOS-service restart recovery probe. Actual server execution pending.'};
fs.writeFileSync(path.join(root,'evidence/ios-package-search/order03-restart-probe-package-lyd.json'),JSON.stringify(report,null,2)+'\n');
console.log(JSON.stringify(report,null,2));
