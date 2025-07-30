using UnityEngine;

[CreateAssetMenu(fileName = "New Armor", menuName = "Survival Game/Armor Data")]
public class ArmorData : ScriptableObject
{
    [Header("Basic Info")]
    public string armorName;
    public string description;
    public Sprite icon;
    public GameObject armorModel; // 3D model for player
    
    [Header("Armor Properties")]
    public ArmorType armorType;
    public ArmorSlot armorSlot;
    public ArmorTier armorTier;
    
    [Header("Protection Values")]
    [Range(0f, 100f)]
    public float projectileProtection = 0f; // Bullets, arrows
    [Range(0f, 100f)]
    public float meleeProtection = 0f; // Melee weapons
    [Range(0f, 100f)]
    public float explosiveProtection = 0f; // Explosions, rockets
    [Range(0f, 100f)]
    public float fireProtection = 0f; // Fire damage
    [Range(0f, 100f)]
    public float radiationProtection = 0f; // Radiation zones
    [Range(0f, 100f)]
    public float coldProtection = 0f; // Temperature protection
    
    [Header("Durability")]
    public float maxDurability = 100f;
    public float repairCostMultiplier = 0.5f; // Cost to repair as % of craft cost
    
    [Header("Crafting")]
    public RecipeIngredient[] craftingCost;
    public float craftingTime = 30f;
    public int requiredWorkbench = 1; // Workbench level required
    
    [Header("Movement Effects")]
    [Range(-50f, 50f)]
    public float movementSpeedModifier = 0f; // % change to movement speed
    [Range(-50f, 50f)]
    public float jumpHeightModifier = 0f; // % change to jump height
    
    [Header("Visual Effects")]
    public Material armorMaterial;
    public Color armorColor = Color.white;
    public bool hasGlow = false;
    public Color glowColor = Color.cyan;

    public float GetTotalProtection()
    {
        return (projectileProtection + meleeProtection + explosiveProtection) / 3f;
    }

    public float GetProtectionForDamageType(DamageType damageType)
    {
        switch (damageType)
        {
            case DamageType.Projectile:
                return projectileProtection;
            case DamageType.Melee:
                return meleeProtection;
            case DamageType.Explosive:
                return explosiveProtection;
            case DamageType.Fire:
                return fireProtection;
            default:
                return GetTotalProtection();
        }
    }

    public static ArmorData CreateArmorSet(ArmorTier tier, ArmorSlot slot)
    {
        ArmorData armor = CreateInstance<ArmorData>();
        armor.armorTier = tier;
        armor.armorSlot = slot;
        
        switch (tier)
        {
            case ArmorTier.Wood:
                SetupWoodArmor(armor, slot);
                break;
            case ArmorTier.Bone:
                SetupBoneArmor(armor, slot);
                break;
            case ArmorTier.Leather:
                SetupLeatherArmor(armor, slot);
                break;
            case ArmorTier.Metal:
                SetupMetalArmor(armor, slot);
                break;
            case ArmorTier.HQM:
                SetupHQMArmor(armor, slot);
                break;
        }
        
        return armor;
    }

    static void SetupWoodArmor(ArmorData armor, ArmorSlot slot)
    {
        armor.armorType = ArmorType.Light;
        armor.projectileProtection = 15f;
        armor.meleeProtection = 25f;
        armor.explosiveProtection = 5f;
        armor.fireProtection = -10f; // Vulnerable to fire
        armor.coldProtection = 10f;
        armor.maxDurability = 50f;
        armor.movementSpeedModifier = -5f;
        
        armor.craftingCost = new RecipeIngredient[]
        {
            new RecipeIngredient { itemData = GetItemData("Wood"), amount = GetWoodAmount(slot) },
            new RecipeIngredient { itemData = GetItemData("Cloth"), amount = GetClothAmount(slot) }
        };
        
        armor.armorName = $"Wood {GetSlotName(slot)}";
        armor.description = "Basic wooden protection. Vulnerable to fire but cheap to craft.";
    }

    static void SetupBoneArmor(ArmorData armor, ArmorSlot slot)
    {
        armor.armorType = ArmorType.Medium;
        armor.projectileProtection = 25f;
        armor.meleeProtection = 35f;
        armor.explosiveProtection = 15f;
        armor.coldProtection = 15f;
        armor.maxDurability = 75f;
        armor.movementSpeedModifier = -10f;
        
        armor.craftingCost = new RecipeIngredient[]
        {
            new RecipeIngredient { itemData = GetItemData("AnimalBone"), amount = GetBoneAmount(slot) },
            new RecipeIngredient { itemData = GetItemData("Cloth"), amount = GetClothAmount(slot) },
            new RecipeIngredient { itemData = GetItemData("AnimalHide"), amount = 2 }
        };
        
        armor.armorName = $"Bone {GetSlotName(slot)}";
        armor.description = "Sturdy bone armor providing good melee protection.";
    }

    static void SetupLeatherArmor(ArmorData armor, ArmorSlot slot)
    {
        armor.armorType = ArmorType.Light;
        armor.projectileProtection = 20f;
        armor.meleeProtection = 30f;
        armor.explosiveProtection = 10f;
        armor.coldProtection = 25f;
        armor.radiationProtection = 5f;
        armor.maxDurability = 60f;
        armor.movementSpeedModifier = 0f; // No movement penalty
        
        armor.craftingCost = new RecipeIngredient[]
        {
            new RecipeIngredient { itemData = GetItemData("AnimalHide"), amount = GetHideAmount(slot) },
            new RecipeIngredient { itemData = GetItemData("Cloth"), amount = GetClothAmount(slot) }
        };
        
        armor.armorName = $"Leather {GetSlotName(slot)}";
        armor.description = "Flexible leather armor with no movement penalty and cold protection.";
    }

    static void SetupMetalArmor(ArmorData armor, ArmorSlot slot)
    {
        armor.armorType = ArmorType.Heavy;
        armor.projectileProtection = 45f;
        armor.meleeProtection = 50f;
        armor.explosiveProtection = 35f;
        armor.fireProtection = 30f;
        armor.radiationProtection = 15f;
        armor.maxDurability = 150f;
        armor.movementSpeedModifier = -20f;
        armor.jumpHeightModifier = -15f;
        
        armor.craftingCost = new RecipeIngredient[]
        {
            new RecipeIngredient { itemData = GetItemData("MetalOre"), amount = GetMetalAmount(slot) },
            new RecipeIngredient { itemData = GetItemData("Cloth"), amount = GetClothAmount(slot) },
            new RecipeIngredient { itemData = GetItemData("LowGradeFuel"), amount = 20 }
        };
        
        armor.armorName = $"Metal {GetSlotName(slot)}";
        armor.description = "Heavy metal armor with excellent protection but reduced mobility.";
        armor.requiredWorkbench = 2;
    }

    static void SetupHQMArmor(ArmorData armor, ArmorSlot slot)
    {
        armor.armorType = ArmorType.Heavy;
        armor.projectileProtection = 65f;
        armor.meleeProtection = 70f;
        armor.explosiveProtection = 55f;
        armor.fireProtection = 50f;
        armor.radiationProtection = 35f;
        armor.coldProtection = 30f;
        armor.maxDurability = 300f;
        armor.movementSpeedModifier = -15f; // Less penalty than metal
        armor.jumpHeightModifier = -10f;
        armor.hasGlow = true;
        
        armor.craftingCost = new RecipeIngredient[]
        {
            new RecipeIngredient { itemData = GetItemData("HighQualityMetal"), amount = GetHQMAmount(slot) },
            new RecipeIngredient { itemData = GetItemData("TechTrash"), amount = 5 },
            new RecipeIngredient { itemData = GetItemData("Cloth"), amount = GetClothAmount(slot) * 2 }
        };
        
        armor.armorName = $"HQM {GetSlotName(slot)}";
        armor.description = "Ultimate protection armor made from high quality metal. Provides excellent all-around protection.";
        armor.requiredWorkbench = 3;
    }

    static string GetSlotName(ArmorSlot slot)
    {
        switch (slot)
        {
            case ArmorSlot.Helmet: return "Helmet";
            case ArmorSlot.Chestplate: return "Chestplate";
            case ArmorSlot.Pants: return "Pants";
            case ArmorSlot.Boots: return "Boots";
            case ArmorSlot.Gloves: return "Gloves";
            default: return "Armor";
        }
    }

    static int GetWoodAmount(ArmorSlot slot)
    {
        switch (slot)
        {
            case ArmorSlot.Helmet: return 15;
            case ArmorSlot.Chestplate: return 25;
            case ArmorSlot.Pants: return 20;
            case ArmorSlot.Boots: return 10;
            case ArmorSlot.Gloves: return 8;
            default: return 15;
        }
    }

    static int GetBoneAmount(ArmorSlot slot)
    {
        switch (slot)
        {
            case ArmorSlot.Helmet: return 8;
            case ArmorSlot.Chestplate: return 15;
            case ArmorSlot.Pants: return 12;
            case ArmorSlot.Boots: return 6;
            case ArmorSlot.Gloves: return 4;
            default: return 8;
        }
    }

    static int GetHideAmount(ArmorSlot slot)
    {
        switch (slot)
        {
            case ArmorSlot.Helmet: return 3;
            case ArmorSlot.Chestplate: return 6;
            case ArmorSlot.Pants: return 5;
            case ArmorSlot.Boots: return 2;
            case ArmorSlot.Gloves: return 2;
            default: return 3;
        }
    }

    static int GetMetalAmount(ArmorSlot slot)
    {
        switch (slot)
        {
            case ArmorSlot.Helmet: return 40;
            case ArmorSlot.Chestplate: return 80;
            case ArmorSlot.Pants: return 60;
            case ArmorSlot.Boots: return 30;
            case ArmorSlot.Gloves: return 25;
            default: return 40;
        }
    }

    static int GetHQMAmount(ArmorSlot slot)
    {
        switch (slot)
        {
            case ArmorSlot.Helmet: return 8;
            case ArmorSlot.Chestplate: return 15;
            case ArmorSlot.Pants: return 12;
            case ArmorSlot.Boots: return 6;
            case ArmorSlot.Gloves: return 5;
            default: return 8;
        }
    }

    static int GetClothAmount(ArmorSlot slot)
    {
        switch (slot)
        {
            case ArmorSlot.Helmet: return 2;
            case ArmorSlot.Chestplate: return 5;
            case ArmorSlot.Pants: return 4;
            case ArmorSlot.Boots: return 2;
            case ArmorSlot.Gloves: return 1;
            default: return 2;
        }
    }

    static ItemData GetItemData(string itemName)
    {
        // This would fetch actual ItemData from a registry
        return new ItemData { itemName = itemName };
    }
}

public enum ArmorType
{
    Light,   // No movement penalty, less protection
    Medium,  // Slight movement penalty, balanced protection
    Heavy    // Movement penalty, high protection
}

public enum ArmorSlot
{
    Helmet,
    Chestplate,
    Pants,
    Boots,
    Gloves
}

public enum ArmorTier
{
    Wood,    // Basic protection, vulnerable to fire
    Bone,    // Good melee protection, moderate cost
    Leather, // Balanced protection, no movement penalty
    Metal,   // High protection, movement penalty
    HQM      // Ultimate protection, expensive
}