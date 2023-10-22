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
public class CutsceneDeleter
{

    public static LevelController.SceneEnterCutsceneType ReplaceCutscene(LevelController.SceneEnterCutsceneType cutscene)
    {
        if (!Plugin.plugin.configEnable.Value || !Plugin.plugin.configRemoveCutscenes.Value) return cutscene;
        else
        {
            switch (cutscene)
            {
                case LevelController.SceneEnterCutsceneType.IrisAlmostOutOfTime:
                case LevelController.SceneEnterCutsceneType.Morgana1:
                case LevelController.SceneEnterCutsceneType.Morgana2:
                case LevelController.SceneEnterCutsceneType.AlmaAlmostOutOfTime1:
                case LevelController.SceneEnterCutsceneType.AlmaAlmostOutOfTime2:
                    return LevelController.SceneEnterCutsceneType.None;
                default:
                    return cutscene;
            }
        }
    }

    [HarmonyTargetMethod]
    public static MethodBase TargetMethod()
    {
        return Plugin.FindEnumeratorMethod(typeof(LevelController), nameof(LevelController.SceneEnterCutscenes));
    }

    [HarmonyTranspiler]
    public static IEnumerable<CodeInstruction> DeleteCutscenes(IEnumerable<CodeInstruction> instructions)
    {
        List<CodeInstruction> result = new(instructions);

        for (int i = 0; i < result.Count; i++)
        {
            if (result[i].opcode == OpCodes.Ldarg_0 &&
                result[i+1].opcode == OpCodes.Ldfld &&
                result[i+2].opcode == OpCodes.Ldc_I4_S &&
                result[i+2].OperandIs(9) &&
                result[i+3].opcode == OpCodes.Bne_Un)
            {
                FieldInfo field = Plugin.FindEnumeratorType(typeof(LevelController), nameof(LevelController.SceneEnterCutscenes)).GetFields(BindingFlags.NonPublic | BindingFlags.Instance).Where(field => field.Name.Contains("nextCutscene")).First();

                List<CodeInstruction> codes = new()
                {
                    new CodeInstruction(OpCodes.Ldarg_0),
                    new CodeInstruction(OpCodes.Ldfld, field),
                    new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(typeof(CutsceneDeleter), nameof(CutsceneDeleter.ReplaceCutscene))),
                    new CodeInstruction(OpCodes.Stfld, field),
                    new CodeInstruction(OpCodes.Ldarg_0),
                };

                result.InsertRange(i + 1, codes);

                break;
            }
        }

        return result;
    }

}
