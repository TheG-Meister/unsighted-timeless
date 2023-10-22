using dev.gmeister.unsighted.timeless;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace Unsighted_Timeless;

[Harmony]
public class MeteorDust
{

    public static int ReplaceMeteorDustHours()
    {
        if (!Plugin.plugin.configEnable.Value) return 25;
        else return Plugin.plugin.configMeteorDustRestore.Value;
    }

    [HarmonyTargetMethod]
    public static IEnumerable<MethodBase> TargetMethods()
    {
        List<Type> types = new()
        {
            typeof(ArmadilloNPC),
            typeof(AvatarNPC),
            typeof(BlacksmithNPC),
            typeof(Blacksmith2NPC),
            typeof(ChipNPC),
            typeof(ElisaNPC),
            typeof(FishNPC),
            typeof(GeneralShopNPC),
            typeof(GrandmaNPC),
            typeof(HarpieNPC),
            typeof(OlgaNPC),
            typeof(ResearcherNPC),
            typeof(ResearcherNPCGarden),
            typeof(TobiasNPC),
            typeof(VanaNPC),
            typeof(WeaponShopNPC),
        };

        foreach (Type type in types)
        {
            yield return Plugin.FindEnumeratorMethod(type, "MeteorDustGivenCoroutine");
        }
        
        yield return Plugin.FindEnumeratorMethod(typeof(PauseMenuPopup), nameof(PauseMenuPopup.GiveDustToIris));
    }

    [HarmonyTranspiler]
    public static IEnumerable<CodeInstruction> TranspileMeteorDustGivenCoroutines(IEnumerable<CodeInstruction> instructions)
    {
        List<CodeInstruction> result = new(instructions);

        for (int i = 0; i < result.Count; i++)
        {
            if (result[i].opcode == OpCodes.Ldc_I4_S && result[i].OperandIs(25))
            {
                result[i] = new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(typeof(MeteorDust), nameof(MeteorDust.ReplaceMeteorDustHours)));
                break;
            }
        }

        return result;
    }

}
