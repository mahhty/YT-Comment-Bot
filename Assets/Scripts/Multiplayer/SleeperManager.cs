using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class SleeperManager : MonoBehaviour
{
    [Header("Sleeper Settings")]
    public GameObject sleeperPrefab;
    public float sleeperSpawnHeight = 0.5f;
    public float sleeperDespawnTime = 300f; // 5 minutes before despawn if in safe zone
    public LayerMask groundLayer = 1;
    
    [Header("Safe Zones")]
    public List<Transform> safeZones = new List<Transform>();
    public float safeZoneRadius = 50f;
    
    [Header("Loot Protection")]
    public float lootProtectionTime = 30f; // Seconds after disconnect before sleeper can be looted
    public bool allowSleeperLooting = true;
    
    private Dictionary<string, SleeperData> activeSleepers = new Dictionary<string, SleeperData>();
    private Dictionary<string, PlayerInventoryData> disconnectedPlayerData = new Dictionary<string, PlayerInventoryData>();

    void Start()
    {
        // Subscribe to player connection events
        // This would integrate with your networking solution
    }

    public void OnPlayerDisconnected(string playerId, Vector3 lastPosition, PlayerInventoryData inventoryData, SurvivalData survivalData)
    {
        // Don't create sleeper if player is already dead
        if (survivalData.currentHealth <= 0f)
        {
            // Just store inventory for potential reconnect
            disconnectedPlayerData[playerId] = inventoryData;
            return;
        }
        
        // Check if in safe zone
        bool inSafeZone = IsInSafeZone(lastPosition);
        
        if (inSafeZone)
        {
            // In safe zone - don't create sleeper, just store data temporarily
            disconnectedPlayerData[playerId] = inventoryData;
            StartCoroutine(CleanupDisconnectedPlayerData(playerId, sleeperDespawnTime));
        }
        else
        {
            // Create sleeper in dangerous area
            CreateSleeper(playerId, lastPosition, inventoryData, survivalData);
        }
    }

    public void OnPlayerReconnected(string playerId)
    {
        // Check if player has an active sleeper
        if (activeSleepers.ContainsKey(playerId))
        {
            SleeperData sleeperData = activeSleepers[playerId];
            
            // Restore player at sleeper position with current health/inventory
            RestorePlayerFromSleeper(playerId, sleeperData);
            
            // Remove sleeper
            DestroySleeper(playerId);
        }
        else if (disconnectedPlayerData.ContainsKey(playerId))
        {
            // Player was in safe zone, restore their data
            // This would integrate with your player spawning system
            Debug.Log($"Restoring player {playerId} from safe zone disconnect");
            disconnectedPlayerData.Remove(playerId);
        }
    }

    void CreateSleeper(string playerId, Vector3 position, PlayerInventoryData inventoryData, SurvivalData survivalData)
    {
        // Find ground position
        Vector3 sleeperPosition = FindGroundPosition(position);
        
        // Instantiate sleeper prefab
        GameObject sleeperObject = Instantiate(sleeperPrefab, sleeperPosition, Quaternion.identity);
        
        // Setup sleeper component
        SleeperController sleeper = sleeperObject.GetComponent<SleeperController>();
        if (sleeper == null)
            sleeper = sleeperObject.AddComponent<SleeperController>();
            
        sleeper.Initialize(playerId, inventoryData, survivalData, lootProtectionTime);
        
        // Store sleeper data
        SleeperData sleeperData = new SleeperData
        {
            playerId = playerId,
            sleeperObject = sleeperObject,
            sleeper = sleeper,
            inventoryData = inventoryData,
            survivalData = survivalData,
            disconnectTime = Time.time,
            canBeLootedTime = Time.time + lootProtectionTime
        };
        
        activeSleepers[playerId] = sleeperData;
        
        Debug.Log($"Created sleeper for player {playerId} at {sleeperPosition}");
    }

    Vector3 FindGroundPosition(Vector3 position)
    {
        RaycastHit hit;
        if (Physics.Raycast(position + Vector3.up * 5f, Vector3.down, out hit, 10f, groundLayer))
        {
            return hit.point + Vector3.up * sleeperSpawnHeight;
        }
        return position; // Fallback to original position
    }

    bool IsInSafeZone(Vector3 position)
    {
        foreach (Transform safeZone in safeZones)
        {
            if (Vector3.Distance(position, safeZone.position) <= safeZoneRadius)
                return true;
        }
        return false;
    }

    void RestorePlayerFromSleeper(string playerId, SleeperData sleeperData)
    {
        // This would integrate with your player spawning/restoration system
        Vector3 respawnPosition = sleeperData.sleeperObject.transform.position;
        
        // Update survival data with any changes that occurred while sleeping
        SurvivalData currentSurvival = sleeperData.sleeper.GetCurrentSurvivalData();
        
        Debug.Log($"Restoring player {playerId} at sleeper position {respawnPosition}");
        Debug.Log($"Health: {currentSurvival.currentHealth}, Hunger: {currentSurvival.currentHunger}, Thirst: {currentSurvival.currentThirst}");
        
        // The actual player restoration would be handled by your networking/player management system
        // This just provides the data needed for restoration
    }

    public void DestroySleeper(string playerId)
    {
        if (activeSleepers.ContainsKey(playerId))
        {
            SleeperData sleeperData = activeSleepers[playerId];
            
            if (sleeperData.sleeperObject != null)
                Destroy(sleeperData.sleeperObject);
                
            activeSleepers.Remove(playerId);
            Debug.Log($"Destroyed sleeper for player {playerId}");
        }
    }

    public void OnSleeperKilled(string playerId, string killerId)
    {
        if (activeSleepers.ContainsKey(playerId))
        {
            SleeperData sleeperData = activeSleepers[playerId];
            
            // Drop loot
            DropSleeperLoot(sleeperData.sleeperObject.transform.position, sleeperData.inventoryData);
            
            // Log kill for stats/notifications
            Debug.Log($"Sleeper {playerId} was killed by {killerId}");
            
            // Remove sleeper
            DestroySleeper(playerId);
            
            // Mark player as dead for when they reconnect
            disconnectedPlayerData[playerId] = new PlayerInventoryData(); // Empty inventory
        }
    }

    void DropSleeperLoot(Vector3 position, PlayerInventoryData inventoryData)
    {
        // Create loot bag or drop items
        // This would integrate with your item drop system
        Debug.Log($"Dropping sleeper loot at {position}");
        
        // In a real implementation, you'd spawn item pickups for each inventory item
        foreach (var item in inventoryData.items)
        {
            if (!item.IsEmpty())
            {
                // SpawnItemPickup(item, position + Random.insideUnitSphere);
                Debug.Log($"Dropped {item.amount}x {item.itemData.GetDisplayName()}");
            }
        }
    }

    IEnumerator CleanupDisconnectedPlayerData(string playerId, float delay)
    {
        yield return new WaitForSeconds(delay);
        
        if (disconnectedPlayerData.ContainsKey(playerId))
        {
            disconnectedPlayerData.Remove(playerId);
            Debug.Log($"Cleaned up disconnected player data for {playerId}");
        }
    }

    // Update sleepers over time (hunger, thirst, health decay)
    void Update()
    {
        foreach (var kvp in activeSleepers)
        {
            SleeperData sleeperData = kvp.Value;
            if (sleeperData.sleeper != null)
            {
                sleeperData.sleeper.UpdateSleeper();
            }
        }
    }

    public List<SleeperData> GetNearbySleepers(Vector3 position, float radius)
    {
        List<SleeperData> nearbySleepers = new List<SleeperData>();
        
        foreach (var kvp in activeSleepers)
        {
            SleeperData sleeperData = kvp.Value;
            if (sleeperData.sleeperObject != null)
            {
                float distance = Vector3.Distance(position, sleeperData.sleeperObject.transform.position);
                if (distance <= radius)
                {
                    nearbySleepers.Add(sleeperData);
                }
            }
        }
        
        return nearbySleepers;
    }

    public bool HasSleeper(string playerId)
    {
        return activeSleepers.ContainsKey(playerId);
    }

    public SleeperData GetSleeperData(string playerId)
    {
        return activeSleepers.ContainsKey(playerId) ? activeSleepers[playerId] : null;
    }
}

[System.Serializable]
public class SleeperData
{
    public string playerId;
    public GameObject sleeperObject;
    public SleeperController sleeper;
    public PlayerInventoryData inventoryData;
    public SurvivalData survivalData;
    public float disconnectTime;
    public float canBeLootedTime;
}

[System.Serializable]
public class PlayerInventoryData
{
    public List<ItemStack> items = new List<ItemStack>();
    public List<ItemStack> hotbarItems = new List<ItemStack>();
    public List<ItemStack> armorItems = new List<ItemStack>();
}

[System.Serializable]
public class SurvivalData
{
    public float currentHealth = 100f;
    public float currentHunger = 100f;
    public float currentThirst = 100f;
    public float currentRadiation = 0f;
    public float currentTemperature = 20f;
}