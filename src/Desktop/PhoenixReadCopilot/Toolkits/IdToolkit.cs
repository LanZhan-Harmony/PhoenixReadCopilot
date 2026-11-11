namespace PhoenixReadCopilot.Toolkits;

/// <summary>
/// 视频 ID 工具箱.
/// </summary>
public static class IdToolkit
{
    private const long XOR_CODE = 23442827791579L;
    private const long MASK_CODE = 2251799813685247L;
    private const long MAX_AID = 1L << 51;
    private const long BASE = 58L;

    private const string DATA = "FcwAPNKTMug3GV5Lj7EJnHpWsx4tb8haYeviqBz6rkCy12mUSDQX9RdoZf";

    /// <summary>
    /// 将 AV 号转换为 BV 号.
    /// </summary>
    /// <param name="aid">AID.</param>
    /// <returns>BVId.</returns>
    public static string Av2Bv(long aid)
    {
        char[] bytes = ['B', 'V', '1', '0', '0', '0', '0', '0', '0', '0', '0', '0', '0'];
        int bvIndex = bytes.Length - 1;
        long tmp = (MAX_AID | aid) ^ XOR_CODE;

        while (tmp > 0)
        {
            bytes[bvIndex] = DATA[(int)(tmp % BASE)];
            tmp /= BASE;
            bvIndex--;
        }

        Swap(ref bytes[3], ref bytes[9]);
        Swap(ref bytes[4], ref bytes[7]);

        return new string(bytes);
    }

    /// <summary>
    /// 将 BV 号转换为 AV 号.
    /// </summary>
    /// <param name="bvid">BV Id.</param>
    /// <returns>AVId.</returns>
    public static long Bv2Av(string bvid)
    {
        char[] bvidArr =
        [
            .. bvid,
        ];
        Swap(ref bvidArr[3], ref bvidArr[9]);
        Swap(ref bvidArr[4], ref bvidArr[7]);
        bvidArr =
        [
            .. bvidArr.Skip(3),
        ];

        long tmp = bvidArr.Aggregate(0L, (pre, bvidChar) => (pre * BASE) + DATA.IndexOf(bvidChar));

        return (tmp & MASK_CODE) ^ XOR_CODE;
    }

    private static void Swap(ref char a, ref char b) => (b, a) = (a, b);
}
