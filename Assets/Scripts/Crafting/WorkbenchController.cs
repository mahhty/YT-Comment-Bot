using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class WorkbenchController : MonoBehaviour, IInteractable
{
    [Header("Workbench Configuration")]
    public WorkbenchData workbenchData;
    public bool isPlayerOwned = true;
    public string ownerID = "";
    
    [Header("UI References")]
    public GameObject workbenchUI;
    public Text workbenchNameText;
    public Text workbenchDescriptionText;
    public Button[] craftingSlots;
    public Transform craftingQueueParent;
    public Button repairButton;
    public Button recycleButton;
    public Button researchButton;
    
    [Header("Research UI")]
    public GameObject researchPanel;
    public RawImage researchItemIcon;
    public Text researchItemName;
    public Text researchCostText;
    public Text researchTimeText;
    public Slider researchProgressSlider;
    public Button startResearchButton;
    public Button cancelResearchButton;
    
    [Header("Repair UI")]
    public GameObject repairPanel;
    public Transform repairSlots;
    public Text repairCostText;
    public Button startRepairButton;
    
    [Header("Status Display")]
    public Slider healthSlider;
    public Text healthText;
    public GameObject authorizationIndicator;
    public Text upkeepStatusText;
    
    [Header("Visual Effects")]
    public ParticleSystem workingEffect;
    public AudioSource audioSource;
    public Renderer workbenchRenderer;
    public Light workbenchLight;
    
    // Internal state
    private float currentHealth;
    private List<CraftingJob> activeCraftingJobs = new List<CraftingJob>();
    private ResearchJob currentResearchJob;
    private PlayerController currentUser;
    private InventoryManager playerInventory;
    private bool isInitialized = false;
    private ToolCupboard authorizedCupboard;

    // Blueprint system
    private BlueprintManager blueprintManager;

    void Start()
    {
        InitializeWorkbench();
    }

    void Update()
    {
        if (!isInitialized) return;
        
        UpdateCraftingJobs();
        UpdateResearchJob();
        UpdateHealthDisplay();
        UpdateVisualEffects();
        CheckDecay();
    }

    void InitializeWorkbench()
    {
        if (workbenchData == null)
        {
            Debug.LogError("Workbench missing WorkbenchData!");
            return;
        }
        
        currentHealth = workbenchData.maxHealth;
        blueprintManager = FindObjectOfType<BlueprintManager>();
        
        // Setup UI
        if (workbenchUI != null)
            workbenchUI.SetActive(false);
            
        // Initialize crafting slots
        for (int i = 0; i < craftingSlots.Length; i++)
        {
            if (i < workbenchData.maxCraftingSlots)
            {
                craftingSlots[i].gameObject.SetActive(true);
                int slotIndex = i;
                craftingSlots[i].onClick.AddListener(() => OpenCraftingSlot(slotIndex));
            }
            else
            {
                craftingSlots[i].gameObject.SetActive(false);
            }
        }
        
        // Setup feature buttons
        if (repairButton != null)
        {
            repairButton.gameObject.SetActive(workbenchData.canRepairItems);
            repairButton.onClick.AddListener(OpenRepairPanel);
        }
        
        if (recycleButton != null)
        {
            recycleButton.gameObject.SetActive(workbenchData.canRecycleItems);
            recycleButton.onClick.AddListener(OpenRecyclePanel);
        }
        
        if (researchButton != null)
        {
            researchButton.gameObject.SetActive(workbenchData.canResearchBlueprints);
            researchButton.onClick.AddListener(OpenResearchPanel);
        }
        
        // Setup research UI
        if (startResearchButton != null)
            startResearchButton.onClick.AddListener(StartResearch);
            
        if (cancelResearchButton != null)
            cancelResearchButton.onClick.AddListener(CancelResearch);
            
        // Setup repair UI
        if (startRepairButton != null)
            startRepairButton.onClick.AddListener(StartRepair);
        
        // Check for authorization
        CheckAuthorization();
        
        isInitialized = true;
        
        Debug.Log($"Initialized {workbenchData.workbenchName}");
    }

    void CheckAuthorization()
    {
        if (!workbenchData.requiresAuthorization)
        {
            if (authorizationIndicator != null)
                authorizationIndicator.SetActive(false);
            return;
        }
        
        // Find nearby tool cupboard
        ToolCupboard[] cupboards = FindObjectsOfType<ToolCupboard>();
        authorizedCupboard = null;
        
        foreach (var cupboard in cupboards)
        {
            if (cupboard.IsInRange(transform.position))
            {
                authorizedCupboard = cupboard;
                break;
            }
        }
        
        if (authorizationIndicator != null)
        {
            authorizationIndicator.SetActive(authorizedCupboard != null);
        }
    }

    bool IsPlayerAuthorized(PlayerController player)
    {
        if (!workbenchData.requiresAuthorization) return true;
        if (authorizedCupboard == null) return isPlayerOwned;
        
        return authorizedCupboard.IsPlayerAuthorized(player);
    }

    // IInteractable implementation
    public bool CanInteract(PlayerController player)
    {
        if (player == null) return false;
        if (currentHealth <= 0) return false;
        
        float distance = Vector3.Distance(transform.position, player.transform.position);
        return distance <= workbenchData.interactionRange;
    }

    public string GetInteractionText(PlayerController player)
    {
        if (!IsPlayerAuthorized(player))
            return "No Building Privilege";
            
        return $"Use {workbenchData.workbenchName}";
    }

    public void Interact(PlayerController player)
    {
        if (!CanInteract(player)) return;
        if (!IsPlayerAuthorized(player)) return;
        
        currentUser = player;
        playerInventory = player.GetComponent<InventoryManager>();
        
        OpenWorkbenchUI();
    }

    void OpenWorkbenchUI()
    {
        if (workbenchUI == null) return;
        
        workbenchUI.SetActive(true);
        
        // Update UI text
        if (workbenchNameText != null)
            workbenchNameText.text = workbenchData.workbenchName;
            
        if (workbenchDescriptionText != null)
            workbenchDescriptionText.text = workbenchData.description;
        
        // Apply Rust styling
        RustUIUpdater updater = workbenchUI.GetComponent<RustUIUpdater>();
        if (updater == null)
        {
            updater = workbenchUI.AddComponent<RustUIUpdater>();
            updater.updateRecursively = true;
        }
        updater.UpdateRustStyling();
        
        UpdateCraftingUI();
        UpdateFeatureButtons();
    }

    public void CloseWorkbenchUI()
    {
        if (workbenchUI != null)
            workbenchUI.SetActive(false);
            
        currentUser = null;
        playerInventory = null;
    }

    void OpenCraftingSlot(int slotIndex)
    {
        if (slotIndex >= workbenchData.maxCraftingSlots) return;
        
        // Open crafting interface for this slot
        CraftingManager craftingManager = FindObjectOfType<CraftingManager>();
        if (craftingManager != null)
        {
            craftingManager.OpenCraftingForWorkbench(this, slotIndex);
        }
    }

    void OpenRepairPanel()
    {
        if (!workbenchData.canRepairItems) return;
        
        if (repairPanel != null)
            repairPanel.SetActive(true);
            
        UpdateRepairUI();
    }

    void OpenRecyclePanel()
    {
        if (!workbenchData.canRecycleItems) return;
        
        // Open recycling interface
        RecyclerManager recycler = FindObjectOfType<RecyclerManager>();
        if (recycler != null)
        {
            recycler.OpenRecyclerFromWorkbench(this);
        }
    }

    void OpenResearchPanel()
    {
        if (!workbenchData.canResearchBlueprints) return;
        
        if (researchPanel != null)
            researchPanel.SetActive(true);
            
        UpdateResearchUI();
    }

    public void StartCraftingJob(CraftingRecipe recipe, int amount = 1)
    {
        if (activeCraftingJobs.Count >= workbenchData.maxCraftingSlots) return;
        if (!workbenchData.CanCraftRecipe(recipe)) return;
        if (playerInventory == null) return;
        
        // Check if player has required materials
        if (!playerInventory.HasRequiredItems(recipe.ingredients, amount))
        {
            ShowMessage("Insufficient materials!");
            return;
        }
        
        // Consume materials
        foreach (var ingredient in recipe.ingredients)
        {
            playerInventory.RemoveItem(ingredient.itemData, ingredient.amount * amount);
        }
        
        // Create crafting job
        CraftingJob job = new CraftingJob
        {
            recipe = recipe,
            amount = amount,
            remainingTime = recipe.craftingTime / workbenchData.craftingSpeedMultiplier,
            totalTime = recipe.craftingTime / workbenchData.craftingSpeedMultiplier,
            slotIndex = activeCraftingJobs.Count
        };
        
        activeCraftingJobs.Add(job);
        
        PlaySound(workbenchData.craftingSound);
        ShowMessage($"Started crafting {recipe.resultItem.itemName} x{amount}");
        
        UpdateCraftingUI();
    }

    void UpdateCraftingJobs()
    {
        for (int i = activeCraftingJobs.Count - 1; i >= 0; i--)
        {
            CraftingJob job = activeCraftingJobs[i];
            job.remainingTime -= Time.deltaTime;
            
            if (job.remainingTime <= 0)
            {
                CompleteCraftingJob(job);
                activeCraftingJobs.RemoveAt(i);
            }
        }
    }

    void CompleteCraftingJob(CraftingJob job)
    {
        // Give result to player or drop it
        if (playerInventory != null)
        {
            bool added = playerInventory.AddItem(job.recipe.resultItem, job.recipe.resultAmount * job.amount);
            if (!added)
            {
                // Drop items near workbench if inventory full
                DropItem(job.recipe.resultItem, job.recipe.resultAmount * job.amount);
            }
        }
        else
        {
            DropItem(job.recipe.resultItem, job.recipe.resultAmount * job.amount);
        }
        
        ShowMessage($"Crafted {job.recipe.resultItem.itemName} x{job.recipe.resultAmount * job.amount}");
        UpdateCraftingUI();
    }

    void StartResearch()
    {
        if (currentResearchJob != null) return;
        if (!workbenchData.canResearchBlueprints) return;
        
        // Get item to research from UI
        ItemData itemToResearch = GetSelectedResearchItem();
        if (itemToResearch == null) return;
        
        if (!workbenchData.CanResearchItem(itemToResearch)) return;
        
        int scrapCost = workbenchData.GetResearchCost(itemToResearch);
        
        // Check if player has the item and scrap
        if (!playerInventory.HasItem(itemToResearch, 1))
        {
            ShowMessage("You need the item to research it!");
            return;
        }
        
        ItemData scrapItem = GetScrapItemData();
        if (!playerInventory.HasItem(scrapItem, scrapCost))
        {
            ShowMessage($"Need {scrapCost} scrap to research this item!");
            return;
        }
        
        // Consume item and scrap
        playerInventory.RemoveItem(itemToResearch, 1);
        playerInventory.RemoveItem(scrapItem, scrapCost);
        
        // Start research job
        currentResearchJob = new ResearchJob
        {
            itemToResearch = itemToResearch,
            remainingTime = workbenchData.GetResearchTime(itemToResearch),
            totalTime = workbenchData.GetResearchTime(itemToResearch)
        };
        
        PlaySound(workbenchData.researchSound);
        ShowMessage($"Researching {itemToResearch.itemName}...");
        
        UpdateResearchUI();
    }

    void UpdateResearchJob()
    {
        if (currentResearchJob == null) return;
        
        currentResearchJob.remainingTime -= Time.deltaTime;
        
        if (researchProgressSlider != null)
        {
            float progress = 1f - (currentResearchJob.remainingTime / currentResearchJob.totalTime);
            researchProgressSlider.value = progress;
        }
        
        if (currentResearchJob.remainingTime <= 0)
        {
            CompleteResearch();
        }
    }

    void CompleteResearch()
    {
        if (currentResearchJob == null) return;
        
        // Learn the blueprint
        if (blueprintManager != null)
        {
            blueprintManager.LearnBlueprint(currentUser, currentResearchJob.itemToResearch);
        }
        
        ShowMessage($"Learned blueprint: {currentResearchJob.itemToResearch.itemName}");
        
        currentResearchJob = null;
        UpdateResearchUI();
    }

    void CancelResearch()
    {
        if (currentResearchJob == null) return;
        
        // Return 50% of scrap cost
        ItemData scrapItem = GetScrapItemData();
        int refund = workbenchData.GetResearchCost(currentResearchJob.itemToResearch) / 2;
        
        if (playerInventory != null)
        {
            playerInventory.AddItem(scrapItem, refund);
        }
        
        currentResearchJob = null;
        ShowMessage($"Research cancelled. Refunded {refund} scrap.");
        
        UpdateResearchUI();
    }

    void StartRepair()
    {
        if (!workbenchData.canRepairItems) return;
        
        // Implementation for item repair
        ShowMessage("Repair system not yet implemented");
    }

    void UpdateCraftingUI()
    {
        // Update crafting slot displays
        for (int i = 0; i < craftingSlots.Length; i++)
        {
            if (i < activeCraftingJobs.Count)
            {
                CraftingJob job = activeCraftingJobs[i];
                // Update progress display
                UpdateCraftingSlotUI(i, job);
            }
            else
            {
                // Clear slot display
                ClearCraftingSlotUI(i);
            }
        }
    }

    void UpdateCraftingSlotUI(int slotIndex, CraftingJob job)
    {
        // Update the UI for this crafting slot
        if (slotIndex >= craftingSlots.Length) return;
        
        Button slot = craftingSlots[slotIndex];
        // This would update progress bar, item icon, etc.
    }

    void ClearCraftingSlotUI(int slotIndex)
    {
        // Clear the UI for this crafting slot
        if (slotIndex >= craftingSlots.Length) return;
        
        Button slot = craftingSlots[slotIndex];
        // This would clear progress bar, item icon, etc.
    }

    void UpdateResearchUI()
    {
        if (researchPanel == null) return;
        
        bool hasActiveResearch = currentResearchJob != null;
        
        if (startResearchButton != null)
            startResearchButton.interactable = !hasActiveResearch;
            
        if (cancelResearchButton != null)
            cancelResearchButton.gameObject.SetActive(hasActiveResearch);
            
        if (researchProgressSlider != null)
            researchProgressSlider.gameObject.SetActive(hasActiveResearch);
        
        if (hasActiveResearch)
        {
            if (researchItemName != null)
                researchItemName.text = currentResearchJob.itemToResearch.itemName;
                
            if (researchTimeText != null)
                researchTimeText.text = RustFontManager.FormatRustTime(currentResearchJob.remainingTime);
        }
    }

    void UpdateRepairUI()
    {
        // Update repair interface
    }

    void UpdateFeatureButtons()
    {
        // Update button states based on workbench capabilities and authorization
        bool authorized = currentUser != null && IsPlayerAuthorized(currentUser);
        
        if (repairButton != null)
            repairButton.interactable = authorized && workbenchData.canRepairItems;
            
        if (recycleButton != null)
            recycleButton.interactable = authorized && workbenchData.canRecycleItems;
            
        if (researchButton != null)
            researchButton.interactable = authorized && workbenchData.canResearchBlueprints;
    }

    void UpdateHealthDisplay()
    {
        if (healthSlider != null)
        {
            healthSlider.value = currentHealth / workbenchData.maxHealth;
        }
        
        if (healthText != null)
        {
            healthText.text = $"{Mathf.RoundToInt(currentHealth)}/{Mathf.RoundToInt(workbenchData.maxHealth)}";
        }
    }

    void UpdateVisualEffects()
    {
        bool isWorking = activeCraftingJobs.Count > 0 || currentResearchJob != null;
        
        if (workingEffect != null)
        {
            if (isWorking && !workingEffect.isPlaying)
                workingEffect.Play();
            else if (!isWorking && workingEffect.isPlaying)
                workingEffect.Stop();
        }
        
        if (workbenchLight != null)
        {
            workbenchLight.enabled = isWorking;
        }
    }

    void CheckDecay()
    {
        if (authorizedCupboard != null && authorizedCupboard.HasUpkeep())
        {
            // No decay if upkeep is maintained
            return;
        }
        
        // Apply decay
        float decayAmount = workbenchData.decayRate * Time.deltaTime / 3600f; // Per hour
        TakeDamage(decayAmount, DamageType.Decay);
    }

    public void TakeDamage(float damage, DamageType damageType)
    {
        currentHealth = Mathf.Max(0, currentHealth - damage);
        
        if (currentHealth <= 0)
        {
            DestroyWorkbench();
        }
    }

    void DestroyWorkbench()
    {
        // Drop some materials
        if (workbenchData.buildingCost != null)
        {
            foreach (var ingredient in workbenchData.buildingCost)
            {
                int dropAmount = ingredient.amount / 4; // 25% of build cost
                if (dropAmount > 0)
                {
                    DropItem(ingredient.itemData, dropAmount);
                }
            }
        }
        
        CloseWorkbenchUI();
        Destroy(gameObject);
    }

    void DropItem(ItemData item, int amount)
    {
        // Create item drop near workbench
        Vector3 dropPosition = transform.position + Random.insideUnitSphere * 2f;
        dropPosition.y = transform.position.y;
        
        // This would create an actual item pickup
        Debug.Log($"Dropped {item.itemName} x{amount} at {dropPosition}");
    }

    void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }

    void ShowMessage(string message)
    {
        Debug.Log($"Workbench: {message}");
        // This would show UI message to the player
    }

    ItemData GetSelectedResearchItem()
    {
        // This would get the item selected in the research UI
        return null; // Placeholder
    }

    ItemData GetScrapItemData()
    {
        // Return reference to scrap item
        return new ItemData { itemName = "Scrap", itemType = ItemType.Component };
    }

    // Public getters
    public WorkbenchData GetWorkbenchData() => workbenchData;
    public bool IsWorking() => activeCraftingJobs.Count > 0 || currentResearchJob != null;
    public int GetAvailableSlots() => workbenchData.maxCraftingSlots - activeCraftingJobs.Count;
}

[System.Serializable]
public class CraftingJob
{
    public CraftingRecipe recipe;
    public int amount;
    public float remainingTime;
    public float totalTime;
    public int slotIndex;
}

[System.Serializable]
public class ResearchJob
{
    public ItemData itemToResearch;
    public float remainingTime;
    public float totalTime;
}