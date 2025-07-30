using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "New Recipe", menuName = "Survival Game/Crafting Recipe")]
public class CraftingRecipe : ScriptableObject
{
    [Header("Recipe Info")]
    public string recipeName;
    public string description;
    public Sprite recipeIcon;
    public CraftingCategory category;
    
    [Header("Result")]
    public ItemData resultItem;
    public int resultAmount = 1;
    
    [Header("Requirements")]
    public List<RecipeIngredient> ingredients = new List<RecipeIngredient>();
    public float craftingTime = 3f;
    
    [Header("Unlock Requirements")]
    public int requiredLevel = 1;
    public List<CraftingRecipe> prerequisiteRecipes = new List<CraftingRecipe>();
    public bool isUnlockedByDefault = false;
    
    [Header("Crafting Station")]
    public CraftingStationType requiredStation = CraftingStationType.None;
    
    [Header("Experience")]
    public int experienceGained = 10;

    public bool CanCraft(InventoryManager inventory, int playerLevel, List<CraftingRecipe> knownRecipes)
    {
        // Check if recipe is unlocked
        if (!IsUnlocked(playerLevel, knownRecipes))
            return false;
            
        // Check if all ingredients are available
        foreach (var ingredient in ingredients)
        {
            if (!inventory.HasItem(ingredient.itemData, ingredient.amount))
                return false;
        }
        
        return true;
    }

    public bool IsUnlocked(int playerLevel, List<CraftingRecipe> knownRecipes)
    {
        if (isUnlockedByDefault)
            return true;
            
        if (playerLevel < requiredLevel)
            return false;
            
        // Check prerequisite recipes
        foreach (var prerequisite in prerequisiteRecipes)
        {
            if (!knownRecipes.Contains(prerequisite))
                return false;
        }
        
        return true;
    }

    public bool ConsumeIngredients(InventoryManager inventory)
    {
        // Check if we can consume all ingredients
        if (!CanCraft(inventory, 1, new List<CraftingRecipe>()))
            return false;
            
        // Consume ingredients
        foreach (var ingredient in ingredients)
        {
            if (!inventory.RemoveItem(ingredient.itemData, ingredient.amount))
                return false; // This shouldn't happen if CanCraft returned true
        }
        
        return true;
    }

    public void GiveResult(InventoryManager inventory)
    {
        inventory.AddItem(resultItem, resultAmount);
    }

    public string GetIngredientsText()
    {
        var parts = new List<string>();
        foreach (var ingredient in ingredients)
        {
            parts.Add($"{ingredient.amount}x {ingredient.itemData.GetDisplayName()}");
        }
        return string.Join("\n", parts);
    }
}

[System.Serializable]
public class RecipeIngredient
{
    public ItemData itemData;
    public int amount = 1;
}

public enum CraftingCategory
{
    Tools,
    Weapons,
    Building,
    Consumables,
    Clothing,
    Misc
}

public enum CraftingStationType
{
    None,           // Can craft anywhere
    Workbench,      // Requires workbench
    Forge,          // Requires forge
    Campfire,       // Requires campfire
    ChemistryTable  // Requires chemistry table
}