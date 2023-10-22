using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace dev.gmeister.unsighted.timeless;

[Harmony]
internal class Clara
{

    public static int GetClaraTime()
    {
        if (Plugin.plugin.configEnable.Value) return 1;
        else return 250;
    }

    [HarmonyTargetMethod]
    public static IEnumerable<MethodBase> TargetMethods()
    {
        yield return AccessTools.Method(typeof(ResearcherNPC), nameof(ResearcherNPC.RefillTime));
        yield return Plugin.FindEnumeratorMethod(typeof(ResearcherAfterCrashCutscene), nameof(ResearcherAfterCrashCutscene.ResearcherCutscene));
    }

    [HarmonyTranspiler]
    public static IEnumerable<CodeInstruction> TranspileClaraTimeRefillers(IEnumerable<CodeInstruction> instructions, MethodBase originalMethod)
    {
        List<CodeInstruction> result = new(instructions);

        for (int i = 0; i < result.Count; i++)
        {
            if (result[i].opcode == OpCodes.Ldc_I4 && result[i].OperandIs(250))
            {
                result[i] = new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(typeof(Clara), nameof(Clara.GetClaraTime)));
            }
        }

        return result;
    }

}
