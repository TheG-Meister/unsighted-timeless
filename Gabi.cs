using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace dev.gmeister.unsighted.timeless;

[Harmony]
public class Gabi
{

    public static int ReplaceWeaponVendorTime()
    {
        if (!Plugin.plugin.configEnable.Value) return 350;
        else
        {
            int hours = Plugin.plugin.configUngiftable.Value;
            if (hours < 1) hours = 1;
            return hours;
        }
    }

    [HarmonyTargetMethod]
    public static IEnumerable<MethodBase> TargetMethods()
    {
        yield return AccessTools.Method(typeof(GabiNPC), nameof(GabiNPC.OnEnable));
        yield return Plugin.FindEnumeratorMethod(typeof(GabiNPC), nameof(GabiNPC.HookshotInteraction));
    }

    [HarmonyTranspiler]
    public static IEnumerable<CodeInstruction> TranspileWeaponVendorTimeRefillers(IEnumerable<CodeInstruction> instructions, MethodBase originalMethod)
    {
        List<CodeInstruction> result = new(instructions);

        for (int i = 0; i < result.Count; i++)
        {
            if (result[i].opcode == OpCodes.Ldc_I4 && result[i].OperandIs(350))
            {
                result[i] = new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(typeof(Gabi), nameof(Gabi.ReplaceWeaponVendorTime)));
                break;
            }
        }

        return result;
    }

}
