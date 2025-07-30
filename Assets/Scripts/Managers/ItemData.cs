using UnityEngine;

[System.Serializable]
public class ItemData
{
    [Header("Basic Info")]
    public string itemName;
    public string description;
    public Sprite icon;
    public ItemType itemType;
    
    [Header("Stack Settings")]
    public int maxStackSize = 100;
    
    [Header("Tool Properties")]
    public ToolType toolType = ToolType.None;
    public float toolEfficiency = 1f;
    public float toolDurability = 100f;
    
    [Header("Consumable Properties")]
    public bool isConsumable = false;
    public float healthRestore = 0f;
    public float hungerRestore = 0f;
    public float thirstRestore = 0f;
    
    [Header("Resource Properties")]
    public ResourceData resourceData;
    
    [Header("Weapon Properties")]
    public float damage = 0f;
    public float attackSpeed = 1f;
    public float range = 1f;

    public string GetDisplayName()
    {
        if (!string.IsNullOrEmpty(itemName))
            return itemName;
        if (resourceData != null)
            return resourceData.resourceName;
        return "Unknown Item";
    }

    public string GetDescription()
    {
        if (!string.IsNullOrEmpty(description))
            return description;
        if (resourceData != null)
            return resourceData.description;
        return "";
    }

    public int GetMaxStackSize()
    {
        if (resourceData != null)
            return resourceData.maxStackSize;
        return maxStackSize;
    }

    public bool CanStackWith(ItemData other)
    {
        if (other == null) return false;
        
        // Resource items can stack if they're the same resource
        if (resourceData != null && other.resourceData != null)
            return resourceData == other.resourceData;
            
        // Non-resource items can stack if they're identical
        return itemName == other.itemName && itemType == other.itemType;
    }

    public bool IsUsable()
    {
        return isConsumable || toolType != ToolType.None;
    }

    public bool UseItem(PlayerController player)
    {
        if (!IsUsable()) return false;
        
        if (isConsumable)
        {
            return ConsumeItem(player);
        }
        
        if (toolType != ToolType.None)
        {
            return EquipTool(player);
        }
        
        return false;
    }

    bool ConsumeItem(PlayerController player)
    {
        SurvivalManager survival = player.GetComponent<SurvivalManager>();
        if (survival != null)
        {
            survival.RestoreHealth(healthRestore);
            survival.RestoreHunger(hungerRestore);
            survival.RestoreThirst(thirstRestore);
            return true;
        }
        return false;
    }

    bool EquipTool(PlayerController player)
    {
        // This would equip the tool for the player
        // For now, just return true
        Debug.Log($"Equipped {GetDisplayName()}");
        return false; // Don't consume the tool
    }

    public bool Equals(ItemData other)
    {
        if (other == null) return false;
        return CanStackWith(other);
    }
}

[System.Serializable]
public class ItemStack
{
    public ItemData itemData;
    public int amount;

    public ItemStack()
    {
        itemData = null;
        amount = 0;
    }

    public ItemStack(ItemData data, int count)
    {
        itemData = data;
        amount = count;
    }

    public bool IsEmpty()
    {
        return itemData == null || amount <= 0;
    }

    public int GetStackSpace()
    {
        if (IsEmpty()) return 0;
        return itemData.GetMaxStackSize() - amount;
    }

    public bool CanAddAmount(int addAmount)
    {
        return GetStackSpace() >= addAmount;
    }
}

public enum ItemType
{
    Resource,
    Tool,
    Weapon,
    Consumable,
    Building,
    Misc
}