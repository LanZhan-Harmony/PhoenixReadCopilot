# PhoenixReadCopilot 开发规范与 AI 协作指南 (AGENTS.md)

本文档是 `PhoenixReadCopilot` 项目的工程架构规范与代码编写指南。任何在此仓库中编写、重构或调试代码的开发者与 AI Agent，均须严格遵循本文档所定义的目录分工、ViewModel 规范、C# 编码规范与边界约束。

---

## 1. 项目概述与核心约束

- **技术栈**：.NET 11 + WinUI 3 (Windows App SDK 2.6) + C# 15 + CommunityToolkit.Mvvm 8.4+。
- **本地 API 依赖**：`PhoenixReadApi` 本地 NuGet 包已在项目中配置就绪，命名空间为 `using PhoenixReadApi;`，严禁再引入旧版第三方接口依赖。
- **编译性能基准**：本项目多平台（x86/x64/ARM64）配置庞大，**构建和调试时必须显式指定 `x64` 平台**以实现最快编译速度：

  ```powershell
  # 编译项目 (Debug x64)
  dotnet build src/Desktop/PhoenixReadCopilot/PhoenixReadCopilot.csproj -p:Platform=x64 -c Debug

  # 运行应用
  winapp run -p x64
  ```

---

## 2. 目录结构与职责归属

项目划分为桌面主工程（Desktop）、底层类库（Libs）与独立工具（Updater）：

```text
d:\code\WinUI\PhoenixReadCopilot\
├── src/
│   ├── Desktop/
│   │   ├── PhoenixReadCopilot/                 # [核心应用工程] WinUI 3 界面交互与业务逻辑
│   │   │   ├── Activation/                     # 应用生命周期与激活调度（协议、命令行等激活器）
│   │   │   ├── Controls/                       # 自定义控件与复合组件（按业务模块聚合，直接继承原生控件）
│   │   │   ├── Converters/                     # XAML x:Bind 专用静态分部类 Converter（各文件定义 partial 方法）
│   │   │   ├── Extensions/                     # 扩展方法、TemplateSelector、MarkupExtension
│   │   │   ├── Forms/                          # 独立弹出框、ContentDialog、确认弹窗
│   │   │   ├── Helpers/                        # UI 层通用无状态辅助类（动画、颜色、字体、服务定位等）
│   │   │   ├── Models/                         # UI 专用常量、设置模型与配置映射
│   │   │   ├── Pages/                          # 顶级导航页面（RankPage, SettingsPage, UserSpacePage 等）
│   │   │   │   └── Overlay/                    # 浮层与二级页面（播放页等）
│   │   │   ├── Services/                       # 业务服务层实现（依赖注入托管，有状态及生命周期管理）
│   │   │   ├── Strings/                        # ReswPlus 多语言本地化资源（zh-Hans）
│   │   │   ├── Styles/                         # XAML 样式与 ResourceDictionary 资源字典
│   │   │   └── ViewModels/                     # MVVM 视图模型（按业务彻底解耦）
│   │   │       ├── Core/                       # 应用级核心 ViewModel（App, Navigation, Player 等）
│   │   │       ├── View/                       # 页面级 ViewModel
│   │   │       ├── Components/                 # 组件级 ViewModel（评论区、搜索框等）
│   │   │       └── Items/                      # 列表项/卡片级 ViewModel（各业务独立声明模型）
│   │   │
│   │   └── PhoenixReadCopilot.Core/            # [轻量共享模型库] 进程内跨模块共享的基础模型、契约与工具
│   │       ├── Contracts/                      # 核心服务契约接口
│   │       ├── Enums/                          # 核心业务枚举
│   │       ├── Helpers/                        # 底层核心辅助类（Debouncer, JsonAotSerializer, Mp4CencDecryptor 等）
│   │       ├── Models/                         # 领域数据传输对象与配置模型
│   │       └── Services/                       # 平台无关通用服务基础类
│   │
│   ├── Libs/
│   │   └── Danmaku.Core/                       # [弹幕引擎] 弹幕协议解析与弹幕轨道渲染模型
│   │
│   └── Updater/
│       └── PhoenixReadCopilot.Updater/         # [独立更新器] 独立进程更新与安装引导程序
```

### 2.1 Services 与 Helpers 的职责边界

- **`Services/`（业务服务层）**：
  - **依赖注入管理**：由 DI 容器统一托管，必须抽象接口（如 `ISettingsService`、`IDramaService`）；
  - **有状态与生命周期**：负责本地持久化、设置读写、会话管理、数据请求调度等核心业务逻辑。
- **`Helpers/`（通用无状态辅助类）**：
  - **静态无状态**：以静态类（`static class`）为主（如 `ColorHelper`、`JsonAotSerializer`、`ServiceHelper`）；
  - **通用底层算法**：负责防抖节流、NativeAOT 序列化桥接、路径转换等通用能力，不持有复杂业务状态。

---

## 3. 页面与控件架构规范

### 3.1 页面（Page）开发规范
- **生命周期显式化**：统一在 XAML 中声明 `Loaded="OnLoaded"` 与 `Unloaded="OnUnloaded"`，并在 code-behind 中实现，确保事件订阅与注销清晰可见。

### 3.2 自定义控件（Control / UserControl）开发规范
- **属性与依赖属性布局规范**：
  - 强类型属性必须写在对应 `DependencyProperty` 的**上方**；
  - 属性与依赖属性必须统一放置在**构造函数的上方**；
  ```csharp
  /// <summary>
  /// 视图模型.
  /// </summary>
  public MyItemViewModel? ViewModel
  {
      get => (MyItemViewModel?)GetValue(ViewModelProperty);
      set => SetValue(ViewModelProperty, value);
  }

  public static readonly DependencyProperty ViewModelProperty = DependencyProperty.Register(
      nameof(ViewModel),
      typeof(MyItemViewModel),
      typeof(MyControl),
      new PropertyMetadata(null, (d, e) => ((MyControl)d).OnViewModelChanged(e.OldValue as MyItemViewModel, e.NewValue as MyItemViewModel))
  );

  public MyControl()
  {
      InitializeComponent();
  }
  ```
- **XAML 根标签标准**：根标签使用标准的 `<UserControl>` 与 `<UserControl.Resources>`，杜绝命名空间垫片污染。

### 3.3 WinRT / CsWinRT 投影约束
- **动态加入视觉树的 WinRT 派生类必须为 `public`**：从 WinRT 类型（如 `Grid`）派生并加入 XAML 视觉树的类（如 `CursorGrid : Grid`），**必须声明为 `public partial class`**。若声明为 `internal`，CsWinRT 将无法生成激活工厂与元数据，导致运行时抛出 `0x800F1000`（`RO_E_METADATA_NAME_NOT_FOUND`）异常。

### 3.4 `x:Bind` 绑定函数与 Converter 规范
- **统一使用 `x:Bind` 静态绑定函数替代 `IValueConverter`**：禁止新建 `IValueConverter` 实现及在 `App.xaml` / 控件资源中注册转换器实例。所有无状态值转换函数统一定义在 `Converters/` 目录下各 `*Converter.cs` 文件中的 `internal static partial class Converter` 分部类中，并在 XAML 中通过 `xmlns:cvt="using:PhoenixReadCopilot.Converters"` 与 `{x:Bind cvt:Converter.Method(...), Mode=OneWay}` 调用（动态入参务必显式声明 `Mode=OneWay`）。
- **正向 `bool -> Visibility` 直接绑定**：WinUI 3 的 `x:Bind` 原生支持 `bool` 到 `Visibility` 的隐式转换，直接写 `Visibility="{x:Bind IsLoading, Mode=OneWay}"`，仅反转显隐时调用 `cvt:Converter.InvertVisibility(...)`。
- **禁止同参个数同名重载**：WinUI 3 XAML 编译器对 `x:Bind` 函数的重载解析**仅按参数个数匹配，不按参数类型区分**。不同输入类型必须使用不同方法名（如 `StringToBitmap` 与 `UriToBitmap`、`CountToText` 与 `DoubleCountToText`）。
- **`DataTemplate` 根对象传参**：在 `DataTemplate` 中将当前 `x:DataType` 根对象本身传入静态函数时，使用无路径强转语法（如 `{x:Bind cvt:Converter.LoopTypeToText((constants:VideoLoopType))}`）。
- **`ControlTemplate` 绑定**：在无 `x:Class` 的纯 `ResourceDictionary` / `ControlTemplate` 中优先使用 `{TemplateBinding}` 绑定强类型依赖属性（如 `AppNavigationViewItem.UnreadVisibility`），避免引入 `IValueConverter`。

---

## 4. ViewModel 规范

### 4.1 属性定义语法（强制 Partial Property 语法）

全面采用 C# 15 + CommunityToolkit.Mvvm 8.4+ 的分部属性生成方式，**禁止使用旧式的私有字段注解**：

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

### 4.2 命令定义语法

使用 `[RelayCommand]` 特性生成 IRelayCommand / AsyncRelayCommand，命令方法以操作语义命名：

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

### 4.3 业务模型解耦原则

- 严禁在不同业务模块共用单一“大一统”ViewModel（如禁止共用大一统的 `VideoItemViewModel`）；
- 各页面、各列表项按需声明专用的 ViewModel（如 `RankDramaItemViewModel`、`UserDramaItemViewModel`、`ExploreDramaItemViewModel`），确保字段明确、职责单一。

---

## 5. C# 编码与语法规范

### 5.1 命名规范

- **私有字段**：下划线前缀小驼峰（`_service`、`_logger`、`_cancellationTokenSource`）；
- **公共属性 / 方法 / 类 / 接口**：大驼峰（`Title`、`EpisodeCount`、`InitializeAsync`、`IDramaDiscoveryService`）；
- **局部变量 / 参数**：小驼峰（`dramaId`、`cancellationToken`）；
- **类型后缀约定**：
  - 页面：`*Page`（如 `RankPage`、`DramaPlayerPage`）
  - 视图模型：`*ViewModel`（如 `RankPageViewModel`、`RankDramaItemViewModel`）
  - 自定义控件：`*Control` / `*Section` / `*Button`（如 `UserMainHeader`、`CommentLikeButton`）
  - 业务服务接口 / 实现：`I*Service` / `*Service`（如 `ISettingsService` / `SettingsService`）
  - 静态辅助类：`*Helper`（如 `ColorHelper`、`ServiceHelper`）
  - 转换器文件：`*Converter.cs`（统一在 `Converters/` 中声明为 `internal static partial class Converter` 的分部文件）

### 5.2 现代 C# 特性与格式

- **空安全声明**：全库启用 `<Nullable>enable</Nullable>`，引用类型需准确声明可空性（`Drama? drama`），杜绝不可控的 `NullReferenceException`；
- **模式匹配**：使用 `is null`、`is not null`、`is { Count: > 0 }` 等现代语法；
- **XML 文档注释**：公共类、公开属性及重要方法均需附带简洁明了的中文注释（`/// <summary>...</summary>`）。

### 5.3 异步编程规范

1. **命名结尾**：异步方法名必须以 `Async` 结尾；
2. **返回类型**：
   - 必须返回 `Task` 或 `Task<T>`；
   - **严禁使用 `async void`**（仅允许在无法修改签名的原生 XAML 事件处理函数中使用，且内部必须使用 `try-catch` 全面拦截异常）；
3. **取消令牌与生命周期**：
   - 跨网络异步请求必须支持传入或持有 `CancellationToken`；
   - 页面销毁、刷新或重载时，应先对旧的 `CancellationTokenSource` 执行 `.Cancel()`；
4. **禁止同步阻塞**：
   - **严禁**调用 `.Result` 或 `.Wait()`，避免死锁；必须使用 `await`；
5. **UI 线程调度**：
   - 在后台线程更新绑定的集合或 UI 元素时，必须使用 `DispatcherQueue.TryEnqueue(...)`。

### 5.4 异常处理与日志记录

- 核心 ViewModel 均需注入 `ILogger<T>`；
- 业务请求必须进行有节制的异常兜底，严禁静默吞掉异常并记录详细日志。

### 5.5 本地化与字符串资源

- **禁止在代码和 XAML 中硬编码业务文本**；
- 文本统一在 `src/Desktop/PhoenixReadCopilot/Strings/zh-CN/Resources.resw` 中定义，在 C# 中通过 `Resources.KeyName` 强类型调用（由 ReswPlus 自动生成）。

---

## 6. 红果短剧 API（PhoenixReadApi）交互规范

1. **单例注入与认证持久化**：
   - 注册 `PhoenixReadApiClient` 为单例；
   - 会话状态持久化文件统一写入 `PathHelper.AuthFolderPath` 目录。
2. **媒体播放与 CENC 解密**：
   - 从 `client.ResolveMediaAsync(dramaId, videoId)` 获取 `PlaybackMedia` 后，检查 `DecryptionKey`；若非空，将 AES-128 解密密钥作为参数传递给播放器：
     ```csharp
     config.FFmpegOptions["decryption_key"] = media.DecryptionKey;
     ```
3. **分页游标规范**：
   - 红果接口返回分页游标 `NextOffset`（而非固定的 PageIndex）；
   - ViewModel 维护 `_offset`，首次传 0，后续翻页传入上一次响应的 `page.NextOffset`，结合 `page.HasMore` 控制加载状态。
