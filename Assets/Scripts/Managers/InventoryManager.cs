using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;

public class InventoryManager : MonoBehaviour
{
    [Header("Inventory Settings")]
    public int inventorySize = 20;
    public int hotbarSize = 6;
    
    [Header("UI References")]
    public GameObject inventoryPanel;
    public Transform inventoryGrid;
    public Transform hotbarGrid;
    public GameObject inventorySlotPrefab;
    public Text inventoryTitle;
    
    [Header("Item Info Panel")]
    public GameObject itemInfoPanel;
    public Image itemInfoIcon;
    public Text itemInfoName;
    public Text itemInfoDescription;
    public Text itemInfoAmount;
    public Button useButton;
    public Button dropButton;
    public Button splitButton;
    
    [Header("Mobile Controls")]
    public float longPressTime = 0.5f;
    public GameObject dragPreview;
    
    private List<InventorySlot> inventorySlots = new List<InventorySlot>();
    private List<InventorySlot> hotbarSlots = new List<InventorySlot>();
    private List<ItemStack> items = new List<ItemStack>();
    
    private bool isInventoryOpen = false;
    private InventorySlot selectedSlot;
    private ItemStack draggedItem;
    private bool isDragging = false;
    
    // Tool efficiency cache
    private Dictionary<ToolType, float> toolEfficiencyCache = new Dictionary<ToolType, float>();

    void Start()
    {
        InitializeInventory();
        SetupUI();
        RefreshToolEfficiency();
    }

    void InitializeInventory()
    {
        // Initialize item stacks
        for (int i = 0; i < inventorySize; i++)
        {
            items.Add(new ItemStack());
        }
        
        // Create inventory slots
        for (int i = 0; i < inventorySize; i++)
        {
            GameObject slotGO = Instantiate(inventorySlotPrefab, inventoryGrid);
            InventorySlot slot = slotGO.GetComponent<InventorySlot>();
            slot.Initialize(i, this);
            inventorySlots.Add(slot);
        }
        
        // Create hotbar slots
        for (int i = 0; i < hotbarSize; i++)
        {
            GameObject slotGO = Instantiate(inventorySlotPrefab, hotbarGrid);
            InventorySlot slot = slotGO.GetComponent<InventorySlot>();
            slot.Initialize(i, this, true);
            hotbarSlots.Add(slot);
        }
    }

    void SetupUI()
    {
        if (inventoryPanel != null)
            inventoryPanel.SetActive(false);
            
        if (itemInfoPanel != null)
            itemInfoPanel.SetActive(false);
            
        // Setup button events
        if (useButton != null)
            useButton.onClick.AddListener(UseSelectedItem);
            
        if (dropButton != null)
            dropButton.onClick.AddListener(DropSelectedItem);
            
        if (splitButton != null)
            splitButton.onClick.AddListener(SplitSelectedItem);
    }

    public void ToggleInventory()
    {
        isInventoryOpen = !isInventoryOpen;
        
        if (inventoryPanel != null)
            inventoryPanel.SetActive(isInventoryOpen);
            
        if (isInventoryOpen)
        {
            RefreshInventoryDisplay();
        }
        else
        {
            HideItemInfo();
        }
    }

    public bool AddItem(ResourceData resourceData, int amount)
    {
        return AddItem(new ItemData { resourceData = resourceData }, amount);
    }

    public bool AddItem(ItemData itemData, int amount)
    {
        int remainingAmount = amount;
        
        // First, try to stack with existing items
        for (int i = 0; i < items.Count && remainingAmount > 0; i++)
        {
            if (items[i].IsEmpty()) continue;
            
            if (items[i].itemData.CanStackWith(itemData))
            {
                int stackSpace = items[i].GetStackSpace();
                int addAmount = Mathf.Min(remainingAmount, stackSpace);
                
                items[i].amount += addAmount;
                remainingAmount -= addAmount;
            }
        }
        
        // Then, try to add to empty slots
        for (int i = 0; i < items.Count && remainingAmount > 0; i++)
        {
            if (items[i].IsEmpty())
            {
                int addAmount = Mathf.Min(remainingAmount, itemData.GetMaxStackSize());
                items[i] = new ItemStack(itemData, addAmount);
                remainingAmount -= addAmount;
            }
        }
        
        RefreshInventoryDisplay();
        RefreshToolEfficiency();
        
        return remainingAmount == 0;
    }

    public bool RemoveItem(ItemData itemData, int amount)
    {
        int remainingAmount = amount;
        
        for (int i = items.Count - 1; i >= 0 && remainingAmount > 0; i--)
        {
            if (items[i].IsEmpty() || !items[i].itemData.Equals(itemData)) continue;
            
            int removeAmount = Mathf.Min(remainingAmount, items[i].amount);
            items[i].amount -= removeAmount;
            remainingAmount -= removeAmount;
            
            if (items[i].amount <= 0)
            {
                items[i] = new ItemStack();
            }
        }
        
        RefreshInventoryDisplay();
        RefreshToolEfficiency();
        
        return remainingAmount == 0;
    }

    public bool HasItem(ItemData itemData, int amount = 1)
    {
        int totalAmount = 0;
        
        foreach (var item in items)
        {
            if (!item.IsEmpty() && item.itemData.Equals(itemData))
            {
                totalAmount += item.amount;
            }
        }
        
        return totalAmount >= amount;
    }

    public bool HasTool(ToolType toolType)
    {
        foreach (var item in items)
        {
            if (!item.IsEmpty() && item.itemData.toolType == toolType)
            {
                return true;
            }
        }
        return false;
    }

    public float GetToolEfficiency(ToolType toolType)
    {
        if (toolEfficiencyCache.ContainsKey(toolType))
            return toolEfficiencyCache[toolType];
        return 1f;
    }

    void RefreshToolEfficiency()
    {
        toolEfficiencyCache.Clear();
        
        foreach (var item in items)
        {
            if (!item.IsEmpty() && item.itemData.toolType != ToolType.None)
            {
                float efficiency = item.itemData.toolEfficiency;
                
                if (!toolEfficiencyCache.ContainsKey(item.itemData.toolType) ||
                    toolEfficiencyCache[item.itemData.toolType] < efficiency)
                {
                    toolEfficiencyCache[item.itemData.toolType] = efficiency;
                }
            }
        }
    }

    void RefreshInventoryDisplay()
    {
        // Update inventory slots
        for (int i = 0; i < inventorySlots.Count; i++)
        {
            if (i < items.Count)
            {
                inventorySlots[i].SetItem(items[i]);
            }
            else
            {
                inventorySlots[i].SetItem(new ItemStack());
            }
        }
        
        // Update hotbar slots (show first 6 items)
        for (int i = 0; i < hotbarSlots.Count; i++)
        {
            if (i < items.Count)
            {
                hotbarSlots[i].SetItem(items[i]);
            }
            else
            {
                hotbarSlots[i].SetItem(new ItemStack());
            }
        }
    }

    public void OnSlotClicked(InventorySlot slot)
    {
        if (isDragging) return;
        
        int slotIndex = slot.GetSlotIndex();
        if (slotIndex >= items.Count) return;
        
        ItemStack item = items[slotIndex];
        
        if (item.IsEmpty())
        {
            HideItemInfo();
            return;
        }
        
        selectedSlot = slot;
        ShowItemInfo(item);
    }

    public void OnSlotLongPress(InventorySlot slot)
    {
        int slotIndex = slot.GetSlotIndex();
        if (slotIndex >= items.Count || items[slotIndex].IsEmpty()) return;
        
        // Start dragging
        StartDrag(slot, items[slotIndex]);
    }

    void StartDrag(InventorySlot slot, ItemStack item)
    {
        isDragging = true;
        draggedItem = item;
        selectedSlot = slot;
        
        // Show drag preview
        if (dragPreview != null)
        {
            dragPreview.SetActive(true);
            Image previewImage = dragPreview.GetComponent<Image>();
            if (previewImage != null && item.itemData.icon != null)
            {
                previewImage.sprite = item.itemData.icon;
            }
        }
    }

    public void OnSlotDrop(InventorySlot targetSlot)
    {
        if (!isDragging || draggedItem == null) return;
        
        int sourceIndex = selectedSlot.GetSlotIndex();
        int targetIndex = targetSlot.GetSlotIndex();
        
        if (sourceIndex >= items.Count || targetIndex >= items.Count) return;
        
        // Swap or stack items
        ItemStack targetItem = items[targetIndex];
        
        if (targetItem.IsEmpty())
        {
            // Move item to empty slot
            items[targetIndex] = draggedItem;
            items[sourceIndex] = new ItemStack();
        }
        else if (targetItem.itemData.CanStackWith(draggedItem.itemData))
        {
            // Stack items
            int stackSpace = targetItem.GetStackSpace();
            int transferAmount = Mathf.Min(draggedItem.amount, stackSpace);
            
            targetItem.amount += transferAmount;
            draggedItem.amount -= transferAmount;
            
            items[targetIndex] = targetItem;
            
            if (draggedItem.amount <= 0)
            {
                items[sourceIndex] = new ItemStack();
            }
            else
            {
                items[sourceIndex] = draggedItem;
            }
        }
        else
        {
            // Swap items
            items[sourceIndex] = targetItem;
            items[targetIndex] = draggedItem;
        }
        
        EndDrag();
    }

    void EndDrag()
    {
        isDragging = false;
        draggedItem = null;
        
        if (dragPreview != null)
            dragPreview.SetActive(false);
            
        RefreshInventoryDisplay();
        RefreshToolEfficiency();
    }

    void ShowItemInfo(ItemStack item)
    {
        if (itemInfoPanel == null) return;
        
        itemInfoPanel.SetActive(true);
        
        if (itemInfoIcon != null && item.itemData.icon != null)
            itemInfoIcon.sprite = item.itemData.icon;
            
        if (itemInfoName != null)
            itemInfoName.text = item.itemData.GetDisplayName();
            
        if (itemInfoDescription != null)
            itemInfoDescription.text = item.itemData.GetDescription();
            
        if (itemInfoAmount != null)
            itemInfoAmount.text = $"Amount: {item.amount}";
            
        // Update button states
        if (useButton != null)
            useButton.interactable = item.itemData.IsUsable();
            
        if (splitButton != null)
            splitButton.interactable = item.amount > 1;
    }

    void HideItemInfo()
    {
        if (itemInfoPanel != null)
            itemInfoPanel.SetActive(false);
            
        selectedSlot = null;
    }

    void UseSelectedItem()
    {
        if (selectedSlot == null) return;
        
        int slotIndex = selectedSlot.GetSlotIndex();
        if (slotIndex >= items.Count || items[slotIndex].IsEmpty()) return;
        
        ItemStack item = items[slotIndex];
        
        // Use the item
        if (item.itemData.UseItem(GetComponent<PlayerController>()))
        {
            item.amount--;
            if (item.amount <= 0)
            {
                items[slotIndex] = new ItemStack();
                HideItemInfo();
            }
            else
            {
                ShowItemInfo(item);
            }
            
            RefreshInventoryDisplay();
        }
    }

    void DropSelectedItem()
    {
        if (selectedSlot == null) return;
        
        int slotIndex = selectedSlot.GetSlotIndex();
        if (slotIndex >= items.Count || items[slotIndex].IsEmpty()) return;
        
        ItemStack item = items[slotIndex];
        
        // Drop one item
        DropItem(item.itemData, 1);
        
        item.amount--;
        if (item.amount <= 0)
        {
            items[slotIndex] = new ItemStack();
            HideItemInfo();
        }
        else
        {
            ShowItemInfo(item);
        }
        
        RefreshInventoryDisplay();
    }

    void SplitSelectedItem()
    {
        if (selectedSlot == null) return;
        
        int slotIndex = selectedSlot.GetSlotIndex();
        if (slotIndex >= items.Count || items[slotIndex].IsEmpty() || items[slotIndex].amount <= 1) return;
        
        ItemStack item = items[slotIndex];
        int splitAmount = item.amount / 2;
        
        // Find empty slot for split
        for (int i = 0; i < items.Count; i++)
        {
            if (items[i].IsEmpty())
            {
                items[i] = new ItemStack(item.itemData, splitAmount);
                item.amount -= splitAmount;
                
                RefreshInventoryDisplay();
                ShowItemInfo(item);
                return;
            }
        }
    }

    void DropItem(ItemData itemData, int amount)
    {
        // Create item pickup in world
        Vector3 dropPosition = transform.position + transform.forward * 2f;
        
        // This would spawn an item pickup prefab in the world
        // For now, just log the action
        Debug.Log($"Dropped {amount} {itemData.GetDisplayName()} at {dropPosition}");
    }

    public ItemStack GetHotbarItem(int index)
    {
        if (index >= 0 && index < hotbarSize && index < items.Count)
            return items[index];
        return new ItemStack();
    }

    public void UseHotbarItem(int index)
    {
        ItemStack item = GetHotbarItem(index);
        if (!item.IsEmpty() && item.itemData.IsUsable())
        {
            if (item.itemData.UseItem(GetComponent<PlayerController>()))
            {
                item.amount--;
                if (item.amount <= 0)
                {
                    items[index] = new ItemStack();
                }
                RefreshInventoryDisplay();
            }
        }
    }
}