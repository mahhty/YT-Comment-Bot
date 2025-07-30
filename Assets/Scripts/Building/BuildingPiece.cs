using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class BuildingPiece : MonoBehaviour, IInteractable
{
    [Header("Building Data")]
    public BuildingData buildingData;
    public BuildingTier currentTier = BuildingTier.Twig;
    public float currentHealth;
    public string ownerId;
    public string ownerName;
    
    [Header("Upgrade System")]
    public Dictionary<BuildingTier, BuildingUpgradeData> upgradeData = new Dictionary<BuildingTier, BuildingUpgradeData>();
    public bool canUpgrade = true;
    
    [Header("Visual Components")]
    public Renderer buildingRenderer;
    public Material[] tierMaterials = new Material[5]; // Materials for each tier
    public GameObject[] tierModels = new GameObject[5]; // Different models for each tier
    
    [Header("Health Display")]
    public Canvas healthCanvas;
    public Slider healthSlider;
    public Text healthText;
    public GameObject damageIndicator;
    
    [Header("Authorization")]
    public bool isAuthorized = true;
    public ToolCupboard authorizedCupboard;
    
    [Header("Decay System")]
    public bool enableDecay = true;
    public float decayRate = 1f; // HP lost per hour without cupboard
    public float lastDecayTime;
    
    private bool isDestroyed = false;
    private float maxHealthForTier;

    void Start()
    {
        InitializeBuilding();
        SetupUpgradeData();
        UpdateVisuals();
    }

    void Update()
    {
        if (enableDecay && !isDestroyed)
        {
            ProcessDecay();
        }
        
        UpdateHealthDisplay();
    }

    void InitializeBuilding()
    {
        if (buildingData == null) return;
        
        maxHealthForTier = GetMaxHealthForTier(currentTier);
        currentHealth = maxHealthForTier;
        lastDecayTime = Time.time;
    }

    void SetupUpgradeData()
    {
        // Define upgrade costs and requirements for each tier
        upgradeData[BuildingTier.Wood] = new BuildingUpgradeData
        {
            requiredResources = new RecipeIngredient[]
            {
                new RecipeIngredient { itemData = GetResourceData(ResourceType.Wood), amount = 150 }
            },
            upgradeTime = 5f,
            maxHealth = 250f
        };
        
        upgradeData[BuildingTier.Stone] = new BuildingUpgradeData
        {
            requiredResources = new RecipeIngredient[]
            {
                new RecipeIngredient { itemData = GetResourceData(ResourceType.Stone), amount = 300 }
            },
            upgradeTime = 10f,
            maxHealth = 500f
        };
        
        upgradeData[BuildingTier.Metal] = new BuildingUpgradeData
        {
            requiredResources = new RecipeIngredient[]
            {
                new RecipeIngredient { itemData = GetResourceData(ResourceType.MetalOre), amount = 200 },
                new RecipeIngredient { itemData = GetResourceData(ResourceType.LowGradeFuel), amount = 50 }
            },
            upgradeTime = 15f,
            maxHealth = 1000f
        };
        
        upgradeData[BuildingTier.HQM] = new BuildingUpgradeData
        {
            requiredResources = new RecipeIngredient[]
            {
                new RecipeIngredient { itemData = GetResourceData(ResourceType.HighQualityMetal), amount = 25 },
                new RecipeIngredient { itemData = GetResourceData(ResourceType.TechTrash), amount = 10 }
            },
            upgradeTime = 30f,
            maxHealth = 2000f
        };
    }

    float GetMaxHealthForTier(BuildingTier tier)
    {
        switch (tier)
        {
            case BuildingTier.Twig: return 10f;
            case BuildingTier.Wood: return 250f;
            case BuildingTier.Stone: return 500f;
            case BuildingTier.Metal: return 1000f;
            case BuildingTier.HQM: return 2000f;
            default: return 100f;
        }
    }

    ItemData GetResourceData(ResourceType resourceType)
    {
        // This would fetch the actual ItemData for the resource type
        // For now, return a placeholder
        return new ItemData { itemName = resourceType.ToString() };
    }

    public void TakeDamage(float damage, DamageType damageType = DamageType.Generic, string attackerId = "")
    {
        if (isDestroyed) return;
        
        // Apply damage resistance based on tier and damage type
        float actualDamage = CalculateDamageWithResistance(damage, damageType);
        
        currentHealth = Mathf.Max(0f, currentHealth - actualDamage);
        
        // Show damage effect
        ShowDamageEffect(actualDamage, damageType);
        
        if (currentHealth <= 0f)
        {
            DestroyBuilding(attackerId);
        }
    }

    float CalculateDamageWithResistance(float damage, DamageType damageType)
    {
        float resistance = 1f; // Default no resistance
        
        switch (currentTier)
        {
            case BuildingTier.Twig:
                resistance = 1f; // No resistance
                break;
                
            case BuildingTier.Wood:
                if (damageType == DamageType.Fire) resistance = 2f; // Weak to fire
                else if (damageType == DamageType.Melee) resistance = 0.5f; // Resistant to melee
                break;
                
            case BuildingTier.Stone:
                if (damageType == DamageType.Explosive) resistance = 1.5f; // Weak to explosives
                else if (damageType == DamageType.Melee) resistance = 0.1f; // Very resistant to melee
                else if (damageType == DamageType.Projectile) resistance = 0.3f; // Resistant to bullets
                break;
                
            case BuildingTier.Metal:
                if (damageType == DamageType.Explosive) resistance = 1.2f; // Slightly weak to explosives
                else if (damageType == DamageType.Fire) resistance = 0.1f; // Very resistant to fire
                else if (damageType == DamageType.Melee) resistance = 0.05f; // Extremely resistant to melee
                else if (damageType == DamageType.Projectile) resistance = 0.2f; // Resistant to bullets
                break;
                
            case BuildingTier.HQM:
                if (damageType == DamageType.Explosive) resistance = 0.8f; // Some resistance even to explosives
                else resistance = 0.1f; // Highly resistant to everything else
                break;
        }
        
        return damage * resistance;
    }

    void ShowDamageEffect(float damage, DamageType damageType)
    {
        // Visual and audio feedback for damage
        if (damageIndicator != null)
        {
            damageIndicator.SetActive(true);
            Invoke(nameof(HideDamageIndicator), 0.5f);
        }
        
        Debug.Log($"Building took {damage:F1} {damageType} damage. Health: {currentHealth:F0}/{maxHealthForTier:F0}");
    }

    void HideDamageIndicator()
    {
        if (damageIndicator != null)
            damageIndicator.SetActive(false);
    }

    void DestroyBuilding(string destroyerId)
    {
        isDestroyed = true;
        
        // Drop some resources based on tier
        DropResources();
        
        // Log destruction
        Debug.Log($"Building destroyed by {destroyerId}");
        
        // Notify building manager
        BuildingManager buildingManager = FindObjectOfType<BuildingManager>();
        if (buildingManager != null)
        {
            buildingManager.OnBuildingDestroyed(this, destroyerId);
        }
        
        // Destroy the game object
        Destroy(gameObject, 0.1f);
    }

    void DropResources()
    {
        // Drop a percentage of resources based on tier and damage taken
        float dropPercentage = Mathf.Clamp01(currentHealth / maxHealthForTier * 0.5f + 0.1f);
        
        if (upgradeData.ContainsKey(currentTier))
        {
            var upgrade = upgradeData[currentTier];
            foreach (var resource in upgrade.requiredResources)
            {
                int dropAmount = Mathf.FloorToInt(resource.amount * dropPercentage);
                if (dropAmount > 0)
                {
                    // SpawnItemPickup(resource.itemData, dropAmount, transform.position);
                    Debug.Log($"Dropped {dropAmount}x {resource.itemData.GetDisplayName()}");
                }
            }
        }
    }

    void ProcessDecay()
    {
        if (authorizedCupboard != null && authorizedCupboard.IsActive())
        {
            // No decay if protected by tool cupboard
            lastDecayTime = Time.time;
            return;
        }
        
        float timeSinceLastDecay = Time.time - lastDecayTime;
        if (timeSinceLastDecay >= 3600f) // 1 hour
        {
            float decayDamage = decayRate * (timeSinceLastDecay / 3600f);
            TakeDamage(decayDamage, DamageType.Decay);
            lastDecayTime = Time.time;
        }
    }

    void UpdateVisuals()
    {
        // Update material based on tier
        if (buildingRenderer != null && tierMaterials.Length > (int)currentTier)
        {
            buildingRenderer.material = tierMaterials[(int)currentTier];
        }
        
        // Update model based on tier
        for (int i = 0; i < tierModels.Length; i++)
        {
            if (tierModels[i] != null)
            {
                tierModels[i].SetActive(i == (int)currentTier);
            }
        }
    }

    void UpdateHealthDisplay()
    {
        if (healthCanvas == null) return;
        
        // Show health bar when damaged
        float healthPercentage = currentHealth / maxHealthForTier;
        bool showHealth = healthPercentage < 1f;
        
        healthCanvas.gameObject.SetActive(showHealth);
        
        if (showHealth)
        {
            if (healthSlider != null)
                healthSlider.value = healthPercentage;
                
            if (healthText != null)
                healthText.text = $"{currentHealth:F0}/{maxHealthForTier:F0}";
        }
    }

    // IInteractable implementation
    public bool CanInteract(PlayerController player)
    {
        if (isDestroyed) return false;
        
        // Check if player is authorized (same team/owner)
        return IsPlayerAuthorized(player);
    }

    public string GetInteractionText()
    {
        if (!canUpgrade || currentTier == BuildingTier.HQM)
            return "Cannot upgrade";
            
        BuildingTier nextTier = currentTier + 1;
        return $"Upgrade to {nextTier}";
    }

    public void Interact(PlayerController player)
    {
        if (!CanInteract(player)) return;
        
        // Open upgrade interface
        ShowUpgradeInterface(player);
    }

    void ShowUpgradeInterface(PlayerController player)
    {
        BuildingTier nextTier = currentTier + 1;
        if (nextTier > BuildingTier.HQM || !upgradeData.ContainsKey(nextTier))
            return;
            
        var upgrade = upgradeData[nextTier];
        InventoryManager inventory = player.GetComponent<InventoryManager>();
        
        if (inventory == null) return;
        
        // Check if player has required resources
        bool hasResources = true;
        foreach (var resource in upgrade.requiredResources)
        {
            if (!inventory.HasItem(resource.itemData, resource.amount))
            {
                hasResources = false;
                break;
            }
        }
        
        if (hasResources)
        {
            StartUpgrade(player, nextTier);
        }
        else
        {
            Debug.Log("Insufficient resources for upgrade");
            // Show required resources
        }
    }

    void StartUpgrade(PlayerController player, BuildingTier newTier)
    {
        var upgrade = upgradeData[newTier];
        InventoryManager inventory = player.GetComponent<InventoryManager>();
        
        // Consume resources
        foreach (var resource in upgrade.requiredResources)
        {
            inventory.RemoveItem(resource.itemData, resource.amount);
        }
        
        // Start upgrade process
        StartCoroutine(UpgradeProcess(newTier, upgrade.upgradeTime));
    }

    System.Collections.IEnumerator UpgradeProcess(BuildingTier newTier, float upgradeTime)
    {
        Debug.Log($"Upgrading to {newTier}... ({upgradeTime}s)");
        
        yield return new WaitForSeconds(upgradeTime);
        
        // Complete upgrade
        currentTier = newTier;
        maxHealthForTier = GetMaxHealthForTier(currentTier);
        currentHealth = maxHealthForTier; // Full health after upgrade
        
        UpdateVisuals();
        
        Debug.Log($"Upgrade to {newTier} complete!");
    }

    bool IsPlayerAuthorized(PlayerController player)
    {
        // Check if player is the owner or authorized by tool cupboard
        string playerId = player.name; // This would be actual player ID in multiplayer
        
        if (playerId == ownerId) return true;
        
        if (authorizedCupboard != null)
            return authorizedCupboard.IsPlayerAuthorized(playerId);
            
        return false;
    }

    public void SetOwner(string id, string name)
    {
        ownerId = id;
        ownerName = name;
    }

    public void SetToolCupboard(ToolCupboard cupboard)
    {
        authorizedCupboard = cupboard;
    }

    public BuildingTier GetTier()
    {
        return currentTier;
    }

    public float GetHealthPercentage()
    {
        return currentHealth / maxHealthForTier;
    }

    public bool IsDestroyed()
    {
        return isDestroyed;
    }
}

[System.Serializable]
public class BuildingUpgradeData
{
    public RecipeIngredient[] requiredResources;
    public float upgradeTime;
    public float maxHealth;
}

public enum DamageType
{
    Generic,
    Melee,
    Projectile,
    Explosive,
    Fire,
    Decay
}