using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Aetheria/Dungeon", fileName = "DungeonData")]
public sealed class AetheriaDungeonData : ScriptableObject
{
    public int dungeonNumber = 1;
    public string dungeonName;
    [TextArea] public string description;
    public int recommendedLevel = 1;
    public int floors = 5;
    public List<AetheriaEnemyData> monsters = new List<AetheriaEnemyData>();
    public AetheriaEnemyData boss;
}
