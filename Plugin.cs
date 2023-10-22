using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;

namespace dev.gmeister.unsighted.timeless;

[BepInPlugin(GUID, NAME, VERSION), Harmony]
internal class Plugin : BaseUnityPlugin
{

	public const string GUID = "dev.gmeister.unsighted.timeless";
	public const string NAME = "Unsighted Timeless";
	public const string VERSION = "1.0.0";

	public ConfigEntry<bool> configEnable;
	public ConfigEntry<Preset> configPreset;
	public ConfigEntry<float> configNPCDivisor;
	public ConfigEntry<int> configAlmaTime;
	public ConfigEntry<int> configUngiftable;
	public ConfigEntry<bool> configRemoveClock;
	public ConfigEntry<bool> configRemoveCutscenes;
	public ConfigEntry<int> configMeteorDustRestore;
	public ConfigEntry<bool> configStartInGearVillage;
	public ConfigEntry<bool> configInstantDeath;
	public ConfigEntry<int> configMDustQuantity;
	public ConfigEntry<bool> configHarvesting;
	public ConfigEntry<bool> configPassTimeInTower;

	public static Plugin plugin = null;
	public static Dictionary<NPCObject, int> npcHours = new();

	public enum Preset
	{
		Off,
		AlmostVanilla,
		Default,
		OneHourChallenge,
	}

	public Plugin()
	{
		Plugin.plugin = this;

		this.configEnable = Config.Bind(NAME, "01 Enable", true, "Enable any and all features of this plugin");
		this.configEnable.SettingChanged += (o, e) => this.SetTotalHours();

		this.configPreset = Config.Bind(NAME, "02 Preset", Preset.Default, "Apply a preset to all options");
		this.configPreset.SettingChanged += (o, e) =>
		{
			switch (this.configPreset.Value)
			{
				case Preset.Off:
					this.configEnable.Value = false;
					break;
				case Preset.AlmostVanilla:
					this.configEnable.Value = true;
					this.configNPCDivisor.Value = 1;
					this.configUngiftable.Value = 500;
					this.configRemoveClock.Value = false;
					this.configRemoveCutscenes.Value = false;
					this.configMeteorDustRestore.Value = 25;
					this.configStartInGearVillage.Value = false;
					this.configInstantDeath.Value = false;
					this.configMDustQuantity.Value = 10;
					this.configHarvesting.Value = true;
					this.configAlmaTime.Value = 325;
					this.configPassTimeInTower.Value = false;
					break;
				case Preset.OneHourChallenge:
					this.configEnable.Value = true;
					this.configNPCDivisor.Value = 99999;
					this.configUngiftable.Value = 5;
					this.configRemoveClock.Value = false;
					this.configRemoveCutscenes.Value = true;
					this.configMeteorDustRestore.Value = 1;
					this.configStartInGearVillage.Value = true;
					this.configInstantDeath.Value = true;
					this.configMDustQuantity.Value = 1;
					this.configHarvesting.Value = false;
					this.configAlmaTime.Value = 1;
					this.configPassTimeInTower.Value = true;
					break;
				default:
					this.configEnable.BoxedValue = this.configEnable.DefaultValue;
					this.configNPCDivisor.BoxedValue = this.configNPCDivisor.DefaultValue;
					this.configUngiftable.BoxedValue = this.configUngiftable.DefaultValue;
					this.configRemoveClock.BoxedValue = this.configRemoveClock.DefaultValue;
					this.configRemoveCutscenes.BoxedValue = this.configRemoveCutscenes.DefaultValue;
					this.configMeteorDustRestore.BoxedValue = this.configMeteorDustRestore.DefaultValue;
					this.configStartInGearVillage.BoxedValue = this.configStartInGearVillage.DefaultValue;
					this.configInstantDeath.BoxedValue = this.configInstantDeath.DefaultValue;
					this.configMDustQuantity.BoxedValue = this.configMDustQuantity.DefaultValue;
					this.configHarvesting.BoxedValue = this.configHarvesting.DefaultValue;
					this.configAlmaTime.BoxedValue = this.configAlmaTime.DefaultValue;
					this.configPassTimeInTower.BoxedValue = this.configPassTimeInTower.DefaultValue;
					break;
			}

			this.Config.Save();
			this.Config.Reload();
		};

		this.configAlmaTime = Config.Bind(NAME, "03 Alma Time Remaining", 16, "The remaining time to give Alma. Usually 325 hours");

		this.configNPCDivisor = Config.Bind(NAME, "04 NPC Time Divisor", 20f, "Divide the remaining time for all NPCs that accept Meteor Dust by this value");
		this.configNPCDivisor.SettingChanged += (o, e) => SetTotalHours();

		this.configUngiftable = Config.Bind(NAME, "05 Remaining Time for Ungiftable NPCs", 72, "The remaining time to give to all NPCs that do not accept Meteor Dust");
		this.configUngiftable.SettingChanged += (o, e) => SetTotalHours();

		this.configMeteorDustRestore = Config.Bind(NAME, "06 Meteor Dust Time", 25, "The amount of additional time given to Alma and NPCs when using Meteor Dust");

		this.configHarvesting = Config.Bind(NAME, "07 Harvesting", false, "Allow the player to consume an NPC's time through M");

		this.configMDustQuantity = Config.Bind(NAME, "08 M Dust Quantity", 10, "The amount of meteor dust given to the player upon defeating M");

		this.configStartInGearVillage = Config.Bind(NAME, "09 Start in Gear Village", true, "Start the player in the Gear Village with some basic equipment. Prevents collecting Meteor Dust in the prologue");

		this.configInstantDeath = Config.Bind(NAME, "10 Instant Death", true, "Kills the player as soon as their time runs out. Usually only happens on scene change");

		this.configPassTimeInTower = Config.Bind(NAME, "11 Pass Time in Crater Tower", true, "Still removes player and Iris time while in Crater Tower");

		this.configRemoveClock = Config.Bind(NAME, "12 Remove Clock", true, "Remove all ancient clock components, replacing them with a nice gift c:");

		this.configRemoveCutscenes = Config.Bind(NAME, "13 Remove Low-On-Time Cutscenes", true, "Remove 5 unskippable cutscenes for when Alma or Iris are low on time");

		Harmony harmony = new Harmony(GUID);
		harmony.PatchAll();
	}

	public void SetTotalHours()
	{
		if (npcHours.Count > 0)
		{
			if (Plugin.plugin.configEnable.Value)
			{
				foreach (KeyValuePair<NPCObject, int> npcHours in Plugin.npcHours)
				{
					switch (npcHours.Key.npcName)
					{
						case "GabiNPC":
						case "DrillNPC":
						case "JoanaNPC":
						case "GhoulWeaponNPC":
						case "CogShopNPC":
							npcHours.Key.totalHours = Plugin.plugin.configUngiftable.Value;
							break;
						default:
							int hours = Mathf.FloorToInt(npcHours.Value / Plugin.plugin.configNPCDivisor.Value);
							if (npcHours.Key.npcName == "IrisNPC" && hours < 2) hours = 2;
							else if (hours < 1) hours = 1;
							npcHours.Key.totalHours = hours;
							break;
					}
				}
			}
			else
			{
				foreach (KeyValuePair<NPCObject, int> npcHours in Plugin.npcHours)
					npcHours.Key.totalHours = npcHours.Value;
			}
		}
	}

	public static Type FindEnumeratorType(Type type, string name)
	{
		return AccessTools.FirstInner(type, t => t.Name.Contains(name));
	}

	public static MethodBase FindEnumeratorMethod(Type type, string name)
	{
		return AccessTools.FirstMethod(Plugin.FindEnumeratorType(type, name), method => method.Name.Contains("MoveNext"));
	}

	[HarmonyPatch(typeof(Helpers), nameof(Helpers.GetRemainingTimeInHoursFromAStartingNPCTime)), HarmonyPostfix]
	public static void AddHourOffset(ref int __result)
	{
		if (Plugin.plugin.configEnable.Value) __result += 9;
	}

	[HarmonyPatch(typeof(Lists), nameof(Lists.Start)), HarmonyPostfix]
	public static void ChangeOriginalNPCTime(Lists __instance)
	{
		foreach (NPCObject npc in __instance.npcDatabase.npcList)
		{
			Plugin.npcHours.Add(npc, npc.totalHours);
		}

		Plugin.plugin.SetTotalHours();
	}

	[HarmonyPatch(typeof(GlobalGameData), nameof(GlobalGameData.CreateDefaultDataSlot)), HarmonyPostfix]
	public static void ChangeAlmaTime(GlobalGameData __instance, int slotNumber)
	{
		if (Plugin.plugin.configEnable.Value)
		{
			int hours = Plugin.plugin.configAlmaTime.Value;
			if (hours < 1) hours = 1;
			__instance.currentData.playerDataSlots[slotNumber].currentGameplayTime.remainingPlayerHours = hours;
		}
	}

	[HarmonyPatch(typeof(Helpers), nameof(Helpers.GetChestReward)), HarmonyPostfix]
	public static void ReplaceClockItems(ref string __result)
	{
		if (Plugin.plugin.configEnable.Value && Plugin.plugin.configRemoveClock.Value)
		{
			if (__result.Contains("Clock")) __result = "Null";
		}
	}

	[HarmonyPatch(typeof(LevelController), nameof(LevelController.GoBackInTime)), HarmonyPrefix]
	public static void BeforeBackInTime(ref int __state)
	{
		__state = PseudoSingleton<Helpers>.instance.GetPlayerData().currentGameplayTime.remainingPlayerHours;
	}

	[HarmonyPatch(typeof(LevelController), nameof(LevelController.GoBackInTime)), HarmonyPostfix]
	public static void AfterBackInTime(int __state)
	{
        if (Plugin.plugin.configEnable.Value)
		{
			int hours = Plugin.plugin.configAlmaTime.Value;
			if (hours < 1) hours = 1;
			PseudoSingleton<Helpers>.instance.GetPlayerData().currentGameplayTime.remainingPlayerHours = __state < hours ? hours : __state;
		}
	}

	[HarmonyPatch(typeof(NewGamePopup), nameof(NewGamePopup.NewGameCoroutine)), HarmonyPrefix]
	public static void BeforeNewGame(NewGamePopup __instance, ref string ___newGameParameter)
	{
		if (Plugin.plugin.configEnable.Value && Plugin.plugin.configStartInGearVillage.Value)
		{
			___newGameParameter = "Village";
		}
	}

	[HarmonyPatch(typeof(Helpers), nameof(Helpers.SetPlayerEquipmentToVillage)), HarmonyPostfix]
	public static void AddMoreItemsToGearVillageStart(Helpers __instance)
	{
		if (Plugin.plugin.configEnable.Value && Plugin.plugin.configStartInGearVillage.Value)
		{
			PlayerData data = __instance.GetPlayerData();
			data.currentWeapons.Add(new EquipmentData("WarAxe"));
			data.playerChips.Add(new PlayerChipData("StrengthChip"));
			data.dataStrings.Add("RescuedIris");
			for (int i = 0; i < 2; i++) data.playersEquipData[i].chips.Add("StrengthChip");
		}
	}

	[HarmonyPatch(typeof(GameplayTime), nameof(GameplayTime.AddInGameSeconds)), HarmonyPostfix]
	public static void AfterTimeDecrease(Helpers __instance)
	{
		if (Plugin.plugin.configEnable.Value && Plugin.plugin.configInstantDeath.Value && __instance.GetPlayerData().currentGameplayTime.remainingPlayerHours <= 0 && !PlayerInfo.cutscene)
		{
			Plugin.plugin.StartCoroutine(KillAlma());
		}
	}

	public static IEnumerator KillAlma()
	{
		PseudoSingleton<CutsceneController>.instance.EnterCutsceneMode();
		PseudoSingleton<PlayersManager>.instance.StopAllPlayers(false);
		Time.timeScale = 1f;
		yield return gameTime.WaitForSeconds(0.5);
		PseudoSingleton<FmodMusicController>.instance.StopMusic(true);
		yield return Plugin.plugin.StartCoroutine(PseudoSingleton<DialogueSystem>.instance.OpenDialogueBox(Plugin.plugin.transform.position));
		yield return Plugin.plugin.StartCoroutine(PseudoSingleton<DialogueSystem>.instance.ShowDialogue("AlmaOutOfTime", "Alma", Plugin.plugin.gameObject, PortraitType.LeftPortrait, PseudoSingleton<Lists>.instance.almaPortrait, "Confused", true, false, false, "", 0));
		yield return Plugin.plugin.StartCoroutine(PseudoSingleton<DialogueSystem>.instance.CloseDialogueBox());
		if (PseudoSingleton<Helpers>.instance.GetPlayerData().dataStrings.Contains("Iris"))
		{
			yield return Plugin.plugin.StartCoroutine(PseudoSingleton<DialogueSystem>.instance.OpenDialogueBox(Plugin.plugin.transform.position));
			yield return Plugin.plugin.StartCoroutine(PseudoSingleton<DialogueSystem>.instance.ShowDialogue("AlmaOutOfTime", "Iris", Plugin.plugin.gameObject, PortraitType.RightPortrait, PseudoSingleton<Lists>.instance.irisPortrait, "Cry", true, false, false, "", 0));
			yield return Plugin.plugin.StartCoroutine(PseudoSingleton<DialogueSystem>.instance.CloseDialogueBox());
		}
		PseudoSingleton<PopupManager>.instance.MessagePopup(Plugin.plugin.gameObject, TranslationSystem.FindTerm("Terms", "AlmaOutOfTime1", false), true);
		yield return null;
		while (PseudoSingleton<PopupManager>.instance.currentPopups.Count > 0)
		{
			yield return null;
		}
		if (PseudoSingleton<Helpers>.instance.GetPlayerData().currentGameplayTime.remainingPlayerHours <= 0)
		{
			yield return PseudoSingleton<CutsceneController>.instance.FadeOut();
			yield return gameTime.WaitForSeconds(1.0);
			AudioController.PausePausableSounds();
			PseudoSingleton<FmodMusicController>.instance.StopAmbiance(true);
			yield return gameTime.WaitForSeconds(1.0);
			yield return Plugin.plugin.StartCoroutine(PseudoSingleton<DialogueSystem>.instance.OpenDialogueBox(Plugin.plugin.transform.position));
			yield return Plugin.plugin.StartCoroutine(PseudoSingleton<DialogueSystem>.instance.ShowDialogue("AlmaOutOfTime", "Ending", Plugin.plugin.gameObject, PortraitType.LeftPortrait, null, "Confused", true, false, false, "", 0));
			yield return Plugin.plugin.StartCoroutine(PseudoSingleton<DialogueSystem>.instance.CloseDialogueBox());
			yield return gameTime.WaitForSeconds(1.0);
			BetaTitleScreen.logoShown = false;
			PseudoSingleton<Helpers>.instance.GetPlayerData().dataStrings.Add("AlmaUNSIGHTED");
			PseudoSingleton<GlobalGameData>.instance.SaveData();
			PseudoSingleton<GlobalSceneManager>.instance.LoadScene("TitleScreen");
			yield break;
		}
		PseudoSingleton<FmodMusicController>.instance.PlayRoomMusic();
		PseudoSingleton<CameraSystem>.instance.scriptedMovement = false;
		PseudoSingleton<CutsceneController>.instance.ExitCutsceneMode();
		Time.timeScale = PseudoSingleton<gameTime>.instance.defaultTimeScale;
	}

	[HarmonyPatch(typeof(GrimReaperBoss), nameof(GrimReaperBoss.EndBoss)), HarmonyPostfix]
	public static void AddOrRemoveExtraMeteorDustAfterMFight()
	{
		if (Plugin.plugin.configEnable.Value)
		{
			if (Plugin.plugin.configMDustQuantity.Value < 10)
			{
				int dustToRemove = 10 - Plugin.plugin.configMDustQuantity.Value;
				if (dustToRemove < 0) dustToRemove = 0;
				PseudoSingleton<Helpers>.instance.ConsumeItem("MeteorDust", dustToRemove);
			}
			else if (Plugin.plugin.configMDustQuantity.Value > 10) PseudoSingleton<Helpers>.instance.AddPlayerItem("MeteorDust", Plugin.plugin.configMDustQuantity.Value - 10);
		}
	}

	[HarmonyPatch(typeof(GrimReaperNPC), nameof(GrimReaperNPC.Interaction)), HarmonyPrefix]
	public static bool ReplaceHarvestingMenu(GrimReaperNPC __instance, PlayerInfo targetPlayer)
	{
		if (Plugin.plugin.configEnable.Value && !Plugin.plugin.configHarvesting.Value)
		{
			targetPlayer.myCharacter.nearNPC = false;
			__instance.skippedCutscene = true;
			PseudoSingleton<Helpers>.instance.GetNPCData("GrimReaperNPC").met = true;
			Plugin.plugin.StartCoroutine(__instance.BattleBegin());
			__instance.interactionTrigger.transform.position += Vector3.up * 355f;
			return false;
		}
		else return true;
	}

	[HarmonyPatch(typeof(Helpers), nameof(Helpers.ReduceNPCHours)), HarmonyPostfix]
	public static void AfterReduceNPCHours(Helpers __instance)
	{
		PlayerData data = PseudoSingleton<Helpers>.instance.GetPlayerData();
		if (Plugin.plugin.configEnable.Value && Plugin.plugin.configPassTimeInTower.Value && PseudoSingleton<MapManager>.instance.playerRoom.areaName == "CraterTower" && !data.timeAssist)
		{
			if (data.currentGameplayTime.remainingPlayerHours > 0) data.currentGameplayTime.remainingPlayerHours--;

			NPCData iris = data.npcData.Where(npc => npc.npcName == "IrisNPC").First();
			if (iris.remainingHours > 1) iris.remainingHours--;
		}
	}


}
