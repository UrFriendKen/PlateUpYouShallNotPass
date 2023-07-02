using HarmonyLib;
using Kitchen;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;

namespace KitchenYouShallNotPass.Patches
{
    [HarmonyPatch]
    static class MoveMobileAppliances_Patch
    {
        static readonly List<OpCode> OPCODES_TO_MATCH = new List<OpCode>()
        {
            OpCodes.Ldarg_0,
            OpCodes.Ldfld,
            OpCodes.Ldloc_S,
            OpCodes.Ldc_I4_0,
            OpCodes.Call,
            OpCodes.Stloc_S
        };

        static readonly List<object> OPERANDS_TO_MATCH = new List<object>()
        {
            null,
            null,
            null,
            null,
            typeof(GenericSystemBase).GetMethod("GetOccupant", BindingFlags.NonPublic | BindingFlags.Instance, null, new Type[] { typeof(UnityEngine.Vector3), typeof(KitchenData.OccupancyLayer) }, null),
            null
        };

        static readonly List<OpCode> MODIFIED_OPCODES = new List<OpCode>()
        {
            //OpCodes.Ldloc_S,
            //OpCodes.Ldfld,
            //OpCodes.Ldc_I4,
            //OpCodes.Ceq,
            //OpCodes.Br_S
        };

        static readonly List<object> MODIFIED_OPERANDS = new List<object>()
        {
            null,
            null,
            null,
            null,
            typeof(PatchController).GetMethod("GetOccupantWithFallbackAndExceptions", BindingFlags.NonPublic | BindingFlags.Static),
            null
        };

        const int EXPECTED_MATCH_COUNT = 1;

        public static MethodBase TargetMethod()
        {
            Type type = AccessTools.FirstInner(typeof(MoveMobileAppliances), t => t.Name.Contains("c__DisplayClass_OnUpdate_LambdaJob0"));
            return AccessTools.FirstMethod(type, method => method.Name.Contains("OriginalLambdaBody"));
        }

        [HarmonyTranspiler]
        static IEnumerable<CodeInstruction> OriginalLambdaBody_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            Main.LogInfo("MoveMobileAppliances Transpiler");
            Main.LogInfo("Attempt change GetOccupant for target to GetOccupantWithFloorFallback");
            List<CodeInstruction> list = instructions.ToList();

            int matches = 0;
            int windowSize = OPCODES_TO_MATCH.Count;
            for (int i = 0; i < list.Count - windowSize; i++)
            {
                for (int j = 0; j < windowSize; j++)
                {
                    if (OPCODES_TO_MATCH[j] == null)
                    {
                        Main.LogError("OPCODES_TO_MATCH cannot contain null!");
                        return instructions;
                    }

                    string logLine = $"{j}:\t{OPCODES_TO_MATCH[j]}";

                    int index = i + j;
                    OpCode opCode = list[index].opcode;
                    if (j < OPCODES_TO_MATCH.Count && opCode != OPCODES_TO_MATCH[j])
                    {
                        if (j > 0)
                        {
                            logLine += $" != {opCode}";
                            Main.LogInfo($"{logLine}\tFAIL");
                        }
                        break;
                    }
                    logLine += $" == {opCode}";

                    if (j == 0)
                        Debug.Log("-------------------------");

                    if (j < OPERANDS_TO_MATCH.Count && OPERANDS_TO_MATCH[j] != null)
                    {
                        logLine += $"\t{OPERANDS_TO_MATCH[j]}";
                        object operand = list[index].operand;
                        if (OPERANDS_TO_MATCH[j] != operand)
                        {
                            logLine += $" != {operand}";
                            Main.LogInfo($"{logLine}\tFAIL");
                            break;
                        }
                        logLine += $" == {operand}";
                    }
                    Main.LogInfo($"{logLine}\tPASS");

                    if (j == OPCODES_TO_MATCH.Count - 1)
                    {
                        Main.LogInfo($"Found match {++matches}");
                        if (matches > EXPECTED_MATCH_COUNT)
                        {
                            Main.LogError("Number of matches found exceeded EXPECTED_MATCH_COUNT! Returning original IL.");
                            return instructions;
                        }

                        // Perform replacements
                        for (int k = 0; k < MODIFIED_OPCODES.Count; k++)
                        {
                            if (MODIFIED_OPCODES[k] != null)
                            {
                                int replacementIndex = i + k;
                                OpCode beforeChange = list[replacementIndex].opcode;
                                list[replacementIndex].opcode = MODIFIED_OPCODES[k];
                                Main.LogInfo($"Line {replacementIndex}: Replaced Opcode ({beforeChange} ==> {MODIFIED_OPCODES[k]})");
                            }
                        }

                        for (int k = 0; k < MODIFIED_OPERANDS.Count; k++)
                        {
                            if (MODIFIED_OPERANDS[k] != null)
                            {
                                int replacementIndex = i + k;
                                object beforeChange = list[replacementIndex].operand;
                                list[replacementIndex].operand = MODIFIED_OPERANDS[k];
                                Main.LogInfo($"Line {replacementIndex}: Replaced operand ({beforeChange ?? "null"} ==> {MODIFIED_OPERANDS[k] ?? "null"})");
                            }
                        }
                    }
                }
            }

            Main.LogWarning($"{(matches > 0 ? (matches == EXPECTED_MATCH_COUNT ? "Transpiler Patch succeeded with no errors" : $"Completed with {matches}/{EXPECTED_MATCH_COUNT} found.") : "Failed to find match")}");
            return list.AsEnumerable();
        }
    }
}
