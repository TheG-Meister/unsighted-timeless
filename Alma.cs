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
public class Alma
{

    public static int ReplaceMeteorDustHours()
    {
        if (!Plugin.plugin.configEnable.Value) return 25;
        else return Plugin.plugin.configMeteorDustRestore.Value;
    }

    public static int ReplaceAnimaChipHours()
    {
        if (!Plugin.plugin.configEnable.Value) return 23;
        else return Plugin.plugin.configMeteorDustRestore.Value;
    }

    [HarmonyTargetMethod]
    public static IEnumerable<MethodBase> TargetMethods()
    {
        yield return AccessTools.Method(typeof(PauseMenuPopup), nameof(PauseMenuPopup.MeteorDustConfirm));
        yield return AccessTools.Method(typeof(MaterialsPopup), nameof(MaterialsPopup.MeteorDustConfirm));
    }

    [HarmonyTranspiler]
    public static IEnumerable<CodeInstruction> TranspileMeteorDustComfirm(IEnumerable<CodeInstruction> instructions)
    {
        List<CodeInstruction> result = new(instructions);

        for (int i = 0; i < result.Count; i++)
        {
            if (result[i].opcode == OpCodes.Ldc_I4_S)
            {
                if (result[i].OperandIs(25))
                {
                    result[i] = new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(typeof(Alma), nameof(Alma.ReplaceMeteorDustHours)));
                }
                else if (result[i].OperandIs(23))
                {
                    result[i] = new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(typeof(Alma), nameof(Alma.ReplaceAnimaChipHours)));
                    break;
                }
            }
        }

        return result;
    }
}
