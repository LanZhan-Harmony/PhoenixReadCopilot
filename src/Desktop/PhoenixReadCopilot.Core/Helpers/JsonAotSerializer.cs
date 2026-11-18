using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using PhoenixReadApi;
using PhoenixReadCopilot.Core.Enums;
using PhoenixReadCopilot.Core.Models.Media;
using PhoenixReadCopilot.Core.Models.Pin;
using PhoenixReadCopilot.Core.Models.Playback;
using PhoenixReadCopilot.Core.Models.Settings;

namespace PhoenixReadCopilot.Core.Helpers;

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
[JsonSerializable(typeof(AppTheme))]
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
[JsonSerializable(typeof(ReadHistoryItem))]
[JsonSerializable(typeof(List<ReadHistoryItem>))]
[JsonSerializable(typeof(MediaDefinition))]
[JsonSerializable(typeof(MediaCodec))]
[JsonSerializable(typeof(PlaybackMedia))]
[JsonSerializable(typeof(List<PlaybackMedia>))]
[JsonSerializable(typeof(DramaPlaybackMediaCache))]
[JsonSerializable(typeof(Dictionary<string, List<PlaybackMedia>>))]
[JsonSerializable(typeof(ReservationItem))]
[JsonSerializable(typeof(List<ReservationItem>))]
[JsonSerializable(typeof(ReservationPage))]
public sealed partial class SourceGenerationContext : JsonSerializerContext;

public static class JsonAotSerializer
{
    private static class TypeCache<T>
    {
        internal static readonly JsonTypeInfo<T> Info =
            SourceGenerationContext.Default.GetTypeInfo(typeof(T)) as JsonTypeInfo<T>
            ?? throw new ArgumentException(
                $"{typeof(T)} is not declared in the SourceGenerationContext attributes."
            );
    }

    /// <summary>
    /// Serializes an object to a JSON string.
    /// </summary>
    public static string Serialize<T>(T value)
    {
        return JsonSerializer.Serialize(value, TypeCache<T>.Info);
    }

    /// <summary>
    /// Deserializes a JSON string to the specified type.
    /// </summary>
    public static T? Deserialize<T>(string value)
    {
        return JsonSerializer.Deserialize(value, TypeCache<T>.Info);
    }

    public static JsonTypeInfo<T> GetTypeInfo<T>()
    {
        return TypeCache<T>.Info;
    }
}
