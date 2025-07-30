using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class ResourceNode : MonoBehaviour, IInteractable
{
    [Header("Resource Configuration")]
    public ResourceData resourceData;
    public int totalAmount = 10;
    public float respawnTime = 300f; // 5 minutes in seconds
    public bool canRespawn = true;
    
    [Header("Visual Feedback")]
    public GameObject gatherProgressUI;
    public Slider gatherProgressSlider;
    public Canvas worldCanvas;
    
    [Header("State Visuals")]
    public GameObject[] visualStages; // Different visual stages as resource depletes
    public Material depletedMaterial;
    
    private int currentAmount;
    private bool isBeingGathered = false;
    private bool isDepleted = false;
    private PlayerController gathererPlayer;
    private Coroutine gatherCoroutine;
    private Renderer nodeRenderer;
    private Material originalMaterial;
    
    // Events
    public System.Action<ResourceData, int> OnResourceGathered;
    public System.Action<ResourceNode> OnResourceDepleted;

    void Start()
    {
        Initialize();
    }

    void Initialize()
    {
        currentAmount = totalAmount;
        nodeRenderer = GetComponent<Renderer>();
        if (nodeRenderer != null)
            originalMaterial = nodeRenderer.material;
            
        UpdateVisualState();
        
        if (gatherProgressUI != null)
            gatherProgressUI.SetActive(false);
            
        // Setup world canvas to face camera
        if (worldCanvas != null)
        {
            worldCanvas.worldCamera = Camera.main;
        }
    }

    public bool CanInteract(PlayerController player)
    {
        if (isDepleted || isBeingGathered) return false;
        
        float distance = Vector3.Distance(transform.position, player.transform.position);
        return distance <= resourceData.gatherRadius;
    }

    public string GetInteractionText()
    {
        if (isDepleted) return "Depleted";
        if (isBeingGathered) return "Gathering...";
        
        string toolText = resourceData.requiredTool != ToolType.None ? 
            $" (Requires {resourceData.requiredTool})" : "";
        return $"Gather {resourceData.resourceName}{toolText}";
    }

    public void Interact(PlayerController player)
    {
        if (!CanInteract(player)) return;
        
        // Check if player has required tool
        InventoryManager inventory = player.GetComponent<InventoryManager>();
        if (inventory != null && resourceData.requiredTool != ToolType.None)
        {
            if (!inventory.HasTool(resourceData.requiredTool))
            {
                ShowMessage($"You need a {resourceData.requiredTool} to gather this resource.");
                return;
            }
        }
        
        StartGathering(player);
    }

    void StartGathering(PlayerController player)
    {
        if (isBeingGathered) return;
        
        isBeingGathered = true;
        gathererPlayer = player;
        
        if (gatherProgressUI != null)
        {
            gatherProgressUI.SetActive(true);
            gatherProgressSlider.value = 0f;
        }
        
        // Play gathering sound
        if (resourceData.gatherSound != null)
        {
            AudioSource.PlayClipAtPoint(resourceData.gatherSound, transform.position);
        }
        
        // Start gathering effect
        if (resourceData.gatherEffect != null)
        {
            Instantiate(resourceData.gatherEffect, transform.position, Quaternion.identity);
        }
        
        gatherCoroutine = StartCoroutine(GatheringProcess());
    }

    IEnumerator GatheringProcess()
    {
        float elapsedTime = 0f;
        float gatherTime = resourceData.gatherTime;
        
        // Apply tool efficiency if player has the right tool
        InventoryManager inventory = gathererPlayer.GetComponent<InventoryManager>();
        if (inventory != null)
        {
            float efficiency = inventory.GetToolEfficiency(resourceData.requiredTool);
            gatherTime = gatherTime / efficiency;
        }
        
        while (elapsedTime < gatherTime)
        {
            // Check if player is still in range and not moving too much
            if (gathererPlayer == null || 
                Vector3.Distance(transform.position, gathererPlayer.transform.position) > resourceData.gatherRadius ||
                gathererPlayer.GetCurrentSpeed() > 1f)
            {
                CancelGathering();
                yield break;
            }
            
            elapsedTime += Time.deltaTime;
            
            if (gatherProgressSlider != null)
            {
                gatherProgressSlider.value = elapsedTime / gatherTime;
            }
            
            yield return null;
        }
        
        CompleteGathering();
    }

    void CompleteGathering()
    {
        int gatherAmount = Random.Range(resourceData.minGatherAmount, resourceData.maxGatherAmount + 1);
        gatherAmount = Mathf.Min(gatherAmount, currentAmount);
        
        // Give resources to player
        InventoryManager inventory = gathererPlayer.GetComponent<InventoryManager>();
        if (inventory != null)
        {
            inventory.AddItem(resourceData, gatherAmount);
        }
        
        // Update resource amount
        currentAmount -= gatherAmount;
        
        // Play completion sound
        if (resourceData.gatherCompleteSound != null)
        {
            AudioSource.PlayClipAtPoint(resourceData.gatherCompleteSound, transform.position);
        }
        
        // Trigger events
        OnResourceGathered?.Invoke(resourceData, gatherAmount);
        
        // Check if depleted
        if (currentAmount <= 0)
        {
            SetDepleted();
        }
        else
        {
            UpdateVisualState();
        }
        
        ResetGatheringState();
        
        // Show gathered amount to player
        ShowFloatingText($"+{gatherAmount} {resourceData.resourceName}");
    }

    void CancelGathering()
    {
        ResetGatheringState();
        ShowMessage("Gathering cancelled.");
    }

    void ResetGatheringState()
    {
        isBeingGathered = false;
        gathererPlayer = null;
        
        if (gatherCoroutine != null)
        {
            StopCoroutine(gatherCoroutine);
            gatherCoroutine = null;
        }
        
        if (gatherProgressUI != null)
        {
            gatherProgressUI.SetActive(false);
        }
    }

    void SetDepleted()
    {
        isDepleted = true;
        currentAmount = 0;
        
        // Change visual appearance
        if (nodeRenderer != null && depletedMaterial != null)
        {
            nodeRenderer.material = depletedMaterial;
        }
        
        // Play depleted effect
        if (resourceData.depletedEffect != null)
        {
            Instantiate(resourceData.depletedEffect, transform.position, Quaternion.identity);
        }
        
        // Trigger event
        OnResourceDepleted?.Invoke(this);
        
        // Start respawn timer if applicable
        if (canRespawn && respawnTime > 0)
        {
            StartCoroutine(RespawnTimer());
        }
        
        UpdateVisualState();
    }

    IEnumerator RespawnTimer()
    {
        yield return new WaitForSeconds(respawnTime);
        Respawn();
    }

    void Respawn()
    {
        isDepleted = false;
        currentAmount = totalAmount;
        
        // Restore original material
        if (nodeRenderer != null && originalMaterial != null)
        {
            nodeRenderer.material = originalMaterial;
        }
        
        UpdateVisualState();
    }

    void UpdateVisualState()
    {
        if (isDepleted)
        {
            // Show depleted state
            foreach (GameObject stage in visualStages)
            {
                if (stage != null) stage.SetActive(false);
            }
            return;
        }
        
        // Show appropriate visual stage based on remaining amount
        if (visualStages.Length > 0)
        {
            float amountPercentage = (float)currentAmount / totalAmount;
            int stageIndex = Mathf.FloorToInt(amountPercentage * visualStages.Length);
            stageIndex = Mathf.Clamp(stageIndex, 0, visualStages.Length - 1);
            
            for (int i = 0; i < visualStages.Length; i++)
            {
                if (visualStages[i] != null)
                {
                    visualStages[i].SetActive(i == stageIndex);
                }
            }
        }
    }

    void ShowMessage(string message)
    {
        // This would integrate with a message system
        Debug.Log(message);
    }

    void ShowFloatingText(string text)
    {
        // This would show floating damage/gain text
        // Could integrate with a floating text system
        Debug.Log(text);
    }

    // Editor/Debug methods
    void OnDrawGizmosSelected()
    {
        if (resourceData != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, resourceData.gatherRadius);
        }
    }

    public int GetCurrentAmount()
    {
        return currentAmount;
    }

    public float GetAmountPercentage()
    {
        return totalAmount > 0 ? (float)currentAmount / totalAmount : 0f;
    }

    public bool IsFullyDepleted()
    {
        return isDepleted;
    }
}