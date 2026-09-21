using CelesteDesktop.AssetWorker.Data;

var tests = new (string Name, Action Body)[]
{
    ("opaque run decodes to BGRA32", OpaqueRunDecodesToBgra32),
    ("alpha run preserves alpha", AlphaRunPreservesAlpha),
    ("transparent run omits color bytes", TransparentRunOmitsColorBytes),
    ("multiple runs preserve order", MultipleRunsPreserveOrder),
    ("run may cross a row boundary", RunMayCrossRowBoundary),
    ("non-seekable input is supported", NonSeekableInputIsSupported),
    ("frame copies source pixels", FrameCopiesSourcePixels),
    ("frame copies returned pixels", FrameCopiesReturnedPixels),
    ("frame dimensions and stride are exact", FrameDimensionsAndStrideAreExact),
    ("frame fingerprint is deterministic", FrameFingerprintIsDeterministic),
    ("public decoder surface accepts streams only", PublicDecoderSurfaceAcceptsStreamsOnly),
    ("unreadable stream is rejected", UnreadableStreamIsRejected),
    ("truncated width is rejected", TruncatedWidthIsRejected),
    ("truncated height is rejected", TruncatedHeightIsRejected),
    ("truncated flag is rejected", TruncatedFlagIsRejected),
    ("invalid alpha flag is rejected", InvalidAlphaFlagIsRejected),
    ("zero width is rejected", ZeroWidthIsRejected),
    ("negative height is rejected", NegativeHeightIsRejected),
    ("dimension budget is enforced", DimensionBudgetIsEnforced),
    ("output byte budget is enforced", OutputByteBudgetIsEnforced),
    ("input byte budget is enforced", InputByteBudgetIsEnforced),
    ("zero run is rejected", ZeroRunIsRejected),
    ("run overflow is rejected", RunOverflowIsRejected),
    ("truncated alpha is rejected", TruncatedAlphaIsRejected),
    ("truncated color is rejected", TruncatedColorIsRejected),
    ("underfilled output is rejected", UnderfilledOutputIsRejected),
    ("trailing data is rejected", TrailingDataIsRejected),
    ("no-alpha format does not consume alpha", NoAlphaDoesNotConsumeAlpha),
    ("transparent then opaque run decodes", TransparentThenOpaqueRunDecodes)
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

static void OpaqueRunDecodesToBgra32()
{
    var frame = Decode(WriteData(2, 1, false, Run(2, 0, 10, 20, 30)));
    AssertPixels(frame.CopyPixels(), 10, 20, 30, 255, 10, 20, 30, 255);
}

static void AlphaRunPreservesAlpha()
{
    var frame = Decode(WriteData(1, 1, true, Run(1, 64, 1, 2, 3)));
    AssertPixels(frame.CopyPixels(), 1, 2, 3, 64);
}

static void TransparentRunOmitsColorBytes()
{
    var frame = Decode(WriteData(2, 1, true, Run(2, 0)));
    AssertPixels(frame.CopyPixels(), 0, 0, 0, 0, 0, 0, 0, 0);
}

static void MultipleRunsPreserveOrder()
{
    var frame = Decode(WriteData(
        3,
        1,
        true,
        Run(1, 255, 1, 2, 3),
        Run(2, 128, 4, 5, 6)));
    AssertPixels(frame.CopyPixels(), 1, 2, 3, 255, 4, 5, 6, 128, 4, 5, 6, 128);
}

static void RunMayCrossRowBoundary()
{
    var frame = Decode(WriteData(2, 2, false, Run(4, 0, 7, 8, 9)));
    Assert(frame.PixelByteCount == 16, "Unexpected output length.");
    Assert(frame.CopyPixels().Where((value, index) => index % 4 != 3).All(value => value is 7 or 8 or 9), "Run was not expanded.");
}

static void NonSeekableInputIsSupported()
{
    using var stream = new NonSeekableReadStream(WriteData(1, 1, false, Run(1, 0, 1, 2, 3)));
    var frame = new AtlasDataDecoder().Decode(stream);
    Assert(frame.Width == 1, "Non-seekable input did not decode.");
}

static void FrameCopiesSourcePixels()
{
    var source = new byte[] { 1, 2, 3, 4 };
    var frame = new CelesteDesktop.Contracts.Assets.Bgra32Frame(1, 1, source);
    source[0] = 9;
    Assert(frame.CopyPixels()[0] == 1, "Frame retained mutable source storage.");
}

static void FrameCopiesReturnedPixels()
{
    var frame = Decode(WriteData(1, 1, false, Run(1, 0, 1, 2, 3)));
    var first = frame.CopyPixels();
    first[0] = 9;
    Assert(frame.CopyPixels()[0] == 1, "Frame exposed mutable pixel storage.");
}

static void FrameDimensionsAndStrideAreExact()
{
    var frame = Decode(WriteData(2, 3, false, Run(6, 0, 1, 2, 3)));
    Assert(frame.Width == 2 && frame.Height == 3, "Dimensions changed.");
    Assert(frame.Stride == 8 && frame.PixelByteCount == 24, "Stride or byte count changed.");
}

static void FrameFingerprintIsDeterministic()
{
    var first = Decode(WriteData(1, 1, false, Run(1, 0, 1, 2, 3)));
    var second = Decode(WriteData(1, 1, false, Run(1, 0, 1, 2, 3)));
    Assert(first.ContentSha256 == second.ContentSha256, "Fingerprint changed.");
    Assert(first.ContentSha256.Length == 64, "Fingerprint is not SHA-256 hex.");
}

static void PublicDecoderSurfaceAcceptsStreamsOnly()
{
    var methods = typeof(AtlasDataDecoder).GetMethods(
        System.Reflection.BindingFlags.Instance |
        System.Reflection.BindingFlags.Public |
        System.Reflection.BindingFlags.DeclaredOnly);
    Assert(methods.Length == 1, "Unexpected public decoder method count.");
    var parameters = methods[0].GetParameters();
    Assert(methods[0].Name == nameof(AtlasDataDecoder.Decode), "Unexpected decoder method.");
    Assert(parameters.Length == 1 && parameters[0].ParameterType == typeof(Stream), "Decoder exposed filesystem input.");
}

static void UnreadableStreamIsRejected()
{
    using var stream = new WriteOnlyStream();
    try
    {
        new AtlasDataDecoder().Decode(stream);
    }
    catch (ArgumentException)
    {
        return;
    }

    throw new InvalidOperationException("Expected unreadable stream rejection.");
}

static void TruncatedWidthIsRejected() => AssertFailure([1, 2, 3], AtlasDataCodes.Truncated);
static void TruncatedHeightIsRejected() => AssertFailure(Int32Bytes(1), AtlasDataCodes.Truncated);
static void TruncatedFlagIsRejected() => AssertFailure(Header(1, 1), AtlasDataCodes.Truncated);
static void InvalidAlphaFlagIsRejected() => AssertFailure([.. Header(1, 1), 2], AtlasDataCodes.AlphaFlagInvalid);
static void ZeroWidthIsRejected() => AssertFailure(Header(0, 1), AtlasDataCodes.DimensionsInvalid);
static void NegativeHeightIsRejected() => AssertFailure(Header(1, -1), AtlasDataCodes.DimensionsInvalid);

static void DimensionBudgetIsEnforced() => AssertFailure(
    Header(3, 1),
    AtlasDataCodes.DimensionsInvalid,
    AtlasDataDecoderOptions.Default with { MaximumDimension = 2 });

static void OutputByteBudgetIsEnforced() => AssertFailure(
    Header(2, 2),
    AtlasDataCodes.OutputTooLarge,
    AtlasDataDecoderOptions.Default with { MaximumOutputBytes = 15 });

static void InputByteBudgetIsEnforced() => AssertFailure(
    WriteData(1, 1, false, Run(1, 0, 1, 2, 3)),
    AtlasDataCodes.InputTooLarge,
    AtlasDataDecoderOptions.Default with { MaximumInputBytes = 9 });

static void ZeroRunIsRejected() => AssertFailure(
    [.. Header(1, 1), 0, 0],
    AtlasDataCodes.RunLengthInvalid);

static void RunOverflowIsRejected() => AssertFailure(
    WriteData(1, 1, false, Run(2, 0, 1, 2, 3)),
    AtlasDataCodes.RunOverflow);

static void TruncatedAlphaIsRejected() => AssertFailure(
    [.. Header(1, 1), 1, 1],
    AtlasDataCodes.Truncated);

static void TruncatedColorIsRejected() => AssertFailure(
    [.. Header(1, 1), 1, 1, 255, 1, 2],
    AtlasDataCodes.Truncated);

static void UnderfilledOutputIsRejected() => AssertFailure(
    WriteData(2, 1, false, Run(1, 0, 1, 2, 3)),
    AtlasDataCodes.Truncated);

static void TrailingDataIsRejected()
{
    var bytes = WriteData(1, 1, false, Run(1, 0, 1, 2, 3));
    Array.Resize(ref bytes, bytes.Length + 1);
    AssertFailure(bytes, AtlasDataCodes.TrailingData);
}

static void NoAlphaDoesNotConsumeAlpha()
{
    var bytes = WriteData(1, 1, false, Run(1, 0, 1, 2, 3));
    bytes = [.. bytes, 4];
    AssertFailure(bytes, AtlasDataCodes.TrailingData);
}

static void TransparentThenOpaqueRunDecodes()
{
    var frame = Decode(WriteData(
        2,
        1,
        true,
        Run(1, 0),
        Run(1, 255, 7, 8, 9)));
    AssertPixels(frame.CopyPixels(), 0, 0, 0, 0, 7, 8, 9, 255);
}

static CelesteDesktop.Contracts.Assets.Bgra32Frame Decode(byte[] bytes)
{
    using var stream = new MemoryStream(bytes, writable: false);
    return new AtlasDataDecoder().Decode(stream);
}

static void AssertFailure(
    byte[] bytes,
    string expectedCode,
    AtlasDataDecoderOptions? options = null)
{
    using var stream = new MemoryStream(bytes, writable: false);
    try
    {
        new AtlasDataDecoder(options).Decode(stream);
    }
    catch (AtlasDataException exception)
    {
        Assert(exception.Code == expectedCode, $"Expected {expectedCode}, got {exception.Code}.");
        return;
    }

    throw new InvalidOperationException($"Expected atlas data failure {expectedCode}.");
}

static byte[] WriteData(int width, int height, bool hasAlpha, params DataRun[] runs)
{
    using var stream = new MemoryStream();
    using (var writer = new BinaryWriter(stream, System.Text.Encoding.UTF8, leaveOpen: true))
    {
        writer.Write(width);
        writer.Write(height);
        writer.Write(hasAlpha ? (byte)1 : (byte)0);
        foreach (var run in runs)
        {
            writer.Write(run.Length);
            if (hasAlpha)
            {
                writer.Write(run.Alpha);
            }

            if (!hasAlpha || run.Alpha != 0)
            {
                writer.Write(run.Blue);
                writer.Write(run.Green);
                writer.Write(run.Red);
            }
        }
    }

    return stream.ToArray();
}

static byte[] Header(int width, int height) => [.. Int32Bytes(width), .. Int32Bytes(height)];
static byte[] Int32Bytes(int value) => BitConverter.GetBytes(value);
static DataRun Run(byte length, byte alpha, byte blue = 0, byte green = 0, byte red = 0) => new(length, alpha, blue, green, red);

static void AssertPixels(byte[] actual, params byte[] expected) =>
    Assert(actual.SequenceEqual(expected), $"Unexpected pixels: {Convert.ToHexString(actual)}.");

static void Assert(bool condition, string message)
{
    if (!condition)
    {
        throw new InvalidOperationException(message);
    }
}

internal sealed record DataRun(byte Length, byte Alpha, byte Blue, byte Green, byte Red);

internal sealed class NonSeekableReadStream : Stream
{
    private readonly MemoryStream _inner;

    public NonSeekableReadStream(byte[] bytes) => _inner = new MemoryStream(bytes, writable: false);
    public override bool CanRead => true;
    public override bool CanSeek => false;
    public override bool CanWrite => false;
    public override long Length => throw new NotSupportedException();
    public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
    public override void Flush() => throw new NotSupportedException();
    public override int Read(byte[] buffer, int offset, int count) => _inner.Read(buffer, offset, count);
    public override int Read(Span<byte> buffer) => _inner.Read(buffer);
    public override int ReadByte() => _inner.ReadByte();
    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
    public override void SetLength(long value) => throw new NotSupportedException();
    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    protected override void Dispose(bool disposing) { if (disposing) _inner.Dispose(); base.Dispose(disposing); }
}

internal sealed class WriteOnlyStream : Stream
{
    public override bool CanRead => false;
    public override bool CanSeek => false;
    public override bool CanWrite => true;
    public override long Length => throw new NotSupportedException();
    public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
    public override void Flush() { }
    public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
    public override void SetLength(long value) => throw new NotSupportedException();
    public override void Write(byte[] buffer, int offset, int count) { }
}
