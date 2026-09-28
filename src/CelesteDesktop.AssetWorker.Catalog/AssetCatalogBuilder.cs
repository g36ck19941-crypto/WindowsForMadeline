using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using CelesteDesktop.AssetWorker.Data;
using CelesteDesktop.Contracts.Assets;

namespace CelesteDesktop.AssetWorker.Catalog;

public sealed class AssetCatalogBuilder
{
    private readonly AtlasDataDecoder _decoder;
    private readonly AssetCatalogBuilderOptions _options;

    public AssetCatalogBuilder(
        AtlasDataDecoder? decoder = null,
        AssetCatalogBuilderOptions? options = null)
    {
        _decoder = decoder ?? new AtlasDataDecoder();
        _options = options ?? AssetCatalogBuilderOptions.Default;
        _options.Validate();
    }

    public NormalizedAssetCatalog Build(
        AtlasMetadataDescriptor atlas,
        SpriteMetadataDescriptor sprites,
        AssetSourceFingerprint source,
        IEnumerable<string> entityAllowlist,
        IAtlasPageStreamSource pageSource)
    {
        ArgumentNullException.ThrowIfNull(atlas);
        ArgumentNullException.ThrowIfNull(sprites);
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(entityAllowlist);
        ArgumentNullException.ThrowIfNull(pageSource);

        ValidateSource(source, _options.MaximumSourceFiles);
        var allowlist = ValidateAllowlist(entityAllowlist);
        var definitions = IndexDefinitions(sprites);
        var entries = IndexEntries(atlas);
        var pages = IndexPages(atlas);
        var decodedPages = new Dictionary<int, Bgra32Frame>();
        var entities = new List<EntityAssetCatalog>();
        var animationCount = 0;
        var frameCount = 0;
        long pixelBytes = 0;

        foreach (var entityId in allowlist.OrderBy(value => value, StringComparer.Ordinal))
        {
            if (!definitions.TryGetValue(entityId, out var definition))
            {
                throw new AssetCatalogException(AssetCatalogCodes.EntityMissing);
            }

            var animations = new List<CatalogAnimationDescriptor>();
            foreach (var animation in definition.Animations.OrderBy(value => value.Id, StringComparer.Ordinal))
            {
                CheckBudget(++animationCount, _options.MaximumAnimations);
                var selectedEntries = ResolveEntries(animation, entries);
                var frames = new List<CatalogFrameDescriptor>(selectedEntries.Count);
                foreach (var entry in selectedEntries)
                {
                    CheckBudget(++frameCount, _options.MaximumFrames);
                    var requiredBytes = (long)entry.FrameWidth * entry.FrameHeight * 4;
                    if (requiredBytes <= 0 || requiredBytes > int.MaxValue)
                    {
                        throw new AssetCatalogException(AssetCatalogCodes.BudgetExceeded);
                    }
                    pixelBytes += requiredBytes;
                    CheckBudget(pixelBytes, _options.MaximumPixelBytes);
                    var page = DecodePage(entry.PageIndex, pages, decodedPages, pageSource);
                    var frame = ExtractFrame(page, entry);
                    frames.Add(new CatalogFrameDescriptor(entry.Id, frame));
                }

                animations.Add(new CatalogAnimationDescriptor(
                    animation.Id,
                    animation.DelaySeconds,
                    animation.IsLooping,
                    animation.GotoExpression,
                    frames));
            }

            entities.Add(new EntityAssetCatalog(
                definition.Id,
                definition.StartAnimationId,
                definition.Origin,
                definition.Position,
                animations));
        }

        var orderedSource = new AssetSourceFingerprint(
            source.ProfileId,
            source.Files.OrderBy(file => file.LogicalPath, StringComparer.Ordinal));
        return new NormalizedAssetCatalog(
            orderedSource,
            entities,
            ComputeCatalogHash(orderedSource, entities),
            decodedPages.Count);
    }

    private string[] ValidateAllowlist(IEnumerable<string> values)
    {
        var result = values.ToArray();
        if (result.Length == 0 || result.Length > _options.MaximumEntities ||
            result.Any(string.IsNullOrWhiteSpace) ||
            result.Distinct(StringComparer.Ordinal).Count() != result.Length)
        {
            throw new AssetCatalogException(AssetCatalogCodes.AllowlistInvalid);
        }

        return result;
    }

    private static Dictionary<string, SpriteDefinitionDescriptor> IndexDefinitions(
        SpriteMetadataDescriptor sprites)
    {
        var result = new Dictionary<string, SpriteDefinitionDescriptor>(StringComparer.Ordinal);
        var insensitive = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var definition in sprites.Definitions)
        {
            if (!result.TryAdd(definition.Id, definition) || !insensitive.Add(definition.Id))
            {
                throw new AssetCatalogException(AssetCatalogCodes.NameAmbiguous);
            }

            if (definition.Animations
                .Select(animation => animation.Id)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Count() != definition.Animations.Count)
            {
                throw new AssetCatalogException(AssetCatalogCodes.NameAmbiguous);
            }
        }

        return result;
    }

    private static Dictionary<string, AtlasEntryDescriptor> IndexEntries(AtlasMetadataDescriptor atlas)
    {
        var result = new Dictionary<string, AtlasEntryDescriptor>(StringComparer.Ordinal);
        var insensitive = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var entry in atlas.Pages.SelectMany(page => page.Entries))
        {
            if (!result.TryAdd(entry.Id, entry) || !insensitive.Add(entry.Id))
            {
                throw new AssetCatalogException(AssetCatalogCodes.AtlasEntryAmbiguous);
            }
        }

        return result;
    }

    private static Dictionary<int, AtlasPageDescriptor> IndexPages(AtlasMetadataDescriptor atlas)
    {
        var result = new Dictionary<int, AtlasPageDescriptor>();
        foreach (var page in atlas.Pages)
        {
            if (!result.TryAdd(page.Index, page))
            {
                throw new AssetCatalogException(AssetCatalogCodes.PageMissing);
            }
        }

        return result;
    }

    private static IReadOnlyList<AtlasEntryDescriptor> ResolveEntries(
        SpriteAnimationDescriptor animation,
        IReadOnlyDictionary<string, AtlasEntryDescriptor> entries)
    {
        if (!animation.UsesAllFrames)
        {
            var result = new List<AtlasEntryDescriptor>(animation.Frames.Count);
            foreach (var index in animation.Frames)
            {
                var id = animation.AtlasPath + index.ToString("D2", CultureInfo.InvariantCulture);
                if (!entries.TryGetValue(id, out var entry))
                {
                    throw new AssetCatalogException(AssetCatalogCodes.AtlasEntryMissing);
                }

                result.Add(entry);
            }

            return result;
        }

        var matches = new SortedDictionary<int, AtlasEntryDescriptor>();
        foreach (var pair in entries)
        {
            if (!pair.Key.StartsWith(animation.AtlasPath, StringComparison.Ordinal))
            {
                continue;
            }

            var suffix = pair.Key[animation.AtlasPath.Length..];
            if (suffix.Length < 2 || !suffix.All(char.IsAsciiDigit) ||
                !int.TryParse(suffix, NumberStyles.None, CultureInfo.InvariantCulture, out var index))
            {
                continue;
            }

            if (!matches.TryAdd(index, pair.Value))
            {
                throw new AssetCatalogException(AssetCatalogCodes.AtlasEntryAmbiguous);
            }
        }

        if (matches.Count == 0)
        {
            throw new AssetCatalogException(AssetCatalogCodes.AtlasEntryMissing);
        }

        return matches.Values.ToArray();
    }

    private Bgra32Frame DecodePage(
        int pageIndex,
        IReadOnlyDictionary<int, AtlasPageDescriptor> pages,
        IDictionary<int, Bgra32Frame> cache,
        IAtlasPageStreamSource source)
    {
        if (cache.TryGetValue(pageIndex, out var cached))
        {
            return cached;
        }

        if (cache.Count >= _options.MaximumDecodedPages ||
            !pages.TryGetValue(pageIndex, out var page))
        {
            throw new AssetCatalogException(
                pages.ContainsKey(pageIndex)
                    ? AssetCatalogCodes.BudgetExceeded
                    : AssetCatalogCodes.PageMissing);
        }

        using var stream = source.OpenPage(page.DataPath);
        if (stream is null)
        {
            throw new AssetCatalogException(AssetCatalogCodes.PageMissing);
        }

        var decoded = _decoder.Decode(stream);
        cache.Add(pageIndex, decoded);
        return decoded;
    }

    private static Bgra32Frame ExtractFrame(Bgra32Frame page, AtlasEntryDescriptor entry)
    {
        if (entry.X < 0 || entry.Y < 0 || entry.Width <= 0 || entry.Height <= 0 ||
            entry.FrameWidth <= 0 || entry.FrameHeight <= 0 ||
            entry.TrimOffsetX < 0 || entry.TrimOffsetY < 0 ||
            (long)entry.X + entry.Width > page.Width ||
            (long)entry.Y + entry.Height > page.Height ||
            (long)entry.TrimOffsetX + entry.Width > entry.FrameWidth ||
            (long)entry.TrimOffsetY + entry.Height > entry.FrameHeight)
        {
            throw new AssetCatalogException(AssetCatalogCodes.PageBoundsInvalid);
        }

        var source = page.CopyPixels();
        var output = new byte[checked(entry.FrameWidth * entry.FrameHeight * 4)];
        for (var row = 0; row < entry.Height; row++)
        {
            var sourceOffset = checked(((entry.Y + row) * page.Width + entry.X) * 4);
            var destinationOffset = checked(
                ((entry.TrimOffsetY + row) * entry.FrameWidth + entry.TrimOffsetX) * 4);
            source.AsSpan(sourceOffset, entry.Width * 4)
                .CopyTo(output.AsSpan(destinationOffset, entry.Width * 4));
        }

        return new Bgra32Frame(entry.FrameWidth, entry.FrameHeight, output);
    }

    private static void ValidateSource(AssetSourceFingerprint source, int maximumFiles)
    {
        if (string.IsNullOrWhiteSpace(source.ProfileId) || source.ProfileId.Length > 256 ||
            source.Files.Count == 0 || source.Files.Count > maximumFiles)
        {
            throw new AssetCatalogException(AssetCatalogCodes.SourceFingerprintInvalid);
        }

        var paths = new HashSet<string>(StringComparer.Ordinal);
        foreach (var file in source.Files)
        {
            if (file is null || string.IsNullOrWhiteSpace(file.LogicalPath) ||
                file.LogicalPath.Length > 1024 || file.ByteLength < 0 ||
                file.Sha256.Length != 64 || !file.Sha256.All(char.IsAsciiHexDigit) ||
                !paths.Add(file.LogicalPath))
            {
                throw new AssetCatalogException(AssetCatalogCodes.SourceFingerprintInvalid);
            }
        }
    }

    private static string ComputeCatalogHash(
        AssetSourceFingerprint source,
        IEnumerable<EntityAssetCatalog> entities)
    {
        using var buffer = new MemoryStream();
        using (var writer = new BinaryWriter(buffer, Encoding.UTF8, leaveOpen: true))
        {
            writer.Write("CDR-015-CATALOG-V1");
            writer.Write(source.ProfileId);
            writer.Write(source.Files.Count);
            foreach (var file in source.Files)
            {
                writer.Write(file.LogicalPath);
                writer.Write(file.ByteLength);
                writer.Write(file.Sha256.ToLowerInvariant());
            }

            var entityArray = entities.ToArray();
            writer.Write(entityArray.Length);
            foreach (var entity in entityArray)
            {
                writer.Write(entity.EntityId);
                writer.Write(entity.StartAnimationId ?? string.Empty);
                writer.Write((int)entity.Origin.Kind);
                writer.Write(entity.Origin.X);
                writer.Write(entity.Origin.Y);
                writer.Write(entity.Position is not null);
                if (entity.Position is not null)
                {
                    writer.Write(entity.Position.X);
                    writer.Write(entity.Position.Y);
                }
                writer.Write(entity.Animations.Count);
                foreach (var animation in entity.Animations)
                {
                    writer.Write(animation.Id);
                    writer.Write(animation.DelaySeconds);
                    writer.Write(animation.IsLooping);
                    writer.Write(animation.GotoExpression ?? string.Empty);
                    writer.Write(animation.Frames.Count);
                    foreach (var frame in animation.Frames)
                    {
                        writer.Write(frame.AtlasEntryId);
                        writer.Write(frame.Frame.Width);
                        writer.Write(frame.Frame.Height);
                        writer.Write(frame.Frame.ContentSha256);
                    }
                }
            }
        }

        return Convert.ToHexString(SHA256.HashData(buffer.ToArray())).ToLowerInvariant();
    }

    private static void CheckBudget(int value, int maximum)
    {
        if (value > maximum)
        {
            throw new AssetCatalogException(AssetCatalogCodes.BudgetExceeded);
        }
    }

    private static void CheckBudget(long value, int maximum)
    {
        if (value > maximum)
        {
            throw new AssetCatalogException(AssetCatalogCodes.BudgetExceeded);
        }
    }
}
