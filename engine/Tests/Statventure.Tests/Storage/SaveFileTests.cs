using System.Buffers.Binary;
using System.Security.Cryptography;
using Statventure.Records;
using Statventure.Storage;
using Statventure.Tests.Records;

namespace Statventure.Tests.Storage;

/// <summary>
/// The save file format (Docs/Assets.md, "A save file"; Q38): the 48-byte header byte for byte, every
/// kind of damage reported (never an exception, never wrong data), a newer format or record version
/// and another record type unreadable, and the committed saves of past record versions read and
/// upgraded.
/// </summary>
public class SaveFileTests
{
    [Fact]
    public void TheHeaderIsMagicVersionLengthAndHash()
    {
        var payload = "payload bytes"u8.ToArray();
        var file = SaveFile.Wrap(payload);
        Assert.Equal(48 + payload.Length, file.Length);
        Assert.Equal("53565356" + "01000000" + "0D00000000000000", Convert.ToHexString(file, 0, 16));
        Assert.Equal(SHA256.HashData(payload), file[16..48]);
        Assert.Equal(payload, file[48..]);
        Assert.Equal(48, SaveFile.HeaderLength);
        Assert.Equal(1u, SaveFile.FormatVersion);
        // An empty payload is a header alone (and isn't a record, so it can't be read as one).
        var empty = SaveFile.Wrap([]);
        Assert.Equal(SaveState.Readable, SaveFile.Check(empty, out var range, out _));
        Assert.Equal(48..48, range);
        Assert.Equal(SaveState.Unreadable, SaveFile.Read<Creature>(empty).State);
    }

    [Fact]
    public void ASaveIsTheRecordsBinaryForm()
    {
        var file = SaveFile.Write(StorageTestData.Sample());
        Assert.Equal(RecordSerializer.ToBinary(StorageTestData.Sample()), file[48..]);
        var reading = SaveFile.Read<Creature>(file);
        Assert.Equal(SaveState.Readable, reading.State);
        Assert.Null(reading.Problem);
        RecordAssert.Equal(StorageTestData.Sample(), reading.Value);
    }

    [Fact]
    public void EveryTruncationIsDamage()
    {
        var file = SaveFile.Write(StorageTestData.Sample());
        for (var length = 0; length < file.Length; length++)
        {
            var reading = SaveFile.Read<Creature>(file[..length]);
            Assert.True(reading.State == SaveState.Damaged, $"{length} bytes: {reading.State}");
            Assert.NotNull(reading.Problem);
            Assert.Throws<InvalidOperationException>(() => reading.Value);
        }
        // So is a byte too many.
        Assert.Equal(SaveState.Damaged, SaveFile.Read<Creature>([.. file, 0]).State);
    }

    [Fact]
    public void EveryFlippedBitIsDamageOrANewerFormat()
    {
        var file = SaveFile.Write(StorageTestData.Sample());
        for (var bit = 0; bit < file.Length * 8; bit++)
        {
            var damaged = (byte[])file.Clone();
            damaged[bit / 8] ^= (byte)(1 << (bit % 8));
            var state = SaveFile.Read<Creature>(damaged).State;
            // The version's bits: 1 becomes 0 (damage) or a larger number (a newer format).
            var expected = bit / 8 is >= 4 and < 8 && damaged[4..8] is var v && BinaryPrimitives.ReadUInt32LittleEndian(v) > 1
                ? SaveState.Unreadable
                : SaveState.Damaged;
            Assert.True(state == expected, $"bit {bit}: {state}, expected {expected}");
        }
    }

    [Fact]
    public void EachProblemIsNamed()
    {
        var payload = RecordSerializer.ToBinary(StorageTestData.Sample());
        string Problem(byte[] file) => SaveFile.Read<Creature>(file).Problem!;
        Assert.Contains("fewer than the 48-byte header", Problem(new byte[47]));
        var file = SaveFile.Wrap(payload);
        file[0] = (byte)'X';
        Assert.Contains("doesn't start with SVSV", Problem(file));
        file = SaveFile.Wrap(payload);
        BinaryPrimitives.WriteUInt32LittleEndian(file.AsSpan(4), 2);
        Assert.Equal(SaveState.Unreadable, SaveFile.Read<Creature>(file).State);
        Assert.Contains("format version 2 is newer than this engine reads (1)", Problem(file));
        BinaryPrimitives.WriteUInt32LittleEndian(file.AsSpan(4), 0);
        Assert.Equal(SaveState.Damaged, SaveFile.Read<Creature>(file).State);
        Assert.Contains("format version is 0", Problem(file));
        file = SaveFile.Wrap(payload);
        BinaryPrimitives.WriteUInt64LittleEndian(file.AsSpan(8), (ulong)payload.Length + 1);
        Assert.Contains($"a payload of {payload.Length + 1} bytes, but {payload.Length} follow it", Problem(file));
        BinaryPrimitives.WriteUInt64LittleEndian(file.AsSpan(8), ulong.MaxValue);
        Assert.Equal(SaveState.Damaged, SaveFile.Read<Creature>(file).State);
        file = SaveFile.Wrap(payload);
        file[^1] ^= 1;
        Assert.Contains("doesn't match its SHA-256", Problem(file));
    }

    [Fact]
    public void AnIntactSaveThisEngineCantReadIsUnreadable()
    {
        // Another record type.
        var other = SaveFile.Write(new Part { Name = "horn" });
        var reading = SaveFile.Read<Creature>(other);
        Assert.Equal(SaveState.Unreadable, reading.State);
        Assert.Contains("its record can't be read", reading.Problem);
        // A record version newer than the engine's (its hash made to match).
        var payload = RecordSerializer.ToBinary(StorageTestData.Sample());
        var version = Array.IndexOf(payload, (byte)3, 1 + 1 + "test.creature".Length);
        Assert.Equal(1 + 1 + "test.creature".Length, version);
        payload[version] = 4;
        reading = SaveFile.Read<Creature>(SaveFile.Wrap(payload));
        Assert.Equal(SaveState.Unreadable, reading.State);
        Assert.Contains("newer than this engine reads", reading.Problem);
    }

    [Fact]
    public void FilesAreReadFromTheDisk()
    {
        using var temp = new StorageFolder();
        Assert.Equal(SaveState.Missing, SaveFile.Read<Creature>(temp["none.dat"]).State);
        Assert.Equal(SaveState.Missing, SaveFile.Read<Creature>(temp["no/such/folder.dat"]).State);
        Assert.Null(SaveFile.Read<Creature>(temp["none.dat"]).Problem);
        var file = temp.Write("one.dat", SaveFile.Write(StorageTestData.Sample()));
        RecordAssert.Equal(StorageTestData.Sample(), SaveFile.Read<Creature>(file).Value);
        // A folder where the file should be can't be read.
        Directory.CreateDirectory(temp["folder.dat"]);
        Assert.Equal(SaveState.Unreadable, SaveFile.Read<Creature>(temp["folder.dat"]).State);
    }

    public static TheoryData<string> Labels()
    {
        var data = new TheoryData<string>();
        foreach (var version in StorageTestData.Versions)
            data.Add(version.Label);
        return data;
    }

    [Fact]
    public void TheOldVersionSavesAreRecorded()
    {
        foreach (var (label, versions, _) in StorageTestData.Versions)
            StorageTestData.Ensure($"Saves/{label}.dat", () => SaveFile.Wrap(StorageTestData.Binary(versions)));
    }

    [Theory]
    [MemberData(nameof(Labels))]
    public void ASaveOfAPastVersionIsUpgraded(string label)
    {
        var (_, versions, expected) = StorageTestData.Versions.Single(v => v.Label == label);
        var file = StorageTestData.Read($"Saves/{label}.dat");
        var reading = SaveFile.Read<Creature>(file);
        Assert.Equal(SaveState.Readable, reading.State);
        RecordAssert.Equal(expected, reading.Value);
        // It holds the versions it was written in.
        Assert.Equal("test.creature", RecordSerializer.RootTypeName(file.AsSpan(48)));
        Assert.Equal(StorageTestData.Binary(versions), file[48..]);
        // The current version's save is written back to the same bytes.
        if (versions.Count == 0)
            Assert.Equal(file, SaveFile.Write(reading.Value));
    }
}
