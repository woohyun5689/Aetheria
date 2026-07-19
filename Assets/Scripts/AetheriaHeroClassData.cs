using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Aetheria/Hero Class", fileName = "HeroClassData")]
public sealed class AetheriaHeroClassData : ScriptableObject
{
    public string heroName;
    [TextArea] public string description;
    public string portraitResourceName;
    public Sprite portrait;
    public bool basicAttackUsesMagic;
    public int maxHp = 100;
    public int maxMp = 50;
    public int physicalAttack = 10;
    public int magicAttack = 10;
    public int defense = 5;
    public float critRate = 0.05f;
    public List<AetheriaSkillData> skills = new List<AetheriaSkillData>();
}
