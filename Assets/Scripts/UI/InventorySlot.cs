using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;

public class InventorySlot : MonoBehaviour, IPointerClickHandler, IPointerDownHandler, IPointerUpHandler, IDropHandler
{
    [Header("UI References")]
    public Image itemIcon;
    public Text amountText;
    public Image backgroundImage;
    public Image highlightImage;
    
    [Header("Visual Settings")]
    public Color normalColor = Color.white;
    public Color selectedColor = Color.yellow;
    public Color emptySlotColor = Color.gray;
    
    private int slotIndex;
    private InventoryManager inventoryManager;
    private ItemStack currentItem;
    private bool isHotbarSlot = false;
    
    // Touch handling
    private bool isPressed = false;
    private float pressStartTime;
    private Coroutine longPressCoroutine;

    public void Initialize(int index, InventoryManager manager, bool hotbar = false)
    {
        slotIndex = index;
        inventoryManager = manager;
        isHotbarSlot = hotbar;
        
        SetItem(new ItemStack());
    }

    public void SetItem(ItemStack item)
    {
        currentItem = item;
        UpdateVisuals();
    }

    void UpdateVisuals()
    {
        if (currentItem == null || currentItem.IsEmpty())
        {
            // Empty slot
            if (itemIcon != null)
            {
                itemIcon.sprite = null;
                itemIcon.color = new Color(1, 1, 1, 0); // Transparent
            }
            
            if (amountText != null)
                amountText.text = "";
                
            if (backgroundImage != null)
                backgroundImage.color = emptySlotColor;
        }
        else
        {
            // Slot with item
            if (itemIcon != null && currentItem.itemData.icon != null)
            {
                itemIcon.sprite = currentItem.itemData.icon;
                itemIcon.color = Color.white;
            }
            
            if (amountText != null)
            {
                if (currentItem.amount > 1)
                    amountText.text = currentItem.amount.ToString();
                else
                    amountText.text = "";
            }
            
            if (backgroundImage != null)
                backgroundImage.color = normalColor;
        }
        
        // Update highlight
        if (highlightImage != null)
            highlightImage.gameObject.SetActive(false);
    }

    public void SetSelected(bool selected)
    {
        if (highlightImage != null)
        {
            highlightImage.gameObject.SetActive(selected);
            highlightImage.color = selectedColor;
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (inventoryManager != null)
        {
            inventoryManager.OnSlotClicked(this);
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        isPressed = true;
        pressStartTime = Time.time;
        
        // Start long press detection
        if (longPressCoroutine != null)
            StopCoroutine(longPressCoroutine);
        longPressCoroutine = StartCoroutine(DetectLongPress());
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        isPressed = false;
        
        if (longPressCoroutine != null)
        {
            StopCoroutine(longPressCoroutine);
            longPressCoroutine = null;
        }
    }

    IEnumerator DetectLongPress()
    {
        yield return new WaitForSeconds(inventoryManager.longPressTime);
        
        if (isPressed && inventoryManager != null)
        {
            inventoryManager.OnSlotLongPress(this);
        }
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (inventoryManager != null)
        {
            inventoryManager.OnSlotDrop(this);
        }
    }

    public int GetSlotIndex()
    {
        return slotIndex;
    }

    public ItemStack GetItem()
    {
        return currentItem;
    }

    public bool IsEmpty()
    {
        return currentItem == null || currentItem.IsEmpty();
    }

    public bool IsHotbarSlot()
    {
        return isHotbarSlot;
    }

    // Animation methods for visual feedback
    public void PlaySelectAnimation()
    {
        // Simple scale animation
        LeanTween.scale(gameObject, Vector3.one * 1.1f, 0.1f)
                 .setEase(LeanTweenType.easeOutQuad)
                 .setLoopPingPong(1);
    }

    public void PlayPickupAnimation()
    {
        // Bounce animation when item is added
        transform.localScale = Vector3.one * 0.8f;
        LeanTween.scale(gameObject, Vector3.one, 0.2f)
                 .setEase(LeanTweenType.easeOutBounce);
    }
}