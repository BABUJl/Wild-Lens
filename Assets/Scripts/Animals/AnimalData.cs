using UnityEngine;

/// <summary>
/// Component attached to animal GameObjects
/// Links the instance to its species data
/// </summary>
public class AnimalData : MonoBehaviour
{
    [Header("Species Information")]
    public AnimalInfo animalInfo;

    [Header("Instance Properties")]
    public bool isRareVariant = false; // Golden bear, albino spider, etc.
    public string variantName = ""; // "Albino", "Golden", etc.

    private void Start()
    {
        if (animalInfo == null)
        {
            Debug.LogError($"AnimalData on {gameObject.name} has no AnimalInfo assigned!");
        }
    }

    /// <summary>
    /// Get display name for this animal
    /// </summary>
    public string GetDisplayName()
    {
        if (isRareVariant && !string.IsNullOrEmpty(variantName))
        {
            return $"{variantName} {animalInfo.speciesName}";
        }
        return animalInfo.speciesName;
    }

    /// <summary>
    /// Get final rarity score (including variant bonus)
    /// </summary>
    public float GetRarityScore()
    {
        float score = animalInfo.rarityScore;
        if (isRareVariant)
        {
            score *= 2f; // Rare variants are worth 2x
        }
        return score;
    }
}