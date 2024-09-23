using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using static SharkyPatcher.Common.LoggerUtil;

namespace SharkyPatcher.Patcher
{
    class DUpdaterPatcher : BasePatcher
    {
        readonly static string _assemblyName = "Dalamud.Updater.exe";
        protected override string _patchDesc { get; } = "應用補丁，移除了对 Dalamud.dll 的完整性檢查";
        public DUpdaterPatcher(string assemblyDir) : base(assemblyDir, _assemblyName) { }
        public DUpdaterPatcher(string assemblyDir, string assemblyName) : base(assemblyDir, assemblyName) { }

        public override void Patch()
        {
            Log.Information($"【鯊鯊補丁】【{_assemblyName}】正在嘗試移除完整性檢查……");

            // get target type and method
            string targetTypeName = "XIVLauncher.Common.Dalamud.DalamudUpdater";
            string targetMethodName = "IsIntegrity";
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

            // get target method body
            var ilProcessor = targetMethod.Body.GetILProcessor();

            // find target instruction
            Instruction targetInstruction = null;
            foreach (var instruction in targetMethod.Body.Instructions)
            {
                if (instruction.OpCode == OpCodes.Brfalse_S)
                {
                    var previousInstruction = instruction.Previous;
                    var nextInstruction = instruction.Next;
                    if (previousInstruction != null
                        && previousInstruction.OpCode == OpCodes.Call
                        && ((MethodReference)previousInstruction.Operand).Name == "op_Inequality")
                    {
                        if (nextInstruction != null
                            && nextInstruction.OpCode == OpCodes.Ldstr
                            && ((string)nextInstruction.Operand).Contains("Integrity check failed"))
                        {
                            targetInstruction = instruction;
                            break;
                        }
                    }
                }
            }

            LogF.Information($"【鯊鯊補丁】【{ _assemblyName }】目標指令內容為 { targetInstruction }");

            if (targetInstruction != null)
            {
                var LdKVPairInstruction = targetInstruction.Previous.Previous.Previous; // load keyValuePair inst
                var LdTextInstruction = LdKVPairInstruction.Previous; // load file hash text inst
                var OkInstruction = (Instruction)targetInstruction.Operand; // ok str inst
                var kvpGetKeyMethod = typeof(KeyValuePair<string, string>).GetProperty("Key").GetGetMethod(); // to do: rm typeof ?
                var stringInequalMethod = targetModule.TypeSystem.String.Resolve().Methods.First(m => m.Name == "op_Inequality" && m.Parameters.Count == 2);

                // bypass dalamud.dll check
                var newInstructions = new[]
                {
                    ilProcessor.Create(OpCodes.Ldloca_S, (VariableDefinition)LdKVPairInstruction.Operand),
                    ilProcessor.Create(OpCodes.Call, targetModule.ImportReference(kvpGetKeyMethod)),
                    ilProcessor.Create(OpCodes.Ldstr, "Dalamud.dll"),
                    ilProcessor.Create(OpCodes.Call, targetModule.ImportReference(stringInequalMethod)),
                    ilProcessor.Create(OpCodes.Brfalse_S, OkInstruction),
                };

                // insert instructions
                foreach (var instruction in newInstructions)
                {
                    ilProcessor.InsertBefore(LdTextInstruction, instruction);
                }

                // write back to dll
                SaveAssembly();
            } else {
                Log.Information($"【鯊鯊補丁】【{ _assemblyName }】未找到完整性檢查指令，已跳過");
            }
        }

        /*  reference instructions
         *
            135	01C6	ldloc.s	V_11 (11)
            136	01C8	ldloca.s	V_7 (7)
            137	01CA	call	instance !1 valuetype [mscorlib]System.Collections.Generic.KeyValuePair`2<string, string>::get_Value()
            138	01CF	call	bool [mscorlib]System.String::op_Inequality(string, string)
            139	01D4	brfalse.s	149 (01EF) ldstr "[DUPDATE] Integrity check OK for {0} ({1})"
            140	01D6	ldstr	"[DUPDATE] Integrity check failed for {0} ({1} - {2})"
         */
    }
}