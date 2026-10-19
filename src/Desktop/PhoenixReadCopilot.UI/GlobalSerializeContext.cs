using System.Text.Json.Serialization;
using PhoenixReadCopilot.UI.Models;
using Richasy.BiliKernel.Models.Media;

namespace PhoenixReadCopilot.UI;

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(MediaIdentifier))]
[JsonSerializable(typeof(List<string>))]
[JsonSerializable(typeof(PinItem))]
[JsonSerializable(typeof(List<PinItem>))]
internal sealed partial class GlobalSerializeContext : JsonSerializerContext { }
