
using System.IO;
using SharkyPatcher.Common;
using static SharkyPatcher.Common.LoggerUtil;

namespace SharkyPatcher.Resolver
{
    public class DalamudResolver : CustomResolver
    {
        public DalamudResolver(string baseDir, DalamudVersionInfo version)
            : base(Path.Combine(baseDir, @"addon\Hooks", version.AssemblyVersion))
        {
            var runtimeDir = Path.Combine(baseDir, @"runtime\shared");
            var NETCoreRuntimeDir =  Path.Combine(runtimeDir, "Microsoft.NETCore.App", version.RuntimeVersion);
            var WINDesktopRuntimeDir = Path.Combine(runtimeDir, "Microsoft.WindowsDesktop.App", version.RuntimeVersion);
            _directories.Add(NETCoreRuntimeDir);
            _directories.Add(WINDesktopRuntimeDir);
            LogF.Information($"【鯊鯊補丁】自定義解析器在以下目錄初始化：{NETCoreRuntimeDir}");
            LogF.Information($"【鯊鯊補丁】自定義解析器在以下目錄初始化：{WINDesktopRuntimeDir}");
        }
    }
}