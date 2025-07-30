using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class RustFontManager : MonoBehaviour
{
    [Header("Rust Font Assets")]
    public Font rustMainFont; // Main UI font (similar to Rust's Roboto)
    public Font rustHeaderFont; // Headers (bold/condensed)
    public Font rustMonoFont; // Monospace for numbers/stats
    public TMP_FontAsset rustTMPFont; // TextMeshPro version
    public TMP_FontAsset rustTMPHeaderFont;
    public TMP_FontAsset rustTMPMonoFont;
    
    [Header("Font Sizes")]
    public int smallTextSize = 14;
    public int normalTextSize = 16;
    public int mediumTextSize = 20;
    public int largeTextSize = 24;
    public int headerTextSize = 32;
    public int titleTextSize = 48;
    
    [Header("Rust Color Scheme")]
    public Color rustWhite = new Color(0.9f, 0.9f, 0.9f, 1f);
    public Color rustGray = new Color(0.6f, 0.6f, 0.6f, 1f);
    public Color rustDarkGray = new Color(0.3f, 0.3f, 0.3f, 1f);
    public Color rustOrange = new Color(1f, 0.5f, 0.2f, 1f);
    public Color rustRed = new Color(0.8f, 0.2f, 0.2f, 1f);
    public Color rustGreen = new Color(0.4f, 0.8f, 0.4f, 1f);
    public Color rustBlue = new Color(0.3f, 0.6f, 1f, 1f);
    public Color rustYellow = new Color(1f, 0.8f, 0.2f, 1f);
    
    [Header("UI Styling")]
    public Material rustUIOutlineMaterial;
    public Shadow.Effect defaultShadowEffect = Shadow.Effect.DropShadow;
    public Vector2 defaultShadowDistance = new Vector2(1f, -1f);
    public Color defaultShadowColor = new Color(0f, 0f, 0f, 0.5f);
    
    private static RustFontManager instance;
    
    public static RustFontManager Instance
    {
        get
        {
            if (instance == null)
                instance = FindObjectOfType<RustFontManager>();
            return instance;
        }
    }

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            LoadRustFonts();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        ApplyRustFontsToScene();
    }

    void LoadRustFonts()
    {
        // Load Rust-style fonts from Resources
        if (rustMainFont == null)
            rustMainFont = Resources.Load<Font>("Fonts/RustMain") ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            
        if (rustHeaderFont == null)
            rustHeaderFont = Resources.Load<Font>("Fonts/RustHeader") ?? rustMainFont;
            
        if (rustMonoFont == null)
            rustMonoFont = Resources.Load<Font>("Fonts/RustMono") ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            
        // Load TextMeshPro fonts
        if (rustTMPFont == null)
            rustTMPFont = Resources.Load<TMP_FontAsset>("Fonts/RustTMP");
            
        if (rustTMPHeaderFont == null)
            rustTMPHeaderFont = Resources.Load<TMP_FontAsset>("Fonts/RustTMPHeader") ?? rustTMPFont;
            
        if (rustTMPMonoFont == null)
            rustTMPMonoFont = Resources.Load<TMP_FontAsset>("Fonts/RustTMPMono") ?? rustTMPFont;
    }

    public void ApplyRustFontsToScene()
    {
        // Apply fonts to all UI Text components
        Text[] allTexts = FindObjectsOfType<Text>(true);
        foreach (Text text in allTexts)
        {
            ApplyRustStyling(text);
        }
        
        // Apply fonts to all TextMeshPro components
        TextMeshProUGUI[] allTMPTexts = FindObjectsOfType<TextMeshProUGUI>(true);
        foreach (TextMeshProUGUI tmpText in allTMPTexts)
        {
            ApplyRustStyling(tmpText);
        }
    }

    public void ApplyRustStyling(Text textComponent)
    {
        if (textComponent == null) return;
        
        // Determine font type based on component name/tag
        RustTextType textType = DetermineTextType(textComponent.gameObject);
        
        // Apply appropriate font
        switch (textType)
        {
            case RustTextType.Header:
                textComponent.font = rustHeaderFont;
                textComponent.fontSize = GetFontSize(textComponent, headerTextSize);
                textComponent.color = rustWhite;
                textComponent.fontStyle = FontStyle.Bold;
                break;
                
            case RustTextType.Title:
                textComponent.font = rustHeaderFont;
                textComponent.fontSize = GetFontSize(textComponent, titleTextSize);
                textComponent.color = rustOrange;
                textComponent.fontStyle = FontStyle.Bold;
                break;
                
            case RustTextType.Monospace:
                textComponent.font = rustMonoFont;
                textComponent.fontSize = GetFontSize(textComponent, normalTextSize);
                textComponent.color = rustWhite;
                break;
                
            case RustTextType.Warning:
                textComponent.font = rustMainFont;
                textComponent.fontSize = GetFontSize(textComponent, normalTextSize);
                textComponent.color = rustRed;
                textComponent.fontStyle = FontStyle.Bold;
                break;
                
            case RustTextType.Success:
                textComponent.font = rustMainFont;
                textComponent.fontSize = GetFontSize(textComponent, normalTextSize);
                textComponent.color = rustGreen;
                break;
                
            default:
                textComponent.font = rustMainFont;
                textComponent.fontSize = GetFontSize(textComponent, normalTextSize);
                textComponent.color = rustWhite;
                break;
        }
        
        // Add outline/shadow effects
        AddRustTextEffects(textComponent.gameObject);
    }

    public void ApplyRustStyling(TextMeshProUGUI tmpComponent)
    {
        if (tmpComponent == null) return;
        
        RustTextType textType = DetermineTextType(tmpComponent.gameObject);
        
        switch (textType)
        {
            case RustTextType.Header:
                tmpComponent.font = rustTMPHeaderFont ?? rustTMPFont;
                tmpComponent.fontSize = GetFontSize(tmpComponent, headerTextSize);
                tmpComponent.color = rustWhite;
                tmpComponent.fontStyle = FontStyles.Bold;
                break;
                
            case RustTextType.Title:
                tmpComponent.font = rustTMPHeaderFont ?? rustTMPFont;
                tmpComponent.fontSize = GetFontSize(tmpComponent, titleTextSize);
                tmpComponent.color = rustOrange;
                tmpComponent.fontStyle = FontStyles.Bold;
                break;
                
            case RustTextType.Monospace:
                tmpComponent.font = rustTMPMonoFont ?? rustTMPFont;
                tmpComponent.fontSize = GetFontSize(tmpComponent, normalTextSize);
                tmpComponent.color = rustWhite;
                break;
                
            case RustTextType.Warning:
                tmpComponent.font = rustTMPFont;
                tmpComponent.fontSize = GetFontSize(tmpComponent, normalTextSize);
                tmpComponent.color = rustRed;
                tmpComponent.fontStyle = FontStyles.Bold;
                break;
                
            case RustTextType.Success:
                tmpComponent.font = rustTMPFont;
                tmpComponent.fontSize = GetFontSize(tmpComponent, normalTextSize);
                tmpComponent.color = rustGreen;
                break;
                
            default:
                tmpComponent.font = rustTMPFont;
                tmpComponent.fontSize = GetFontSize(tmpComponent, normalTextSize);
                tmpComponent.color = rustWhite;
                break;
        }
        
        // Apply material for outline effects
        if (rustUIOutlineMaterial != null)
        {
            tmpComponent.fontSharedMaterial = rustUIOutlineMaterial;
        }
    }

    RustTextType DetermineTextType(GameObject textObject)
    {
        string name = textObject.name.ToLower();
        string tag = textObject.tag;
        
        // Check for specific naming conventions
        if (name.Contains("title") || tag == "Title")
            return RustTextType.Title;
        if (name.Contains("header") || name.Contains("heading") || tag == "Header")
            return RustTextType.Header;
        if (name.Contains("mono") || name.Contains("stat") || name.Contains("number") || tag == "Monospace")
            return RustTextType.Monospace;
        if (name.Contains("warning") || name.Contains("error") || tag == "Warning")
            return RustTextType.Warning;
        if (name.Contains("success") || name.Contains("win") || tag == "Success")
            return RustTextType.Success;
        if (name.Contains("info") || tag == "Info")
            return RustTextType.Info;
            
        // Check parent objects for context
        Transform parent = textObject.transform.parent;
        if (parent != null)
        {
            string parentName = parent.name.ToLower();
            if (parentName.Contains("title"))
                return RustTextType.Title;
            if (parentName.Contains("header"))
                return RustTextType.Header;
            if (parentName.Contains("inventory") || parentName.Contains("stat"))
                return RustTextType.Monospace;
        }
        
        return RustTextType.Normal;
    }

    int GetFontSize(Component textComponent, int defaultSize)
    {
        // Scale font size based on screen resolution
        float scaleFactor = Screen.height / 1080f; // Base on 1080p
        return Mathf.RoundToInt(defaultSize * Mathf.Clamp(scaleFactor, 0.5f, 2f));
    }

    void AddRustTextEffects(GameObject textObject)
    {
        // Add shadow effect if not present
        Shadow shadow = textObject.GetComponent<Shadow>();
        if (shadow == null)
        {
            shadow = textObject.AddComponent<Shadow>();
        }
        
        shadow.effectColor = defaultShadowColor;
        shadow.effectDistance = defaultShadowDistance;
        
        // Add outline effect for headers
        if (DetermineTextType(textObject) == RustTextType.Header || 
            DetermineTextType(textObject) == RustTextType.Title)
        {
            Outline outline = textObject.GetComponent<Outline>();
            if (outline == null)
            {
                outline = textObject.AddComponent<Outline>();
            }
            
            outline.effectColor = Color.black;
            outline.effectDistance = Vector2.one;
        }
    }

    // Public utility methods for creating Rust-styled text
    public static Text CreateRustText(GameObject parent, string text, RustTextType textType = RustTextType.Normal)
    {
        GameObject textObj = new GameObject("RustText");
        textObj.transform.SetParent(parent.transform, false);
        
        Text textComponent = textObj.AddComponent<Text>();
        textComponent.text = text;
        
        if (Instance != null)
        {
            Instance.ApplyRustStyling(textComponent);
        }
        
        return textComponent;
    }

    public static TextMeshProUGUI CreateRustTMPText(GameObject parent, string text, RustTextType textType = RustTextType.Normal)
    {
        GameObject textObj = new GameObject("RustTMPText");
        textObj.transform.SetParent(parent.transform, false);
        
        TextMeshProUGUI tmpComponent = textObj.AddComponent<TextMeshProUGUI>();
        tmpComponent.text = text;
        
        if (Instance != null)
        {
            Instance.ApplyRustStyling(tmpComponent);
        }
        
        return tmpComponent;
    }

    // Color utility methods
    public static Color GetRustColor(RustColorType colorType)
    {
        if (Instance == null) return Color.white;
        
        switch (colorType)
        {
            case RustColorType.White: return Instance.rustWhite;
            case RustColorType.Gray: return Instance.rustGray;
            case RustColorType.DarkGray: return Instance.rustDarkGray;
            case RustColorType.Orange: return Instance.rustOrange;
            case RustColorType.Red: return Instance.rustRed;
            case RustColorType.Green: return Instance.rustGreen;
            case RustColorType.Blue: return Instance.rustBlue;
            case RustColorType.Yellow: return Instance.rustYellow;
            default: return Instance.rustWhite;
        }
    }

    // Method to update text with Rust formatting
    public static void SetRustText(Text textComponent, string text, RustTextType textType = RustTextType.Normal)
    {
        if (textComponent == null) return;
        
        textComponent.text = text;
        
        if (Instance != null)
        {
            // Re-apply styling based on new type
            GameObject tempObj = new GameObject();
            tempObj.name = textType.ToString();
            tempObj.transform.SetParent(textComponent.transform.parent);
            textComponent.transform.SetParent(tempObj.transform);
            
            Instance.ApplyRustStyling(textComponent);
            
            textComponent.transform.SetParent(tempObj.transform.parent);
            Destroy(tempObj);
        }
    }

    // Method to format numbers in Rust style
    public static string FormatRustNumber(int number)
    {
        if (number >= 1000000)
            return (number / 1000000f).ToString("0.0") + "M";
        if (number >= 1000)
            return (number / 1000f).ToString("0.0") + "K";
        return number.ToString();
    }

    public static string FormatRustTime(float seconds)
    {
        if (seconds >= 3600)
        {
            int hours = Mathf.FloorToInt(seconds / 3600);
            int minutes = Mathf.FloorToInt((seconds % 3600) / 60);
            return $"{hours}h {minutes}m";
        }
        if (seconds >= 60)
        {
            int minutes = Mathf.FloorToInt(seconds / 60);
            int secs = Mathf.FloorToInt(seconds % 60);
            return $"{minutes}m {secs}s";
        }
        return $"{Mathf.FloorToInt(seconds)}s";
    }
}

public enum RustTextType
{
    Normal,
    Header,
    Title,
    Monospace,
    Warning,
    Success,
    Info
}

public enum RustColorType
{
    White,
    Gray,
    DarkGray,
    Orange,
    Red,
    Green,
    Blue,
    Yellow
}