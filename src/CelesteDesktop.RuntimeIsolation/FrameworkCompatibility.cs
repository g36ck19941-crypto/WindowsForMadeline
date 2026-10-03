#if NETFRAMEWORK
// Own compiler compatibility marker only; not a gameplay or framework implementation.
[assembly: System.Runtime.Versioning.TargetFramework(".NETFramework,Version=v4.7.2")]
namespace System.Runtime.CompilerServices
{
    internal sealed class IsExternalInit { }
}
#endif
