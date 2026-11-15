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

## 开发

在开发红果助理的过程中，抽离出了红果 API 单独组件，可以被其它开发者集成到自己的项目中：

- [PhoenixReadApi](https://github.com/LanZhan-Harmony/PhoenixReadApi)  
  红果助理与 红果 API 交互的核心代码，是一层 .NET 包装器，基于 .NET 11，完全的AOT支持，便于移植和二次开发。

克隆时可以运行下面的命令：

```shell
git clone https://github.com/LanZhan-Harmony/PhoenixReadCopilot.git
```

## 使用

### 登录

红果助理可使用扫码或验证码登录。不登录也可以使用，但登录后可以获得更多的功能。

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

- [哔哩助理](https://github.com/Richasy/Bili.Copilot)
- [OpenHongGuo](https://github.com/beilunyang/openhongguo)
