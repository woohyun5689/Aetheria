using UnityEngine;

[CreateAssetMenu(menuName = "Aetheria/Rarity", fileName = "RarityData")]
public sealed class AetheriaRarityData : ScriptableObject
{
    public string rarityName;
    public int rank;
    public Color color = Color.white;
    public float statMultiplier = 1f;
    public float dropWeight = 1f;
}
