using System.Text;
using CelesteDesktop.AssetWorker.SpriteXml;
using CelesteDesktop.Contracts.Assets;

var tests = new (string Name, Action Body)[]
{
    ("allowlisted definition produces immutable descriptors", ValidDefinitionProducesDescriptors),
    ("loop semantics are preserved", LoopSemanticsArePreserved),
    ("master delay is inherited", MasterDelayIsInherited),
    ("missing frame expression selects all frames", MissingFramesSelectsAll),
    ("range repeat and reverse frames preserve order", FrameTricksPreserveOrder),
    ("center origin is preserved", CenterOriginIsPreserved),
    ("absolute origin is preserved", AbsoluteOriginIsPreserved),
    ("non-seekable fragmented input is supported", FragmentedInputIsSupported),
    ("descriptor collections reject mutation", CollectionsRejectMutation),
    ("public reader surface accepts streams only", PublicSurfaceAcceptsStreamsOnly),
    ("unallowlisted definitions are isolated", UnallowlistedDefinitionsAreSkipped),
    ("malformed XML is rejected", MalformedXmlIsRejected),
    ("DTD is rejected", DtdIsRejected),
    ("input byte budget is enforced", InputBudgetIsEnforced),
    ("wrong root is rejected", WrongRootIsRejected),
    ("root attributes are rejected", RootAttributesAreRejected),
    ("definition budget is enforced", DefinitionBudgetIsEnforced),
    ("missing allowlisted definition is rejected", MissingDefinitionIsRejected),
    ("duplicate selected definition is rejected", DuplicateDefinitionIsRejected),
    ("unknown definition attribute is rejected", UnknownDefinitionAttributeIsRejected),
    ("missing sprite path is rejected", MissingPathIsRejected),
    ("path traversal is rejected", PathTraversalIsRejected),
    ("single animation trailing separator is preserved", AnimationTrailingSeparatorIsPreserved),
    ("repeated trailing path separators are rejected", RepeatedTrailingSeparatorsAreRejected),
    ("unknown selected child is rejected", UnknownChildIsRejected),
    ("animation budget is enforced", AnimationBudgetIsEnforced),
    ("definition without animation is rejected", MissingAnimationIsRejected),
    ("duplicate animation is rejected case-insensitively", DuplicateAnimationIsRejected),
    ("missing start animation is rejected", MissingStartAnimationIsRejected),
    ("invalid frame expression is rejected", InvalidFrameExpressionIsRejected),
    ("frame budget is enforced before expansion", FrameBudgetIsEnforced),
    ("frame index budget is enforced", FrameIndexBudgetIsEnforced),
    ("conflicting origin nodes are rejected", OriginConflictIsRejected),
    ("duplicate metadata block is rejected", DuplicateMetadataIsRejected),
    ("metadata for missing animation is rejected", MissingMetadataAnimationIsRejected),
    ("metadata binds to atlas path shared by animations", MetadataBindsToAtlasPath),
    ("metadata frame count mismatch is rejected", MetadataFrameMismatchIsRejected),
    ("invalid hair metadata is rejected", InvalidHairIsRejected),
    ("empty hair metadata hides hair for all frames", EmptyHairHidesAllFrames),
    ("hair coordinates allow bounded separator whitespace", HairWhitespaceIsAccepted),
    ("invalid carry metadata is rejected", InvalidCarryIsRejected),
    ("justify outside unit range is rejected", InvalidJustifyIsRejected),
    ("unknown metadata child is rejected", UnknownMetadataChildIsRejected),
    ("unreadable input is rejected", UnreadableInputIsRejected)
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

static void ValidDefinitionProducesDescriptors()
{
    var metadata = Read(ValidDocument());

    Assert(metadata.Definitions.Count == 1, "Unexpected definition count.");
    var player = metadata.Definitions[0];
    Assert(player.Id == "player", "Unexpected sprite ID.");
    Assert(player.AtlasPathPrefix == "characters/player/", "Unexpected path prefix.");
    Assert(player.StartAnimationId == "idle", "Unexpected start animation.");
    Assert(player.Origin.Kind == SpriteOriginKind.Justify, "Unexpected origin kind.");
    Assert(player.Origin.X == 0.5 && player.Origin.Y == 1, "Unexpected justify values.");
    Assert(player.Position == new SpritePointDescriptor(1, -2), "Unexpected position.");
    Assert(player.Animations.Count == 2, "Unexpected animation count.");
    Assert(player.Animations[1].AtlasPath == "characters/player/dash", "Unexpected atlas path.");
    Assert(player.FrameMetadata.Count == 1, "Unexpected frame metadata count.");
    var frameMetadata = player.FrameMetadata[0];
    var hairFrames = frameMetadata.HairFrames ??
        throw new InvalidOperationException("Hair frames were not parsed.");
    Assert(hairFrames.Count == 3, "Unexpected hair frame count.");
    Assert(hairFrames[1] == new SpriteHairFrameDescriptor(true, 1, -2, 2), "Hair facing was lost.");
    Assert(hairFrames[2].IsVisible == false, "Hidden hair frame was lost.");
    Assert(frameMetadata.CarryOffsets?.SequenceEqual([0, 1, -1]) == true, "Carry offsets were lost.");
}

static void LoopSemanticsArePreserved()
{
    var animation = Read(ValidDocument()).Definitions[0].Animations[0];
    Assert(animation.IsLooping, "Loop was not marked as looping.");
    Assert(animation.GotoExpression is null, "Loop acquired goto state.");
}

static void MasterDelayIsInherited()
{
    var animation = Read(ValidDocument()).Definitions[0].Animations[0];
    Assert(Math.Abs(animation.DelaySeconds - 0.2) < 0.0001, "Master delay was not inherited.");
}

static void MissingFramesSelectsAll()
{
    var metadata = Read(Sprite("<Anim id=\"idle\" path=\"idle\" />"));
    var animation = metadata.Definitions[0].Animations[0];
    Assert(animation.UsesAllFrames, "Missing frames did not select all frames.");
    Assert(animation.Frames.Count == 0, "All-frame animation exposed explicit frames.");
}

static void FrameTricksPreserveOrder()
{
    var metadata = Read(Sprite("<Anim id=\"idle\" path=\"idle\" frames=\"0-2,4*2,3-1\" />"));
    Assert(
        metadata.Definitions[0].Animations[0].Frames.SequenceEqual([0, 1, 2, 4, 4, 3, 2, 1]),
        "Frame expression order changed.");
}

static void CenterOriginIsPreserved()
{
    var metadata = Read(Sprite("<Center/><Loop id=\"idle\" path=\"idle\" frames=\"0\" />"));
    Assert(metadata.Definitions[0].Origin.Kind == SpriteOriginKind.Center, "Center was not preserved.");
}

static void AbsoluteOriginIsPreserved()
{
    var metadata = Read(Sprite("<Origin x=\"8\" y=\"16\"/><Loop id=\"idle\" path=\"idle\" frames=\"0\" />"));
    Assert(metadata.Definitions[0].Origin == new SpriteOriginDescriptor(SpriteOriginKind.Absolute, 8, 16), "Origin was not preserved.");
}

static void FragmentedInputIsSupported()
{
    using var stream = new FragmentedReadStream(Bytes(ValidDocument()), 1);
    var metadata = new SpriteXmlReader(["player"]).Read(stream);
    Assert(metadata.Definitions.Count == 1, "Fragmented stream was not parsed.");
}

static void CollectionsRejectMutation()
{
    var metadata = Read(ValidDocument());
    AssertReadOnly((IList<SpriteDefinitionDescriptor>)metadata.Definitions, metadata.Definitions[0]);
    AssertReadOnly((IList<SpriteAnimationDescriptor>)metadata.Definitions[0].Animations, metadata.Definitions[0].Animations[0]);
    AssertReadOnly((IList<int>)metadata.Definitions[0].Animations[0].Frames, 0);
}

static void PublicSurfaceAcceptsStreamsOnly()
{
    var methods = typeof(SpriteXmlReader).GetMethods(
        System.Reflection.BindingFlags.Instance |
        System.Reflection.BindingFlags.Public |
        System.Reflection.BindingFlags.DeclaredOnly);
    Assert(methods.Length == 1, "Unexpected public reader method count.");
    var parameters = methods[0].GetParameters();
    Assert(methods[0].Name == nameof(SpriteXmlReader.Read), "Unexpected public method.");
    Assert(parameters.Length == 1 && parameters[0].ParameterType == typeof(Stream), "Reader exposed filesystem input.");
}

static void UnallowlistedDefinitionsAreSkipped()
{
    const string xml = "<Sprites><other path=\"bad\"><Unknown/></other><player path=\"characters/player/\"><Loop id=\"idle\" path=\"idle\" frames=\"0\"/></player></Sprites>";
    Assert(Read(xml).Definitions.Count == 1, "Unallowlisted definition affected output.");
}

static void MalformedXmlIsRejected() => AssertFailure("<Sprites><player>", SpriteXmlCodes.Malformed);

static void DtdIsRejected() => AssertFailure(
    "<!DOCTYPE Sprites [<!ENTITY xxe SYSTEM 'file:///forbidden'>]><Sprites>&xxe;</Sprites>",
    SpriteXmlCodes.Malformed);

static void InputBudgetIsEnforced() => AssertFailure(
    ValidDocument(),
    SpriteXmlCodes.InputTooLarge,
    SpriteXmlReaderOptions.Default with { MaximumInputBytes = 16 });

static void WrongRootIsRejected() => AssertFailure("<NotSprites />", SpriteXmlCodes.RootInvalid);

static void RootAttributesAreRejected() => AssertFailure("<Sprites version=\"1\" />", SpriteXmlCodes.RootInvalid);

static void DefinitionBudgetIsEnforced() => AssertFailure(
    "<Sprites><other/><player path=\"characters/player/\"><Loop id=\"idle\" path=\"idle\" frames=\"0\"/></player></Sprites>",
    SpriteXmlCodes.DefinitionBudgetExceeded,
    SpriteXmlReaderOptions.Default with { MaximumDefinitions = 1 });

static void MissingDefinitionIsRejected() => AssertFailure("<Sprites><other/></Sprites>", SpriteXmlCodes.DefinitionMissing);

static void DuplicateDefinitionIsRejected() => AssertFailure(
    "<Sprites>" + SpriteBody() + SpriteBody() + "</Sprites>",
    SpriteXmlCodes.DefinitionDuplicate);

static void UnknownDefinitionAttributeIsRejected() => AssertFailure(
    Sprite("<Loop id=\"idle\" path=\"idle\" frames=\"0\" />", " extra=\"x\""),
    SpriteXmlCodes.AttributeUnknown);

static void MissingPathIsRejected() => AssertFailure(
    "<Sprites><player><Loop id=\"idle\" path=\"idle\" frames=\"0\"/></player></Sprites>",
    SpriteXmlCodes.AttributeMissing);

static void PathTraversalIsRejected() => AssertFailure(
    Sprite("<Loop id=\"idle\" path=\"idle\" frames=\"0\" />", " path=\"../escape/\"", includeDefaultPath: false),
    SpriteXmlCodes.PathInvalid);

static void AnimationTrailingSeparatorIsPreserved()
{
    var metadata = Read(Sprite("<Loop id=\"idle\" path=\"wakeUp/\" />"));
    Assert(
        metadata.Definitions[0].Animations[0].AtlasPath == "characters/player/wakeUp/",
        "A valid animation subdirectory was not preserved.");
}

static void RepeatedTrailingSeparatorsAreRejected() => AssertFailure(
    Sprite("<Loop id=\"idle\" path=\"idle\" frames=\"0\" />", " path=\"characters/player//\"", includeDefaultPath: false),
    SpriteXmlCodes.PathInvalid);

static void UnknownChildIsRejected() => AssertFailure(Sprite("<Unknown/>"), SpriteXmlCodes.NodeUnknown);

static void AnimationBudgetIsEnforced() => AssertFailure(
    Sprite("<Loop id=\"a\" path=\"a\"/><Loop id=\"b\" path=\"b\"/>"),
    SpriteXmlCodes.AnimationBudgetExceeded,
    SpriteXmlReaderOptions.Default with { MaximumAnimationsPerDefinition = 1 });

static void MissingAnimationIsRejected() => AssertFailure(Sprite(string.Empty), SpriteXmlCodes.AnimationMissing);

static void DuplicateAnimationIsRejected() => AssertFailure(
    Sprite("<Loop id=\"idle\" path=\"a\"/><Anim id=\"IDLE\" path=\"b\"/>"),
    SpriteXmlCodes.AnimationDuplicate);

static void MissingStartAnimationIsRejected() => AssertFailure(
    Sprite("<Loop id=\"idle\" path=\"idle\"/>", " start=\"run\""),
    SpriteXmlCodes.StartAnimationMissing);

static void InvalidFrameExpressionIsRejected() => AssertFailure(
    Sprite("<Loop id=\"idle\" path=\"idle\" frames=\"0,,1\"/>"),
    SpriteXmlCodes.FrameExpressionInvalid);

static void FrameBudgetIsEnforced() => AssertFailure(
    Sprite("<Loop id=\"idle\" path=\"idle\" frames=\"0-10\"/>"),
    SpriteXmlCodes.FrameBudgetExceeded,
    SpriteXmlReaderOptions.Default with { MaximumFramesPerAnimation = 4 });

static void FrameIndexBudgetIsEnforced() => AssertFailure(
    Sprite("<Loop id=\"idle\" path=\"idle\" frames=\"9\"/>"),
    SpriteXmlCodes.FrameExpressionInvalid,
    SpriteXmlReaderOptions.Default with { MaximumFrameIndex = 8 });

static void OriginConflictIsRejected() => AssertFailure(
    Sprite("<Center/><Origin x=\"0\" y=\"0\"/><Loop id=\"idle\" path=\"idle\"/>"),
    SpriteXmlCodes.OriginConflict);

static void DuplicateMetadataIsRejected() => AssertFailure(
    Sprite("<Loop id=\"idle\" path=\"idle\"/><Metadata/><Metadata/>"),
    SpriteXmlCodes.MetadataDuplicate);

static void MissingMetadataAnimationIsRejected() => AssertFailure(
    Sprite("<Loop id=\"idle\" path=\"idle\"/><Metadata><Frames path=\"run\" hair=\"0,0\"/></Metadata>"),
    SpriteXmlCodes.MetadataAnimationMissing);

static void MetadataBindsToAtlasPath()
{
    var metadata = Read(Sprite(
        "<Anim id=\"first\" path=\"shared\" frames=\"0-1\"/>" +
        "<Loop id=\"second\" path=\"shared\" frames=\"2\"/>" +
        "<Metadata><Frames path=\"shared\" hair=\"0,0|1,0|2,0\"/></Metadata>",
        " start=\"first\""));
    Assert(
        metadata.Definitions[0].FrameMetadata[0].AtlasPath == "characters/player/shared",
        "Metadata did not bind to the normalized Atlas path.");
    Assert(metadata.Definitions[0].FrameMetadata[0].Key == "shared", "Metadata key was not preserved.");
}

static void MetadataFrameMismatchIsRejected() => AssertFailure(
    Sprite("<Loop id=\"idle\" path=\"idle\" frames=\"0-1\"/><Metadata><Frames path=\"idle\" hair=\"0,0|1,0\" carry=\"0\"/></Metadata>"),
    SpriteXmlCodes.MetadataFrameMismatch);

static void InvalidHairIsRejected() => AssertFailure(
    Sprite("<Loop id=\"idle\" path=\"idle\"/><Metadata><Frames path=\"idle\" hair=\"0,0:9\"/></Metadata>"),
    SpriteXmlCodes.HairInvalid);

static void EmptyHairHidesAllFrames()
{
    var metadata = Read(Sprite(
        "<Loop id=\"idle\" path=\"idle\" frames=\"0-2\"/>" +
        "<Metadata><Frames path=\"idle\" hair=\"\"/></Metadata>"));
    var frameMetadata = metadata.Definitions[0].FrameMetadata[0];
    Assert(frameMetadata.HidesHairForAllFrames, "Empty hair metadata did not preserve hide-all semantics.");
    Assert(frameMetadata.HairFrames?.Count == 0, "Hide-all metadata invented hair frames.");
}

static void HairWhitespaceIsAccepted()
{
    var metadata = Read(Sprite(
        "<Loop id=\"idle\" path=\"idle\" frames=\"0\"/>" +
        "<Metadata><Frames path=\"idle\" hair=\"1, 2\"/></Metadata>"));
    Assert(
        metadata.Definitions[0].FrameMetadata[0].HairFrames?[0] ==
            new SpriteHairFrameDescriptor(true, 1, 2, 0),
        "Whitespace around a coordinate separator changed the value.");
}

static void InvalidCarryIsRejected() => AssertFailure(
    Sprite("<Loop id=\"idle\" path=\"idle\"/><Metadata><Frames path=\"idle\" carry=\"oops\"/></Metadata>"),
    SpriteXmlCodes.CarryInvalid);

static void InvalidJustifyIsRejected() => AssertFailure(
    Sprite("<Justify x=\"2\" y=\"0.5\"/><Loop id=\"idle\" path=\"idle\"/>"),
    SpriteXmlCodes.ValueInvalid);

static void UnknownMetadataChildIsRejected() => AssertFailure(
    Sprite("<Loop id=\"idle\" path=\"idle\"/><Metadata><Unknown/></Metadata>"),
    SpriteXmlCodes.NodeUnknown);

static void UnreadableInputIsRejected()
{
    using var stream = new WriteOnlyStream();
    try
    {
        new SpriteXmlReader(["player"]).Read(stream);
    }
    catch (ArgumentException)
    {
        return;
    }

    throw new InvalidOperationException("Unreadable stream was accepted.");
}

static SpriteMetadataDescriptor Read(string xml, SpriteXmlReaderOptions? options = null)
{
    using var stream = new MemoryStream(Bytes(xml), writable: false);
    return new SpriteXmlReader(["player"], options).Read(stream);
}

static void AssertFailure(
    string xml,
    string expectedCode,
    SpriteXmlReaderOptions? options = null)
{
    try
    {
        Read(xml, options);
    }
    catch (SpriteXmlException exception) when (exception.Code == expectedCode)
    {
        return;
    }

    throw new InvalidOperationException($"Expected {expectedCode}.");
}

static string ValidDocument() =>
    "<Sprites>" +
    "<ignored><Anything/></ignored>" +
    "<player path=\"characters/player/\" start=\"idle\" delay=\"0.2\">" +
    "<Justify x=\"0.5\" y=\"1\"/>" +
    "<Position x=\"1\" y=\"-2\"/>" +
    "<Loop id=\"idle\" path=\"idle\" frames=\"0-2\"/>" +
    "<Anim id=\"dash\" path=\"dash\" frames=\"3-1,4*2\" delay=\"0.05\" goto=\"idle\"/>" +
    "<Metadata><Frames path=\"idle\" hair=\"0,-2|1,-2:2|x\" carry=\"0,1,-1\"/></Metadata>" +
    "</player></Sprites>";

static string Sprite(
    string children,
    string extraAttributes = "",
    bool includeDefaultPath = true) =>
    "<Sprites><player" +
    (includeDefaultPath ? " path=\"characters/player/\"" : string.Empty) +
    extraAttributes + ">" + children + "</player></Sprites>";

static string SpriteBody() =>
    "<player path=\"characters/player/\"><Loop id=\"idle\" path=\"idle\"/></player>";

static byte[] Bytes(string value) => Encoding.UTF8.GetBytes(value);

static void Assert(bool condition, string message)
{
    if (!condition)
    {
        throw new InvalidOperationException(message);
    }
}

static void AssertReadOnly<T>(IList<T> values, T value)
{
    try
    {
        values.Add(value);
    }
    catch (NotSupportedException)
    {
        return;
    }

    throw new InvalidOperationException("Collection was mutable.");
}

sealed class FragmentedReadStream : Stream
{
    private readonly MemoryStream _inner;
    private readonly int _maximumChunk;

    public FragmentedReadStream(byte[] bytes, int maximumChunk)
    {
        _inner = new MemoryStream(bytes, writable: false);
        _maximumChunk = maximumChunk;
    }

    public override bool CanRead => true;
    public override bool CanSeek => false;
    public override bool CanWrite => false;
    public override long Length => throw new NotSupportedException();
    public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
    public override void Flush() { }
    public override int Read(byte[] buffer, int offset, int count) =>
        _inner.Read(buffer, offset, Math.Min(count, _maximumChunk));
    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
    public override void SetLength(long value) => throw new NotSupportedException();
    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    protected override void Dispose(bool disposing) { if (disposing) _inner.Dispose(); base.Dispose(disposing); }
}

sealed class WriteOnlyStream : Stream
{
    public override bool CanRead => false;
    public override bool CanSeek => false;
    public override bool CanWrite => true;
    public override long Length => 0;
    public override long Position { get => 0; set => throw new NotSupportedException(); }
    public override void Flush() { }
    public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
    public override void SetLength(long value) => throw new NotSupportedException();
    public override void Write(byte[] buffer, int offset, int count) { }
}
