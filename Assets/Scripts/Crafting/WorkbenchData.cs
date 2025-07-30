using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "New Workbench", menuName = "Survival Game/Workbench Data")]
public class WorkbenchData : ScriptableObject
{
    [Header("Basic Info")]
    public string workbenchName;
    public string description;
    public Sprite icon;
    public GameObject workbenchPrefab;
    
    [Header("Workbench Properties")]
    public WorkbenchTier tier;
    public WorkbenchType workbenchType;
    
    [Header("Crafting Capabilities")]
    public List<CraftingCategory> allowedCategories = new List<CraftingCategory>();
    public int maxCraftingSlots = 1; // How many items can be crafted simultaneously
    public float craftingSpeedMultiplier = 1f; // Speed bonus for crafting
    public bool canRepairItems = true;
    public bool canRecycleItems = false;
    
    [Header("Research Capabilities")]
    public bool canResearchBlueprints = false;
    public int scrapCostPerResearch = 75; // Base scrap cost
    public float researchTimeMultiplier = 1f;
    public List<ItemType> researchableItemTypes = new List<ItemType>();
    
    [Header("Building Requirements")]
    public RecipeIngredient[] buildingCost;
    public float buildTime = 30f;
    public bool requiresFoundation = true;
    public Vector3 placementSize = new Vector3(2f, 1f, 1f);
    
    [Header("Durability & Maintenance")]
    public float maxHealth = 500f;
    public float decayRate = 1f; // HP lost per hour without upkeep
    public RecipeIngredient[] upkeepCost; // Daily upkeep materials
    
    [Header("Visual & Audio")]
    public Material workbenchMaterial;
    public Color workbenchColor = Color.white;
    public AudioClip craftingSound;
    public AudioClip researchSound;
    public ParticleSystem craftingEffect;
    public ParticleSystem researchEffect;
    
    [Header("Access Control")]
    public bool requiresAuthorization = true; // Needs tool cupboard auth
    public float interactionRange = 3f;
    public bool canBeUsedByEnemies = false;

    public static WorkbenchData CreateWorkbenchTier(WorkbenchTier tier)
    {
        WorkbenchData workbench = CreateInstance<WorkbenchData>();
        workbench.tier = tier;
        workbench.workbenchType = WorkbenchType.Workbench;
        
        switch (tier)
        {
            case WorkbenchTier.Tier1:
                SetupTier1Workbench(workbench);
                break;
            case WorkbenchTier.Tier2:
                SetupTier2Workbench(workbench);
                break;
            case WorkbenchTier.Tier3:
                SetupTier3Workbench(workbench);
                break;
        }
        
        return workbench;
    }

    static void SetupTier1Workbench(WorkbenchData workbench)
    {
        workbench.workbenchName = "Workbench Level 1";
        workbench.description = "Basic crafting station for simple tools and weapons.";
        workbench.maxCraftingSlots = 1;
        workbench.craftingSpeedMultiplier = 1f;
        workbench.canRepairItems = true;
        workbench.canRecycleItems = false;
        workbench.canResearchBlueprints = false;
        workbench.maxHealth = 250f;
        workbench.buildTime = 30f;
        
        // Allowed crafting categories for Tier 1
        workbench.allowedCategories = new List<CraftingCategory>
        {
            CraftingCategory.Tools,
            CraftingCategory.Weapons,
            CraftingCategory.BasicArmor,
            CraftingCategory.Components,
            CraftingCategory.Consumables
        };
        
        // Building cost for Tier 1
        workbench.buildingCost = new RecipeIngredient[]
        {
            new RecipeIngredient { itemData = GetItemData("Wood"), amount = 500 },
            new RecipeIngredient { itemData = GetItemData("MetalOre"), amount = 50 }
        };
        
        // Upkeep cost
        workbench.upkeepCost = new RecipeIngredient[]
        {
            new RecipeIngredient { itemData = GetItemData("Wood"), amount = 20 }
        };
    }

    static void SetupTier2Workbench(WorkbenchData workbench)
    {
        workbench.workbenchName = "Workbench Level 2";
        workbench.description = "Advanced crafting station with repair and recycling capabilities.";
        workbench.maxCraftingSlots = 2;
        workbench.craftingSpeedMultiplier = 1.2f;
        workbench.canRepairItems = true;
        workbench.canRecycleItems = true;
        workbench.canResearchBlueprints = false;
        workbench.maxHealth = 500f;
        workbench.buildTime = 60f;
        
        // Allowed crafting categories for Tier 2
        workbench.allowedCategories = new List<CraftingCategory>
        {
            CraftingCategory.Tools,
            CraftingCategory.Weapons,
            CraftingCategory.BasicArmor,
            CraftingCategory.MediumArmor,
            CraftingCategory.Components,
            CraftingCategory.Consumables,
            CraftingCategory.Electronics,
            CraftingCategory.Explosives
        };
        
        // Building cost for Tier 2
        workbench.buildingCost = new RecipeIngredient[]
        {
            new RecipeIngredient { itemData = GetItemData("Wood"), amount = 500 },
            new RecipeIngredient { itemData = GetItemData("MetalOre"), amount = 200 },
            new RecipeIngredient { itemData = GetItemData("Scrap"), amount = 100 },
            new RecipeIngredient { itemData = GetItemData("TechTrash"), amount = 5 }
        };
        
        // Upkeep cost
        workbench.upkeepCost = new RecipeIngredient[]
        {
            new RecipeIngredient { itemData = GetItemData("Wood"), amount = 20 },
            new RecipeIngredient { itemData = GetItemData("MetalOre"), amount = 10 }
        };
    }

    static void SetupTier3Workbench(WorkbenchData workbench)
    {
        workbench.workbenchName = "Workbench Level 3";
        workbench.description = "Master crafting station for the most advanced items and HQM gear.";
        workbench.maxCraftingSlots = 3;
        workbench.craftingSpeedMultiplier = 1.5f;
        workbench.canRepairItems = true;
        workbench.canRecycleItems = true;
        workbench.canResearchBlueprints = false;
        workbench.maxHealth = 1000f;
        workbench.buildTime = 120f;
        
        // Allowed crafting categories for Tier 3 (everything)
        workbench.allowedCategories = new List<CraftingCategory>
        {
            CraftingCategory.Tools,
            CraftingCategory.Weapons,
            CraftingCategory.BasicArmor,
            CraftingCategory.MediumArmor,
            CraftingCategory.HeavyArmor,
            CraftingCategory.Components,
            CraftingCategory.Consumables,
            CraftingCategory.Electronics,
            CraftingCategory.Explosives,
            CraftingCategory.Advanced
        };
        
        // Building cost for Tier 3
        workbench.buildingCost = new RecipeIngredient[]
        {
            new RecipeIngredient { itemData = GetItemData("Wood"), amount = 1000 },
            new RecipeIngredient { itemData = GetItemData("MetalOre"), amount = 500 },
            new RecipeIngredient { itemData = GetItemData("HighQualityMetal"), amount = 5 },
            new RecipeIngredient { itemData = GetItemData("Scrap"), amount = 200 },
            new RecipeIngredient { itemData = GetItemData("TechTrash"), amount = 10 },
            new RecipeIngredient { itemData = GetItemData("Components"), amount = 3 }
        };
        
        // Upkeep cost
        workbench.upkeepCost = new RecipeIngredient[]
        {
            new RecipeIngredient { itemData = GetItemData("Wood"), amount = 30 },
            new RecipeIngredient { itemData = GetItemData("MetalOre"), amount = 20 },
            new RecipeIngredient { itemData = GetItemData("HighQualityMetal"), amount = 1 }
        };
    }

    public static WorkbenchData CreateResearchTable()
    {
        WorkbenchData researchTable = CreateInstance<WorkbenchData>();
        researchTable.workbenchName = "Research Table";
        researchTable.description = "Research items to learn their blueprints. Consumes the item and scrap.";
        researchTable.workbenchType = WorkbenchType.ResearchTable;
        researchTable.tier = WorkbenchTier.Research;
        researchTable.maxCraftingSlots = 0;
        researchTable.canRepairItems = false;
        researchTable.canRecycleItems = false;
        researchTable.canResearchBlueprints = true;
        researchTable.scrapCostPerResearch = 75;
        researchTable.researchTimeMultiplier = 1f;
        researchTable.maxHealth = 300f;
        researchTable.buildTime = 45f;
        
        // Research table can research most item types
        researchTable.researchableItemTypes = new List<ItemType>
        {
            ItemType.Tool,
            ItemType.Weapon,
            ItemType.Armor,
            ItemType.Component,
            ItemType.Deployable,
            ItemType.Medical,
            ItemType.Explosive
        };
        
        // Building cost for Research Table
        researchTable.buildingCost = new RecipeIngredient[]
        {
            new RecipeIngredient { itemData = GetItemData("Wood"), amount = 200 },
            new RecipeIngredient { itemData = GetItemData("MetalOre"), amount = 100 },
            new RecipeIngredient { itemData = GetItemData("Scrap"), amount = 75 }
        };
        
        // Upkeep cost
        researchTable.upkeepCost = new RecipeIngredient[]
        {
            new RecipeIngredient { itemData = GetItemData("Wood"), amount = 10 }
        };
        
        return researchTable;
    }

    public bool CanCraftCategory(CraftingCategory category)
    {
        return allowedCategories.Contains(category);
    }

    public bool CanCraftRecipe(CraftingRecipe recipe)
    {
        if (recipe == null) return false;
        
        // Check if workbench tier is sufficient
        if (recipe.requiredWorkbench > (int)tier) return false;
        
        // Check if category is allowed
        if (!CanCraftCategory(recipe.category)) return false;
        
        return true;
    }

    public bool CanResearchItem(ItemData item)
    {
        if (!canResearchBlueprints) return false;
        if (item == null) return false;
        
        return researchableItemTypes.Contains(item.itemType);
    }

    public int GetResearchCost(ItemData item)
    {
        if (!CanResearchItem(item)) return 0;
        
        // Base cost modified by item complexity
        float multiplier = 1f;
        
        switch (item.itemType)
        {
            case ItemType.Tool:
                multiplier = 0.8f;
                break;
            case ItemType.Weapon:
                multiplier = 1.2f;
                break;
            case ItemType.Armor:
                multiplier = 1.5f;
                break;
            case ItemType.Component:
                multiplier = 0.5f;
                break;
            case ItemType.Explosive:
                multiplier = 2f;
                break;
            case ItemType.Medical:
                multiplier = 1.1f;
                break;
            default:
                multiplier = 1f;
                break;
        }
        
        return Mathf.RoundToInt(scrapCostPerResearch * multiplier);
    }

    public float GetResearchTime(ItemData item)
    {
        if (!CanResearchItem(item)) return 0f;
        
        // Base research time of 10 seconds, modified by item complexity
        float baseTime = 10f;
        float multiplier = researchTimeMultiplier;
        
        switch (item.itemType)
        {
            case ItemType.Tool:
                multiplier *= 0.8f;
                break;
            case ItemType.Weapon:
                multiplier *= 1.2f;
                break;
            case ItemType.Armor:
                multiplier *= 1.5f;
                break;
            case ItemType.Component:
                multiplier *= 0.5f;
                break;
            case ItemType.Explosive:
                multiplier *= 2f;
                break;
            default:
                multiplier *= 1f;
                break;
        }
        
        return baseTime * multiplier;
    }

    static ItemData GetItemData(string itemName)
    {
        // This would fetch actual ItemData from a registry
        return new ItemData { itemName = itemName };
    }
}

public enum WorkbenchTier
{
    Tier1 = 1,
    Tier2 = 2,
    Tier3 = 3,
    Research = 0  // Special tier for research table
}

public enum WorkbenchType
{
    Workbench,      // Standard crafting workbench
    ResearchTable,  // Research station for blueprints
    RepairBench,    // Specialized for repairs (future)
    Recycler       // For recycling items (future)
}