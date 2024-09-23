using Mono.Cecil;
using System.Collections.Generic;
using System.IO;
using static SharkyPatcher.Common.LoggerUtil;

namespace SharkyPatcher.Resolver
{
    public class CustomResolver : BaseAssemblyResolver
    {
        protected readonly List<string> _directories;

        public CustomResolver(string customDirectory)
        {
            _directories = new List<string>
            {
                customDirectory
            };
            LogF.Information($"【鯊鯊補丁】自定義解析器在以下目錄初始化：{customDirectory}");
        }

        public override AssemblyDefinition Resolve(AssemblyNameReference name)
        {
            // Use a default ReaderParameters if none are provided
            return Resolve(name, new ReaderParameters());
        }

        public override AssemblyDefinition Resolve(AssemblyNameReference name, ReaderParameters parameters)
        {
            foreach (var directory in _directories)
            {
                // try to resolve from the custom directory.
                string assemblyPath = Path.Combine(directory, name.Name + ".dll");
                LogF.Information($"【鯊鯊補丁】正在嘗試從 {assemblyPath} 解析 {name.Name}");

                if (File.Exists(assemblyPath))
                {
                    LogF.Information($"【鯊鯊補丁】成功從自定義目錄解析到了 {name.Name}");
                    parameters.AssemblyResolver = this; // Ensure the custom resolver is used for nested resolutions
                    return GetAssembly(assemblyPath, parameters);
                }
            }
            
            LogF.Information($"【鯊鯊補丁】未在自定義目錄中解析到 {name.Name}，繼續使用默認解析器解析");
            // If not found in the custom directory, fall back to the default base resolver.
            return base.Resolve(name, parameters);
        }

        AssemblyDefinition GetAssembly(string file, ReaderParameters parameters)
        {
            if (parameters.AssemblyResolver == null)
                parameters.AssemblyResolver = this;

            return ModuleDefinition.ReadModule(file, parameters).Assembly;
        }
    }
}