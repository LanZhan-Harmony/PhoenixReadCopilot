namespace Danmaku.Core;

internal sealed class DanmakuYSlotManager
{
    private readonly Lock _lock = new();
    private readonly Random _random;
    private Slot[] _ySlotArray;

    public DanmakuYSlotManager(uint length)
    {
        _ySlotArray = new Slot[length];
        _random = new Random();
    }

    public void UpdateLength(uint newLength)
    {
        lock (_lock)
        {
            _ySlotArray = new Slot[newLength];
        }
    }

    /// <summary>
    /// Get an available Y position for Danmaku.
    /// </summary>
    /// <returns>Is any slot occupied.</returns>
    public bool GetY(uint danmakuId, uint height, out uint y)
    {
        lock (_lock)
        {
            if (height > _ySlotArray.Length)
            {
                // Danmaku's height is larger than total available height
                y = 0;
                return false;
            }

            uint index = 0;
            while (index + height < _ySlotArray.Length)
            {
                var found = true;
                for (uint i = 0; i < height; i++)
                {
                    if (_ySlotArray[index + i].Length > 0)
                    {
                        // Move to next available slot
                        found = false;
                        index = index + i + _ySlotArray[index + i].Length;
                        break;
                    }
                }

                if (found)
                {
                    _ySlotArray[index].Id = danmakuId;
                    _ySlotArray[index].Length = height;
                    y = index;
                    return true;
                }
            }

            // Can't find available slot, then return a random Y.
            y = (uint)_random.Next(0, Math.Max(1, _ySlotArray.Length - (int)height));
            return false;
        }
    }

    public void ReleaseYSlot(uint danmakuId, uint y)
    {
        lock (_lock)
        {
            if (y < _ySlotArray.Length && _ySlotArray[y].Id == danmakuId)
            {
                _ySlotArray[y].Id = 0;
                _ySlotArray[y].Length = 0;
            }
        }
    }

    /// <summary>
    /// Thread safe.
    /// </summary>
    public void Clear()
    {
        lock (_lock)
        {
            for (var i = 0; i < _ySlotArray.Length; i++)
            {
                _ySlotArray[i].Id = 0;
                _ySlotArray[i].Length = 0;
            }
        }
    }

    private struct Slot
    {
        public uint Id;
        public uint Length;
    }
}
