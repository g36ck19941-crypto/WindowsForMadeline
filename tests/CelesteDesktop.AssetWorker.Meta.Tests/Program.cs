using System.Text;
using CelesteDesktop.AssetWorker.Meta;
using CelesteDesktop.Contracts.Assets;

var tests = new (string Name, Action Body)[]
{
    ("valid metadata produces immutable descriptors", ValidMetadataProducesDescriptors),
    ("backslash paths are canonicalized", BackslashPathsAreCanonicalized),
    ("multiple pages preserve page indices", MultiplePagesPreserveIndices),
    ("non-seekable input is supported", NonSeekableInputIsSupported),
    ("descriptor collections reject mutation", DescriptorCollectionsRejectMutation),
    ("public reader surface accepts streams only", PublicReaderSurfaceAcceptsStreamsOnly),
    ("truncated header is rejected", TruncatedHeaderIsRejected),
    ("truncated entry is rejected", TruncatedEntryIsRejected),
    ("input byte budget is enforced", InputByteBudgetIsEnforced),
    ("negative format version is rejected", NegativeFormatVersionIsRejected),
    ("blank packer is rejected", BlankPackerIsRejected),
    ("negative packer version is rejected", NegativePackerVersionIsRejected),
    ("zero pages are rejected", ZeroPagesAreRejected),
    ("page count budget is enforced", PageCountBudgetIsEnforced),
    ("absolute page path is rejected", AbsolutePagePathIsRejected),
    ("page traversal is rejected", PageTraversalIsRejected),
    ("duplicate pages are rejected case-insensitively", DuplicatePagesAreRejected),
    ("negative entry count is rejected", NegativeEntryCountIsRejected),
    ("total entry budget is enforced", TotalEntryBudgetIsEnforced),
    ("entry traversal is rejected", EntryTraversalIsRejected),
    ("duplicate canonical entries are rejected", DuplicateEntriesAreRejected),
    ("Unicode-equivalent entries are rejected", UnicodeEquivalentEntriesAreRejected),
    ("invalid UTF-8 is rejected", InvalidUtf8IsRejected),
    ("overlong string length is rejected", OverlongStringLengthIsRejected),
    ("string byte budget is enforced", StringByteBudgetIsEnforced),
    ("negative rectangle coordinate is rejected", NegativeRectangleIsRejected),
    ("atlas extent overflow is rejected", AtlasExtentOverflowIsRejected),
    ("positive stored trim offset is rejected", PositiveStoredTrimOffsetIsRejected),
    ("minimum stored trim offset is rejected", MinimumStoredTrimOffsetIsRejected),
    ("trim outside frame is rejected", TrimOutsideFrameIsRejected),
    ("zero frame dimension is rejected", ZeroFrameDimensionIsRejected),
    ("trailing data is rejected", TrailingDataIsRejected)
};

var failed = 0;
foreach (var test in tests)
{
    try
    {
        test.Body();
        Console.WriteLine($"PASS {test.Name}");
    }
    catch (Exception exception)
    {
        failed++;
        Console.Error.WriteLine($"FAIL {test.Name}");
        Console.Error.WriteLine(exception);
    }
}

Console.WriteLine($"RESULT total={tests.Length} passed={tests.Length - failed} failed={failed}");
return failed == 0 ? 0 : 1;

static void ValidMetadataProducesDescriptors()
{
    var metadata = Read(WriteMeta(Page("Gameplay0", Entry("characters/player/idle00"))));

    Assert(metadata.FormatVersion == 1, "Unexpected format version.");
    Assert(metadata.Packer == "Crunch", "Unexpected packer.");
    Assert(metadata.PackerVersion == 7, "Unexpected packer version.");
    Assert(metadata.Pages.Count == 1, "Unexpected page count.");
    var entry = metadata.Pages[0].Entries[0];
    Assert(entry.PageIndex == 0, "Unexpected page index.");
    Assert(entry.TrimOffsetX == 2 && entry.TrimOffsetY == 3, "Trim was not normalized.");
    Assert(entry.FrameWidth == 12 && entry.FrameHeight == 14, "Unexpected frame.");
}

static void BackslashPathsAreCanonicalized()
{
    var metadata = Read(WriteMeta(
        Page("pages\\Gameplay0", Entry("characters\\player\\idle00"))));

    Assert(metadata.Pages[0].DataPath == "pages/Gameplay0", "Page path was not canonicalized.");
    Assert(
        metadata.Pages[0].Entries[0].Id == "characters/player/idle00",
        "Entry path was not canonicalized.");
}

static void MultiplePagesPreserveIndices()
{
    var metadata = Read(WriteMeta(
        Page("Gameplay0", Entry("characters/player/idle00")),
        Page("Gameplay1", Entry("objects/spring00"))));

    Assert(metadata.Pages.Count == 2, "Unexpected page count.");
    Assert(metadata.Pages[1].Entries[0].PageIndex == 1, "Second page index was lost.");
}

static void NonSeekableInputIsSupported()
{
    using var input = new NonSeekableReadStream(WriteMeta(
        Page("Gameplay0", Entry("characters/player/idle00"))));

    var metadata = new AtlasMetadataReader().Read(input);

    Assert(metadata.Pages.Count == 1, "Non-seekable input did not parse.");
}

static void DescriptorCollectionsRejectMutation()
{
    var metadata = Read(WriteMeta(Page("Gameplay0", Entry("characters/player/idle00"))));
    var pages = (IList<AtlasPageDescriptor>)metadata.Pages;

    try
    {
        pages.Add(metadata.Pages[0]);
    }
    catch (NotSupportedException)
    {
        return;
    }

    throw new InvalidOperationException("Descriptor page collection was mutable.");
}

static void PublicReaderSurfaceAcceptsStreamsOnly()
{
    var methods = typeof(AtlasMetadataReader)
        .GetMethods(
            System.Reflection.BindingFlags.Instance |
            System.Reflection.BindingFlags.Public |
            System.Reflection.BindingFlags.DeclaredOnly);

    Assert(methods.Length == 1, "Unexpected public reader method count.");
    var parameters = methods[0].GetParameters();
    Assert(methods[0].Name == nameof(AtlasMetadataReader.Read), "Unexpected reader method.");
    Assert(
        parameters.Length == 1 && parameters[0].ParameterType == typeof(Stream),
        "Reader exposed a non-stream input surface.");
}

static void TruncatedHeaderIsRejected() => AssertFailure(
    new byte[] { 1, 0 },
    AtlasMetadataCodes.Truncated);

static void TruncatedEntryIsRejected()
{
    var complete = WriteMeta(Page("Gameplay0", Entry("characters/player/idle00")));
    AssertFailure(complete[..^1], AtlasMetadataCodes.Truncated);
}

static void InputByteBudgetIsEnforced() => AssertFailure(
    WriteMeta(Page("Gameplay0", Entry("characters/player/idle00"))),
    AtlasMetadataCodes.InputTooLarge,
    AtlasMetadataReaderOptions.Default with { MaximumInputBytes = 4 });

static void NegativeFormatVersionIsRejected() => AssertFailure(
    WriteMetaWithHeader(-1, "Crunch", 7, Page("Gameplay0")),
    AtlasMetadataCodes.VersionInvalid);

static void BlankPackerIsRejected() => AssertFailure(
    WriteMetaWithHeader(1, " ", 7, Page("Gameplay0")),
    AtlasMetadataCodes.HeaderInvalid);

static void NegativePackerVersionIsRejected() => AssertFailure(
    WriteMetaWithHeader(1, "Crunch", -1, Page("Gameplay0")),
    AtlasMetadataCodes.VersionInvalid);

static void ZeroPagesAreRejected() => AssertFailure(
    WriteMeta(),
    AtlasMetadataCodes.PageCountInvalid);

static void PageCountBudgetIsEnforced() => AssertFailure(
    WriteMeta(Page("Gameplay0"), Page("Gameplay1")),
    AtlasMetadataCodes.PageCountInvalid,
    AtlasMetadataReaderOptions.Default with { MaximumPages = 1 });

static void AbsolutePagePathIsRejected() => AssertFailure(
    WriteMeta(Page("C:/Gameplay0")),
    AtlasMetadataCodes.PathInvalid);

static void PageTraversalIsRejected() => AssertFailure(
    WriteMeta(Page("../Gameplay0")),
    AtlasMetadataCodes.PathInvalid);

static void DuplicatePagesAreRejected() => AssertFailure(
    WriteMeta(Page("pages/Gameplay0"), Page("PAGES\\gameplay0")),
    AtlasMetadataCodes.PageDuplicate);

static void NegativeEntryCountIsRejected()
{
    using var stream = new MemoryStream();
    using (var writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true))
    {
        WriteHeader(writer, 1, "Crunch", 7, 1);
        writer.Write("Gameplay0");
        writer.Write((short)-1);
    }

    AssertFailure(stream.ToArray(), AtlasMetadataCodes.EntryCountInvalid);
}

static void TotalEntryBudgetIsEnforced() => AssertFailure(
    WriteMeta(Page(
        "Gameplay0",
        Entry("characters/player/idle00"),
        Entry("characters/player/idle01"))),
    AtlasMetadataCodes.EntryBudgetExceeded,
    AtlasMetadataReaderOptions.Default with { MaximumEntries = 1 });

static void EntryTraversalIsRejected() => AssertFailure(
    WriteMeta(Page("Gameplay0", Entry("characters/../secret"))),
    AtlasMetadataCodes.PathInvalid);

static void DuplicateEntriesAreRejected() => AssertFailure(
    WriteMeta(Page(
        "Gameplay0",
        Entry("characters\\player\\idle00"),
        Entry("CHARACTERS/player/IDLE00"))),
    AtlasMetadataCodes.EntryDuplicate);

static void UnicodeEquivalentEntriesAreRejected() => AssertFailure(
    WriteMeta(Page(
        "Gameplay0",
        Entry("characters/caf\u00E9"),
        Entry("characters/cafe\u0301"))),
    AtlasMetadataCodes.EntryDuplicate);

static void InvalidUtf8IsRejected()
{
    using var stream = new MemoryStream();
    using (var writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true))
    {
        writer.Write(1);
        writer.Write((byte)1);
        writer.Write((byte)0xFF);
    }

    AssertFailure(stream.ToArray(), AtlasMetadataCodes.Utf8Invalid);
}

static void OverlongStringLengthIsRejected()
{
    using var stream = new MemoryStream();
    using (var writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true))
    {
        writer.Write(1);
        writer.Write((byte)0x81);
        writer.Write((byte)0x00);
        writer.Write((byte)'A');
    }

    AssertFailure(stream.ToArray(), AtlasMetadataCodes.StringLengthInvalid);
}

static void StringByteBudgetIsEnforced() => AssertFailure(
    WriteMeta(Page("Gameplay0")),
    AtlasMetadataCodes.StringTooLong,
    AtlasMetadataReaderOptions.Default with { MaximumStringBytes = 3 });

static void NegativeRectangleIsRejected() => AssertFailure(
    WriteMeta(Page("Gameplay0", Entry("entry", x: -1))),
    AtlasMetadataCodes.RectangleInvalid);

static void AtlasExtentOverflowIsRejected() => AssertFailure(
    WriteMeta(Page("Gameplay0", Entry("entry", x: 16_380, width: 8))),
    AtlasMetadataCodes.RectangleInvalid);

static void PositiveStoredTrimOffsetIsRejected() => AssertFailure(
    WriteMeta(Page("Gameplay0", Entry("entry", storedOffsetX: 1))),
    AtlasMetadataCodes.TrimInvalid);

static void MinimumStoredTrimOffsetIsRejected() => AssertFailure(
    WriteMeta(Page("Gameplay0", Entry("entry", storedOffsetX: short.MinValue))),
    AtlasMetadataCodes.TrimInvalid);

static void TrimOutsideFrameIsRejected() => AssertFailure(
    WriteMeta(Page("Gameplay0", Entry(
        "entry",
        width: 10,
        storedOffsetX: -3,
        frameWidth: 12))),
    AtlasMetadataCodes.TrimInvalid);

static void ZeroFrameDimensionIsRejected() => AssertFailure(
    WriteMeta(Page("Gameplay0", Entry("entry", frameWidth: 0))),
    AtlasMetadataCodes.RectangleInvalid);

static void TrailingDataIsRejected()
{
    var bytes = WriteMeta(Page("Gameplay0"));
    Array.Resize(ref bytes, bytes.Length + 1);
    bytes[^1] = 0x5A;
    AssertFailure(bytes, AtlasMetadataCodes.TrailingData);
}

static AtlasMetadataDescriptor Read(byte[] bytes)
{
    using var stream = new MemoryStream(bytes, writable: false);
    return new AtlasMetadataReader().Read(stream);
}

static void AssertFailure(
    byte[] bytes,
    string expectedCode,
    AtlasMetadataReaderOptions? options = null)
{
    using var stream = new MemoryStream(bytes, writable: false);
    try
    {
        new AtlasMetadataReader(options).Read(stream);
    }
    catch (AtlasMetadataException exception)
    {
        Assert(
            exception.Code == expectedCode,
            $"Expected {expectedCode}, got {exception.Code}.");
        return;
    }

    throw new InvalidOperationException($"Expected metadata failure {expectedCode}.");
}

static byte[] WriteMeta(params MetaPage[] pages) =>
    WriteMetaWithHeader(1, "Crunch", 7, pages);

static byte[] WriteMetaWithHeader(
    int formatVersion,
    string packer,
    int packerVersion,
    params MetaPage[] pages)
{
    using var stream = new MemoryStream();
    using (var writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true))
    {
        WriteHeader(writer, formatVersion, packer, packerVersion, pages.Length);
        foreach (var page in pages)
        {
            writer.Write(page.Path);
            writer.Write(checked((short)page.Entries.Length));
            foreach (var entry in page.Entries)
            {
                writer.Write(entry.Id);
                writer.Write(entry.X);
                writer.Write(entry.Y);
                writer.Write(entry.Width);
                writer.Write(entry.Height);
                writer.Write(entry.StoredOffsetX);
                writer.Write(entry.StoredOffsetY);
                writer.Write(entry.FrameWidth);
                writer.Write(entry.FrameHeight);
            }
        }
    }

    return stream.ToArray();
}

static void WriteHeader(
    BinaryWriter writer,
    int formatVersion,
    string packer,
    int packerVersion,
    int pageCount)
{
    writer.Write(formatVersion);
    writer.Write(packer);
    writer.Write(packerVersion);
    writer.Write(checked((short)pageCount));
}

static MetaPage Page(string path, params MetaEntry[] entries) => new(path, entries);

static MetaEntry Entry(
    string id,
    short x = 10,
    short y = 20,
    short width = 8,
    short height = 9,
    short storedOffsetX = -2,
    short storedOffsetY = -3,
    short frameWidth = 12,
    short frameHeight = 14) => new(
        id,
        x,
        y,
        width,
        height,
        storedOffsetX,
        storedOffsetY,
        frameWidth,
        frameHeight);

static void Assert(bool condition, string message)
{
    if (!condition)
    {
        throw new InvalidOperationException(message);
    }
}

internal sealed record MetaPage(string Path, MetaEntry[] Entries);

internal sealed record MetaEntry(
    string Id,
    short X,
    short Y,
    short Width,
    short Height,
    short StoredOffsetX,
    short StoredOffsetY,
    short FrameWidth,
    short FrameHeight);

internal sealed class NonSeekableReadStream : Stream
{
    private readonly MemoryStream _inner;

    public NonSeekableReadStream(byte[] bytes)
    {
        _inner = new MemoryStream(bytes, writable: false);
    }

    public override bool CanRead => true;
    public override bool CanSeek => false;
    public override bool CanWrite => false;
    public override long Length => throw new NotSupportedException();
    public override long Position
    {
        get => throw new NotSupportedException();
        set => throw new NotSupportedException();
    }

    public override void Flush() => throw new NotSupportedException();

    public override int Read(byte[] buffer, int offset, int count) =>
        _inner.Read(buffer, offset, count);

    public override int Read(Span<byte> buffer) => _inner.Read(buffer);

    public override int ReadByte() => _inner.ReadByte();

    public override long Seek(long offset, SeekOrigin origin) =>
        throw new NotSupportedException();

    public override void SetLength(long value) => throw new NotSupportedException();

    public override void Write(byte[] buffer, int offset, int count) =>
        throw new NotSupportedException();

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _inner.Dispose();
        }

        base.Dispose(disposing);
    }
}
