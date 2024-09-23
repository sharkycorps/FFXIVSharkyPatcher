using System.IO;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using SharkyPatcher.Common;
using SharkyPatcher.Resolver;
using static SharkyPatcher.Common.LoggerUtil;

namespace SharkyPatcher.Patcher
{
    class DalamudPatcher : BasePatcher
    {
        readonly static string _assemblyName = "Dalamud.dll";
        protected override string _patchDesc { get; } = "應用補丁，移除了用戶協議、跟踪代碼以及插件屏蔽";
        public DalamudPatcher(string baseDir, DalamudVersionInfo version) 
            : this(baseDir, _assemblyName, version) {}
        public DalamudPatcher(string baseDir, string assemblyName, DalamudVersionInfo version) 
            : base(Path.Combine(baseDir, @"addon\Hooks", version.AssemblyVersion), assemblyName, 
                new DalamudResolver(baseDir, version)) {}

        void PatchAnalytics()
        {
            Log.Information($"【鯊鯊補丁】【{_assemblyName}】正在嘗試移除跟踪代碼……");

            string targetTypeName = "Dalamud.Support.EventTracking";
            ModuleDefinition targetModule = _targetAssembly.MainModule;
            TypeDefinition targetType = targetModule.GetType(targetTypeName);
            if (targetType == null)
            {
                Log.Information($"【鯊鯊補丁】【{_assemblyName}】未找到跟踪類 <{targetTypeName}>，已跳過");
                return;
            }

            // remove analytics url
            string targetFieldName = "AnalyticsUrl";
            FieldDefinition targetField = targetType.Fields.FirstOrDefault(f => f.Name == targetFieldName);
            if (targetField == null || targetField.HasConstant == false)
            {
                Log.Information($"【鯊鯊補丁】【{_assemblyName}】未找到跟踪連結 <{targetFieldName}>，已跳過");
            }
            else
            {
                targetField.Constant = "";
            }

            // clear send measurement method
            string targetMethodName = "SendMeasurement";
            MethodDefinition targetMethod = targetType.Methods.FirstOrDefault(m => m.Name == targetMethodName);
            if (targetMethod == null)
            {
                Log.Information($"【鯊鯊補丁】【{_assemblyName}】未找到跟踪函式 <{targetMethodName}>，已跳過");
            }
            else
            {
                ILProcessor ilProcessor = targetMethod.Body.GetILProcessor();
                ilProcessor.Body.ExceptionHandlers.Clear();
                ilProcessor.Body.Variables.Clear();
                ilProcessor.Body.Instructions.Clear();
                ilProcessor.Append(ilProcessor.Create(OpCodes.Ret));
            }
        }

        void PatchToSWindow()
        {
            Log.Information($"【鯊鯊補丁】【{_assemblyName}】正在嘗試移除用戶協議視窗……");

            string targetTypeName = "Dalamud.Interface.Internal.Windows.ToSWindow";
            ModuleDefinition targetModule = _targetAssembly.MainModule;
            TypeDefinition targetType = targetModule.GetType(targetTypeName);
            if (targetType == null) {
                Log.Information($"【鯊鯊補丁】【{_assemblyName}】未找到用戶協議視窗 <{targetTypeName}>，已跳過");
                return;
            }

            // clear get tos method
            string targetMethodName = "GetRemoteTOS";
            MethodDefinition targetMethod = targetType.Methods.FirstOrDefault(m => m.Name == targetMethodName);
            if (targetMethod == null)
            {
                Log.Information($"【鯊鯊補丁】【{_assemblyName}】未找到用戶協議請求函式 <{targetMethodName}>，已跳過");
            }
            else
            {
                ILProcessor ilProcessor = targetMethod.Body.GetILProcessor();
                ilProcessor.Body.ExceptionHandlers.Clear();
                ilProcessor.Body.Variables.Clear();
                ilProcessor.Body.Instructions.Clear();
                ilProcessor.Append(ilProcessor.Create(OpCodes.Ret));
            }

            // clear draw method
            string drawMethodName = "Draw";
            MethodDefinition drawMethod = targetType.Methods.FirstOrDefault(m => m.Name == drawMethodName);
            if (targetMethod == null)
            {
                Log.Information($"【鯊鯊補丁】【{_assemblyName}】未找到用戶協議繪製函式 <{drawMethodName}>，已跳過");
            }
            else
            {
                ILProcessor drawIlProcessor = drawMethod.Body.GetILProcessor();
                drawIlProcessor.Body.ExceptionHandlers.Clear();
                drawIlProcessor.Body.Variables.Clear();
                drawIlProcessor.Body.Instructions.Clear();

                // set isOpen false
                string windowTypeName = "Dalamud.Interface.Windowing.Window";
                string setIsOpenMethodName = "set_IsOpen";
                TypeDefinition windowType = targetModule.GetType(windowTypeName);
                MethodDefinition setIsOpenMethod = windowType.Methods.First(m => m.Name == setIsOpenMethodName);
                drawIlProcessor.Append(drawIlProcessor.Create(OpCodes.Ldarg_0));
                drawIlProcessor.Append(drawIlProcessor.Create(OpCodes.Ldc_I4_0));
                drawIlProcessor.Append(drawIlProcessor.Create(OpCodes.Call, targetModule.ImportReference(setIsOpenMethod)));
                drawIlProcessor.Append(drawIlProcessor.Create(OpCodes.Ret));
            }
        }

        void PatchRemoteTos()
        {
            Log.Information($"【鯊鯊補丁】【{_assemblyName}】正在嘗試移除用戶協議連結……");

            string targetTypeName = "Dalamud.Utility.Util";
            ModuleDefinition targetModule = _targetAssembly.MainModule;
            TypeDefinition targetType = targetModule.GetType(targetTypeName);

            // remove tos url
            string targetFieldName = "TOSRemoteUrl";
            FieldDefinition targetField = targetType.Fields.FirstOrDefault(f => f.Name == targetFieldName);
            if (targetField == null) {
                Log.Information($"【鯊鯊補丁】【{_assemblyName}】未找到用戶協議連結 <{targetFieldName}>，已跳過");
            }
            else
            {
                MethodDefinition staticConstructor = targetType.Methods.FirstOrDefault(m => m.IsConstructor && m.IsStatic);
                Instruction targetInstruction = staticConstructor.Body.Instructions.FirstOrDefault(i =>
                    i.OpCode == OpCodes.Stsfld && ((FieldReference)i.Operand).FullName == targetField.FullName);
                Instruction loadStringInstruction = targetInstruction.Previous;
                if (loadStringInstruction.OpCode == OpCodes.Ldstr)
                {
                    loadStringInstruction.Operand = string.Empty;
                }
            }
            
            // return empty tos hash
            string targetMethodName = "GetRemoteTOSHash";
            MethodDefinition targetMethod = targetType.Methods.FirstOrDefault(m => m.Name == targetMethodName);
            if (targetMethod == null)
            {
                Log.Information($"【鯊鯊補丁】【{_assemblyName}】未找到用戶協議請求函式 <{targetMethodName}>，已跳過");
            }
            else
            {
                ILProcessor ilProcessor = targetMethod.Body.GetILProcessor();
                ilProcessor.Body.ExceptionHandlers.Clear();
                ilProcessor.Body.Variables.Clear();
                ilProcessor.Body.Instructions.Clear();
                ilProcessor.Append(ilProcessor.Create(OpCodes.Ldstr, string.Empty));
                ilProcessor.Append(ilProcessor.Create(OpCodes.Ret));
            }
        }
        void PatchPluginBans()
        {
            Log.Information($"【鯊鯊補丁】【{_assemblyName}】正在嘗試移除插件屏蔽……");

            string targetTypeName = "Dalamud.Plugin.Internal.PluginManager";
            ModuleDefinition targetModule = _targetAssembly.MainModule;
            TypeDefinition targetType = targetModule.GetType(targetTypeName);
            MethodDefinition targetMethod = targetType.Methods.First(m => m.IsConstructor && !m.IsStatic && m.Parameters.Count == 2);
            ILProcessor ilProcessor = targetMethod.Body.GetILProcessor();

            // skip if no ban
            string bannedPluginJsonName = "bannedplugin.json";
            if (!targetMethod.Body.Instructions.Any(i => i.OpCode == OpCodes.Ldstr
                && ((string)i.Operand) == bannedPluginJsonName)) 
            {
                Log.Information($"【鯊鯊補丁】【{_assemblyName}】未找到插件屏蔽列表 <{bannedPluginJsonName}>，已跳過");
                return;
            }

            Instruction stfldInstruction = targetMethod.Body.Instructions.FirstOrDefault(i => i.OpCode == OpCodes.Stfld
                && ((FieldReference)i.Operand).Name == "bannedPlugins");
            // skip if patched or changed
            if (stfldInstruction.Previous.OpCode == OpCodes.Call && stfldInstruction.Previous.Previous.OpCode == OpCodes.Call) {
                // remove unused instruction
                ilProcessor.Remove(stfldInstruction.Previous.Previous.Previous.Previous);
                ilProcessor.Remove(stfldInstruction.Previous.Previous.Previous);

                // create zero length array
                TypeDefinition bannedPluginType = targetModule.GetType("Dalamud.Plugin.Internal.Types.BannedPlugin");
                stfldInstruction.Previous.Previous.OpCode = OpCodes.Ldc_I4_0;
                stfldInstruction.Previous.Previous.Operand = null;
                stfldInstruction.Previous.OpCode = OpCodes.Newarr;
                stfldInstruction.Previous.Operand = targetModule.ImportReference(bannedPluginType);
            } else {
                Log.Information($"【鯊鯊補丁】【{_assemblyName}】已移除過插件屏蔽，已跳過");
            }
        }

        void AddSettingsTitle()
        {
            string targetTypeName = "Dalamud.Interface.Internal.Windows.Settings.SettingsWindow";
            ModuleDefinition targetModule = _targetAssembly.MainModule;
            TypeDefinition targetType = targetModule.GetType(targetTypeName);
            MethodDefinition targetMethod = targetType.Methods.First(m => m.IsConstructor && !m.IsStatic && m.Parameters.Count == 0);
            Instruction strInstruction = targetMethod.Body.Instructions.FirstOrDefault(i => i.OpCode == OpCodes.Ldstr
                && ((string)i.Operand).Contains("###XlSettings2"));
            strInstruction.Operand = " | Sharky Patched###XlSettings2";
        }
        public override void Patch()
        {
            // remove analytics
            PatchAnalytics();

            // remove tos
            PatchToSWindow();
            PatchRemoteTos();

            // remove plugin ban
            PatchPluginBans();

            // add sharky to settings title
            AddSettingsTitle();

            // write back to dll
            SaveAssembly();
        }
    }
}