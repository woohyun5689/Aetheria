using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Aetheria/Enemy", fileName = "EnemyData")]
public sealed class AetheriaEnemyData : ScriptableObject
{
    public string enemyName;
    [TextArea] public string description;
    public int hp = 100;
    public int maxMp = 0;
    public int attack = 10;
    public int magic = 0;
    public int defense = 5;
    public int speed = 5;
    public float critRate = 0.05f;
    public int goldReward = 10;
    public int xpReward = 20;
    public List<AetheriaSkillData> skills = new List<AetheriaSkillData>();
}
