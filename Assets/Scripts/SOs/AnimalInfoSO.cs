using UnityEngine;

/// <summary>
/// ScriptableObject defining an animal species
/// </summary>
[CreateAssetMenu(fileName = "New Animal", menuName = "Wild Lens/Animal Info")]
public class AnimalInfo : ScriptableObject
{
    [Header("Identity")]
    public string speciesName;
    public string scientificName;
    [TextArea(3, 5)]
    public string description;

    [Header("Photography Properties")]
    [Range(0f, 10f)]
    public float rarityScore = 1f; // 1 = common, 10 = ultra rare
    public float optimalPhotoDistance = 10f; // Ideal distance for photos

    [Header("Visuals")]
    public Sprite icon; // For collection UI
    public Color uiColor = Color.white;

    [Header("Behavior Hints")]
    public string[] commonBehaviors; // "Eating", "Sleeping", "Running"
    public string habitatType; // "Forest", "Mountain", "Plains"
}