using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class SafeZoneManager : MonoBehaviour
{
    [Header("Safe Zone Settings")]
    public float safeZoneRadius = 50f;
    public LayerMask playerLayer = 1;
    public bool preventPvP = true;
    public bool preventBuilding = true;
    public bool preventRaiding = true;
    
    [Header("Visual Indicators")]
    public GameObject safeZoneEffect;
    public Material safeZoneMaterial;
    public Color safeZoneColor = Color.green;
    public ParticleSystem boundaryEffect;
    
    [Header("UI Elements")]
    public GameObject safeZoneUI;
    public Text safeZoneNameText;
    public Text safeZoneInfoText;
    public Button shopButton;
    public Button gamblingButton;
    public Button recyclerButton;
    
    [Header("Safe Zone Features")]
    public bool hasShop = true;
    public bool hasGambling = true;
    public bool hasRecycler = true;
    public bool hasWorkbenches = true;
    public bool hasSleepingBags = false; // Usually no sleeping in safe zones
    
    [Header("Gambling Wheel")]
    public GamblingWheel gamblingWheel;
    public int minBet = 10;
    public int maxBet = 1000;
    
    [Header("NPCs")]
    public List<SafeZoneNPC> npcs = new List<SafeZoneNPC>();
    
    private HashSet<PlayerController> playersInZone = new HashSet<PlayerController>();
    private Collider safeZoneCollider;
    private ShopManager shopManager;

    void Start()
    {
        InitializeSafeZone();
        SetupUI();
        CreateSafeZoneBoundary();
    }

    void Update()
    {
        CheckPlayersInZone();
        UpdateSafeZoneEffects();
    }

    void InitializeSafeZone()
    {
        // Setup collider for safe zone detection
        safeZoneCollider = GetComponent<SphereCollider>();
        if (safeZoneCollider == null)
        {
            safeZoneCollider = gameObject.AddComponent<SphereCollider>();
        }
        
        safeZoneCollider.radius = safeZoneRadius;
        safeZoneCollider.isTrigger = true;
        
        // Initialize components
        shopManager = GetComponent<ShopManager>();
        if (shopManager == null && hasShop)
        {
            shopManager = gameObject.AddComponent<ShopManager>();
        }
        
        // Setup gambling wheel
        if (gamblingWheel == null && hasGambling)
        {
            GameObject wheelGO = new GameObject("GamblingWheel");
            wheelGO.transform.SetParent(transform);
            gamblingWheel = wheelGO.AddComponent<GamblingWheel>();
        }
    }

    void SetupUI()
    {
        if (safeZoneUI != null)
            safeZoneUI.SetActive(false);
            
        if (shopButton != null && hasShop)
            shopButton.onClick.AddListener(OpenShop);
            
        if (gamblingButton != null && hasGambling)
            gamblingButton.onClick.AddListener(OpenGambling);
            
        if (recyclerButton != null && hasRecycler)
            recyclerButton.onClick.AddListener(OpenRecycler);
            
        // Update UI text
        if (safeZoneNameText != null)
            safeZoneNameText.text = "SAFE ZONE";
            
        if (safeZoneInfoText != null)
            safeZoneInfoText.text = "No PvP • No Building • Trading Available";
    }

    void CreateSafeZoneBoundary()
    {
        // Create visual boundary
        GameObject boundary = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        boundary.name = "SafeZoneBoundary";
        boundary.transform.SetParent(transform);
        boundary.transform.localPosition = Vector3.zero;
        boundary.transform.localScale = Vector3.one * safeZoneRadius * 2f;
        
        // Remove collider from visual boundary
        Destroy(boundary.GetComponent<Collider>());
        
        // Set material
        Renderer boundaryRenderer = boundary.GetComponent<Renderer>();
        if (boundaryRenderer != null && safeZoneMaterial != null)
        {
            boundaryRenderer.material = safeZoneMaterial;
            boundaryRenderer.material.color = safeZoneColor;
        }
        
        // Start particle effects
        if (boundaryEffect != null)
        {
            boundaryEffect.transform.localScale = Vector3.one * safeZoneRadius;
            boundaryEffect.Play();
        }
    }

    void CheckPlayersInZone()
    {
        // This is handled by OnTriggerEnter/Exit, but we can add additional checks here
        foreach (var player in playersInZone)
        {
            if (player == null) continue;
            
            // Heal players slowly in safe zone
            SurvivalManager survival = player.GetComponent<SurvivalManager>();
            if (survival != null && survival.GetHealthPercentage() < 1f)
            {
                survival.RestoreHealth(5f * Time.deltaTime); // 5 HP per second
            }
        }
    }

    void UpdateSafeZoneEffects()
    {
        // Update visual effects based on players in zone
        if (safeZoneEffect != null)
        {
            bool hasPlayers = playersInZone.Count > 0;
            safeZoneEffect.SetActive(hasPlayers);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        PlayerController player = other.GetComponent<PlayerController>();
        if (player != null)
        {
            EnterSafeZone(player);
        }
    }

    void OnTriggerExit(Collider other)
    {
        PlayerController player = other.GetComponent<PlayerController>();
        if (player != null)
        {
            ExitSafeZone(player);
        }
    }

    void EnterSafeZone(PlayerController player)
    {
        if (playersInZone.Contains(player)) return;
        
        playersInZone.Add(player);
        
        // Show safe zone UI
        ShowSafeZoneUI(player);
        
        // Apply safe zone effects
        ApplySafeZoneEffects(player, true);
        
        // Notify player
        ShowMessage(player, "You have entered a SAFE ZONE");
        
        Debug.Log($"Player {player.name} entered safe zone");
    }

    void ExitSafeZone(PlayerController player)
    {
        if (!playersInZone.Contains(player)) return;
        
        playersInZone.Remove(player);
        
        // Hide safe zone UI
        HideSafeZoneUI(player);
        
        // Remove safe zone effects
        ApplySafeZoneEffects(player, false);
        
        // Notify player
        ShowMessage(player, "You have left the SAFE ZONE");
        
        Debug.Log($"Player {player.name} left safe zone");
    }

    void ShowSafeZoneUI(PlayerController player)
    {
        if (safeZoneUI != null)
        {
            safeZoneUI.SetActive(true);
            
            // Update button states based on available features
            if (shopButton != null)
                shopButton.gameObject.SetActive(hasShop);
                
            if (gamblingButton != null)
                gamblingButton.gameObject.SetActive(hasGambling);
                
            if (recyclerButton != null)
                recyclerButton.gameObject.SetActive(hasRecycler);
        }
    }

    void HideSafeZoneUI(PlayerController player)
    {
        if (safeZoneUI != null)
            safeZoneUI.SetActive(false);
    }

    void ApplySafeZoneEffects(PlayerController player, bool entering)
    {
        // Apply or remove safe zone status effects
        SafeZoneStatus status = player.GetComponent<SafeZoneStatus>();
        if (status == null)
            status = player.gameObject.AddComponent<SafeZoneStatus>();
            
        status.SetInSafeZone(entering);
        
        if (entering)
        {
            // Stop any ongoing combat
            CombatManager combat = player.GetComponent<CombatManager>();
            if (combat != null)
            {
                combat.ExitCombat();
            }
            
            // Cancel any raiding actions
            RaidingManager raiding = player.GetComponent<RaidingManager>();
            if (raiding != null)
            {
                raiding.OnMobileAimCancel();
            }
        }
    }

    void ShowMessage(PlayerController player, string message)
    {
        // This would show a UI message to the player
        Debug.Log($"Message to {player.name}: {message}");
    }

    // UI Button Methods
    public void OpenShop()
    {
        if (shopManager != null && hasShop)
        {
            shopManager.OpenShop();
        }
    }

    public void OpenGambling()
    {
        if (gamblingWheel != null && hasGambling)
        {
            gamblingWheel.OpenGamblingInterface();
        }
    }

    public void OpenRecycler()
    {
        // Open recycler interface
        RecyclerManager recycler = FindObjectOfType<RecyclerManager>();
        if (recycler != null && hasRecycler)
        {
            recycler.OpenRecycler();
        }
    }

    // Public methods for other systems to check safe zone status
    public bool IsPlayerInSafeZone(PlayerController player)
    {
        return playersInZone.Contains(player);
    }

    public bool CanPlayerPvP(PlayerController player)
    {
        return !preventPvP || !IsPlayerInSafeZone(player);
    }

    public bool CanPlayerBuild(PlayerController player, Vector3 position)
    {
        if (!preventBuilding) return true;
        
        float distance = Vector3.Distance(transform.position, position);
        return distance > safeZoneRadius;
    }

    public bool CanPlayerRaid(PlayerController player, Vector3 position)
    {
        if (!preventRaiding) return true;
        
        float distance = Vector3.Distance(transform.position, position);
        return distance > safeZoneRadius;
    }

    public List<PlayerController> GetPlayersInZone()
    {
        return new List<PlayerController>(playersInZone);
    }

    public int GetPlayerCount()
    {
        return playersInZone.Count;
    }

    // Static method to check if any safe zone prevents an action
    public static bool CanPerformAction(Vector3 position, SafeZoneAction action)
    {
        SafeZoneManager[] safeZones = FindObjectsOfType<SafeZoneManager>();
        
        foreach (var safeZone in safeZones)
        {
            float distance = Vector3.Distance(safeZone.transform.position, position);
            if (distance <= safeZone.safeZoneRadius)
            {
                switch (action)
                {
                    case SafeZoneAction.PvP:
                        return !safeZone.preventPvP;
                    case SafeZoneAction.Building:
                        return !safeZone.preventBuilding;
                    case SafeZoneAction.Raiding:
                        return !safeZone.preventRaiding;
                }
            }
        }
        
        return true; // Action allowed if not in any safe zone
    }

    // Gizmos for editor visualization
    void OnDrawGizmosSelected()
    {
        Gizmos.color = safeZoneColor;
        Gizmos.DrawWireSphere(transform.position, safeZoneRadius);
        
        Gizmos.color = new Color(safeZoneColor.r, safeZoneColor.g, safeZoneColor.b, 0.1f);
        Gizmos.DrawSphere(transform.position, safeZoneRadius);
    }
}

public enum SafeZoneAction
{
    PvP,
    Building,
    Raiding
}

[System.Serializable]
public class SafeZoneNPC
{
    public string npcName;
    public GameObject npcPrefab;
    public Vector3 spawnPosition;
    public NPCRole role;
}

public enum NPCRole
{
    Shopkeeper,
    Gambling_Dealer,
    Recycler_Operator,
    Guard,
    Information
}