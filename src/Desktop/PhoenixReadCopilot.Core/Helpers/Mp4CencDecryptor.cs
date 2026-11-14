using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;

namespace PhoenixReadCopilot.Core.Helpers;

/// <summary>
/// 纯 C# MP4 CENC (Common Encryption, ISO/IEC 23001-7) AES-128-CTR 就地极速解密器.
/// 无需借助 FFmpeg 命令行，支持多音轨/视频轨、8/16字节IV与子样本解密.
/// </summary>
public static class Mp4CencDecryptor
{
    /// <summary>
    /// 在后台线程中对包含 CENC AES-128 加密的 MP4 文件进行解密，并输出标准解密视频.
    /// </summary>
    /// <param name="inputFilePath">输入加密视频路径.</param>
    /// <param name="outputFilePath">输出解密视频路径.</param>
    /// <param name="key">16 字节 AES 密钥.</param>
    /// <param name="cancellationToken">取消令牌.</param>
    public static async Task DecryptFileAsync(
        string inputFilePath,
        string outputFilePath,
        byte[] key,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(inputFilePath);
        ArgumentNullException.ThrowIfNull(outputFilePath);
        ArgumentNullException.ThrowIfNull(key);

        if (!File.Exists(inputFilePath))
        {
            throw new FileNotFoundException("未找到待解密的视频文件.", inputFilePath);
        }

        if (key.Length != 16)
        {
            throw new ArgumentException("CENC AES-128 密钥长度必须为 16 字节.", nameof(key));
        }

        string tempOutputFile = outputFilePath + $".dec_{Guid.NewGuid():N}.tmp";
        try
        {
            // 在独立后台线程中复制并就地解密
            await Task.Run(
                    () =>
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        File.Copy(inputFilePath, tempOutputFile, overwrite: true);

                        using FileStream fs = new(
                            tempOutputFile,
                            FileMode.Open,
                            FileAccess.ReadWrite,
                            FileShare.None,
                            bufferSize: 65536,
                            useAsync: false
                        );

                        DecryptInPlace(fs, key, cancellationToken);
                    },
                    cancellationToken
                )
                .ConfigureAwait(false);

            if (File.Exists(outputFilePath))
            {
                File.Delete(outputFilePath);
            }

            File.Move(tempOutputFile, outputFilePath);
        }
        catch
        {
            if (File.Exists(tempOutputFile))
            {
                try
                {
                    File.Delete(tempOutputFile);
                }
                catch { }
            }
            throw;
        }
    }

    /// <summary>
    /// 对指定的文件流就地执行 CENC 解密.
    /// </summary>
    /// <param name="stream">可读写的视频文件流.</param>
    /// <param name="key">16 字节 AES-128 密钥.</param>
    /// <param name="cancellationToken">取消令牌.</param>
    public static void DecryptInPlace(
        Stream stream,
        byte[] key,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentNullException.ThrowIfNull(key);

        if (!stream.CanSeek || !stream.CanRead || !stream.CanWrite)
        {
            throw new ArgumentException("流必须支持随机寻址（Seek）与读写访问.", nameof(stream));
        }

        using Aes aes = Aes.Create();
        aes.Mode = CipherMode.ECB;
        aes.Padding = PaddingMode.None;
        aes.Key = key;

        // 1. 定位 moov box
        long moovOffset = -1;
        long moovSize = -1;

        stream.Seek(0, SeekOrigin.Begin);
        byte[] hdr = new byte[8];
        while (stream.Position + 8 <= stream.Length)
        {
            cancellationToken.ThrowIfCancellationRequested();
            long boxStart = stream.Position;
            stream.ReadExactly(hdr);
            uint size32 = BinaryPrimitives.ReadUInt32BigEndian(hdr.AsSpan(0, 4));
            string type = Encoding.ASCII.GetString(hdr, 4, 4);

            long boxSize = size32;
            if (size32 == 1)
            {
                byte[] largeHdr = new byte[8];
                stream.ReadExactly(largeHdr);
                boxSize = (long)BinaryPrimitives.ReadUInt64BigEndian(largeHdr);
            }
            else if (size32 == 0)
            {
                boxSize = stream.Length - boxStart;
            }

            if (type == "moov")
            {
                moovOffset = boxStart;
                moovSize = boxSize;
                break;
            }

            stream.Seek(boxStart + boxSize, SeekOrigin.Begin);
        }

        if (moovOffset < 0)
        {
            throw new InvalidDataException("MP4 格式解析失败: 未找到 moov box.");
        }

        // 读取完整的 moov 数据块
        stream.Seek(moovOffset, SeekOrigin.Begin);
        byte[] moovData = new byte[moovSize];
        stream.ReadExactly(moovData);

        // 解析 moov 下的所有 protected trak
        List<TrackInfo> tracks = ParseMoov(moovData, moovOffset);
        if (tracks.Count == 0)
        {
            // 无加密轨道，直接返回
            return;
        }

        // 2. 解密每个轨道的样本数据
        byte[] counter = new byte[16];
        byte[] sampleBuffer = [];

        foreach (TrackInfo track in tracks)
        {
            cancellationToken.ThrowIfCancellationRequested();

            for (int i = 0; i < track.Samples.Count; i++)
            {
                if ((i & 0x7F) == 0)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                }

                SampleInfo sample = track.Samples[i];
                if (sample.Size == 0)
                {
                    continue;
                }

                if (sampleBuffer.Length < sample.Size)
                {
                    sampleBuffer = new byte[Math.Max(sample.Size, sampleBuffer.Length * 2)];
                }

                stream.Seek(sample.Offset, SeekOrigin.Begin);
                stream.ReadExactly(sampleBuffer, 0, (int)sample.Size);

                // 初始化当前样本专属计数器块
                Array.Clear(counter);
                if (sample.Iv.Length == 8)
                {
                    Buffer.BlockCopy(sample.Iv, 0, counter, 0, 8);
                }
                else if (sample.Iv.Length == 16)
                {
                    Buffer.BlockCopy(sample.Iv, 0, counter, 0, 16);
                }

                if (sample.Subsamples is not null && sample.Subsamples.Count > 0)
                {
                    int bufferOffset = 0;
                    foreach (SubsampleInfo sub in sample.Subsamples)
                    {
                        bufferOffset += sub.BytesOfClearData;
                        int encBytes = (int)sub.BytesOfEncryptedData;
                        if (encBytes > 0)
                        {
                            DecryptBytes(aes, sampleBuffer.AsSpan(bufferOffset, encBytes), counter);
                            bufferOffset += encBytes;
                        }
                    }
                }
                else
                {
                    DecryptBytes(aes, sampleBuffer.AsSpan(0, (int)sample.Size), counter);
                }

                stream.Seek(sample.Offset, SeekOrigin.Begin);
                stream.Write(sampleBuffer, 0, (int)sample.Size);
            }

            // 3. 回写并清洗 moov 元数据:
            // (a) 将 encv/enca 还原为原始格式标识 (如 hvc1, avc1, mp4a)
            if (track.EncFormatOffsetInMoov >= 0 && !string.IsNullOrEmpty(track.OriginalFormat))
            {
                byte[] origBytes = Encoding.ASCII.GetBytes(track.OriginalFormat);
                stream.Seek(moovOffset + track.EncFormatOffsetInMoov, SeekOrigin.Begin);
                stream.Write(origBytes);
            }

            // (b) 将已解密的 sinf, senc, saio, saiz 标记为 free 填充块，使标准播放器无缝直放
            ReadOnlySpan<byte> freeTag = "free"u8;
            if (track.SinfOffsetInMoov >= 0)
            {
                stream.Seek(moovOffset + track.SinfOffsetInMoov + 4, SeekOrigin.Begin);
                stream.Write(freeTag);
            }

            if (track.SencOffsetInMoov >= 0)
            {
                stream.Seek(moovOffset + track.SencOffsetInMoov + 4, SeekOrigin.Begin);
                stream.Write(freeTag);
            }

            if (track.SaioOffsetInMoov >= 0)
            {
                stream.Seek(moovOffset + track.SaioOffsetInMoov + 4, SeekOrigin.Begin);
                stream.Write(freeTag);
            }

            if (track.SaizOffsetInMoov >= 0)
            {
                stream.Seek(moovOffset + track.SaizOffsetInMoov + 4, SeekOrigin.Begin);
                stream.Write(freeTag);
            }
        }

        stream.Flush();
    }

    private static void DecryptBytes(Aes aes, Span<byte> data, Span<byte> counter)
    {
        Span<byte> keystream = stackalloc byte[16];
        int offset = 0;
        int remaining = data.Length;

        while (remaining > 0)
        {
            aes.EncryptEcb(counter, keystream, PaddingMode.None);
            IncrementCounter(counter);

            int take = Math.Min(16, remaining);
            for (int j = 0; j < take; j++)
            {
                data[offset + j] ^= keystream[j];
            }

            offset += take;
            remaining -= take;
        }
    }

    private static void IncrementCounter(Span<byte> counter)
    {
        for (int i = 15; i >= 0; i--)
        {
            if (++counter[i] != 0)
            {
                break;
            }
        }
    }

    private static List<TrackInfo> ParseMoov(byte[] moovData, long moovFileOffset)
    {
        List<TrackInfo> tracks = [];
        int offset = 8;
        if (BinaryPrimitives.ReadUInt32BigEndian(moovData.AsSpan(0, 4)) == 1)
        {
            offset = 16;
        }

        while (offset + 8 <= moovData.Length)
        {
            (string type, int boxSize, _) = ReadBoxHeader(moovData, offset);
            if (boxSize <= 0 || offset + boxSize > moovData.Length)
            {
                break;
            }

            if (type == "trak")
            {
                TrackInfo? t = ParseTrak(moovData, offset, boxSize, moovFileOffset);
                if (t is not null && t.IsProtected)
                {
                    tracks.Add(t);
                }
            }
            offset += boxSize;
        }
        return tracks;
    }

    private static TrackInfo? ParseTrak(
        byte[] data,
        int trakOffset,
        int trakSize,
        long moovFileOffset
    )
    {
        int mdiaOffset = FindSubBox(data, trakOffset + 8, trakOffset + trakSize, "mdia");
        if (mdiaOffset < 0)
        {
            return null;
        }

        (int mdiaSize, _) = GetBoxSize(data, mdiaOffset);

        int minfOffset = FindSubBox(data, mdiaOffset + 8, mdiaOffset + mdiaSize, "minf");
        if (minfOffset < 0)
        {
            return null;
        }

        (int minfSize, _) = GetBoxSize(data, minfOffset);

        int stblOffset = FindSubBox(data, minfOffset + 8, minfOffset + minfSize, "stbl");
        if (stblOffset < 0)
        {
            return null;
        }

        (int stblSize, _) = GetBoxSize(data, stblOffset);

        int stblEnd = stblOffset + stblSize;

        int stsdOffset = FindSubBox(data, stblOffset + 8, stblEnd, "stsd");
        if (stsdOffset < 0)
        {
            return null;
        }

        int encFormatOffsetInMoov = -1;
        string encType = "";
        string originalFormat = "";
        int sinfOffsetInMoov = -1;
        int defaultIvSize = 8;
        bool isProtected = false;

        int sampleEntryOffset = stsdOffset + 8 + 8;
        if (sampleEntryOffset + 8 <= stblEnd)
        {
            (int entrySize, _) = GetBoxSize(data, sampleEntryOffset);
            string entryType = Encoding.ASCII.GetString(data, sampleEntryOffset + 4, 4);
            if (entryType is "encv" or "enca" or "encs" or "enct")
            {
                isProtected = true;
                encType = entryType;
                encFormatOffsetInMoov = sampleEntryOffset + 4;

                int sinfOffset = ScanForBox(
                    data,
                    sampleEntryOffset + 8,
                    sampleEntryOffset + entrySize,
                    "sinf"
                );
                if (sinfOffset >= 0)
                {
                    sinfOffsetInMoov = sinfOffset;
                    (int sinfSize, _) = GetBoxSize(data, sinfOffset);
                    int sinfEnd = sinfOffset + sinfSize;

                    int frmaOffset = ScanForBox(data, sinfOffset + 8, sinfEnd, "frma");
                    if (frmaOffset >= 0)
                    {
                        originalFormat = Encoding.ASCII.GetString(data, frmaOffset + 8, 4);
                    }

                    int tencOffset = ScanForBox(data, sinfOffset + 8, sinfEnd, "tenc");
                    if (tencOffset >= 0)
                    {
                        int tencPayload = tencOffset + 8;
                        defaultIvSize = data[tencPayload + 7];
                        if (defaultIvSize == 0)
                        {
                            defaultIvSize = 8;
                        }
                    }
                }
            }
        }

        if (!isProtected)
        {
            return null;
        }

        int stszOffset = FindSubBox(data, stblOffset + 8, stblEnd, "stsz");
        int stscOffset = FindSubBox(data, stblOffset + 8, stblEnd, "stsc");
        int stcoOffset = FindSubBox(data, stblOffset + 8, stblEnd, "stco");
        int co64Offset = FindSubBox(data, stblOffset + 8, stblEnd, "co64");
        int sencOffset = FindSubBox(data, stblOffset + 8, stblEnd, "senc");
        int saioOffset = FindSubBox(data, stblOffset + 8, stblEnd, "saio");
        int saizOffset = FindSubBox(data, stblOffset + 8, stblEnd, "saiz");

        if (
            stszOffset < 0
            || stscOffset < 0
            || (stcoOffset < 0 && co64Offset < 0)
            || sencOffset < 0
        )
        {
            return null;
        }

        // 解析 stsz
        int stszPayload = stszOffset + 8;
        uint defSize = BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(stszPayload + 4, 4));
        uint sampleCount = BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(stszPayload + 8, 4));
        uint[] sampleSizes = new uint[sampleCount];
        if (defSize == 0)
        {
            for (int i = 0; i < sampleCount; i++)
            {
                sampleSizes[i] = BinaryPrimitives.ReadUInt32BigEndian(
                    data.AsSpan(stszPayload + 12 + i * 4, 4)
                );
            }
        }
        else
        {
            Array.Fill(sampleSizes, defSize);
        }

        // 解析 stco / co64
        long[] chunkOffsets;
        if (stcoOffset >= 0)
        {
            int stcoPayload = stcoOffset + 8;
            uint chunkCount = BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(stcoPayload + 4, 4));
            chunkOffsets = new long[chunkCount];
            for (int i = 0; i < chunkCount; i++)
            {
                chunkOffsets[i] = BinaryPrimitives.ReadUInt32BigEndian(
                    data.AsSpan(stcoPayload + 8 + i * 4, 4)
                );
            }
        }
        else
        {
            int co64Payload = co64Offset + 8;
            uint chunkCount = BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(co64Payload + 4, 4));
            chunkOffsets = new long[chunkCount];
            for (int i = 0; i < chunkCount; i++)
            {
                chunkOffsets[i] = (long)
                    BinaryPrimitives.ReadUInt64BigEndian(data.AsSpan(co64Payload + 8 + i * 8, 8));
            }
        }

        // 解析 stsc
        int stscPayload = stscOffset + 8;
        uint stscCount = BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(stscPayload + 4, 4));
        StscEntry[] stscEntries = new StscEntry[stscCount];
        for (int i = 0; i < stscCount; i++)
        {
            stscEntries[i] = new StscEntry(
                BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(stscPayload + 8 + i * 12, 4)),
                BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(stscPayload + 12 + i * 12, 4)),
                BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(stscPayload + 16 + i * 12, 4))
            );
        }

        // 解析 senc
        int sencPayload = sencOffset + 8;
        byte sencFlags = data[sencPayload + 3];
        bool hasSubsamples = (sencFlags & 0x02) != 0;
        uint sencCount = BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(sencPayload + 4, 4));

        int sencPtr = sencPayload + 8;
        List<SampleEncryptionInfo> encInfos = [with((int)sencCount)];
        for (int i = 0; i < sencCount; i++)
        {
            byte[] iv = new byte[defaultIvSize];
            Buffer.BlockCopy(data, sencPtr, iv, 0, defaultIvSize);
            sencPtr += defaultIvSize;

            List<SubsampleInfo>? subs = null;
            if (hasSubsamples)
            {
                ushort subCount = BinaryPrimitives.ReadUInt16BigEndian(data.AsSpan(sencPtr, 2));
                sencPtr += 2;
                subs = [with(subCount)];
                for (int s = 0; s < subCount; s++)
                {
                    ushort clear = BinaryPrimitives.ReadUInt16BigEndian(data.AsSpan(sencPtr, 2));
                    uint enc = BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(sencPtr + 2, 4));
                    sencPtr += 6;
                    subs.Add(new SubsampleInfo(clear, enc));
                }
            }

            encInfos.Add(new SampleEncryptionInfo(iv, subs));
        }

        // 装配完整样本结构与文件全局偏移
        List<SampleInfo> samples = [with((int)sampleCount)];
        int currentSampleIdx = 0;
        int currentStscIdx = 0;

        for (int chunkIdx = 0; chunkIdx < chunkOffsets.Length; chunkIdx++)
        {
            uint chunkNumber = (uint)(chunkIdx + 1);
            if (
                currentStscIdx + 1 < stscEntries.Length
                && chunkNumber >= stscEntries[currentStscIdx + 1].FirstChunk
            )
            {
                currentStscIdx++;
            }

            uint samplesInChunk = stscEntries[currentStscIdx].SamplesPerChunk;
            long curPos = chunkOffsets[chunkIdx];

            for (int s = 0; s < samplesInChunk; s++)
            {
                if (currentSampleIdx >= sampleCount)
                {
                    break;
                }

                uint size = sampleSizes[currentSampleIdx];
                SampleEncryptionInfo encInfo = encInfos[currentSampleIdx];

                samples.Add(new SampleInfo(curPos, size, encInfo.Iv, encInfo.Subsamples));

                curPos += size;
                currentSampleIdx++;
            }
        }

        return new TrackInfo
        {
            IsProtected = true,
            OriginalFormat = originalFormat,
            EncType = encType,
            DefaultIvSize = defaultIvSize,
            EncFormatOffsetInMoov = encFormatOffsetInMoov,
            SinfOffsetInMoov = sinfOffsetInMoov,
            SencOffsetInMoov = sencOffset,
            SaioOffsetInMoov = saioOffset,
            SaizOffsetInMoov = saizOffset,
            Samples = samples,
        };
    }

    private static int FindSubBox(byte[] data, int start, int end, string targetType)
    {
        int offset = start;
        while (offset + 8 <= end)
        {
            (string type, int boxSize, _) = ReadBoxHeader(data, offset);
            if (boxSize <= 0)
            {
                break;
            }

            if (type == targetType)
            {
                return offset;
            }

            offset += boxSize;
        }
        return -1;
    }

    private static int ScanForBox(byte[] data, int start, int end, string targetType)
    {
        byte[] target = Encoding.ASCII.GetBytes(targetType);
        for (int i = start; i <= end - 8; i++)
        {
            if (
                data[i + 4] == target[0]
                && data[i + 5] == target[1]
                && data[i + 6] == target[2]
                && data[i + 7] == target[3]
            )
            {
                uint size = BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(i, 4));
                if (size >= 8 && i + size <= data.Length)
                {
                    return i;
                }
            }
        }
        return -1;
    }

    private static (string Type, int Size, int HeaderSize) ReadBoxHeader(byte[] data, int offset)
    {
        uint size32 = BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(offset, 4));
        string type = Encoding.ASCII.GetString(data, offset + 4, 4);
        if (size32 == 1)
        {
            long size64 = (long)BinaryPrimitives.ReadUInt64BigEndian(data.AsSpan(offset + 8, 8));
            return (type, (int)size64, 16);
        }
        return (type, (int)size32, 8);
    }

    private static (int Size, int HeaderSize) GetBoxSize(byte[] data, int offset)
    {
        uint size32 = BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(offset, 4));
        if (size32 == 1)
        {
            long size64 = (long)BinaryPrimitives.ReadUInt64BigEndian(data.AsSpan(offset + 8, 8));
            return ((int)size64, 16);
        }
        return ((int)size32, 8);
    }

    private sealed record StscEntry(
        uint FirstChunk,
        uint SamplesPerChunk,
        uint SampleDescriptionIndex
    );

    private sealed record SampleEncryptionInfo(byte[] Iv, List<SubsampleInfo>? Subsamples);

    public sealed record SubsampleInfo(ushort BytesOfClearData, uint BytesOfEncryptedData);

    public sealed record SampleInfo(
        long Offset,
        uint Size,
        byte[] Iv,
        List<SubsampleInfo>? Subsamples
    );

    public sealed class TrackInfo
    {
        public bool IsProtected { get; set; }
        public string OriginalFormat { get; set; } = string.Empty;
        public string EncType { get; set; } = string.Empty;
        public int DefaultIvSize { get; set; } = 8;
        public int EncFormatOffsetInMoov { get; set; } = -1;
        public int SinfOffsetInMoov { get; set; } = -1;
        public int SencOffsetInMoov { get; set; } = -1;
        public int SaioOffsetInMoov { get; set; } = -1;
        public int SaizOffsetInMoov { get; set; } = -1;
        public List<SampleInfo> Samples { get; set; } = [];
    }
}
