# iOS 连接验证工程

本仓库用于验证 Unity/Xcode 构建及 iPhone 侧载安装。**首次连接验证 IPA 已由本人确认安装并打开标题；本次增加恢复中的原生战斗内核离线验证，完整游戏玩法尚未恢复。** 请保留为私有仓库。

[首次构建 44217212585](https://github.com/Mradong/KairiseiMA-ios/actions/runs/44217212585)已通过官方 Personal 激活、Unity 导出、Xcode 编译和结构检查。本次构建会先用 Go 1.25.1/Xcode 编译 iOS ARM64 静态库，并将“离线战斗内核验证”接到相同应用标识的 0.0.2 版。验证使用 20 张合成战斗表、四名测试队员，以及由 Windows 原 DLL 对照通过的 30 步胜利流程哈希；不读取玩家数据、不连接服务器。新版的云端构建与手机验证结果应以对应 Actions 记录为准，不能将源码准备当作成功编译。

## 只在 GitHub 中填写 Unity 登录资料

打开本仓库 **Settings → Secrets and variables → Actions → New repository secret**，添加：

| Name | Secret |
| --- | --- |
| `UNITY_EMAIL` | 已激活 Personal 的 Unity 账号邮箱 |
| `UNITY_PASSWORD` | 同一 Unity 账号的密码 |

不要把这些值写进源码、Issue、聊天或普通 Actions Variables。

当前工作流在 macOS 临时环境中运行官方 Unity Licensing Client 的 `--activate-ulf` 命令，为当前机器申请实际 Personal ULF 许可证，再交给 GameCI 编译。首次实际激活与归还已成功。无需手工把本机的 `UnityEntitlementLicense.xml` 改名或转换成 `.ulf`，也不需要在本机安装 Editor。失败时不会跳过 Unity 的许可校验或伪报生成安装包。

运行结束后会尝试归还本次取得的许可证。激活日志和许可证留在临时 runner 中，不作为构建产物上传。脚本会隐藏凭据、生成的许可证及派生序列号。

## 构建和产物

工作流 **Build iOS connection-test IPA** 仅手动触发，不会因为上传代码自动运行。macOS 构建可能消耗账号的 Actions 额度，实际运行前应核对可用额度。

成功后，Actions 的产物名为 `ios-connection-test-unsigned`，包含连接验证用的未签名 IPA 及检查报告，保留 7 天。该 IPA 仍需使用自己的 Apple ID 签名侧载，不会发布到 App Store。

Unity Editor 固定为 `6000.3.26f1`，云端 Hub 固定为保留 CLI 的 `3.22.6`，runner 使用标准 Intel 标签 `macos-15-intel`。公共 Go 引擎依赖固定为提交 `d1a202eb539b99511e8a33f3975fac409ab545bf`，只在临时 runner 中检出且拒绝修改过的依赖。恢复中的桥接层已有独立战斗流程，原客户端注册数据到完整内容的解码仍未完成；完整游戏、iOS 资源与新版真机验证仍待完成。

构建源码包含重建的 Go 桥接代码、合成战斗夹具及两项已恢复的展示素材（标题图与字体），不含玩家账号、存档、原始 DLL 或本机工具。续签或升级请沿用同一 Apple 账号和应用标识覆盖安装，不要先删除 App。后续恢复工作在本地独立目录内进行，不修改原 Windows 游戏。
