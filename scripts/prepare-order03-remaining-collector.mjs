import fs from 'node:fs';
import path from 'node:path';
import crypto from 'node:crypto';
import { spawnSync } from 'node:child_process';
import { fileURLToPath } from 'node:url';

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
const stamp = new Date().toISOString().replace(/[-:.]/g, '').replace('T', '-');
const directory = path.join(root, 'dist/order03-server-collector-' + stamp);
const script = 'scripts/Collect-Order03RemainingEvidence.ps1';
const sha = filename => crypto.createHash('sha256').update(fs.readFileSync(filename)).digest('hex');
const validation = JSON.parse(fs.readFileSync(path.join(root, 'evidence/ios-package-search/order03-remaining-collector-tests.json'), 'utf8'));
if (!validation.success || validation.scriptSha256 !== sha(path.join(root, script))) throw new Error('Collector validation is missing or stale');
fs.mkdirSync(directory);
const selected = [
  [script, 'Collect-Order03RemainingEvidence.ps1'],
  ['scripts/Collect-Order03-Remaining.cmd', 'Collect-Order03-Remaining.cmd'],
  ['evidence/ios-package-search/order03-expected-tables.json', 'order03-expected-tables.json']
];
for (const [from, to] of selected) {
  const content = fs.readFileSync(path.join(root, from), 'utf8').replace(/\r?\n/g, '\r\n');
  fs.writeFileSync(path.join(directory, to), content);
}
const instructions = `# 顺序 03 服务器补证采集

已根据共享对话中的 Windows Server 2016 和 IOS-Isolated-Test 部署方式准备。运行环境为系统自带 Windows PowerShell 5.1。

1. 解压本包，将三个文件 Collect-Order03RemainingEvidence.ps1、Collect-Order03-Remaining.cmd、order03-expected-tables.json 复制到现有 IOS-Isolated-Test 文件夹，与 isolated-config.json 放在同一层。
2. 双击 Collect-Order03-Remaining.cmd。看到 Saved 后，把当前目录中新生成的 Order03-Remaining-日期时间.zip 交给客户端项目的 Codex。

对话中该目录为 C:\\Users\\Administrator\\Downloads\\IOS-Isolated-Test-Preparation-20261119\\IOS-Isolated-Test。请使用你当前实际运行的同名目录。

脚本采集当前 27040 监听进程的程序哈希和内嵌 Go 构建信息，解析配置中的资源路径并核对主表身份，收集找到的 Go 源码、部署脚本、来源清单、原始静态关卡 JSON，以及经过字段筛选的启动日志。每次新建结果目录和 ZIP，SHA256SUMS.txt 固定包内文件字节。

source.json 表明实际找到了哪些源码以及 Git 提交和工作树状态。runtime.json 记录磁盘程序、配置和资源身份。static/ 包含找到且通过敏感字段检查的静态 JSON 原始字节。startup-observations.json 仅保留选定的启动事件。capture.json 记录遗漏原因和操作范围。remaining.json 列出还需复核的事实。

仅在有需要时使用以下可选方式。

- 源码位于其他位置，可在 PowerShell 中执行 & .\\Collect-Order03RemainingEvidence.ps1 -SourceRoot '实际包含 go.mod 和 cmd\\kairi-server 的 server 目录'。
- 资源清单位于其他位置，可传 -ResourceManifest '实际 resource-set.json 路径'。
- 已有脱敏构建记录、来源说明或独立战斗测试证据，可放进 IOS-Isolated-Test\\Order03-Provided-Evidence 后再次运行。支持该目录直接包含的 JSON、MD、TXT、PS1 和 PATCH 文件。

结果中的来源关联需要结合实际材料判断。缺少源码、历史来源或实测记录时，对应项保持 UNKNOWN/NOT_TESTED。本脚本已在本地合成目录通过 PowerShell 5.1 检查，服务器实际执行尚待确认。采集只写新证据目录，不调用游戏业务接口，操作计数记录在 capture.json。
`;
fs.writeFileSync(path.join(directory, '先读说明.md'), '\ufeff' + instructions.replace(/\n/g, '\r\n'));
const files = fs.readdirSync(directory).map(name => ({ name, bytes: fs.statSync(path.join(directory, name)).size, sha256: sha(path.join(directory, name)) }));
fs.writeFileSync(path.join(directory, 'package-manifest.json'), JSON.stringify({ collector: 'order03-remaining-20261124-1', files, checkedOn: '2026-11-24' }, null, 2) + '\n');
const zipPath = directory + '.zip';
const quote = text => "'" + text.replaceAll("'", "''") + "'";
const result = spawnSync('powershell.exe', ['-NoProfile', '-Command',
  `Add-Type -AssemblyName System.IO.Compression.FileSystem; [IO.Compression.ZipFile]::CreateFromDirectory(${quote(directory)}, ${quote(zipPath)})`],
{ encoding: 'utf8', windowsHide: true });
if (result.status !== 0) throw new Error(result.stdout + result.stderr);
const report = {
  preparedAt: new Date().toISOString(), archive: zipPath, archiveBytes: fs.statSync(zipPath).size,
  archiveSha256: sha(zipPath), files, collectorSourceSha256: sha(path.join(root, script)),
  testedWith: 'Windows PowerShell 5.1, synthetic deployment only', serverExecuted: false,
  sharedConversation: 'https://chatgpt.com/share/6b053936-fbc4-83ec-9054-585a966e7ea0',
  scope: 'Existing deployment/source/static evidence capture. Missing original provenance and unexecuted service contracts remain UNKNOWN/NOT_TESTED.'
};
fs.writeFileSync(path.join(root, 'evidence/ios-package-search/order03-remaining-collector-package.json'), JSON.stringify(report, null, 2) + '\n');
console.log(JSON.stringify(report, null, 2));
