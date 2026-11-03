using System.Buffers.Binary;
using System.Runtime.InteropServices;
using System.Text;

namespace Statventure.Records;

// The binary form's primitives (Docs/Assets.md, "Records (the serializer)"): lengths and counts as
// variable-length integers (LEB128: 7 bits a byte, low first, the high bit set on all but the last;
// only the shortest encoding is valid), numbers fixed-width little-endian, floats bit for bit,
// strings as their UTF-8 byte count and bytes, arrays of numbers and vectors as one block.

/// <summary>Writes a record's binary form into a growing buffer.</summary>
sealed class BinaryRecordWriter(WriteSession session)
{
    static readonly UTF8Encoding Utf8 = new(false, true);

    byte[] _buffer = new byte[256];
    int _length;

    public WriteSession Session { get; } = session;
    public ReadOnlySpan<byte> Written => _buffer.AsSpan(0, _length);

    Span<byte> Reserve(int count)
    {
        if (_length + count > _buffer.Length)
            Array.Resize(ref _buffer, Math.Max(_buffer.Length * 2, _length + count));
        var span = _buffer.AsSpan(_length, count);
        _length += count;
        return span;
    }

    public void WriteByte(byte value) => Reserve(1)[0] = value;

    public void WriteVarUInt(ulong value)
    {
        while (value >= 0x80)
        {
            WriteByte((byte)(value | 0x80));
            value >>= 7;
        }
        WriteByte((byte)value);
    }

    public void WriteCount(int count) => WriteVarUInt((ulong)count);

    public void WriteBool(bool value) => WriteByte(value ? (byte)1 : (byte)0);

    public void WriteInt16(short value) => BinaryPrimitives.WriteInt16LittleEndian(Reserve(2), value);
    public void WriteUInt16(ushort value) => BinaryPrimitives.WriteUInt16LittleEndian(Reserve(2), value);
    public void WriteInt32(int value) => BinaryPrimitives.WriteInt32LittleEndian(Reserve(4), value);
    public void WriteUInt32(uint value) => BinaryPrimitives.WriteUInt32LittleEndian(Reserve(4), value);
    public void WriteInt64(long value) => BinaryPrimitives.WriteInt64LittleEndian(Reserve(8), value);
    public void WriteUInt64(ulong value) => BinaryPrimitives.WriteUInt64LittleEndian(Reserve(8), value);
    // Through the bits, so a NaN keeps its payload.
    public void WriteSingle(float value) => WriteInt32(BitConverter.SingleToInt32Bits(value));
    public void WriteDouble(double value) => WriteInt64(BitConverter.DoubleToInt64Bits(value));

    public void WriteString(string value)
    {
        int count;
        try
        {
            count = Utf8.GetByteCount(value);
        }
        catch (EncoderFallbackException)
        {
            throw new RecordFormatException("the string isn't valid UTF-16 (a lone surrogate)");
        }
        WriteCount(count);
        Utf8.GetBytes(value, Reserve(count));
    }

    /// <summary>Elements of a fixed-size type of numbers, one after another, each little-endian.</summary>
    public void WriteBlock<T>(ReadOnlySpan<T> values) where T : unmanaged
    {
        WriteCount(values.Length);
        MemoryMarshal.AsBytes(values).CopyTo(Reserve(values.Length * BlockSize<T>()));
    }

    internal static int BlockSize<T>() where T : unmanaged
    {
        // The engine runs on little-endian machines only (x64, arm64): a block is the memory itself.
        if (!BitConverter.IsLittleEndian)
            throw new PlatformNotSupportedException("records need a little-endian machine");
        return Marshal.SizeOf<T>();
    }
}

/// <summary>Reads a record's binary form, refusing anything that doesn't fit with a <see cref="RecordFormatException"/>.</summary>
ref struct BinaryRecordReader(ReadOnlySpan<byte> data, ReadSession session)
{
    static readonly UTF8Encoding Utf8 = new(false, true);

    readonly ReadOnlySpan<byte> _data = data;
    int _position;

    public ReadSession Session = session;
    /// <summary>How many records deep the read is (<see cref="RecordFamily.MaxDepth"/>).</summary>
    public int Depth;
    public readonly int Position => _position;
    public readonly int Remaining => _data.Length - _position;

    RecordFormatException Ends() => new($"the data ends early (byte {_data.Length} of at least {_position + 1})");

    public ReadOnlySpan<byte> Take(int count)
    {
        if (count > Remaining)
        {
            _position = _data.Length;
            throw new RecordFormatException($"the data ends early (byte {_data.Length})");
        }
        var span = _data.Slice(_position, count);
        _position += count;
        return span;
    }

    public byte ReadByte()
    {
        if (_position >= _data.Length)
            throw Ends();
        return _data[_position++];
    }

    public ulong ReadVarUInt()
    {
        var start = _position;
        ulong value = 0;
        for (var shift = 0; ; shift += 7)
        {
            var b = ReadByte();
            if (shift == 63 && b > 1)
                throw new RecordFormatException($"a variable-length integer at byte {start} is too long");
            value |= (ulong)(b & 0x7F) << shift;
            if (b < 0x80)
            {
                if (b == 0 && shift > 0)
                    throw new RecordFormatException($"a variable-length integer at byte {start} isn't in its shortest form");
                return value;
            }
        }
    }

    /// <summary>A count of elements that take at least a byte each: no more than the bytes left.</summary>
    public int ReadCount()
    {
        var start = _position;
        var count = ReadVarUInt();
        if (count > (ulong)Remaining)
            throw new RecordFormatException($"a count at byte {start} ({count}) is more than the {Remaining} bytes left");
        return (int)count;
    }

    public int ReadVarInt32(string what)
    {
        var start = _position;
        var value = ReadVarUInt();
        if (value > int.MaxValue)
            throw new RecordFormatException($"{what} at byte {start} is out of range ({value})");
        return (int)value;
    }

    byte ReadFlag(string what)
    {
        var start = _position;
        var b = ReadByte();
        if (b > 1)
            throw new RecordFormatException($"{what} at byte {start} is {b}, not 0 or 1");
        return b;
    }

    public bool ReadBool() => ReadFlag("a bool") == 1;

    /// <summary>The presence byte before a value that may be null: true when the value follows.</summary>
    public bool ReadPresence() => ReadFlag("a presence byte") == 1;

    public short ReadInt16() => BinaryPrimitives.ReadInt16LittleEndian(Take(2));
    public ushort ReadUInt16() => BinaryPrimitives.ReadUInt16LittleEndian(Take(2));
    public int ReadInt32() => BinaryPrimitives.ReadInt32LittleEndian(Take(4));
    public uint ReadUInt32() => BinaryPrimitives.ReadUInt32LittleEndian(Take(4));
    public long ReadInt64() => BinaryPrimitives.ReadInt64LittleEndian(Take(8));
    public ulong ReadUInt64() => BinaryPrimitives.ReadUInt64LittleEndian(Take(8));
    public float ReadSingle() => BitConverter.Int32BitsToSingle(ReadInt32());
    public double ReadDouble() => BitConverter.Int64BitsToDouble(ReadInt64());

    public string ReadString()
    {
        var start = _position;
        var bytes = Take(ReadCount());
        try
        {
            return Utf8.GetString(bytes);
        }
        catch (DecoderFallbackException)
        {
            throw new RecordFormatException($"the string at byte {start} isn't valid UTF-8");
        }
    }

    public T[] ReadBlock<T>() where T : unmanaged
    {
        var start = _position;
        var size = BinaryRecordWriter.BlockSize<T>();
        var count = ReadVarUInt();
        if (count > (ulong)(Remaining / size))
            throw new RecordFormatException($"a block at byte {start} ({count} × {size} bytes) is longer than the {Remaining} bytes left");
        var values = new T[(int)count];
        Take(values.Length * size).CopyTo(MemoryMarshal.AsBytes(values.AsSpan()));
        return values;
    }
}
