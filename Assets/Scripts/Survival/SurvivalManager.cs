using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class SurvivalManager : MonoBehaviour
{
    [Header("Health System")]
    public float maxHealth = 100f;
    public float currentHealth = 100f;
    public float healthRegenRate = 0.5f; // HP per second when conditions are met
    public float healthRegenThreshold = 50f; // Min hunger/thirst to regen health
    
    [Header("Hunger System")]
    public float maxHunger = 100f;
    public float currentHunger = 100f;
    public float hungerDecayRate = 0.2f; // Hunger lost per second
    public float hungerDamageThreshold = 0f; // Start taking damage when hunger hits this
    public float hungerDamageRate = 2f; // Damage per second when starving
    
    [Header("Thirst System")]
    public float maxThirst = 100f;
    public float currentThirst = 100f;
    public float thirstDecayRate = 0.3f; // Thirst lost per second (faster than hunger)
    public float thirstDamageThreshold = 0f; // Start taking damage when thirst hits this
    public float thirstDamageRate = 5f; // Damage per second when dehydrated (faster than starvation)
    
    [Header("Temperature System")]
    public float currentTemperature = 20f; // Celsius
    public float comfortableMinTemp = 10f;
    public float comfortableMaxTemp = 30f;
    public float freezingTemp = -10f;
    public float overheatTemp = 50f;
    public float temperatureDamageRate = 1f;
    
    [Header("Radiation System")]
    public float currentRadiation = 0f;
    public float maxRadiation = 500f;
    public float radiationDecayRate = 1f; // Radiation lost per second
    public float radiationDamageThreshold = 50f;
    public float radiationDamageRate = 2f;
    
    [Header("UI References")]
    public Slider healthBar;
    public Slider hungerBar;
    public Slider thirstBar;
    public Slider radiationBar;
    public Text healthText;
    public Text hungerText;
    public Text thirstText;
    public Text temperatureText;
    public GameObject radiationIndicator;
    public GameObject freezingIndicator;
    public GameObject overheatIndicator;
    
    [Header("Status Effects")]
    public Image healthIcon;
    public Image hungerIcon;
    public Image thirstIcon;
    public Color normalColor = Color.white;
    public Color warningColor = Color.yellow;
    public Color dangerColor = Color.red;
    public Color criticalColor = new Color(1f, 0f, 0f, 0.5f);
    
    // Status tracking
    private bool isRegeneratingHealth = false;
    private bool isStarving = false;
    private bool isDehydrated = false;
    private bool isFreezing = false;
    private bool isOverheating = false;
    private bool isRadiated = false;
    
    // Effects
    private Coroutine healthRegenCoroutine;
    private float lastHungerTime;
    private float lastThirstTime;

    void Start()
    {
        InitializeSurvival();
        UpdateUI();
    }

    void Update()
    {
        UpdateSurvivalStats();
        CheckStatusEffects();
        UpdateUI();
    }

    void InitializeSurvival()
    {
        // Start with full stats
        currentHealth = maxHealth;
        currentHunger = maxHunger;
        currentThirst = maxThirst;
        currentRadiation = 0f;
        
        lastHungerTime = Time.time;
        lastThirstTime = Time.time;
    }

    void UpdateSurvivalStats()
    {
        float deltaTime = Time.deltaTime;
        
        // Update hunger (decreases over time)
        currentHunger = Mathf.Max(0f, currentHunger - hungerDecayRate * deltaTime);
        
        // Update thirst (decreases faster than hunger)
        currentThirst = Mathf.Max(0f, currentThirst - thirstDecayRate * deltaTime);
        
        // Update radiation (decreases over time when not in radiated area)
        if (currentRadiation > 0f)
        {
            currentRadiation = Mathf.Max(0f, currentRadiation - radiationDecayRate * deltaTime);
        }
        
        // Temperature effects (would be influenced by environment, clothing, etc.)
        UpdateTemperatureEffects();
    }

    void CheckStatusEffects()
    {
        // Check health regeneration conditions
        bool canRegenHealth = currentHunger >= healthRegenThreshold && 
                             currentThirst >= healthRegenThreshold && 
                             currentHealth < maxHealth && 
                             !isStarving && !isDehydrated && 
                             !isFreezing && !isOverheating;
        
        if (canRegenHealth && !isRegeneratingHealth)
        {
            StartHealthRegeneration();
        }
        else if (!canRegenHealth && isRegeneratingHealth)
        {
            StopHealthRegeneration();
        }
        
        // Check starvation damage
        if (currentHunger <= hungerDamageThreshold)
        {
            if (!isStarving)
            {
                isStarving = true;
                InvokeRepeating(nameof(ApplyStarvationDamage), 1f, 1f);
            }
        }
        else if (isStarving)
        {
            isStarving = false;
            CancelInvoke(nameof(ApplyStarvationDamage));
        }
        
        // Check dehydration damage
        if (currentThirst <= thirstDamageThreshold)
        {
            if (!isDehydrated)
            {
                isDehydrated = true;
                InvokeRepeating(nameof(ApplyDehydrationDamage), 1f, 1f);
            }
        }
        else if (isDehydrated)
        {
            isDehydrated = false;
            CancelInvoke(nameof(ApplyDehydrationDamage));
        }
        
        // Check radiation damage
        if (currentRadiation >= radiationDamageThreshold)
        {
            if (!isRadiated)
            {
                isRadiated = true;
                InvokeRepeating(nameof(ApplyRadiationDamage), 1f, 1f);
            }
        }
        else if (isRadiated)
        {
            isRadiated = false;
            CancelInvoke(nameof(ApplyRadiationDamage));
        }
        
        // Check temperature damage
        CheckTemperatureDamage();
    }

    void UpdateTemperatureEffects()
    {
        // This would be influenced by:
        // - Environment (biome, weather, time of day)
        // - Clothing/armor
        // - Shelter
        // - Campfires/heat sources
        // For now, we'll use a base temperature
        
        // Temperature effects on other stats
        if (currentTemperature < comfortableMinTemp)
        {
            // Cold increases hunger decay
            hungerDecayRate = 0.3f;
        }
        else if (currentTemperature > comfortableMaxTemp)
        {
            // Heat increases thirst decay
            thirstDecayRate = 0.5f;
        }
        else
        {
            // Normal rates
            hungerDecayRate = 0.2f;
            thirstDecayRate = 0.3f;
        }
    }

    void CheckTemperatureDamage()
    {
        // Freezing damage
        if (currentTemperature <= freezingTemp)
        {
            if (!isFreezing)
            {
                isFreezing = true;
                InvokeRepeating(nameof(ApplyFreezingDamage), 1f, 1f);
            }
        }
        else if (isFreezing)
        {
            isFreezing = false;
            CancelInvoke(nameof(ApplyFreezingDamage));
        }
        
        // Overheating damage
        if (currentTemperature >= overheatTemp)
        {
            if (!isOverheating)
            {
                isOverheating = true;
                InvokeRepeating(nameof(ApplyOverheatDamage), 1f, 1f);
            }
        }
        else if (isOverheating)
        {
            isOverheating = false;
            CancelInvoke(nameof(ApplyOverheatDamage));
        }
    }

    void StartHealthRegeneration()
    {
        isRegeneratingHealth = true;
        if (healthRegenCoroutine != null)
            StopCoroutine(healthRegenCoroutine);
        healthRegenCoroutine = StartCoroutine(HealthRegeneration());
    }

    void StopHealthRegeneration()
    {
        isRegeneratingHealth = false;
        if (healthRegenCoroutine != null)
        {
            StopCoroutine(healthRegenCoroutine);
            healthRegenCoroutine = null;
        }
    }

    IEnumerator HealthRegeneration()
    {
        while (isRegeneratingHealth && currentHealth < maxHealth)
        {
            currentHealth = Mathf.Min(maxHealth, currentHealth + healthRegenRate * Time.deltaTime);
            yield return null;
        }
        isRegeneratingHealth = false;
    }

    // Damage methods
    void ApplyStarvationDamage()
    {
        TakeDamage(hungerDamageRate, "Starvation");
    }

    void ApplyDehydrationDamage()
    {
        TakeDamage(thirstDamageRate, "Dehydration");
    }

    void ApplyRadiationDamage()
    {
        float damage = radiationDamageRate * (currentRadiation / maxRadiation);
        TakeDamage(damage, "Radiation");
    }

    void ApplyFreezingDamage()
    {
        TakeDamage(temperatureDamageRate, "Freezing");
    }

    void ApplyOverheatDamage()
    {
        TakeDamage(temperatureDamageRate, "Overheating");
    }

    public void TakeDamage(float damage, string source = "Unknown")
    {
        currentHealth = Mathf.Max(0f, currentHealth - damage);
        
        // Visual feedback
        ShowDamageEffect(damage, source);
        
        if (currentHealth <= 0f)
        {
            Die(source);
        }
    }

    void Die(string cause)
    {
        Debug.Log($"Player died from: {cause}");
        // Handle player death
        // In multiplayer, this would:
        // - Drop inventory
        // - Create sleeper if disconnected
        // - Respawn at beach/sleeping bag
    }

    // Public methods for consuming items
    public void RestoreHealth(float amount)
    {
        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
    }

    public void RestoreHunger(float amount)
    {
        currentHunger = Mathf.Min(maxHunger, currentHunger + amount);
    }

    public void RestoreThirst(float amount)
    {
        currentThirst = Mathf.Min(maxThirst, currentThirst + amount);
    }

    public void AddRadiation(float amount)
    {
        currentRadiation = Mathf.Min(maxRadiation, currentRadiation + amount);
    }

    public void SetTemperature(float temperature)
    {
        currentTemperature = temperature;
    }

    void UpdateUI()
    {
        // Update health bar
        if (healthBar != null)
        {
            healthBar.value = currentHealth / maxHealth;
            Color healthColor = GetHealthColor();
            healthBar.fillRect.GetComponent<Image>().color = healthColor;
            
            if (healthIcon != null)
                healthIcon.color = healthColor;
        }
        
        if (healthText != null)
            healthText.text = $"{currentHealth:F0}";
        
        // Update hunger bar
        if (hungerBar != null)
        {
            hungerBar.value = currentHunger / maxHunger;
            Color hungerColor = GetHungerColor();
            hungerBar.fillRect.GetComponent<Image>().color = hungerColor;
            
            if (hungerIcon != null)
                hungerIcon.color = hungerColor;
        }
        
        if (hungerText != null)
            hungerText.text = $"{currentHunger:F0}";
        
        // Update thirst bar
        if (thirstBar != null)
        {
            thirstBar.value = currentThirst / maxThirst;
            Color thirstColor = GetThirstColor();
            thirstBar.fillRect.GetComponent<Image>().color = thirstColor;
            
            if (thirstIcon != null)
                thirstIcon.color = thirstColor;
        }
        
        if (thirstText != null)
            thirstText.text = $"{currentThirst:F0}";
        
        // Update radiation bar
        if (radiationBar != null)
        {
            radiationBar.value = currentRadiation / maxRadiation;
            radiationBar.gameObject.SetActive(currentRadiation > 0f);
        }
        
        if (radiationIndicator != null)
            radiationIndicator.SetActive(currentRadiation > radiationDamageThreshold);
        
        // Update temperature text
        if (temperatureText != null)
            temperatureText.text = $"{currentTemperature:F0}°C";
        
        // Update status indicators
        if (freezingIndicator != null)
            freezingIndicator.SetActive(isFreezing);
            
        if (overheatIndicator != null)
            overheatIndicator.SetActive(isOverheating);
    }

    Color GetHealthColor()
    {
        float percentage = currentHealth / maxHealth;
        if (percentage > 0.7f) return normalColor;
        if (percentage > 0.3f) return warningColor;
        if (percentage > 0.1f) return dangerColor;
        return criticalColor;
    }

    Color GetHungerColor()
    {
        float percentage = currentHunger / maxHunger;
        if (percentage > 0.5f) return normalColor;
        if (percentage > 0.2f) return warningColor;
        if (percentage > 0.05f) return dangerColor;
        return criticalColor;
    }

    Color GetThirstColor()
    {
        float percentage = currentThirst / maxThirst;
        if (percentage > 0.5f) return normalColor;
        if (percentage > 0.2f) return warningColor;
        if (percentage > 0.05f) return dangerColor;
        return criticalColor;
    }

    void ShowDamageEffect(float damage, string source)
    {
        // Show floating damage text or screen effect
        Debug.Log($"Took {damage:F1} damage from {source}");
    }

    // Getters for other systems
    public float GetHealthPercentage() => currentHealth / maxHealth;
    public float GetHungerPercentage() => currentHunger / maxHunger;
    public float GetThirstPercentage() => currentThirst / maxThirst;
    public float GetRadiationPercentage() => currentRadiation / maxRadiation;
    public bool IsAlive() => currentHealth > 0f;
    public bool IsHealthy() => currentHealth > maxHealth * 0.7f && currentHunger > maxHunger * 0.5f && currentThirst > maxThirst * 0.5f;
}