using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class RaidingManager : MonoBehaviour
{
    [Header("Raiding Weapons")]
    public GameObject c4Prefab;
    public GameObject rocketPrefab;
    public GameObject molotovPrefab;
    public GameObject grenadePrefab;
    public GameObject satchelPrefab;
    
    [Header("Deployment Settings")]
    public float throwForce = 10f;
    public float maxThrowDistance = 15f;
    public LayerMask targetLayers = -1;
    
    [Header("UI Elements")]
    public GameObject raidingUI;
    public Button c4Button;
    public Button rocketButton;
    public Button molotovButton;
    public Text selectedWeaponText;
    public Text ammoCountText;
    
    [Header("Targeting")]
    public GameObject targetIndicator;
    public LineRenderer trajectoryLine;
    public int trajectoryPoints = 30;
    
    private PlayerController player;
    private InventoryManager inventory;
    private RaidingWeaponType selectedWeapon = RaidingWeaponType.C4;
    private bool isAiming = false;
    private Camera playerCamera;

    void Start()
    {
        player = GetComponent<PlayerController>();
        inventory = GetComponent<InventoryManager>();
        playerCamera = Camera.main;
        
        SetupUI();
        UpdateAmmoDisplay();
    }

    void Update()
    {
        if (isAiming)
        {
            HandleAiming();
        }
        
        HandleInput();
    }

    void SetupUI()
    {
        if (raidingUI != null)
            raidingUI.SetActive(false);
            
        if (c4Button != null)
            c4Button.onClick.AddListener(() => SelectWeapon(RaidingWeaponType.C4));
            
        if (rocketButton != null)
            rocketButton.onClick.AddListener(() => SelectWeapon(RaidingWeaponType.Rocket));
            
        if (molotovButton != null)
            molotovButton.onClick.AddListener(() => SelectWeapon(RaidingWeaponType.Molotov));
            
        if (targetIndicator != null)
            targetIndicator.SetActive(false);
            
        if (trajectoryLine != null)
            trajectoryLine.enabled = false;
    }

    void HandleInput()
    {
        // Toggle raiding UI
        if (Input.GetKeyDown(KeyCode.R))
        {
            ToggleRaidingUI();
        }
        
        // Start aiming when holding right mouse button (or long press on mobile)
        if (Input.GetMouseButtonDown(1) && HasSelectedWeapon())
        {
            StartAiming();
        }
        
        // Deploy weapon when releasing right mouse button
        if (Input.GetMouseButtonUp(1) && isAiming)
        {
            DeployWeapon();
        }
        
        // Cancel aiming
        if (Input.GetKeyDown(KeyCode.Escape) && isAiming)
        {
            CancelAiming();
        }
    }

    void HandleAiming()
    {
        // Cast ray from camera to determine target point
        Ray ray = playerCamera.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        
        Vector3 targetPoint;
        if (Physics.Raycast(ray, out hit, maxThrowDistance, targetLayers))
        {
            targetPoint = hit.point;
        }
        else
        {
            targetPoint = ray.GetPoint(maxThrowDistance);
        }
        
        // Update target indicator
        if (targetIndicator != null)
        {
            targetIndicator.transform.position = targetPoint;
            
            // Change color based on whether target is valid
            Renderer targetRenderer = targetIndicator.GetComponent<Renderer>();
            if (targetRenderer != null)
            {
                bool validTarget = IsValidTarget(hit.collider);
                targetRenderer.material.color = validTarget ? Color.green : Color.red;
            }
        }
        
        // Update trajectory line
        UpdateTrajectoryLine(targetPoint);
    }

    void UpdateTrajectoryLine(Vector3 targetPoint)
    {
        if (trajectoryLine == null) return;
        
        Vector3 startPosition = transform.position + Vector3.up * 1.5f; // Hand position
        Vector3 direction = (targetPoint - startPosition).normalized;
        float distance = Vector3.Distance(startPosition, targetPoint);
        
        Vector3[] points = new Vector3[trajectoryPoints];
        
        for (int i = 0; i < trajectoryPoints; i++)
        {
            float t = (float)i / (trajectoryPoints - 1);
            Vector3 point = startPosition + direction * distance * t;
            
            // Add gravity effect for thrown weapons
            if (selectedWeapon != RaidingWeaponType.Rocket)
            {
                float gravity = Physics.gravity.y * t * t * 0.5f;
                point.y += gravity;
            }
            
            points[i] = point;
        }
        
        trajectoryLine.positionCount = trajectoryPoints;
        trajectoryLine.SetPositions(points);
    }

    bool IsValidTarget(Collider targetCollider)
    {
        if (targetCollider == null) return false;
        
        // Check if target is a building
        BuildingPiece building = targetCollider.GetComponent<BuildingPiece>();
        if (building != null)
        {
            // Check if player can damage this building (authorization)
            ToolCupboard cupboard = building.authorizedCupboard;
            if (cupboard != null && cupboard.IsActive())
            {
                string playerId = player.name; // This would be actual player ID
                return !cupboard.IsPlayerAuthorized(playerId);
            }
            return true; // Can damage if no cupboard protection
        }
        
        // Check if target is a player or sleeper
        PlayerController targetPlayer = targetCollider.GetComponent<PlayerController>();
        SleeperController sleeper = targetCollider.GetComponent<SleeperController>();
        
        return targetPlayer != null || sleeper != null;
    }

    public void ToggleRaidingUI()
    {
        if (raidingUI != null)
        {
            bool newState = !raidingUI.activeInHierarchy;
            raidingUI.SetActive(newState);
            
            if (newState)
            {
                UpdateAmmoDisplay();
            }
        }
    }

    public void SelectWeapon(RaidingWeaponType weaponType)
    {
        selectedWeapon = weaponType;
        
        if (selectedWeaponText != null)
            selectedWeaponText.text = GetWeaponName(weaponType);
            
        UpdateAmmoDisplay();
    }

    string GetWeaponName(RaidingWeaponType type)
    {
        switch (type)
        {
            case RaidingWeaponType.C4: return "C4 Explosive";
            case RaidingWeaponType.Rocket: return "Rocket";
            case RaidingWeaponType.Molotov: return "Molotov Cocktail";
            case RaidingWeaponType.Grenade: return "F1 Grenade";
            case RaidingWeaponType.Satchel: return "Satchel Charge";
            default: return "Unknown";
        }
    }

    void UpdateAmmoDisplay()
    {
        if (ammoCountText == null || inventory == null) return;
        
        // Count available raiding weapons in inventory
        int count = GetWeaponCount(selectedWeapon);
        ammoCountText.text = $"Available: {count}";
        
        // Update button availability
        UpdateButtonStates();
    }

    void UpdateButtonStates()
    {
        if (c4Button != null)
            c4Button.interactable = GetWeaponCount(RaidingWeaponType.C4) > 0;
            
        if (rocketButton != null)
            rocketButton.interactable = GetWeaponCount(RaidingWeaponType.Rocket) > 0;
            
        if (molotovButton != null)
            molotovButton.interactable = GetWeaponCount(RaidingWeaponType.Molotov) > 0;
    }

    int GetWeaponCount(RaidingWeaponType weaponType)
    {
        if (inventory == null) return 0;
        
        // This would check inventory for specific raiding weapon items
        // For now, return a simulated count
        switch (weaponType)
        {
            case RaidingWeaponType.C4:
                return GetItemCount("C4 Explosive");
            case RaidingWeaponType.Rocket:
                return GetItemCount("High Velocity Rocket");
            case RaidingWeaponType.Molotov:
                return GetItemCount("Molotov Cocktail");
            default:
                return 0;
        }
    }

    int GetItemCount(string itemName)
    {
        // This would interface with the actual inventory system
        // For simulation purposes, return random values
        return Random.Range(0, 5);
    }

    bool HasSelectedWeapon()
    {
        return GetWeaponCount(selectedWeapon) > 0;
    }

    void StartAiming()
    {
        if (!HasSelectedWeapon()) return;
        
        isAiming = true;
        
        if (targetIndicator != null)
            targetIndicator.SetActive(true);
            
        if (trajectoryLine != null)
            trajectoryLine.enabled = true;
    }

    void CancelAiming()
    {
        isAiming = false;
        
        if (targetIndicator != null)
            targetIndicator.SetActive(false);
            
        if (trajectoryLine != null)
            trajectoryLine.enabled = false;
    }

    void DeployWeapon()
    {
        if (!isAiming || !HasSelectedWeapon()) return;
        
        // Get target position
        Ray ray = playerCamera.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        Vector3 targetPoint;
        
        if (Physics.Raycast(ray, out hit, maxThrowDistance, targetLayers))
        {
            targetPoint = hit.point;
        }
        else
        {
            targetPoint = ray.GetPoint(maxThrowDistance);
        }
        
        // Spawn and deploy the weapon
        GameObject weaponPrefab = GetWeaponPrefab(selectedWeapon);
        if (weaponPrefab != null)
        {
            Vector3 spawnPosition = transform.position + Vector3.up * 1.5f;
            GameObject weaponInstance = Instantiate(weaponPrefab, spawnPosition, Quaternion.identity);
            
            // Calculate throw force
            Vector3 direction = (targetPoint - spawnPosition).normalized;
            Vector3 force = direction * throwForce;
            
            // Initialize the weapon
            RaidingWeapon weapon = weaponInstance.GetComponent<RaidingWeapon>();
            if (weapon != null)
            {
                weapon.Initialize(player, force);
            }
            
            // Consume weapon from inventory
            ConsumeWeapon(selectedWeapon);
            
            // Log raid attempt
            LogRaidAttempt(selectedWeapon, targetPoint);
        }
        
        CancelAiming();
        UpdateAmmoDisplay();
    }

    GameObject GetWeaponPrefab(RaidingWeaponType weaponType)
    {
        switch (weaponType)
        {
            case RaidingWeaponType.C4: return c4Prefab;
            case RaidingWeaponType.Rocket: return rocketPrefab;
            case RaidingWeaponType.Molotov: return molotovPrefab;
            case RaidingWeaponType.Grenade: return grenadePrefab;
            case RaidingWeaponType.Satchel: return satchelPrefab;
            default: return null;
        }
    }

    void ConsumeWeapon(RaidingWeaponType weaponType)
    {
        // This would remove the weapon from inventory
        string weaponName = GetWeaponName(weaponType);
        Debug.Log($"Consumed 1x {weaponName}");
        
        // In a real implementation, this would call:
        // inventory.RemoveItem(weaponItemData, 1);
    }

    void LogRaidAttempt(RaidingWeaponType weaponType, Vector3 targetPosition)
    {
        // Log the raid attempt for statistics and admin monitoring
        Debug.Log($"Player {player.name} used {GetWeaponName(weaponType)} at {targetPosition}");
        
        // This could also send to a server logging system in multiplayer
        RaidEventData raidEvent = new RaidEventData
        {
            attackerId = player.name,
            weaponType = weaponType,
            targetPosition = targetPosition,
            timestamp = System.DateTime.Now
        };
        
        // Send to raid tracking system
        SendRaidEvent(raidEvent);
    }

    void SendRaidEvent(RaidEventData raidEvent)
    {
        // This would send the raid event to server for tracking
        // Could be used for admin logs, statistics, etc.
    }

    // Public methods for mobile UI integration
    public void OnMobileAimStart()
    {
        if (HasSelectedWeapon())
            StartAiming();
    }

    public void OnMobileAimEnd()
    {
        if (isAiming)
            DeployWeapon();
    }

    public void OnMobileAimCancel()
    {
        CancelAiming();
    }
}

[System.Serializable]
public class RaidEventData
{
    public string attackerId;
    public RaidingWeaponType weaponType;
    public Vector3 targetPosition;
    public System.DateTime timestamp;
}