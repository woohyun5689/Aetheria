using UnityEngine;

[CreateAssetMenu(menuName = "Aetheria/Skill", fileName = "SkillData")]
public sealed class AetheriaSkillData : ScriptableObject
{
    public string skillName;
    [TextArea] public string description;
    public int mpCost;
    public float multiplier = 1f;
    public bool usesMagic;
    public bool noDamage;
    public bool grantsShield;
    public bool healsSelf;
    public float stunChance;
    public float burnChance;
    public float poisonChance;
    public float bleedChance;
    public float shockChance;
    public float freezeChance;
    public float blindChance;
    public float weakenChance;
    public float vulnerableChance;
    public float silenceChance;
    public float manaBurnChance;
    public string selfStatusType;
    public int selfStatusDuration;
    public float selfStatusValue;
    public float critBonus;
    public float lifeStealRatio;
    public bool forceCrit;
}
