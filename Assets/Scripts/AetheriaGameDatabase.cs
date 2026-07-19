using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Aetheria/Game Database", fileName = "AetheriaGameDatabase")]
public sealed class AetheriaGameDatabase : ScriptableObject
{
    public List<AetheriaHeroClassData> heroClasses = new List<AetheriaHeroClassData>();
    public List<AetheriaDungeonData> dungeons = new List<AetheriaDungeonData>();
    public List<AetheriaRarityData> rarities = new List<AetheriaRarityData>();
}
