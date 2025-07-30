using UnityEngine;

[CreateAssetMenu(fileName = "New Building", menuName = "Survival Game/Building Data")]
public class BuildingData : ScriptableObject
{
    [Header("Basic Info")]
    public string buildingName;
    public string description;
    public Sprite icon;
    public GameObject prefab;
    
    [Header("Building Properties")]
    public BuildingType buildingType;
    public BuildingCategory category;
    public Vector3 size = Vector3.one;
    public bool requiresFoundation = true;
    public bool canUpgrade = false;
    
    [Header("Placement")]
    public LayerMask placementLayerMask = 1;
    public float placementRange = 5f;
    public bool snapToGrid = true;
    public float gridSize = 1f;
    
    [Header("Cost")]
    public RecipeIngredient[] buildCost;
    public float buildTime = 5f;
    
    [Header("Health & Durability")]
    public float maxHealth = 100f;
    public float durabilityLoss = 1f; // Per day
    public bool canDecay = true;
    
    [Header("Functionality")]
    public bool providesStorage = false;
    public int storageSlots = 0;
    public bool providesShelte = false;
    public float shelterRadius = 0f;
    public bool isWorkstation = false;
    public CraftingStationType workstationType;
    
    [Header("Upgrade Path")]
    public BuildingData[] upgradePaths;
    public RecipeIngredient[] upgradeCost;

    public bool CanPlace(Vector3 position, Quaternion rotation, LayerMask groundLayer)
    {
        // Check if position is valid for placement
        Collider[] overlapping = Physics.OverlapBox(
            position + Vector3.up * size.y * 0.5f,
            size * 0.5f,
            rotation
        );
        
        // Check for conflicts with existing buildings
        foreach (var collider in overlapping)
        {
            if (collider.GetComponent<BuildingPiece>() != null)
                return false;
        }
        
        // Check ground placement
        if (requiresFoundation)
        {
            RaycastHit hit;
            if (!Physics.Raycast(position + Vector3.up, Vector3.down, out hit, 2f, groundLayer))
                return false;
        }
        
        return true;
    }

    public Vector3 SnapToGrid(Vector3 position)
    {
        if (!snapToGrid) return position;
        
        float snappedX = Mathf.Round(position.x / gridSize) * gridSize;
        float snappedZ = Mathf.Round(position.z / gridSize) * gridSize;
        
        return new Vector3(snappedX, position.y, snappedZ);
    }

    public bool HasRequiredResources(InventoryManager inventory)
    {
        foreach (var cost in buildCost)
        {
            if (!inventory.HasItem(cost.itemData, cost.amount))
                return false;
        }
        return true;
    }

    public bool ConsumeResources(InventoryManager inventory)
    {
        if (!HasRequiredResources(inventory))
            return false;
            
        foreach (var cost in buildCost)
        {
            if (!inventory.RemoveItem(cost.itemData, cost.amount))
                return false;
        }
        
        return true;
    }

    public string GetCostText()
    {
        var parts = new System.Collections.Generic.List<string>();
        foreach (var cost in buildCost)
        {
            parts.Add($"{cost.amount}x {cost.itemData.GetDisplayName()}");
        }
        return string.Join("\n", parts);
    }
}

public enum BuildingType
{
    Foundation,
    Wall,
    Floor,
    Ceiling,
    Door,
    Window,
    Stairs,
    Roof,
    Workstation,
    Storage,
    Decoration
}

public enum BuildingTier
{
    Twig,      // Basic wood structures - very weak
    Wood,      // Standard wood - moderate strength
    Stone,     // Stone - strong against melee, weak to explosives
    Metal,     // Metal - strong against most attacks
    HQM        // High Quality Metal - strongest tier
}

public enum BuildingCategory
{
    Structure,
    Crafting,
    Storage,
    Defense,
    Comfort,
    Utility
}