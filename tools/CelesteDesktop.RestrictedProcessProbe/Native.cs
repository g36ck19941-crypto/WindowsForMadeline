using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32.SafeHandles;

internal sealed class OwnedHandle : SafeHandleZeroOrMinusOneIsInvalid
{
    public OwnedHandle(nint value) : base(true) => SetHandle(value);
    protected override bool ReleaseHandle() => Native.CloseHandle(handle);
}

internal static class Native
{
    internal const uint WaitObject = 0, WaitTimeout = 258;
    internal const uint JobFlags = 0x2000 | 0x8 | 0x200; // kill-on-close, active process count, job commit memory
    internal const ulong GuiPolicy = 1UL << 28;
    [StructLayout(LayoutKind.Sequential)] internal struct StartupInfo
    {
        public uint cb; public nint reserved, desktop, title;
        public uint x, y, xSize, ySize, xChars, yChars, fill, flags;
        public ushort show, reservedSize; public nint reservedBytes, input, output, error;
    }
    [StructLayout(LayoutKind.Sequential)] internal struct StartupInfoEx { public StartupInfo startup; public nint attributes; }
    [StructLayout(LayoutKind.Sequential)] internal struct ProcessInfo { public nint process, thread; public uint pid, tid; }
    [StructLayout(LayoutKind.Sequential)] internal struct BasicLimits
    {
        public long processTime, jobTime; public uint flags; public nuint minWorkingSet, maxWorkingSet;
        public uint activeProcesses; public nuint affinity; public uint priority, scheduling;
    }
    [StructLayout(LayoutKind.Sequential)] internal struct IoCounters { public ulong readOps, writeOps, otherOps, readBytes, writeBytes, otherBytes; }
    [StructLayout(LayoutKind.Sequential)] internal struct ExtendedLimits
    {
        public BasicLimits basic; public IoCounters io; public nuint processMemory, jobMemory, peakProcessMemory, peakJobMemory;
    }
    [StructLayout(LayoutKind.Sequential)] internal struct CpuLimits { public uint flags, rate; }
    [StructLayout(LayoutKind.Sequential)] internal struct Accounting
    {
        public long user, kernel, periodUser, periodKernel;
        public uint pageFaults, totalProcesses, activeProcesses, terminatedProcesses;
    }
    [DllImport("kernel32.dll", SetLastError = true)] internal static extern bool CloseHandle(nint handle);
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)] internal static extern nint CreateJobObjectW(nint attributes, string? name);
    [DllImport("kernel32.dll", SetLastError = true)] internal static extern bool SetInformationJobObject(OwnedHandle job, int kind, nint data, uint size);
    [DllImport("kernel32.dll", SetLastError = true)] internal static extern bool QueryInformationJobObject(OwnedHandle job, int kind, nint data, uint size, nint returned);
    [DllImport("kernel32.dll", SetLastError = true)] internal static extern bool AssignProcessToJobObject(OwnedHandle job, OwnedHandle process);
    [DllImport("kernel32.dll", SetLastError = true)] internal static extern bool IsProcessInJob(nint process, nint job, out bool result);
    [DllImport("kernel32.dll", SetLastError = true)] internal static extern bool GetProcessMitigationPolicy(nint process, int policy, out uint flags, nuint size);
    [DllImport("kernel32.dll")] internal static extern nint GetCurrentProcess();
    [DllImport("kernel32.dll", SetLastError = true)] internal static extern bool InitializeProcThreadAttributeList(nint list, int count, uint flags, ref nuint size);
    [DllImport("kernel32.dll", SetLastError = true)] internal static extern bool UpdateProcThreadAttribute(nint list, uint flags, nuint attribute, nint value, nuint size, nint previous, nint returned);
    [DllImport("kernel32.dll")] internal static extern void DeleteProcThreadAttributeList(nint list);
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    internal static extern bool CreateProcessW(string application, StringBuilder command, nint processAttributes, nint threadAttributes,
        bool inherit, uint flags, nint environment, string directory, ref StartupInfoEx startup, out ProcessInfo information);
    [DllImport("kernel32.dll", SetLastError = true)] internal static extern uint ResumeThread(OwnedHandle thread);
    [DllImport("kernel32.dll", SetLastError = true)] internal static extern uint WaitForSingleObject(OwnedHandle handle, uint milliseconds);
    [DllImport("kernel32.dll", SetLastError = true)] internal static extern bool GetExitCodeProcess(OwnedHandle process, out uint code);
    [DllImport("kernel32.dll", SetLastError = true)] internal static extern bool TerminateJobObject(OwnedHandle job, uint code);
    [DllImport("kernel32.dll", SetLastError = true)] internal static extern bool TerminateProcess(OwnedHandle process, uint code);
    [StructLayout(LayoutKind.Explicit, Size = 176)] internal struct DebugEvent
    {
        [FieldOffset(0)] public uint code;
        [FieldOffset(4)] public uint pid;
        [FieldOffset(8)] public uint tid;
        [FieldOffset(16)] public uint exceptionCode;
        [FieldOffset(16)] public nint fileHandle;
        [FieldOffset(40)] public uint parameterCount;
        [FieldOffset(32)] public nint exceptionAddress;
        [FieldOffset(40)] public nint processImageBase;
        [FieldOffset(24)] public nint dllImageBase;
        [FieldOffset(48)] public nuint parameter0;
        [FieldOffset(56)] public nuint parameter1;
        [FieldOffset(168)] public uint firstChance;
    }
    [DllImport("kernel32.dll", SetLastError = true)] internal static extern bool WaitForDebugEvent(out DebugEvent debugEvent, uint milliseconds);
    [DllImport("kernel32.dll", SetLastError = true)] internal static extern bool ContinueDebugEvent(uint pid, uint tid, uint status);
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)] internal static extern uint GetFinalPathNameByHandleW(nint file, StringBuilder path, uint size, uint flags);
    [StructLayout(LayoutKind.Sequential)] internal struct MemoryInfo
    {
        public nint baseAddress, allocationBase; public uint allocationProtection;
        public ushort partition; public nuint regionSize; public uint state, protection, type;
    }
    [DllImport("kernel32.dll", SetLastError = true)] internal static extern nuint VirtualQueryEx(OwnedHandle process, nint address, out MemoryInfo info, nuint size);

    internal static void Check(bool success, string stage)
    {
        if (!success) throw new Win32Exception(Marshal.GetLastWin32Error(), stage);
    }
    internal static uint Wait(OwnedHandle process, uint milliseconds)
    {
        var result = WaitForSingleObject(process, milliseconds);
        if (result is not WaitObject and not WaitTimeout) throw new Win32Exception(Marshal.GetLastWin32Error(), "OWN_PROCESS_WAIT_FAILED");
        return result;
    }
    internal static void Set<T>(OwnedHandle job, int kind, T value) where T : struct
    {
        var size = Marshal.SizeOf<T>(); var buffer = Marshal.AllocHGlobal(size);
        try { Marshal.StructureToPtr(value, buffer, false); Check(SetInformationJobObject(job, kind, buffer, (uint)size), "JOB_SET_" + kind); }
        finally { Marshal.FreeHGlobal(buffer); }
    }
    internal static T Query<T>(OwnedHandle job, int kind) where T : struct
    {
        var size = Marshal.SizeOf<T>(); var buffer = Marshal.AllocHGlobal(size);
        try { Check(QueryInformationJobObject(job, kind, buffer, (uint)size, 0), "JOB_QUERY_" + kind); return Marshal.PtrToStructure<T>(buffer); }
        finally { Marshal.FreeHGlobal(buffer); }
    }
}
