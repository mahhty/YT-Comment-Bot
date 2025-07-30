using UnityEngine;

[CreateAssetMenu(fileName = "New Resource", menuName = "Survival Game/Resource Data")]
public class ResourceData : ScriptableObject
{
    [Header("Basic Info")]
    public string resourceName;
    public string description;
    public Sprite icon;
    public GameObject worldModel; // 3D model for world placement
    
    [Header("Resource Properties")]
    public ResourceType resourceType;
    public int baseValue = 1;
    public float gatherTime = 2f; // Time in seconds to gather
    public int maxStackSize = 100;
    
    [Header("Gathering")]
    public ToolType requiredTool = ToolType.None;
    public int minGatherAmount = 1;
    public int maxGatherAmount = 3;
    public float gatherRadius = 2f; // Distance from which player can gather
    
    [Header("Audio")]
    public AudioClip gatherSound;
    public AudioClip gatherCompleteSound;
    
    [Header("Effects")]
    public GameObject gatherEffect; // Particle effect when gathering
    public GameObject depletedEffect; // Effect when resource is depleted
}

public enum ResourceType
{
    Wood,
    Stone,
    MetalOre,
    HighQualityMetal, // HQM - rare upgrade material
    Food,
    Water,
    Fiber,
    Coal,
    Sulfur,
    AnimalHide,
    Cloth,
    LowGradeFuel,
    Scrap,
    TechTrash,
    Components
}

public enum ToolType
{
    None,
    Hatchet,
    Pickaxe,
    Knife,
    Bucket
}