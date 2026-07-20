using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public sealed partial class AetheriaGame
{
    private bool HasSave(int slot)
    {
        return AetheriaSaveFiles.HasSave(slot) || PlayerPrefs.HasKey(SaveKey(slot));
    }

    private int FirstEmptySaveSlot()
    {
        for (var slot = 0; slot < SaveSlotCount; slot++)
        {
            if (!HasSave(slot))
            {
                return slot;
            }
        }

        return Mathf.Clamp(activeSlot, 0, SaveSlotCount - 1);
    }

    private bool HasEmptySaveSlot()
    {
        for (var slot = 0; slot < SaveSlotCount; slot++)
        {
            if (!HasSave(slot))
            {
                return true;
            }
        }

        return false;
    }

    private string SavePreview(int slot)
    {
        if (!TryReadSaveState(slot, out var state))
        {
            return "슬롯 " + (slot + 1) + "\n저장 데이터 손상\n초기화 후 사용";
        }

        var dungeonCount = Dungeons().Count;
        var stageLabel = dungeonCount > 0 ? Mathf.Clamp(state.stage, 1, dungeonCount) + "/" + dungeonCount : state.stage.ToString();
        return "슬롯 " + (slot + 1) + "\n레벨 " + Mathf.Max(1, state.level) + " " + NormalizeHeroClassName(state.heroClass) + "\n해금 던전 " + stageLabel + "  " + Mathf.Max(0, state.gold) + "G";
    }

    private bool LoadGame(int slot)
    {
        slot = Mathf.Clamp(slot, 0, SaveSlotCount - 1);
        activeSlot = slot;
        if (!TryReadSaveState(slot, out player))
        {
            player = null;
            MarkPlayerDataDirty();
            return false;
        }

        MarkPlayerDataDirty();
        MigrateSaveData(player);
        EnsurePlayerData(true);
        player.hp = Mathf.Clamp(player.hp, 1, MaxHp());
        player.mp = Mathf.Clamp(player.mp, 0, MaxMp());
        SaveGame();
        return true;
    }

    private bool TryReadSaveState(int slot, out PlayerState state)
    {
        state = null;
        if (TryDeserializeSaveState(AetheriaSaveFiles.Load(slot), out state))
        {
            return true;
        }

        return TryDeserializeSaveState(PlayerPrefs.GetString(SaveKey(slot), ""), out state);
    }

    private bool TryDeserializeSaveState(string data, out PlayerState state)
    {
        state = null;
        if (string.IsNullOrWhiteSpace(data))
        {
            return false;
        }

        try
        {
            state = JsonUtility.FromJson<PlayerState>(data);
        }
        catch (Exception)
        {
            state = null;
        }

        return state != null && !string.IsNullOrEmpty(state.heroClass);
    }

    private void MigrateSaveData(PlayerState state)
    {
        if (state == null)
        {
            return;
        }

        if (state.saveVersion < 1)
        {
            state.saveVersion = 1;
        }

        if (state.saveVersion < 2)
        {
            state.hasSeenGuide = false;
            state.saveVersion = 2;
        }

        state.saveVersion = CurrentSaveVersion;
    }

    private void MarkPlayerDataDirty()
    {
        playerDataValidated = false;
    }

    private void EnsurePlayerData()
    {
        EnsurePlayerData(false);
    }

    private void EnsurePlayerData(bool forceFullValidation)
    {
        if (player == null)
        {
            return;
        }

        if (!forceFullValidation && playerDataValidated)
        {
            ClampPlayerVitals();
            return;
        }

        MigrateSaveData(player);
        player.heroClass = NormalizeHeroClassName(player.heroClass);
        HydratePlayerClassDefaults();
        player.level = Mathf.Max(1, player.level);
        player.gold = Mathf.Max(0, player.gold);
        player.xp = Mathf.Max(0, player.xp);
        var dungeonCount = Dungeons().Count;
        player.stage = Mathf.Clamp(player.stage <= 0 ? 1 : player.stage, 1, Mathf.Max(1, dungeonCount + 1));

        if (player.inventory == null)
        {
            player.inventory = new List<ItemState>();
        }

        if (player.skillLevels == null)
        {
            player.skillLevels = new List<SkillLevelState>();
        }

        if (player.dungeonProgress == null)
        {
            player.dungeonProgress = new List<DungeonProgressState>();
        }

        if (player.generalLogs == null)
        {
            player.generalLogs = new List<string>();
        }

        player.inventory.RemoveAll(item => item == null);
        player.skillLevels.RemoveAll(entry => entry == null || string.IsNullOrEmpty(entry.skillName));
        player.dungeonProgress.RemoveAll(entry => entry == null || entry.dungeonNumber <= 0);
        player.generalLogs.RemoveAll(string.IsNullOrEmpty);
        TrimGeneralLogs();
        DeduplicateSkillLevels();
        DeduplicateDungeonProgress();

        foreach (var skill in SkillsForClass(player.heroClass))
        {
            if (!player.skillLevels.Exists(entry => entry.skillName == skill.name))
            {
                player.skillLevels.Add(new SkillLevelState { skillName = skill.name, level = 1 });
            }
        }

        NormalizeItemStats(player.weapon);
        NormalizeItemStats(player.armor);
        NormalizeItemStats(player.charm);
        NormalizeItemStats(player.charm2);
        NormalizeItemStats(player.charm3);
        NormalizeItemStats(player.charm4);
        foreach (var item in player.inventory)
        {
            NormalizeItemStats(item);
        }

        ClampPlayerVitals();
        playerDataValidated = true;
    }

    private void ClampPlayerVitals()
    {
        if (player == null)
        {
            return;
        }

        player.hp = player.hp <= 0 ? MaxHp() : Mathf.Clamp(player.hp, 1, MaxHp());
        player.mp = Mathf.Clamp(player.mp, 0, MaxMp());
    }

    private string NormalizeHeroClassName(string heroClass)
    {
        heroClass = string.IsNullOrWhiteSpace(heroClass) ? "" : heroClass.Trim();
        if (heroClass == "Paladin") return "성기사";
        if (heroClass == "Elementalist") return "원소술사";
        if (heroClass == "Rogue" || heroClass == "Ranger" || heroClass == "ShadowAssassin") return "그림자 자객";
        if (heroClass == "Cleric" || heroClass == "Priest") return "빛의 사제";
        if (heroClass == "Bomber") return "폭렬술사";
        if (heroClass == "SpiritCaller") return "정령술사";
        if (heroClass == "WindArcher") return "바람 궁수";
        if (heroClass == "Monk") return "무투가";
        return string.IsNullOrEmpty(heroClass) ? "성기사" : heroClass;
    }

    private void HydratePlayerClassDefaults()
    {
        var heroClass = HeroClasses().Find(entry => entry.name == player.heroClass);
        if (heroClass == null)
        {
            return;
        }

        if (string.IsNullOrEmpty(player.heroName))
        {
            player.heroName = "에테리안";
        }

        if (string.IsNullOrEmpty(player.portraitName) || IsLegacyPortraitName(player.portraitName))
        {
            player.portraitName = heroClass.portraitName;
        }

        if (player.baseHp <= 0) player.baseHp = heroClass.hp;
        if (player.baseMp <= 0) player.baseMp = heroClass.mp;
        if (player.baseAttack <= 0) player.baseAttack = heroClass.attack;
        if (player.baseMagic <= 0) player.baseMagic = heroClass.magic;
        if (player.baseDefense <= 0) player.baseDefense = heroClass.defense;
        if (player.baseCrit <= 0f) player.baseCrit = heroClass.crit;
    }

    private bool IsLegacyPortraitName(string portraitName)
    {
        if (string.IsNullOrEmpty(portraitName))
        {
            return true;
        }

        return portraitName == "female-paladin-standalone"
            || portraitName == "female-paladin-wide-shield"
            || portraitName == "female-paladin-natural-hair"
            || portraitName == "novice-adventurer-elemental-aura-01"
            || portraitName == "novice-adventurer-elemental-spirits-01"
            || portraitName == "novice-adventurer-elemental-spirits-02"
            || portraitName == "novice-adventurer-no-shoulder-bags-01"
            || portraitName == "novice-adventurer-shoulder-satchel-01"
            || portraitName == "novice-adventurer-no-arm-bracer-01";
    }

    private void NormalizeItemStats(ItemState item)
    {
        if (item == null)
        {
            return;
        }

        NormalizeItemBasics(item);
        if (!ItemHasAnyStats(item))
        {
            var heroClass = player != null ? player.heroClass : "성기사";
            var level = player != null ? player.level : 1;
            var stage = player != null ? player.stage : 1;
            var dungeonCount = Dungeons().Count;
            if (dungeonCount > 0)
            {
                stage = Mathf.Clamp(stage, 1, dungeonCount);
            }

            ApplyGeneratedGearStats(item, heroClass, level, stage);
        }

        ClampItemRateStats(item);
        item.power = GearPowerFromStats(item);
    }

    private bool ItemHasAnyStats(ItemState item)
    {
        return item.maxHp != 0
            || item.maxMp != 0
            || item.attack != 0
            || item.magic != 0
            || item.defense != 0
            || item.speed != 0
            || item.critRate != 0f
            || item.critDamage != 0f
            || item.evasion != 0f
            || item.damageReduction != 0f
            || item.lifeSteal != 0f
            || item.manaRegen != 0f
            || item.statusPower != 0f
            || item.itemFind != 0f;
    }

    private void NormalizeItemBasics(ItemState item)
    {
        if (string.IsNullOrEmpty(item.id))
        {
            item.id = Guid.NewGuid().ToString();
        }

        if (item.type == "weapon")
        {
            item.type = "Weapon";
        }
        else if (item.type == "armor")
        {
            item.type = "Armor";
        }
        else if (item.type == "accessory")
        {
            item.type = "Charm";
        }
        else if (item.type != "Weapon" && item.type != "Armor" && item.type != "Charm")
        {
            item.type = "Charm";
        }

        item.rarity = Mathf.Clamp(item.rarity, 0, NamedRarity);
        item.power = Mathf.Max(1, item.power);
        item.level = Mathf.Clamp(item.level, 0, MaxGearEnhancementLevel);
        item.maxHp = Mathf.Max(0, item.maxHp);
        item.maxMp = Mathf.Max(0, item.maxMp);
        item.attack = Mathf.Max(0, item.attack);
        item.magic = Mathf.Max(0, item.magic);
        item.defense = Mathf.Max(0, item.defense);
        item.speed = Mathf.Max(0, item.speed);
        ClampItemRateStats(item);

        if (string.IsNullOrEmpty(item.name))
        {
            item.name = RarityLabel(item.rarity) + " " + TypeLabel(item.type);
        }
        else
        {
            item.name = item.name.Trim();
        }
    }

    private void DeduplicateSkillLevels()
    {
        var bestLevels = new Dictionary<string, int>();
        for (var i = player.skillLevels.Count - 1; i >= 0; i--)
        {
            var entry = player.skillLevels[i];
            entry.skillName = entry.skillName.Trim();
            var level = Mathf.Clamp(entry.level, 1, MaxSkillLevel);
            if (bestLevels.TryGetValue(entry.skillName, out var bestLevel))
            {
                bestLevels[entry.skillName] = Mathf.Max(bestLevel, level);
            }
            else
            {
                bestLevels.Add(entry.skillName, level);
            }
        }

        player.skillLevels.Clear();
        foreach (var pair in bestLevels)
        {
            player.skillLevels.Add(new SkillLevelState { skillName = pair.Key, level = pair.Value });
        }
    }

    private void DeduplicateDungeonProgress()
    {
        var mergedProgress = new Dictionary<int, DungeonProgressState>();
        var dungeonCount = Mathf.Max(1, Dungeons().Count);
        for (var i = player.dungeonProgress.Count - 1; i >= 0; i--)
        {
            var entry = player.dungeonProgress[i];
            if (entry.dungeonNumber > dungeonCount)
            {
                continue;
            }

            if (mergedProgress.TryGetValue(entry.dungeonNumber, out var existing))
            {
                existing.floorCleared = existing.floorCleared || entry.floorCleared;
                existing.bossCleared = existing.bossCleared || entry.bossCleared;
            }
            else
            {
                mergedProgress.Add(entry.dungeonNumber, new DungeonProgressState
                {
                    dungeonNumber = entry.dungeonNumber,
                    floorCleared = entry.floorCleared,
                    bossCleared = entry.bossCleared
                });
            }
        }

        player.dungeonProgress.Clear();
        foreach (var pair in mergedProgress)
        {
            player.dungeonProgress.Add(pair.Value);
        }
    }

    private void TrimGeneralLogs()
    {
        if (player == null || player.generalLogs == null)
        {
            return;
        }

        while (player.generalLogs.Count > MaxGeneralLogLines)
        {
            player.generalLogs.RemoveAt(0);
        }
    }

    private void ClampItemRateStats(ItemState item)
    {
        if (item == null)
        {
            return;
        }

        item.critRate = Mathf.Clamp(item.critRate, 0f, 0.95f);
        item.critDamage = Mathf.Max(0f, item.critDamage);
        item.evasion = Mathf.Clamp(item.evasion, 0f, 0.8f);
        item.damageReduction = Mathf.Clamp(item.damageReduction, 0f, 0.5f);
        item.lifeSteal = Mathf.Clamp(item.lifeSteal, 0f, 0.35f);
        item.manaRegen = Mathf.Clamp(item.manaRegen, 0f, ManaRegenCap);
        item.statusPower = Mathf.Clamp(item.statusPower, 0f, 0.75f);
        item.itemFind = Mathf.Clamp(item.itemFind, 0f, 0.5f);
    }

    private void SaveGame()
    {
        if (player == null)
        {
            return;
        }

        EnsurePlayerData(true);
        activeSlot = Mathf.Clamp(activeSlot, 0, SaveSlotCount - 1);
        player.saveVersion = CurrentSaveVersion;
        var json = JsonUtility.ToJson(player, true);
        AetheriaSaveFiles.Save(activeSlot, json);
        PlayerPrefs.SetString(SaveKey(activeSlot), json);
        PlayerPrefs.Save();
    }

    private string SaveKey(int slot)
    {
        return "AetheriaUnitySave_" + Mathf.Clamp(slot, 0, SaveSlotCount - 1);
    }

    private void DeleteSave(int slot)
    {
        slot = Mathf.Clamp(slot, 0, SaveSlotCount - 1);
        var deletingActiveSlot = slot == activeSlot;
        AetheriaSaveFiles.Delete(slot);
        PlayerPrefs.DeleteKey(SaveKey(slot));
        PlayerPrefs.Save();

        if (!deletingActiveSlot)
        {
            return;
        }

        player = null;
        MarkPlayerDataDirty();
        selectedHeroClassName = "";
        selectedInventoryIndex = -1;
        ResetCombatState(true);
    }

    private static class AetheriaSaveFiles
    {
        private const string SaveFolderName = "AetheriaSaves";

        public static bool HasSave(int slot)
        {
            try
            {
                return File.Exists(SavePath(slot));
            }
            catch (Exception exception)
            {
                Debug.LogWarning("Failed to check Aetheria save file: " + exception.Message);
                return false;
            }
        }

        public static string Load(int slot)
        {
            try
            {
                var path = SavePath(slot);
                return File.Exists(path) ? File.ReadAllText(path) : "";
            }
            catch (Exception exception)
            {
                Debug.LogWarning("Failed to load Aetheria save file: " + exception.Message);
                return "";
            }
        }

        public static void Save(int slot, string json)
        {
            try
            {
                var folder = SaveFolder();
                Directory.CreateDirectory(folder);
                var path = SavePath(slot);
                var tempPath = path + ".tmp";
                File.WriteAllText(tempPath, json);
                File.Copy(tempPath, path, true);
                File.Delete(tempPath);
            }
            catch (Exception exception)
            {
                Debug.LogWarning("Failed to write Aetheria save file: " + exception.Message);
            }
        }

        public static void Delete(int slot)
        {
            try
            {
                var path = SavePath(slot);
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
            catch (Exception exception)
            {
                Debug.LogWarning("Failed to delete Aetheria save file: " + exception.Message);
            }
        }

        private static string SaveFolder()
        {
            return Path.Combine(Application.persistentDataPath, SaveFolderName);
        }

        private static string SavePath(int slot)
        {
            return Path.Combine(SaveFolder(), "slot_" + Mathf.Clamp(slot, 0, SaveSlotCount - 1) + ".json");
        }
    }
}
