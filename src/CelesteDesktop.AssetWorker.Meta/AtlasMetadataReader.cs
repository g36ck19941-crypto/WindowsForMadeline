using System.Buffers.Binary;
using System.Text;
using CelesteDesktop.Contracts.Assets;

namespace CelesteDesktop.AssetWorker.Meta;

public sealed class AtlasMetadataReader
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

    private static readonly UTF8Encoding StrictUtf8 = new(
        encoderShouldEmitUTF8Identifier: false,
        throwOnInvalidBytes: true);

    private readonly AtlasMetadataReaderOptions _options;

    public AtlasMetadataReader(AtlasMetadataReaderOptions? options = null)
    {
        _options = options ?? AtlasMetadataReaderOptions.Default;
        _options.Validate();
    }

    public AtlasMetadataDescriptor Read(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);
        if (!stream.CanRead)
        {
            throw new ArgumentException("The metadata stream must be readable.", nameof(stream));
        }

        var input = new BoundedInput(stream, _options.MaximumInputBytes);
        var formatVersion = input.ReadInt32();
        if (formatVersion < 0)
        {
            throw new AtlasMetadataException(AtlasMetadataCodes.VersionInvalid);
        }

        var packer = input.ReadString(_options.MaximumStringBytes);
        ValidateHeaderText(packer);

        var packerVersion = input.ReadInt32();
        if (packerVersion < 0)
        {
            throw new AtlasMetadataException(AtlasMetadataCodes.VersionInvalid);
        }

        var pageCount = input.ReadInt16();
        if (pageCount <= 0 || pageCount > _options.MaximumPages)
        {
            throw new AtlasMetadataException(AtlasMetadataCodes.PageCountInvalid);
        }

        var pages = new List<AtlasPageDescriptor>(pageCount);
        var pagePaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var entryIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var totalEntries = 0;

        for (var pageIndex = 0; pageIndex < pageCount; pageIndex++)
        {
            var dataPath = NormalizeLogicalPath(
                input.ReadString(_options.MaximumStringBytes));
            if (!pagePaths.Add(dataPath))
            {
                throw new AtlasMetadataException(AtlasMetadataCodes.PageDuplicate);
            }

            var entryCount = input.ReadInt16();
            if (entryCount < 0 || entryCount > _options.MaximumEntriesPerPage)
            {
                throw new AtlasMetadataException(AtlasMetadataCodes.EntryCountInvalid);
            }

            totalEntries = checked(totalEntries + entryCount);
            if (totalEntries > _options.MaximumEntries)
            {
                throw new AtlasMetadataException(AtlasMetadataCodes.EntryBudgetExceeded);
            }

            var entries = new List<AtlasEntryDescriptor>(entryCount);
            for (var entryIndex = 0; entryIndex < entryCount; entryIndex++)
            {
                var id = NormalizeLogicalPath(
                    input.ReadString(_options.MaximumStringBytes));
                if (!entryIds.Add(id))
                {
                    throw new AtlasMetadataException(AtlasMetadataCodes.EntryDuplicate);
                }

                entries.Add(ReadEntry(input, id, pageIndex));
            }

            pages.Add(new AtlasPageDescriptor(pageIndex, dataPath, entries));
        }

        if (input.TryReadByte(out _))
        {
            throw new AtlasMetadataException(AtlasMetadataCodes.TrailingData);
        }

        return new AtlasMetadataDescriptor(
            formatVersion,
            packer,
            packerVersion,
            pages);
    }

    private AtlasEntryDescriptor ReadEntry(
        BoundedInput input,
        string id,
        int pageIndex)
    {
        var x = input.ReadInt16();
        var y = input.ReadInt16();
        var width = input.ReadInt16();
        var height = input.ReadInt16();
        var storedOffsetX = input.ReadInt16();
        var storedOffsetY = input.ReadInt16();
        var frameWidth = input.ReadInt16();
        var frameHeight = input.ReadInt16();

        if (x < 0 || y < 0 || width <= 0 || height <= 0 ||
            frameWidth <= 0 || frameHeight <= 0 ||
            x + width > _options.MaximumAtlasExtent ||
            y + height > _options.MaximumAtlasExtent ||
            frameWidth > _options.MaximumAtlasExtent ||
            frameHeight > _options.MaximumAtlasExtent)
        {
            throw new AtlasMetadataException(AtlasMetadataCodes.RectangleInvalid);
        }

        if (storedOffsetX > 0 || storedOffsetY > 0 ||
            storedOffsetX == short.MinValue ||
            storedOffsetY == short.MinValue)
        {
            throw new AtlasMetadataException(AtlasMetadataCodes.TrimInvalid);
        }

        var trimOffsetX = -storedOffsetX;
        var trimOffsetY = -storedOffsetY;
        if (trimOffsetX + width > frameWidth ||
            trimOffsetY + height > frameHeight)
        {
            throw new AtlasMetadataException(AtlasMetadataCodes.TrimInvalid);
        }

        return new AtlasEntryDescriptor(
            id,
            pageIndex,
            x,
            y,
            width,
            height,
            trimOffsetX,
            trimOffsetY,
            frameWidth,
            frameHeight);
    }

    private static void ValidateHeaderText(string value)
    {
        if (string.IsNullOrWhiteSpace(value) ||
            value.Length > 128 ||
            value.Any(char.IsControl))
        {
            throw new AtlasMetadataException(AtlasMetadataCodes.HeaderInvalid);
        }
    }

    private static string NormalizeLogicalPath(string value)
    {
        if (string.IsNullOrWhiteSpace(value) ||
            value.Length != value.Trim().Length ||
            value.Any(char.IsControl))
        {
            throw new AtlasMetadataException(AtlasMetadataCodes.PathInvalid);
        }

        var normalized = value
            .Normalize(NormalizationForm.FormC)
            .Replace('\\', '/');
        if (normalized.StartsWith('/') ||
            normalized.EndsWith('/') ||
            normalized.Contains(':') ||
            normalized.IndexOfAny(DisallowedPathCharacters) >= 0)
        {
            throw new AtlasMetadataException(AtlasMetadataCodes.PathInvalid);
        }

        var segments = normalized.Split('/');
        if (segments.Any(IsInvalidPathSegment))
        {
            throw new AtlasMetadataException(AtlasMetadataCodes.PathInvalid);
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
        var stem = extensionIndex < 0
            ? segment
            : segment[..extensionIndex];
        return ReservedWindowsNames.Contains(stem);
    }

    private sealed class BoundedInput
    {
        private readonly Stream _stream;
        private readonly int _maximumBytes;
        private int _bytesRead;

        public BoundedInput(Stream stream, int maximumBytes)
        {
            _stream = stream;
            _maximumBytes = maximumBytes;
        }

        public short ReadInt16()
        {
            Span<byte> bytes = stackalloc byte[sizeof(short)];
            ReadExact(bytes);
            return BinaryPrimitives.ReadInt16LittleEndian(bytes);
        }

        public int ReadInt32()
        {
            Span<byte> bytes = stackalloc byte[sizeof(int)];
            ReadExact(bytes);
            return BinaryPrimitives.ReadInt32LittleEndian(bytes);
        }

        public string ReadString(int maximumStringBytes)
        {
            var length = Read7BitEncodedLength();
            if (length > maximumStringBytes)
            {
                throw new AtlasMetadataException(AtlasMetadataCodes.StringTooLong);
            }

            var bytes = new byte[length];
            ReadExact(bytes);
            try
            {
                return StrictUtf8.GetString(bytes);
            }
            catch (DecoderFallbackException exception)
            {
                throw new AtlasMetadataException(
                    AtlasMetadataCodes.Utf8Invalid,
                    exception);
            }
        }

        public bool TryReadByte(out byte value)
        {
            var next = _stream.ReadByte();
            if (next < 0)
            {
                value = 0;
                return false;
            }

            AccountForRead(1);
            value = (byte)next;
            return true;
        }

        private int Read7BitEncodedLength()
        {
            var value = 0;
            for (var index = 0; index < 5; index++)
            {
                var current = ReadRequiredByte();
                if (index == 4 && (current & 0xF0) != 0)
                {
                    throw new AtlasMetadataException(
                        AtlasMetadataCodes.StringLengthInvalid);
                }

                value |= (current & 0x7F) << (index * 7);
                if ((current & 0x80) == 0)
                {
                    if (value < 0 ||
                        (index > 0 && value < (1 << (index * 7))))
                    {
                        throw new AtlasMetadataException(
                            AtlasMetadataCodes.StringLengthInvalid);
                    }

                    return value;
                }
            }

            throw new AtlasMetadataException(
                AtlasMetadataCodes.StringLengthInvalid);
        }

        private byte ReadRequiredByte()
        {
            if (!TryReadByte(out var value))
            {
                throw new AtlasMetadataException(AtlasMetadataCodes.Truncated);
            }

            return value;
        }

        private void ReadExact(Span<byte> buffer)
        {
            var offset = 0;
            while (offset < buffer.Length)
            {
                var count = _stream.Read(buffer[offset..]);
                if (count == 0)
                {
                    throw new AtlasMetadataException(AtlasMetadataCodes.Truncated);
                }

                AccountForRead(count);
                offset += count;
            }
        }

        private void AccountForRead(int count)
        {
            _bytesRead = checked(_bytesRead + count);
            if (_bytesRead > _maximumBytes)
            {
                throw new AtlasMetadataException(AtlasMetadataCodes.InputTooLarge);
            }
        }
    }
}
