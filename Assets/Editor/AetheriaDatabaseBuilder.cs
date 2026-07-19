#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;

public static class AetheriaDatabaseBuilder
{
    private static readonly BindingFlags InstanceFieldFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    private static readonly BindingFlags StaticMethodFlags = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

    private const string ResourcesFolder = "Assets/Resources";
    private const string DataFolder = "Assets/Data";
    private const string HeroFolder = DataFolder + "/Heroes";
    private const string SkillFolder = DataFolder + "/Skills";
    private const string EnemySkillFolder = SkillFolder + "/Enemies";
    private const string RarityFolder = DataFolder + "/Rarities";
    private const string DungeonFolder = DataFolder + "/Dungeons";
    private const string EnemyFolder = DataFolder + "/Enemies";
    private const string DatabasePath = ResourcesFolder + "/AetheriaGameDatabase.asset";
    private const string BuildRequestPath = "ProjectSettings/AetheriaBuildDefaultGameDatabase.request";

    [InitializeOnLoadMethod]
    private static void BuildDefaultGameDatabaseIfRequested()
    {
        var requestPath = ProjectFilePath(BuildRequestPath);
        if (!File.Exists(requestPath))
        {
            return;
        }

        EditorApplication.delayCall += () =>
        {
            if (!File.Exists(requestPath))
            {
                return;
            }

            File.Delete(requestPath);
            BuildDefaultGameDatabase();
        };
    }

    [MenuItem("Aetheria/Build Default Game Database")]
    public static void BuildDefaultGameDatabase()
    {
        EnsureFolder(ResourcesFolder);
        EnsureFolder(DataFolder);
        EnsureFolder(HeroFolder);
        EnsureFolder(SkillFolder);
        EnsureFolder(EnemySkillFolder);
        EnsureFolder(RarityFolder);
        EnsureFolder(DungeonFolder);
        EnsureFolder(EnemyFolder);

        AetheriaGameDatabase database = null;
        AssetDatabase.StartAssetEditing();
        try
        {
            database = LoadOrCreateAsset<AetheriaGameDatabase>(DatabasePath);

            database.heroClasses = BuildDefaultHeroClasses();
            database.dungeons = BuildDefaultDungeons();
            database.rarities = BuildDefaultRarities();

            EditorUtility.SetDirty(database);
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        ValidateGeneratedScriptReferences();
        Debug.Log("Aetheria default game database updated.");
    }

    private static List<AetheriaRarityData> BuildDefaultRarities()
    {
        var rarities = new List<AetheriaRarityData>();
        foreach (var definition in RarityDefinitions())
        {
            var path = RarityFolder + "/Rarity_" + definition.rank.ToString("00") + "_" + definition.assetName + ".asset";
            var data = LoadOrCreateAsset<AetheriaRarityData>(path);

            data.rarityName = definition.displayName;
            data.rank = definition.rank;
            data.color = definition.color;
            data.statMultiplier = definition.statMultiplier;
            data.dropWeight = definition.dropWeight;
            EditorUtility.SetDirty(data);
            rarities.Add(data);
        }

        return rarities;
    }

    private static List<AetheriaHeroClassData> BuildDefaultHeroClasses()
    {
        var heroes = new List<AetheriaHeroClassData>();
        foreach (var definition in HeroDefinitions())
        {
            var path = HeroFolder + "/Hero_" + definition.assetName + ".asset";
            var data = LoadOrCreateAsset<AetheriaHeroClassData>(path);

            data.heroName = definition.displayName;
            data.description = definition.description;
            data.portraitResourceName = definition.portraitResourceName;
            data.basicAttackUsesMagic = definition.basicAttackUsesMagic;
            data.maxHp = definition.maxHp;
            data.maxMp = definition.maxMp;
            data.physicalAttack = definition.physicalAttack;
            data.magicAttack = definition.magicAttack;
            data.defense = definition.defense;
            data.critRate = definition.critRate;
            data.skills = BuildDefaultSkills(definition.assetName, definition.skills);
            EditorUtility.SetDirty(data);
            heroes.Add(data);
        }

        return heroes;
    }

    private static List<AetheriaSkillData> BuildDefaultSkills(string heroAssetName, IEnumerable<SkillDefinition> definitions)
    {
        var skills = new List<AetheriaSkillData>();
        foreach (var definition in definitions)
        {
            var path = SkillFolder + "/Skill_" + heroAssetName + "_" + definition.assetName + ".asset";
            var data = LoadOrCreateAsset<AetheriaSkillData>(path);

            data.skillName = definition.displayName;
            data.description = "";
            data.mpCost = definition.mpCost;
            data.multiplier = definition.multiplier;
            data.usesMagic = definition.usesMagic;
            data.noDamage = definition.noDamage;
            data.grantsShield = definition.grantsShield;
            data.healsSelf = definition.healsSelf;
            data.stunChance = definition.stunChance;
            data.burnChance = definition.burnChance;
            data.poisonChance = definition.poisonChance;
            data.bleedChance = definition.bleedChance;
            data.shockChance = definition.shockChance;
            data.freezeChance = definition.freezeChance;
            data.blindChance = definition.blindChance;
            data.weakenChance = definition.weakenChance;
            data.vulnerableChance = definition.vulnerableChance;
            data.silenceChance = definition.silenceChance;
            data.manaBurnChance = definition.manaBurnChance;
            data.selfStatusType = definition.selfStatusType;
            data.selfStatusDuration = definition.selfStatusDuration;
            data.selfStatusValue = definition.selfStatusValue;
            data.critBonus = definition.critBonus;
            data.lifeStealRatio = definition.lifeStealRatio;
            data.forceCrit = definition.forceCrit;
            EditorUtility.SetDirty(data);
            skills.Add(data);
        }

        return skills;
    }

    private static List<AetheriaDungeonData> BuildDefaultDungeons()
    {
        var dungeons = new List<AetheriaDungeonData>();
        foreach (var definition in RuntimeDefaultDungeons())
        {
            var number = ReadField<int>(definition, "number");
            var path = DungeonFolder + "/Dungeon_" + number.ToString("00") + ".asset";
            var data = LoadOrCreateAsset<AetheriaDungeonData>(path);

            data.dungeonNumber = number;
            data.dungeonName = ReadField<string>(definition, "name");
            data.recommendedLevel = ReadField<int>(definition, "recommendedLevel");
            data.floors = ReadField<int>(definition, "floors");
            data.description = ReadField<string>(definition, "description");
            data.monsters = new List<AetheriaEnemyData>();

            var monsterIndex = 1;
            var monsters = ReadField<IEnumerable>(definition, "monsters");
            if (monsters != null)
            {
                foreach (var monster in monsters)
                {
                    var enemy = BuildDefaultEnemy(number, "Monster_" + monsterIndex.ToString("00"), monster);
                    if (enemy != null)
                    {
                        data.monsters.Add(enemy);
                    }

                    monsterIndex++;
                }
            }

            data.boss = BuildDefaultEnemy(number, "Boss", ReadField<object>(definition, "boss"));
            EditorUtility.SetDirty(data);
            dungeons.Add(data);
        }

        dungeons.Sort((left, right) => left.dungeonNumber.CompareTo(right.dungeonNumber));
        return dungeons;
    }

    private static AetheriaEnemyData BuildDefaultEnemy(int dungeonNumber, string roleKey, object definition)
    {
        if (definition == null)
        {
            return null;
        }

        var path = EnemyFolder + "/Enemy_D" + dungeonNumber.ToString("00") + "_" + roleKey + ".asset";
        var data = LoadOrCreateAsset<AetheriaEnemyData>(path);
        var existingSkills = data.skills != null ? new List<AetheriaSkillData>(data.skills) : new List<AetheriaSkillData>();
        data.enemyName = ReadField<string>(definition, "name");
        data.description = ReadField<string>(definition, "description");
        data.hp = ReadField<int>(definition, "hp");
        var maxMp = ReadField<int>(definition, "maxMp");
        if (maxMp > 0 || data.maxMp <= 0)
        {
            data.maxMp = maxMp;
        }

        data.attack = ReadField<int>(definition, "attack");
        data.magic = ReadField<int>(definition, "magic");
        data.defense = ReadField<int>(definition, "defense");
        var speed = ReadField<int>(definition, "speed");
        if (speed > 0 || data.speed <= 0)
        {
            data.speed = speed;
        }

        data.critRate = ReadField<float>(definition, "crit");
        data.goldReward = ReadField<int>(definition, "gold");
        data.xpReward = ReadField<int>(definition, "xp");
        data.skills = BuildDefaultEnemySkills(dungeonNumber, roleKey, ReadField<IEnumerable>(definition, "skills"));
        PreserveExistingEnemySkills(data.skills, existingSkills);
        EditorUtility.SetDirty(data);
        return data;
    }

    private static List<AetheriaSkillData> BuildDefaultEnemySkills(int dungeonNumber, string roleKey, IEnumerable definitions)
    {
        var skills = new List<AetheriaSkillData>();
        if (definitions == null)
        {
            return skills;
        }

        var skillIndex = 1;
        foreach (var definition in definitions)
        {
            if (definition == null)
            {
                continue;
            }

            var path = EnemySkillFolder + "/Skill_D" + dungeonNumber.ToString("00") + "_" + roleKey + "_" + skillIndex.ToString("00") + ".asset";
            var existing = AssetDatabase.LoadAssetAtPath<AetheriaSkillData>(path);
            var data = existing != null ? existing : LoadOrCreateAsset<AetheriaSkillData>(path);
            if (existing == null || string.IsNullOrEmpty(data.skillName))
            {
                ApplyDefaultEnemySkill(data, definition);
            }

            EditorUtility.SetDirty(data);
            skills.Add(data);
            skillIndex++;
        }

        return skills;
    }

    private static void ApplyDefaultEnemySkill(AetheriaSkillData data, object definition)
    {
        data.skillName = ReadField<string>(definition, "name");
        data.description = "";
        data.mpCost = ReadField<int>(definition, "mpCost");
        data.multiplier = ReadField<float>(definition, "multiplier");
        data.usesMagic = ReadField<bool>(definition, "magic");
        data.noDamage = ReadField<bool>(definition, "noDamage");
        data.grantsShield = ReadField<bool>(definition, "grantsShield");
        data.healsSelf = ReadField<bool>(definition, "healsSelf");
        data.stunChance = ReadField<float>(definition, "stunChance");
        data.burnChance = ReadField<float>(definition, "burnChance");
        data.poisonChance = ReadField<float>(definition, "poisonChance");
        data.bleedChance = ReadField<float>(definition, "bleedChance");
        data.shockChance = ReadField<float>(definition, "shockChance");
        data.freezeChance = ReadField<float>(definition, "freezeChance");
        data.blindChance = ReadField<float>(definition, "blindChance");
        data.weakenChance = ReadField<float>(definition, "weakenChance");
        data.vulnerableChance = ReadField<float>(definition, "vulnerableChance");
        data.silenceChance = ReadField<float>(definition, "silenceChance");
        data.manaBurnChance = ReadField<float>(definition, "manaBurnChance");
        data.selfStatusType = ReadField<string>(definition, "selfStatusType");
        data.selfStatusDuration = ReadField<int>(definition, "selfStatusDuration");
        data.selfStatusValue = ReadField<float>(definition, "selfStatusValue");
        data.critBonus = ReadField<float>(definition, "critBonus");
        data.lifeStealRatio = ReadField<float>(definition, "lifeStealRatio");
        data.forceCrit = ReadField<bool>(definition, "forceCrit");
    }

    private static void PreserveExistingEnemySkills(List<AetheriaSkillData> target, IEnumerable<AetheriaSkillData> existingSkills)
    {
        if (target == null || existingSkills == null)
        {
            return;
        }

        foreach (var skill in existingSkills)
        {
            if (skill == null || target.Contains(skill))
            {
                continue;
            }

            target.Add(skill);
        }
    }

    private static T LoadOrCreateAsset<T>(string path) where T : ScriptableObject
    {
        var data = AssetDatabase.LoadAssetAtPath<T>(path);
        if (data == null)
        {
            if (File.Exists(ProjectFilePath(path)))
            {
                AssetDatabase.DeleteAsset(path);
            }

            data = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(data, path);
        }

        return data;
    }

    private static List<object> RuntimeDefaultDungeons()
    {
        var method = typeof(AetheriaGame).GetMethod("DefaultDungeons", StaticMethodFlags);
        if (method == null)
        {
            throw new MissingMethodException(nameof(AetheriaGame), "DefaultDungeons");
        }

        var source = method.Invoke(null, null) as IEnumerable;
        var dungeons = new List<object>();
        if (source == null)
        {
            return dungeons;
        }

        foreach (var dungeon in source)
        {
            if (dungeon != null)
            {
                dungeons.Add(dungeon);
            }
        }

        return dungeons;
    }

    private static T ReadField<T>(object source, string fieldName)
    {
        if (source == null)
        {
            return default(T);
        }

        var field = source.GetType().GetField(fieldName, InstanceFieldFlags);
        if (field == null)
        {
            return default(T);
        }

        var value = field.GetValue(source);
        if (value == null)
        {
            return default(T);
        }

        if (value is T)
        {
            return (T)value;
        }

        return (T)Convert.ChangeType(value, typeof(T));
    }

    private static IEnumerable<HeroDefinition> HeroDefinitions()
    {
        yield return new HeroDefinition("Paladin", "성기사", "높은 방어력과 생명력으로 전선을 지키는 강철 방패입니다.", "knight", false, 160, 50, 18, 5, 12, 0.05f, new[]
        {
            new SkillDefinition("ShieldSlam", "방패 후려치기", 15, 1.3f, false, stunChance: 0.5f),
            new SkillDefinition("IronWall", "철벽 방어", 20, 0.6f, false, noDamage: true, grantsShield: true),
            new SkillDefinition("HolyCrush", "성광 분쇄", 25, 1.6f, false, stunChance: 0.5f)
        });
        yield return new HeroDefinition("Elementalist", "원소술사", "낮은 생명력 대신 강력한 원소 마법 화력을 발휘합니다.", "mage", true, 90, 150, 6, 24, 4, 0.08f, new[]
        {
            new SkillDefinition("Fireball", "화염구", 25, 1.8f, true, burnChance: 0.64f),
            new SkillDefinition("FrostBarrier", "냉기 장벽", 20, 0.55f, false, noDamage: true, grantsShield: true),
            new SkillDefinition("LightningStorm", "번개 폭풍", 35, 1.95f, true, stunChance: 0.35f, critBonus: 0.4f)
        });
        yield return new HeroDefinition("Rogue", "그림자 자객", "민첩한 움직임과 치명타, 독으로 적의 급소를 노립니다.", "rogue", false, 110, 70, 15, 8, 6, 0.2f, new[]
        {
            new SkillDefinition("ShadowRaid", "그림자 습격", 20, 1.82f, false, critBonus: 0.4f, lifeStealRatio: 0.25f),
            new SkillDefinition("PoisonDagger", "독 묻은 단검", 25, 1.17f, false, poisonChance: 1f),
            new SkillDefinition("TwilightFlurry", "황혼 난무", 40, 2.02f, false, poisonChance: 1f, critBonus: 0.4f, forceCrit: true)
        });
        yield return new HeroDefinition("Cleric", "빛의 사제", "빛의 권능으로 회복과 공격을 함께 수행하는 마법 전투가입니다.", "priest", true, 120, 130, 7, 21, 7, 0.07f, new[]
        {
            new SkillDefinition("RadiantPunishment", "찬란한 징벌", 22, 1.74f, true, stunChance: 0.5f),
            new SkillDefinition("Sanctuary", "성역", 28, 0.6f, false, noDamage: true, grantsShield: true, selfStatusType: "damage_boost", selfStatusDuration: 2, selfStatusValue: 0.2f),
            new SkillDefinition("SalvationRay", "구원의 광선", 36, 1.75f, true, vulnerableChance: 0.19f, lifeStealRatio: 0.2f)
        });
        yield return new HeroDefinition("Bomber", "폭렬술사", "폭발 마법으로 짧은 시간에 큰 피해를 쏟아붓습니다.", "bomber", true, 95, 135, 5, 27, 4, 0.1f, new[]
        {
            new SkillDefinition("BlazingFlame", "폭염 불꽃", 26, 1.85f, true, burnChance: 0.54f),
            new SkillDefinition("ShatterBomb", "파쇄 폭탄", 34, 2.05f, true, vulnerableChance: 0.39f),
            new SkillDefinition("ChainDetonation", "연쇄 기폭", 48, 2.35f, true, stunChance: 0.09f, shockChance: 0.49f, critBonus: 0.4f)
        });
        yield return new HeroDefinition("SpiritCaller", "정령술사", "불, 물, 바람, 땅 정령을 번갈아 부리는 사원소 전문가입니다.", "spirit", true, 105, 170, 1, 22, 5, 0.06f, new[]
        {
            new SkillDefinition("FireSpirit", "화염 정령", 30, 1.85f, true, burnChance: 0.59f),
            new SkillDefinition("WaterSpirit", "물결 정령", 30, 1.85f, true, selfStatusType: "shield", selfStatusValue: 0.25f),
            new SkillDefinition("WindSpirit", "질풍 정령", 30, 1.85f, true, blindChance: 0.5f, critBonus: 0.4f),
            new SkillDefinition("EarthSpirit", "대지 정령", 30, 1.85f, true, weakenChance: 0.44f, vulnerableChance: 0.44f)
        });
        yield return new HeroDefinition("WindArcher", "바람 궁수", "원거리에서 치명타와 상태이상 화살을 운용합니다.", "archer", false, 115, 85, 17, 6, 6, 0.16f, new[]
        {
            new SkillDefinition("AimedShot", "정조준 사격", 18, 1.65f, false, critBonus: 0.4f),
            new SkillDefinition("BindingArrow", "속박 화살", 24, 1.35f, false, stunChance: 0.54f),
            new SkillDefinition("PiercingVolley", "관통 연사", 34, 1.95f, false, bleedChance: 0.49f, vulnerableChance: 0.19f),
            new SkillDefinition("WindstepShot", "바람걸음 사격", 30, 1.55f, false, critBonus: 0.4f, selfStatusType: "evasion_boost", selfStatusDuration: 2, selfStatusValue: 0.18f)
        });
        yield return new HeroDefinition("Monk", "무투가", "단련된 육체와 빠른 연격으로 적을 제압합니다.", "monk", false, 130, 75, 19, 4, 8, 0.18f, new[]
        {
            new SkillDefinition("IronFistCombo", "철권 연타", 20, 1.65f, false, critBonus: 0.4f),
            new SkillDefinition("PressureSeal", "급소 봉쇄", 28, 1.55f, false, stunChance: 0.45f),
            new SkillDefinition("InnerPowerBurst", "내공 폭발", 36, 2.05f, false, critBonus: 0.4f, selfStatusType: "damage_boost", selfStatusDuration: 2, selfStatusValue: 0.18f)
        });
    }

    private static IEnumerable<RarityDefinition> RarityDefinitions()
    {
        yield return new RarityDefinition("Common", "일반", 0, new Color32(156, 163, 175, 255), 1f, 45f);
        yield return new RarityDefinition("Uncommon", "고급", 1, new Color32(34, 197, 94, 255), 1.12f, 25f);
        yield return new RarityDefinition("Rare", "희귀", 2, new Color32(59, 130, 246, 255), 1.25f, 15f);
        yield return new RarityDefinition("Epic", "영웅", 3, new Color32(168, 85, 247, 255), 1.6f, 8f);
        yield return new RarityDefinition("Legendary", "전설", 4, new Color32(245, 158, 11, 255), 2.1f, 4f);
        yield return new RarityDefinition("Mythic", "신화", 5, new Color32(236, 72, 153, 255), 2.75f, 2f);
        yield return new RarityDefinition("Ancient", "고대", 6, new Color32(20, 184, 166, 255), 3.5f, 0.7f);
        yield return new RarityDefinition("Immortal", "불멸", 7, new Color32(239, 68, 68, 255), 4.4f, 0.25f);
        yield return new RarityDefinition("Relic", "성물", 8, new Color32(132, 204, 22, 255), 5.2f, 0.12f);
        yield return new RarityDefinition("Celestial", "성좌", 9, new Color32(96, 165, 250, 255), 6.4f, 0.06f);
        yield return new RarityDefinition("Origin", "근원", 10, new Color32(251, 113, 133, 255), 7.8f, 0.03f);
        yield return new RarityDefinition("Transcendent", "초월", 11, new Color32(248, 250, 252, 255), 9.2f, 0.01f);
        yield return new RarityDefinition("Named", "네임드", 12, new Color32(250, 204, 21, 255), 10.5f, 0f);
    }

    private static void EnsureFolder(string folder)
    {
        if (AssetDatabase.IsValidFolder(folder))
        {
            return;
        }

        var slash = folder.LastIndexOf('/');
        if (slash <= 0)
        {
            return;
        }

        var parent = folder.Substring(0, slash);
        var child = folder.Substring(slash + 1);
        EnsureFolder(parent);
        if (!AssetDatabase.IsValidFolder(folder))
        {
            AssetDatabase.CreateFolder(parent, child);
        }
    }

    private static string ProjectFilePath(string relativePath)
    {
        var projectFolder = Directory.GetParent(Application.dataPath).FullName;
        return Path.Combine(projectFolder, relativePath.Replace('/', Path.DirectorySeparatorChar));
    }

    private static void ValidateGeneratedScriptReferences()
    {
        var brokenAssets = new List<string>();
        CollectBrokenScriptReferences(ProjectFilePath(DataFolder), brokenAssets);

        var databasePath = ProjectFilePath(DatabasePath);
        if (File.Exists(databasePath) && File.ReadAllText(databasePath).Contains("m_Script: {fileID: 0}"))
        {
            brokenAssets.Add(DatabasePath);
        }

        if (brokenAssets.Count == 0)
        {
            return;
        }

        var shown = Mathf.Min(8, brokenAssets.Count);
        throw new InvalidOperationException("Generated ScriptableObject assets have missing script references: " + string.Join(", ", brokenAssets.GetRange(0, shown)) + (brokenAssets.Count > shown ? " ..." : ""));
    }

    private static void CollectBrokenScriptReferences(string folder, List<string> brokenAssets)
    {
        if (!Directory.Exists(folder))
        {
            return;
        }

        foreach (var path in Directory.GetFiles(folder, "*.asset", SearchOption.AllDirectories))
        {
            if (File.ReadAllText(path).Contains("m_Script: {fileID: 0}"))
            {
                brokenAssets.Add(path);
            }
        }
    }

    private readonly struct HeroDefinition
    {
        public readonly string assetName;
        public readonly string displayName;
        public readonly string description;
        public readonly string portraitResourceName;
        public readonly bool basicAttackUsesMagic;
        public readonly int maxHp;
        public readonly int maxMp;
        public readonly int physicalAttack;
        public readonly int magicAttack;
        public readonly int defense;
        public readonly float critRate;
        public readonly SkillDefinition[] skills;

        public HeroDefinition(string assetName, string displayName, string description, string portraitResourceName, bool basicAttackUsesMagic, int maxHp, int maxMp, int physicalAttack, int magicAttack, int defense, float critRate, SkillDefinition[] skills)
        {
            this.assetName = assetName;
            this.displayName = displayName;
            this.description = description;
            this.portraitResourceName = portraitResourceName;
            this.basicAttackUsesMagic = basicAttackUsesMagic;
            this.maxHp = maxHp;
            this.maxMp = maxMp;
            this.physicalAttack = physicalAttack;
            this.magicAttack = magicAttack;
            this.defense = defense;
            this.critRate = critRate;
            this.skills = skills;
        }
    }

    private readonly struct SkillDefinition
    {
        public readonly string assetName;
        public readonly string displayName;
        public readonly int mpCost;
        public readonly float multiplier;
        public readonly bool usesMagic;
        public readonly bool noDamage;
        public readonly bool grantsShield;
        public readonly bool healsSelf;
        public readonly float stunChance;
        public readonly float burnChance;
        public readonly float poisonChance;
        public readonly float bleedChance;
        public readonly float shockChance;
        public readonly float freezeChance;
        public readonly float blindChance;
        public readonly float weakenChance;
        public readonly float vulnerableChance;
        public readonly float silenceChance;
        public readonly float manaBurnChance;
        public readonly string selfStatusType;
        public readonly int selfStatusDuration;
        public readonly float selfStatusValue;
        public readonly float critBonus;
        public readonly float lifeStealRatio;
        public readonly bool forceCrit;

        public SkillDefinition(string assetName, string displayName, int mpCost, float multiplier, bool usesMagic, bool noDamage = false, bool grantsShield = false, bool healsSelf = false, float stunChance = 0f, float burnChance = 0f, float poisonChance = 0f, float bleedChance = 0f, float shockChance = 0f, float freezeChance = 0f, float blindChance = 0f, float weakenChance = 0f, float vulnerableChance = 0f, float silenceChance = 0f, float manaBurnChance = 0f, string selfStatusType = "", int selfStatusDuration = 0, float selfStatusValue = 0f, float critBonus = 0f, float lifeStealRatio = 0f, bool forceCrit = false)
        {
            this.assetName = assetName;
            this.displayName = displayName;
            this.mpCost = mpCost;
            this.multiplier = multiplier;
            this.usesMagic = usesMagic;
            this.noDamage = noDamage;
            this.grantsShield = grantsShield;
            this.healsSelf = healsSelf;
            this.stunChance = stunChance;
            this.burnChance = burnChance;
            this.poisonChance = poisonChance;
            this.bleedChance = bleedChance;
            this.shockChance = shockChance;
            this.freezeChance = freezeChance;
            this.blindChance = blindChance;
            this.weakenChance = weakenChance;
            this.vulnerableChance = vulnerableChance;
            this.silenceChance = silenceChance;
            this.manaBurnChance = manaBurnChance;
            this.selfStatusType = selfStatusType;
            this.selfStatusDuration = selfStatusDuration;
            this.selfStatusValue = selfStatusValue;
            this.critBonus = critBonus;
            this.lifeStealRatio = lifeStealRatio;
            this.forceCrit = forceCrit;
        }
    }

    private readonly struct RarityDefinition
    {
        public readonly string assetName;
        public readonly string displayName;
        public readonly int rank;
        public readonly Color color;
        public readonly float statMultiplier;
        public readonly float dropWeight;

        public RarityDefinition(string assetName, string displayName, int rank, Color color, float statMultiplier, float dropWeight)
        {
            this.assetName = assetName;
            this.displayName = displayName;
            this.rank = rank;
            this.color = color;
            this.statMultiplier = statMultiplier;
            this.dropWeight = dropWeight;
        }
    }
}
#endif
