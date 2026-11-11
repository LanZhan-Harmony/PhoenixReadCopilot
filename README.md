<p align="center">
<img src="assets/Icon.png" width="48px"/>
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

在开发红果助理的过程中，抽离出了红果 API 单独组件，可以被其它开发者集成到自己的项目中：

- [PhoenixReadApi](https://github.com/LanZhan-Harmony/PhoenixReadApi)  
  红果助理与 红果 API 交互的核心代码，是一层 .NET 包装器，基于 .NET 10，完全的AOT支持，便于移植和二次开发。

克隆时可以运行下面的命令：

```shell
git clone https://github.com/LanZhan-Harmony/PhoenixReadCopilot.git
```

## 使用

### 登录

红果助理可使用验证码登录。不登录也可以使用，但登录后可以获得更多的功能。

### 视频播放

红果助理支持多种播放方案：

1. FFmpeg
2. WebView2

下面是几种播放方案的具体比较，请根据自己的情况选择合适的播放方案：

| 方案     | 优点     | 缺点               |
| -------- | -------- | ------------------ |
| FFmpeg   | 性能高   | 部分设备可能不支持 |
| WebView2 | 兼容性好 | 内存占用较高       |

## 应用截图

## 鸣谢

- [Windows App SDK](https://github.com/microsoft/windowsappsdk)
- [WinUI](https://github.com/microsoft/microsoft-ui-xaml)
- [寒霜弹幕使](https://github.com/cotaku/DanmakuFrostMaster)
- [cnbluefire/WinUI3.Win2D](https://github.com/cnbluefire/WinUI3.Win2D)
- [Windows Community Toolkit](https://github.com/CommunityToolkit/Windows)
- [FluentIcons](https://github.com/davidxuang/FluentIcons)
- [ComputeSharp](https://github.com/Sergio0694/ComputeSharp)
