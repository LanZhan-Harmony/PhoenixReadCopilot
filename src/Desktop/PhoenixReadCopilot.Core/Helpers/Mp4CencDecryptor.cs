using System.Buffers;
using System.Buffers.Binary;
using System.IO.MemoryMappedFiles;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Security.Cryptography;

namespace PhoenixReadCopilot.Core.Helpers;

/// <summary>
/// MP4 CENC (Common Encryption, ISO/IEC 23001-7) AES-128-CTR 就地极速解密器.
/// 基于 Span/Memory、MemoryMappedFile 零拷贝 I/O、AES-NI 批量多块加密与 SIMD 向量化异或加速.
/// </summary>
public static class Mp4CencDecryptor
{
    private static readonly uint FourCcMoov = BinaryPrimitives.ReadUInt32BigEndian("moov"u8);
    private static readonly uint FourCcTrak = BinaryPrimitives.ReadUInt32BigEndian("trak"u8);
    private static readonly uint FourCcMdia = BinaryPrimitives.ReadUInt32BigEndian("mdia"u8);
    private static readonly uint FourCcMinf = BinaryPrimitives.ReadUInt32BigEndian("minf"u8);
    private static readonly uint FourCcStbl = BinaryPrimitives.ReadUInt32BigEndian("stbl"u8);
    private static readonly uint FourCcStsd = BinaryPrimitives.ReadUInt32BigEndian("stsd"u8);
    private static readonly uint FourCcStsz = BinaryPrimitives.ReadUInt32BigEndian("stsz"u8);
    private static readonly uint FourCcStsc = BinaryPrimitives.ReadUInt32BigEndian("stsc"u8);
    private static readonly uint FourCcStco = BinaryPrimitives.ReadUInt32BigEndian("stco"u8);
    private static readonly uint FourCcCo64 = BinaryPrimitives.ReadUInt32BigEndian("co64"u8);
    private static readonly uint FourCcSenc = BinaryPrimitives.ReadUInt32BigEndian("senc"u8);
    private static readonly uint FourCcSaio = BinaryPrimitives.ReadUInt32BigEndian("saio"u8);
    private static readonly uint FourCcSaiz = BinaryPrimitives.ReadUInt32BigEndian("saiz"u8);
    private static readonly uint FourCcSinf = BinaryPrimitives.ReadUInt32BigEndian("sinf"u8);
    private static readonly uint FourCcFrma = BinaryPrimitives.ReadUInt32BigEndian("frma"u8);
    private static readonly uint FourCcTenc = BinaryPrimitives.ReadUInt32BigEndian("tenc"u8);
    private static readonly uint FourCcEncv = BinaryPrimitives.ReadUInt32BigEndian("encv"u8);
    private static readonly uint FourCcEnca = BinaryPrimitives.ReadUInt32BigEndian("enca"u8);
    private static readonly uint FourCcEncs = BinaryPrimitives.ReadUInt32BigEndian("encs"u8);
    private static readonly uint FourCcEnct = BinaryPrimitives.ReadUInt32BigEndian("enct"u8);
    private static readonly uint FourCcFree = BinaryPrimitives.ReadUInt32BigEndian("free"u8);

    /// <summary>
    /// 对指定的文件流就地执行 CENC 解密.
    /// 针对 FileStream 采用 MemoryMappedFile 零拷贝 I/O；针对普通流自动回退至池化流式模式.
    /// </summary>
    /// <param name="stream">可读写的视频文件流.</param>
    /// <param name="key">16 字节 AES-128 密钥.</param>
    /// <param name="cancellationToken">取消令牌.</param>
    public static void DecryptInPlace(
        FileStream stream,
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

        if (key.Length != 16)
        {
            throw new ArgumentException("CENC AES-128 密钥长度必须为 16 字节.", nameof(key));
        }

        // 文件长度小于 2GB 时，使用 MemoryMappedFile 进行全内存零拷贝就地解密
        if (stream.Length > 0 && stream.Length <= int.MaxValue)
        {
            DecryptFileStreamViaMmf(stream, key, cancellationToken);
            return;
        }

        // 通用回退流式轨道（支持大于 2GB 的超大流）
        DecryptStreamFallback(stream, key, cancellationToken);
    }

    private static void DecryptFileStreamViaMmf(
        FileStream fs,
        byte[] key,
        CancellationToken cancellationToken
    )
    {
        using MemoryMappedFile mmf = MemoryMappedFile.CreateFromFile(
            fs,
            mapName: null,
            capacity: 0,
            MemoryMappedFileAccess.ReadWrite,
            HandleInheritability.None,
            leaveOpen: true
        );

        using MemoryMappedViewAccessor accessor = mmf.CreateViewAccessor(
            0,
            fs.Length,
            MemoryMappedFileAccess.ReadWrite
        );

        byte* ptr = null;
        accessor.SafeMemoryMappedViewHandle.AcquirePointer(ref ptr);
        try
        {
            Span<byte> fileSpan = new(ptr, (int)fs.Length);
            DecryptSpan(fileSpan, key, cancellationToken);
        }
        finally
        {
            accessor.SafeMemoryMappedViewHandle.ReleasePointer();
        }
    }

    private static void DecryptSpan(
        Span<byte> fileSpan,
        byte[] key,
        CancellationToken cancellationToken
    )
    {
        using Aes aes = Aes.Create();
        aes.Mode = CipherMode.ECB;
        aes.Padding = PaddingMode.None;
        aes.Key = key;

        if (!TryLocateMoov(fileSpan, out int moovOffset, out int moovSize))
        {
            throw new InvalidDataException("MP4 格式解析失败: 未找到 moov box.");
        }

        ReadOnlySpan<byte> moovData = fileSpan.Slice(moovOffset, moovSize);
        List<TrackInfoInternal> tracks = ParseMoov(moovData);
        if (tracks.Count == 0)
        {
            return;
        }

        foreach (TrackInfoInternal track in tracks)
        {
            cancellationToken.ThrowIfCancellationRequested();

            List<SampleInfoInternal> samples = track.Samples;
            ReadOnlySpan<SubsampleInfo> subsamples = CollectionsMarshal.AsSpan(track.Subsamples);

            for (int i = 0; i < samples.Count; i++)
            {
                if ((i & 0x7F) == 0)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                }

                ref readonly SampleInfoInternal sample = ref CollectionsMarshal.AsSpan(samples)[i];
                if (sample.Size == 0)
                {
                    continue;
                }

                Span<byte> sampleData = fileSpan.Slice((int)sample.Offset, (int)sample.Size);
                ulong counterHigh = sample.IvHigh;
                ulong counterLow = sample.IvLow;

                if (sample.SubsampleCount > 0)
                {
                    ReadOnlySpan<SubsampleInfo> subs = subsamples.Slice(
                        sample.SubsampleStart,
                        sample.SubsampleCount
                    );
                    int bufOffset = 0;
                    for (int s = 0; s < subs.Length; s++)
                    {
                        ref readonly SubsampleInfo sub = ref subs[s];
                        bufOffset += sub.BytesOfClearData;
                        int encBytes = (int)sub.BytesOfEncryptedData;
                        if (encBytes > 0)
                        {
                            DecryptBytesOptimized(
                                aes,
                                sampleData.Slice(bufOffset, encBytes),
                                ref counterHigh,
                                ref counterLow
                            );
                            bufOffset += encBytes;
                        }
                    }
                }
                else
                {
                    DecryptBytesOptimized(aes, sampleData, ref counterHigh, ref counterLow);
                }
            }

            // 回写并清洗 moov 元数据
            if (track.EncFormatOffsetInMoov >= 0 && track.OriginalFormatFourCc != 0)
            {
                BinaryPrimitives.WriteUInt32BigEndian(
                    fileSpan.Slice(moovOffset + track.EncFormatOffsetInMoov, 4),
                    track.OriginalFormatFourCc
                );
            }

            if (track.SinfOffsetInMoov >= 0)
            {
                BinaryPrimitives.WriteUInt32BigEndian(
                    fileSpan.Slice(moovOffset + track.SinfOffsetInMoov + 4, 4),
                    FourCcFree
                );
            }

            if (track.SencOffsetInMoov >= 0)
            {
                BinaryPrimitives.WriteUInt32BigEndian(
                    fileSpan.Slice(moovOffset + track.SencOffsetInMoov + 4, 4),
                    FourCcFree
                );
            }

            if (track.SaioOffsetInMoov >= 0)
            {
                BinaryPrimitives.WriteUInt32BigEndian(
                    fileSpan.Slice(moovOffset + track.SaioOffsetInMoov + 4, 4),
                    FourCcFree
                );
            }

            if (track.SaizOffsetInMoov >= 0)
            {
                BinaryPrimitives.WriteUInt32BigEndian(
                    fileSpan.Slice(moovOffset + track.SaizOffsetInMoov + 4, 4),
                    FourCcFree
                );
            }
        }
    }

    private static void DecryptStreamFallback(
        FileStream stream,
        byte[] key,
        CancellationToken cancellationToken
    )
    {
        using Aes aes = Aes.Create();
        aes.Mode = CipherMode.ECB;
        aes.Padding = PaddingMode.None;
        aes.Key = key;

        // 1. 定位 moov box
        long moovOffset = -1;
        long moovSize = -1;

        stream.Seek(0, SeekOrigin.Begin);
        byte[] hdr = new byte[16];
        while (stream.Position + 8 <= stream.Length)
        {
            cancellationToken.ThrowIfCancellationRequested();
            long boxStart = stream.Position;
            stream.ReadExactly(hdr.AsSpan(0, 8));
            uint size32 = BinaryPrimitives.ReadUInt32BigEndian(hdr.AsSpan(0, 4));
            uint type = BinaryPrimitives.ReadUInt32BigEndian(hdr.AsSpan(4, 4));

            long boxSize = size32;
            if (size32 == 1)
            {
                stream.ReadExactly(hdr.AsSpan(8, 8));
                boxSize = (long)BinaryPrimitives.ReadUInt64BigEndian(hdr.AsSpan(8, 8));
            }
            else if (size32 == 0)
            {
                boxSize = stream.Length - boxStart;
            }

            if (type == FourCcMoov)
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

        byte[] moovData = ArrayPool<byte>.Shared.Rent((int)moovSize);
        List<TrackInfoInternal> tracks;
        try
        {
            stream.Seek(moovOffset, SeekOrigin.Begin);
            stream.ReadExactly(moovData, 0, (int)moovSize);
            tracks = ParseMoov(moovData.AsSpan(0, (int)moovSize));
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(moovData);
        }

        if (tracks.Count == 0)
        {
            return;
        }

        byte[] sampleBuffer = [];
        Span<byte> tagBuf = stackalloc byte[4];
        try
        {
            foreach (TrackInfoInternal track in tracks)
            {
                cancellationToken.ThrowIfCancellationRequested();

                List<SampleInfoInternal> samples = track.Samples;
                ReadOnlySpan<SubsampleInfo> subsamples = CollectionsMarshal.AsSpan(
                    track.Subsamples
                );

                for (int i = 0; i < samples.Count; i++)
                {
                    if ((i & 0x7F) == 0)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                    }

                    ref readonly SampleInfoInternal sample = ref CollectionsMarshal.AsSpan(samples)[
                        i
                    ];
                    if (sample.Size == 0)
                    {
                        continue;
                    }

                    if (sampleBuffer.Length < sample.Size)
                    {
                        if (sampleBuffer.Length > 0)
                        {
                            ArrayPool<byte>.Shared.Return(sampleBuffer);
                        }
                        sampleBuffer = ArrayPool<byte>.Shared.Rent((int)sample.Size);
                    }

                    stream.Seek(sample.Offset, SeekOrigin.Begin);
                    stream.ReadExactly(sampleBuffer, 0, (int)sample.Size);

                    ulong counterHigh = sample.IvHigh;
                    ulong counterLow = sample.IvLow;

                    Span<byte> sampleSpan = sampleBuffer.AsSpan(0, (int)sample.Size);
                    if (sample.SubsampleCount > 0)
                    {
                        ReadOnlySpan<SubsampleInfo> subs = subsamples.Slice(
                            sample.SubsampleStart,
                            sample.SubsampleCount
                        );
                        int bufOffset = 0;
                        for (int s = 0; s < subs.Length; s++)
                        {
                            ref readonly SubsampleInfo sub = ref subs[s];
                            bufOffset += sub.BytesOfClearData;
                            int encBytes = (int)sub.BytesOfEncryptedData;
                            if (encBytes > 0)
                            {
                                DecryptBytesOptimized(
                                    aes,
                                    sampleSpan.Slice(bufOffset, encBytes),
                                    ref counterHigh,
                                    ref counterLow
                                );
                                bufOffset += encBytes;
                            }
                        }
                    }
                    else
                    {
                        DecryptBytesOptimized(aes, sampleSpan, ref counterHigh, ref counterLow);
                    }

                    stream.Seek(sample.Offset, SeekOrigin.Begin);
                    stream.Write(sampleBuffer, 0, (int)sample.Size);
                }

                // 回写 moov 元数据
                if (track.EncFormatOffsetInMoov >= 0 && track.OriginalFormatFourCc != 0)
                {
                    BinaryPrimitives.WriteUInt32BigEndian(tagBuf, track.OriginalFormatFourCc);
                    stream.Seek(moovOffset + track.EncFormatOffsetInMoov, SeekOrigin.Begin);
                    stream.Write(tagBuf);
                }

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
        finally
        {
            if (sampleBuffer.Length > 0)
            {
                ArrayPool<byte>.Shared.Return(sampleBuffer);
            }
        }
    }

    private static bool TryLocateMoov(ReadOnlySpan<byte> data, out int moovOffset, out int moovSize)
    {
        moovOffset = -1;
        moovSize = -1;
        int offset = 0;
        int len = data.Length;

        while (offset + 8 <= len)
        {
            uint size32 = BinaryPrimitives.ReadUInt32BigEndian(data.Slice(offset, 4));
            uint type = BinaryPrimitives.ReadUInt32BigEndian(data.Slice(offset + 4, 4));

            long boxSize = size32;
            int headerSize = 8;
            if (size32 == 1)
            {
                if (offset + 16 > len)
                {
                    break;
                }
                boxSize = (long)BinaryPrimitives.ReadUInt64BigEndian(data.Slice(offset + 8, 8));
                headerSize = 16;
            }
            else if (size32 == 0)
            {
                boxSize = len - offset;
            }

            if (boxSize < headerSize || offset + boxSize > len)
            {
                break;
            }

            if (type == FourCcMoov)
            {
                moovOffset = offset;
                moovSize = (int)boxSize;
                return true;
            }

            offset += (int)boxSize;
        }

        return false;
    }

    private static List<TrackInfoInternal> ParseMoov(ReadOnlySpan<byte> moovData)
    {
        List<TrackInfoInternal> tracks = [];
        int offset = 8;
        if (BinaryPrimitives.ReadUInt32BigEndian(moovData[..4]) == 1)
        {
            offset = 16;
        }

        while (offset + 8 <= moovData.Length)
        {
            (uint type, int boxSize) = ReadBoxHeader(moovData, offset);
            if (boxSize <= 0 || offset + boxSize > moovData.Length)
            {
                break;
            }

            if (type == FourCcTrak)
            {
                TrackInfoInternal? t = ParseTrak(moovData, offset, boxSize);
                if (t is not null && t.IsProtected)
                {
                    tracks.Add(t);
                }
            }
            offset += boxSize;
        }
        return tracks;
    }

    private static TrackInfoInternal? ParseTrak(
        ReadOnlySpan<byte> data,
        int trakOffset,
        int trakSize
    )
    {
        int mdiaOffset = FindSubBox(data, trakOffset + 8, trakOffset + trakSize, FourCcMdia);
        if (mdiaOffset < 0)
        {
            return null;
        }

        (uint _, int mdiaSize) = ReadBoxHeader(data, mdiaOffset);

        int minfOffset = FindSubBox(data, mdiaOffset + 8, mdiaOffset + mdiaSize, FourCcMinf);
        if (minfOffset < 0)
        {
            return null;
        }

        (uint _, int minfSize) = ReadBoxHeader(data, minfOffset);

        int stblOffset = FindSubBox(data, minfOffset + 8, minfOffset + minfSize, FourCcStbl);
        if (stblOffset < 0)
        {
            return null;
        }

        (uint _, int stblSize) = ReadBoxHeader(data, stblOffset);
        int stblEnd = stblOffset + stblSize;

        int stsdOffset = FindSubBox(data, stblOffset + 8, stblEnd, FourCcStsd);
        if (stsdOffset < 0)
        {
            return null;
        }

        int sampleEntryOffset = stsdOffset + 16;
        if (sampleEntryOffset + 8 > stblEnd)
        {
            return null;
        }

        (uint entryFourCc, int entrySize) = ReadBoxHeader(data, sampleEntryOffset);
        bool isProtected =
            entryFourCc == FourCcEncv
            || entryFourCc == FourCcEnca
            || entryFourCc == FourCcEncs
            || entryFourCc == FourCcEnct;
        if (!isProtected)
        {
            return null;
        }

        int encFormatOffsetInMoov = sampleEntryOffset + 4;
        uint originalFormatFourCc = 0;
        int sinfOffsetInMoov = -1;
        int defaultIvSize = 8;

        int sinfOffset = ScanForBox(
            data,
            sampleEntryOffset + 8,
            sampleEntryOffset + entrySize,
            "sinf"u8
        );
        if (sinfOffset >= 0)
        {
            sinfOffsetInMoov = sinfOffset;
            (uint _, int sinfSize) = ReadBoxHeader(data, sinfOffset);
            int sinfEnd = sinfOffset + sinfSize;

            int frmaOffset = ScanForBox(data, sinfOffset + 8, sinfEnd, "frma"u8);
            if (frmaOffset >= 0)
            {
                originalFormatFourCc = BinaryPrimitives.ReadUInt32BigEndian(
                    data.Slice(frmaOffset + 8, 4)
                );
            }

            int tencOffset = ScanForBox(data, sinfOffset + 8, sinfEnd, "tenc"u8);
            if (tencOffset >= 0)
            {
                defaultIvSize = data[tencOffset + 8 + 7];
                if (defaultIvSize == 0)
                {
                    defaultIvSize = 8;
                }
            }
        }

        int stszOffset = FindSubBox(data, stblOffset + 8, stblEnd, FourCcStsz);
        int stscOffset = FindSubBox(data, stblOffset + 8, stblEnd, FourCcStsc);
        int stcoOffset = FindSubBox(data, stblOffset + 8, stblEnd, FourCcStco);
        int co64Offset = FindSubBox(data, stblOffset + 8, stblEnd, FourCcCo64);
        int sencOffset = FindSubBox(data, stblOffset + 8, stblEnd, FourCcSenc);
        int saioOffset = FindSubBox(data, stblOffset + 8, stblEnd, FourCcSaio);
        int saizOffset = FindSubBox(data, stblOffset + 8, stblEnd, FourCcSaiz);

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
        uint defSize = BinaryPrimitives.ReadUInt32BigEndian(data.Slice(stszPayload + 4, 4));
        uint sampleCount = BinaryPrimitives.ReadUInt32BigEndian(data.Slice(stszPayload + 8, 4));

        // 解析 stco / co64
        bool isCo64 = co64Offset >= 0;
        int chunkOffsetBoxPayload = (isCo64 ? co64Offset : stcoOffset) + 8;
        uint chunkCount = BinaryPrimitives.ReadUInt32BigEndian(
            data.Slice(chunkOffsetBoxPayload + 4, 4)
        );

        // 解析 stsc
        int stscPayload = stscOffset + 8;
        uint stscCount = BinaryPrimitives.ReadUInt32BigEndian(data.Slice(stscPayload + 4, 4));
        Span<(uint FirstChunk, uint SamplesPerChunk)> stscEntries =
            stscCount <= 256
                ? stackalloc (uint, uint)[(int)stscCount]
                : new (uint, uint)[stscCount];
        for (int i = 0; i < stscCount; i++)
        {
            stscEntries[i] = (
                BinaryPrimitives.ReadUInt32BigEndian(data.Slice(stscPayload + 8 + i * 12, 4)),
                BinaryPrimitives.ReadUInt32BigEndian(data.Slice(stscPayload + 12 + i * 12, 4))
            );
        }

        // 解析 senc
        int sencPayload = sencOffset + 8;
        byte sencFlags = data[sencPayload + 3];
        bool hasSubsamples = (sencFlags & 0x02) != 0;
        uint sencCount = BinaryPrimitives.ReadUInt32BigEndian(data.Slice(sencPayload + 4, 4));
        int sencPtr = sencPayload + 8;

        List<SampleInfoInternal> samples = [with((int)sampleCount)];
        List<SubsampleInfo> subsamples = hasSubsamples ? new((int)sampleCount * 2) : [];

        int currentSampleIdx = 0;
        int currentStscIdx = 0;
        Span<byte> ivTmp = stackalloc byte[16];

        for (int chunkIdx = 0; chunkIdx < chunkCount; chunkIdx++)
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
            long curPos = isCo64
                ? (long)
                    BinaryPrimitives.ReadUInt64BigEndian(
                        data.Slice(chunkOffsetBoxPayload + 8 + chunkIdx * 8, 8)
                    )
                : BinaryPrimitives.ReadUInt32BigEndian(
                    data.Slice(chunkOffsetBoxPayload + 8 + chunkIdx * 4, 4)
                );

            for (int s = 0; s < samplesInChunk; s++)
            {
                if (currentSampleIdx >= sampleCount)
                {
                    break;
                }

                uint size =
                    defSize != 0
                        ? defSize
                        : BinaryPrimitives.ReadUInt32BigEndian(
                            data.Slice(stszPayload + 12 + currentSampleIdx * 4, 4)
                        );

                // 解析 IV
                ulong ivHigh = 0;
                ulong ivLow = 0;
                if (defaultIvSize == 8)
                {
                    ivHigh = BinaryPrimitives.ReadUInt64BigEndian(data.Slice(sencPtr, 8));
                    sencPtr += 8;
                }
                else if (defaultIvSize == 16)
                {
                    ivHigh = BinaryPrimitives.ReadUInt64BigEndian(data.Slice(sencPtr, 8));
                    ivLow = BinaryPrimitives.ReadUInt64BigEndian(data.Slice(sencPtr + 8, 8));
                    sencPtr += 16;
                }
                else
                {
                    ivTmp.Clear();
                    data.Slice(sencPtr, defaultIvSize).CopyTo(ivTmp);
                    ivHigh = BinaryPrimitives.ReadUInt64BigEndian(ivTmp[..8]);
                    ivLow = BinaryPrimitives.ReadUInt64BigEndian(ivTmp.Slice(8, 8));
                    sencPtr += defaultIvSize;
                }

                int subStart = 0;
                int subCnt = 0;
                if (hasSubsamples)
                {
                    ushort subCount = BinaryPrimitives.ReadUInt16BigEndian(data.Slice(sencPtr, 2));
                    sencPtr += 2;
                    subStart = subsamples.Count;
                    subCnt = subCount;
                    for (int si = 0; si < subCount; si++)
                    {
                        ushort clear = BinaryPrimitives.ReadUInt16BigEndian(data.Slice(sencPtr, 2));
                        uint enc = BinaryPrimitives.ReadUInt32BigEndian(data.Slice(sencPtr + 2, 4));
                        sencPtr += 6;
                        subsamples.Add(new SubsampleInfo(clear, enc));
                    }
                }

                samples.Add(new SampleInfoInternal(curPos, size, ivHigh, ivLow, subStart, subCnt));
                curPos += size;
                currentSampleIdx++;
            }
        }

        return new TrackInfoInternal
        {
            IsProtected = true,
            OriginalFormatFourCc = originalFormatFourCc,
            DefaultIvSize = defaultIvSize,
            EncFormatOffsetInMoov = encFormatOffsetInMoov,
            SinfOffsetInMoov = sinfOffsetInMoov,
            SencOffsetInMoov = sencOffset,
            SaioOffsetInMoov = saioOffset,
            SaizOffsetInMoov = saizOffset,
            Samples = samples,
            Subsamples = subsamples,
        };
    }

    private static (uint Type, int Size) ReadBoxHeader(ReadOnlySpan<byte> data, int offset)
    {
        uint size32 = BinaryPrimitives.ReadUInt32BigEndian(data.Slice(offset, 4));
        uint type = BinaryPrimitives.ReadUInt32BigEndian(data.Slice(offset + 4, 4));
        if (size32 == 1)
        {
            long size64 = (long)BinaryPrimitives.ReadUInt64BigEndian(data.Slice(offset + 8, 8));
            return (type, (int)size64);
        }
        return (type, (int)size32);
    }

    private static int FindSubBox(ReadOnlySpan<byte> data, int start, int end, uint targetFourCc)
    {
        int offset = start;
        while (offset + 8 <= end)
        {
            (uint type, int boxSize) = ReadBoxHeader(data, offset);
            if (boxSize <= 0)
            {
                break;
            }

            if (type == targetFourCc)
            {
                return offset;
            }

            offset += boxSize;
        }
        return -1;
    }

    private static int ScanForBox(
        ReadOnlySpan<byte> data,
        int start,
        int end,
        ReadOnlySpan<byte> targetFourCc
    )
    {
        ReadOnlySpan<byte> slice = data[start..end];
        int index = slice.IndexOf(targetFourCc);
        while (index >= 0)
        {
            int boxStart = start + index - 4;
            if (boxStart >= start && boxStart + 8 <= end)
            {
                uint size = BinaryPrimitives.ReadUInt32BigEndian(data.Slice(boxStart, 4));
                if (size >= 8 && boxStart + size <= data.Length)
                {
                    return boxStart;
                }
            }
            int nextOffset = index + 4;
            if (nextOffset >= slice.Length)
            {
                break;
            }
            int nextMatch = slice[nextOffset..].IndexOf(targetFourCc);
            if (nextMatch < 0)
            {
                break;
            }
            index = nextOffset + nextMatch;
        }
        return -1;
    }

    /// <summary>
    /// AES-128-CTR 批量解密内核：单次 ECB 批量生成多块密钥流并利用 SIMD 硬件指令批量异或.
    /// </summary>
    private static void DecryptBytesOptimized(
        Aes aes,
        Span<byte> data,
        ref ulong counterHigh,
        ref ulong counterLow
    )
    {
        const int BatchBlocks = 128; // 每次批量生成 128 个 Counter 块 (2048 字节)
        const int BatchBytes = BatchBlocks * 16;

        Span<byte> counterBatch = stackalloc byte[BatchBytes];
        Span<byte> keystreamBatch = stackalloc byte[BatchBytes];

        int offset = 0;
        int remaining = data.Length;

        while (remaining > 0)
        {
            int blocksToProcess = Math.Min(BatchBlocks, (remaining + 15) / 16);
            int batchByteCount = blocksToProcess * 16;

            for (int i = 0; i < blocksToProcess; i++)
            {
                int blockOffset = i * 16;
                BinaryPrimitives.WriteUInt64BigEndian(
                    counterBatch.Slice(blockOffset, 8),
                    counterHigh
                );
                BinaryPrimitives.WriteUInt64BigEndian(
                    counterBatch.Slice(blockOffset + 8, 8),
                    counterLow + (ulong)i
                );
            }

            // 单次 P/Invoke 调用，进入 native CNG/AES-NI 流水线全速执行
            aes.EncryptEcb(
                counterBatch[..batchByteCount],
                keystreamBatch[..batchByteCount],
                PaddingMode.None
            );

            ulong newLow = counterLow + (ulong)blocksToProcess;
            if (newLow < counterLow)
            {
                counterHigh++;
            }
            counterLow = newLow;

            int bytesToXor = Math.Min(batchByteCount, remaining);
            XorSimd(data.Slice(offset, bytesToXor), keystreamBatch[..bytesToXor]);

            offset += bytesToXor;
            remaining -= bytesToXor;
        }
    }

    /// <summary>
    /// 向量化 SIMD 异或运算（AVX2: 32字节/指令，SSE2: 16字节/指令）.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void XorSimd(Span<byte> dest, ReadOnlySpan<byte> src)
    {
        ref byte pDest = ref MemoryMarshal.GetReference(dest);
        ref byte pSrc = ref MemoryMarshal.GetReference(src);
        int i = 0;
        int len = dest.Length;

        if (Vector256.IsHardwareAccelerated && len >= 32)
        {
            int limit = len - 32;
            while (i <= limit)
            {
                Vector256<byte> vDest = Vector256.LoadUnsafe(ref pDest, (nuint)i);
                Vector256<byte> vSrc = Vector256.LoadUnsafe(ref pSrc, (nuint)i);
                (vDest ^ vSrc).StoreUnsafe(ref pDest, (nuint)i);
                i += 32;
            }
        }

        if (Vector128.IsHardwareAccelerated && i <= len - 16)
        {
            Vector128<byte> vDest = Vector128.LoadUnsafe(ref pDest, (nuint)i);
            Vector128<byte> vSrc = Vector128.LoadUnsafe(ref pSrc, (nuint)i);
            (vDest ^ vSrc).StoreUnsafe(ref pDest, (nuint)i);
            i += 16;
        }

        while (i < len)
        {
            Unsafe.Add(ref pDest, i) ^= Unsafe.Add(ref pSrc, i);
            i++;
        }
    }

    /// <summary>
    /// 子样本明文/密文区间定义（值类型轻量结构）.
    /// </summary>
    public readonly record struct SubsampleInfo(ushort BytesOfClearData, uint BytesOfEncryptedData);

    private readonly record struct SampleInfoInternal(
        long Offset,
        uint Size,
        ulong IvHigh,
        ulong IvLow,
        int SubsampleStart,
        int SubsampleCount
    );

    private sealed class TrackInfoInternal
    {
        public bool IsProtected { get; set; }
        public uint OriginalFormatFourCc { get; set; }
        public int DefaultIvSize { get; set; } = 8;
        public int EncFormatOffsetInMoov { get; set; } = -1;
        public int SinfOffsetInMoov { get; set; } = -1;
        public int SencOffsetInMoov { get; set; } = -1;
        public int SaioOffsetInMoov { get; set; } = -1;
        public int SaizOffsetInMoov { get; set; } = -1;
        public List<SampleInfoInternal> Samples { get; set; } = [];
        public List<SubsampleInfo> Subsamples { get; set; } = [];
    }
}
