// Own net472/x86 diagnostic only; never original behavior or an XNA host.
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Threading;
[assembly: TargetFramework(".NETFramework,Version=v4.7.2")]
internal static class FrameworkChild
{
    [DllImport("kernel32.dll")] static extern IntPtr GetCurrentProcess();
    [DllImport("kernel32.dll", SetLastError=true)] [return: MarshalAs(UnmanagedType.Bool)]
    static extern bool GetProcessMitigationPolicy(IntPtr process, int policy, out uint flags, UIntPtr size);
    [DllImport("kernel32.dll", SetLastError=true)] [return: MarshalAs(UnmanagedType.Bool)]
    static extern bool IsProcessInJob(IntPtr process, IntPtr job, [MarshalAs(UnmanagedType.Bool)] out bool assigned);
    public static int Main(string[] args)
    {
        Guid id;
        if (args.Length != 3 || args[0] != "--child" || !Guid.TryParseExact(args[2], "N", out id) ||
            Array.IndexOf(new[] { "Complete", "EarlyExit", "Hang", "BeforeResumeAbort", "CloseJobAfterReady", "ParentFailureAfterReady" }, args[1]) < 0) return 31;
        if (IntPtr.Size != 4) return 32;
        var directory=Path.Combine(Directory.GetCurrentDirectory(), "artifacts", "cdr-082-restricted-process", "runs", args[2]);
        if (!File.Exists(Path.Combine(Directory.GetCurrentDirectory(), "CelesteDesktopRuntime.sln"))) return 33;
        NoLinks(directory);
        Phase(directory,args,0,"own-entry-file-written");
        foreach(var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            var name=assembly.GetName().Name;
            if (name != "mscorlib" && name != "framework-child") return 34; // Fixed own executable + BCL only.
        }
        Phase(directory,args,1,"own-assembly-check-completed");
        uint flags; bool assigned;
        if (!GetProcessMitigationPolicy(GetCurrentProcess(),4,out flags,new UIntPtr(4))) return 35;
        Phase(directory,args,2,"gui-policy-query-returned");
        if (!IsProcessInJob(GetCurrentProcess(),IntPtr.Zero,out assigned) || !assigned || (flags&1)==0 || (flags&2)!=0) return 36;
        Phase(directory,args,3,"job-query-verified");
        Report(directory,"ready",args,"ready");
        if(args[1]=="EarlyExit") return 23;
        if(args[1]!="Complete") Thread.Sleep(30000);
        Report(directory,"complete",args,"completed");
        return 0;
    }
    static void NoLinks(string path)
    {
        for(var current=Path.GetFullPath(path);current!=null;current=Path.GetDirectoryName(current))
            if((Directory.Exists(current)||File.Exists(current)) && (File.GetAttributes(current)&FileAttributes.ReparsePoint)!=0)
                throw new IOException("OWN_FRAMEWORK_LINK");
    }
    static void Phase(string directory,string[] args,int index,string stage)
    {
        var path=Path.Combine(directory,"phase-"+index+".txt"); NoLinks(path);
        File.WriteAllText(path,args[2]+"\n"+args[1]+"\n"+stage);
    }
    static void Report(string directory,string name,string[] args,string stage)
    {
        var path=Path.Combine(directory,name+".json"); var temp=path+".tmp"; NoLinks(path); NoLinks(temp);
        File.WriteAllText(temp,"{\"runId\":\""+args[2]+"\",\"scenario\":\""+args[1]+"\",\"stage\":\""+stage+"\",\"guiDenied\":true,\"inJob\":true}");
        File.Move(temp,path);
    }
}
