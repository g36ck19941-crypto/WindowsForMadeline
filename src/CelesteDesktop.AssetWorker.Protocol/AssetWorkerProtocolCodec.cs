using System.Buffers.Binary;
using System.Text.Json;
using System.Text.Json.Serialization;
using CelesteDesktop.Contracts.AssetWorker;

namespace CelesteDesktop.AssetWorker.Protocol;

public sealed class AssetWorkerProtocolCodec
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        MaxDepth = 8,
        PropertyNameCaseInsensitive = false,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow
    };

    public byte[] Encode(AssetWorkerEnvelope envelope)
    {
        ValidateEnvelope(envelope);

        byte[] payload;
        try
        {
            payload = JsonSerializer.SerializeToUtf8Bytes(envelope, SerializerOptions);
        }
        catch (Exception exception) when (
            exception is JsonException or NotSupportedException)
        {
            throw new AssetWorkerProtocolException(
                AssetWorkerProtocolCodes.JsonInvalid,
                exception);
        }

        if (payload.Length > AssetWorkerProtocolConstants.MaximumPayloadBytes)
        {
            throw new AssetWorkerProtocolException(
                AssetWorkerProtocolCodes.FrameTooLarge);
        }

        var frame = new byte[
            AssetWorkerProtocolConstants.LengthPrefixBytes + payload.Length];
        BinaryPrimitives.WriteInt32LittleEndian(frame, payload.Length);
        payload.CopyTo(frame.AsSpan(AssetWorkerProtocolConstants.LengthPrefixBytes));
        return frame;
    }

    public async ValueTask WriteAsync(
        Stream stream,
        AssetWorkerEnvelope envelope,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(stream);

        var frame = Encode(envelope);
        await stream.WriteAsync(frame, cancellationToken).ConfigureAwait(false);
        await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
    }

    public async ValueTask<AssetWorkerEnvelope> ReadAsync(
        Stream stream,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(stream);

        var header = new byte[AssetWorkerProtocolConstants.LengthPrefixBytes];
        await ReadExactAsync(stream, header, cancellationToken).ConfigureAwait(false);

        var payloadLength = BinaryPrimitives.ReadInt32LittleEndian(header);
        if (payloadLength <= 0)
        {
            throw new AssetWorkerProtocolException(
                AssetWorkerProtocolCodes.FrameLengthInvalid);
        }

        if (payloadLength > AssetWorkerProtocolConstants.MaximumPayloadBytes)
        {
            throw new AssetWorkerProtocolException(
                AssetWorkerProtocolCodes.FrameTooLarge);
        }

        var payload = new byte[payloadLength];
        await ReadExactAsync(stream, payload, cancellationToken).ConfigureAwait(false);

        AssetWorkerEnvelope? envelope;
        try
        {
            envelope = JsonSerializer.Deserialize<AssetWorkerEnvelope>(
                payload,
                SerializerOptions);
        }
        catch (Exception exception) when (
            exception is JsonException or NotSupportedException)
        {
            throw new AssetWorkerProtocolException(
                AssetWorkerProtocolCodes.JsonInvalid,
                exception);
        }

        if (envelope is null)
        {
            throw new AssetWorkerProtocolException(
                AssetWorkerProtocolCodes.JsonInvalid);
        }

        ValidateEnvelope(envelope);
        return envelope;
    }

    public void ValidateEnvelope(AssetWorkerEnvelope envelope)
    {
        ArgumentNullException.ThrowIfNull(envelope);

        if (envelope.ProtocolVersion != AssetWorkerProtocolConstants.Version)
        {
            throw new AssetWorkerProtocolException(
                AssetWorkerProtocolCodes.VersionUnsupported);
        }

        if (envelope.MessageType == AssetWorkerMessageType.Unknown ||
            !Enum.IsDefined(envelope.MessageType))
        {
            throw new AssetWorkerProtocolException(
                AssetWorkerProtocolCodes.MessageInvalid);
        }

        if (envelope.RequestId == Guid.Empty)
        {
            throw new AssetWorkerProtocolException(
                AssetWorkerProtocolCodes.RequestIdInvalid);
        }

        var hasErrorCode = !string.IsNullOrWhiteSpace(envelope.ErrorCode);
        if (envelope.MessageType == AssetWorkerMessageType.Error)
        {
            if (!hasErrorCode ||
                envelope.ErrorCode!.Length >
                AssetWorkerProtocolConstants.MaximumErrorCodeCharacters)
            {
                throw new AssetWorkerProtocolException(
                    AssetWorkerProtocolCodes.ErrorCodeInvalid);
            }

            return;
        }

        if (hasErrorCode)
        {
            throw new AssetWorkerProtocolException(
                AssetWorkerProtocolCodes.ErrorCodeInvalid);
        }
    }

    private static async ValueTask ReadExactAsync(
        Stream stream,
        Memory<byte> buffer,
        CancellationToken cancellationToken)
    {
        var offset = 0;
        while (offset < buffer.Length)
        {
            var count = await stream.ReadAsync(
                buffer[offset..],
                cancellationToken).ConfigureAwait(false);
            if (count == 0)
            {
                throw new AssetWorkerProtocolException(
                    AssetWorkerProtocolCodes.FrameTruncated);
            }

            offset += count;
        }
    }
}
