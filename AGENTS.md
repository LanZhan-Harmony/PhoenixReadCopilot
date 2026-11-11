# PhoenixReadCopilot 开发规范与 AI 协作指南 (AGENTS.md)

本文档是 `PhoenixReadCopilot` 项目的工程架构规范与代码编写指南。任何在此仓库中编写、重构或调试代码的开发者与 AI Agent，均须严格遵循本文档所定义的目录分工、ViewModel 拆分结构、C# 编码规范与边界约束。

---

## 1. 项目概述与核心约束

- **技术栈**：.NET 10 + WinUI 3 (Windows App SDK 2.6) + C# 14 + CommunityToolkit.Mvvm 8.4+。
- **重构目标**：本项目正在从原 Bilibili 客户端（BiliKernel）全面迁移重构为**红果短剧（PhoenixRead）**桌面客户端。
- **本地 API 依赖**：`PhoenixReadApi` 本地 NuGet 包已在项目中配置就绪，命名空间为 `using PhoenixReadApi;`，严禁再引入旧版 B 站接口依赖。
- **编译性能基准**：本项目多平台（x86/x64/ARM64）配置庞大，**构建和调试时必须显式指定 `x64` 平台**以实现最快编译速度：

  ```powershell
  # 编译项目 (Debug x64)
  dotnet build src/Desktop/PhoenixReadCopilot.UI/PhoenixReadCopilot.UI.csproj -p:Platform=x64 -c Debug

  # 运行应用
  winapp run -p x64
  ```

---

## 2. 目录结构与职责归属（每个文件夹放什么）

项目结构分为桌面应用主工程（Desktop）与底层类库（Libs）：

```text
d:\code\WinUI3\PhoenixReadCopilot\
├── src/
│   ├── Desktop/
│   │   ├── PhoenixReadCopilot/              # [核心应用工程] WinUI 3 界面、交互与业务逻辑
│   │   │   ├── Controls/                       # 自定义控件与复合组件（按业务模块聚合）
│   │   │   ├── Converters/                     # XAML 专用 IValueConverter 转换器
│   │   │   ├── Extensions/                     # 扩展方法、TemplateSelector、MarkupExtension
│   │   │   ├── Forms/                          # 独立弹出框、ContentDialog、确认弹窗
│   │   │   ├── Helpers/                        # 通用无状态辅助类（防抖、NativeAOT 序列化、路径、服务访问等）
│   │   │   ├── Models/                         # UI 专用常量、枚举与配置映射
│   │   │   ├── Pages/                          # 顶级导航页面（PopularPage, SettingsPage 等）
│   │   │   │   └── Overlay/                    # 浮层/详情/二级页面（播放页、历史页、书架页等）
│   │   │   ├── Services/                       # 业务服务层接口与实现（DI 注入，配置、鉴权、本地持久化与数据桥接）
│   │   │   ├── Strings/                        # ReswPlus 多语言本地化资源（zh-CN, en-US）
│   │   │   ├── Styles/                         # XAML 样式与 ResourceDictionary 资源字典
│   │   │   ├── Toolkits/                       # 偏系统/UI 交互的工具类（AppToolkit, SettingsToolkit 等）
│   │   │   └── ViewModels/                     # MVVM 视图模型（按层次拆分）
│   │   │       ├── Core/                       # 应用级核心 ViewModel（App, Navigation, Player 等）
│   │   │       ├── View/                       # 页面级 ViewModel（以独立文件夹存放）
│   │   │       ├── Components/                 # 组件级 ViewModel（评论区、搜索框等）
│   │   │       └── Items/                      # 列表项/卡片级 ViewModel（剧集项、分集项等）
│   │   │
│   │   └── PhoenixReadCopilot.Core/       # [轻量模型库] 进程内跨模块共享的基础模型与数据传输对象
│   │
│   └── Libs/
│       ├── Danmaku.Core/                       # [弹幕引擎] 弹幕协议解析与弹幕轨道渲染模型
│       └── WinUIKernel.Share/                  # ⚠️ [只读共享内核] 跨项目共享基础库（见红线规则）
```

### ⚠️ 红线规则：严禁在 `src\Libs\WinUIKernel.Share` 里面新增代码！

- `WinUIKernel.Share` 是跨多个 WinAppSDK 项目公用的外部基础 submodule/类库。
- **严禁向 `src\Libs\WinUIKernel.Share` 新增任何业务类、自定义控件、工具方法或样式！**
- 所有针对红果助理新加的控件、转化器、辅助工具及 UI 样式，**一律放入 `src/Desktop/PhoenixReadCopilot.UI` 对应的目录下**。

### 2.1 Services、Helpers 与 Toolkits 的职责边界

为避免工具与逻辑杂糅，三者遵循严格的职责划分：

- **`Services/`（业务服务层）**：
  - **依赖注入管理**：由 DI 容器托管，必须抽象接口（如 `ISettingsService`、`IDramaService`）；
  - **有状态与生命周期**：负责本地持久化、设置读取写入、登录会话管理、数据请求调度等核心业务逻辑。
- **`Helpers/`（通用无状态辅助类）**：
  - **静态无状态**：以静态类（`static class`）为主（如 `PathHelper`、`JsonAotSerializer`、`Debouncer`、`ServiceHelper`）；
  - **通用底层算法**：负责路径计算、防抖节流、NativeAOT 序列化桥接、服务定位器快捷访问等通用功能，不持有复杂业务状态。
- **`Toolkits/`（平台与 UI 特性工具包）**：
  - **平台与系统交互**：面向 Windows App SDK、WinUI 控件生命周期、系统窗口/主题/多语言等 UI 层特性的工具类（如 `AppToolkit`、`SettingsToolkit`）。

---

## 3. ViewModel 架构与多文件 Partial 拆分规范

本项目强依赖 `CommunityToolkit.Mvvm` 的源代码生成器。为了保证单个文件职责单一、避免上千行臃肿代码，**所有复杂的页面级 ViewModel 必须建立独立文件夹，并采用 `partial class` 分离拆分**：

### 3.1 拆分结构标准

以 `PopularPageViewModel` 或 `DramaPlayerPageViewModel` 为例：

| 文件命名                     | 包含内容与职责                                                                                                                                                                                |
| :--------------------------- | :-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `XxxViewModel.cs`            | **主类与生命周期**：构造函数、服务依赖注入绑定、初始化入口 (`InitializeAsync`)、清理 (`Dispose`/`OnCloseAsync`)、主要页面路由。                                                               |
| `XxxViewModel.Properties.cs` | **私有状态与公开属性**：私有字段（`_service`, `_logger`, 游标/偏移量、`CancellationTokenSource`）、公开属性 (`[ObservableProperty] public partial ...`) 以及事件定义 (`event EventHandler`)。 |
| `XxxViewModel.Requests.cs`   | **网络与数据请求**：对 `PhoenixReadApiClient` 的异步数据拉取、分页加载、游标更新、列表拼装 (`LoadRecommendVideosAsync` 等)。                                                                  |
| `XxxViewModel.Operations.cs` | **用户交互命令**：响应用户主动点击的命令 (`[RelayCommand]`，如收藏、点赞、关注、换清晰度、分享、选集等)。                                                                                     |
| `XxxViewModel.Methods.cs`    | **内部辅助与数据转换**：非请求类的内部计算、视图初始化数据映射、格式化方法等。                                                                                                                |

> **注意**：较简单的小型 ViewModel（如简单的卡片列表项 `ItemViewModel`），若代码量少于 100 行，可保留在单个文件中；一旦属性或操作复杂，必须立即遵循上述规则拆分为 `.cs` 与 `.Properties.cs`。

### 3.2 属性定义语法（强制 Partial Property 语法）

本项目全面采用 C# 13 + CommunityToolkit.Mvvm 8.4+ 的分部属性生成方式，**禁止使用旧式的私有字段注解**：

```csharp
// 正确（本项目强制规范）：
[ObservableProperty]
public partial bool IsLoading { get; set; }

[ObservableProperty]
public partial ObservableCollection<DramaItemViewModel> Dramas { get; set; }

// 错误（禁止使用旧式私有字段写法）：
[ObservableProperty]
private bool _isLoading;
```

### 3.3 命令定义语法

使用 `[RelayCommand]` 特性生成 IRelayCommand / AsyncRelayCommand，命令方法名以操作语义命名：

```csharp
[RelayCommand]
private async Task RefreshAsync()
{
    // ...
}

[RelayCommand]
private void SelectEpisode(EpisodeItemViewModel episode)
{
    // ...
}
```

---

## 4. C# 编码与语法规范

### 4.1 命名规范

- **私有字段**：下划线前缀的小驼峰：`_service`、`_logger`、`_recommendOffset`、`_cancellationTokenSource`。
- **公共属性 / 方法 / 类 / 接口**：大驼峰（PascalCase）：`Title`、`EpisodeCount`、`InitializeAsync`、`IDramaDiscoveryService`。
- **局部变量 / 参数**：小驼峰（camelCase）：`seriesId`、`cancellationToken`。
- **类型后缀约定**：
  - 页面：`*Page`（如 `PopularPage`、`DramaPlayerPage`）
  - 页面基类：`*PageBase`（如 `PopularPageBase`）
  - 视图模型：`*ViewModel`
  - 自定义控件：`*Control` / `*Section` / `*Button`
  - 业务服务接口 / 实现：`I*Service` / `*Service`（如 `ISettingsService` / `SettingsService`）
  - 静态辅助类：`*Helper`（如 `PathHelper`、`ServiceHelper`）
  - 工具类：`*Toolkit`（如 `AppToolkit`、`SettingsToolkit`）
  - 转换器：`*Converter`（如 `BoolToVisibilityConverter`）

### 4.2 现代 C# 特性与格式

- **空安全声明**：全库启用 `<Nullable>enable</Nullable>`，引用类型需准确声明可空性（`Drama? drama`），杜绝不可控的 `NullReferenceException`。
- **模式匹配**：使用 `is null`、`is not null`、`is { Count: > 0 }` 等现代语法。
- **XML 文档注释**：公共类、公开属性及重要方法均需附带简洁明了的中文注释（`/// <summary>...</summary>`）。

### 4.3 异步编程规范

1. **命名结尾**：凡执行异步操作的方法，方法名必须以 `Async` 结尾。
2. **返回类型**：
   - 必须返回 `Task` 或 `Task<T>`；
   - **严禁使用 `async void`**（仅允许在无法修改签名的 XAML UI 原生事件处理函数中使用，且内部必须使用 `try-catch` 全面拦截异常）。
3. **取消令牌与生命周期**：
   - 跨网络异步请求必须支持传入或持有 `CancellationToken`；
   - 页面销毁、刷新或重载时，应先对旧的 `CancellationTokenSource` 执行 `.Cancel()`，防止网络竞争条件与内存泄漏。
4. **禁止同步阻塞**：
   - **严禁**调用 `.Result` 或 `.Wait()`，避免死锁；必须使用 `await`。
5. **UI 线程调度**：
   - 依赖注入获取 `DispatcherQueue`；当在后台线程或非 UI 上下文中更新绑定的 `ObservableCollection` 或 UI 元素时，必须使用：
     ```csharp
     _dispatcherQueue.TryEnqueue(() =>
     {
         Dramas.Add(item);
     });
     ```

### 4.4 异常处理与日志记录

- 每个核心 ViewModel 均需注入 `ILogger<T>`。
- 业务请求必须进行有节制的异常兜底，严禁静默吞掉异常：
  ```csharp
  try
  {
      await LoadDataAsync(cancellationToken);
  }
  catch (Exception ex)
  {
      _logger.LogError(ex, "加载短剧列表失败.");
      // 通知 UI 或更新状态
      IsLoadingFailed = true;
  }
  ```

### 4.5 本地化与字符串资源

- **禁止在代码和 XAML 中硬编码业务文本**（如提示信息、按钮名称）。
- 文本统一在 `src/Desktop/PhoenixReadCopilot.UI/Strings/zh-CN/Resources.resw` 中定义，在 C# 中通过 `Resources.KeyName` 强类型调用（由 ReswPlus 自动生成）。

---

## 5. 红果短剧 API（PhoenixReadApi）交互与集成规范

在对接 `D:\code\C#\PhoenixReadApi` 时，应严格遵循以下指引：

1. **依赖注入与单例管理**：
   - 注册 `PhoenixReadApiClient` 为单例；
   - 会话状态持久化文件统一写入 `PathHelper.AuthFolderPath` 文件夹。
2. **媒体播放与 CENC 解密**：
   - 项目播放核心为主力 `FFmpegPlayerViewModel`（基于 `FFmpegInteropX`）和备选 `WebPlayerViewModel`（基于 WebView2 + EME）；
   - 从 `client.ResolveMediaAsync(seriesId, videoId)` 获取 [PlaybackMedia](file:///D:/code/C#/PhoenixReadApi/PhoenixReadApi/Public/Models/PlaybackMedia.cs) 后，**必须检查 `DecryptionKey`**；若非空，将 AES-128 解密密钥作为参数传递给播放器：
     ```csharp
     config.FFmpegOptions["decryption_key"] = media.DecryptionKey;
     ```
3. **分页游标规范**：
   - 红果接口通常返回游标 `NextOffset`（而非固定的 PageIndex）；
   - ViewModel 中维护 `_offset`，首次传 0，后续翻页传入上一次响应返回的 `page.NextOffset`，并通过 `page.HasMore` 控制加载状态。

