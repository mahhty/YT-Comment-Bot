using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class RustUIUpdater : MonoBehaviour
{
    [Header("Auto-Update Settings")]
    public bool updateOnStart = true;
    public bool updateOnEnable = true;
    public bool updateRecursively = true;
    
    [Header("Specific Text Types")]
    public RustTextType textType = RustTextType.Normal;
    public bool overrideAutoDetection = false;

    void Start()
    {
        if (updateOnStart)
        {
            UpdateRustStyling();
        }
    }

    void OnEnable()
    {
        if (updateOnEnable)
        {
            UpdateRustStyling();
        }
    }

    public void UpdateRustStyling()
    {
        if (RustFontManager.Instance == null) return;

        if (updateRecursively)
        {
            // Update all text components in children
            Text[] texts = GetComponentsInChildren<Text>(true);
            foreach (Text text in texts)
            {
                if (overrideAutoDetection)
                {
                    RustFontManager.SetRustText(text, text.text, textType);
                }
                else
                {
                    RustFontManager.Instance.ApplyRustStyling(text);
                }
            }

            // Update all TextMeshPro components in children
            TextMeshProUGUI[] tmpTexts = GetComponentsInChildren<TextMeshProUGUI>(true);
            foreach (TextMeshProUGUI tmpText in tmpTexts)
            {
                RustFontManager.Instance.ApplyRustStyling(tmpText);
            }
        }
        else
        {
            // Update only direct components
            Text text = GetComponent<Text>();
            if (text != null)
            {
                if (overrideAutoDetection)
                {
                    RustFontManager.SetRustText(text, text.text, textType);
                }
                else
                {
                    RustFontManager.Instance.ApplyRustStyling(text);
                }
            }

            TextMeshProUGUI tmpText = GetComponent<TextMeshProUGUI>();
            if (tmpText != null)
            {
                RustFontManager.Instance.ApplyRustStyling(tmpText);
            }
        }
    }

    // Method to update specific UI elements with Rust styling
    public static void UpdateMainMenu()
    {
        // Find and update main menu elements
        GameObject mainMenu = GameObject.Find("MainMenuPanel");
        if (mainMenu != null)
        {
            // Update game title
            Text gameTitle = mainMenu.transform.Find("GameTitle")?.GetComponent<Text>();
            if (gameTitle != null)
            {
                RustFontManager.SetRustText(gameTitle, "DETERIORATE", RustTextType.Title);
            }

            // Update version text
            Text versionText = mainMenu.transform.Find("VersionText")?.GetComponent<Text>();
            if (versionText != null)
            {
                RustFontManager.SetRustText(versionText, versionText.text, RustTextType.Info);
            }

            // Update button texts
            Button[] buttons = mainMenu.GetComponentsInChildren<Button>();
            foreach (Button button in buttons)
            {
                Text buttonText = button.GetComponentInChildren<Text>();
                if (buttonText != null)
                {
                    RustFontManager.Instance.ApplyRustStyling(buttonText);
                }
            }
        }
    }

    public static void UpdateInventoryUI()
    {
        InventoryManager inventory = FindObjectOfType<InventoryManager>();
        if (inventory != null)
        {
            // Update inventory title
            if (inventory.inventoryTitle != null)
            {
                RustFontManager.SetRustText(inventory.inventoryTitle, "INVENTORY", RustTextType.Header);
            }

            // Update all inventory slot texts
            InventorySlot[] slots = FindObjectsOfType<InventorySlot>();
            foreach (InventorySlot slot in slots)
            {
                if (slot.amountText != null)
                {
                    RustFontManager.Instance.ApplyRustStyling(slot.amountText);
                }
            }
        }
    }

    public static void UpdateSurvivalUI()
    {
        SurvivalManager survival = FindObjectOfType<SurvivalManager>();
        if (survival != null)
        {
            // Update health text
            if (survival.healthText != null)
            {
                RustFontManager.SetRustText(survival.healthText, survival.healthText.text, RustTextType.Monospace);
            }

            // Update hunger text
            if (survival.hungerText != null)
            {
                RustFontManager.SetRustText(survival.hungerText, survival.hungerText.text, RustTextType.Monospace);
            }

            // Update thirst text
            if (survival.thirstText != null)
            {
                RustFontManager.SetRustText(survival.thirstText, survival.thirstText.text, RustTextType.Monospace);
            }

            // Update temperature text
            if (survival.temperatureText != null)
            {
                RustFontManager.SetRustText(survival.temperatureText, survival.temperatureText.text, RustTextType.Monospace);
            }
        }
    }

    public static void UpdateServerBrowser()
    {
        // Find server browser elements
        GameObject serverBrowser = GameObject.Find("ServerBrowserPanel");
        if (serverBrowser != null)
        {
            // Update server count text
            Text serverCountText = serverBrowser.transform.Find("ServerCountText")?.GetComponent<Text>();
            if (serverCountText != null)
            {
                RustFontManager.SetRustText(serverCountText, serverCountText.text, RustTextType.Info);
            }

            // Update all server entry texts
            ServerEntryUI[] serverEntries = FindObjectsOfType<ServerEntryUI>();
            foreach (ServerEntryUI entry in serverEntries)
            {
                RustUIUpdater updater = entry.GetComponent<RustUIUpdater>();
                if (updater == null)
                {
                    updater = entry.gameObject.AddComponent<RustUIUpdater>();
                    updater.updateRecursively = true;
                }
                updater.UpdateRustStyling();
            }
        }
    }

    public static void UpdateGamblingUI()
    {
        GamblingWheel gambling = FindObjectOfType<GamblingWheel>();
        if (gambling != null)
        {
            // Update bet amount text
            if (gambling.betAmountText != null)
            {
                RustFontManager.SetRustText(gambling.betAmountText, gambling.betAmountText.text, RustTextType.Monospace);
            }

            // Update scrap amount text
            if (gambling.scrapAmountText != null)
            {
                RustFontManager.SetRustText(gambling.scrapAmountText, gambling.scrapAmountText.text, RustTextType.Monospace);
            }

            // Update result text
            if (gambling.resultText != null)
            {
                RustFontManager.SetRustText(gambling.resultText, gambling.resultText.text, RustTextType.Header);
            }

            // Update instructions
            if (gambling.instructionsText != null)
            {
                RustFontManager.SetRustText(gambling.instructionsText, gambling.instructionsText.text, RustTextType.Info);
            }
        }
    }

    public static void UpdateSafeZoneUI()
    {
        SafeZoneManager[] safeZones = FindObjectsOfType<SafeZoneManager>();
        foreach (SafeZoneManager safeZone in safeZones)
        {
            // Update safe zone name
            if (safeZone.safeZoneNameText != null)
            {
                RustFontManager.SetRustText(safeZone.safeZoneNameText, "SAFE ZONE", RustTextType.Header);
            }

            // Update safe zone info
            if (safeZone.safeZoneInfoText != null)
            {
                RustFontManager.SetRustText(safeZone.safeZoneInfoText, safeZone.safeZoneInfoText.text, RustTextType.Info);
            }
        }
    }

    // Static method to update all UI elements at once
    public static void UpdateAllRustUI()
    {
        // Apply Rust font manager to entire scene
        if (RustFontManager.Instance != null)
        {
            RustFontManager.Instance.ApplyRustFontsToScene();
        }

        // Update specific UI systems
        UpdateMainMenu();
        UpdateInventoryUI();
        UpdateSurvivalUI();
        UpdateServerBrowser();
        UpdateGamblingUI();
        UpdateSafeZoneUI();
    }
}

// Component to automatically apply Rust styling to buttons
public class RustButton : MonoBehaviour
{
    [Header("Button Styling")]
    public RustTextType textType = RustTextType.Normal;
    public RustColorType normalColor = RustColorType.White;
    public RustColorType highlightColor = RustColorType.Orange;
    public RustColorType pressedColor = RustColorType.Yellow;
    public RustColorType disabledColor = RustColorType.Gray;

    private Button button;
    private Text buttonText;

    void Start()
    {
        button = GetComponent<Button>();
        buttonText = GetComponentInChildren<Text>();

        ApplyRustButtonStyling();
    }

    void ApplyRustButtonStyling()
    {
        if (buttonText != null)
        {
            RustFontManager.SetRustText(buttonText, buttonText.text, textType);
        }

        if (button != null)
        {
            ColorBlock colors = button.colors;
            colors.normalColor = RustFontManager.GetRustColor(normalColor);
            colors.highlightedColor = RustFontManager.GetRustColor(highlightColor);
            colors.pressedColor = RustFontManager.GetRustColor(pressedColor);
            colors.disabledColor = RustFontManager.GetRustColor(disabledColor);
            button.colors = colors;
        }
    }

    public void SetButtonText(string text)
    {
        if (buttonText != null)
        {
            RustFontManager.SetRustText(buttonText, text, textType);
        }
    }
}