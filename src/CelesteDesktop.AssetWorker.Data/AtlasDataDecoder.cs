using System.Buffers.Binary;
using CelesteDesktop.Contracts.Assets;

namespace CelesteDesktop.AssetWorker.Data;

public sealed class AtlasDataDecoder
{
    private readonly AtlasDataDecoderOptions _options;

    public AtlasDataDecoder(AtlasDataDecoderOptions? options = null)
    {
        _options = options ?? AtlasDataDecoderOptions.Default;
        _options.Validate();
    }

    public Bgra32Frame Decode(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);
        if (!stream.CanRead)
        {
            throw new ArgumentException("The atlas data stream must be readable.", nameof(stream));
        }

        var input = new BoundedInput(stream, _options.MaximumInputBytes);
        var width = input.ReadInt32();
        var height = input.ReadInt32();
        ValidateDimensions(width, height);

        var outputLength = GetOutputLength(width, height);
        var alphaFlag = input.ReadRequiredByte();
        if (alphaFlag > 1)
        {
            throw new AtlasDataException(AtlasDataCodes.AlphaFlagInvalid);
        }

        var hasAlpha = alphaFlag == 1;
        var pixels = new byte[outputLength];
        var pixelCount = outputLength / 4;
        var decodedPixels = 0;

        while (decodedPixels < pixelCount)
        {
            var runLength = input.ReadRequiredByte();
            if (runLength == 0)
            {
                throw new AtlasDataException(AtlasDataCodes.RunLengthInvalid);
            }

            if (runLength > pixelCount - decodedPixels)
            {
                throw new AtlasDataException(AtlasDataCodes.RunOverflow);
            }

            var alpha = hasAlpha ? input.ReadRequiredByte() : byte.MaxValue;
            byte blue = 0;
            byte green = 0;
            byte red = 0;
            if (alpha != 0)
            {
                blue = input.ReadRequiredByte();
                green = input.ReadRequiredByte();
                red = input.ReadRequiredByte();
            }

            for (var index = 0; index < runLength; index++)
            {
                var offset = (decodedPixels + index) * 4;
                pixels[offset] = blue;
                pixels[offset + 1] = green;
                pixels[offset + 2] = red;
                pixels[offset + 3] = alpha;
            }

            decodedPixels += runLength;
        }

        if (input.TryReadByte(out _))
        {
            throw new AtlasDataException(AtlasDataCodes.TrailingData);
        }

        return new Bgra32Frame(width, height, pixels);
    }

    private void ValidateDimensions(int width, int height)
    {
        if (width <= 0 || height <= 0 ||
            width > _options.MaximumDimension ||
            height > _options.MaximumDimension)
        {
            throw new AtlasDataException(AtlasDataCodes.DimensionsInvalid);
        }
    }

    private int GetOutputLength(int width, int height)
    {
        try
        {
            var length = checked(width * height * 4);
            if (length > _options.MaximumOutputBytes)
            {
                throw new AtlasDataException(AtlasDataCodes.OutputTooLarge);
            }

            return length;
        }
        catch (OverflowException)
        {
            throw new AtlasDataException(AtlasDataCodes.OutputTooLarge);
        }
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

        public int ReadInt32()
        {
            Span<byte> bytes = stackalloc byte[sizeof(int)];
            ReadExact(bytes);
            return BinaryPrimitives.ReadInt32LittleEndian(bytes);
        }

        public byte ReadRequiredByte()
        {
            if (!TryReadByte(out var value))
            {
                throw new AtlasDataException(AtlasDataCodes.Truncated);
            }

            return value;
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

        private void ReadExact(Span<byte> buffer)
        {
            var offset = 0;
            while (offset < buffer.Length)
            {
                var count = _stream.Read(buffer[offset..]);
                if (count == 0)
                {
                    throw new AtlasDataException(AtlasDataCodes.Truncated);
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
                throw new AtlasDataException(AtlasDataCodes.InputTooLarge);
            }
        }
    }
}
