# 原工程功能和依赖覆盖（持续核实）

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
