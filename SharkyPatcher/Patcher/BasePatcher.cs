using System;
using System.IO;
using Mono.Cecil;
using SharkyPatcher.Resolver;
using static SharkyPatcher.Common.LoggerUtil;

namespace SharkyPatcher.Patcher
{
    class BasePatcher
    {
        readonly string _assemblyDir;
        readonly string _assemblyName;
        protected virtual string _patchDesc { get; } = "應用補丁";
        protected AssemblyDefinition _targetAssembly;
        protected CustomResolver _resolver;

        public virtual void Patch() { }

        public BasePatcher(string assemblyDir, string assemblyName) 
            : this(assemblyDir, assemblyName, new CustomResolver(assemblyDir)) {}
        public BasePatcher(string assemblyDir, string assemblyName, CustomResolver resolver)
        {
            _assemblyDir = assemblyDir;
            _assemblyName = assemblyName;
            _resolver = resolver;
            _targetAssembly = LoadAssembly(_assemblyDir, _assemblyName);
        }

        AssemblyDefinition LoadAssembly(string assemblyDir, string assemblyName)
        {
            if (string.IsNullOrEmpty(assemblyDir) || string.IsNullOrEmpty(assemblyName))
            {
                Log.Error($"【鯊鯊補丁】讀取失敗，目標檔案或路徑不存在！");
                Exit();
            }

            // use custom resolver and allow write back
            ReaderParameters parameters = new ReaderParameters
            {
                AssemblyResolver = _resolver,
                ReadWrite = true,
            };

            string fullPath = Path.Combine(assemblyDir, assemblyName);
            return AssemblyDefinition.ReadAssembly(fullPath, parameters);
        }

        protected void SaveAssembly()
        {
            try
            {
                _targetAssembly.Write();
                Log.Information($"【鯊鯊補丁】【{_assemblyName}】已成功{_patchDesc}");
            }
            catch (Exception ex)
            {
                Log.Error($"【鯊鯊補丁】【{_assemblyName}】寫入補丁失敗！");
                _targetAssembly.Dispose();
                Exit(ex);
            }
        }

        public void Dispose ()
		{
			_targetAssembly.Dispose();
		}
    }
}
