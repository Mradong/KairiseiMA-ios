# 当前状态

## 2026-11-23：03 / 真实敌人基础数据的准备态查询

用隔离候选快照和服务端静态候选关卡中两只敌人的基础字段构造注册记录，玩家为合成数据，未注册敌方技能或 AI。修复前，4 组 `ENEMY_TRIBAL` 查询原 DLL 成功而重建桥拒绝：真实目录为无反应敌人也创建了空量表对象，桥将它一概视为活跃量表。修复后只在准备态查询允许完全零状态、零限制、无反应的量表；注册战斗创建/下一波仍保持拒绝，活跃量表仍拒绝。前后证据分别为 `evidence/ios-package-search/order03-real-enemy-query-before-fix.json`、`order03-real-enemy-query.json`。

Windows x64 原 DLL 对照的两敌、两种族条件共 4 组均一致；Go 测试覆盖空量表查询放行、战斗拒绝和十种非空量表字段拒绝；既有 1,856 组准备态敌人查询及注册战斗合成回归通过。当前重建 DLL SHA-256 `e62fa374bd0a94b825dc154517a366747d28695babab1c7ca872d2e944007d8b`。此结果不覆盖真实敌方技能、AI、量表行为、完整注册战斗、客户端版本匹配、ARM64 或设备，修复不在既有 IPA；顺序 03 继续。

## 2026-11-23：03 / 隔离候选主表快照

将哈希核对过的 29 张交付 CSV 原文与现代导出中当前部分桥接所需的 23 个候选枚举族组合，在 `test-output/order03-candidate/DO-NOT-DEPLOY-CombatTables.json` 生成 57,738,263 字节隔离候选，SHA-256 `f69af8be21c9b4afa0e823d89f48908f12e8b729e4cef4ed574a640e960fb1cb`。生成时验证每表哈希、UTF-8 字节往返及枚举声明解析，逐表和逐枚举源哈希见 `evidence/ios-package-search/order03-candidate-snapshot.json`。

实际 Windows .NET `CombatTableSet.Validate` 通过，Windows x64 原 DLL 和重建 DLL 均成功打开及关闭目录；证据 `evidence/ios-package-search/order03-candidate-validation.json`。这比单一占位枚举更接近完整快照，但 23 族仅按当前部分桥接选取，原客户端版本、表版本、完整注册语义和真实效果未证实；候选不得复制到客户端、上传或打包。顺序 03 继续核验版本、完整枚举和真实服务业务契约；没有 Unity、ARM64 或真机结果。

## 2026-11-23：03 / 交付主表原 DLL 目录加载对照

使用已核验的 29 张交付 CSV 构成的 57,706,143 字节隔离测试快照，Windows x64 原 DLL 和修复后重建 DLL 的 `battlecore_catalog_open_tables` 及关闭均返回 0。测试快照仍仅含虚构占位枚举，保存在 `test-output`，不可用于注册或交付；没有创建战斗。证据 `evidence/ios-package-search/order03-real-catalog-open.json`，脚本 `scripts/test-order03-real-catalog-open.mjs`、测试 `tests/Order03RealCatalogOpenProbe.cs`，原 DLL 哈希保持一致。

目录加载成功不证明真实枚举、主表与客户端版本匹配、各字段及战斗语义、Unity 资源或 iOS 设备行为。完整 schema、真实业务接口证据与生产快照仍待验证；顺序 03 保持进行中。未发起在线业务请求或新构建。

## 2026-11-23：03 / 修正效果参数枚举族映射

Windows 合成注册技能对照发现：`DOT` 和 `TRANCE_GAUGE_STATE` 两种规则，原 DLL 要求 schema 使用 `SKILL_ROLE_DOT`、`ENEMY_TRANCE_GAUGE_STATE`，而重建桥错误使用规则名。修复 `native/bridge/registered_skill.go` 后重新编译 Windows x64 DLL，四个正确/错误族名查询均与原 DLL 的状态和结果一致；错误族名拒绝时输出保持不变。修复前的四处差异保留在 `evidence/ios-package-search/order03-role-alias-before-fix.json`，修复后见 `evidence/ios-package-search/order03-role-alias-differential.json`。

Go 桥接测试通过，既有 155 组注册技能查询原 DLL 差分通过；当前重建 DLL SHA-256 `a4553408677125adadf806c0f201bbb0981d9180b6a74ae88bb9e31437b98734`。这些只覆盖合成查询中的参数解码与拒绝，不证明效果执行、真实主表版本、完整枚举、ARM64、Unity 或真机；该修复不在已交付 IPA。继续顺序 03 的版本与实际业务契约核验。

## 2026-11-23：03 / 独立角色卡牌契约与交付主表编号对照

在已授权独立服务新建隔离游客和测试角色，未调用 HomeShow，实测 `/CardShow2` HTTP 200、三段响应：返回 10 张卡及 4 个卡组，40 个非零主卡槽均能关联返回的卡牌实例。读取前核对交付的 `card.csv` 与 `card_alchemist.csv` SHA-256，10 张卡的定义编号均能在其中找到；不输出或保存账号、卡牌实例编号、会话、名字或响应正文。非法字段请求返回 HTTP 400。可复核脱敏结果 `evidence/ios-package-search/order03-isolated-card-contract.json`，脚本 `scripts/probe-order03-isolated-card-contract.mjs`。

本次创建的测试身份与角色可能持久化；未写卡组或调用主页、关卡、入场、续关及结算。编号存在和初始卡组引用完整不证明完整数据、技能参数、客户端枚举或服务/主表同版本；登录返回的已知 iOS 内容清单仅为 UI 包。顺序 03 仍需真实战斗契约、注册枚举及匹配验证，无新 Unity 或真机结果。

## 2026-11-23：03 / 真实效果参数枚举名称盘点

对服务端真实效果表审计中观察到的 11 类非 VALUE 规则，将其符号与现代导出枚举声明静态对照：除单独分类的空值、`NULL` 和位掩码 `ALL` 外，所有观察到的名称均有同名项。`DOT` 实际对应 `SKILL_ROLE_DOT`，`TRANCE_GAUGE_STATE` 对应 `ENEMY_TRANCE_GAUGE_STATE`；位掩码规则使用基础 `ATTR` / `SKILL_KIND`，重建桥按规则名查 schema，未来构建时需要明确处理别名。脚本与逐族计数、候选值和源哈希见 `scripts/audit-order03-role-enum-tokens.mjs`、`evidence/ios-package-search/order03-role-enum-token-audit.json`。

别名依据 1.4.0 有方法体代码，现代相应方法为存根；名称存在不证明这批表来自该客户端或数值/语义、ABI 和战斗效果一致，尚未生成生产 schema。下一步核验注册结构映射、版本关联及实际业务脱敏契约；顺序 03 保持进行中。没有新构建或真机结果。

## 2026-11-23：03 / 独立服务脱敏协议局部实测

对已授权独立服务使用新生成的隔离游客身份，实测 `/healthz`、`/local/client/login` 和 `/Connect`：健康检查 PASS，正常登录 HTTP 200 且 API/资源地址同源，Connect HTTP 200、双段响应、公共 `res_code=0`、未创建角色；无效 UUID 登录和 Connect 额外字段分别返回 HTTP 400。证据只记录字段名称、类型、状态和测试状态，不记录 UUID、用户 ID、会话或响应原文：`evidence/ios-package-search/order03-isolated-contract-probe.json`，脚本 `scripts/probe-order03-isolated-contract.mjs`。

游客登录可能在隔离服务保存新身份；没有创建角色，也没有调用 HomeShow、关卡、入场、续关或结算。此结果补充实际登录与连接契约，不能证明卡组或战斗接口、服务端部署版本、真实主表和客户端版本相互匹配。顺序 03 仍需相应脱敏业务证据与枚举/快照核验；无 Unity、iOS 或设备测试。

## 2026-11-23：03 / 枚举查询拒绝原因与复测

上轮 72 次一致拒绝均发生在 `SELF_MAIN_DECK_SKILL_KIND`（候选值 36）：测试记录的第二技能种类参数为 2，但当时提供的候选 schema 漏掉了现代导出中的 `SKILL_KIND.SORCERY=2`。补齐该测试值后，Windows 原 DLL 与重建 DLL 的 144 组合成查询全部成功且一致：20 组选择、124 组未选择，0 组拒绝；脚本现在核对完整计数。前一次的 12/60/72 结果仍记录在同一证据文件的 `priorPartialSchema` 中。见 `evidence/ios-package-search/order03-modern-enum-differential.json`。

这是对测试清单缺项的修正，并非对真实版本匹配或全部技能行为的证明；未修改产品源码，也未生成生产快照、构建 Unity/iOS 或测试真机。仍需完整注册枚举及真实主表版本关联、隔离服务实际脱敏契约。下一步继续核实真实数据到注册结构的映射和必要分支，不以局部差分替代顺序 03 完成条件。

## 2026-11-23：03 / 现代导出枚举局部差分

此前部分合成差分的枚举由测试顺序人为编号，不能用作真实客户端定义。本轮从 `recovered-unity/ExportedProject/Assets/Scripts/Assembly-CSharp` 的现代导出中读取所需候选常量，Windows x64 使用原 DLL 和当前重建 DLL 比较合成已注册卡牌的主卡组属性及技能种类查询：144 组状态和输出一致，其中 12 组选择、60 组未选择、72 组双方拒绝。拒绝结果不代表相关行为已支持。脚本与每个枚举来源哈希见 `scripts/test-order03-modern-enum-differential.mjs`、`evidence/ios-package-search/order03-modern-enum-differential.json`。

该测试未证明导出枚举与交付主表属于同一版本，也没有覆盖全部枚举、真实卡牌输入、完整战斗、Unity 或目标设备；候选数值尚不能直接用于生产 `CombatTables.json`。顺序 03 仍需完整枚举和表版本关联，以及服务端实际脱敏接口证据。无产品代码、Unity 构建或设备变更。

## 2026-11-23：03 / 旧版主表兼容性局部核对

对照 1.4.0 有方法体的 `SkillDataContainer.getRoleParam` 和 `Global.parseInt`，25 处服务端报告的 VALUE 异常值按旧版 `Int32.TryParse` / 默认 0 规则均得到报告的 0：INT 5、MND 14、DARK 2、5000000000 4。Windows .NET 使用旧版实际 `CSV.cs`，对 36 条短 AI 行逐字段比较原文与补空读取，206 个缺失尾字段均一致；测试依赖用隔离桩，见 `tests/Order03LegacyCompatibility.cs` 与 `evidence/ios-package-search/order03-legacy-compatibility.json`。

这只证明所列旧版数值及 CSV 字段读取行为，不证明主表源自 1.4.0、现代客户端采用相同行为、完整 AI 触发或数值正确。现代枚举与 ABI、服务端实际脱敏接口和最终真实快照仍待核实；03 保持进行中。无产品源码、Unity 构建、IPA 或真机变更。下一步继续依据实际原始客户端及 DLL 证据核对注册枚举和对应转换，不能拿测试枚举交付。

## 2026-11-23：03 / 服务端真实主表交付及本机复核

用户提供 `Order03-Combat-Verified-20261123.zip`，SHA-256 `a44390313bb4a0d13897b70f35c313bafd380282189b44689e8a48340c6621ce`。仅解压到 `inputs/server-order03/Order03-Combat-Verified-20261123`；58 个包清单条目及 29 张表哈希全部一致，29 张表有效 UTF-8 无 BOM。交付说明称表属于同一已安装资源清单，原始客户端版本和导出过程未知；不把目录名当作版本证明。

本机核对 164 个服务端源码文件哈希一致后，重跑交付的 Go 加载及离线引用审计：446,725 条明确引用缺失 0、3,102 条静态关卡敌人链缺失 0；25 处原始数值参数、36 条需补空 AI 行、34 个静态关联 boss 与交付报告一致。引用闭合不证明原版行为、开放状态或设备可用。`evidence/ios-package-search/order03-server-delivery-validation.json` 记录来源、范围和可复现产物。

在 `test-output` 用原表和明确的虚构占位枚举构造 57,706,143 字节临时测试数据；实际 C# 传输校验、Windows 现有重建 DLL 的目录及准备上下文打开通过。该枚举不可用于注册、战斗或发布；没有验证原 DLL、Unity、ARM64、iOS 包或设备。服务端答复是源码推导契约而非现服脱敏请求响应，未执行任何在线业务接口。

下一步核实原生注册枚举版本和语义，调查 25 处参数与 36 条 AI 行，并取得隔离服务实际脱敏契约证据；符合要求后才能制作真实客户端快照。顺序 03、RESOURCE-01 和 BATTLE-01 保持未完成，不进行真实入场、续关或结算。

## 2026-11-23：03 / RESOURCE-01、BATTLE-01：主表盘点，等待服务端

用户本次明确要求将顺序 03 设为正式目标，优先于旧的不设目标指令。未发现 `ios-client/Assets/Recovery/Combat/CombatTables.json`；项目内发现的 20 张同名 CSV 由 `scripts/make-native-session-fixture.mjs` 明确生成，属于合成夹具。文件名检索不能排除其他名称、压缩包或远端数据。真实来源、版本、哈希、关联完整性与枚举仍无法核验。

独立服务 `/healthz` 返回 HTTP 200 / PASS；已知 iOS 内容 ID `55c68a8971aafc9a137f79f16d5c2795bfb37c89b7f64bdca1542fc0eb1fd2bb` 的公开清单当前 HTTP 200 且仅列 `recovery/ui.dat`。这不证明服务器内部无主表，也不确认目前登录返回该清单。历史 Windows 协议探针读过角色、关卡及伙伴，未入场，不能代替服务端答复或真机战斗。证据 `evidence/ios-package-search/order03-master-inventory.json`，可转交请求 `顺序03服务端交付请求.md`。等待服务端提供真实数据和脱敏接口证据，03 保持未完成。

等待期间增量更新 `evidence/feature-coverage.md` 的主流程版本和证据，顺序 04 仍未完成。无源码、Unity 构建、IPA 或真机变化。下一项：继续顺序 04 未核实的功能范围，同时收到服务端交付后先验证哈希、版本与关联，再推进真实战斗。

## 2026-11-23：02 / RESOURCE-01：资源切换按记录范围完成

0.0.8 云构建及 IPA 核验通过；用户确认同身份覆盖未删除、资源安装 0/复用 1、重启登录和原有测试角色正常。用户提供的设备日志记录内置内容 ID `2d60141ec1a984788232a415fe6a1df63a9dfebadb574c3ed871f70420337495` 和服务器内容 ID `55c68a8971aafc9a137f79f16d5c2795bfb37c89b7f64bdca1542fc0eb1fd2bb`，各为安装 0/复用 1；日志行本身不标识应用版本，版本依用户确认。证据 `evidence/ios-package-search/device-client008-content-log.json`。

隔离故障夹具在手机上触发 Unity 资源加载失败，用户确认完整提示无 `rollback failed`，随后切回独立服务、重启登录及角色状态正常。结合构建源码推断回滚未报告失败，但没有直接检查重载后的 AssetBundle；临时规则和夹具均已删除/停止。实际加载器的缺失、截断、哈希错误、重试及回滚测试在 Windows 模拟网络/Unity 下通过。逐项证据与限制见 `evidence/ios-package-search/resource-switch-acceptance-008.json`。**顺序 02 按上述证据范围完成**；完整 iOS 资源、真实游戏、其他生命周期及更广 `RESOURCE-01` / `SAVE-01` 尚未完成。

## 2026-11-23：02 / RESOURCE-01：实际加载器下载错误路径

使用两份已核验的真实 iOS 资源及实际 `RecoveryContentLoader`、清单与缓存代码，Windows .NET 模拟网络和 Unity 的测试覆盖下载缺失、截断、哈希错误、三次失败保持旧版本及损坏后重试成功；并复测加载失败回滚与旧缓存损坏。七个场景通过，证据 `evidence/ios-package-search/content-loader-download-tests.json`。该测试不能代替 UnityWebRequest 或设备网络证据。

## 2026-11-23：02 / RESOURCE-01：0.0.8 设备故障注入

用户同意后临时添加仅限项目 Node、专用网络、TCP 27108 和热点网关 `172.20.10.1` 的入站规则；手机 Safari 访问隔离夹具 PASS。用户在 0.0.8 中连接夹具并登录后，反馈界面出现资源包不能加载的错误；切回独立测试服务后反馈“正常”。夹具使用合成登录和故意无效的校验完整资源，没有访问真实账号；测试后夹具进程和临时防火墙规则均已核实删除。证据 `evidence/ios-package-search/device-client008-failure-test.json`。

用户确认测试前应用先显示本地资源就绪、未关闭应用即进行故障测试，错误完整显示且没有 `rollback failed`；切回服务后资源为新安装 0、缓存复用 1，故障后关闭重开并重新登录，原有测试角色正常。结合 0.0.8 源码在回滚失败时会追加错误，可推断本次回滚未报告失败；没有设备日志、截图或对重载 AssetBundle 的直接检查，证据范围以此为限。顺序 02 继续审核错误路径。

## 2026-11-23：02 / RESOURCE-01：0.0.8 设备缓存计数反馈

针对覆盖安装 0.0.8、重启前后本地和服务器资源检查的请求，用户反馈“都显示新安装0，缓存复用1”，并确认界面显示 0.0.8、使用相同签名身份和应用标识直接覆盖且没有删除、关闭应用重开并重新登录后原有测试角色状态正常；记录 `evidence/ios-package-search/device-client008-cache-report.json`。这是用户反馈，没有截图、签名包或日志。

## 2026-11-23：02 / RESOURCE-01：0.0.8 云构建成功，等待设备结果

运行 `45097069398` / 源码 `66a63fbf1f52f45d8eeefb16502ac7c133da961f` 已完成 ARM64、Unity、Xcode 和资源构建；28 个云端服务测试及子测试通过。下载的两份未签名 IPA 均为 26,126,717 字节，SHA-256 `0d3fa2a20df7c9e8f58400179348d6aea609cd6c7fd71e796b660ce41eade800`；包内为 0.0.8 / 8、`local.kairisei.recovery`，ATS 检查及实际 plist 哈希对照通过。证据 `evidence/ios-package-search/ipa-client008-delivery.json`、`ios-client008-artifact-verification.json`、`ios-client008-cloud-status.json`。包含资源回滚与新安装/复用计数，不包含随后添加的本地测试夹具。

已请求用户同身份覆盖安装并反馈版本、资源计数、登录和重启前后状态；目前没有 0.0.8 安装、真机运行或持久性结果。隔离故障夹具在当前手机热点下从手机不可达，未修改系统防火墙。顺序 02 仍未完成。

## 2026-11-23：02 设备夹具可达性

用户确认电脑通过目标 iPhone 热点联网；隔离夹具在电脑当前 WLAN 地址 `172.20.10.3:27108` 本机 `/healthz` 返回 PASS，但用户报告同一手机 Safari 无法打开。只读检查显示 WLAN 为专用网络、专用网络防火墙默认阻止入站，夹具使用的项目内 Node 可见允许规则仅匹配公用网络；这是可能原因，尚未验证。用户暂时不方便切换普通 Wi-Fi；夹具已停止，未修改防火墙或网络配置。地址会随热点变化而变化。0.0.8 构建 `45097069398` 仍在进行；本窗口负责下载与核验。

## 2026-11-23：02 / RESOURCE-01：设备加载失败测试夹具

新增 `scripts/resource-failure-fixture.mjs`：仅监听明确指定的 IPv4 地址，提供隔离用合成登录和一个元数据完整、但故意不是 Unity AssetBundle 的资源，不提供游戏接口或保存账号/状态。本地回环 HTTP 测试及实际 C# 清单解析和缓存校验通过，见 `evidence/ios-package-search/resource-failure-fixture-tests.json`。这是为手机触发加载失败与回滚准备的夹具；尚未验证 Unity 确实拒绝它、手机可访问或实际回滚，不是游戏服务。完成后切回独立游戏服务验证重试。

## 2026-11-23：02 / RESOURCE-01：真实缓存与清单的本地回滚检查

实际 `RecoveryContentLoader`、`ContentCache` 和清单解析代码使用两份先前核验过的 iOS 资源：0.0.6 内置包及独立服务包，验证模拟加载失败时恢复旧版本、旧缓存损坏时拒绝恢复且新缓存保持完整、随后重试成功。三项 Windows .NET 检查通过，证据 `evidence/ios-package-search/content-loader-real-cache-tests.json`。AssetBundle 加载及网络仍为模拟，不能证明 Unity 或 iPhone 行为；此测试在正在运行的 0.0.8 源码提交之后，不改变该构建的代码。

## 2026-11-23：02 / RESOURCE-01：0.0.8 云构建已启动

用户在自动审批拒绝后明确同意本次上传和构建。复核 257 个白名单文件及工作树后，源码 `66a63fbf1f52f45d8eeefb16502ac7c133da961f` 已推送，云构建 `45097069398` 已请求；最初状态 queued，尚无构建或 IPA 结果。请求见 `evidence/ios-package-search/ios-client008-cloud-request.json`。本窗口负责监听、下载和核验，不要重复触发或同时下载。旧审批阻塞由本次授权解除，记录保留。

## 2026-11-23：02 / RESOURCE-01：0.0.8 云构建请求被自动审批拒绝

已在隔离仓库准备并审查源码提交 `66a63fbf1f52f45d8eeefb16502ac7c133da961f`，含 0.0.8 / build 8、资源回滚及设备可见的安装/复用计数；257 个白名单文件已生成哈希清单。请求推送至私有仓库 `Mradong/KairiseiMA-ios` 并启动构建时，自动审批拒绝了整个操作，理由是认为用户尚未明确授权本次源码外传；命令未启动，没有推送或新构建。记录 `evidence/ios-package-search/ios-client008-cloud-blocker.json`。此前项目授权记录未被该自动审查看作本次授权。需要用户明确批准此次具体源码上传与构建后再继续；不要绕过拒绝。

本地六项模拟加载器测试和 31 项合成 IPA 检查通过，未产生 0.0.8 IPA，顺序 02 的真实 Unity、iOS 和设备验收尚未完成；0.0.7 保持不变。

## 2026-11-23：02 / RESOURCE-01：0.0.8 构建准备

准备将资源加载失败回滚纳入独立的 0.0.8 / build 8；初始化和服务器更新提示增加本轮新安装及缓存复用计数，便于在设备上区分重启后的行为。资源加载器六项 Windows 模拟测试、31 项合成 IPA 结构检查和 0.0.8 脚本语法检查通过；不代表 Unity、IPA 或真机已通过。真实 Unity 对同名资源包卸载/重载及加载失败回滚仍需验证，详细设备失败注入方式尚待准备。0.0.7 包和结果保持原版本范围。

## 2026-11-23：02 / RESOURCE-01：资源加载失败回滚（本地）

资源切换记录上次成功清单，加载新包失败时卸载已加载集合并核验旧缓存、按依赖顺序尝试重载；回滚失败则清空部分集合，保留原错误及回滚错误。即使恢复旧包也保持 `Ready=false`，避免调用方误报服务器资源更新成功。实际加载器在 Windows .NET 模拟 Unity、网络和缓存的六个场景通过，覆盖后续下载失败、成功切换、加载失败回滚、回滚失败、重试和旧缓存损坏；见 `evidence/ios-package-search/content-loader-rollback-tests.json`。

尚未验证真实 Unity 对同名 AssetBundle 的卸载/重载、iOS、真机或持久性；该改动不在 0.0.7。此 Windows 环境未找到项目指定的 Unity 6000.3.26f1，IPA 的 Xcode 打包需要 Mac 云构建；后续需在协调其他窗口后准备独立版本、审查白名单并进行 Unity/iOS 和设备验证。完整游戏与真实战斗主表仍未完成。

## 2026-11-23：01 / CLIENT-01：0.0.7 手机联调反馈

用户明确反馈 0.0.7 手机连通验证通过，并确认计划第 01 项的连接检查、游客登录、服务器资源更新及角色状态检查**全部通过**。这是用户口述的真机结果，关联已交付构建 `45093283023` / `3f42572b4861539baa1ef4f85733b11d3014f640`；本次未独立读取签名后的 IPA、设备日志或截图，用户也未逐项重述设备/系统、屏幕版本、同一签名身份及覆盖方式。证据 `evidence/ios-package-search/device-client007-connectivity.json`。下方旧的“尚无 0.0.7 手机结果”均为历史快照。

这些反馈不证明资源或存档在重启/覆盖后持久化，也不代表真实战斗或完整游戏通过。下一步记录计划 01 的签名身份、覆盖方式和具体结果等尚未单独采集的细节，并推进计划 02 的资源加载失败回滚；0.0.7 之后的本地改动不在该设备包中。

## 2026-11-23：跨窗口详细执行计划

已新增 `详细执行计划.md`，按 0.0.7 手机联调、资源切换、真实战斗主表、原行为范围、登录主页、卡组关卡、真实战斗、结算、持久性、其余功能及最终 IPA 明确依赖与验收证据。新窗口先核对最新状态，再选择计划中最早未完成且依赖满足的任务；阻塞时按计划推进独立主流程工作，协调多窗口避免覆盖或重复构建。此项是计划与交接整理，不代表新增功能或真机通过。

## 2026-11-23：0.0.7 云构建与 IPA；本地资源切换改进

构建 `45093283023` / 源码 `3f42572b4861539baa1ef4f85733b11d3014f640` 成功；Unity、Xcode 和编译资源的云端服务验证通过。两份未签名 IPA 均为 26,126,010 字节，SHA-256 `07e7a667ca58f142d02be8b922d6701f9d374f8bf7859931e6a74cf059152aa8`。实际 IPA 内 plist 与云端已验证 plist 哈希相同；版本 0.0.7 / 7，ATS 为 `NSAllowsArbitraryLoads=true` 且无三个覆盖键。证据 `evidence/ios-package-search/ipa-client007-delivery.json`、`ios-client007-artifact-verification.json` 和 `ios-client007-cloud-status.json`。签名、安装、手机网络、游客登录、资源和角色状态均待用户验证；步骤见 `安装到iPhone.md`。真实战斗表未包含，完整游戏未完成。

独立的后续本地 RESOURCE-01 改动将所有资源下载及缓存校验提前到替换已加载包之前；Windows 模拟 Unity 的实际加载器测试覆盖后续下载失败保留旧包及完整缓存成功切换。证据 `evidence/ios-package-search/content-loader-staging-tests.json`。Unity AssetBundle 加载阶段失败仍可能留下部分切换，尚无回滚；没有 Unity/iOS/真机验证，该改动不在 0.0.7 构建中。

历史快照：0.0.7 云构建曾处于进行中；该描述已由上方成功结果更新。请求与状态见 `evidence/ios-package-search/ios-client007-cloud-request.json` 和 `ios-client007-cloud-status.json`。

## 2026-11-23：定位并修正 iOS ATS 配置冲突（源码，待构建和真机验证）

用户提供的 0.0.6 设备日志显示应用 CFNetwork 报 NSURLErrorDomain -1022：App Transport Security 拒绝了不安全连接。0.0.6 构建产物 plist 同时包含 NSAllowsArbitraryLoads 和 NSAllowsLocalNetworking；Apple 文档说明后者的存在使前者在 iOS 10 及以上被忽略，因此此前将该 plist 判断为允许公网 HTTP 是错误的。0.0.7 源码移除三个会覆盖全局例外的键，实际构建产物检查加入防回归；31 项本地结构/策略检查通过，0.0.6 实际 plist 被正确拒绝。证据 `evidence/ios-package-search/device-ats-diagnosis.json`。手机签名安装后的 plist 未读取；新源码尚未完成 iOS 构建或真机确认，登录、资源更新与完整玩法仍待验证。

## 2026-11-23：版本号在当前界面保持可见（本地）

将上轮的 `Application.version` 显示移到当前恢复界面的右下方，并在卡组、关卡及奖励弹窗绘制之后显示，避免弹窗遮挡版本号。版本来自应用构建配置，不手填；尚未执行 Unity/iOS 构建或设备验证，不属于已交付的 0.0.6。用户反馈的 0.0.6 即时 `HTTP 0` 仍未定位，Safari 可访问及构建清单允许 HTTP 不能单独证明根因；等待设备日志。

## 2026-11-23：客户端显示版本及设备连接反馈

用户确认手机安装的是 0.0.6：应用界面打开后检查连接立即显示 `HTTP 0: Unknown Error`；同一台手机 Safari 访问相同服务 `/healthz` 可见 PASS，应用无线数据允许 WLAN 与蜂窝数据。这些事实缩小了排查范围，尚不足以断定具体根因；0.0.6 自身的 iOS 网络配置允许 HTTP。手机登录和 UI 资源更新尚未通过。

按用户要求，后续客户端主界面标题从 Unity `Application.version` 显示构建版本号，避免手工版本与包版本不一致。此代码目前仅在本地，尚未 Unity/iOS 构建或手机验证，不属于 0.0.6。证据 `evidence/ios-package-search/device-http0-report.json`。

隔离新测试角色还通过公网 C# 读取单人关卡（3788 项）和第一个关卡的 5 名候选队友，未扣点入场；首次探针因自身 1 MiB 读取上限失败，使用与客户端相同的 4 MiB 上限后通过。证据 `evidence/ios-package-search/ios-live-quest-limit-failure.json`、`ios-live-quest-flow.json`。这是工作站协议验证，非设备证据。

## 2026-11-23：设备截图显示连接失败；隔离角色服务联调

用户截图显示恢复验证界面已打开、服务器地址输入正确，点击连接后显示 `HTTP 0: Unknown Error`；截图没有版本号或设备信息，暂不能归属 0.0.6，也不能判断 HTTP 0 的原因。服务端同期从工作站访问 `/healthz` 为 HTTP 200/PASS；0.0.6 构建产物的 Info.plist 显示允许 HTTP。两项检查不能证明手机可连接或排除运行时问题。已请用户用同一 iPhone 的 Safari 打开健康地址并确认安装包版本，等待反馈。

并行独立测试账号的实际 C# 公网流程成功创建角色、读取主页、解析 10 张初始卡和 4 套卡组，重新登录后角色仍存在。`HomeShow` 可能领取该测试角色的登录奖励；未修改卡组或进入战斗。证据 `evidence/ios-package-search/ios-live-character-flow.json`。这些是 Windows 网络/协议验证，不证明手机或完整游戏通过。

## 2026-11-23：RESOURCE-01 两个实际 iOS 资源版本的缓存切换

使用 0.0.6 构建 `44704875254` 的实际内置 UI 包及独立测试服务实际下载的 0.0.4 UI 包，运行本版 C# 清单和缓存代码：10 项检查通过。两份不同版本可共存；服务器包下载截断或内容损坏时，已验证的内置资源和已有服务器资源保持可用；重新创建缓存对象后两份仍通过哈希与 CRC 检查。证据 `evidence/ios-package-search/ios-real-resource-transition-tests.json`。

测试为 Windows .NET 本地文件和真实资源字节，不涉及 UnityWebRequest、Unity AssetBundle 加载、iPhone 文件系统、覆盖安装或画面。本次未修改 0.0.6 源码或重新构建 IPA；真实设备验证仍待解决 Sideloadly 问题后进行。原文件和只读参考保护通过。

## 2026-11-23：设备联调停在 Sideloadly 签名组件

用户截图提示 Fake2 Anisette 崩溃后被禁用、iTunes Anisette 与 Remote 不可用，当前旁加载未完成；没有 iPhone 游戏运行失败证据，截图也不标明 IPA 版本。只读检查确认 iTunes/iCloud 已安装及两个 Apple 服务正在运行，但不能据此证明 Anisette 可加载。未修改 Windows 配置或账号缓存。

已在 `安装到iPhone.md` 补充先在 Settings 重新启用 Fake2、重开工具重试一次的步骤；若再次崩溃，需要设置页和脱敏错误上下文定位，尚无恢复成功证据。同时将通用安装步骤中遗留的 0.0.3 文件名改为当前已核验的 0.0.6。证据 `evidence/ios-package-search/sideloadly-anisette-error.json`，完整目标仍未完成。

## 2026-11-23：0.0.6 交付复核与公网服务复验

两份 0.0.6 IPA 的 SHA-256 仍为 `5c5b51dbe12842cf33d223a6f8de073066aeb6d469bbce320db3b021f8058249`，对应构建 `44704875254`、源码 `692ef39`，未重新构建或替换安装包。独立服务在三天后的复验中健康、登录、重复身份、Connect、资源 HEAD/GET 和缓存重开均通过；仍提供 0.0.4 的部分 UI 内容 `55c68a...`。当前 C# 协议源码哈希与构建源码一致，复验记录 `evidence/ios-package-search/ios-live-service-test.json` 已脱敏。

已补齐云构建、安装包及资源验证记录和 0.0.6 覆盖安装步骤。尚无本版签名安装、iPhone 登录、UI 加载/绘制、重启及覆盖持久性的证据；也没有完整战斗主表或真实游戏闭环。原文件与只读参考保护通过。正式目标保持未完成，下一项设备操作见 `安装到iPhone.md`。

## 2026-11-20：0.0.6 云构建成功与联调 IPA 交付

源码 `692ef3939dfeb0278f43d8bffee2871dfbdd0dd6` 已推送并核对远端，224 个白名单文件的已提交内容与清单哈希一致。构建 [44704875254](https://github.com/Mradong/KairiseiMA-ios/actions/runs/44704875254) 已成功完成 ARM64、Unity、Xcode 和资源包服务验证，28 个云端服务测试及子测试通过。已下载并核验 `install-kit/KairiseiMA-ios-resource-bootstrap-0.0.6-unsigned.ipa` 与 `dist/ready/` 副本：26,126,147 字节，SHA-256 `5c5b51dbe12842cf33d223a6f8de073066aeb6d469bbce320db3b021f8058249`，两份一致。

此版包含 UUID 格式修复、服务器更新后重新加载标题 UI，以及此前已提交的主表入口、伙伴和球等增量。内嵌部分 UI 资源 ID 为 `e86930101893f0a77a8df57a14361c2f438675c05522f328fe669d4938b2f1f7`，当前公网服务仍提供 0.0.4 的 `55c68a...`；客户端应按登录返回的清单更新。没有真实战斗主表，`combatTablesIncluded=false`。证据 `evidence/ios-package-search/ipa-client006-delivery.json`、`ios-client006-cloud-review.json`、`ios-client006-artifact-verification.json`。

尚无本版签名安装及 iPhone 登录/资源更新/画面证据；不能用 0.0.3 或其他旧 IPA 的真机结果认证本版。设备操作见 `公网联调结果.md` 和 `安装到iPhone.md`。完整游戏目标保持未完成，下方“0.0.6 准备”是构建前快照。

## 2026-11-20：CLIENT-01 / RESOURCE-01 公网登录与资源联调、0.0.6 准备

用户确认独立服务 `http://47.116.51.61:27040` 已启动后，以独立测试身份联调。发现实际客户端使用无连字符 UUID，服务端要求标准带连字符格式，旧请求返回 400；修复 GUID 规范化及本地保存，保留原 GUID。实际 C# 网络复测的健康检查、登录、重复登录、Connect 均为 HTTP 200，同身份与状态保持一致，127 项协议回归通过。未创建角色、领奖、编辑卡组或入场。

0.0.4 部分 UI 资源的公网 HEAD/GET、清单平台/Unity/内容 ID、长度/SHA-256/CRC 和实际 C# 缓存重新读取通过。静态导入公网包无错误，解析六张图片、四份图集并通过 10 项下载数据的 C# 图集检查；尚非 Unity 或手机加载证据。运行时已修复更新后未重新加载标题贴图的问题，拟纳入 0.0.6（build 6）云构建。详情 `公网联调结果.md`；证据 `evidence/ios-package-search/ios-live-service-test.json`、`ios-live-service-uuid-format-failure.json`、`ios-live-ui-inspection.json`。尚无新构建或真机结果。

更正上轮认证判断：默认 GitHub CLI 配置不可用，项目专用 `GH_CONFIG_DIR` 的认证实际可用；先前推送失败没有完成认证，未造成远端更新。已恢复使用项目脚本和该配置，无须用户重新授权。

## 2026-11-20：BATTLE-01 注册普通球与圣杯球（本地）

中断前的球注册增量已接通三个技能块、次数、类型和可用条件，并由实际 C# 适配器消费。五个 Windows 合成场景与原 DLL 的完整结果/日志一致：三个普通球位置以执行等级 3 造成 11 伤害；TURN_AFTER 从首回合不可用转为次回合可用；圣杯球预约后造成 11 自身伤害。次数从 2 减至 1，普通下一波重置为 2；各场景均以实际伤害完成双波胜利。全部托管结果/日志比较 518 次，既有状态差分及 Go 回归通过。

球追加/支援技能的实际触发、EVOLUTION 及其他规则仍未验证或拒绝。重建 DLL SHA-256 `b2bd51120e4bb970648ab93e09cfce0c02b5000cff9ede4481b2a4e6b83edddd`；本地测试不代表公网玩法或当前源码真机通过。证据 `evidence/native/registered-battle-differential-tests.json` 及 `registered-sphere-checkpoint.json`。原文件及只读参考保护通过。

## 2026-11-20：BATTLE-01 注册伙伴与实际爆发技能（本地）

解除注册战斗对所有非零伙伴的拒绝，解码 17096 字节伙伴记录中的三组主动／追加技能、被动技能及费用、冷却、解锁等级，传入实际战斗和下一波。保留每个技能的执行等级，克隆主表后覆盖注册定义；缺少主表、非法费用、冲突定义或同一实例内同技能编号对应不同执行等级时拒绝，避免误用共享数据。球仍要求空记录，伙伴追加技能的实际触发尚未验证。

六个新增 Windows 合成伙伴场景经实际 C# `NativeRegisteredBattle` 与原 DLL 比较：五个装备位置均出现在启动结果中；主伙伴三项技能分别造成 13/14/15 伤害，并保留执行等级 2/3/4、费用 20/30/40 和冷却输出 3/4/5；被动技能在进入爆发时造成 3 点自身伤害。两个主伙伴场景均通过实际伤害赢下首波，并对照下一波和胜利，完整结果与日志一致。连同既有场景有 216 次托管结果/日志比较；原有 3633 项状态比较、12 个双波夹具和 Go 状态、独立战斗、准备态及注册卡牌查询回归通过。

证据 `evidence/native/registered-buddy-checkpoint.json`、`registered-battle-differential-tests.json` 及关联原始输出。重建 Windows DLL SHA-256 `18ba6c8a0a19138990061a0c03bddb35aba4abba040ace10ad73f28390e4ae8e`。真实伙伴主表及技能、非主伙伴被动、完整条件/效果、Unity/ARM64 和 iPhone 尚未验证，改动不在 0.0.5 中。原文件及只读参考保护通过。

上轮源码检查点 `19fbb59` 的推送因本机 GitHub 身份验证不可用而失败；未启动新云构建，代码继续保存在隔离仓库本地。服务端地址/报告由用户准备中，本轮没有再次请求或操作线上账号。

## 2026-11-20：RESOURCE-01 / BATTLE-01 主表资源到注册上下文（本地）

新增可选 `CombatTables.json` 真实主表快照约定和传输验证器；iOS 资源构建器在快照存在时编译独立战斗表包，并将其纳入清单、catalog 和服务端交付配置。运行时可从当前已校验资源包加载快照并创建实际 `NativeRegisteredBattle`，原生目录负责解析 CSV；缺失时拒绝，不回退演示表。成功切换资源集时移除新清单不再包含的旧包，避免沿用旧战斗表。表名、上限及交付事项见 `战斗主表接入.md` 和 `服务端协作需求.md`。

41 项 Windows C# 传输检查通过；实际战斗适配器的原 DLL 差分仍通过 3633 次状态比较、12 个双波夹具及 3 个托管场景，新增错误卡牌 CSV 的原版/适配器拒绝检查。证据 `evidence/ios-package-search/ios-combat-table-tests.json`、`evidence/native/registered-battle-differential-tests.json`。

当前没有真实 `CombatTables.json`，测试仍使用合成主表；可选包的 Unity 构建、实际资源加载及清单切换尚未运行验证。开战按钮、实际技能/卡组/敌人注册和真实结算未接入；没有新 IPA 或真机证据，0.0.5 不包含此修改。原文件及只读参考保护通过。

## 2026-11-20：BATTLE-01 队友主卡记录打包入口（本地）

新增将单人入场响应的队友主卡实例字段与调用方明确提供的主表信息合成为 4960 字节注册记录的入口，保留两个 2436 字节技能块和完整 15 字段档案。缺失技能块、不完整档案或两个技能 ID 均为空时拒绝；真实稀有度、爱情比例、技能和档案仍必须来自匹配主表，当前没有该主表数据加载链。

120 项协议/夹具检查通过，覆盖卡牌编号、生命、等级、名声、技能块边界与档案字段。三张隔离服务卡牌实例在明确合成的技能和主表信息下完成原 DLL 与重建库注册；测试随即恢复合成卡牌再进行战斗，因此只证明记录构造与注册，不证明这三张真实卡牌的技能或玩法。证据 `evidence/protocol-tests.json`、`evidence/native/registered-service-card-registration.txt`。原文件及只读参考保护通过，无 Unity/iOS 或真机证据，改动不在 0.0.5 中。

## 2026-11-20：BATTLE-01 服务队友用户参数投影（本地）

参考 1.4.0 `BattleDefs.cnvPartnerDeckInfo2UserData` 与现代 `battle/UserData.cs` 的字段顺序，将单人入场响应中的队友生命、攻击、职业、外观、初始费用和手牌上限转换为 18 项注册用户参数；拒绝缺失外观或越界数据。实际 C# 战斗适配器可直接注册队友及入场随机种子。

隔离服务入场样例的三名队友经该路径注册，Windows 原 DLL 与重建库的用户参数输出和后续结果/日志一致；第四名成员、卡牌和敌人仍为合成夹具。115 项协议检查及注册战斗差分通过，证据 `evidence/native/registered-service-partners.txt`、`evidence/native/registered-battle-differential-tests.json` 和 `evidence/protocol-tests.json`。旧版参考不证明现代客户端各项语义全部一致；真实卡牌/技能/敌人、Unity/iOS 和真机尚未验证，此改动不在 0.0.5 中。

## 2026-11-20：0.0.5 云构建与 IPA 核验

构建 [44662701250](https://github.com/Mradong/KairiseiMA-ios/actions/runs/44662701250) 对应 `ec71223aa0d433edd098ea6a39141edfc263c203`，已完成 ARM64、Unity/Xcode 和资源构建，28 个云端服务测试及子测试通过。已下载并核验 `install-kit/KairiseiMA-ios-resource-bootstrap-0.0.5-unsigned.ipa` 和 `dist/ready/` 副本：26,101,565 字节，SHA-256 `5d64b80a605fb92fbaf7ff39bd4115915d1bc048c11bc3b19fd65a81b130fcb4`，两份一致；资源和 IPA 结构校验通过。证据 `evidence/ios-package-search/ipa-client005-delivery.json`、`ios-client005-artifact-verification.json`。

尚未签名、安装、真机或完整游戏验证；此构建不包含其后队友选择、我方卡组选择及战斗配置等源码。独立测试服务连接情况尚需确认，不能把旧 IPA 的真机结果用于本版。

## 2026-11-20：BATTLE-01 额外技能注册入口（本地）

实际 C# 注册战斗适配器新增额外技能记录入口，校验成员、卡槽、种类和记录长度。Windows 合成双波夹具为每名角色注册种类 1 的非零额外技能记录，原 DLL 与重建库均成功注册，启动、伤害与日志对照通过；无效技能种类被拒绝。证据 `evidence/native/registered-battle-differential-tests.json`。这不证明该额外技能在战斗中触发，也不代表实际资源转换或完整技能恢复；无 Unity/iOS 或真机验证，不在 0.0.5 源码中。

## 2026-11-20：BATTLE-01 入场回合上限传递（本地）

单人入场响应解析器验证并保存 `end_turn`，实际 C# 战斗适配器可将其传给注册战斗。隔离服务入场样例的 `end_turn=0` 经解析与适配器传递，配合合成角色、卡牌和敌人数据，与原 DLL 的启动、双波结果及日志一致；另有合成上限 20 的对照。112 项协议检查与注册战斗原 DLL 差分通过，证据 `evidence/protocol-tests.json`、`evidence/native/registered-battle-differential-tests.json` 和 `evidence/native/registered-managed-service-turn-start.txt`。

这只接通单个配置字段；真实角色、卡组、敌人、界面与完整战斗仍未接入。没有 Unity/iOS 或真机验证，改动晚于 0.0.5 云构建提交 `ec71223`，不在该包中。

## 2026-11-20：BATTLE-01 C# 战斗配置入口（本地）

实际客户端 `NativeRegisteredBattle` 增加续关开关、回合上限及玩家初始爆发值的注册入口，供后续服务端入场参数接入。Windows 合成注册战斗中，在原 DLL 和重建库都设置允许续关、上限 20、1 号玩家初始爆发 40，实际 C# 适配器与原版结果和日志一致，启动输出包含 `503,20`；续关与爆发的具体战斗效果没有单独验证。原 DLL 差分及原文件保护检查通过；证据 `evidence/native/registered-battle-differential-tests.json`、`evidence/native/registered-managed-configured-start.txt`。

这些入口尚未连接真实入场响应和战斗界面，未使用真实资源；无 Unity/iOS 或真机验证，不在正在运行的 0.0.5 构建 `ec71223` 中。

## 2026-11-20：PLAY-01 单人入场我方卡组选择（本地）

关卡界面可重新读取服务器卡组并切换我方入场卡组；协议层可将选定职业、卡组索引及队友草稿组合成入场请求。隔离服务的四个初始卡组用于核验可选卡组、选定职业和索引、重复卡组身份拒绝；含已有主页、关卡、队友及入场夹具共 110 项协议检查通过，证据 `evidence/protocol-tests.json`。实际界面未发起入场，服务器仍负责卡组及关卡条件校验。

未接入真实战斗、Unity/iOS 或真机验证；此更改晚于正在构建的 0.0.5 源码 `ec71223`，不在该版本中。原文件和只读参考检查通过。

## 2026-11-20：PLAY-01 单人队友草稿（本地）

单人关卡界面新增从服务器候选中浏览、加入或撤销最多三名队友的草稿；切换关卡清除选择。队友编号、职业和出租卡组索引可生成开战请求的 `partner_deck_selects`，重复候选及超出数量被拒绝；关卡条件和实际卡组合法性仍由服务端校验。Windows 下以隔离服务候选响应验证请求字段、清除和边界，102 项协议测试通过，证据 `evidence/protocol-tests.json`。

当前没有真正发起战斗或提交队伍，自己的卡组、关卡入场限制和完整战斗接入仍未完成。无 Unity/iOS 或真机验证；此改动晚于 0.0.5 云构建源码 `ec71223`，不在该构建中。

## 2026-11-20：0.0.5 下载重试修复（本地）

云构建运行时修正下载脚本：失败下载保留在独立临时目录，重试成功后才将完整目录提升为正式产物目录；IPA 校验每次使用独立解压目录。`scripts/Test-IosClient005Download.ps1` 模拟失败后重试通过，脚本语法检查、原文件校验及只读参考状态检查通过。此脚本修复在已触发的云提交之后，仅影响本机下载和验证，不改变正在运行的构建源码。

## 2026-11-20：0.0.5 云构建进行中

源码提交 `ec71223aa0d433edd098ea6a39141edfc263c203` 已推送至私有仓库并核对远端；153 个白名单文件及提交内容通过核查。云构建 [44662701250](https://github.com/Mradong/KairiseiMA-ios/actions/runs/44662701250) 的 ARM64 桥接编译和 Unity 导出步骤通过，最近查询为 `in_progress / Archive device build and package unsigned IPA`，尚无完整构建结果或新 IPA。可用 `scripts/Get-IosClient005CloudBuild.ps1 -RunId 44662701250` 查询并在成功后核验；不得将 0.0.4 或更早的真机结果用于此源码。

## 2026-11-20：0.0.5 云构建准备

准备把 0.0.4 之后的关卡、敌方注册与图集等本地增量纳入独立 0.0.5（build 5）云构建，保留同一应用标识和旧包。新增独立构建下载、IPA 校验脚本，PowerShell 语法检查及图集测试通过；当前仅准备源码，尚无云构建结果、可交付新 IPA 或本源码真机证据。独立测试服务端口此前未连通，未经确认不操作账号。

## 2026-11-19：0.0.4 iOS 资源包供服务端联调

用户反馈服务端已准备独立测试配置及端口 27040/27041；此信息来自用户转述，客户端侧尚未验证或连接。已将构建 44434628647 的 `ios-resource-delivery` 全部产物打包为 `dist/server-handoff-20261119/ios-resource-delivery-0.0.4-44434628647.zip`，685,851 字节，SHA-256 `3acfe313870241d10e5374577cd7bc07ab82413693d67189cbe1eeb190c0d307`；解压后重新校验平台、清单、catalog、包哈希、CRC、UnityFS 与内容 ID 均通过。只包含该构建声明的部分 UI 资源，不能当作完整游戏资源；服务器尚未接收、部署或联调，0.0.4 无新真机验证。

## 2026-11-19：RESOURCE-01 旧版图集帧读取（本地）

新增 TexturePacker 图集帧读取，按服务器返回的职业尝试在主页显示旧版 Party 图集中对应图标；服务器资源更新后重新读取图集。资源缺失、图片尺寸不符或图标无法解析时跳过图标并记录错误，不影响进入主页。四职业帧的原始图片尺寸、坐标、UV 换算及缺失、越界、旋转、裁切拒绝共 10 项 Windows .NET 检查通过，94 项协议回归通过；证据 `evidence/ios-package-search/ios-atlas-tests.json`。

未执行 Unity 资源导入、实际绘制、iOS 构建或真机验证；不能声称手机显示正确或原版主页已恢复。本增量不在 0.0.4 中。

## 2026-11-19：CLIENT-01 主页视觉数据及旧资源调查（本地）

客户端解析主页时保留服务器 `background`、用户 `navi_type` 及完整业务数据；字段缺失保留未知值，非法值拒绝。隔离服务主页响应中的背景 1、导航 0 经实际 C# 解析，94 项协议检查通过。该改动只保留数据，尚未加载或显示对应主页资源。

旧版 1.4.0 可读代码根据导航角色背景配置或主页返回的背景编号加载背景，失败后尝试默认 1000；6.0.2 随包 `patch.txt` 包含主页资源包条目，导出有导航角色表，但尚未找到该包本体，也未证明 6.0.2 同样执行 1.4.0 逻辑。证据 `evidence/ios-package-search/ios602-home-resource-audit.json`、`evidence/protocol-tests.json`，复核脚本 `scripts/audit-602-home.mjs`。无 Unity/iOS 或真机验证，不在 0.0.4 中。

## 2026-11-19：SCOPE-01 旧版场景依赖调查（静态）

对已导出的 6.0.2 Home、Deck、Battle、Result、TeamStOne、StageSt 六个场景扫描对象与 GUID。Home 有 21 个对象、27 个脚本组件，可将 `Instance` 关联到导出的 `Home.cs`，并定位动画、材质及 Live2D 网格；这 27 个组件在 1.4.0 导出中均有同路径脚本，但同名不证明版本兼容。六个场景导出的脚本组件均无自定义字段；Result 的 `Result_PvpCNRank` 有一个空脚本引用。静态 GUID 对应只证明文件可定位，不证明传递依赖、资产可加载或运行时可用。

可复现脚本 `scripts/audit-602-scenes.mjs`、检查 `scripts/test-602-scene-audit.mjs`、逐对象证据 `evidence/ios-package-search/ios602-scene-audit.json`。下一步核对 1.4.0 Home 行为与 6.0.2 元数据、资源加载路径和服务器协议，再评估可迁移的场景组件；不直接用旧脚本替代 6.0.2 未知行为。没有 Unity、iOS 或真机验证。

## 2026-11-19：服务端协作事项

用户提供服务地址 `47.116.51.61:26040`，尚未验证其在线状态、部署版本、iOS 配置或协议。已整理 `服务端协作需求.md`，覆盖旧版兼容性、隔离环境、iOS 登录与资源、角色卡组、单人战斗及完整范围验收，供用户转交服务端 Codex；未访问或修改该服务器与真实账号。

## 2026-11-19：恢复路线调整

按用户要求重新规划：优先评估 6.0.2 已导出场景、组件字段、资源和可验证逻辑，比较现有服务协议后选择直接适配或复用素材并重建缺失行为。完整功能、iPhone 13 / iOS 17.4.1 真实流程、持久性与可安装 IPA 的验收标准不变。阶段、决策证据及首项场景调查见 `EXECUTION_PLAN.md` 首节；本条仅调整计划，不代表新增功能、构建或真机通过。

## 2026-11-19：PLAY-01 关卡队友查询（本地）

关卡清单可选择上一关、下一关，并向 `/TeamBattleSoloPartnerShow` 查询和显示该关卡的候选队友及职业、等级；切换关卡会清除旧结果。隔离服务业务处理器生成队友响应，经本机 HTTP 交给实际 C# 协议代码解析；88 项协议检查及服务队友测试通过，证据 `evidence/ios-package-search/ios-partner-flow-test.log`、`ios-partner-isolated-fixture.json` 和 `evidence/protocol-tests.json`。

此界面尚不支持选定队友或开始战斗。Unity 符号编译检查因现有 `code-dump/DummyDll/UnityEngine.CoreModule.dll` 元数据不可读而失败，见 `evidence/ios-package-search/ios-runtime-api-check.json`；没有 Unity 编辑器、iOS 或真机验证，不在 0.0.4 IPA 中。

## 2026-11-19：PLAY-01 单人战斗入场协议（本地）

客户端协议代码增加单人战斗入场请求构造和响应解析，保留波次、随机种子、开战时间及完整队友卡组等原始字段，供后续真实战斗注册使用。隔离服务业务处理器测试实际接受该请求、扣除战斗点并核对重试沿用同一战斗；C# 生成的请求与测试接受的请求逐字段一致，并解析同次响应。85 项协议检查通过，证据 `evidence/ios-package-search/ios-solo-start-test.log`、`ios-solo-start-fixture.json`、`ios-solo-start-request.json` 和 `evidence/protocol-tests.json`。

目前协议方法尚未接入 Unity 界面或战斗适配器；还缺实际关卡资源、卡组到注册数据转换、战斗交互及结算。没有 Unity/iOS 构建、真机或真实服务验证；此增量不在 0.0.4 IPA 中。

## 2026-11-19：BATTLE-01 敌方普通技能槽 1–14（本地）

敌方普通技能槽 1 至 14 均可受限投影佣兵目标与空 AI 条件。Windows x64 合成注册战斗对每个槽分别运行：技能 2002 命中存活佣兵并造成 11 点伤害；原版与重建 DLL 的状态、完整结果和日志一致。槽 1 的测试同时注册普通攻击，但未证明两个动作都执行；其他目标、特殊槽、死亡槽和非空 AI 条件仍未验证或拒绝。

注册战斗差分共 3633 次状态比较、445 次推进和 475 次日志快照；Go 状态、独立战斗和准备态敌人查询回归通过。重建 DLL SHA-256 `cf9d11214cd9e16d1d58560566b452866d00be94f3fbcf173773f8965ec9d18a`，证据 `evidence/native/registered-battle-differential-tests.json` 和 `evidence/native/registered-enemy-skill-slots.txt`。此增量没有真实资源、ARM64 或真机验证，不在 0.0.4 IPA 中。

## 2026-11-19：BATTLE-01 敌方额外技能槽（本地）

受限地支持敌方技能槽 1 的佣兵目标，仍要求空 AI 条件。Windows x64 合成注册战斗同时注册普通槽 0 与技能槽 1；原 DLL 与重建库的状态、完整结果和日志逐字节一致，观察到技能 2002 对佣兵造成 11 点伤害。该夹具未证明两个动作都执行；其他技能槽、技能槽 1 的其他目标、非空 AI 条件及完整敌方程序仍未验证或拒绝。

注册战斗差分通过 2736 次状态比较、302 次推进及 332 次日志快照，Go 状态、独立战斗和准备态敌人查询回归通过。重建 DLL SHA-256 `e3ff19ce77f786912214e607f247db54aa9e2cfdc57f327fd5f73034395e61b5`；证据 `evidence/native/registered-battle-differential-tests.json`、`evidence/native/registered-enemy-slot1-action.txt`。没有真实资源、ARM64 或本源码真机验证，本增量不在 0.0.4 中。

## 2026-11-19：BATTLE-01 敌方指定职业攻击（本地）

在受限的敌方普通槽中增加佣兵、富豪、盗贼和歌姬目标选择。Windows x64 合成注册战斗与原 DLL 对照，在四名角色均存活时四种选择均命中对应职业并造成 7 点伤害，完整结果与输入日志一致；原有随机玩家、自身目标及命中率回归通过。死亡角色回退、其他技能槽、目标与非空 AI 触发仍未验证或明确拒绝。

重建 DLL SHA-256 `1580d4ec22cfdccb757930b43fb69dbb76b2573080411e33f08adf698b309029`；差分和状态/独立战斗/敌人查询回归证据见 `evidence/native/registered-battle-differential-tests.json` 与 `evidence/native/registered-enemy-role-attacks.txt`。无真实资源、ARM64 或真机验证，本增量不在 0.0.4 中。

## 2026-11-19：BATTLE-01 敌方普通攻击玩家（本地）

在先前受限敌方普通槽支持上，增加 `RANDOM` 目标；中立属性目标掩码经原 DLL 对照后可命中玩家。Windows x64 合成注册战斗中，原版与重建库都随机选中玩家并造成 7 点伤害，结果 CSV 与输入日志一致；本轮另保留敌人自伤和命中率为零的对照。普通槽以外的非空技能、其他目标和非空 AI 触发仍受限；不能据此认定完整敌方 AI 已恢复。

注册战斗差分及 Go 状态、独立战斗、准备态敌人查询回归通过。Windows DLL SHA-256 `3022733bf34eb3cec998b165f42bb01bbce5ead1e66a035b04fc28da0c38171d`，证据 `evidence/native/registered-battle-differential-tests.json`、`evidence/native/registered-enemy-user-attack.txt`。本增量没有真实资源、ARM64 或手机证据，不在 0.0.4 中。

## 2026-11-19：0.0.4 构建交付与敌方普通技能增量

构建 [44434628647](https://github.com/Mradong/KairiseiMA-ios/actions/runs/44434628647) 已成功，源码提交 `090f19003b9bc2ecdc8b024c67024d9398ffc8f0`；ARM64、Unity/Xcode、iOS 资源构建及云端 28 个测试/子测试通过。已下载并核验 [0.0.4 未签名 IPA](install-kit/KairiseiMA-ios-resource-bootstrap-0.0.4-unsigned.ipa)，26,088,293 字节，SHA-256 `c4e3e39967c3281035d8ea587579425ccab24f77430d45a5a7f15c27805ed463`，安装包两份一致。证据 `evidence/ios-package-search/ipa-client004-delivery.json`、`ios-client004-artifact-verification.json`。尚未签名、安装或真机验证，不能当作完整游戏；该构建不包含之后的关卡列表及下述敌方技能修改。

本地 BATTLE-01 新增受限的非空敌方普通技能：只支持普通槽 0、空 AI 条件和 ENEMY1 目标。Windows 合成数据原 DLL 差分中，命中率 100 时敌人对自身造成 7 点伤害，命中率 0 时无该伤害；结果与日志一致。未知槽位、目标和触发条件仍拒绝。重建 DLL SHA-256 `c99ed5edf428cdce990787788bbba316418f620a66ef9db04789e64ecc0bcd8d`，测试 `evidence/native/registered-battle-differential-tests.json`。这不证明敌人能攻击玩家或 AI 完整，尚无真实资源、ARM64 或本增量真机证据。

## 2026-11-19：BATTLE-01 敌方空技能槽（本地）

根据原 DLL 行为，注册敌人的技能 ID 为零时跳过空技能槽，不再因为空记录拒绝战斗；实际 C# `NativeRegisteredBattle` 适配类也增加敌方技能注册入口。Windows x64 原 DLL 差分注册全部 35 个普通／被动／增幅相关空槽，创建战斗并推进到敌方回合，结果与输入日志一致；含此前双波及 C# 消费者场景，共 2,182 次状态比较、214 次推进、244 次日志快照。Go 状态测试、独立战斗和敌人查询回归通过。重建 DLL SHA-256 `1990f4768faf6779e8c84578627834c12762d3a59494125cba44807c34554e2b`，证据 `evidence/native/registered-battle-differential-tests.json`。

此项仅验证技能 ID 为零的记录；非空敌方程序、AI、量表等仍未实现，不能代表敌方行动恢复。测试均为 Windows 合成数据；没有本源码的 ARM64 或真机证据。运行中的 0.0.4 构建 `44434628647` 对应更早的 `090f190`，不包含本增量。下一步恢复非空敌方技能与真实战斗注册数据。

## 2026-11-19：PLAY-01 单人关卡清单（本地）

客户端按 `/HomeShow` 的当前职业请求 `/TeamBattleSoloShow`，保留普通、活动、钥匙及特别活动的分类，显示服务器返回的关卡名称、难度、战斗点消耗与锁定状态，并保存关卡原始字段供后续战斗入口使用。当前不能启动关卡，炼金塔扩展、原版场景和实际战斗未接入；调用服务端清单接口会清理服务端待处理的关卡选择状态，不扣除战斗点。

隔离服务教程游客返回的关卡清单通过本机 HTTP 交给实际 C# 代码解析；84 项协议和夹具检查通过，证据 `evidence/ios-package-search/ios-quest-list-test.log`、`evidence/ios-package-search/ios-quest-isolated-fixture.json` 和 `evidence/protocol-tests.json`。这批改动晚于 0.0.4 构建提交 `090f190`，不在构建 44434628647 中，尚无 Unity/iOS 或设备验证。下一步根据真实战斗开始响应和表资源恢复战斗输入，不以关卡列表代替玩法。

## 2026-11-19：0.0.4 云构建进行中

源码提交 `090f19003b9bc2ecdc8b024c67024d9398ffc8f0` 已推送至私有仓库 `main` 并核对远端；118 个白名单文件及清单哈希通过核查。构建 [44434628647](https://github.com/Mradong/KairiseiMA-ios/actions/runs/44434628647) 已启动，最新查询为 `in_progress / Run actions/setup-go@v6`，尚无结果或新 IPA。记录 `evidence/ios-package-search/ios-client004-cloud-request.json` 与 `ios-client004-cloud-status.json`。可用 `scripts/Get-IosClient004CloudBuild.ps1 -RunId 44434628647` 查询；成功后脚本下载并核验独立 0.0.4 包，失败时保存日志。设备结果仍未取得，旧 0.0.3 证据不可用于本源码。

## 2026-11-19：0.0.4 云构建准备（执行中）

本地提交 `0434575` 至 `30b4856` 包含注册双波和游客角色、主页、卡组读写增量，均未包含在 0.0.3。为验证新 Unity 代码，将版本设为 0.0.4（build 4），保留应用标识；修正云工作流将固定参考服务源码复制进已有测试目录时的嵌套路径问题。准备独立的 `Get-IosClient004CloudBuild.ps1` 与 `Verify-IosClient004IPA.ps1`，使用独立产物及证据文件，不覆盖 0.0.3。脚本已通过 PowerShell 语法检查；本条不代表云构建、设备运行或完整游戏通过。

## 2026-11-19：CLIENT-01 主卡组编辑与保存（本地）

卡组数据视图新增主卡槽选择、从已持有卡牌中选取或清空、草稿修改及 `/CardDeckSet` 保存；保存沿用服务器返回的职业、队长、支援、球、伙伴和其余字段。未保存草稿不能直接切换卡组，可放弃返回；请求发出后强制重新读取，避免网络结果不明时继续使用旧快照。服务端负责校验卡组合法性。当前界面仍是临时数据视图，仅编辑主卡槽；没有卡牌名称/插画、支援编辑、队长编辑、完整原版卡组 UX 或战斗接入。

独立服务临时游客实测交换主卡槽、保存并重新读取；非法卡牌提交被拒绝且已保存卡组未改变。实际 C# 请求体与该服务测试请求逐字段一致，80 项协议及夹具检查通过。证据 `evidence/ios-package-search/ios-deck-save-test.log`、`evidence/ios-package-search/ios-deck-request-fixture.json` 和 `evidence/protocol-tests.json`。没有 Unity 引擎、iOS 构建、真机、真实资源或断线恢复验证；不能据此宣称完整卡组或游戏可玩。

## 2026-11-19：CLIENT-01 服务器卡组读取（本地）

已创建角色在读取主页后可请求 `/CardShow2`，客户端保存卡牌和卡组原始字段，保留 64 位唯一编号及主卡、支援卡、球、伙伴槽位，并可浏览各卡组和已返回卡牌等级。卡牌名称、插画、编辑保存、教程流程以及真实战斗输入仍未接入；画面是临时数据视图，不是原版卡组场景。

隔离服务的新游客实际返回 10 张初始卡和 4 个卡组；其响应经本机 HTTP 转发后由实际 C# 协议代码解析。77 项协议和夹具检查通过，含大于 32 位的唯一编号、缺失/错误槽位及原始字段保留；服务测试日志 `evidence/ios-package-search/ios-card-flow-test.log`，响应为 `evidence/ios-package-search/ios-cards-isolated-fixture.json`，协议结果 `evidence/protocol-tests.json`。测试身份、服务和资源均为隔离夹具；尚未验证 Unity 显示、iOS 构建和真机，也未验证实际完整资源与卡组编辑。继续恢复卡牌资源、编辑和战斗数据链。

## 2026-11-19：CLIENT-01 登录奖励通知（本地）

主页响应中的每日、新手和累计登录奖励现可提取并显示；新手从连续日程中选择当前天，累计只显示首条实际领取，避免把其余预览当成已获得。未确认提示以服务器地址与用户编号派生的键保存在 `PlayerPrefs`，直到用户确认；这是当前简化提示，不是原版奖励弹窗或物品图鉴。服务端提交后网络中断、解析错误或应用在本地保存前退出仍可能丢失展示，事件奖励、其他弹窗及奖励名称/图标未恢复。

Windows 下 72 项 C# 协议/夹具检查通过，其中解析独立服务主页响应以及真实 Go 奖励投影函数生成的合成领取数据；奖励投影测试见 `evidence/ios-package-search/ios-bonus-projection-test.log`，协议证据 `evidence/protocol-tests.json`。独立服务游客真实主页夹具仍处于教程中，实际没有登录奖励，故本轮没有端到端真实领奖证据。`PlayerPrefs`、Unity UI 和重启保留没有运行验证；未进行 iOS 编译或真机测试。继续恢复教程、奖励及主页导航。

## 2026-11-19：CLIENT-01 主页角色数据（本地）

已有角色可手动请求 `/HomeShow`，客户端校验返回的用户编号，并显示服务器提供的名称、等级、体力、战斗点和金币。服务端进入主页会结束未完成的单人战斗，也可能领取登录奖励；当前界面会在进入前说明，尚未恢复奖励展示、弹窗、原主页场景与导航，不能当作完整主页。

隔离服务真实路由使用临时游客和 Windows 平台资源夹具，完成角色创建、重新登录和主页数据读取；实际响应交给客户端 C# 解析，全部 68 项协议与夹具检查通过。证据 `evidence/protocol-tests.json`、`evidence/ios-package-search/ios-character-home-test.log`，测试 `server-ios/server/internal/cnbootstrap/ios_character_flow_test.go`。本轮没有 Unity/iOS 编译、真机或实际 iOS 服务验证；旧 IPA 的真机结果不适用于本源码。继续恢复主页奖励和导航、教程与卡组。

## 2026-11-19：CLIENT-01 隔离游客角色创建（本地）

连接后若服务器表示角色尚未创建，客户端允许输入名称、选择佣兵／富豪／盗贼／歌姬，并发送原协议 `/UserCreate`；解析服务器返回的角色身份和职业。请求发出后清除本地创建许可，超时或结果不明时须重新检查 `/Connect`，避免直接重复创建。已有角色不会显示创建入口。

Windows 下 63 项客户端协议与本机合成 HTTP 夹具检查通过。独立服务副本使用临时数据库和独立游客，通过实际路由验证新角色状态、创建结果、重复创建拒绝及同游客重新登录后状态；服务测试使用 Windows 平台资源夹具，并非 iOS 联机验收。日志 `evidence/ios-package-search/ios-character-flow-test.log`，协议证据 `evidence/protocol-tests.json`，服务测试 `server-ios/server/internal/cnbootstrap/ios_character_flow_test.go`。未进行 Unity/iOS 构建、设备测试或真实账号操作；原版角色创建场景、教程和主页尚未恢复，本增量不代表可玩。下一步恢复实际主页数据及其交互，并处理进入主页会结束未完成战斗的服务语义。

## 2026-11-19：CLIENT-01 连接状态解析（本地）

客户端此前将 `/Connect` 响应当作角色初始化数据；根据 `server-ios/server/internal/httpapi/session_handlers.go` 和旧版 `GameMgr.cs`，该响应实际包含角色是否创建和教程标志。现已增加严格的状态解析，界面区分已有角色与未创建角色，并在字段缺失或无效时报告错误，不自动访问会改变战斗状态的 `/HomeShow`。Windows 下实际 C# 协议代码及本机 HTTP 夹具通过 54 项检查，证据 `evidence/protocol-tests.json`。这是连接状态的一小步，尚未实现角色创建、主页、真实服务联调或 Unity/iOS 编译；0.0.3 真机结果不适用于此源码。下一步恢复角色创建及主页数据与交互，继续保持隔离测试身份。

## 最新交付：0.0.3 iOS 资源启动版（2026-11-18）

构建 [44293950030](https://github.com/Mradong/KairiseiMA-ios/actions/runs/44293950030) 已成功，源提交 `32f0ece04bb20a0291374ff6aa7aa7f7de6f82ba`。用户于北京时间 19:55 执行监控脚本完成下载与核验，代理随后复核本地证据、两份 IPA 哈希和云端服务测试日志。ARM64 原生库、Unity iOS 资源编译/导出、Xcode 打包及实际编译资源的服务端交付测试均通过。

- 最新安装包：`install-kit/KairiseiMA-ios-resource-bootstrap-0.0.3-unsigned.ipa`（同包另存 `dist/ready/`），26,040,111 字节，SHA-256 `83e65a639ad4dc62c6dcb703a585810e926a9c0ed0cbbe51ebb8ae5fd09ce223`；57 个 ZIP 条目、应用标识 `local.kairisei.recovery`、最低 iOS 15.0。保持旧 0.0.2 与其证据。
- 实际 iOS 资源包 `recovery/ui.dat` 为 713,900 字节，SHA-256 `672c14931393b0f9c92a0eebb1bd83a462f936ab7a75155e829c34bd89ba6d30`，文件 CRC `A52C46F3`；包含从原 IPA 选定的十个纹理/图集数据源，字体已通过 Unity 简体字形检查。内容 ID `35887f789f6dfd29667dacdfaec643a635b392d9cd165baef013a3c18f1a8bea`。
- 云端 27 个测试及子测试通过，其中 `TestIOSCompiledProfile` 对真正编译的 iOS UnityFS 包核验 SHA-256 并通过服务 HTTP 路由交付完全一致的字节；此证据不等于手机联网流程或真实服务部署通过。
- 证据：`ios-resource-cloud-status.json`、`ios-resource-artifact-verification.json`、`ipa-resource-delivery.json`、`cloud-resource-review.json`，均位于 `evidence/ios-package-search/`。

**0.0.3 真机反馈：**用户在本版交付后提供截图，应用已打开，当前可见简体中文正常、未见方框，离线合成验证显示 30 步与原版一致并到胜利结算；背景可见。版本按交付后的反馈顺序关联，截图本身没有版本标签，未核验签名后 IPA 哈希。证据 `evidence/ios-package-search/device-resource-validation.json`。资源就绪提示已被合成结果覆盖，尚未捕获缓存安装/复用计数；退出重进、覆盖持久性、服务器登录及完整游戏仍未验收。0.0.3 不包含本地提交 `0434575` 的注册双波修复，不能用本次设备结果认证那批源码。下文 queued/未收到真机反馈等描述属于此前历史。

## 本地继续：CLIENT-01 场景脚本字段恢复（2026-11-18，执行中）

用户要求继续本地 iOS 恢复，不再停留审批后台排查。当前没有本机监控脚本保存的新构建结果；云端状态仍只有此前 queued 证据。本轮复用已有 IL2CPP 工具，尝试从 6.0.2 IPA 的原始主程序与元数据恢复该版本脚本字段，供原场景组件数据读取与主页/卡组 UI 恢复使用。只做静态处理，输出限本项目；不执行旧应用，不修改原 IPA，不将存根方法当作真实游戏实现。若因主程序加密无法恢复，则记录具体边界并推进可用的场景/资源消费者。

静态字段恢复尝试已结束：工具明确报告 Mach-O 加密，退出码 1，类型映射和 DummyDll 生成失败；虽然产生 `dump.cs` 与空 DummyDll 目录，输出含错误映射（如类型基类被错误映射为 string），不可使用。证据 `evidence/ios-package-search/ios602-symbol-recovery.json` 与 `logs/ios602-symbol-recovery.log`；输入主程序副本哈希检查一致。未进行主程序解密或旧应用运行。

转入 BATTLE-01：工作区原已有未通过验证的下一波草稿（未包含在已推送 32f0ece 内），先保留原 DLL、测试源码与失败记录至 `evidence/native/local-resume-20261118/`。复现原失败 `wave did not reach victory`，定位到合成卡牌没有目标属性掩码，原 DLL 同样不攻击；补上 FIRE 掩码后发现注册技能效果序号映射 `slot+1` 与原 DLL 零起始不一致，已改为 `slot`。当前正在重新编译和差分验证，不提前标记通过。

### 本轮已验证的注册连续战斗增量

- 修复 `native/bridge/registered_skill.go` 的效果序号映射，验证原有 `registered_next.go` 草稿能够在普通模式胜利后装载新的注册敌人并继续战斗。新 Windows DLL SHA-256：`64614cfd73b4c50e9c36d5acbac69319f94ee306b2738f9c76eda4339a11775e`。
- 新增 `tests/RegisteredWaveCases.cs`：12 个合成场景覆盖连续 1/3/5 个技能效果、进入下一波的 enemy_type 0/1/2/3、实际伤害、两波胜利、新敌人 HP、错误准备态/觉醒参数、未读结果优先级和短/空缓冲。2,087 次状态比较、204 次 Advance 辅助调用、234 次完整日志快照与原 DLL 一致。计数分别表示状态、调用与快照，不加总为独立游戏功能数量。
- 新增 Unity 工程运行时代码 `NativeRegisteredBattle.cs`，让调用方传入真实表与 packed 记录，持有并释放目录/准备态/战斗句柄，消费选卡/伤害结果和日志，支持注册下一波。该类不包含合成数据，尚未接入真实游戏 UI。新增 `RegisteredManagedCases.cs` 实际编译并调用此 C# 类，通过三次双波战斗与 108 次原版结果/日志比较；重复创建和释放未耗尽目录容量。证据为 `evidence/native/registered-battle-differential-tests.json`。
- 同一新 DLL 下，独立战斗 748 项、非零注册技能查询 155 项、Go 桥接状态测试通过。原始 DLL 只读哈希未变。**上述全部为 Windows / 合成数据证据；不覆盖 ARM64、Unity 引擎、真机或完整游戏 UI/资源。**
- 本轮源码尚未上传/云构建；已触发的 44293950030 对应旧提交 32f0ece，不包含这批修复，不能混用证据。BATTLE-01 继续 incomplete：其他模式、觉醒、NPC/球/伙伴、敌方程序/量表、完整条件/效果覆盖和真实数据仍未完成。下一步恢复真实注册输入与 UI 消费，或根据新云构建实际日志修复资源链。

本地审核快照已提交到隔离仓库：`04345758c3f170ee86bab4d4a599a2576fc5cc3a`，102 个白名单源码/证据文件、20,868,031 字节；未上传，未触发新构建。提交前逐文件哈希及清单与工作树范围核对通过，`git diff --cached --check` 通过。七个原始核心文件哈希一致、只读参考工作树干净，0.0.2 安装包哈希仍为 `dc83be178166a91b4923a7447cbcd77380dc2308f77329c0824a349213aa0b3e`。独立本轮证据为 `evidence/native/registered-wave-checkpoint.json`；旧 `implementation-status.json` 中 38/41 及 iOS 标志属于早期快照，不能用于当前源码判断。

**最新云构建：**用户已在本人 PowerShell 成功推送提交 `32f0ece04bb20a0291374ff6aa7aa7f7de6f82ba` 并启动 [44293950030](https://github.com/Mradong/KairiseiMA-ios/actions/runs/44293950030)。用户截图显示该提交处于 `queued`；`ios-resource-cloud-request.json` 记录请求。尚未确认 Unity/Xcode 或产物成功。代理尝试只读查询时自动审批服务仍返回 503，未执行状态查询；准备本人终端的监控/下载/核验脚本继续交付。下方“上传未执行”描述保留为此前审批失败的历史，现已由本人执行解除上传阻塞。

**后续监控与交付工具已准备：**`scripts/Get-IosResourceCloudBuild.ps1 -RunId 44293950030 -Wait` 可由本人终端执行，等待同一提交、失败时保存日志、成功时下载并调用 `verify-ios-resource-delivery.mjs` 与 `Verify-IosResourceIPA.ps1` 核验。新验证器与 0.0.3 使用独立输出和证据路径，原 0.0.2 不覆盖。脚本已完成语法检查，尚未对真实 0.0.3 产物执行；当前没有新 IPA。构建状态和实际核验结果将在本人执行后记录。

## 2026-11-18：RESOURCE-01 / CLIENT-01 结合旧 IPA 的恢复（执行中）

用户要求结合已提供的 6.0.2 IPA 继续构建，并及时同步文档。本轮先导入本地游戏 Data、恢复资源/场景清单并核验补丁与服务依赖，再接入可验证的客户端资源链；预期用户行为是后续客户端加载真实 iOS 资源并复用持久缓存。依赖是有效资源、与 Unity 6 的转换以及真正 iOS 服务配置。当前尚未产生新 IPA 或新真机结果；原包与 Windows 文件保持只读。

执行记录：新增 `scripts/Import-LegacyIos.ps1`，固定已核验 SHA-256，仅导入 Data 与 Info.plist 到项目内部，逐文件记录哈希；排除加密主程序、签名和 SDK 配置。已导入 129 个文件、237,631,011 字节至 `inputs/ios/cn602`；AssetRipper 导出至 `ios602-unity`，一个类型 129 的导入错误、零导出错误。得到 53 个场景、338 张纹理、6 个字体；2,223 个脚本均为存根，场景自定义 MonoBehaviour 字段未恢复，不能直接编译为游戏。补丁表有 707 个名称，尚未确定全部字段语义与缺失资源。参考服务端 `platform_delivery.go:112` 仅接受 Windows/Android，必须另行补 iOS 支持。

新增 `scripts/analyze-legacy-ios.mjs` 和 `evidence/ios-package-search/cn602-resource-analysis.json`，记录导出数量、兼容性与字体 cmap 覆盖。原包 `W3(P).ttf`、`W7(P).ttf`、`W9(P).ttf` 对当前代码使用的 194 个中日韩字符均无缺失；三个 FOT 字体缺失多种简体字。接下来接入可重复构建的 iOS 资源包与持久缓存；字体修复随该功能构建核验，尚无新真机结果。

### 本轮实现与本地验证

- `prepare-ios-content.mjs` 选取 11 个明确来源的 PNG、图集 JSON 和 `W3(P).ttf`（共 6,597,877 字节）用于工程；不导入旧版存根、序列化播放器或 SDK 配置。新增代码后重新核验字体，当前 196 个中日韩字符均覆盖。
- `RecoveryContentBuild.cs` 实现 `BuildTarget.iOS` 资源编译、ASTC 纹理设置、SHA-256/文件 CRC 清单及服务端 `ios-delivery.json` 生成；`RecoveryBuild` 计划版本为 0.0.3，应用标识继续 `local.kairisei.recovery`。当前尚未通过 Unity/Xcode 编译，不能写成已交付。
- `RecoveryContent.cs` / `RecoveryContentLoader.cs` 接入启动资源初始化、大小/SHA-256/CRC 验证、依赖排序、失败重试及 `persistentDataPath/content-v1` 缓存，成功后加载背景纹理；保留已有字体作为回退并优先使用原国服简体字字体。`ios-content-tests.json` 的 29 项 Windows .NET 检查通过，覆盖中断、同长度损坏、重复加载、重启、新旧内容共存和错误清单；不等于手机资源持久性通过。
- 根据用户授权，建立独立服务端副本 `server-ios/server`，只在副本增加 `iOS` 平台与 Windows 8.3 路径根目录规范化修复；`patches/ios-server.patch` 已通过对固定参考的 `git apply --check`。`ios-server-tests.json` 的 iOS 登录/资源 GET/HEAD/Range/ETag/错误版本/独立测试角色以及原 Windows/Android 相关回归通过（18 个测试及子测试通过）。原参考源码保持不变。
- 服务器真实可部署资源仍未齐全。本轮资源包只是启动与 UI 资源的一部分，不能拿它冒充原游戏完整 `main_c/*` 资源集；不会自动把它加进原 Windows 服务。正在准备独立服务端二进制与云端真实 iOS 资源交付校验。

本地提交 `443b6e573ebf46facc77d98c186d553e6b9c7675` 已完成，90 个白名单文件、20,793,098 字节。Windows/Linux amd64 服务端二进制均已编译，见 `evidence/ios-package-search/ios-server-build.json`。两次尝试上传时，自动审批服务均返回 HTTP 503（审批模型无可用通道），**推送未执行，云构建未启动**；属于审批基础设施失败，未认定操作不安全。继续完成不依赖云端的工作，并准备可在本人终端执行的明确构建步骤。

### 服务端到客户端的资源接入

后续增量已补上登录后的公共资源清单下载：服务端 `platform_manifest.go` 校验公共清单与平台注册版本、内容 ID、完整下载清单的大小/哈希/CRC 一致；`client-manifest.json` 支持 GET/HEAD。客户端以登录返回的内容 ID 验证清单，再按原 `patch/<bundle>.v<CRC>` 路由下载，限制响应大小、验证后提交缓存、重用已有版本。此前本地 iOS/Windows/Android 回归及新增公共清单错误路径测试均通过，现共 26 个测试及子测试通过；真实 Unity 编译资源的服务测试已接入云工作流，但尚未执行。

已补充 AssetBundle Unity 模块依赖与 IL2CPP 清单类型保留。C# 纯逻辑 29 项测试通过；尝试用现代 DummyDll 做额外 API 编译检查时，旧 .NET 编译器读取参考程序集元数据失败，**没有完成 Unity 协程/API 编译验证**，详见 `ios-runtime-api-check.json`。不能把纯逻辑测试移作 Unity/Xcode 证据。

部署与云构建步骤见 [iOS服务部署.md](iOS服务部署.md)；本地脚本 `Start-IosResourceCloudBuild.ps1` 已准备完成，供本人终端在审批基础设施不可用时执行。当前仍缺完整游戏资源、真实场景组件和登录后游戏 UI；本轮不会将局部资源配置部署到原服务。

2026-11-18 最新：用户已取消 GPT-6.1 Terra 执行限制，可由当前可用模型继续恢复。用户提供的国服 6.0.2 IPA 已在原路径只读静态检查：138,633,863 字节，SHA-256 `d86d7609d6235ae836d4721591358de496f7f679a6ef192aea7ab054949c930b`，确含 ARMv7/ARM64、Unity 5.3.5p1 场景与资源、IL2CPP v21 元数据。主程序两个架构均标记 `cryptid=1`。资源完整性、现有服务端兼容性、安装及真机运行仍未验证；尚未改变恢复路线。详情见 [旧版iOS包检索.md](旧版iOS包检索.md) 和 `evidence/ios-package-search/ipa-602-static-inspection.json`。

**新窗口续作入口：[新窗口接手.md](新窗口接手.md)**。原 Terra 执行代理已结束；按交接文件、本文最新记录及验收表续作，执行模型限制已取消。

2026-11-18（Asia/Shanghai）。目标是 iPhone 13 / iOS 17.4.1 私下安装 IPA，不上架 App Store。用户没有 Mac、付费 Apple 开发者账号或现成签名证书。本机与云端 Personal 激活均已确认；连接验证工程已通过实际 Unity/Xcode 构建。所有恢复开发仅限本文件夹。

**最新 0.0.2 内核验证 IPA 已生成，完整游戏仍未完成。** 文件在 `dist/ready/KairiseiMA-ios-native-validation-unsigned.ipa` 和 `install-kit/`，23,747,763 字节。SHA-256：`dc83be178166a91b4923a7447cbcd77380dc2308f77329c0824a349213aa0b3e`。实际 ARM64 库编译、Unity 导出、Xcode 链接和打包均已成功；云端／本地哈希、50 个 ZIP 条目、iOS 结构（最低 iOS 15.0、元数据 39）及实际框架中的 Go 模块标记均通过。用户最新截图已确认新版在 iPhone 13 / iOS 17.4.1 完成 30 步合成战斗并到胜利结算；截图同时暴露部分中文缺字。此项不代表完整游戏恢复，见 `evidence/native/device-validation.json`。

此前的 0.0.1 标题版仍保留在 `dist/ready/KairiseiMA-ios-connection-test-unsigned.ipa`，22,214,689 字节，SHA-256 为 `20917cca959643fc68e1ee66c6a62cf55861c6c5f896b56b5f801f43243f5bb7`。你已确认它在 iPhone 安装后能打开标题，此结果来自本人反馈，未采集手机登录资料或签名后的 IPA。见 `evidence/device-validation.json`。新版操作步骤见 [安装到iPhone.md](安装到iPhone.md)。

新增进展：

- 本轮新增 `evidence/feature-coverage.md`，列出主流程与卡牌、商店、社交、竞技、故事及平台依赖，区分旧版有方法体与现代存根；现代范围仍待核实，不标记完成。新增 `native/bridge/skill_wire.go`，将 2436 字节技能记录解析为有符号原始字段、条件参数和五组扩展/角色，供未来注册创建及分支查询转换使用；目前注册校验接入其 ID/等级，尚无 domain 枚举转换或注册战斗消费。此解析比已有 `skill_info` 的 250 项统计提供完整原始字段，但不能据此宣称技能扩展功能已恢复。
- 新构建 Windows DLL SHA-256 `5df881ef8f77866bf998771f1b4d593875d1544acb74f4bf79f842b71e5e71dc`；布局 Go 测试及状态测试通过，四套原 DLL 差分共 73,089 项通过，实际 Unity C# 适配器合成回放连续三次通过。注册差分只验证接口状态而非全部解码字段语义；字段布局测试是独立合成夹具，尚需用原 DLL 可观察行为验证 domain 转换。执行者自查见 `evidence/reviews/terra-checkpoint-001.json`。原始文件 7/7 一致，固定参考工作树干净；此版本尚未打包，无新 ARM64 或真机验证；原 0.0.2 证据属于此前构建。
- REA 6.9.0 / Ghidra 12.2 / JDK 25 已局部配置并实际运行。保留 Go 符号映射及 86 个主模块函数、73 个桥接模块函数的伪代码；缺失模块的原始源码仍未找到。
- `native/bridge` 已实现 38/41 个导出边界，包含参数、准备注册、战斗表加载、独立战斗创建／推进／结果／错误和观察接口。已跑通合成胜利流程；原客户端的完整注册数据解码、注册式创建／切换战斗及技能扩展查询仍未实现。非注册上下文按原版拒绝输入日志读取，尚无注册输入日志。接口数量不代表完整游戏恢复比例，能力位仍保持保守。
- 原生对照测试通过 21,340 项检查。新增技能查询 3,323 组：933 组成功、2,390 组拒绝，共比较 830,500 个输出槽，失败时检查输出缓冲保持不变。能力掩码仍只声明参数功能。
- 准备接口对照测试通过 15,497 项检查，含八种模式共 8,434 组技能查询、4,888 组用户参数 CSV、64 个句柄上限及错误输入优先级。没有创建战斗或读取真实账号；原 21,340 项检查仍通过。
- 新增注册接口对照测试通过 35,504 项检查（9,390 次成功、26,114 次拒绝），覆盖注册前置条件、成员与槽位边界、NPC 1,024 条和敌方反应 50 条上限、初始化表与错误优先级。另有九组 Go 状态测试，检查缓冲复制、拒绝时保留旧记录、召唤双记录、敌方替换／清空和剧情数值转换。三套原版对照共通过 72,341 项检查。
- 新增独立战斗对照 748 项：用 20 张合成表创建四人战斗，完成胜利结算；51 次推进调用共比较 28,726 字节结果／错误。覆盖表引用、两个目录／64 场战斗上限、参数覆盖、缓冲区重读和故障保留。四套原版对照累计 73,089 项通过，另有 Go 状态及合成战斗预热检查。Unity 的实际 C# 适配代码在 Windows 上连续三次通过 30 步原版参考哈希验证，没有使用真实账号或服务器。
- 新版 Unity 0.0.2 源码已接入“离线战斗内核验证”按钮，应用标识仍为 `local.kairisei.recovery`。源码共 32 个清单文件、14,013,063 字节（另有清单文件本身），上传提交为 `e4ea0a59441d663a083c63cd774817374d3ba6b0`。[构建 44253783860](https://github.com/Mradong/KairiseiMA-ios/actions/runs/44253783860)已完成 ARM64 静态库、Unity、Xcode 和 IPA 打包。静态库 9,802,360 字节，SHA-256 为 `b7a0037267863e719a3bba4d1e57a78d5f843569ed1c45e0f729353eeb146e88`；实际框架含 Go 1.25.1 及恢复模块标记。手机上的 30 步回放已由用户截图确认通过（仅合成数据）；字体缺字待修复。
- 结构检查器通过 23 项合成夹具检查，并已用于实际构建与下载后的 IPA 复核。
- 已按用户明确授权，通过官方便携 GitHub CLI 2.105.0 连接账号 `Mradong`，创建私有仓库 `Mradong/KairiseiMA-ios` 并完成源码上传。初次 16 个文件逐一核对 Git blob 哈希一致。初始源码审核 ZIP 保留在 `dist/cloud-source/`，不是 IPA。
- 已增加官方 Unity Licensing Client 云端激活脚本，源码共 15 个构建文件、13,945,923 字节，加 README 与清单共 17 个文件。Runner 为 `macos-15-intel`，Hub 3.22.6，Editor 6000.3.26f1；实际云端 Personal 激活及归还均成功。
- 首次构建 `44217212585` 于北京时间 2026-11-18 09:37 启动，约 10:05 成功完成。Unity 导出、Xcode 编译、IPA 打包及结构检查全部通过。下载后再次核对 SHA-256、50 个 ZIP 条目、主程序及 UnityFramework；执行文件哈希与云端报告一致。仅手动触发，未上架。

验证情况：

- `evidence/protocol-tests.json`：48 项协议检查通过，并通过仅限本机的 health/login/Connect HTTP 合约测试。
- `evidence/original-input-verification.json`：7 个原始核心文件 SHA-256 与首次记录一致。
- `evidence/source-repository.json`：独立参考仓库为提交 `d1a202eb539b99511e8a33f3975fac409ab545bf`，47 次提交、935 个跟踪文件，参考副本工作树未修改。
- PowerShell 脚本语法检查和 Node 脚本语法检查通过。
- `evidence/unity-build-preflight.json`：未在检查的位置找到 Unity 6000.3.26f1，构建预检明确失败，没有假报生成 Xcode 工程。
- REA 静态分析及 Ghidra 反编译记录见 `evidence/rea/NOTES.md` 与 `evidence/native/`。
- `evidence/native/parameter-differential-tests.json`：原生对照结果、DLL 哈希及 Windows 测试范围。
- `evidence/ci-source-preparation.json`：本地源码子集清单，非上传记录。
- `evidence/ipa-delivery.json`：实际 IPA 来源、哈希、本地复核及尚未签名／真机验证的边界。

当前状态：本人已完成签名侧载并确认标题打开。后续新版仍需沿用同一个 Apple 账号和应用标识覆盖安装；不要先删除 App。免费签名续期通常保留应用数据；完整资源下载与持久保存功能尚待恢复及续签验证。本机 17763 不在 Unity 6.3 官方支持范围内（最低 19043），实际编译已在云端完成，无需本机 Editor。本机许可证 XML 未读取或上传；当前连接无法查询 Actions 用量 API，未修改计费设置。

Hub 安装兼容性已定位：本机 Windows 10 Enterprise LTSC 2019 / 1809，内部版本 17763；新版 Hub MSIX 要求至少 19043，报错 `0x80073CFD`。官方 Hub 3.19.5 x64 EXE 的版本和签名 Valid 已核对；来源及哈希在 `tools/unity-hub/download.json`。用户已自行安装并激活 Personal。本机目标 Editor 未安装；云端连接工程构建已完成，完整游戏仍未完成。

后续仍需解决：旧代码到新版场景的移植、真正的 iOS 游戏资源与服务端配置、已注册原生数据到战斗内容的完整解码及三个剩余导出，以及网页／音频／Live2D／Shader。已交付的 0.0.1 IPA 只含标题与连接验证；本次构建的 0.0.2 仍是合成战斗验证工程，完整游戏玩法尚未恢复。ARM64 构建、链接与合成回放真机结果已分别记录；真实游戏流程尚待恢复和验收。

迁移说明：本次恢复工程与参考仓库均已放入本文件夹；仓库迁移前后 997 个文件（含 Git 元数据）逐个核对一致。旧临时位置 `../source-review` 留有受 Windows 访问限制的目录残留，未在该位置继续工作，也没有修改权限来强行清理。原有 Windows 游戏文件未参与移动。

安装准备：`install-kit/` 已集中 3 个官方来源安装包和 IPA，共 530,073,394 字节。iTunes 12.13.12.3 与 iCloud 7.21.0.23 的 Apple 签名有效，Sideloadly 官网包为 NotSigned；安装器未由代理执行。后续只读检查已确认本人安装了 iTunes、iCloud 与手机驱动；本人已确认 IPA 可打开标题。安装、Apple 账号、签名错误排查和七天续签保留数据的步骤已写入两份安装指南，来源和哈希在 `evidence/install-kit.json`。

历史会话的正式完整恢复目标已于 2026-11-18 建立并保持进行中；用户最新指示已取消指定执行模型限制。执行计划见 [EXECUTION_PLAN.md](EXECUTION_PLAN.md)，约束见 [AGENTS.md](AGENTS.md)，验收表见 `evidence/acceptance.json`。0.0.2 真机合成验证通过，完整游戏仍未完成。

## 2026-11-18：BATTLE-01 技能分支查询部分进展

`battlecore_skill_extend_index` 已接入活动独立战斗的受限查询，支持 NONE、NULL、RANDOM、SELF_HP_PER、SELF_OTHER_PLAY_NUM、SELF_PLAY_MOST_LOW_COST、SELF_PLAY_COST_TOTAL 和 TARGET_ATTR。TARGET_ATTR 的首参数按 ATTR 枚举转换，其余参数保留有符号 64 位数值；此前该条件被保守拒绝的问题已修复。其他尚未恢复的条件仍返回无效状态。

Windows x64 合成测试中，与原 DLL 的 450 组状态及索引差分通过，其中 169 组覆盖 TARGET_ATTR 的有效、缺失和越界枚举、两个条件槽及稀疏分支；65 组覆盖缓冲区、句柄优先级、空准备态成员及错误输出保持。查询没有目标对象，因此不能证明带目标时的属性匹配。证据见 `evidence/native/skill-query-differential-tests.json`；Go 桥接测试通过。

准备态已注册角色的卡牌及敌人投影、其他条件枚举转换、注册战斗创建和下一波及日志仍未完成，BATTLE-01 保持未完成。本地 Windows DLL SHA-256 为 `0cf96c78628b32a376b43ef8940f7c609cb70506d08240022dfc323254525c2d`。本次源码没有新的 ARM64 构建或真机验证，旧 0.0.2 IPA 的真机证据不适用于这些改动。

## 2026-11-18：BATTLE-01 准备态注册角色查询

已将准备态注册角色的基础字段接到 `NewPreparedSkillQuery`，支持无非零卡牌及无敌人的准备态查询；注册卡牌或敌人的完整投影仍未完成，遇到这些记录会明确拒绝。Windows x64 使用隔离合成数据，在八种模式下反复注册角色并改变生命值与条件范围，与原 DLL 对照的 2,800 组状态和分支索引一致，其中 880 组选择分支、1,920 组不选择。证据见 `evidence/native/prepared-skill-query-differential-tests.json`。活动战斗查询 450 组差分和 Go 状态测试在当前 DLL 下通过，七个原始核心文件哈希一致，参考工作树干净。

当前 Windows DLL SHA-256 为 `8998c195e3975f6e18b21c2de028c71a76ef1cae1ee346088f187c7bd5685fad`。此证据不覆盖注册卡牌、敌人、真实游戏数据、ARM64 或新真机运行。注册战斗创建、下一波、输入日志、其他条件和资源及客户端流程仍未完成；旧 IPA 的真机结果不适用于当前源码。

源码、测试和证据的隔离仓库提交为 `ae1ddf0f66418b98a188a3a0f87b21d34aa3e080`。首次推送遭自动审批拒绝后，用户明确授权该提交推送；现已推送至私有仓库 `main`，并通过 GitHub API 核实远端提交哈希一致。没有发起本轮云构建。

## 2026-11-18：BATTLE-01 注册卡牌标签查询

准备态查询现在可投影内嵌技能 ID 为零的注册卡牌资料标签，并支持主卡组、支援卡组及全部卡组的标签条件；非零技能和敌人投影仍会拒绝，不能用于宣称真实卡组已恢复。Windows x64 原 DLL 对照 312 组通过，覆盖主／支援槽、标签匹配和不匹配、缺失卡牌元数据、无效稀有度、错误技能等级以及重复卡牌一致性。证据见 `evidence/native/prepared-card-query-differential-tests.json`。当前 Windows DLL SHA-256 为 `af8f63f61a426d898a78c6ce0b2ac80292ea30e2b631cf6268706573e865417d`；准备态角色的 2,800 组、活动战斗的 450 组差分以及 Go 状态测试均在该 DLL 下通过。

本增量未做 ARM64 构建或真机验证；真实卡牌通常包含非零技能，后续必须恢复这些技能与敌人解码、注册战斗创建和 UI／资源链。旧 IPA 的设备证据不适用于当前源码。

本增量的代码、测试和证据已保存为隔离仓库提交 `792dc80`，并随之后的提交一同推送至已授权的私有仓库。

## 2026-11-18：BATTLE-01 非零注册技能查询

准备态注册卡牌现在解码非零技能头、受支持的条件、效果参数与共享效果集，并在独立内容副本中用于卡组属性和技能种类查询。Windows x64 使用合成的非零 `ATTACK_AA` 技能和效果，与原 DLL 比较了 155 组状态及分支结果，包含属性／种类变化、无效字段、重复技能和共享效果集冲突；证据见 `evidence/native/registered-skill-query-differential-tests.json`。此前注册卡牌标签 312 组、注册角色生命值 2,800 组、活动战斗查询 450 组及 Go 状态测试在同一新 DLL 上通过。

当前 Windows DLL SHA-256 为 `96d604aa4008d0bbdc5102e9b679aaec623413ce46867d7bb93fe77ba35bf4bf`。验证仅覆盖合成的单种效果及所列查询；其他效果和条件族、注册敌人、注册战斗创建／下一波和日志仍未完整恢复或验证。没有本版本 ARM64 构建、实际游戏或新真机证据；旧 IPA 设备证据不适用于当前源码。

本轮源码、测试和证据已提交并推送至私有仓库 `main`，提交 `8a93f7e0cc9c3aa517b9349b27fa9cc72aa32879`；GitHub API 确认远端分支指向该哈希。未发起本轮云构建。

## 2026-11-18：BATTLE-01 准备态注册敌人

准备态查询已接入没有敌方程序和特殊槽的注册敌人基础记录，包括种族、属性、生命值、基础参数和掉落字段；敌方程序与特殊槽记录仍明确拒绝。`ENEMY_TRIBAL` 查询与原 DLL 的 Windows x64 合成对照通过 1,856 组，覆盖敌方槽位、种族、生命值／倍率、多人模式下的错误、父子关系及错误输入。864 组单敌人普通模式查询中，216 组选择分支、648 组不选择；多人模式冲突及其余边界测试比较错误状态和输出保持。敌人生命值映射另有 Go 字段测试，但本项 DLL 差分的分支条件不观察生命值，因此不能据此证明原 DLL 对该字段的完整行为。证据见 `evidence/native/prepared-enemy-query-differential-tests.json`。

此前四套相关查询差分和 Go 状态测试在同一 DLL 下通过。当前 Windows DLL SHA-256 为 `c262fa19e1cec7b16a647c4b978ec982f911757b2400c3d23a5ab95b2a5a87de`。该增量不覆盖敌方技能、特殊槽、注册战斗创建或实际游戏内容，也没有 ARM64 或新真机验证；旧 IPA 证据不能用于本源码。七个原始核心文件的哈希检查通过，参考工作树干净。

本轮源码、测试和证据已推送至已授权私有仓库 `main`，提交 `2a67c30b98176066eca956c6a4eaab3678c6c88b`；GitHub API 核实远端哈希一致。没有发起本轮云构建。

## 2026-11-18：BATTLE-01 注册战斗创建与回合

已将准备态角色、卡牌、技能和基础敌人投影接入受限的普通模式注册战斗创建；启动时按原版输出卡牌身份。Windows x64 合成输入与原 DLL 对照完成创建、启动、发牌、四人选卡、攻击、敌方回合和恢复等 14 步，结果 CSV 逐字节一致；另验证注册敌人当前／最大生命按倍率进入启动结果。证据见 `evidence/native/registered-battle-differential-tests.json`。原生独立战斗 748 项回归和准备态查询回归通过。

当前 Windows DLL SHA-256 为 `b6285908ff269c5cd7eef9111494e4fec86987197243e3055a10091c864ebb64`。**注册输入日志尚未实现**：测试明确观察到原版读取成功而当前库返回无效状态；下一波、NPC、非空球与伙伴、敌方程序和量表及其他模式仍未实现或验证。本次仅为合成战斗内核证据，没有真实资源、游戏 UI、ARM64 或新真机验证；完整游戏和可玩 IPA 未完成。

本轮代码、测试和证据已推送至已授权私有仓库 `main`，提交 `6d04a8596ad353e8c9ea9bc07857821889d8327f`，并核实远端哈希。七个原始核心文件哈希一致、参考工作树干净；未发起云构建。

## 2026-11-18：BATTLE-01 普通模式输入日志

已恢复受限普通模式注册战斗的输入日志：记录操作前状态和选卡目标，接收洗牌顺序回调，仅在操作成功后提交，并处理短缓冲、重复读取和失败状态。Windows x64 隔离合成数据与原 DLL 对照，15 步战斗中的 18 次完整日志快照逐字节一致；包含多卡选择、重复提交、未读取结果时拒绝下一步、无效请求、短／空缓冲及失败后的日志读取。证据见 `evidence/native/registered-battle-differential-tests.json`。此前独立战斗 748 项、敌人查询 1,856 项、非零技能查询 155 项和 Go 状态测试在同一 DLL 下通过。

当前 Windows DLL SHA-256 为 `aa8a4493c1696ffb48c26f69989f120912b2ea81307b95e149030f681004f180`。本轮未验证 NPC、必杀、其他模式、实际资源、下一波或真实服务；没有 ARM64 或新真机结果。完整可玩和最终 IPA 均未完成。

本轮代码、测试和证据已推送至已授权私有仓库 `main`，提交 `a8c4868171df42f20306f19409aa1a427431ed55`；GitHub API 核实远端哈希。七个原始核心文件哈希一致、参考工作树干净；未发起云构建。
