using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using CelesteDesktop.Contracts.Assets;

namespace CelesteDesktop.AssetWorker.SpriteXml;

public sealed partial class SpriteXmlReader
{
    private static readonly char[] DisallowedPathCharacters =
        ['<', '>', '"', '|', '?', '*'];

    private static readonly HashSet<string> ReservedWindowsNames = new(
        [
            "CON", "PRN", "AUX", "NUL",
            "COM1", "COM2", "COM3", "COM4", "COM5",
            "COM6", "COM7", "COM8", "COM9",
            "LPT1", "LPT2", "LPT3", "LPT4", "LPT5",
            "LPT6", "LPT7", "LPT8", "LPT9"
        ],
        StringComparer.OrdinalIgnoreCase);

    private readonly HashSet<string> _allowlistedSpriteIds;
    private readonly SpriteXmlReaderOptions _options;

    public SpriteXmlReader(
        IEnumerable<string> allowlistedSpriteIds,
        SpriteXmlReaderOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(allowlistedSpriteIds);

        _options = options ?? SpriteXmlReaderOptions.Default;
        _options.Validate();
        _allowlistedSpriteIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var id in allowlistedSpriteIds)
        {
            var normalized = NormalizeIdentifier(id);
            if (!_allowlistedSpriteIds.Add(normalized))
            {
                throw new ArgumentException(
                    "The sprite allowlist contains a duplicate identifier.",
                    nameof(allowlistedSpriteIds));
            }
        }

        if (_allowlistedSpriteIds.Count == 0)
        {
            throw new ArgumentException(
                "At least one sprite identifier must be allowlisted.",
                nameof(allowlistedSpriteIds));
        }
    }

    public SpriteMetadataDescriptor Read(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);
        if (!stream.CanRead)
        {
            throw new ArgumentException("The sprite XML stream must be readable.", nameof(stream));
        }

        var input = ReadBounded(stream);
        XDocument document;
        try
        {
            var settings = new XmlReaderSettings
            {
                DtdProcessing = DtdProcessing.Prohibit,
                XmlResolver = null,
                CheckCharacters = true,
                IgnoreComments = true,
                IgnoreProcessingInstructions = true,
                MaxCharactersFromEntities = 0,
                MaxCharactersInDocument = _options.MaximumInputBytes
            };
            using var inputStream = new MemoryStream(input, writable: false);
            using var reader = XmlReader.Create(inputStream, settings);
            document = XDocument.Load(reader, LoadOptions.None);
        }
        catch (XmlException exception)
        {
            throw new SpriteXmlException(SpriteXmlCodes.Malformed, exception);
        }

        var root = document.Root;
        if (root is null ||
            root.Name.NamespaceName.Length != 0 ||
            !string.Equals(root.Name.LocalName, "Sprites", StringComparison.Ordinal) ||
            root.HasAttributes ||
            HasNonWhitespaceText(root))
        {
            throw new SpriteXmlException(SpriteXmlCodes.RootInvalid);
        }

        var elements = root.Elements().ToArray();
        if (elements.Length > _options.MaximumDefinitions)
        {
            throw new SpriteXmlException(SpriteXmlCodes.DefinitionBudgetExceeded);
        }

        var definitions = new List<SpriteDefinitionDescriptor>();
        var found = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var element in elements)
        {
            if (element.Name.NamespaceName.Length != 0)
            {
                throw new SpriteXmlException(SpriteXmlCodes.RootInvalid);
            }

            var id = NormalizeIdentifier(element.Name.LocalName);
            if (!_allowlistedSpriteIds.Contains(id))
            {
                continue;
            }

            if (!found.Add(id))
            {
                throw new SpriteXmlException(SpriteXmlCodes.DefinitionDuplicate);
            }

            definitions.Add(ParseDefinition(element, id));
        }

        if (found.Count != _allowlistedSpriteIds.Count)
        {
            throw new SpriteXmlException(SpriteXmlCodes.DefinitionMissing);
        }

        return new SpriteMetadataDescriptor(definitions);
    }

    private SpriteDefinitionDescriptor ParseDefinition(XElement element, string id)
    {
        ValidateAttributes(element, "path", "start", "delay");
        if (HasNonWhitespaceText(element))
        {
            throw new SpriteXmlException(SpriteXmlCodes.NodeUnknown);
        }

        var atlasPathPrefix = NormalizeAtlasPath(
            RequiredAttribute(element, "path"),
            allowEmpty: false,
            allowTrailingSlash: true);
        var startAnimationId = OptionalIdentifierAttribute(element, "start");
        var masterDelay = OptionalDelay(element, "delay", 0);

        var animations = new List<SpriteAnimationDescriptor>();
        var animationIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var metadataElements = new List<XElement>();
        SpriteOriginDescriptor? origin = null;
        SpritePointDescriptor? position = null;

        foreach (var child in element.Elements())
        {
            if (child.Name.NamespaceName.Length != 0)
            {
                throw new SpriteXmlException(SpriteXmlCodes.NodeUnknown);
            }

            switch (child.Name.LocalName)
            {
                case "Anim":
                case "Loop":
                    if (animations.Count >= _options.MaximumAnimationsPerDefinition)
                    {
                        throw new SpriteXmlException(SpriteXmlCodes.AnimationBudgetExceeded);
                    }

                    var animation = ParseAnimation(
                        child,
                        atlasPathPrefix,
                        masterDelay,
                        child.Name.LocalName == "Loop");
                    if (!animationIds.Add(animation.Id))
                    {
                        throw new SpriteXmlException(SpriteXmlCodes.AnimationDuplicate);
                    }

                    animations.Add(animation);
                    break;

                case "Center":
                    EnsureEmptyElement(child);
                    SetOrigin(ref origin, new SpriteOriginDescriptor(
                        SpriteOriginKind.Center,
                        0.5,
                        0.5));
                    break;

                case "Justify":
                    SetOrigin(ref origin, ParseOrigin(child, SpriteOriginKind.Justify));
                    break;

                case "Origin":
                    SetOrigin(ref origin, ParseOrigin(child, SpriteOriginKind.Absolute));
                    break;

                case "Position":
                    if (position is not null)
                    {
                        throw new SpriteXmlException(SpriteXmlCodes.NodeUnknown);
                    }

                    position = ParsePoint(child);
                    break;

                case "Metadata":
                    metadataElements.Add(child);
                    break;

                default:
                    throw new SpriteXmlException(SpriteXmlCodes.NodeUnknown);
            }
        }

        if (animations.Count == 0)
        {
            throw new SpriteXmlException(SpriteXmlCodes.AnimationMissing);
        }

        if (startAnimationId is not null && !animationIds.Contains(startAnimationId))
        {
            throw new SpriteXmlException(SpriteXmlCodes.StartAnimationMissing);
        }

        if (metadataElements.Count > 1)
        {
            throw new SpriteXmlException(SpriteXmlCodes.MetadataDuplicate);
        }

        var frameMetadata = metadataElements.Count == 0
            ? []
            : ParseMetadata(metadataElements[0], atlasPathPrefix, animations);

        return new SpriteDefinitionDescriptor(
            id,
            atlasPathPrefix,
            startAnimationId,
            origin ?? new SpriteOriginDescriptor(SpriteOriginKind.Unspecified, 0, 0),
            position,
            animations,
            frameMetadata);
    }

    private SpriteAnimationDescriptor ParseAnimation(
        XElement element,
        string prefix,
        double masterDelay,
        bool isLooping)
    {
        ValidateAttributes(
            element,
            isLooping
                ? ["id", "path", "delay", "frames"]
                : ["id", "path", "delay", "frames", "goto"]);
        EnsureNoChildContent(element);

        var id = NormalizeIdentifier(RequiredAttribute(element, "id"));
        var relativePath = NormalizeAtlasPath(
            OptionalAttribute(element, "path") ?? string.Empty,
            allowEmpty: true,
            allowTrailingSlash: true);
        var atlasPath = NormalizeAtlasPath(
            prefix + relativePath,
            allowEmpty: false,
            allowTrailingSlash: true);
        var delay = OptionalDelay(element, "delay", masterDelay);
        var gotoExpression = isLooping
            ? null
            : OptionalBoundedText(element, "goto");
        var frameExpression = OptionalAttribute(element, "frames");
        var usesAllFrames = string.IsNullOrWhiteSpace(frameExpression);
        var frames = usesAllFrames
            ? []
            : ParseFrameExpression(frameExpression!);

        return new SpriteAnimationDescriptor(
            id,
            atlasPath,
            delay,
            isLooping,
            gotoExpression,
            usesAllFrames,
            frames);
    }

    private IReadOnlyList<SpriteFrameMetadataDescriptor> ParseMetadata(
        XElement element,
        string atlasPathPrefix,
        IReadOnlyList<SpriteAnimationDescriptor> animations)
    {
        ValidateAttributes(element);
        if (HasNonWhitespaceText(element))
        {
            throw new SpriteXmlException(SpriteXmlCodes.NodeUnknown);
        }

        var found = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var metadata = new List<SpriteFrameMetadataDescriptor>();
        foreach (var child in element.Elements())
        {
            if (child.Name.NamespaceName.Length != 0 || child.Name.LocalName != "Frames")
            {
                throw new SpriteXmlException(SpriteXmlCodes.NodeUnknown);
            }

            ValidateAttributes(child, "path", "hair", "carry");
            EnsureNoChildContent(child);
            var metadataKey = NormalizeAtlasPath(
                RequiredAttribute(child, "path"),
                allowEmpty: false,
                allowTrailingSlash: true);
            var pathCandidate = NormalizeAtlasPath(
                atlasPathPrefix + metadataKey,
                allowEmpty: false,
                allowTrailingSlash: true);
            var targetPaths = animations
                .Where(animation =>
                    string.Equals(animation.Id, metadataKey, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(animation.AtlasPath, pathCandidate, StringComparison.OrdinalIgnoreCase))
                .Select(animation => animation.AtlasPath)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
            if (targetPaths.Length == 0)
            {
                throw new SpriteXmlException(SpriteXmlCodes.MetadataAnimationMissing);
            }
            if (targetPaths.Length > 1)
            {
                throw new SpriteXmlException(SpriteXmlCodes.MetadataTargetAmbiguous);
            }

            if (!found.Add(metadataKey))
            {
                throw new SpriteXmlException(SpriteXmlCodes.MetadataDuplicate);
            }

            if (metadata.Count >= _options.MaximumFrameMetadataEntries)
            {
                throw new SpriteXmlException(SpriteXmlCodes.FrameBudgetExceeded);
            }

            var hairValue = OptionalAttribute(child, "hair");
            var hidesHairForAllFrames = hairValue is not null && hairValue.Length == 0;
            var hair = hairValue switch
            {
                null => null,
                "" => [],
                _ => ParseHairFrames(hairValue)
            };
            var carry = OptionalAttribute(child, "carry") is { } carryValue
                ? ParseCarryOffsets(carryValue)
                : null;
            if (hair is null && carry is null)
            {
                throw new SpriteXmlException(SpriteXmlCodes.AttributeMissing);
            }

            ValidateMetadataFrameCounts(
                hair,
                carry,
                hidesHairForAllFrames);
            metadata.Add(new SpriteFrameMetadataDescriptor(
                metadataKey,
                targetPaths[0],
                hair,
                carry,
                hidesHairForAllFrames));
        }

        return metadata;
    }

    private void ValidateMetadataFrameCounts(
        IReadOnlyList<SpriteHairFrameDescriptor>? hair,
        IReadOnlyList<int>? carry,
        bool hidesHairForAllFrames)
    {
        if (!hidesHairForAllFrames && hair is not null && carry is not null && hair.Count != carry.Count)
        {
            throw new SpriteXmlException(SpriteXmlCodes.MetadataFrameMismatch);
        }
    }

    private IReadOnlyList<int> ParseFrameExpression(string expression)
    {
        ValidateBoundedText(expression);
        var frames = new List<int>();
        foreach (var rawToken in expression.Split(','))
        {
            var token = rawToken.Trim();
            if (token.Length == 0)
            {
                throw new SpriteXmlException(SpriteXmlCodes.FrameExpressionInvalid);
            }

            var range = FrameRangeRegex().Match(token);
            if (range.Success)
            {
                var first = ParseFrameIndex(range.Groups[1].Value);
                var last = ParseFrameIndex(range.Groups[2].Value);
                var count = Math.Abs((long)last - first) + 1;
                EnsureFrameCapacity(frames.Count, count);
                var step = first <= last ? 1 : -1;
                for (var frame = first; ; frame += step)
                {
                    frames.Add(frame);
                    if (frame == last)
                    {
                        break;
                    }
                }

                continue;
            }

            var repeat = FrameRepeatRegex().Match(token);
            if (repeat.Success)
            {
                var frame = ParseFrameIndex(repeat.Groups[1].Value);
                if (!int.TryParse(
                        repeat.Groups[2].Value,
                        NumberStyles.None,
                        CultureInfo.InvariantCulture,
                        out var count) || count <= 0)
                {
                    throw new SpriteXmlException(SpriteXmlCodes.FrameExpressionInvalid);
                }

                EnsureFrameCapacity(frames.Count, count);
                for (var index = 0; index < count; index++)
                {
                    frames.Add(frame);
                }

                continue;
            }

            EnsureFrameCapacity(frames.Count, 1);
            frames.Add(ParseFrameIndex(token));
        }

        return frames;
    }

    private IReadOnlyList<SpriteHairFrameDescriptor> ParseHairFrames(string value)
    {
        ValidateBoundedText(value);
        var tokens = value.Split('|');
        if (tokens.Length == 0 || tokens.Length > _options.MaximumFramesPerAnimation)
        {
            throw new SpriteXmlException(SpriteXmlCodes.FrameBudgetExceeded);
        }

        var frames = new List<SpriteHairFrameDescriptor>(tokens.Length);
        foreach (var rawToken in tokens)
        {
            var token = rawToken.Trim();
            if (string.Equals(token, "x", StringComparison.OrdinalIgnoreCase))
            {
                frames.Add(new SpriteHairFrameDescriptor(false, 0, 0, 0));
                continue;
            }

            var match = HairFrameRegex().Match(token);
            if (!match.Success ||
                !int.TryParse(match.Groups[1].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var x) ||
                !int.TryParse(match.Groups[2].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var y) ||
                Math.Abs((long)x) > _options.MaximumCoordinateMagnitude ||
                Math.Abs((long)y) > _options.MaximumCoordinateMagnitude)
            {
                throw new SpriteXmlException(SpriteXmlCodes.HairInvalid);
            }

            var facing = match.Groups[3].Success
                ? int.Parse(match.Groups[3].Value, CultureInfo.InvariantCulture)
                : 0;
            frames.Add(new SpriteHairFrameDescriptor(true, x, y, facing));
        }

        return frames;
    }

    private IReadOnlyList<int> ParseCarryOffsets(string value)
    {
        ValidateBoundedText(value);
        var tokens = value.Split(',');
        if (tokens.Length == 0 || tokens.Length > _options.MaximumFramesPerAnimation)
        {
            throw new SpriteXmlException(SpriteXmlCodes.FrameBudgetExceeded);
        }

        var offsets = new List<int>(tokens.Length);
        foreach (var rawToken in tokens)
        {
            if (!int.TryParse(
                    rawToken.Trim(),
                    NumberStyles.Integer,
                    CultureInfo.InvariantCulture,
                    out var offset) ||
                Math.Abs((long)offset) > _options.MaximumCoordinateMagnitude)
            {
                throw new SpriteXmlException(SpriteXmlCodes.CarryInvalid);
            }

            offsets.Add(offset);
        }

        return offsets;
    }

    private SpriteOriginDescriptor ParseOrigin(XElement element, SpriteOriginKind kind)
    {
        var point = ParsePoint(element);
        if (kind == SpriteOriginKind.Justify &&
            (point.X < 0 || point.X > 1 || point.Y < 0 || point.Y > 1))
        {
            throw new SpriteXmlException(SpriteXmlCodes.ValueInvalid);
        }

        return new SpriteOriginDescriptor(kind, point.X, point.Y);
    }

    private SpritePointDescriptor ParsePoint(XElement element)
    {
        ValidateAttributes(element, "x", "y");
        EnsureNoChildContent(element);
        return new SpritePointDescriptor(
            ParseCoordinate(RequiredAttribute(element, "x")),
            ParseCoordinate(RequiredAttribute(element, "y")));
    }

    private double ParseCoordinate(string value)
    {
        if (!double.TryParse(
                value,
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out var result) ||
            !double.IsFinite(result) ||
            Math.Abs(result) > _options.MaximumCoordinateMagnitude)
        {
            throw new SpriteXmlException(SpriteXmlCodes.ValueInvalid);
        }

        return result;
    }

    private double OptionalDelay(XElement element, string name, double defaultValue)
    {
        var value = OptionalAttribute(element, name);
        if (value is null)
        {
            return defaultValue;
        }

        if (!double.TryParse(
                value,
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out var result) ||
            !double.IsFinite(result) || result < 0 || result > 60)
        {
            throw new SpriteXmlException(SpriteXmlCodes.ValueInvalid);
        }

        return result;
    }

    private string? OptionalIdentifierAttribute(XElement element, string name)
    {
        var value = OptionalAttribute(element, name);
        return value is null ? null : NormalizeIdentifier(value);
    }

    private string? OptionalBoundedText(XElement element, string name)
    {
        var value = OptionalAttribute(element, name);
        if (value is null)
        {
            return null;
        }

        ValidateBoundedText(value);
        if (string.IsNullOrWhiteSpace(value) || value.Any(char.IsControl))
        {
            throw new SpriteXmlException(SpriteXmlCodes.ValueInvalid);
        }

        return value.Normalize(NormalizationForm.FormC);
    }

    private string NormalizeIdentifier(string value)
    {
        ValidateBoundedText(value);
        if (string.IsNullOrWhiteSpace(value) ||
            value.Length != value.Trim().Length ||
            value.Any(char.IsControl))
        {
            throw new SpriteXmlException(SpriteXmlCodes.ValueInvalid);
        }

        return value.Normalize(NormalizationForm.FormC);
    }

    private string NormalizeAtlasPath(
        string value,
        bool allowEmpty,
        bool allowTrailingSlash)
    {
        ValidateBoundedText(value);
        if (value.Length == 0 && allowEmpty)
        {
            return string.Empty;
        }

        if (string.IsNullOrWhiteSpace(value) ||
            value.Length != value.Trim().Length ||
            value.Any(char.IsControl))
        {
            throw new SpriteXmlException(SpriteXmlCodes.PathInvalid);
        }

        var normalized = value.Normalize(NormalizationForm.FormC).Replace('\\', '/');
        if (normalized.StartsWith('/') ||
            (!allowTrailingSlash && normalized.EndsWith('/')) ||
            normalized.Contains(':') ||
            normalized.IndexOfAny(DisallowedPathCharacters) >= 0)
        {
            throw new SpriteXmlException(SpriteXmlCodes.PathInvalid);
        }

        var pathToValidate = allowTrailingSlash && normalized.EndsWith('/')
            ? normalized[..^1]
            : normalized;
        if (pathToValidate.Length == 0 ||
            pathToValidate.Split('/').Any(IsInvalidPathSegment))
        {
            throw new SpriteXmlException(SpriteXmlCodes.PathInvalid);
        }

        return normalized;
    }

    private static bool IsInvalidPathSegment(string segment)
    {
        if (string.IsNullOrEmpty(segment) ||
            segment is "." or ".." ||
            segment.EndsWith('.') ||
            segment.EndsWith(' '))
        {
            return true;
        }

        var extensionIndex = segment.IndexOf('.');
        var stem = extensionIndex < 0 ? segment : segment[..extensionIndex];
        return ReservedWindowsNames.Contains(stem);
    }

    private int ParseFrameIndex(string value)
    {
        if (!int.TryParse(
                value,
                NumberStyles.None,
                CultureInfo.InvariantCulture,
                out var frame) ||
            frame < 0 || frame > _options.MaximumFrameIndex)
        {
            throw new SpriteXmlException(SpriteXmlCodes.FrameExpressionInvalid);
        }

        return frame;
    }

    private void EnsureFrameCapacity(int currentCount, long additionalCount)
    {
        if (additionalCount <= 0 ||
            currentCount + additionalCount > _options.MaximumFramesPerAnimation)
        {
            throw new SpriteXmlException(SpriteXmlCodes.FrameBudgetExceeded);
        }
    }

    private void ValidateBoundedText(string value)
    {
        if (value.Length > _options.MaximumStringCharacters)
        {
            throw new SpriteXmlException(SpriteXmlCodes.ValueInvalid);
        }
    }

    private static void SetOrigin(
        ref SpriteOriginDescriptor? current,
        SpriteOriginDescriptor value)
    {
        if (current is not null)
        {
            throw new SpriteXmlException(SpriteXmlCodes.OriginConflict);
        }

        current = value;
    }

    private static void EnsureEmptyElement(XElement element)
    {
        ValidateAttributes(element);
        EnsureNoChildContent(element);
    }

    private static void EnsureNoChildContent(XElement element)
    {
        if (element.HasElements || HasNonWhitespaceText(element))
        {
            throw new SpriteXmlException(SpriteXmlCodes.NodeUnknown);
        }
    }

    private static bool HasNonWhitespaceText(XElement element) =>
        element.Nodes().OfType<XText>().Any(text => !string.IsNullOrWhiteSpace(text.Value));

    private static void ValidateAttributes(XElement element, params string[] allowed)
    {
        var names = new HashSet<string>(allowed, StringComparer.Ordinal);
        if (element.Attributes().Any(attribute =>
                attribute.IsNamespaceDeclaration ||
                attribute.Name.NamespaceName.Length != 0 ||
                !names.Contains(attribute.Name.LocalName)))
        {
            throw new SpriteXmlException(SpriteXmlCodes.AttributeUnknown);
        }
    }

    private static string RequiredAttribute(XElement element, string name) =>
        OptionalAttribute(element, name) is { } value
            ? value
            : throw new SpriteXmlException(SpriteXmlCodes.AttributeMissing);

    private static string? OptionalAttribute(XElement element, string name) =>
        element.Attribute(name)?.Value;

    private byte[] ReadBounded(Stream stream)
    {
        using var output = new MemoryStream();
        var buffer = new byte[8192];
        while (true)
        {
            var count = stream.Read(buffer, 0, buffer.Length);
            if (count == 0)
            {
                return output.ToArray();
            }

            if (output.Length + count > _options.MaximumInputBytes)
            {
                throw new SpriteXmlException(SpriteXmlCodes.InputTooLarge);
            }

            output.Write(buffer, 0, count);
        }
    }

    [GeneratedRegex("^(\\d+)-(\\d+)$", RegexOptions.CultureInvariant)]
    private static partial Regex FrameRangeRegex();

    [GeneratedRegex("^(\\d+)\\*(\\d+)$", RegexOptions.CultureInvariant)]
    private static partial Regex FrameRepeatRegex();

    [GeneratedRegex("^(-?\\d+)\\s*,\\s*(-?\\d+)(?:\\s*:\\s*([0-2]))?$", RegexOptions.CultureInvariant)]
    private static partial Regex HairFrameRegex();
}
