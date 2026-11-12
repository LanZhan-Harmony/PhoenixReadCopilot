using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Microsoft.UI.Xaml;
using PhoenixReadApi;
using PhoenixReadCopilot.Core.Enums;
using PhoenixReadCopilot.Core.Models.Media;
using PhoenixReadCopilot.Core.Models.Pin;
using PhoenixReadCopilot.Models.Settings;

namespace PhoenixReadCopilot.Helpers;

[JsonSourceGenerationOptions(
    WriteIndented = true,
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    PropertyNameCaseInsensitive = true,
    UseStringEnumConverter = true
)]
[JsonSerializable(typeof(bool))]
[JsonSerializable(typeof(char))]
[JsonSerializable(typeof(byte))]
[JsonSerializable(typeof(short))]
[JsonSerializable(typeof(ushort))]
[JsonSerializable(typeof(int))]
[JsonSerializable(typeof(uint))]
[JsonSerializable(typeof(long))]
[JsonSerializable(typeof(ulong))]
[JsonSerializable(typeof(float))]
[JsonSerializable(typeof(double))]
[JsonSerializable(typeof(string))]
[JsonSerializable(typeof(Dictionary<string, string>))]
[JsonSerializable(typeof(ElementTheme))]
[JsonSerializable(typeof(PlayerDisplayMode))]
[JsonSerializable(typeof(PreferCodecType))]
[JsonSerializable(typeof(PreferQualityType))]
[JsonSerializable(typeof(PlayerType))]
[JsonSerializable(typeof(MTCBehavior))]
[JsonSerializable(typeof(AppSettings))]
[JsonSerializable(typeof(DanmakuLocation))]
[JsonSerializable(typeof(MediaIdentifier))]
[JsonSerializable(typeof(List<string>))]
[JsonSerializable(typeof(PinItem))]
[JsonSerializable(typeof(List<PinItem>))]
[JsonSerializable(typeof(Drama))]
[JsonSerializable(typeof(List<Drama>))]
internal sealed partial class SourceGenerationContext : JsonSerializerContext;

internal static class JsonAotSerializer
{
    /// <summary>
    /// Serializes an object to a JSON string.
    /// </summary>
    internal static string Serialize<T>(T value)
    {
        if (
            SourceGenerationContext.Default.GetTypeInfo(typeof(T))
            is not JsonTypeInfo<T> jsonTypeInfo
        )
        {
            throw new ArgumentNullException(
                nameof(value),
                $"{typeof(T)} is not declared in the SourceGenerationContext attributes."
            );
        }
        return JsonSerializer.Serialize(value, jsonTypeInfo);
    }

    /// <summary>
    /// Deserializes a JSON string to the specified type.
    /// </summary>
    internal static T? Deserialize<T>(string value)
    {
        if (
            SourceGenerationContext.Default.GetTypeInfo(typeof(T))
            is not JsonTypeInfo<T> jsonTypeInfo
        )
        {
            throw new ArgumentNullException(
                nameof(value),
                $"{typeof(T)} is not declared in the SourceGenerationContext attributes."
            );
        }
        return JsonSerializer.Deserialize(value, jsonTypeInfo);
    }

    internal static JsonTypeInfo<T> GetTypeInfo<T>()
    {
        if (
            SourceGenerationContext.Default.GetTypeInfo(typeof(T))
            is not JsonTypeInfo<T> jsonTypeInfo
        )
        {
            throw new InvalidOperationException(
                $"{typeof(T)} is not declared in the SourceGenerationContext attributes."
            );
        }
        return jsonTypeInfo;
    }
}
