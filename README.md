<p align="center">
<img src="assets/StoreLogo.png" width="48px"/>
</p>

<div align="center">

# 红果助理

[![GitHub release (latest by date)](https://img.shields.io/github/v/release/LanZhan-Harmony/PhoenixReadCopilot)](https://github.com/LanZhan-Harmony/PhoenixReadCopilot/releases) ![GitHub Release Date](https://img.shields.io/github/release-date/LanZhan-Harmony/PhoenixReadCopilot) ![GitHub All Releases](https://img.shields.io/github/downloads/LanZhan-Harmony/PhoenixReadCopilot/total) ![GitHub stars](https://img.shields.io/github/stars/LanZhan-Harmony/PhoenixReadCopilot?style=flat) ![GitHub forks](https://img.shields.io/github/forks/LanZhan-Harmony/PhoenixReadCopilot)

`红果助理` 是 [红果短剧](https://hongguoduanju.com/) 的第三方桌面应用，适用于 Windows 10 和 Windows 11.

</div>
<p align="center">
<a href="#开发">开发</a> &nbsp;&bull;&nbsp;
<a href="#安装">安装</a> &nbsp;&bull;&nbsp;
<a href="#使用">使用</a> &nbsp;&bull;&nbsp;
<a href="#鸣谢">鸣谢</a>
</p>

## 安装

### 商店下载（推荐）

[从 Microsoft Store 中获取](https://apps.microsoft.com/detail/9N2WRRN32KCT?launch=true&mode=full)

### 侧加载

1. 打开系统设置，依次选择 `系统` -> `开发者选项`，打开 `开发人员模式`。滚动到页面底部，展开 `PowerShell` 区块，开启 `更改执行策略...` 选项
2. 打开 [Release](https://github.com/LanZhan-Harmony/PhoenixReadCopilot/releases) 页面
3. 在最新版本的 **Assets** 中找到应用包下载。命名格式为：`PhoenixReadCopilot_{version}_{platform}.zip`
4. 下载应用包后解压，右键单击文件夹中的 `install.ps1` 脚本，选择 `使用 PowerShell 运行`

## 开发

在开发红果助理的过程中，抽离出了多个单独组件，可以被其它开发者集成到自己的项目中：

- [PhoenixReadApi](https://github.com/LanZhan-Harmony/PhoenixReadApi)  
  红果助理与 红果 API 交互的核心代码，是一层 .NET 包装器，基于 .NET 10，完全的AOT支持，便于移植和二次开发。
- [mpv-winui](https://github.com/Richasy/mpv-winui)  
  红果助理的核心播放器之一，将 MPV 集成进 WinUI3 以实现良好的播放体验。
- [winui-kernel](https://github.com/Richasy/winui-kernel)  
  在多个 WinAppSDK 项目之间共用的一些基础样式及实现。

除 mpv-winui 外，其它都以 nuget 形式集成在项目内

第一次克隆时可以运行下面的命令：

```shell
git clone --recurse-submodules https://github.com/LanZhan-Harmony/PhoenixReadCopilot.git
```

如果你已经克隆了仓库，但是忘记克隆子仓库了，那么可以运行下面的命令：

```shell
git submodule update --init --recursive
```

在克隆之后，你需要在命令行中进入克隆的子模块的目录，切换 HEAD，这是对应的列表：

| 模块      | 路径               | HEAD (分支) |
| --------- | ------------------ | ----------- |
| mpv-winui | src\Libs\mpv-winui | main        |

切换分支完成后，还需要下载 mpv / ffmpeg 到对应的目录：

| 文件名       | 目录                                                            | 说明                                                                                                                                                                         |
| ------------ | --------------------------------------------------------------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| libmpv-2.dll | src\Desktop\PhoenixReadCopilot.UI\Assets\libmpv\x64(或者 arm64) | 可以在 [mpv-winbuild](https://github.com/zhongfly/mpv-winbuild) 下载最新的 dev 构建（x64 对应 x86_x64，arm64 对应 aarch64），把 libmpv-2.dll 放入对应文件夹中，用以 mpv 播放 |
| ffmpeg.exe   | src\Desktop\PhoenixReadCopilot.UI\Assets\ffmpeg                 | 可以在 [mpv-winbuild](https://github.com/zhongfly/mpv-winbuild) 下载最新 ffmpeg x64 构建，将 ffmpeg.exe 放入对应文件夹中，用于视频下载后的混流                               |

## 使用

### 登录

红果助理可使用验证码登录。不登录也可以使用，但登录后可以获得更多的功能。

### 视频播放

红果助理支持多种播放方案：

1. MPV
2. FFmpeg
3. WebView2
4. 外部播放器（支持 MPV 和 MPV.NET）

红果助理已经将 MPV 嵌入到了 WinUI XAML 界面之中，可以借助 MPV 强大的播放能力实现稳定高效的在线流媒体播放，这也是默认的选项。

> [!TIP]
> 目前正在进行播放方案的过渡测试，MPV 的播放方案目前在应用内存在两个，推荐的播放器选择是 `岛`，后期会替换掉现在的 mpv 播放方案。

下面是几种播放方案的具体比较，请根据自己的情况选择合适的播放方案：

| 方案       | 优点                               | 缺点                       |
| ---------- | ---------------------------------- | -------------------------- |
| MPV        | 解码速度快，播放稳定               | 内存占用相对较高           |
| FFmpeg     | 兼容性好，支持多种格式             | 播放视觉效果不如 MPV       |
| WebView2   | 兼容性好，支持多种格式             | 内存占用较高               |
| 外部播放器 | 自定义程度高，可以获得最佳播放体验 | 需要额外安装配置，门槛较高 |

## 应用截图

## 鸣谢

- [Windows App SDK](https://github.com/microsoft/windowsappsdk)
- [WinUI](https://github.com/microsoft/microsoft-ui-xaml)
- [寒霜弹幕使](https://github.com/cotaku/DanmakuFrostMaster)
- [cnbluefire/WinUI3.Win2D](https://github.com/cnbluefire/WinUI3.Win2D)
- [Windows Community Toolkit](https://github.com/CommunityToolkit/Windows)
- [FluentIcons](https://github.com/davidxuang/FluentIcons)
- [ComputeSharp](https://github.com/Sergio0694/ComputeSharp)
