using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class GamblingWheel : MonoBehaviour
{
    [Header("Gambling Settings")]
    public int minBet = 10;
    public int maxBet = 1000;
    public float houseEdge = 0.05f; // 5% house edge
    
    [Header("Wheel Configuration")]
    public int wheelSegments = 20;
    public float spinDuration = 3f;
    public AnimationCurve spinCurve = AnimationCurve.EaseOut(0f, 0f, 1f, 1f);
    
    [Header("UI References")]
    public GameObject gamblingUI;
    public Transform wheelTransform;
    public Slider betSlider;
    public Text betAmountText;
    public Text scrapAmountText;
    public Button spinButton;
    public Button closeButton;
    public Button maxBetButton;
    public Text resultText;
    public Text instructionsText;
    
    [Header("Visual Effects")]
    public ParticleSystem winEffect;
    public ParticleSystem loseEffect;
    public AudioClip spinSound;
    public AudioClip winSound;
    public AudioClip loseSound;
    public Material[] segmentMaterials;
    
    [Header("Payout Configuration")]
    public List<WheelSegment> wheelSegments_Data = new List<WheelSegment>();
    
    private bool isSpinning = false;
    private int currentBet = 10;
    private PlayerController currentPlayer;
    private InventoryManager playerInventory;
    private AudioSource audioSource;

    void Start()
    {
        InitializeWheel();
        SetupUI();
        SetupWheelSegments();
    }

    void InitializeWheel()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();
            
        if (gamblingUI != null)
            gamblingUI.SetActive(false);
    }

    void SetupUI()
    {
        if (betSlider != null)
        {
            betSlider.minValue = minBet;
            betSlider.maxValue = maxBet;
            betSlider.value = minBet;
            betSlider.onValueChanged.AddListener(OnBetChanged);
        }
        
        if (spinButton != null)
            spinButton.onClick.AddListener(SpinWheel);
            
        if (closeButton != null)
            closeButton.onClick.AddListener(CloseGamblingInterface);
            
        if (maxBetButton != null)
            maxBetButton.onClick.AddListener(SetMaxBet);
            
        if (instructionsText != null)
            instructionsText.text = "Place your bet and spin the wheel!\nHigher multipliers have lower chances.";
    }

    void SetupWheelSegments()
    {
        if (wheelSegments_Data.Count == 0)
        {
            // Create default wheel segments
            wheelSegments_Data.Add(new WheelSegment { color = Color.red, multiplier = 0f, probability = 0.4f, name = "LOSE" });
            wheelSegments_Data.Add(new WheelSegment { color = Color.green, multiplier = 1.5f, probability = 0.3f, name = "1.5x" });
            wheelSegments_Data.Add(new WheelSegment { color = Color.blue, multiplier = 2f, probability = 0.15f, name = "2x" });
            wheelSegments_Data.Add(new WheelSegment { color = Color.yellow, multiplier = 3f, probability = 0.08f, name = "3x" });
            wheelSegments_Data.Add(new WheelSegment { color = Color.purple, multiplier = 5f, probability = 0.05f, name = "5x" });
            wheelSegments_Data.Add(new WheelSegment { color = Color.cyan, multiplier = 10f, probability = 0.015f, name = "10x" });
            wheelSegments_Data.Add(new WheelSegment { color = Color.magenta, multiplier = 20f, probability = 0.005f, name = "JACKPOT!" });
        }
        
        CreateWheelVisual();
    }

    void CreateWheelVisual()
    {
        if (wheelTransform == null) return;
        
        // Clear existing segments
        foreach (Transform child in wheelTransform)
        {
            Destroy(child.gameObject);
        }
        
        // Create visual segments
        float segmentAngle = 360f / wheelSegments;
        
        for (int i = 0; i < wheelSegments; i++)
        {
            GameObject segment = new GameObject($"Segment_{i}");
            segment.transform.SetParent(wheelTransform);
            
            // Position and rotate segment
            float angle = i * segmentAngle;
            segment.transform.localRotation = Quaternion.Euler(0f, 0f, angle);
            
            // Add visual component
            WheelSegmentVisual visual = segment.AddComponent<WheelSegmentVisual>();
            WheelSegment data = GetSegmentForIndex(i);
            visual.Setup(data, segmentAngle);
        }
    }

    WheelSegment GetSegmentForIndex(int index)
    {
        // Distribute segments based on probability
        float totalProbability = 0f;
        float targetProbability = (float)index / wheelSegments;
        
        foreach (var segment in wheelSegments_Data)
        {
            totalProbability += segment.probability;
            if (targetProbability <= totalProbability)
                return segment;
        }
        
        return wheelSegments_Data[0]; // Default to first segment
    }

    public void OpenGamblingInterface()
    {
        currentPlayer = FindObjectOfType<PlayerController>();
        if (currentPlayer == null) return;
        
        playerInventory = currentPlayer.GetComponent<InventoryManager>();
        if (playerInventory == null) return;
        
        if (gamblingUI != null)
        {
            gamblingUI.SetActive(true);
            UpdateScrapDisplay();
            UpdateBetDisplay();
        }
    }

    public void CloseGamblingInterface()
    {
        if (gamblingUI != null)
            gamblingUI.SetActive(false);
            
        currentPlayer = null;
        playerInventory = null;
    }

    void OnBetChanged(float value)
    {
        currentBet = Mathf.RoundToInt(value);
        UpdateBetDisplay();
    }

    void SetMaxBet()
    {
        int playerScrap = GetPlayerScrap();
        currentBet = Mathf.Min(playerScrap, maxBet);
        
        if (betSlider != null)
            betSlider.value = currentBet;
            
        UpdateBetDisplay();
    }

    void UpdateBetDisplay()
    {
        if (betAmountText != null)
            betAmountText.text = $"Bet: {currentBet} Scrap";
            
        // Update spin button state
        if (spinButton != null)
        {
            bool canSpin = !isSpinning && currentBet > 0 && GetPlayerScrap() >= currentBet;
            spinButton.interactable = canSpin;
        }
    }

    void UpdateScrapDisplay()
    {
        if (scrapAmountText != null)
            scrapAmountText.text = $"Scrap: {GetPlayerScrap()}";
    }

    int GetPlayerScrap()
    {
        if (playerInventory == null) return 0;
        
        // Count scrap in player inventory
        // This would check for actual scrap items
        return Random.Range(50, 500); // Simulated for now
    }

    public void SpinWheel()
    {
        if (isSpinning || currentPlayer == null) return;
        
        int playerScrap = GetPlayerScrap();
        if (playerScrap < currentBet)
        {
            ShowResult("Not enough scrap!", Color.red);
            return;
        }
        
        StartCoroutine(SpinWheelCoroutine());
    }

    IEnumerator SpinWheelCoroutine()
    {
        isSpinning = true;
        
        // Disable UI during spin
        if (spinButton != null)
            spinButton.interactable = false;
            
        // Consume scrap before spinning
        ConsumeScrap(currentBet);
        UpdateScrapDisplay();
        
        // Play spin sound
        PlaySound(spinSound);
        
        // Determine result
        WheelSegment resultSegment = DetermineResult();
        float targetAngle = CalculateTargetAngle(resultSegment);
        
        // Spin animation
        yield return StartCoroutine(AnimateWheelSpin(targetAngle));
        
        // Process result
        ProcessSpinResult(resultSegment);
        
        isSpinning = false;
        UpdateBetDisplay();
    }

    WheelSegment DetermineResult()
    {
        float random = Random.value;
        float cumulativeProbability = 0f;
        
        // Apply house edge
        random *= (1f + houseEdge);
        
        foreach (var segment in wheelSegments_Data)
        {
            cumulativeProbability += segment.probability;
            if (random <= cumulativeProbability)
                return segment;
        }
        
        return wheelSegments_Data[0]; // Default to lose
    }

    float CalculateTargetAngle(WheelSegment segment)
    {
        // Find which segment matches our result
        int segmentIndex = 0;
        for (int i = 0; i < wheelSegments; i++)
        {
            if (GetSegmentForIndex(i).Equals(segment))
            {
                segmentIndex = i;
                break;
            }
        }
        
        float segmentAngle = 360f / wheelSegments;
        float baseAngle = segmentIndex * segmentAngle;
        
        // Add multiple rotations for visual effect
        float extraRotations = Random.Range(3f, 6f) * 360f;
        
        return baseAngle + extraRotations;
    }

    IEnumerator AnimateWheelSpin(float targetAngle)
    {
        if (wheelTransform == null) yield break;
        
        float startAngle = wheelTransform.localEulerAngles.z;
        float elapsedTime = 0f;
        
        while (elapsedTime < spinDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / spinDuration;
            float curveValue = spinCurve.Evaluate(t);
            
            float currentAngle = Mathf.Lerp(startAngle, startAngle + targetAngle, curveValue);
            wheelTransform.localEulerAngles = new Vector3(0f, 0f, currentAngle);
            
            yield return null;
        }
        
        wheelTransform.localEulerAngles = new Vector3(0f, 0f, startAngle + targetAngle);
    }

    void ProcessSpinResult(WheelSegment result)
    {
        int winAmount = Mathf.RoundToInt(currentBet * result.multiplier);
        
        if (result.multiplier > 0f)
        {
            // Win!
            GiveScrap(winAmount);
            ShowResult($"YOU WIN! +{winAmount} Scrap ({result.name})", Color.green);
            PlayWinEffect();
            PlaySound(winSound);
        }
        else
        {
            // Lose
            ShowResult($"YOU LOSE! -{currentBet} Scrap", Color.red);
            PlayLoseEffect();
            PlaySound(loseSound);
        }
        
        UpdateScrapDisplay();
        
        // Log gambling activity
        LogGamblingActivity(currentBet, winAmount, result);
    }

    void ConsumeScrap(int amount)
    {
        // Remove scrap from player inventory
        Debug.Log($"Consumed {amount} scrap");
        // playerInventory.RemoveItem(scrapItemData, amount);
    }

    void GiveScrap(int amount)
    {
        // Add scrap to player inventory
        Debug.Log($"Gave {amount} scrap");
        // playerInventory.AddItem(scrapItemData, amount);
    }

    void ShowResult(string message, Color color)
    {
        if (resultText != null)
        {
            resultText.text = message;
            resultText.color = color;
            
            // Fade out result after delay
            StartCoroutine(FadeResultText());
        }
    }

    IEnumerator FadeResultText()
    {
        yield return new WaitForSeconds(3f);
        
        if (resultText != null)
            resultText.text = "";
    }

    void PlayWinEffect()
    {
        if (winEffect != null)
            winEffect.Play();
    }

    void PlayLoseEffect()
    {
        if (loseEffect != null)
            loseEffect.Play();
    }

    void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
            audioSource.PlayOneShot(clip);
    }

    void LogGamblingActivity(int betAmount, int winAmount, WheelSegment result)
    {
        // Log for admin monitoring and statistics
        Debug.Log($"Player {currentPlayer.name} bet {betAmount} scrap, won {winAmount} scrap on {result.name}");
    }

    // Mobile touch support
    public void OnMobileBetIncrease()
    {
        currentBet = Mathf.Min(currentBet + 10, maxBet);
        if (betSlider != null)
            betSlider.value = currentBet;
        UpdateBetDisplay();
    }

    public void OnMobileBetDecrease()
    {
        currentBet = Mathf.Max(currentBet - 10, minBet);
        if (betSlider != null)
            betSlider.value = currentBet;
        UpdateBetDisplay();
    }
}

[System.Serializable]
public class WheelSegment
{
    public string name;
    public Color color;
    public float multiplier; // 0 = lose, 1 = break even, >1 = win
    public float probability; // 0-1, should sum to 1 across all segments
    
    public bool Equals(WheelSegment other)
    {
        return other != null && name == other.name && multiplier == other.multiplier;
    }
}

public class WheelSegmentVisual : MonoBehaviour
{
    public void Setup(WheelSegment data, float angle)
    {
        // Create visual representation of wheel segment
        // This would create a pie-slice shaped mesh with the appropriate color
        
        GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Quad);
        visual.transform.SetParent(transform);
        visual.transform.localPosition = Vector3.zero;
        
        Renderer renderer = visual.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material.color = data.color;
        }
        
        // Add text label
        GameObject textObj = new GameObject("Label");
        textObj.transform.SetParent(transform);
        textObj.transform.localPosition = Vector3.forward * 0.8f;
        
        TextMesh textMesh = textObj.AddComponent<TextMesh>();
        textMesh.text = data.name;
        textMesh.fontSize = 20;
        textMesh.color = Color.white;
        textMesh.anchor = TextAnchor.MiddleCenter;
    }
}