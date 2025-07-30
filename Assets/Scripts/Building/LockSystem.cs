using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class LockSystem : MonoBehaviour, IInteractable
{
    [Header("Lock Configuration")]
    public LockType lockType = LockType.CodeLock;
    public bool isLocked = false;
    public bool hasOwner = false;
    public string ownerID = "";
    
    [Header("Code Lock Settings")]
    public string currentCode = "";
    public int maxCodeLength = 4;
    public int maxAttempts = 3;
    public float lockoutDuration = 30f;
    
    [Header("Key Lock Settings")]
    public ItemData keyItem;
    public string keyID = "";
    public bool allowMasterKey = true;
    
    [Header("Access Management")]
    public List<string> authorizedPlayers = new List<string>();
    public List<string> guestCodes = new List<string>();
    public bool allowTeamAccess = true;
    
    [Header("UI References")]
    public GameObject lockUI;
    public Text lockStatusText;
    public InputField codeInputField;
    public Button[] numberButtons;
    public Button submitButton;
    public Button clearButton;
    public Button lockToggleButton;
    public Button addGuestButton;
    
    [Header("Visual Effects")]
    public Renderer lockRenderer;
    public Material lockedMaterial;
    public Material unlockedMaterial;
    public AudioClip lockSound;
    public AudioClip unlockSound;
    public AudioClip wrongCodeSound;
    public AudioClip keySound;
    
    // State tracking
    private int attemptCount = 0;
    private float lockoutEndTime = 0f;
    private bool isInLockout = false;
    private string inputCode = "";
    private PlayerController currentUser;
    private AudioSource audioSource;

    void Start()
    {
        InitializeLock();
    }

    void Update()
    {
        UpdateLockout();
        UpdateVisuals();
    }

    void InitializeLock()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();
            
        // Setup UI
        if (lockUI != null)
            lockUI.SetActive(false);
            
        // Setup number buttons
        for (int i = 0; i < numberButtons.Length; i++)
        {
            if (numberButtons[i] != null)
            {
                int number = i;
                numberButtons[i].onClick.AddListener(() => OnNumberPressed(number));
            }
        }
        
        // Setup other buttons
        if (submitButton != null)
            submitButton.onClick.AddListener(OnSubmitPressed);
            
        if (clearButton != null)
            clearButton.onClick.AddListener(OnClearPressed);
            
        if (lockToggleButton != null)
            lockToggleButton.onClick.AddListener(OnLockTogglePressed);
            
        if (addGuestButton != null)
            addGuestButton.onClick.AddListener(OnAddGuestPressed);
        
        // Generate random key ID if not set
        if (lockType == LockType.KeyLock && string.IsNullOrEmpty(keyID))
        {
            keyID = System.Guid.NewGuid().ToString();
        }
        
        UpdateLockStatus();
    }

    void UpdateLockout()
    {
        if (isInLockout && Time.time >= lockoutEndTime)
        {
            isInLockout = false;
            attemptCount = 0;
            UpdateLockStatus();
        }
    }

    void UpdateVisuals()
    {
        if (lockRenderer != null)
        {
            if (isLocked && lockedMaterial != null)
                lockRenderer.material = lockedMaterial;
            else if (!isLocked && unlockedMaterial != null)
                lockRenderer.material = unlockedMaterial;
        }
    }

    // IInteractable implementation
    public bool CanInteract(PlayerController player)
    {
        return player != null;
    }

    public string GetInteractionText(PlayerController player)
    {
        if (isInLockout)
            return $"Locked out for {Mathf.Ceil(lockoutEndTime - Time.time)}s";
            
        if (lockType == LockType.KeyLock)
        {
            if (HasValidKey(player))
                return isLocked ? "Unlock with Key" : "Lock with Key";
            else
                return "Requires Key";
        }
        else
        {
            if (IsAuthorized(player))
                return isLocked ? "Enter Code to Unlock" : "Enter Code to Lock";
            else
                return "Enter Code";
        }
    }

    public void Interact(PlayerController player)
    {
        if (isInLockout) return;
        
        currentUser = player;
        
        if (lockType == LockType.KeyLock)
        {
            HandleKeyInteraction(player);
        }
        else
        {
            OpenCodeInterface(player);
        }
    }

    void HandleKeyInteraction(PlayerController player)
    {
        if (!HasValidKey(player))
        {
            ShowMessage("You don't have the right key!");
            return;
        }
        
        ToggleLock();
        PlaySound(keySound);
    }

    void OpenCodeInterface(PlayerController player)
    {
        if (lockUI == null) return;
        
        lockUI.SetActive(true);
        inputCode = "";
        
        UpdateLockStatus();
        UpdateCodeDisplay();
        
        // Apply Rust styling
        RustUIUpdater updater = lockUI.GetComponent<RustUIUpdater>();
        if (updater == null)
        {
            updater = lockUI.AddComponent<RustUIUpdater>();
            updater.updateRecursively = true;
        }
        updater.UpdateRustStyling();
    }

    public void CloseCodeInterface()
    {
        if (lockUI != null)
            lockUI.SetActive(false);
            
        currentUser = null;
    }

    void OnNumberPressed(int number)
    {
        if (isInLockout) return;
        if (inputCode.Length >= maxCodeLength) return;
        
        inputCode += number.ToString();
        UpdateCodeDisplay();
    }

    void OnClearPressed()
    {
        inputCode = "";
        UpdateCodeDisplay();
    }

    void OnSubmitPressed()
    {
        if (isInLockout) return;
        if (string.IsNullOrEmpty(inputCode)) return;
        
        ProcessCodeInput();
    }

    void OnLockTogglePressed()
    {
        if (currentUser == null) return;
        if (!IsAuthorized(currentUser)) return;
        
        ToggleLock();
    }

    void OnAddGuestPressed()
    {
        if (currentUser == null) return;
        if (!IsOwner(currentUser)) return;
        if (string.IsNullOrEmpty(inputCode)) return;
        
        AddGuestCode(inputCode);
    }

    void ProcessCodeInput()
    {
        bool isCorrect = false;
        
        // Check main code
        if (inputCode == currentCode)
        {
            isCorrect = true;
        }
        // Check guest codes
        else if (guestCodes.Contains(inputCode))
        {
            isCorrect = true;
        }
        
        if (isCorrect)
        {
            HandleCorrectCode();
        }
        else
        {
            HandleWrongCode();
        }
        
        inputCode = "";
        UpdateCodeDisplay();
    }

    void HandleCorrectCode()
    {
        attemptCount = 0;
        
        // Grant access based on context
        if (!hasOwner)
        {
            // First time setup - claim ownership
            ClaimOwnership(currentUser);
            SetCode(inputCode);
        }
        else if (IsAuthorized(currentUser))
        {
            // Owner/authorized player - toggle lock
            ToggleLock();
        }
        else
        {
            // Guest access - unlock temporarily
            UnlockTemporarily();
        }
        
        PlaySound(unlockSound);
        ShowMessage("Access granted!");
        
        CloseCodeInterface();
    }

    void HandleWrongCode()
    {
        attemptCount++;
        PlaySound(wrongCodeSound);
        
        if (attemptCount >= maxAttempts)
        {
            StartLockout();
            ShowMessage($"Too many failed attempts! Locked out for {lockoutDuration}s");
            CloseCodeInterface();
        }
        else
        {
            int remaining = maxAttempts - attemptCount;
            ShowMessage($"Wrong code! {remaining} attempts remaining");
        }
    }

    void StartLockout()
    {
        isInLockout = true;
        lockoutEndTime = Time.time + lockoutDuration;
        attemptCount = 0;
    }

    void ClaimOwnership(PlayerController player)
    {
        hasOwner = true;
        ownerID = GetPlayerID(player);
        authorizedPlayers.Clear();
        authorizedPlayers.Add(ownerID);
        
        Debug.Log($"Lock claimed by player {ownerID}");
    }

    void SetCode(string newCode)
    {
        if (newCode.Length != maxCodeLength) return;
        
        currentCode = newCode;
        Debug.Log($"Lock code set to {newCode}");
    }

    void ToggleLock()
    {
        isLocked = !isLocked;
        
        if (isLocked)
        {
            PlaySound(lockSound);
            ShowMessage("Locked");
        }
        else
        {
            PlaySound(unlockSound);
            ShowMessage("Unlocked");
        }
        
        UpdateLockStatus();
        NotifyLockChange();
    }

    void UnlockTemporarily()
    {
        if (isLocked)
        {
            isLocked = false;
            UpdateLockStatus();
            NotifyLockChange();
            
            // Lock again after a delay
            Invoke(nameof(RelockAfterDelay), 5f);
        }
    }

    void RelockAfterDelay()
    {
        if (!isLocked)
        {
            isLocked = true;
            UpdateLockStatus();
            NotifyLockChange();
            PlaySound(lockSound);
        }
    }

    void AddGuestCode(string guestCode)
    {
        if (guestCode.Length != maxCodeLength) return;
        if (guestCodes.Contains(guestCode)) return;
        if (guestCode == currentCode) return;
        
        guestCodes.Add(guestCode);
        ShowMessage($"Guest code {guestCode} added");
        
        Debug.Log($"Added guest code: {guestCode}");
    }

    void RemoveGuestCode(string guestCode)
    {
        if (guestCodes.Contains(guestCode))
        {
            guestCodes.Remove(guestCode);
            ShowMessage($"Guest code {guestCode} removed");
        }
    }

    void UpdateCodeDisplay()
    {
        if (codeInputField != null)
        {
            // Show asterisks for security
            string displayCode = new string('*', inputCode.Length);
            codeInputField.text = displayCode;
        }
    }

    void UpdateLockStatus()
    {
        if (lockStatusText == null) return;
        
        string statusText = "";
        
        if (isInLockout)
        {
            statusText = $"LOCKED OUT - {Mathf.Ceil(lockoutEndTime - Time.time)}s";
        }
        else if (!hasOwner)
        {
            statusText = "UNCLAIMED - Enter code to claim";
        }
        else if (isLocked)
        {
            statusText = "LOCKED";
        }
        else
        {
            statusText = "UNLOCKED";
        }
        
        RustFontManager.SetRustText(lockStatusText, statusText, RustTextType.Header);
        
        // Update button states
        if (lockToggleButton != null)
        {
            lockToggleButton.interactable = currentUser != null && IsAuthorized(currentUser) && !isInLockout;
        }
        
        if (addGuestButton != null)
        {
            addGuestButton.interactable = currentUser != null && IsOwner(currentUser) && !isInLockout;
        }
    }

    void NotifyLockChange()
    {
        // Notify connected objects (doors, storage boxes, etc.)
        ILockable[] lockableObjects = GetComponents<ILockable>();
        foreach (var lockable in lockableObjects)
        {
            lockable.OnLockStateChanged(isLocked);
        }
        
        // Also check parent objects
        ILockable parentLockable = GetComponentInParent<ILockable>();
        if (parentLockable != null)
        {
            parentLockable.OnLockStateChanged(isLocked);
        }
    }

    bool HasValidKey(PlayerController player)
    {
        if (lockType != LockType.KeyLock) return false;
        if (keyItem == null) return false;
        
        InventoryManager inventory = player.GetComponent<InventoryManager>();
        if (inventory == null) return false;
        
        // Check for specific key
        if (inventory.HasItem(keyItem, 1))
        {
            // Verify key ID matches
            ItemStack keyStack = inventory.GetItemStack(keyItem);
            if (keyStack != null)
            {
                KeyData keyData = keyStack.item.GetComponent<KeyData>();
                if (keyData != null && keyData.keyID == keyID)
                {
                    return true;
                }
            }
        }
        
        // Check for master key
        if (allowMasterKey)
        {
            ItemData masterKey = GetMasterKeyItem();
            if (masterKey != null && inventory.HasItem(masterKey, 1))
            {
                return true;
            }
        }
        
        return false;
    }

    bool IsAuthorized(PlayerController player)
    {
        if (!hasOwner) return true; // Unclaimed locks can be accessed by anyone
        
        string playerID = GetPlayerID(player);
        
        // Check if player is in authorized list
        if (authorizedPlayers.Contains(playerID))
            return true;
        
        // Check team access if enabled
        if (allowTeamAccess)
        {
            ClanManager clanManager = FindObjectOfType<ClanManager>();
            if (clanManager != null)
            {
                return clanManager.AreInSameClan(ownerID, playerID);
            }
        }
        
        return false;
    }

    bool IsOwner(PlayerController player)
    {
        return hasOwner && GetPlayerID(player) == ownerID;
    }

    void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }

    void ShowMessage(string message)
    {
        Debug.Log($"Lock: {message}");
        // This would show a UI message to the player
    }

    string GetPlayerID(PlayerController player)
    {
        return player != null ? player.gameObject.GetInstanceID().ToString() : "";
    }

    ItemData GetMasterKeyItem()
    {
        // Return reference to master key item
        return Resources.Load<ItemData>("Items/MasterKey");
    }

    // Public methods for external access
    public bool IsLocked() => isLocked;
    public bool HasOwner() => hasOwner;
    public string GetOwnerID() => ownerID;
    public LockType GetLockType() => lockType;
    
    public void SetLockType(LockType newType)
    {
        lockType = newType;
        if (lockType == LockType.KeyLock && string.IsNullOrEmpty(keyID))
        {
            keyID = System.Guid.NewGuid().ToString();
        }
    }
    
    public void AuthorizePlayer(string playerID)
    {
        if (!authorizedPlayers.Contains(playerID))
        {
            authorizedPlayers.Add(playerID);
        }
    }
    
    public void DeauthorizePlayer(string playerID)
    {
        authorizedPlayers.Remove(playerID);
    }
    
    public void TransferOwnership(string newOwnerID)
    {
        ownerID = newOwnerID;
        authorizedPlayers.Clear();
        authorizedPlayers.Add(newOwnerID);
    }
}

public enum LockType
{
    CodeLock,   // 4-digit code lock
    KeyLock     // Physical key lock
}

// Interface for objects that can be locked
public interface ILockable
{
    void OnLockStateChanged(bool isLocked);
}

// Component for key items to store key data
[System.Serializable]
public class KeyData : MonoBehaviour
{
    public string keyID;
    public string keyName;
    public LockType compatibleLockType = LockType.KeyLock;
}