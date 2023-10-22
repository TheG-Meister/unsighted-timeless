using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Unsighted_Timeless;

namespace dev.gmeister.unsighted.timeless;

[Harmony]
public class Elisa
{

    public static int ReplaceMeteorDustHours()
    {
        if (!Plugin.plugin.configEnable.Value) return 72;
        else return Plugin.plugin.configMeteorDustRestore.Value * 3;
    }

    [HarmonyTargetMethod]
    public static MethodBase TargetMethod()
    {
        return Plugin.FindEnumeratorMethod(typeof(ElisaNPC), nameof(ElisaNPC.BuyPermanentSyringe));
    }

    [HarmonyTranspiler]
    public static IEnumerable<CodeInstruction> TranspileBuyPermanentSyringe(IEnumerable<CodeInstruction> instructions)
    {
        List<CodeInstruction> result = new(instructions);

        for (int i = 0; i < result.Count; i++)
        {
            if (result[i].opcode == OpCodes.Ldc_I4_S && result[i].OperandIs(72))
            {
                result[i] = new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(typeof(Elisa), nameof(Elisa.ReplaceMeteorDustHours)));
                break;
            }
        }

        return result;
    }

}
