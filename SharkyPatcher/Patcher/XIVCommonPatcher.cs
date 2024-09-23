using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using static SharkyPatcher.Common.LoggerUtil;

namespace SharkyPatcher.Patcher
{
    class XIVCommonPatcher : BasePatcher
    {
        readonly static string _assemblyName = "XIVLauncher.Common.dll";
        protected override string _patchDesc { get; } = "應用補丁，移除了对 Dalamud.dll 的完整性檢查";
        public XIVCommonPatcher(string assemblyDir) : base(assemblyDir, _assemblyName) { }
        public XIVCommonPatcher(string assemblyDir, string assemblyName) : base(assemblyDir, assemblyName) { }

        public override void Patch()
        {
            Log.Information($"【鯊鯊補丁】【{_assemblyName}】正在嘗試移除完整性檢查……");

            // get target type and method
            string targetTypeName = "XIVLauncher.Common.Dalamud.DalamudUpdater";
            string targetMethodName = "CheckIntegrity";
            ModuleDefinition targetModule = _targetAssembly.MainModule;
            TypeDefinition targetType = targetModule.GetType(targetTypeName);
            MethodDefinition targetMethod = targetType.Methods.FirstOrDefault(m => m.Name == targetMethodName);
            if (targetMethod == null)
            {
                Log.Information($"【鯊鯊補丁】【{_assemblyName}】未找到完整性檢查函式 <{targetMethodName}>，已跳過");
                return;
            }

            if (targetMethod.Body.Instructions.Any(i => i.OpCode == OpCodes.Ldstr
                && ((string)i.Operand) == "Dalamud.dll")) 
            {
                Log.Information($"【鯊鯊補丁】【{_assemblyName}】已移除過完整性檢查，已跳過");
                return;
            }

            // load Newtonsoft.Json referenced by target dll
            AssemblyNameReference jsonReference = targetModule.AssemblyReferences.First(reference => reference.Name == "Newtonsoft.Json");
            ModuleDefinition jsonModule = _resolver.Resolve(jsonReference).MainModule;

            // find jobject methods reference, tostring from base object
            TypeDefinition jObjectType = jsonModule.GetType("Newtonsoft.Json.Linq.JObject");
            MethodDefinition parseMethod = jObjectType.Methods.First(m => m.Name == "Parse" && m.Parameters.Count == 1);
            MethodDefinition removeMethod = jObjectType.Methods.First(m => m.Name == "Remove" && m.Parameters.Count == 1);
            TypeDefinition objType = targetModule.TypeSystem.Object.Resolve();
            MethodDefinition toStringMethod = objType.Methods.First(m => m.Name == "ToString" && m.Parameters.Count == 0);

            TypeReference jObjectTypeRef = targetModule.ImportReference(jObjectType);
            MethodReference parseMethodRef = targetModule.ImportReference(parseMethod);
            MethodReference removeMethodRef = targetModule.ImportReference(removeMethod);
            MethodReference toStringMethodRef = targetModule.ImportReference(toStringMethod);

            // insert il code before target method body
            ILProcessor ilProcessor = targetMethod.Body.GetILProcessor();
            Instruction firstInstruction = targetMethod.Body.Instructions.First();

            // add temp jobject variable
            VariableDefinition jObjVar = new VariableDefinition(jObjectTypeRef);
            targetMethod.Body.Variables.Add(jObjVar);

            // load second json str parameter and parse to jobject
            ilProcessor.InsertBefore(firstInstruction, ilProcessor.Create(OpCodes.Ldarg_1));
            ilProcessor.InsertBefore(firstInstruction, ilProcessor.Create(OpCodes.Call, parseMethodRef));
            ilProcessor.InsertBefore(firstInstruction, ilProcessor.Create(OpCodes.Stloc, jObjVar));

            // delete target property from the jobject
            ilProcessor.InsertBefore(firstInstruction, ilProcessor.Create(OpCodes.Ldloc, jObjVar));
            ilProcessor.InsertBefore(firstInstruction, ilProcessor.Create(OpCodes.Ldstr, "Dalamud.dll"));
            ilProcessor.InsertBefore(firstInstruction, ilProcessor.Create(OpCodes.Callvirt, removeMethodRef));
            ilProcessor.InsertBefore(firstInstruction, ilProcessor.Create(OpCodes.Pop)); // clear the stack

            // convert back to json str and save back to second parameter
            ilProcessor.InsertBefore(firstInstruction, ilProcessor.Create(OpCodes.Ldloc, jObjVar));
            ilProcessor.InsertBefore(firstInstruction, ilProcessor.Create(OpCodes.Callvirt, toStringMethodRef));
            ilProcessor.InsertBefore(firstInstruction, ilProcessor.Create(OpCodes.Starg_S, (byte)1));

            // write back to dll
            SaveAssembly();
        }
    }
}