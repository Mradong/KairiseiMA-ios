# 当前状态

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
