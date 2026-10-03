namespace CelesteDesktop.RuntimeIsolation;

[Flags]
public enum Buttons { None = 0, Jump = 1, Dash = 2, Grab = 4 }

public readonly record struct InjectedInput(int MoveX, int MoveY, Buttons Held);
public readonly record struct StepContext(long Tick, double ElapsedSeconds, float DeltaSeconds,
    InjectedInput Input, Buttons Pressed, Buttons Released);
public enum ExternalService { Steam, Audio, Window, GraphicsDevice, LiveInput, AssetRead, FileWrite, OriginalCode }
public readonly record struct ServiceDecision(bool Allowed, string Code, ExternalService Service);

/// <summary>Own managed context adapter, not gameplay and not a process security sandbox.
/// Does not bind or execute Celeste. All external services fail closed.</summary>
public sealed class IsolationSession : IDisposable
{
    public const int MaximumFrames = 4096;
    public const float FixedDeltaSeconds = 1f / 60f;
    private readonly InjectedInput[] frames;
    private int cursor;
    private bool disposed;
    private Buttons previous;

    public IsolationSession(IReadOnlyList<InjectedInput> generatedFrames)
    {
        ArgumentNullException.ThrowIfNull(generatedFrames);
        if (generatedFrames.Count is < 1 or > MaximumFrames)
            throw new ArgumentOutOfRangeException(nameof(generatedFrames), "ISOLATION_INPUT_COUNT_INVALID");
        frames = new InjectedInput[generatedFrames.Count];
        for (var i = 0; i < frames.Length; i++)
        {
            var frame = generatedFrames[i];
            if (frame.MoveX is < -1 or > 1 || frame.MoveY is < -1 or > 1 ||
                (frame.Held & ~(Buttons.Jump | Buttons.Dash | Buttons.Grab)) != 0)
                throw new ArgumentException("ISOLATION_INPUT_INVALID", nameof(generatedFrames));
            frames[i] = frame;
        }
    }

    public bool OriginalBound => false;
    public string BindingStatus => "ORIGINAL_BRIDGE_NOT_BOUND";
    public int RemainingFrames { get { RequireOpen(); return frames.Length - cursor; } }

    // Advances only the adapter's injected context. Never calls a recovered constructor/update.
    public StepContext Advance()
    {
        RequireOpen();
        if (cursor == frames.Length) throw new InvalidOperationException("ISOLATION_INPUT_EXHAUSTED");
        var input = frames[cursor];
        var context = new StepContext(cursor, cursor / 60d, FixedDeltaSeconds, input,
            input.Held & ~previous, previous & ~input.Held);
        previous = input.Held;
        cursor++;
        return context;
    }

    public ServiceDecision RequestService(ExternalService service)
    {
        RequireOpen();
        if (!Enum.IsDefined(service)) throw new ArgumentOutOfRangeException(nameof(service), "ISOLATION_SERVICE_INVALID");
        return new ServiceDecision(false, "ISOLATION_SERVICE_DENIED", service);
    }

    private void RequireOpen()
    {
        if (disposed) throw new ObjectDisposedException(nameof(IsolationSession), "ISOLATION_SESSION_CLOSED");
    }

    public void Dispose() { disposed = true; }
}
