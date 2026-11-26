import fs from 'node:fs';
import path from 'node:path';
import crypto from 'node:crypto';
import assert from 'node:assert/strict';
import {spawnSync} from 'node:child_process';
import {fileURLToPath} from 'node:url';
const root=path.resolve(path.dirname(fileURLToPath(import.meta.url)),'..');
const stamp=new Date().toISOString().replace(/[:.]/g,'-');
const folder=path.join(root,'dist','order05-scene-resources-'+stamp+'-lyd');
fs.mkdirSync(folder,{recursive:true});
const script='Export-Order05-SceneResources-lyd.ps1';
fs.copyFileSync(path.join(root,'scripts',script),path.join(folder,script));
fs.writeFileSync(path.join(folder,'Export-Order05-SceneResources-lyd.cmd'),'@echo off\r\ncd /d "%~dp0"\r\npowershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0'+script+'"\r\necho.\r\npause\r\n');
fs.writeFileSync(path.join(folder,'README-lyd.md'),`# 顺序05真实场景资源导出

在独立测试服务器解压此包，双击 Export-Order05-SceneResources-lyd.cmd。

默认读取用户提供的 C:\\Users\\Administrator\\Desktop\\ma\\Server\\resource-set，核对03已接受资源清单；并检查相邻 Server\\platform\\pc\\bundles。
只复制 ui_home.dat、map_home_01000.dat、live2d_uathach.dat 的固定 canonical/PC 版本，长度和SHA-256必须与公开2.0.4清单一致。
返回新生成的 Order05-SceneResources-时间-lyd.zip。PARTIAL_RESOURCES也请保留结果，其中明确标记缺失/不匹配，不要改哈希绕过。

此脚本不读取账号、存档、凭据或配置，不发登录/游戏请求，不启停服务，不修改原资源。仅在本包旁写导出目录/ZIP。
包外服务器资源不可删除；导出不是iOS编译或真实场景运行证明。所有包内/输出文件名均包含-lyd。
`, 'utf8');
const hash=file=>crypto.createHash('sha256').update(fs.readFileSync(file)).digest('hex');
const names=fs.readdirSync(folder).sort();assert(names.every(n=>n.includes('-lyd.')));
fs.writeFileSync(path.join(folder,'SHA256SUMS-lyd.txt'),names.map(n=>hash(path.join(folder,n))+'  '+n).join('\n')+'\n');
const zip=folder+'.zip';
const escaped=p=>"'"+p.replaceAll("'","''")+"'";
const ps=`Compress-Archive -LiteralPath @(${fs.readdirSync(folder).map(n=>escaped(path.join(folder,n))).join(',')}) -DestinationPath ${escaped(zip)} -CompressionLevel Optimal`;
const result=spawnSync('powershell',['-NoProfile','-ExecutionPolicy','Bypass','-Command',ps],{encoding:'utf8',windowsHide:true});
assert.equal(result.status,0,result.stdout+result.stderr);
const report={preparedAt:new Date().toISOString(),archive:path.relative(root,zip).replaceAll('\\','/'),bytes:fs.statSync(zip).size,sha256:hash(zip),
  scriptSha256:hash(path.join(folder,script)),expectedResourceSetRoot:'C:\\Users\\Administrator\\Desktop\\ma\\Server\\resource-set',
  originalResourcesReadOnly:true,accountDataRead:false,networkRequests:0,serverExecuted:false,readyForSceneAcceptance:false,files:fs.readdirSync(folder)};
fs.writeFileSync(path.join(root,'evidence/ios-package-search/order05-scene-resource-collector-package-lyd.json'),JSON.stringify(report,null,2)+'\n');
console.log(JSON.stringify(report,null,2));
