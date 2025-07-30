using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class DeathManager : MonoBehaviour
{
    [Header("Death Screen UI")]
    public GameObject deathScreenUI;
    public Text deathMessageText;
    public Text killerInfoText;
    public Text distanceText;
    public Text weaponText;
    public Text deathTimeText;
    public Button respawnButton;
    public Button spectateButton;
    public Button exitToMenuButton;
    
    [Header("Respawn Options")]
    public Transform[] spawnPoints;
    public Text respawnTimerText;
    public float respawnCooldown = 5f;
    public bool canRespawnImmediately = false;
    
    [Header("Loot Bag")]
    public GameObject lootBagPrefab;
    public float lootBagDespawnTime = 300f; // 5 minutes
    public int maxLootBagsPerPlayer = 3;
    
    [Header("Death Effects")]
    public AudioClip deathSound;
    public ParticleSystem deathEffect;
    public Color deathScreenColor = new Color(0.8f, 0.2f, 0.2f, 0.3f);
    
    [Header("Spectate Settings")]
    public Camera spectateCamera;
    public float spectateSpeed = 5f;
    public bool canSpectateEnemies = false;
    
    // Death tracking
    private DeathInfo lastDeath;
    private List<GameObject> playerLootBags = new List<GameObject>();
    private bool isDead = false;
    private float respawnTimer = 0f;
    private PlayerController playerController;
    private InventoryManager inventoryManager;
    private SurvivalManager survivalManager;

    void Start()
    {
        InitializeDeathManager();
    }

    void Update()
    {
        if (isDead)
        {
            UpdateRespawnTimer();
            HandleSpectateInput();
        }
    }

    void InitializeDeathManager()
    {
        playerController = GetComponent<PlayerController>();
        inventoryManager = GetComponent<InventoryManager>();
        survivalManager = GetComponent<SurvivalManager>();
        
        // Setup UI buttons
        if (respawnButton != null)
            respawnButton.onClick.AddListener(AttemptRespawn);
            
        if (spectateButton != null)
            spectateButton.onClick.AddListener(StartSpectating);
            
        if (exitToMenuButton != null)
            exitToMenuButton.onClick.AddListener(ExitToMainMenu);
        
        // Hide death screen initially
        if (deathScreenUI != null)
            deathScreenUI.SetActive(false);
    }

    public void OnPlayerDeath(DeathInfo deathInfo)
    {
        if (isDead) return;
        
        isDead = true;
        lastDeath = deathInfo;
        respawnTimer = respawnCooldown;
        
        // Create loot bag
        CreateLootBag(transform.position);
        
        // Clear player inventory
        ClearPlayerInventory();
        
        // Show death screen
        ShowDeathScreen();
        
        // Play death effects
        PlayDeathEffects();
        
        // Disable player controls
        DisablePlayerControls();
        
        // Notify other systems
        NotifyPlayerDeath();
        
        Debug.Log($"Player died: {deathInfo.cause} by {deathInfo.killerName}");
    }

    void CreateLootBag(Vector3 position)
    {
        if (lootBagPrefab == null) return;
        
        // Check if we need to remove old loot bags
        while (playerLootBags.Count >= maxLootBagsPerPlayer)
        {
            GameObject oldestBag = playerLootBags[0];
            playerLootBags.RemoveAt(0);
            if (oldestBag != null)
                Destroy(oldestBag);
        }
        
        // Create new loot bag
        GameObject lootBag = Instantiate(lootBagPrefab, position, Quaternion.identity);
        LootBag lootBagComponent = lootBag.GetComponent<LootBag>();
        
        if (lootBagComponent != null)
        {
            // Transfer all inventory items to loot bag
            if (inventoryManager != null)
            {
                List<ItemStack> allItems = inventoryManager.GetAllItems();
                foreach (var itemStack in allItems)
                {
                    lootBagComponent.AddItem(itemStack.item, itemStack.amount);
                }
            }
            
            // Set loot bag properties
            lootBagComponent.SetOwner(playerController);
            lootBagComponent.SetDespawnTime(lootBagDespawnTime);
            lootBagComponent.SetDeathInfo(lastDeath);
        }
        
        playerLootBags.Add(lootBag);
        
        Debug.Log($"Created loot bag at {position} with {inventoryManager?.GetItemCount() ?? 0} items");
    }

    void ClearPlayerInventory()
    {
        if (inventoryManager != null)
        {
            inventoryManager.ClearInventory();
        }
    }

    void ShowDeathScreen()
    {
        if (deathScreenUI == null) return;
        
        deathScreenUI.SetActive(true);
        
        // Update death information
        UpdateDeathScreenInfo();
        
        // Apply Rust styling
        RustUIUpdater updater = deathScreenUI.GetComponent<RustUIUpdater>();
        if (updater == null)
        {
            updater = deathScreenUI.AddComponent<RustUIUpdater>();
            updater.updateRecursively = true;
        }
        updater.UpdateRustStyling();
        
        // Set death screen overlay color
        Image overlay = deathScreenUI.GetComponent<Image>();
        if (overlay != null)
        {
            overlay.color = deathScreenColor;
        }
    }

    void UpdateDeathScreenInfo()
    {
        if (lastDeath == null) return;
        
        // Main death message
        if (deathMessageText != null)
        {
            string message = GetDeathMessage(lastDeath);
            RustFontManager.SetRustText(deathMessageText, message, RustTextType.Header);
        }
        
        // Killer information
        if (killerInfoText != null && !string.IsNullOrEmpty(lastDeath.killerName))
        {
            string killerInfo = $"Killed by: {lastDeath.killerName}";
            RustFontManager.SetRustText(killerInfoText, killerInfo, RustTextType.Warning);
        }
        
        // Distance information
        if (distanceText != null && lastDeath.distance > 0)
        {
            string distance = $"Distance: {lastDeath.distance:F1}m";
            RustFontManager.SetRustText(distanceText, distance, RustTextType.Info);
        }
        
        // Weapon information
        if (weaponText != null && !string.IsNullOrEmpty(lastDeath.weapon))
        {
            string weapon = $"Weapon: {lastDeath.weapon}";
            RustFontManager.SetRustText(weaponText, weapon, RustTextType.Info);
        }
        
        // Death time
        if (deathTimeText != null)
        {
            string timeText = System.DateTime.Now.ToString("HH:mm");
            RustFontManager.SetRustText(deathTimeText, $"Time of Death: {timeText}", RustTextType.Info);
        }
    }

    string GetDeathMessage(DeathInfo deathInfo)
    {
        switch (deathInfo.cause)
        {
            case DeathCause.Player:
                return $"YOU WERE KILLED BY {deathInfo.killerName.ToUpper()}";
            case DeathCause.NPC:
                return $"YOU WERE KILLED BY {deathInfo.killerName.ToUpper()}";
            case DeathCause.Fall:
                return "YOU FELL TO YOUR DEATH";
            case DeathCause.Drowning:
                return "YOU DROWNED";
            case DeathCause.Starvation:
                return "YOU STARVED TO DEATH";
            case DeathCause.Dehydration:
                return "YOU DIED OF THIRST";
            case DeathCause.Cold:
                return "YOU FROZE TO DEATH";
            case DeathCause.Heat:
                return "YOU DIED OF HEAT EXHAUSTION";
            case DeathCause.Radiation:
                return "YOU DIED FROM RADIATION POISONING";
            case DeathCause.Explosion:
                return "YOU WERE BLOWN UP";
            case DeathCause.Fire:
                return "YOU BURNED TO DEATH";
            case DeathCause.Suicide:
                return "YOU KILLED YOURSELF";
            default:
                return "YOU DIED";
        }
    }

    void UpdateRespawnTimer()
    {
        if (respawnTimer > 0)
        {
            respawnTimer -= Time.deltaTime;
            
            if (respawnTimerText != null)
            {
                if (respawnTimer > 0)
                {
                    string timerText = $"Respawn in {Mathf.Ceil(respawnTimer)}s";
                    RustFontManager.SetRustText(respawnTimerText, timerText, RustTextType.Warning);
                }
                else
                {
                    RustFontManager.SetRustText(respawnTimerText, "RESPAWN AVAILABLE", RustTextType.Success);
                }
            }
            
            // Update respawn button state
            if (respawnButton != null)
            {
                respawnButton.interactable = respawnTimer <= 0 || canRespawnImmediately;
            }
        }
    }

    void PlayDeathEffects()
    {
        // Play death sound
        AudioSource audioSource = GetComponent<AudioSource>();
        if (audioSource != null && deathSound != null)
        {
            audioSource.PlayOneShot(deathSound);
        }
        
        // Play death particle effect
        if (deathEffect != null)
        {
            deathEffect.transform.position = transform.position;
            deathEffect.Play();
        }
    }

    void DisablePlayerControls()
    {
        if (playerController != null)
        {
            playerController.enabled = false;
        }
        
        // Disable other player components
        var movement = GetComponent<CharacterController>();
        if (movement != null)
        {
            movement.enabled = false;
        }
    }

    void EnablePlayerControls()
    {
        if (playerController != null)
        {
            playerController.enabled = true;
        }
        
        // Re-enable other player components
        var movement = GetComponent<CharacterController>();
        if (movement != null)
        {
            movement.enabled = true;
        }
    }

    void NotifyPlayerDeath()
    {
        // Notify other systems about player death
        SleeperManager sleeperManager = FindObjectOfType<SleeperManager>();
        if (sleeperManager != null)
        {
            sleeperManager.OnPlayerDeath(playerController);
        }
        
        // Update server statistics
        ServerManager serverManager = FindObjectOfType<ServerManager>();
        if (serverManager != null)
        {
            serverManager.RecordPlayerDeath(playerController, lastDeath);
        }
    }

    public void AttemptRespawn()
    {
        if (respawnTimer > 0 && !canRespawnImmediately)
        {
            Debug.Log("Cannot respawn yet, timer still active");
            return;
        }
        
        RespawnPlayer();
    }

    void RespawnPlayer()
    {
        if (!isDead) return;
        
        // Find spawn point
        Vector3 spawnPosition = GetRespawnPosition();
        
        // Reset player position
        transform.position = spawnPosition;
        
        // Reset player state
        ResetPlayerState();
        
        // Hide death screen
        HideDeathScreen();
        
        // Re-enable controls
        EnablePlayerControls();
        
        // Reset death state
        isDead = false;
        lastDeath = null;
        
        Debug.Log($"Player respawned at {spawnPosition}");
    }

    Vector3 GetRespawnPosition()
    {
        // Try to find a valid spawn point
        if (spawnPoints != null && spawnPoints.Length > 0)
        {
            // Check for sleeping bags first (player-made spawn points)
            SleepingBag[] sleepingBags = FindObjectsOfType<SleepingBag>();
            foreach (var bag in sleepingBags)
            {
                if (bag.CanUse(playerController))
                {
                    return bag.GetSpawnPosition();
                }
            }
            
            // Fall back to random spawn point
            Transform randomSpawn = spawnPoints[Random.Range(0, spawnPoints.Length)];
            return randomSpawn.position;
        }
        
        // Default spawn at origin
        return Vector3.zero;
    }

    void ResetPlayerState()
    {
        // Reset survival stats
        if (survivalManager != null)
        {
            survivalManager.ResetStats();
        }
        
        // Give starting items
        GiveStartingItems();
    }

    void GiveStartingItems()
    {
        if (inventoryManager == null) return;
        
        // Give basic starting items (like Rust)
        ItemData rockItem = GetItemData("Rock");
        ItemData torchItem = GetItemData("Torch");
        ItemData bandageItem = GetItemData("Bandage");
        
        if (rockItem != null)
            inventoryManager.AddItem(rockItem, 1);
            
        if (torchItem != null)
            inventoryManager.AddItem(torchItem, 1);
            
        if (bandageItem != null)
            inventoryManager.AddItem(bandageItem, 2);
    }

    void HideDeathScreen()
    {
        if (deathScreenUI != null)
            deathScreenUI.SetActive(false);
    }

    public void StartSpectating()
    {
        if (spectateCamera == null) return;
        
        // Enable spectate camera
        spectateCamera.gameObject.SetActive(true);
        
        // Position camera at death location
        spectateCamera.transform.position = transform.position + Vector3.up * 10f;
        
        Debug.Log("Started spectating");
    }

    void HandleSpectateInput()
    {
        if (spectateCamera == null || !spectateCamera.gameObject.activeInHierarchy) return;
        
        // Simple spectate camera movement (mobile-friendly)
        Vector3 movement = Vector3.zero;
        
        // This would be replaced with proper mobile input
        if (Input.GetKey(KeyCode.W)) movement += Vector3.forward;
        if (Input.GetKey(KeyCode.S)) movement += Vector3.back;
        if (Input.GetKey(KeyCode.A)) movement += Vector3.left;
        if (Input.GetKey(KeyCode.D)) movement += Vector3.right;
        if (Input.GetKey(KeyCode.Q)) movement += Vector3.down;
        if (Input.GetKey(KeyCode.E)) movement += Vector3.up;
        
        spectateCamera.transform.Translate(movement * spectateSpeed * Time.deltaTime);
    }

    public void ExitToMainMenu()
    {
        // Save any necessary data before leaving
        SavePlayerData();
        
        // Load main menu scene
        UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
    }

    void SavePlayerData()
    {
        // Save player progression, blueprints, etc.
        BlueprintManager blueprintManager = FindObjectOfType<BlueprintManager>();
        if (blueprintManager != null)
        {
            blueprintManager.SavePlayerBlueprints(playerController);
        }
    }

    ItemData GetItemData(string itemName)
    {
        // This would fetch actual ItemData from registry
        return new ItemData { itemName = itemName };
    }

    // Public getters
    public bool IsDead() => isDead;
    public DeathInfo GetLastDeath() => lastDeath;
    public List<GameObject> GetLootBags() => playerLootBags;
}

[System.Serializable]
public class DeathInfo
{
    public DeathCause cause;
    public string killerName;
    public string weapon;
    public float distance;
    public Vector3 deathPosition;
    public System.DateTime deathTime;
}

public enum DeathCause
{
    Player,        // Killed by another player
    NPC,          // Killed by NPC/AI
    Fall,         // Fall damage
    Drowning,     // Drowning
    Starvation,   // Hunger reached 0
    Dehydration,  // Thirst reached 0
    Cold,         // Temperature too low
    Heat,         // Temperature too high
    Radiation,    // Radiation damage
    Explosion,    // Explosive damage
    Fire,         // Fire damage
    Suicide,      // Player killed themselves
    Unknown       // Unknown cause
}