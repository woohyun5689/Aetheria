using System.Collections.Generic;
using UnityEngine;

public sealed partial class AetheriaGame
{
    private static AetheriaGameDatabase configuredDatabase;
    private AetheriaGameDatabase gameDatabase;

    public static void ConfigureDatabase(AetheriaGameDatabase database)
    {
        configuredDatabase = database;
    }

    private void LoadGameDatabase()
    {
        gameDatabase = configuredDatabase != null
            ? configuredDatabase
            : Resources.Load<AetheriaGameDatabase>("AetheriaGameDatabase");
        ClearGameDataCaches();
    }

    private void ClearGameDataCaches()
    {
        cachedDungeons = null;
        cachedHeroClasses = null;
        skillCacheByClass.Clear();
        rarityCache.Clear();
        MarkPlayerDataDirty();
    }

    private List<HeroClass> HeroClassesFromDatabase()
    {
        var classes = new List<HeroClass>();
        if (gameDatabase == null || gameDatabase.heroClasses == null)
        {
            return classes;
        }

        foreach (var data in gameDatabase.heroClasses)
        {
            if (data == null || string.IsNullOrEmpty(data.heroName))
            {
                continue;
            }

            var heroName = NormalizeHeroClassName(data.heroName);
            var portraitResourceName = data.portraitResourceName;
            if (string.IsNullOrEmpty(portraitResourceName) && data.portrait != null)
            {
                portraitResourceName = data.portrait.name;
            }

            var basicAttackUsesMagic = data.basicAttackUsesMagic || data.magicAttack > data.physicalAttack;
            var skills = SkillsFromData(data.skills);
            classes.Add(new HeroClass(
                heroName,
                HeroDescriptionForClass(heroName, data.description),
                string.IsNullOrEmpty(portraitResourceName) ? heroName : portraitResourceName,
                Mathf.Max(1, data.maxHp),
                Mathf.Max(0, data.maxMp),
                Mathf.Max(0, data.physicalAttack),
                Mathf.Max(0, data.magicAttack),
                Mathf.Max(0, data.defense),
                Mathf.Clamp(data.critRate, 0f, 0.95f),
                basicAttackUsesMagic,
                skills));
        }

        return classes;
    }

    private List<SkillState> SkillsForClassFromDatabase(string heroClass)
    {
        var mergedSkills = new List<SkillState>();
        var normalizedHeroClass = NormalizeHeroClassName(heroClass);
        if (gameDatabase == null || gameDatabase.heroClasses == null)
        {
            return mergedSkills;
        }

        foreach (var data in gameDatabase.heroClasses)
        {
            if (data == null || NormalizeHeroClassName(data.heroName) != normalizedHeroClass)
            {
                continue;
            }

            mergedSkills = MergeSkills(mergedSkills, SkillsFromData(data.skills));
        }

        return mergedSkills;
    }

    private List<DungeonData> DungeonsFromDatabase()
    {
        var dungeons = new List<DungeonData>();
        if (gameDatabase == null || gameDatabase.dungeons == null)
        {
            return dungeons;
        }

        foreach (var data in gameDatabase.dungeons)
        {
            if (data == null)
            {
                continue;
            }

            var dungeonNumber = Mathf.Max(1, data.dungeonNumber);
            var monsters = new List<EnemyTemplate>();
            if (data.monsters != null)
            {
                foreach (var monster in data.monsters)
                {
                    var template = EnemyFromData(monster);
                    if (template != null)
                    {
                        monsters.Add(template);
                    }
                }
            }

            var boss = EnemyFromData(data.boss);
            var dungeonName = string.IsNullOrEmpty(data.dungeonName) ? "던전 " + dungeonNumber : data.dungeonName;

            dungeons.Add(new DungeonData(
                dungeonNumber,
                dungeonName,
                data.recommendedLevel,
                data.floors,
                string.IsNullOrEmpty(data.description) ? dungeonName + "입니다." : data.description,
                monsters,
                boss));
        }

        return dungeons;
    }

    private EnemyTemplate EnemyFromData(AetheriaEnemyData data)
    {
        if (data == null || string.IsNullOrEmpty(data.enemyName))
        {
            return null;
        }

        return new EnemyTemplate(
            data.enemyName,
            string.IsNullOrEmpty(data.description) ? data.enemyName + "입니다." : data.description,
            Mathf.Max(1, data.hp),
            Mathf.Max(0, data.maxMp),
            Mathf.Max(0, data.attack),
            Mathf.Max(0, data.magic),
            Mathf.Max(0, data.defense),
            Mathf.Max(0, data.speed),
            Mathf.Clamp(data.critRate, 0f, 0.95f),
            Mathf.Max(0, data.goldReward),
            Mathf.Max(0, data.xpReward),
            SkillsFromData(data.skills));
    }

    private List<SkillState> SkillsFromData(IEnumerable<AetheriaSkillData> source)
    {
        var skills = new List<SkillState>();
        if (source == null)
        {
            return skills;
        }

        foreach (var skill in source)
        {
            var converted = SkillFromData(skill);
            if (converted == null)
            {
                continue;
            }

            var index = skills.FindIndex(entry => entry != null && entry.name == converted.name);
            if (index >= 0)
            {
                skills[index] = converted;
            }
            else
            {
                skills.Add(converted);
            }
        }

        return skills;
    }

    private bool TryGetRarityData(int rarity, out AetheriaRarityData rarityData)
    {
        rarityData = null;
        if (rarityCache.TryGetValue(rarity, out rarityData))
        {
            return rarityData != null;
        }

        if (gameDatabase == null || gameDatabase.rarities == null)
        {
            return false;
        }

        foreach (var data in gameDatabase.rarities)
        {
            if (data != null && data.rank == rarity)
            {
                rarityData = data;
                rarityCache[rarity] = rarityData;
                return true;
            }
        }

        if (rarity >= 0 && rarity < gameDatabase.rarities.Count)
        {
            rarityData = gameDatabase.rarities[rarity];
        }

        rarityCache[rarity] = rarityData;
        return rarityData != null;
    }

    private SkillState SkillFromData(AetheriaSkillData data)
    {
        if (data == null || string.IsNullOrEmpty(data.skillName))
        {
            return null;
        }

        var multiplier = data.noDamage || data.grantsShield || data.healsSelf
            ? Mathf.Max(0f, data.multiplier)
            : Mathf.Max(0.1f, data.multiplier);
        var selfStatusType = string.IsNullOrEmpty(data.selfStatusType) ? "" : data.selfStatusType;
        return new SkillState(data.skillName, Mathf.Max(0, data.mpCost), multiplier, data.usesMagic)
        {
            stunChance = Mathf.Clamp01(data.stunChance),
            burnChance = Mathf.Clamp01(data.burnChance),
            poisonChance = Mathf.Clamp01(data.poisonChance),
            bleedChance = Mathf.Clamp01(data.bleedChance),
            shockChance = Mathf.Clamp01(data.shockChance),
            freezeChance = Mathf.Clamp01(data.freezeChance),
            blindChance = Mathf.Clamp01(data.blindChance),
            weakenChance = Mathf.Clamp01(data.weakenChance),
            vulnerableChance = Mathf.Clamp01(data.vulnerableChance),
            silenceChance = Mathf.Clamp01(data.silenceChance),
            manaBurnChance = Mathf.Clamp01(data.manaBurnChance),
            selfStatusType = selfStatusType,
            selfStatusDuration = string.IsNullOrEmpty(selfStatusType) ? 0 : Mathf.Clamp(data.selfStatusDuration, 1, 10),
            selfStatusValue = Mathf.Clamp(data.selfStatusValue, -10f, 10f),
            critBonus = Mathf.Clamp(data.critBonus, 0f, 0.95f),
            lifeStealRatio = Mathf.Clamp(data.lifeStealRatio, 0f, 0.8f),
            forceCrit = data.forceCrit,
            noDamage = data.noDamage,
            grantsShield = data.grantsShield,
            healsSelf = data.healsSelf
        };
    }
}
