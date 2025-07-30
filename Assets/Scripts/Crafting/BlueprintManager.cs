using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class BlueprintManager : MonoBehaviour
{
    [Header("Blueprint Configuration")]
    public List<CraftingRecipe> defaultBlueprints = new List<CraftingRecipe>();
    public List<CraftingRecipe> allRecipes = new List<CraftingRecipe>();
    
    [Header("Blueprint Categories")]
    public bool learnBasicToolsAtStart = true;
    public bool learnPrimitivesAtStart = true;
    public bool learnBasicBuildingAtStart = true;
    
    // Player blueprint data
    private Dictionary<string, PlayerBlueprintData> playerBlueprints = new Dictionary<string, PlayerBlueprintData>();
    
    // Events
    public System.Action<PlayerController, ItemData> OnBlueprintLearned;
    public System.Action<PlayerController, List<CraftingRecipe>> OnBlueprintsUpdated;

    void Start()
    {
        InitializeBlueprintSystem();
        LoadAllRecipes();
        SetupDefaultBlueprints();
    }

    void InitializeBlueprintSystem()
    {
        // Initialize blueprint system
        Debug.Log("Blueprint Manager initialized");
    }

    void LoadAllRecipes()
    {
        // Load all crafting recipes from resources
        CraftingRecipe[] recipes = Resources.LoadAll<CraftingRecipe>("Recipes");
        allRecipes.AddRange(recipes);
        
        Debug.Log($"Loaded {allRecipes.Count} total recipes");
    }

    void SetupDefaultBlueprints()
    {
        defaultBlueprints.Clear();
        
        // Add basic starting blueprints
        if (learnBasicToolsAtStart)
        {
            AddDefaultBlueprints(CraftingCategory.Tools, 1); // Basic tools only
        }
        
        if (learnPrimitivesAtStart)
        {
            AddDefaultBlueprints(CraftingCategory.Consumables, 1); // Basic consumables
        }
        
        if (learnBasicBuildingAtStart)
        {
            AddDefaultBlueprints(CraftingCategory.Structure, 1); // Basic building
        }
        
        Debug.Log($"Setup {defaultBlueprints.Count} default blueprints");
    }

    void AddDefaultBlueprints(CraftingCategory category, int maxTier)
    {
        var categoryRecipes = allRecipes.Where(r => 
            r.category == category && 
            r.requiredWorkbench <= maxTier
        ).ToList();
        
        defaultBlueprints.AddRange(categoryRecipes);
    }

    public void InitializePlayerBlueprints(PlayerController player)
    {
        string playerID = GetPlayerID(player);
        
        if (playerBlueprints.ContainsKey(playerID))
            return;
            
        PlayerBlueprintData blueprintData = new PlayerBlueprintData
        {
            playerID = playerID,
            learnedBlueprints = new List<string>(),
            researchProgress = new Dictionary<string, float>()
        };
        
        // Add default blueprints
        foreach (var recipe in defaultBlueprints)
        {
            blueprintData.learnedBlueprints.Add(recipe.recipeName);
        }
        
        playerBlueprints[playerID] = blueprintData;
        
        OnBlueprintsUpdated?.Invoke(player, GetKnownRecipes(player));
        
        Debug.Log($"Initialized blueprints for player {playerID} with {blueprintData.learnedBlueprints.Count} default recipes");
    }

    public bool HasBlueprint(PlayerController player, CraftingRecipe recipe)
    {
        if (player == null || recipe == null) return false;
        
        string playerID = GetPlayerID(player);
        
        if (!playerBlueprints.ContainsKey(playerID))
        {
            InitializePlayerBlueprints(player);
        }
        
        return playerBlueprints[playerID].learnedBlueprints.Contains(recipe.recipeName);
    }

    public bool HasBlueprint(PlayerController player, string recipeName)
    {
        if (player == null || string.IsNullOrEmpty(recipeName)) return false;
        
        string playerID = GetPlayerID(player);
        
        if (!playerBlueprints.ContainsKey(playerID))
        {
            InitializePlayerBlueprints(player);
        }
        
        return playerBlueprints[playerID].learnedBlueprints.Contains(recipeName);
    }

    public void LearnBlueprint(PlayerController player, ItemData item)
    {
        if (player == null || item == null) return;
        
        // Find recipe that creates this item
        CraftingRecipe recipe = FindRecipeForItem(item);
        if (recipe == null)
        {
            Debug.LogWarning($"No recipe found for item: {item.itemName}");
            return;
        }
        
        LearnBlueprint(player, recipe);
    }

    public void LearnBlueprint(PlayerController player, CraftingRecipe recipe)
    {
        if (player == null || recipe == null) return;
        
        string playerID = GetPlayerID(player);
        
        if (!playerBlueprints.ContainsKey(playerID))
        {
            InitializePlayerBlueprints(player);
        }
        
        PlayerBlueprintData blueprintData = playerBlueprints[playerID];
        
        if (blueprintData.learnedBlueprints.Contains(recipe.recipeName))
        {
            Debug.Log($"Player {playerID} already knows {recipe.recipeName}");
            return;
        }
        
        blueprintData.learnedBlueprints.Add(recipe.recipeName);
        
        // Trigger events
        OnBlueprintLearned?.Invoke(player, recipe.resultItem);
        OnBlueprintsUpdated?.Invoke(player, GetKnownRecipes(player));
        
        // Show notification to player
        ShowBlueprintLearnedNotification(player, recipe);
        
        Debug.Log($"Player {playerID} learned blueprint: {recipe.recipeName}");
        
        // Save blueprints
        SavePlayerBlueprints(player);
    }

    public List<CraftingRecipe> GetKnownRecipes(PlayerController player)
    {
        if (player == null) return new List<CraftingRecipe>();
        
        string playerID = GetPlayerID(player);
        
        if (!playerBlueprints.ContainsKey(playerID))
        {
            InitializePlayerBlueprints(player);
        }
        
        PlayerBlueprintData blueprintData = playerBlueprints[playerID];
        List<CraftingRecipe> knownRecipes = new List<CraftingRecipe>();
        
        foreach (string recipeName in blueprintData.learnedBlueprints)
        {
            CraftingRecipe recipe = allRecipes.FirstOrDefault(r => r.recipeName == recipeName);
            if (recipe != null)
            {
                knownRecipes.Add(recipe);
            }
        }
        
        return knownRecipes;
    }

    public List<CraftingRecipe> GetKnownRecipesByCategory(PlayerController player, CraftingCategory category)
    {
        return GetKnownRecipes(player).Where(r => r.category == category).ToList();
    }

    public List<CraftingRecipe> GetAvailableRecipesForWorkbench(PlayerController player, WorkbenchController workbench)
    {
        if (workbench == null) return new List<CraftingRecipe>();
        
        WorkbenchData workbenchData = workbench.GetWorkbenchData();
        List<CraftingRecipe> knownRecipes = GetKnownRecipes(player);
        
        return knownRecipes.Where(r => workbenchData.CanCraftRecipe(r)).ToList();
    }

    public List<ItemData> GetResearchableItems(PlayerController player)
    {
        List<ItemData> researchableItems = new List<ItemData>();
        List<CraftingRecipe> unknownRecipes = GetUnknownRecipes(player);
        
        foreach (var recipe in unknownRecipes)
        {
            if (recipe.resultItem != null && CanResearchItem(recipe.resultItem))
            {
                researchableItems.Add(recipe.resultItem);
            }
        }
        
        return researchableItems.Distinct().ToList();
    }

    public List<CraftingRecipe> GetUnknownRecipes(PlayerController player)
    {
        List<CraftingRecipe> knownRecipes = GetKnownRecipes(player);
        List<string> knownNames = knownRecipes.Select(r => r.recipeName).ToList();
        
        return allRecipes.Where(r => !knownNames.Contains(r.recipeName)).ToList();
    }

    bool CanResearchItem(ItemData item)
    {
        // Check if item type can be researched
        return item.itemType == ItemType.Tool ||
               item.itemType == ItemType.Weapon ||
               item.itemType == ItemType.Armor ||
               item.itemType == ItemType.Component ||
               item.itemType == ItemType.Deployable ||
               item.itemType == ItemType.Medical ||
               item.itemType == ItemType.Explosive;
    }

    CraftingRecipe FindRecipeForItem(ItemData item)
    {
        return allRecipes.FirstOrDefault(r => r.resultItem != null && r.resultItem.itemName == item.itemName);
    }

    void ShowBlueprintLearnedNotification(PlayerController player, CraftingRecipe recipe)
    {
        // Show UI notification that player learned a new blueprint
        string message = $"BLUEPRINT LEARNED: {recipe.resultItem.itemName}";
        
        // This would show a proper UI notification
        Debug.Log($"[BLUEPRINT] {message}");
        
        // Update any open crafting UIs
        CraftingManager craftingManager = FindObjectOfType<CraftingManager>();
        if (craftingManager != null)
        {
            craftingManager.RefreshAvailableRecipes();
        }
    }

    // Research progress tracking
    public void SetResearchProgress(PlayerController player, ItemData item, float progress)
    {
        string playerID = GetPlayerID(player);
        
        if (!playerBlueprints.ContainsKey(playerID))
        {
            InitializePlayerBlueprints(player);
        }
        
        playerBlueprints[playerID].researchProgress[item.itemName] = progress;
    }

    public float GetResearchProgress(PlayerController player, ItemData item)
    {
        string playerID = GetPlayerID(player);
        
        if (!playerBlueprints.ContainsKey(playerID))
        {
            InitializePlayerBlueprints(player);
        }
        
        if (playerBlueprints[playerID].researchProgress.ContainsKey(item.itemName))
        {
            return playerBlueprints[playerID].researchProgress[item.itemName];
        }
        
        return 0f;
    }

    // Blueprint trading/sharing (for future team features)
    public bool CanShareBlueprint(PlayerController fromPlayer, PlayerController toPlayer, CraftingRecipe recipe)
    {
        // Check if blueprint sharing is allowed
        return HasBlueprint(fromPlayer, recipe) && !HasBlueprint(toPlayer, recipe);
    }

    public void ShareBlueprint(PlayerController fromPlayer, PlayerController toPlayer, CraftingRecipe recipe)
    {
        if (!CanShareBlueprint(fromPlayer, toPlayer, recipe)) return;
        
        LearnBlueprint(toPlayer, recipe);
        
        Debug.Log($"Player {GetPlayerID(fromPlayer)} shared blueprint {recipe.recipeName} with {GetPlayerID(toPlayer)}");
    }

    // Admin functions
    public void LearnAllBlueprints(PlayerController player)
    {
        string playerID = GetPlayerID(player);
        
        if (!playerBlueprints.ContainsKey(playerID))
        {
            InitializePlayerBlueprints(player);
        }
        
        PlayerBlueprintData blueprintData = playerBlueprints[playerID];
        blueprintData.learnedBlueprints.Clear();
        
        foreach (var recipe in allRecipes)
        {
            blueprintData.learnedBlueprints.Add(recipe.recipeName);
        }
        
        OnBlueprintsUpdated?.Invoke(player, GetKnownRecipes(player));
        
        Debug.Log($"Player {playerID} learned all {allRecipes.Count} blueprints");
    }

    public void ResetBlueprints(PlayerController player)
    {
        string playerID = GetPlayerID(player);
        
        if (playerBlueprints.ContainsKey(playerID))
        {
            playerBlueprints.Remove(playerID);
        }
        
        InitializePlayerBlueprints(player);
        
        Debug.Log($"Reset blueprints for player {playerID}");
    }

    // Save/Load system
    public void SavePlayerBlueprints(PlayerController player)
    {
        string playerID = GetPlayerID(player);
        
        if (!playerBlueprints.ContainsKey(playerID)) return;
        
        PlayerBlueprintData data = playerBlueprints[playerID];
        string json = JsonUtility.ToJson(data, true);
        
        // Save to PlayerPrefs for now (would use proper save system in production)
        PlayerPrefs.SetString($"Blueprints_{playerID}", json);
        PlayerPrefs.Save();
    }

    public void LoadPlayerBlueprints(PlayerController player)
    {
        string playerID = GetPlayerID(player);
        string json = PlayerPrefs.GetString($"Blueprints_{playerID}", "");
        
        if (string.IsNullOrEmpty(json))
        {
            InitializePlayerBlueprints(player);
            return;
        }
        
        try
        {
            PlayerBlueprintData data = JsonUtility.FromJson<PlayerBlueprintData>(json);
            playerBlueprints[playerID] = data;
            
            OnBlueprintsUpdated?.Invoke(player, GetKnownRecipes(player));
            
            Debug.Log($"Loaded {data.learnedBlueprints.Count} blueprints for player {playerID}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to load blueprints for player {playerID}: {e.Message}");
            InitializePlayerBlueprints(player);
        }
    }

    // Utility methods
    string GetPlayerID(PlayerController player)
    {
        // In a real multiplayer game, this would return the actual player ID
        return player != null ? player.gameObject.GetInstanceID().ToString() : "unknown";
    }

    // Statistics and progress tracking
    public BlueprintStats GetBlueprintStats(PlayerController player)
    {
        List<CraftingRecipe> knownRecipes = GetKnownRecipes(player);
        
        return new BlueprintStats
        {
            totalRecipes = allRecipes.Count,
            learnedRecipes = knownRecipes.Count,
            progressPercentage = (float)knownRecipes.Count / allRecipes.Count * 100f,
            categoriesLearned = knownRecipes.Select(r => r.category).Distinct().Count(),
            totalCategories = System.Enum.GetValues(typeof(CraftingCategory)).Length
        };
    }

    // Public getters
    public int GetTotalRecipeCount() => allRecipes.Count;
    public int GetLearnedRecipeCount(PlayerController player) => GetKnownRecipes(player).Count;
    public float GetLearningProgress(PlayerController player) => (float)GetLearnedRecipeCount(player) / GetTotalRecipeCount() * 100f;
}

[System.Serializable]
public class PlayerBlueprintData
{
    public string playerID;
    public List<string> learnedBlueprints = new List<string>();
    public Dictionary<string, float> researchProgress = new Dictionary<string, float>();
}

[System.Serializable]
public class BlueprintStats
{
    public int totalRecipes;
    public int learnedRecipes;
    public float progressPercentage;
    public int categoriesLearned;
    public int totalCategories;
}