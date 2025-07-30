using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class CraftingManager : MonoBehaviour
{
    [Header("UI References")]
    public GameObject craftingPanel;
    public Transform categoryButtonContainer;
    public Transform recipeListContainer;
    public GameObject categoryButtonPrefab;
    public GameObject recipeButtonPrefab;
    
    [Header("Recipe Details Panel")]
    public GameObject recipeDetailsPanel;
    public Image recipeIcon;
    public Text recipeName;
    public Text recipeDescription;
    public Text ingredientsText;
    public Text craftingTimeText;
    public Button craftButton;
    public Slider craftingProgressSlider;
    public Text craftingProgressText;
    
    [Header("Category Filter")]
    public Dropdown categoryDropdown;
    public InputField searchField;
    
    [Header("Crafting Settings")]
    public List<CraftingRecipe> allRecipes = new List<CraftingRecipe>();
    public float craftingSpeedMultiplier = 1f;
    
    // State
    private bool isCraftingOpen = false;
    private CraftingCategory selectedCategory = CraftingCategory.Tools;
    private CraftingRecipe selectedRecipe;
    private List<CraftingRecipe> knownRecipes = new List<CraftingRecipe>();
    private List<CraftingRecipe> filteredRecipes = new List<CraftingRecipe>();
    
    // Crafting queue
    private Queue<CraftingJob> craftingQueue = new Queue<CraftingJob>();
    private CraftingJob currentCraftingJob;
    private bool isCrafting = false;
    
    // References
    private InventoryManager inventoryManager;
    private PlayerController playerController;
    private int playerCraftingLevel = 1;

    void Start()
    {
        InitializeCrafting();
        SetupUI();
        LoadKnownRecipes();
    }

    void Update()
    {
        if (isCrafting)
        {
            UpdateCrafting();
        }
    }

    void InitializeCrafting()
    {
        inventoryManager = GetComponent<InventoryManager>();
        playerController = GetComponent<PlayerController>();
        
        if (inventoryManager == null)
            inventoryManager = FindObjectOfType<InventoryManager>();
    }

    void SetupUI()
    {
        if (craftingPanel != null)
            craftingPanel.SetActive(false);
            
        if (recipeDetailsPanel != null)
            recipeDetailsPanel.SetActive(false);
            
        if (craftButton != null)
            craftButton.onClick.AddListener(StartCrafting);
            
        // Setup category dropdown
        if (categoryDropdown != null)
        {
            categoryDropdown.ClearOptions();
            var categoryNames = System.Enum.GetNames(typeof(CraftingCategory)).ToList();
            categoryDropdown.AddOptions(categoryNames);
            categoryDropdown.onValueChanged.AddListener(OnCategoryChanged);
        }
        
        // Setup search field
        if (searchField != null)
        {
            searchField.onValueChanged.AddListener(OnSearchChanged);
        }
        
        CreateCategoryButtons();
    }

    void LoadKnownRecipes()
    {
        // Load known recipes from save data or set defaults
        knownRecipes.Clear();
        
        // Add default unlocked recipes
        foreach (var recipe in allRecipes)
        {
            if (recipe.isUnlockedByDefault)
            {
                knownRecipes.Add(recipe);
            }
        }
        
        // Check for newly unlocked recipes based on level
        CheckUnlockedRecipes();
    }

    void CheckUnlockedRecipes()
    {
        foreach (var recipe in allRecipes)
        {
            if (!knownRecipes.Contains(recipe) && recipe.IsUnlocked(playerCraftingLevel, knownRecipes))
            {
                knownRecipes.Add(recipe);
                ShowNewRecipeNotification(recipe);
            }
        }
    }

    void CreateCategoryButtons()
    {
        if (categoryButtonContainer == null || categoryButtonPrefab == null) return;
        
        // Clear existing buttons
        foreach (Transform child in categoryButtonContainer)
        {
            Destroy(child.gameObject);
        }
        
        // Create buttons for each category
        var categories = System.Enum.GetValues(typeof(CraftingCategory));
        foreach (CraftingCategory category in categories)
        {
            GameObject buttonGO = Instantiate(categoryButtonPrefab, categoryButtonContainer);
            Button button = buttonGO.GetComponent<Button>();
            Text buttonText = buttonGO.GetComponentInChildren<Text>();
            
            if (buttonText != null)
                buttonText.text = category.ToString();
                
            if (button != null)
            {
                CraftingCategory capturedCategory = category;
                button.onClick.AddListener(() => SelectCategory(capturedCategory));
            }
        }
    }

    public void ToggleCrafting()
    {
        isCraftingOpen = !isCraftingOpen;
        
        if (craftingPanel != null)
            craftingPanel.SetActive(isCraftingOpen);
            
        if (isCraftingOpen)
        {
            RefreshRecipeList();
        }
        else
        {
            HideRecipeDetails();
        }
    }

    void SelectCategory(CraftingCategory category)
    {
        selectedCategory = category;
        RefreshRecipeList();
    }

    void OnCategoryChanged(int categoryIndex)
    {
        selectedCategory = (CraftingCategory)categoryIndex;
        RefreshRecipeList();
    }

    void OnSearchChanged(string searchText)
    {
        RefreshRecipeList();
    }

    void RefreshRecipeList()
    {
        if (recipeListContainer == null || recipeButtonPrefab == null) return;
        
        // Clear existing recipe buttons
        foreach (Transform child in recipeListContainer)
        {
            Destroy(child.gameObject);
        }
        
        // Filter recipes
        filteredRecipes = GetFilteredRecipes();
        
        // Create recipe buttons
        foreach (var recipe in filteredRecipes)
        {
            CreateRecipeButton(recipe);
        }
    }

    List<CraftingRecipe> GetFilteredRecipes()
    {
        var filtered = knownRecipes.Where(recipe => recipe.category == selectedCategory).ToList();
        
        // Apply search filter
        if (searchField != null && !string.IsNullOrEmpty(searchField.text))
        {
            string searchTerm = searchField.text.ToLower();
            filtered = filtered.Where(recipe => 
                recipe.recipeName.ToLower().Contains(searchTerm) ||
                recipe.description.ToLower().Contains(searchTerm)
            ).ToList();
        }
        
        return filtered;
    }

    void CreateRecipeButton(CraftingRecipe recipe)
    {
        GameObject buttonGO = Instantiate(recipeButtonPrefab, recipeListContainer);
        Button button = buttonGO.GetComponent<Button>();
        
        // Setup button visuals
        Image recipeIconImage = buttonGO.transform.Find("Icon")?.GetComponent<Image>();
        Text recipeNameText = buttonGO.transform.Find("Name")?.GetComponent<Text>();
        Image canCraftIndicator = buttonGO.transform.Find("CanCraftIndicator")?.GetComponent<Image>();
        
        if (recipeIconImage != null && recipe.recipeIcon != null)
            recipeIconImage.sprite = recipe.recipeIcon;
            
        if (recipeNameText != null)
            recipeNameText.text = recipe.recipeName;
            
        // Check if can craft
        bool canCraft = recipe.CanCraft(inventoryManager, playerCraftingLevel, knownRecipes);
        if (canCraftIndicator != null)
        {
            canCraftIndicator.color = canCraft ? Color.green : Color.red;
        }
        
        // Setup button click
        if (button != null)
        {
            button.onClick.AddListener(() => SelectRecipe(recipe));
            button.interactable = canCraft;
        }
    }

    void SelectRecipe(CraftingRecipe recipe)
    {
        selectedRecipe = recipe;
        ShowRecipeDetails(recipe);
    }

    void ShowRecipeDetails(CraftingRecipe recipe)
    {
        if (recipeDetailsPanel == null) return;
        
        recipeDetailsPanel.SetActive(true);
        
        if (recipeIcon != null && recipe.recipeIcon != null)
            recipeIcon.sprite = recipe.recipeIcon;
            
        if (recipeName != null)
            recipeName.text = recipe.recipeName;
            
        if (recipeDescription != null)
            recipeDescription.text = recipe.description;
            
        if (ingredientsText != null)
            ingredientsText.text = "Ingredients:\n" + recipe.GetIngredientsText();
            
        if (craftingTimeText != null)
            craftingTimeText.text = $"Crafting Time: {recipe.craftingTime:F1}s";
            
        // Update craft button
        bool canCraft = recipe.CanCraft(inventoryManager, playerCraftingLevel, knownRecipes);
        if (craftButton != null)
        {
            craftButton.interactable = canCraft && !isCrafting;
            Text buttonText = craftButton.GetComponentInChildren<Text>();
            if (buttonText != null)
            {
                if (isCrafting)
                    buttonText.text = "Crafting...";
                else if (canCraft)
                    buttonText.text = "Craft";
                else
                    buttonText.text = "Cannot Craft";
            }
        }
        
        // Hide crafting progress initially
        if (craftingProgressSlider != null)
            craftingProgressSlider.gameObject.SetActive(false);
    }

    void HideRecipeDetails()
    {
        if (recipeDetailsPanel != null)
            recipeDetailsPanel.SetActive(false);
            
        selectedRecipe = null;
    }

    void StartCrafting()
    {
        if (selectedRecipe == null || isCrafting) return;
        
        if (!selectedRecipe.CanCraft(inventoryManager, playerCraftingLevel, knownRecipes))
            return;
            
        // Add to crafting queue
        CraftingJob job = new CraftingJob
        {
            recipe = selectedRecipe,
            remainingTime = selectedRecipe.craftingTime / craftingSpeedMultiplier,
            totalTime = selectedRecipe.craftingTime / craftingSpeedMultiplier
        };
        
        craftingQueue.Enqueue(job);
        
        // Start crafting if not already crafting
        if (!isCrafting)
        {
            ProcessNextCraftingJob();
        }
    }

    void ProcessNextCraftingJob()
    {
        if (craftingQueue.Count == 0)
        {
            isCrafting = false;
            if (craftingProgressSlider != null)
                craftingProgressSlider.gameObject.SetActive(false);
            UpdateCraftButton();
            return;
        }
        
        currentCraftingJob = craftingQueue.Dequeue();
        
        // Consume ingredients
        if (!currentCraftingJob.recipe.ConsumeIngredients(inventoryManager))
        {
            Debug.LogWarning("Failed to consume ingredients for crafting");
            ProcessNextCraftingJob();
            return;
        }
        
        isCrafting = true;
        
        // Show crafting progress
        if (craftingProgressSlider != null)
        {
            craftingProgressSlider.gameObject.SetActive(true);
            craftingProgressSlider.value = 0f;
        }
        
        UpdateCraftButton();
    }

    void UpdateCrafting()
    {
        if (currentCraftingJob == null) return;
        
        currentCraftingJob.remainingTime -= Time.deltaTime;
        
        // Update progress UI
        if (craftingProgressSlider != null)
        {
            float progress = 1f - (currentCraftingJob.remainingTime / currentCraftingJob.totalTime);
            craftingProgressSlider.value = progress;
        }
        
        if (craftingProgressText != null)
        {
            craftingProgressText.text = $"Crafting... {currentCraftingJob.remainingTime:F1}s";
        }
        
        // Check if crafting is complete
        if (currentCraftingJob.remainingTime <= 0f)
        {
            CompleteCrafting();
        }
    }

    void CompleteCrafting()
    {
        if (currentCraftingJob == null) return;
        
        // Give result to player
        currentCraftingJob.recipe.GiveResult(inventoryManager);
        
        // Add experience
        AddCraftingExperience(currentCraftingJob.recipe.experienceGained);
        
        // Play completion effect
        PlayCraftingCompleteEffect();
        
        // Process next job
        ProcessNextCraftingJob();
        
        // Refresh UI
        RefreshRecipeList();
        if (selectedRecipe != null)
        {
            ShowRecipeDetails(selectedRecipe);
        }
    }

    void UpdateCraftButton()
    {
        if (craftButton == null) return;
        
        Text buttonText = craftButton.GetComponentInChildren<Text>();
        if (buttonText != null)
        {
            if (isCrafting)
                buttonText.text = $"Crafting... ({craftingQueue.Count + 1})";
            else if (selectedRecipe != null && selectedRecipe.CanCraft(inventoryManager, playerCraftingLevel, knownRecipes))
                buttonText.text = "Craft";
            else
                buttonText.text = "Cannot Craft";
        }
        
        craftButton.interactable = selectedRecipe != null && 
                                  selectedRecipe.CanCraft(inventoryManager, playerCraftingLevel, knownRecipes);
    }

    void AddCraftingExperience(int amount)
    {
        // This would integrate with a level system
        Debug.Log($"Gained {amount} crafting experience");
        
        // Check for level up and new recipes
        CheckUnlockedRecipes();
    }

    void PlayCraftingCompleteEffect()
    {
        // Play sound effect
        // Show visual effect
        Debug.Log("Crafting completed!");
    }

    void ShowNewRecipeNotification(CraftingRecipe recipe)
    {
        // Show notification that a new recipe was unlocked
        Debug.Log($"New recipe unlocked: {recipe.recipeName}");
    }

    public bool IsRecipeKnown(CraftingRecipe recipe)
    {
        return knownRecipes.Contains(recipe);
    }

    public void LearnRecipe(CraftingRecipe recipe)
    {
        if (!knownRecipes.Contains(recipe))
        {
            knownRecipes.Add(recipe);
            ShowNewRecipeNotification(recipe);
            
            if (isCraftingOpen)
            {
                RefreshRecipeList();
            }
        }
    }
}

[System.Serializable]
public class CraftingJob
{
    public CraftingRecipe recipe;
    public float remainingTime;
    public float totalTime;
}