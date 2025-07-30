using UnityEngine;
using UnityEngine.UI;

public class SleeperController : MonoBehaviour, IInteractable
{
    [Header("Sleeper Data")]
    public string playerId;
    public string playerName;
    public PlayerInventoryData inventoryData;
    public SurvivalData survivalData;
    
    [Header("Visual Components")]
    public Renderer sleeperRenderer;
    public Animator sleeperAnimator;
    public Canvas nameCanvas;
    public Text nameText;
    public GameObject interactionPrompt;
    
    [Header("Loot Protection")]
    public float lootProtectionTime = 30f;
    public bool canBeLooted = false;
    public GameObject protectionIndicator;
    
    [Header("Survival Decay")]
    public bool enableSurvivalDecay = true;
    public float hungerDecayRate = 0.1f; // Slower decay while sleeping
    public float thirstDecayRate = 0.15f;
    public float radiationDecayRate = 0.5f;
    
    private float disconnectTime;
    private bool isDead = false;
    private SleeperManager sleeperManager;

    public void Initialize(string id, PlayerInventoryData inventory, SurvivalData survival, float protection)
    {
        playerId = id;
        inventoryData = inventory;
        survivalData = survival;
        lootProtectionTime = protection;
        disconnectTime = Time.time;
        
        sleeperManager = FindObjectOfType<SleeperManager>();
        
        SetupVisuals();
        SetupNameDisplay();
        StartLootProtection();
    }

    void SetupVisuals()
    {
        // Set sleeper to sleeping pose
        if (sleeperAnimator != null)
        {
            sleeperAnimator.SetBool("IsSleeping", true);
            sleeperAnimator.SetFloat("Health", survivalData.currentHealth / 100f);
        }
        
        // Setup renderer materials based on health status
        if (sleeperRenderer != null)
        {
            UpdateHealthVisuals();
        }
    }

    void SetupNameDisplay()
    {
        if (nameCanvas != null && nameText != null)
        {
            nameText.text = $"{playerName}\n[SLEEPING]";
            nameCanvas.worldCamera = Camera.main;
        }
    }

    void StartLootProtection()
    {
        canBeLooted = false;
        
        if (protectionIndicator != null)
            protectionIndicator.SetActive(true);
            
        Invoke(nameof(EndLootProtection), lootProtectionTime);
    }

    void EndLootProtection()
    {
        canBeLooted = true;
        
        if (protectionIndicator != null)
            protectionIndicator.SetActive(false);
            
        Debug.Log($"Sleeper {playerId} can now be looted");
    }

    public void UpdateSleeper()
    {
        if (isDead) return;
        
        if (enableSurvivalDecay)
        {
            UpdateSurvivalStats();
        }
        
        UpdateHealthVisuals();
        CheckDeath();
    }

    void UpdateSurvivalStats()
    {
        float deltaTime = Time.deltaTime;
        
        // Slower decay while sleeping
        survivalData.currentHunger = Mathf.Max(0f, survivalData.currentHunger - hungerDecayRate * deltaTime);
        survivalData.currentThirst = Mathf.Max(0f, survivalData.currentThirst - thirstDecayRate * deltaTime);
        
        // Radiation decays faster while sleeping (body processes it)
        if (survivalData.currentRadiation > 0f)
        {
            survivalData.currentRadiation = Mathf.Max(0f, survivalData.currentRadiation - radiationDecayRate * deltaTime);
        }
        
        // Apply damage from starvation/dehydration
        if (survivalData.currentHunger <= 0f)
        {
            survivalData.currentHealth = Mathf.Max(0f, survivalData.currentHealth - 1f * deltaTime);
        }
        
        if (survivalData.currentThirst <= 0f)
        {
            survivalData.currentHealth = Mathf.Max(0f, survivalData.currentHealth - 2f * deltaTime);
        }
    }

    void UpdateHealthVisuals()
    {
        if (sleeperAnimator != null)
        {
            sleeperAnimator.SetFloat("Health", survivalData.currentHealth / 100f);
        }
        
        // Change visual appearance based on health
        if (sleeperRenderer != null)
        {
            float healthPercentage = survivalData.currentHealth / 100f;
            Color baseColor = Color.white;
            
            if (healthPercentage < 0.3f)
                baseColor = Color.red;
            else if (healthPercentage < 0.6f)
                baseColor = Color.yellow;
                
            sleeperRenderer.material.color = baseColor;
        }
    }

    void CheckDeath()
    {
        if (survivalData.currentHealth <= 0f && !isDead)
        {
            Die();
        }
    }

    void Die()
    {
        isDead = true;
        
        // Change animation to death pose
        if (sleeperAnimator != null)
        {
            sleeperAnimator.SetBool("IsDead", true);
        }
        
        // Update name display
        if (nameText != null)
        {
            nameText.text = $"{playerName}\n[DEAD]";
        }
        
        // Can always be looted when dead
        canBeLooted = true;
        
        if (protectionIndicator != null)
            protectionIndicator.SetActive(false);
            
        Debug.Log($"Sleeper {playerId} died while offline");
    }

    // IInteractable implementation
    public bool CanInteract(PlayerController player)
    {
        if (!canBeLooted && !isDead) return false;
        
        float distance = Vector3.Distance(transform.position, player.transform.position);
        return distance <= 3f; // Interaction range
    }

    public string GetInteractionText()
    {
        if (isDead)
            return $"Loot {playerName}'s body";
        else if (canBeLooted)
            return $"Search {playerName}";
        else
            return $"{playerName} is protected";
    }

    public void Interact(PlayerController player)
    {
        if (!CanInteract(player)) return;
        
        // Open loot interface
        OpenLootInterface(player);
    }

    void OpenLootInterface(PlayerController looter)
    {
        Debug.Log($"Player {looter.name} is looting sleeper {playerId}");
        
        // This would open a loot UI showing the sleeper's inventory
        // For now, we'll just transfer some items as an example
        
        InventoryManager looterInventory = looter.GetComponent<InventoryManager>();
        if (looterInventory != null && inventoryData.items.Count > 0)
        {
            // Transfer first non-empty item as example
            for (int i = 0; i < inventoryData.items.Count; i++)
            {
                if (!inventoryData.items[i].IsEmpty())
                {
                    ItemStack item = inventoryData.items[i];
                    if (looterInventory.AddItem(item.itemData, item.amount))
                    {
                        inventoryData.items[i] = new ItemStack(); // Remove from sleeper
                        Debug.Log($"Looted {item.amount}x {item.itemData.GetDisplayName()}");
                        break;
                    }
                }
            }
        }
        
        // Check if sleeper inventory is now empty
        bool hasItems = false;
        foreach (var item in inventoryData.items)
        {
            if (!item.IsEmpty())
            {
                hasItems = true;
                break;
            }
        }
        
        // If dead and no items left, despawn the sleeper
        if (isDead && !hasItems)
        {
            if (sleeperManager != null)
            {
                sleeperManager.DestroySleeper(playerId);
            }
        }
    }

    public void TakeDamage(float damage, string attackerId)
    {
        if (isDead) return;
        
        survivalData.currentHealth = Mathf.Max(0f, survivalData.currentHealth - damage);
        
        Debug.Log($"Sleeper {playerId} took {damage} damage from {attackerId}");
        
        if (survivalData.currentHealth <= 0f)
        {
            // Notify sleeper manager of death
            if (sleeperManager != null)
            {
                sleeperManager.OnSleeperKilled(playerId, attackerId);
            }
        }
    }

    public SurvivalData GetCurrentSurvivalData()
    {
        return survivalData;
    }

    public PlayerInventoryData GetCurrentInventoryData()
    {
        return inventoryData;
    }

    public bool IsDead()
    {
        return isDead;
    }

    public bool IsProtected()
    {
        return !canBeLooted && !isDead;
    }

    public float GetTimeSinceDisconnect()
    {
        return Time.time - disconnectTime;
    }

    // Gizmos for debugging
    void OnDrawGizmosSelected()
    {
        Gizmos.color = canBeLooted ? Color.red : Color.blue;
        Gizmos.DrawWireSphere(transform.position, 3f); // Interaction range
    }
}