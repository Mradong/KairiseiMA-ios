# iOS 连接验证工程

本仓库用于验证 Unity/Xcode 构建及 iPhone 侧载安装。**尚未恢复完整游戏玩法，也没有已验证的 IPA。** 请保留为私有仓库。

## 只在 GitHub 中填写 Unity 登录资料

打开本仓库 **Settings → Secrets and variables → Actions → New repository secret**，添加：

| Name | Secret |
| --- | --- |
| `UNITY_EMAIL` | 已激活 Personal 的 Unity 账号邮箱 |
| `UNITY_PASSWORD` | 同一 Unity 账号的密码 |

不要把这些值写进源码、Issue、聊天或普通 Actions Variables。

当前工作流在 macOS 临时环境中运行官方 Unity Licensing Client 的 `--activate-ulf` 命令，为当前机器申请实际 Personal ULF 许可证，再交给 GameCI 编译。无需手工把本机的 `UnityEntitlementLicense.xml` 改名或转换成 `.ulf`，也不需要在本机安装 Editor。账号是否能通过此云端激活流程仍需实际构建验证；失败时不会跳过 Unity 的许可校验或伪报生成安装包。

运行结束后会尝试归还本次取得的许可证。激活日志和许可证留在临时 runner 中，不作为构建产物上传。脚本会隐藏凭据、生成的许可证及派生序列号。

## 构建和产物

工作流 **Build iOS connection-test IPA** 仅手动触发，不会因为上传代码自动运行。macOS 构建可能消耗账号的 Actions 额度，实际运行前应核对可用额度。

成功后，Actions 的产物名为 `ios-connection-test-unsigned`，包含连接验证用的未签名 IPA 及检查报告，保留 7 天。该 IPA 仍需使用自己的 Apple ID 签名侧载，不会发布到 App Store。

Unity Editor 固定为 `6000.3.26f1`，云端 Hub 固定为保留 CLI 的 `3.22.6`，runner 明确使用标准 Intel 标签 `macos-15-intel`。完整游戏、战斗原生库、iOS 游戏资源和真机运行尚未完成。

构建源码包含两项已恢复的展示素材（标题图与字体），不含玩家账号、存档、原始 DLL 或本机工具。后续恢复工作在本地独立目录内进行，不修改原 Windows 游戏。
