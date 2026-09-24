# Z-Orbit 星轨

**用你喜欢的头像，把常用应用放进一条轨道。**
A visual application launcher by **ZShonz**.

**目前仅支持 Windows 10 / 11 x64。** macOS、Linux 和原生 ARM64 版本暂未提供。支持简体中文和英文界面，默认简体中文。

[**直接下载 Windows 安装包**](https://github.com/ZShonz/Z-Orbit/releases/latest/download/Z-Orbit-Setup.exe) · [查看全部版本](https://github.com/ZShonz/Z-Orbit/releases) · [English](#english)

## 项目介绍

Z-Orbit 是一个基于头像轮播的 Windows 应用启动助手。按下快捷键，用鼠标滚轮或方向键选择头像，即可打开对应的本地程序、文件夹或网页。它提供独立的应用管理窗口，让添加、替换头像、修改地址和调整顺序都能在界面中完成。

它是一个辅助启动工具，不替换 Windows 桌面，也不包含或安装示例中的第三方应用。

## 演示截图

截图来自作者的个人配置，展示可实现的界面效果；公开版初始只提供 Grok、Claude、GPT 三个网页示例，不包含截图中的其他本地应用配置。

### 图片一：纯净版（界面中称“洁净版模式”）

隐藏品牌标识、关闭按钮与操作提示，保留轮播和应用管理入口。需要在管理窗口中手动开启。

![Z-Orbit 纯净版：仅保留头像轮播和应用管理入口](docs/images/clean-mode.png)

### 图片二：默认版本

首次使用默认关闭洁净版，显示品牌、隐藏按钮及鼠标、键盘操作说明，方便新用户上手。

![Z-Orbit 默认版本：显示完整操作提示](docs/images/default-mode.png)

### 图片三：管理设置

管理应用与头像、修改目标地址、拖动排序、切换配色、设置呼出快捷键和开机启动。

![Z-Orbit 应用管理与设置窗口](docs/images/app-management.png)

## 功能

- **头像轮播**：中央头像保持原色，未选中头像压暗，支持鼠标和键盘切换。
- **应用管理**：添加、编辑、删除本地程序、快捷方式、文件夹和网页入口。
- **拖入添加**：拖入一个 `.lnk`、`.url` 或 `.exe`，读取名称、路径和 Windows 原图标；确认保存后加入轮播。
- **自定义图片**：支持 PNG、JPG、BMP、ICO，单张不超过 20 MB；也可直接拖入图片替换头像。
- **拖动排序**：点击“应用排序”，拖动头像到插入线位置，松开自动保存；拖到列表上下边缘会自动滚动。
- **四种配色**：深色陶土、深色冰蓝、深色紫罗兰、浅色奶油。
- **洁净版开关**：默认关闭，熟悉操作后可开启。
- **中英文切换**：在应用管理窗口右上角选择“简体中文”或“English”，即时生效并记住选择。
- **自定义全局快捷键**：默认 `Alt + Space`；冲突或保存失败时保留原设置。
- **托盘与开机启动**：可在管理窗口开关；启用后登录 Windows 时在托盘后台运行，不自动弹出轮播。
- **本地保存**：应用列表、排序、主题和设置保存于本机。

## 下载与安装

1. [下载安装包](https://github.com/ZShonz/Z-Orbit/releases/latest/download/Z-Orbit-Setup.exe)，或在 [Releases](https://github.com/ZShonz/Z-Orbit/releases) 中选择版本。
2. 如果已有版本正在运行，先从系统托盘菜单退出 Z-Orbit。
3. 运行安装程序，完成后启动 Z-Orbit。安装包自带 .NET 运行时，无需另装 .NET。
4. 点击右下角“应用管理”，添加自己的应用。

需要英文界面时，在“应用管理”右上角点击 **English**。语言切换不会修改应用名称、地址、图片或未保存的编辑内容。Windows 自带文件选择框、系统按钮及系统错误文字仍跟随系统语言。

安装位置默认为 `%LOCALAPPDATA%\Programs\Z-Orbit`。公开版自带的三个网页入口为：

| 示例 | 打开地址 |
| --- | --- |
| Grok | https://grok.com/ |
| Claude | https://claude.ai/ |
| GPT | https://chatgpt.com/ |

这些网站可能需要各自的账户或订阅，Z-Orbit 不提供账户或绕过登录。示例不代表与相应服务存在合作关系。

默认配置开启登录后后台启动，可在管理窗口关闭。重新安装会保留已有个人设置；因此已有用户的洁净版选择不会被新版本默认值覆盖。

安装包暂未进行代码签名。建议从本仓库 Releases 下载，并用随附的 `SHA256SUMS.txt` 校验文件。

## 操作说明

| 操作 | 效果 |
| --- | --- |
| `Alt + Space`（可修改） | 呼出 / 隐藏启动器 |
| 鼠标滚轮或方向键 | 切换应用 |
| 右键点击头像 | 直接打开对应应用 |
| 左键点击头像 | 选中；再次点击选中头像即打开 |
| `Enter` | 打开当前选中应用 |
| 点击空白处 / `Esc` | 隐藏启动器，返回桌面 |
| 托盘图标双击 | 显示启动器 |
| 托盘菜单“退出” | 完全退出程序 |
| 管理窗口 `Ctrl + S` | 保存编辑 |

拖入快捷方式时保留其原始路径和启动设置，原快捷方式文件仍需保留。头像导入后会保存独立图片副本。

## 数据与隐私

为兼容更名前的版本，数据目录继续使用 `%LOCALAPPDATA%\CharacterLauncher`：

- `apps.json`：应用列表和设置；`apps.json.bak`：上一份保存的配置。
- `avatars/`：导入的头像副本。

公开源码和安装包不包含作者的个人配置、本机快捷方式、账户令牌或本地应用路径。软件不包含遥测或自动更新功能；打开网页由系统默认浏览器处理。

## 从源码构建

需要 Windows、Git 和 **.NET 8 SDK**。克隆仓库后运行：

```powershell
git clone https://github.com/ZShonz/Z-Orbit.git
cd Z-Orbit
dotnet restore CharacterLauncher.csproj
dotnet build CharacterLauncher.csproj -c Release --no-restore
.\bin\Release\net8.0-windows\win-x64\Z-Orbit.exe
```

项目文件和内部命名空间保留 `CharacterLauncher` 以兼容既有代码；生成的产品和 EXE 名称为 `Z-Orbit`。

验证：

```powershell
dotnet run --project verification/LauncherChecks.csproj -c Release -- "$PWD"
```

验证使用隔离配置与启动项目录，会短暂注册测试快捷键，不启动示例目标程序。

制作安装包需要 **Inno Setup 6**：

```powershell
.\installer\build.ps1 -Compiler 'C:\Program Files (x86)\Inno Setup 6\ISCC.exe'
```

构建脚本优先使用项目本地 SDK，未找到时使用 PATH 中的 `dotnet`。输出位于 `installer/output/`。运行时和编译器不提交到仓库。

## 开源与反馈

Copyright © 2026 **ZShonz**。源码按 **GNU GPL v3.0** 发布，完整条款见 [LICENSE](LICENSE)。分发本项目的修改版本时，须遵守 GPL 对相应源码和许可证的要求。.NET 等第三方组件按各自许可证发布，见 [docs/notices](docs/notices)。

欢迎通过 [Issues](https://github.com/ZShonz/Z-Orbit/issues) 提交问题和建议，或提交 Pull Request。反馈时请附 Windows 版本、Z-Orbit 版本和复现步骤，不要上传含私人路径或账户信息的配置文件。

---

# English

**Z-Orbit** is a visual Windows application launcher by **ZShonz**. Put your favorite portraits on a carousel and use them to open applications, folders, and websites.

**Windows 10 / 11 x64 only.** No macOS, Linux, or native ARM64 build is currently provided. The app supports Simplified Chinese and English, with Simplified Chinese as the default.

[**Download the Windows installer directly**](https://github.com/ZShonz/Z-Orbit/releases/latest/download/Z-Orbit-Setup.exe) · [All releases](https://github.com/ZShonz/Z-Orbit/releases)

## Screenshots

The screenshots above show the author's customized setup. The public release ships with only three website examples: Grok, Claude, and GPT.

1. **Clean mode** — [Image 1](docs/images/clean-mode.png): hides branding, the hide button, and operation hints. The carousel and management entry remain visible. Enable this mode manually.
2. **Default mode** — [Image 2](docs/images/default-mode.png): the initial experience, with full mouse and keyboard instructions. Clean mode is off by default.
3. **Application management** — [Image 3](docs/images/app-management.png): manage entries, images, target addresses, ordering, themes, hotkeys, and startup preferences.

## Features

- Portrait-based carousel with an untinted selected image and dimmed unselected images.
- Add, edit, and delete local application, shortcut, folder, and website entries.
- Drop a `.lnk`, `.url`, or `.exe` to populate its name, path, and Windows icon, then save.
- Replace portraits with PNG, JPG, BMP, or ICO images up to 20 MB.
- Enter sorting mode and drag entries to reorder them; insertion indicators and edge scrolling help with longer lists. Changes save on drop.
- Four themes: dark terracotta, ice blue, violet, and light cream.
- Optional clean mode, off on first use.
- Instant language switching between Simplified Chinese and English, with your preference saved locally.
- Configurable global hotkey, `Alt + Space` by default, with conflict handling.
- System tray operation and configurable launch at Windows sign-in.
- Local persistence of entries, order, theme, and preferences.

## Install and use

Download the installer from [Releases](https://github.com/ZShonz/Z-Orbit/releases), exit any running Z-Orbit instance through its tray menu, and run the installer. The .NET runtime is included. The default installation directory is `%LOCALAPPDATA%\Programs\Z-Orbit`.

To use English, click **应用管理** at the bottom right of the launcher, then select **English** at the top right of the management window. The change applies immediately and is remembered. App names, addresses, images, and unsaved edits are preserved. Native Windows dialogs, system buttons, and system error messages follow your Windows language. The screenshots above show the Chinese interface.

The public defaults open `https://grok.com/`, `https://claude.ai/`, and `https://chatgpt.com/` in your default browser. Add your own applications through the management window. These third-party services may require their own accounts or subscriptions; Z-Orbit is not affiliated with them.

| Input | Action |
| --- | --- |
| `Alt + Space` (configurable) | Show / hide the launcher |
| Mouse wheel or arrow keys | Change selection |
| Right-click a portrait | Open that entry directly |
| Left-click a portrait | Select; click the selected portrait again to open |
| `Enter` | Open the selected entry |
| Blank-area click / `Esc` | Hide and return to the desktop |
| Double-click tray icon | Show the launcher |
| Tray menu Exit | Quit the application |
| `Ctrl + S` in management | Save edits |

Startup is enabled in the default configuration and can be disabled in management. Sign-in startup runs in the tray without displaying the carousel. Reinstalling preserves existing settings, including an explicitly chosen clean-mode preference.

Imported shortcut files must remain at their original locations because their launch settings are preserved. Imported portrait images are stored as independent copies.

The installer is currently unsigned. Download from this repository's Releases and verify against the provided `SHA256SUMS.txt`.

## Data, building, and license

For compatibility with the earlier project name, settings remain in `%LOCALAPPDATA%\CharacterLauncher`: `apps.json`, its previous-save backup, and an `avatars/` directory. Public distributions do not include the author's local shortcuts, personal settings, or credentials. The app has no telemetry or automatic updater.

Build on Windows with **.NET 8 SDK** using the commands in the Chinese build section above. The project filename remains `CharacterLauncher.csproj`; the generated application is `Z-Orbit.exe`. **Inno Setup 6** is required to build the installer. The test program uses isolated configuration/startup directories and temporary test hotkeys without opening target applications.

Copyright © 2026 **ZShonz**. Released under **GNU GPL v3.0**; see [LICENSE](LICENSE). Distributed modified versions must comply with GPL source and licensing requirements. Third-party runtime notices are included in [docs/notices](docs/notices).

Issues and pull requests are welcome. Include your Windows version, application version, and reproduction steps, and remove private paths or account information from reports.
