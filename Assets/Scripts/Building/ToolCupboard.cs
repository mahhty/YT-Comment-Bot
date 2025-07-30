using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;

public class ToolCupboard : MonoBehaviour, IInteractable
{
    [Header("Authorization")]
    public float authorizationRadius = 25f;
    public List<string> authorizedPlayers = new List<string>();
    public string ownerId;
    public string ownerName;
    
    [Header("Upkeep System")]
    public bool requiresUpkeep = true;
    public float upkeepInterval = 24f; // Hours
    public Dictionary<ResourceType, int> upkeepCosts = new Dictionary<ResourceType, int>();
    public float lastUpkeepTime;
    public float currentUpkeepHours = 0f; // Hours of upkeep stored
    public float maxUpkeepHours = 24f; // Maximum upkeep that can be stored
    
    [Header("Storage")]
    public List<ItemStack> upkeepStorage = new List<ItemStack>();
    public int storageSlots = 12;
    
    [Header("UI References")]
    public GameObject cupboardUI;
    public Transform authorizedList;
    public GameObject authorizedPlayerPrefab;
    public Text upkeepTimeText;
    public Text statusText;
    public Button authorizeButton;
    public Button deauthorizeButton;
    public Transform storageGrid;
    public GameObject storageSlotPrefab;
    
    [Header("Visual Indicators")]
    public GameObject authorizationIndicator;
    public GameObject lowUpkeepIndicator;
    public GameObject noUpkeepIndicator;
    public Material authorizedMaterial;
    public Material unauthorizedMaterial;
    public Renderer cupboardRenderer;
    
    [Header("Protection")]
    public LayerMask protectedStructures;
    public bool preventBuilding = true;
    public bool preventDestruction = false; // Only when authorized
    
    private List<BuildingPiece> protectedBuildings = new List<BuildingPiece>();
    private bool isActive = true;
    private BuildingManager buildingManager;

    void Start()
    {
        InitializeCupboard();
        SetupUI();
        SetupUpkeepCosts();
        RefreshProtectedBuildings();
    }

    void Update()
    {
        if (requiresUpkeep)
        {
            ProcessUpkeep();
        }
        
        UpdateVisuals();
        UpdateUI();
    }

    void InitializeCupboard()
    {
        buildingManager = FindObjectOfType<BuildingManager>();
        lastUpkeepTime = Time.time;
        
        // Initialize storage
        for (int i = 0; i < storageSlots; i++)
        {
            upkeepStorage.Add(new ItemStack());
        }
        
        // Register with building manager
        if (buildingManager != null)
        {
            buildingManager.RegisterToolCupboard(this);
        }
    }

    void SetupUI()
    {
        if (cupboardUI != null)
            cupboardUI.SetActive(false);
            
        if (authorizeButton != null)
            authorizeButton.onClick.AddListener(() => AuthorizeCurrentPlayer());
            
        if (deauthorizeButton != null)
            deauthorizeButton.onClick.AddListener(() => DeauthorizeCurrentPlayer());
    }

    void SetupUpkeepCosts()
    {
        // Define upkeep costs (per 24 hours)
        upkeepCosts[ResourceType.Wood] = 100;
        upkeepCosts[ResourceType.Stone] = 50;
        upkeepCosts[ResourceType.MetalOre] = 25;
        upkeepCosts[ResourceType.HighQualityMetal] = 2;
    }

    void ProcessUpkeep()
    {
        float timeSinceLastUpkeep = (Time.time - lastUpkeepTime) / 3600f; // Convert to hours
        
        if (timeSinceLastUpkeep >= 1f) // Process every hour
        {
            float hoursToProcess = Mathf.Floor(timeSinceLastUpkeep);
            
            for (int i = 0; i < hoursToProcess; i++)
            {
                if (currentUpkeepHours > 0f)
                {
                    currentUpkeepHours -= 1f;
                }
                else
                {
                    // Try to consume resources from storage
                    if (!ConsumeUpkeepResources())
                    {
                        // No upkeep available - cupboard becomes inactive
                        SetActive(false);
                        break;
                    }
                }
            }
            
            lastUpkeepTime = Time.time;
        }
    }

    bool ConsumeUpkeepResources()
    {
        // Check if we have enough resources for one hour of upkeep
        Dictionary<ResourceType, int> hourlyUpkeep = new Dictionary<ResourceType, int>();
        foreach (var cost in upkeepCosts)
        {
            hourlyUpkeep[cost.Key] = Mathf.CeilToInt(cost.Value / 24f); // Daily cost / 24 hours
        }
        
        // Check availability
        foreach (var cost in hourlyUpkeep)
        {
            int available = GetResourceAmount(cost.Key);
            if (available < cost.Value)
                return false;
        }
        
        // Consume resources
        foreach (var cost in hourlyUpkeep)
        {
            ConsumeResource(cost.Key, cost.Value);
        }
        
        currentUpkeepHours += 1f;
        return true;
    }

    int GetResourceAmount(ResourceType resourceType)
    {
        int total = 0;
        foreach (var item in upkeepStorage)
        {
            if (!item.IsEmpty() && item.itemData.resourceData != null && 
                item.itemData.resourceData.resourceType == resourceType)
            {
                total += item.amount;
            }
        }
        return total;
    }

    void ConsumeResource(ResourceType resourceType, int amount)
    {
        int remaining = amount;
        
        for (int i = 0; i < upkeepStorage.Count && remaining > 0; i++)
        {
            var item = upkeepStorage[i];
            if (!item.IsEmpty() && item.itemData.resourceData != null && 
                item.itemData.resourceData.resourceType == resourceType)
            {
                int consumeAmount = Mathf.Min(remaining, item.amount);
                item.amount -= consumeAmount;
                remaining -= consumeAmount;
                
                if (item.amount <= 0)
                {
                    upkeepStorage[i] = new ItemStack();
                }
            }
        }
    }

    void RefreshProtectedBuildings()
    {
        protectedBuildings.Clear();
        
        Collider[] colliders = Physics.OverlapSphere(transform.position, authorizationRadius, protectedStructures);
        foreach (var collider in colliders)
        {
            BuildingPiece building = collider.GetComponent<BuildingPiece>();
            if (building != null)
            {
                protectedBuildings.Add(building);
                building.SetToolCupboard(this);
            }
        }
    }

    void UpdateVisuals()
    {
        // Update cupboard material based on status
        if (cupboardRenderer != null)
        {
            cupboardRenderer.material = isActive ? authorizedMaterial : unauthorizedMaterial;
        }
        
        // Update indicators
        if (authorizationIndicator != null)
            authorizationIndicator.SetActive(isActive);
            
        if (lowUpkeepIndicator != null)
            lowUpkeepIndicator.SetActive(isActive && currentUpkeepHours < 6f);
            
        if (noUpkeepIndicator != null)
            noUpkeepIndicator.SetActive(!isActive);
    }

    void UpdateUI()
    {
        if (!cupboardUI.activeInHierarchy) return;
        
        // Update upkeep time display
        if (upkeepTimeText != null)
        {
            if (isActive)
            {
                upkeepTimeText.text = $"Upkeep: {currentUpkeepHours:F1}h";
            }
            else
            {
                upkeepTimeText.text = "NO UPKEEP - DECAYING";
                upkeepTimeText.color = Color.red;
            }
        }
        
        // Update status text
        if (statusText != null)
        {
            if (isActive)
                statusText.text = "AUTHORIZED";
            else
                statusText.text = "UNAUTHORIZED - ADD UPKEEP";
        }
        
        RefreshAuthorizedList();
    }

    void RefreshAuthorizedList()
    {
        if (authorizedList == null || authorizedPlayerPrefab == null) return;
        
        // Clear existing list
        foreach (Transform child in authorizedList)
        {
            Destroy(child.gameObject);
        }
        
        // Add authorized players
        foreach (string playerId in authorizedPlayers)
        {
            GameObject playerEntry = Instantiate(authorizedPlayerPrefab, authorizedList);
            Text playerText = playerEntry.GetComponentInChildren<Text>();
            Button removeButton = playerEntry.GetComponentInChildren<Button>();
            
            if (playerText != null)
                playerText.text = GetPlayerName(playerId);
                
            if (removeButton != null)
            {
                string capturedId = playerId;
                removeButton.onClick.AddListener(() => RemoveAuthorization(capturedId));
            }
        }
    }

    string GetPlayerName(string playerId)
    {
        // This would fetch the actual player name from the multiplayer system
        return playerId;
    }

    // IInteractable implementation
    public bool CanInteract(PlayerController player)
    {
        float distance = Vector3.Distance(transform.position, player.transform.position);
        return distance <= 3f;
    }

    public string GetInteractionText()
    {
        return "Open Tool Cupboard";
    }

    public void Interact(PlayerController player)
    {
        if (!CanInteract(player)) return;
        
        OpenCupboardUI(player);
    }

    void OpenCupboardUI(PlayerController player)
    {
        if (cupboardUI != null)
        {
            cupboardUI.SetActive(true);
            
            // Check if player is authorized
            string playerId = GetPlayerId(player);
            bool isAuthorized = IsPlayerAuthorized(playerId);
            
            // Update button states
            if (authorizeButton != null)
                authorizeButton.interactable = !isAuthorized;
                
            if (deauthorizeButton != null)
                deauthorizeButton.interactable = isAuthorized;
        }
    }

    public void CloseCupboardUI()
    {
        if (cupboardUI != null)
            cupboardUI.SetActive(false);
    }

    string GetPlayerId(PlayerController player)
    {
        // This would return the actual multiplayer player ID
        return player.name;
    }

    void AuthorizeCurrentPlayer()
    {
        // This would be called with the current interacting player
        PlayerController currentPlayer = FindObjectOfType<PlayerController>();
        if (currentPlayer != null)
        {
            string playerId = GetPlayerId(currentPlayer);
            AddAuthorization(playerId);
        }
    }

    void DeauthorizeCurrentPlayer()
    {
        PlayerController currentPlayer = FindObjectOfType<PlayerController>();
        if (currentPlayer != null)
        {
            string playerId = GetPlayerId(currentPlayer);
            RemoveAuthorization(playerId);
        }
    }

    public void AddAuthorization(string playerId)
    {
        if (!authorizedPlayers.Contains(playerId))
        {
            authorizedPlayers.Add(playerId);
            Debug.Log($"Authorized player {playerId} for tool cupboard");
        }
    }

    public void RemoveAuthorization(string playerId)
    {
        if (authorizedPlayers.Remove(playerId))
        {
            Debug.Log($"Removed authorization for player {playerId}");
        }
    }

    public bool IsPlayerAuthorized(string playerId)
    {
        return playerId == ownerId || authorizedPlayers.Contains(playerId);
    }

    public bool CanPlayerBuild(string playerId, Vector3 position)
    {
        if (!isActive) return true; // Can build if cupboard is inactive
        
        float distance = Vector3.Distance(transform.position, position);
        if (distance > authorizationRadius) return true; // Outside range
        
        return IsPlayerAuthorized(playerId);
    }

    public bool CanPlayerDestroy(string playerId, BuildingPiece building)
    {
        if (!isActive) return true; // Can destroy if cupboard is inactive
        
        if (!protectedBuildings.Contains(building)) return true; // Not protected
        
        return IsPlayerAuthorized(playerId);
    }

    public void SetActive(bool active)
    {
        isActive = active;
        
        if (!active)
        {
            // Start decay on all protected buildings
            foreach (var building in protectedBuildings)
            {
                if (building != null)
                {
                    building.enableDecay = true;
                }
            }
        }
    }

    public bool IsActive()
    {
        return isActive;
    }

    public void SetOwner(string id, string name)
    {
        ownerId = id;
        ownerName = name;
        
        // Owner is automatically authorized
        if (!authorizedPlayers.Contains(id))
        {
            authorizedPlayers.Add(id);
        }
    }

    public void AddUpkeepResource(ItemData itemData, int amount)
    {
        // Find empty or matching slot
        for (int i = 0; i < upkeepStorage.Count; i++)
        {
            if (upkeepStorage[i].IsEmpty())
            {
                upkeepStorage[i] = new ItemStack(itemData, amount);
                break;
            }
            else if (upkeepStorage[i].itemData.CanStackWith(itemData))
            {
                int stackSpace = upkeepStorage[i].GetStackSpace();
                int addAmount = Mathf.Min(amount, stackSpace);
                upkeepStorage[i].amount += addAmount;
                amount -= addAmount;
                
                if (amount <= 0) break;
            }
        }
        
        // Try to process upkeep with new resources
        TryProcessUpkeep();
    }

    void TryProcessUpkeep()
    {
        while (currentUpkeepHours < maxUpkeepHours && ConsumeUpkeepResources())
        {
            // Keep adding upkeep until max or resources run out
        }
        
        if (currentUpkeepHours > 0f && !isActive)
        {
            SetActive(true);
        }
    }

    public float GetUpkeepHours()
    {
        return currentUpkeepHours;
    }

    public List<BuildingPiece> GetProtectedBuildings()
    {
        return protectedBuildings;
    }

    // Gizmos for debugging
    void OnDrawGizmosSelected()
    {
        Gizmos.color = isActive ? Color.green : Color.red;
        Gizmos.DrawWireSphere(transform.position, authorizationRadius);
    }

    void OnDestroy()
    {
        // Remove cupboard protection from all buildings
        foreach (var building in protectedBuildings)
        {
            if (building != null)
            {
                building.SetToolCupboard(null);
                building.enableDecay = true;
            }
        }
        
        // Unregister from building manager
        if (buildingManager != null)
        {
            buildingManager.UnregisterToolCupboard(this);
        }
    }
}