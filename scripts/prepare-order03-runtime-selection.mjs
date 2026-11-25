import fs from 'node:fs';
import path from 'node:path';
import crypto from 'node:crypto';
import { spawnSync } from 'node:child_process';
import { fileURLToPath } from 'node:url';
import {labelVerificationFiles} from './server-verification-names.mjs';

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
const sha = filename => crypto.createHash('sha256').update(fs.readFileSync(filename)).digest('hex');
const script = 'scripts/Collect-Order03RuntimeSelection.ps1';
const tests = JSON.parse(fs.readFileSync(path.join(root, 'evidence/ios-package-search/order03-runtime-selection-tests-lyd.json'), 'utf8'));
if (!tests.success || tests.scriptSha256 !== sha(path.join(root, script))) throw new Error('Collector tests missing or stale');
const stamp = new Date().toISOString().replace(/[-:.]/g, '').replace('T','-');
const directory = path.join(root, 'dist/order03-runtime-selection-' + stamp + '-lyd');
fs.mkdirSync(directory);
for (const [from, to] of [
  [script, 'Collect-Order03RuntimeSelection.ps1'],
  ['scripts/Collect-Order03-Selection.cmd','Collect-Order03-Selection.cmd'],
  ['evidence/ios-package-search/order03-expected-tables.json','order03-expected-tables.json']
]) fs.writeFileSync(path.join(directory, to), fs.readFileSync(path.join(root, from), 'utf8').replace(/\r?\n/g, '\r\n'));
const instructions = `# 顺序 03 运行进程资源选择补证

服务器程序已由基础源码加 source-patch 精确复现。这个包用于进一步核对运行进程实际选择的资源清单和 iOS profile。

1. 将三个脚本/清单文件复制到现有 IOS-Isolated-Test 文件夹，与 isolated-config.json 同层。
2. 双击 Collect-Order03-Selection.cmd，等待显示 Saved。
3. 返回新生成的 Order03-Selection-日期时间-lyd.zip。

脚本使用系统自带 PowerShell 5.1 和本机进程信息，先核对 27040 监听程序的 SHA-256，再解析已确认程序的资源参数。导出只保留选定资源清单、29 张 CSV、战斗/普通关卡静态文件及公共平台配置的身份信息。进程参数正文、账号数据和配置敏感字段留在导出范围之外。操作计数记录在 selection-lyd.json。

正常结果为 state=selected_paths_hashed。如果 state=incomplete，请直接返回生成的 ZIP，failureStage 会指出首个未完成环节。

本地已验证引号/空格参数、最后一次资源参数覆盖、流式 JSON 路径读取、分离目录、路径拒绝、CIM 时间精度以及完整入口的模拟进程场景。服务器实际执行尚待确认。所选参数与文件哈希支持进一步判断加载关系，内存中的实际字节和历史 CSV 提取来源仍需单独核实。
`;
fs.writeFileSync(path.join(directory, '先读说明.md'), '\ufeff' + instructions.replace(/\n/g, '\r\n'));
labelVerificationFiles(directory);
const files = fs.readdirSync(directory).map(name => ({ name, bytes: fs.statSync(path.join(directory, name)).size, sha256: sha(path.join(directory, name)) }));
fs.writeFileSync(path.join(directory, 'package-manifest-lyd.json'), JSON.stringify({ collector:'order03-selection-20261124-1', files }, null, 2) + '\n');
const quote = value => "'" + value.replaceAll("'", "''") + "'";
const zip = directory + '.zip';
const result = spawnSync('powershell.exe', ['-NoProfile','-Command', 'Add-Type -AssemblyName System.IO.Compression.FileSystem; [IO.Compression.ZipFile]::CreateFromDirectory(' + quote(directory) + ',' + quote(zip) + ')'], { encoding:'utf8', windowsHide:true });
if (result.status !== 0) throw new Error(result.stdout + result.stderr);
const report = { preparedAt:new Date().toISOString(), archive:zip, archiveBytes:fs.statSync(zip).size, archiveSha256:sha(zip), files, sourceScriptSha256:sha(path.join(root, script)), serverExecuted:false, scope:'Prepared targeted process/resource-selection collector. Local synthetic fixture validation only.' };
fs.writeFileSync(path.join(root, 'evidence/ios-package-search/order03-runtime-selection-package-lyd.json'), JSON.stringify(report,null,2)+'\n');
console.log(JSON.stringify(report,null,2));
