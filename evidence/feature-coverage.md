# 原工程功能和依赖覆盖（持续核实）

2026-11-23 回环补充：合成服务截断保存响应时，Windows HttpClient 实际执行三种提交/冲突场景并重读，恢复助手结果与预期一致，见 `evidence/ios-package-search/deck-save-recovery-transport.json`；不涵盖 UnityWebRequest、设备或真实服务竞态。

2026-11-23 卡组失败恢复：源码在保存结果未知后要求重新读取，以返回的主卡槽和库存判断已一致、恢复内存草稿或冲突；不自动重复写入。Windows .NET 协议/回环 111 项通过，见 `evidence/ios-package-search/deck-save-recovery-tests.json`。UnityWebRequest 实际故障、并发、设备及进程重启未验证；重新登录或退出会清除内存草稿。

2026-11-23 卡组增量：当前源码在保存前拒绝同一非零实例重复占据主卡组槽位，保留错误草稿；Windows .NET 协议及回环 106 项通过，见 `evidence/ios-package-search/deck-duplicate-validation-tests.json`。旧版 `DeckChecker` 的卡牌定义/家族检查和本地服务校验是参考；不同实例同家族、支援、活动规则、Unity 及设备尚未验证，不能视为完整卡组校验。

## 2026-11-23 顺序 04 增量：隔离卡组和关卡

| 功能 | 本次可用证据与版本边界 | 仍缺的依赖和下一步 |
| --- | --- | --- |
| 卡牌、卡组 | 隔离服务上新角色的两个自有非队长卡槽重排、读取、重新登录后读取通过；无效卡牌实例 HTTP 400 后卡槽未变，见 `evidence/ios-package-search/order03-isolated-deck-contract.json`。另一新角色的 10 个定义可在已交付表投影追到 20 个非零技能引用和 23 条效果，见 `evidence/ios-package-search/order03-starter-card-closure.json`。 | 仅 Windows 协议和所测隔离身份；服务进程重启、并发、完整错误路径、版本匹配和设备编辑未验证。依赖可信客户端枚举与设备 UI。 |
| 选关 | 新隔离角色列表的 3,788 个不同编号中，2,744 个在交付静态投影；其余 1,044 个中 1,043 个的编号和字段与本地自带卡组变体规则一致，见 `evidence/ios-package-search/order03-quest-variant-gap.json`。 | 列表可清除待处理选关状态；不证明线上部署规则、关卡开放或准入。依赖版本化发布配置和安全恢复契约，再验证设备 UI。 |
| 战斗与结算 | 29 张表的哈希与所检查的正 ID 引用已验证；候选快照通过 Windows 目录打开/关闭，但目标版本及枚举仍未知。`SELF_ENEMY_AI_VAR` 在旧版和现代候选间编号不同，见 `evidence/ios-package-search/order03-enum-version-differences.json`。 | 原生技能/敌人完整执行、真实入场、恢复和结算未验收；不得用候选枚举或合成查询替代目标版本及设备验证。 |

## 2026-11-23 顺序 04 增量：主流程证据更新

以下记录覆盖旧表中与现有证据冲突的历史状态；来源中的旧版为 1.4.0，6.0.2 的组件导出缺少业务字段，现代行为仍待核实。各项均未通过完整功能验收。

| 功能 | 已有证据及版本 | 依赖、未验证内容和下一步 |
| --- | --- | --- |
| 启动、资源更新 | 0.0.8 真机覆盖和重启后资源安装 0/复用 1 为用户反馈；故障回滚依据提示与源码推断，见 `evidence/ios-package-search/resource-switch-acceptance-008.json`。 | 仅部分 UI 资源；继续取得完整 iOS 资源、真实战斗表及实际 AssetBundle 回滚检查。 |
| 登录、主页 | 0.0.7 用户报告连接、游客登录、资源及角色状态通过，见 `evidence/ios-package-search/device-client007-connectivity.json`；Windows 独立服务探针读取主页，见 `evidence/ios-package-search/ios-live-quest-flow.json`。 | 手机上的完整身份、角色创建、主页交互、奖励和错误恢复未逐项验证；核实服务副作用并在同一设备版本验证。 |
| 卡组 | Windows 隔离服务探针读取角色卡牌和卡组；旧版 Deck/DeckChecker 的代码是行为线索。 | 没有设备上编辑、校验、保存及重读证据；依赖匹配的真实卡牌表和隔离角色，核对服务持久化。 |
| 选关和队友 | Windows 实际客户端协议探针解析 3788 条关卡及首关 5 名伙伴，未入场，见 `evidence/ios-package-search/ios-live-quest-flow.json`。 | 这不是设备 UI 或关卡可用性证明；需要核对权限、体力、角色卡组、伙伴和安全入场契约。 |
| 战斗及结算 | 注册战斗普通模式有 Windows 合成输入与原 DLL 差分，见 `evidence/native/registered-wave-checkpoint.json`；0.0.2 真机仅合成回放。 | 真实主表、完整注册输入、更多技能和敌方行为、界面、真实入场与结算均未验收；等待顺序 03 数据及契约。 |
| 持久性 | 0.0.8 同身份覆盖、重启后资源复用与原有测试角色正常由用户报告。 | 尚未验证真实卡组和完整服务状态、资源中断及所有生命周期；保留 SAVE-01 未完成。 |

顺序 04 其余功能族仍按下表待核实；本次仅依据已有记录增量更新，不宣称场景或现代功能范围完整。

2026-11-19 静态场景调查：`evidence/ios-package-search/ios602-scene-audit.json` 映射 6.0.2 六个主流程场景中的对象、脚本 GUID 和直接外部引用。Home 的 27 个组件均能找到导出脚本及 1.4.0 同路径代码，但这些导出组件均无业务字段；Result 有一个空脚本引用。可定位不等于资源完整或版本兼容，具体对象见记录，所有功能验收状态不变。

路径相对于 KairiseiMA-ios。旧版 `legacy-unity/ExportedProject` 为 1.4.0、Unity 5，含方法体；现代 `recovered-unity/ExportedProject` 是 AssetRipper 存根，`code-dump` 是现代符号及无方法体 DLL。旧版存在不证明新版行为相同。固定参考服务端提交 d1a202eb539b99511e8a33f3975fac409ab545bf。本表为待验证清单，不代表范围穷尽或功能已完成。

2026-11-18 更新：6.0.2 IPA 已导出至 `ios602-unity`，同样有 53 个场景、资源及存根，但自定义组件字段缺失；直接 IL2CPP 字段恢复因加密和错误类型映射失败，不使用 `ios602-code-dump`。已选择纹理/图集/简体字体接入真正 iOS 资源重编译及缓存源码（尚未核实云构建成功）。普通模式注册战斗新增下一波、两波胜利和实际 C# 适配器 Windows 差分证据；真实 UI 和真实注册输入仍缺，不能据此把下方战斗/场景功能标记完成。

| 功能和来源 | 依赖、当前状态与下一步 |
| --- | --- |
| 启动：旧版 `Assets/Scene/AppInit.unity`、`Title.unity`，`Assets/Scripts/Assembly-CSharp/Title.cs` 的 `PreStart`、`PostStart` | 0.0.1 仅标题可打开；`ios-client` 是验证壳。核实现代入口、初始化顺序及场景资源。 |
| 下载：`Title.cs` 的 `resourceRecoverRequest`、`BundleDownloader.cs`、`AssetBundleDef.cs`、`Scene/Download.unity` | 版本清单、资源、持久缓存和重试；真正 iOS 资源与服务配置未恢复。 |
| 登录：`Title.cs` 的 `OnStartLogin`、`UUIDLogin`、`StartConnect`、`onLogin` | LoginMgr、服务器列表、平台身份和服务协议；在隔离身份下恢复真实流程。 |
| 主页：`Scene/Home.unity`、`Home.cs` | 玩家数据、资源与 UI；核实现代导航及行为。 |
| 卡组：`Scene/Deck.unity`、`Deck.cs`、`DeckChecker.cs`、`battle/DeckData.cs` | 卡牌、球、伙伴和持久化；选择、校验、保存未恢复。 |
| 关卡：`Scene/StageSt.unity`、`StMainSl.unity`、`StSubSl.unity`，`battle/BattleSoloFunction.cs` | 任务、体力、敌人、服务与场景转移；真实数据未接入。 |
| 战斗：`Battle.cs` 的 `BattleMgr.battleStart`、`battle/BattlePluginMgr.cs`、`BattleDllImport.cs`、`BattleDirection*.cs` | `native/bridge` 已验证独立合成流程；注册解码、创建/下一波、技能扩展、日志和实际 UI 尚未完成。 |
| 结算：`Scene/Result.unity`、`battle/BattleResultCmdMgr.cs`、`BattleResultCmdSoloFunction.cs` | 胜败、奖励与服务状态、主页转场均待真实验证。 |
| 重启和覆盖安装 | 存储、缓存、身份与签名；目标机持久性尚未验证。 |

以上旧版源码及场景路径以前缀 `legacy-unity/ExportedProject/Assets/` 为准。主流程之外的原版场景也属于待核实范围：

| 功能族 | `legacy-unity/ExportedProject/Assets/Scene/` 场景证据 | 状态和依赖 |
| --- | --- | --- |
| 卡牌管理 | CardCl、CardDev、CardExchange、CardSell、Fusion、FusionEv、FusionRe、DeckRec | 未恢复；卡牌数据、服务和动画。 |
| 商店和抽卡 | Shop、Gacha、GachaRe、AvatarShop | 未恢复；货币和服务，使用隔离测试数据。 |
| 社交和组队 | Friend、ChatEdit、TeamRm、TeamStMake、TeamStOne、TeamStPastBoss、TeamStSearch、TeamStUnion | 未恢复；聊天、多人服务及同步。 |
| 竞技 | PvPStNPC、PvPUI、TeamRaceEntry、TeamRaceMatching | 未恢复；匹配、竞技战斗和服务。 |
| 故事、活动、探索 | Story、StoryMenu、Intro、IntroSlPl、EventPage、Explore、Advertise、Comeback | 未恢复；活动数据、剧情、资源和网页依赖待核实。 |
| 角色与奖励 | Avatar、AvatarResult、BuddyInfo、ItemInve、Mission、Present、ProfileBG、Navi | 未恢复；角色、物品、任务及奖励服务。 |

平台依赖：旧版 `Assets/Plugins/Live2DUnity.dll` 和 `Assets/Resources/shader/merged/Live2D*.shader` 存在；现代版本及 iOS 支持待核实。CRI 音频、WebView、Shader、字体、输入与生命周期均未验收；0.0.2 截图中有简体中文缺字。公开服务端资源配置仅见 Windows/Android，需独立实现真正 iOS 配置。现代功能全量清单、资源来源与中间件版本仍未知。

战斗实现参考 `source-review/kairisei-ma-ch/server/battlecore/skill_query.go` 的 `SkillBranchIndex`、`NewPreparedSkillQuery`；原 DLL 分支解码反编译在 `evidence/native/library-pseudocode/functions/0x28f565680.c`。目标机 0.0.2 的 30 步胜利仅合成验证，不是实际游戏闭环。
