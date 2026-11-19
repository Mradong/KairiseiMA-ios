# iPhone 安装与免费 Apple ID 签名

## 最新构建：0.0.4（尚未真机验证）

新增 [0.0.4 未签名 IPA](install-kit/KairiseiMA-ios-resource-bootstrap-0.0.4-unsigned.ipa)，26,088,293 字节，SHA-256 `c4e3e39967c3281035d8ea587579425ccab24f77430d45a5a7f15c27805ed463`。构建 [44434628647](https://github.com/Mradong/KairiseiMA-ios/actions/runs/44434628647) 来自提交 `090f190`；Unity、ARM64、Xcode、iOS 资源和本地包校验已通过。它包含游客角色创建、主页数据和主卡组编辑的部分实现，仍不是完整游戏，也尚未验证手机安装或联机。它不包含稍后的关卡列表和敌方技能增量。

若要测试，沿用现有 Sideloadly 操作、同一 Apple 账号与应用标识覆盖安装，先不要删除手机上的旧应用。仅凭覆盖成功不能认定缓存或存档保留；实际结果需要单独记录。下文 0.0.3 的反馈只适用于 0.0.3。

已为本机 Windows 10 LTSC 2019（64 位）准备好安装包，适用手机为 iPhone 13 / iOS 17.4.1。文件最后核对日期：2026-11-18。

当前最新 IPA 为 **0.0.3 资源启动版**，新增从原 iOS 包恢复的资源、校验缓存与简体中文字体。云端 Unity/Xcode 构建、实际 iOS 资源服务测试及本地结构/哈希核验通过；用户截图已确认应用打开、可见简体字正常及合成战斗通过。完整游戏仍在恢复，0.0.2 的旧真机结果不能代替本版验证。

2026-11-18 最新反馈：用户安装本版后截图确认应用打开、可见简体字正常、30 步合成战斗验证到胜利结算。下方安装与检查步骤保留供续签/覆盖使用；退出重进、缓存复用、覆盖存档及服务器登录尚无证据。

## 已装好旧版：覆盖安装 0.0.3

1. 打开 Sideloadly，连接并解锁 iPhone。
2. 拖入 [KairiseiMA-ios-resource-bootstrap-0.0.3-unsigned.ipa](install-kit/KairiseiMA-ios-resource-bootstrap-0.0.3-unsigned.ipa)，沿用上次的 Apple 账号和应用标识，点 **Start** 覆盖安装。不要先删除手机上的 App。
3. 打开 **Kairisei Connection Test**，等待资源初始化，正常提示应包含“本地资源已就绪”。检查“资源”“验证”“战斗”等简体文字是否仍显示方框；保存出现提示后的截图。若显示资源加载失败，保留完整错误。
4. 从应用切换器退出后重开，确认仍能到资源就绪；服务器地址若以前保存过，应继续保留。仅界面再次出现不能单独证明缓存免重装，缓存 installed/reused 计数及资源/存档覆盖持久性后续另验。
5. 点击 **离线战斗内核验证**，正常仍为 30 步并到胜利结算。此项使用合成输入，不会显示完整游戏战斗画面，也不需要连接游戏服务器。将资源就绪/字体与合成战斗两类结果分别反馈。

0.0.3 为 26,040,111 字节，SHA-256：`83e65a639ad4dc62c6dcb703a585810e926a9c0ed0cbbe51ebb8ae5fd09ce223`。构建记录：[44293950030](https://github.com/Mradong/KairiseiMA-ios/actions/runs/44293950030)，提交 `32f0ece`。实际 iOS ARM64、Unity/Xcode 和资源清单/哈希/CRC 核验均通过。既有服务尚未部署完整 iOS 资源，暂不以点击游客登录作为本轮手机验证前提。完整界面和真实游戏战斗仍在恢复；本包不含稍后本地 `0434575` 的双波修复。

## 一、已下载的文件

所有文件集中在 `D:\KairiseiMA-Windows\KairiseiMA-ios\install-kit`。不需要再到网站下载：

| 顺序 | 文件 | 用途 |
| --- | --- | --- |
| 1 | [01-iTunes64Setup.exe](install-kit/01-iTunes64Setup.exe) | iTunes 12.13.12.3，含 Apple 手机驱动 |
| 2 | [02-iCloudSetup.exe](install-kit/02-iCloudSetup.exe) | iCloud 网页安装版 7.21.0.23，提供签名工具所需 Apple 组件 |
| 3 | [03-SideloadlySetup64.exe](install-kit/03-SideloadlySetup64.exe) | 使用免费 Apple 账号签名并安装 IPA |
| 4 | [KairiseiMA-ios-resource-bootstrap-0.0.3-unsigned.ipa](install-kit/KairiseiMA-ios-resource-bootstrap-0.0.3-unsigned.ipa) | 最新 0.0.3，含 iOS 资源启动/缓存与字体，约 26 MB |
| 保留 | [KairiseiMA-ios-native-validation-unsigned.ipa](install-kit/KairiseiMA-ios-native-validation-unsigned.ipa) | 0.0.2，已验证手机合成战斗，约 23.7 MB |
| 保留 | [KairiseiMA-ios-connection-test-unsigned.ipa](install-kit/KairiseiMA-ios-connection-test-unsigned.ipa) | 已验证标题可打开的旧 0.0.1，约 22 MB |

iTunes 与 iCloud 来自 Apple 官方下载地址，Apple 数字签名均有效。Sideloadly 来自其官网，Windows 数字签名状态为 `NotSigned`，无法通过 Windows 签名验证发布者；本任务没有运行这些安装器。下载地址、版本及 SHA-256 在本目录的 `*-download.json` 和 `下载核对.json` 中。

本机已安装 VC++ x64 运行库 14.44.35211，不需要重复安装。Unity Hub、Unity Editor、GitHub CLI 都不是这一步在手机上安装所必需的软件。

软件组合依据：[Sideloadly 官网的 Windows 安装要求](https://sideloadly.io/)。这里使用官网提供的网页安装版 iTunes/iCloud，未使用 Microsoft Store 版。

## 二、准备免费 Apple 账号

### 已经有 Apple ID

如果你平时已经用 Apple ID 下载 App Store 应用，可以使用这个现有账号。先确认自己知道登录账号、密码，并能收到双重认证验证码。

不需要为了此次安装退出 iPhone 上现有的 Apple 账号。Apple ID 现在称作“Apple 账户”，仍是同一个账号。

### 没有 Apple ID

1. 打开 [Apple 账号官网](https://account.apple.com/)，点击“创建你的 Apple 账户”。
2. 按页面填写姓名、生日、实际国家或地区、邮箱、密码和可以接收验证信息的手机号。
3. 完成邮箱和手机号验证。
4. 注册完成后，可以先用这个账号在 Sideloadly 中尝试签名，不必先完成开发者网站登记。

账号由你本人注册。密码、短信码和双重认证码都不要发到聊天。

来源：[Apple 官方创建账号步骤](https://support.apple.com/zh-cn/108647)。

## 三、是否需要先登记开发者

**不必把开发者网站的免费登记当成安装前必须手工完成的步骤。** Sideloadly 官网给出的流程是加载 IPA、输入免费或付费 Apple 账号、开始安装；可以先用现有免费账号尝试，不需要购买 Apple Developer Program。

1. 先按下面的步骤安装电脑软件、连接手机，再在 Sideloadly 中尝试签名。
2. Sideloadly 会使用该账号向 Apple 申请个人开发签名和描述文件，再签名 IPA。无需手工制作或购买 `.p12` 证书。
3. 如果签名时明确提示需要接受开发者协议或处理账号资格，再打开 [Apple Developer 账号页面](https://developer.apple.com/account/)，按实际提示处理。协议由你本人阅读并决定是否同意。
4. 开发者网站上的资格错误不代表 Sideloadly 一定失败，也不保证换到 Sideloadly 就能通过同一账号的资格检查；以实际签名结果为准。

免费账号的开发配置有效期为 **7 天**，每台设备最多同时安装 **3 个**此类应用。Apple 的签名要求仍然存在，但不等于必须先在网站手动登记或购买会员。[Sideloadly 官方安装流程](https://sideloadly.io/)、[Apple 账号与 Personal Team 说明](https://developer.apple.com/help/account/basics/about-your-developer-account)。

### 提示“Apple ID 暂时不符合使用此应用程序的条件”

这条提示表示 Developer 登录未获接受，单凭截图不能确定是密码问题、手机号登录方式还是账号资格限制。你已确认当前账号只有手机号、尚未绑定邮箱，因此先给同一账号添加并验证邮箱，再尝试邮箱登录；这是排查步骤，不保证能解除所有账号限制。

1. 打开 [Apple 账号官网](https://account.apple.com/)，用现有手机号账号登录。
2. 进入 **登录和安全 → 电子邮件和电话号码**，选择 **添加**。
3. 输入你能正常收信、且未绑定其他 Apple 账号的邮箱。
4. 在邮箱中接收 Apple 的验证邮件，回到页面输入验证码并保存。
5. 重新打开 [Apple Developer 账号页](https://developer.apple.com/account/)，用这个已验证邮箱和同一账号的密码登录，按提示完成手机验证。
6. 登录成功后再处理免费开发者协议；无需购买会员。若仍是同样的资格提示，需要继续核对账号状态，必要时联系 [Apple Developer 支持](https://developer.apple.com/support/)。

Apple 官方确认已登记的其他邮箱也可用于登录，并给出了上述添加流程；这不等于 Apple 已确认本次报错的唯一原因。[Apple 官方邮箱管理说明](https://support.apple.com/zh-cn/102529)。账号资料和验证由你本人操作，邮箱、手机号、密码和验证码不需要发到聊天。

## 四、按顺序安装电脑软件

1. 双击 **01-iTunes64Setup.exe**，按安装向导完成安装。
2. 双击 **02-iCloudSetup.exe**，按安装向导完成安装。这里需要它的 Apple 组件，不需要购买 iCloud+ 套餐或启用照片同步。
3. 双击 **03-SideloadlySetup64.exe**，按向导安装 Sideloadly。该官网安装器未提供 Windows 数字签名，请由本人确认是否运行。
4. 如果安装向导要求重启电脑，先保存正在编辑的文件，再由你自己重启。

这些安装器会正常安装到 Windows 程序目录并添加必要驱动；不会替换原游戏代码。本任务只下载并检查文件，没有替你运行安装或重启电脑。

## 五、连接 iPhone

1. 用可传输数据的 USB 线把 iPhone 接到 Windows 电脑。
2. 解锁 iPhone，看到“要信任此电脑吗？”时点“信任”，按提示输入手机锁屏密码。
3. 打开 iTunes，确认能看到手机图标。
4. 打开 Sideloadly，确认设备栏能选到你的 iPhone。首次先用 USB 连接完成安装。

如果显示 `No devices detected`，先保持手机解锁、重新插线，并确认 iTunes 能识别手机。[Sideloadly 设备识别说明](https://sideloadly.io/faq)。

## 六、签名并安装 IPA

1. 将本目录最新的 **KairiseiMA-ios-resource-bootstrap-0.0.3-unsigned.ipa** 拖进 Sideloadly 窗口。
2. 在 **Apple ID** 栏填写你准备好的 Apple 登录账号。
3. 点击 **Start**。在 Sideloadly 自己的提示框里填写 Apple 账号密码。
4. 如果 Apple 要求双重认证，在自己的手机或短信中接收验证码，再填进 Sideloadly 的提示框。
5. 保持 USB 连接，等待软件显示安装完成。

这里使用正常的 Apple 账号登录密码。按 Sideloadly 当前 FAQ，应用专用密码只部分适用于付费开发者账号，不是本次免费签名的默认做法。密码和验证码只在本机工具或 Apple 官方页面输入，不发到聊天。[Sideloadly FAQ](https://sideloadly.io/faq)。

### 登录失败：`-22411 / This action cannot be completed at this time`

如果错误出现在 `Login failed`，说明本次操作停在 Apple 登录阶段，尚不能据此判断 IPA 安装或运行是否正常。这个错误不表示必须购买开发者会员，也不能单凭错误码确定账号被封或密码错误。

1. 若账号仍只有手机号，可先按上面的邮箱添加步骤绑定并验证邮箱，再使用同一账号的已验证邮箱尝试登录。这是排查步骤，不保证解决该错误。
2. 先在 [Apple 账号官网](https://account.apple.com/) 确认该账号能正常登录；如 Apple 明确要求验证邮箱或补全账号资料，由你本人完成。
3. 区分手机是否收到登录确认或验证码。如果手机收到，但 Sideloadly 未出现验证码输入框，可能涉及工具的双重认证流程。其他签名工具的开发记录确认过同码的这类问题，不能直接证明本次 Sideloadly 的原因；不要把验证码拼到密码后面或关闭双重认证。
4. 若网页登录正常但工具仍失败，可在 iTunes 的“账户 → 登录”中尝试一次，判断 Apple 自带客户端是否也受影响。密码与验证码只在软件内填写。若 iTunes 也失败，优先检查日期时间、网络和 Apple 账号状态；可换一个可信网络测试一次，不要连续反复点击 Start。
5. 若 iTunes 登录正常、只有 Sideloadly 失败，再核对 Sideloadly 的版本、认证设置和报错前几行日志。分享日志前遮住账号、设备序列号及其他个人信息，不分享密码或验证码。

本机在 2026-11-18 的只读检查已确认 iTunes 12.13.12.3、iCloud 7.21.0.23、Apple Mobile Device Support 20.0.1.47 已安装，Apple Mobile Device Service 正在运行，时区为中国标准时间。没有修改 Apple 登录缓存或账号设置。

参考：[Apple 服务连接排查](https://support.apple.com/zh-cn/108093)、[AltSign 关于同码双重认证问题的开发记录](https://github.com/rileytestut/AltSign/pull/59)。后者适用于 AltSign，不能当作 Sideloadly 故障已确诊。

## 七、手机上允许运行

1. 如果提示需要开发者模式，打开 **设置 → 隐私与安全性 → 开发者模式**，打开开关，按提示重启手机。
2. 重启解锁后，如出现确认弹窗，再点“启用”，按提示输入锁屏密码。
3. 如果提示“不受信任的开发者”，打开 **设置 → 通用 → VPN 与设备管理**，找到刚才用于签名的 Apple 账号，按提示信任该开发者应用。
4. 打开 **Kairisei Connection Test**，确认进入标题界面，再点击 **离线战斗内核验证**，核对是否显示通过 30 步合成战斗。

开发者模式选项可能在开始配对或部署开发应用后才出现；看不到时，先完成 USB 信任和 Sideloadly 的安装尝试，再回设置查看。[Apple 开发者模式说明](https://developer.apple.com/documentation/xcode/enabling-developer-mode-on-a-device)。

安装和真机打开由你本人操作。这个测试包不自动读取 Windows 游戏账号或存档；完整游戏资源和玩法仍在恢复中。

## 八、七天后怎么续签

继续使用同一个 Apple 账号、同一个 IPA 和相同应用标识，在 Sideloadly 中再次点 Start 覆盖安装即可。**不要先删除手机上的应用**。

七天到期影响的是签名与启动资格，本身不会自动清空应用数据。保持同一个 Apple 账号和应用标识进行覆盖安装，通常可以保留应用内下载的资源和本地数据，不必因续签全部重下。后续收到新版 IPA，也应沿用同一账号和标识覆盖升级。若工具提示必须先卸载，先停止操作并检查原因。

当前验证版没有完整游戏资源下载功能；完整客户端将把需长期保留的下载资源写到应用持久目录，资源清单升级时校验并复用未变化的文件。此功能仍待实现和真机续签验证。更换签名账号／应用标识、删除 App、主动清理数据或游戏资源版本变更，仍可能导致需要重新下载。重要账号恢复资料和存档应另做备份。

安装后正常使用不必一直连接电脑。七天后的续签仍需要电脑；Sideloadly 的自动刷新也需要电脑运行，并通过 USB 或同一局域网连接手机。后续再配置 Wi-Fi 自动刷新，首次使用先按 USB 流程完成。[Sideloadly FAQ](https://sideloadly.io/faq)。
