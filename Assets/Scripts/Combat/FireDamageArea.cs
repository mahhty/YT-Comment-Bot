using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class FireDamageArea : MonoBehaviour
{
    [Header("Fire Properties")]
    public float burnDuration = 30f;
    public float damagePerSecond = 5f;
    public float burnRadius = 3f;
    public string attackerId = "";
    
    [Header("Visual Effects")]
    public ParticleSystem fireParticles;
    public ParticleSystem smokeParticles;
    public Light fireLight;
    public AudioSource burnAudio;
    
    [Header("Spread Settings")]
    public bool canSpread = true;
    public float spreadChance = 0.1f; // 10% chance per second to spread to nearby wood
    public float spreadRadius = 2f;
    public float maxSpreadDistance = 10f;
    
    private float remainingBurnTime;
    private HashSet<BuildingPiece> burningBuildings = new HashSet<BuildingPiece>();
    private Vector3 originalPosition;
    private int damageTicksPerSecond = 4; // Damage 4 times per second for smooth effect

    public void Initialize(float duration, float dps, float radius, string attacker)
    {
        burnDuration = duration;
        damagePerSecond = dps;
        burnRadius = radius;
        attackerId = attacker;
        
        remainingBurnTime = burnDuration;
        originalPosition = transform.position;
        
        StartBurning();
    }

    void Start()
    {
        if (remainingBurnTime <= 0f)
        {
            remainingBurnTime = burnDuration;
            originalPosition = transform.position;
            StartBurning();
        }
    }

    void StartBurning()
    {
        // Start visual effects
        if (fireParticles != null)
            fireParticles.Play();
            
        if (smokeParticles != null)
            smokeParticles.Play();
            
        if (fireLight != null)
            fireLight.enabled = true;
            
        if (burnAudio != null)
            burnAudio.Play();
        
        // Start damage and spread coroutines
        StartCoroutine(BurnDamageLoop());
        
        if (canSpread)
            StartCoroutine(FireSpreadLoop());
    }

    IEnumerator BurnDamageLoop()
    {
        float damageInterval = 1f / damageTicksPerSecond;
        float damagePerTick = damagePerSecond / damageTicksPerSecond;
        
        while (remainingBurnTime > 0f)
        {
            ApplyFireDamage(damagePerTick);
            remainingBurnTime -= damageInterval;
            
            // Update fire intensity based on remaining time
            UpdateFireIntensity();
            
            yield return new WaitForSeconds(damageInterval);
        }
        
        ExtinguishFire();
    }

    IEnumerator FireSpreadLoop()
    {
        while (remainingBurnTime > 0f)
        {
            if (Random.value < spreadChance)
            {
                TrySpreadFire();
            }
            
            yield return new WaitForSeconds(1f);
        }
    }

    void ApplyFireDamage(float damage)
    {
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, burnRadius);
        
        foreach (Collider hitCollider in hitColliders)
        {
            float distance = Vector3.Distance(transform.position, hitCollider.transform.position);
            float damageMultiplier = 1f - (distance / burnRadius);
            
            // Damage buildings
            BuildingPiece building = hitCollider.GetComponent<BuildingPiece>();
            if (building != null)
            {
                float buildingDamage = CalculateFireDamage(building, damage * damageMultiplier);
                building.TakeDamage(buildingDamage, DamageType.Fire, attackerId);
                
                // Track burning buildings
                if (!burningBuildings.Contains(building))
                {
                    burningBuildings.Add(building);
                    AddBurningEffect(building);
                }
            }
            
            // Damage players
            PlayerController player = hitCollider.GetComponent<PlayerController>();
            if (player != null)
            {
                SurvivalManager survival = player.GetComponent<SurvivalManager>();
                if (survival != null)
                {
                    float playerDamage = damage * damageMultiplier * 0.3f; // Reduced damage to players
                    survival.TakeDamage(playerDamage, "Fire");
                }
            }
            
            // Damage sleepers
            SleeperController sleeper = hitCollider.GetComponent<SleeperController>();
            if (sleeper != null)
            {
                float sleeperDamage = damage * damageMultiplier * 0.3f;
                sleeper.TakeDamage(sleeperDamage, attackerId);
            }
        }
    }

    float CalculateFireDamage(BuildingPiece building, float baseDamage)
    {
        BuildingTier tier = building.GetTier();
        
        switch (tier)
        {
            case BuildingTier.Twig:
                return baseDamage * 3f; // Very vulnerable to fire
            case BuildingTier.Wood:
                return baseDamage * 2f; // Vulnerable to fire
            case BuildingTier.Stone:
                return baseDamage * 0.1f; // Resistant to fire
            case BuildingTier.Metal:
                return baseDamage * 0.05f; // Very resistant to fire
            case BuildingTier.HQM:
                return baseDamage * 0.02f; // Extremely resistant to fire
            default:
                return baseDamage;
        }
    }

    void TrySpreadFire()
    {
        // Find nearby wood structures to spread to
        Collider[] nearbyObjects = Physics.OverlapSphere(transform.position, spreadRadius);
        
        foreach (Collider obj in nearbyObjects)
        {
            BuildingPiece building = obj.GetComponent<BuildingPiece>();
            if (building != null && 
                (building.GetTier() == BuildingTier.Wood || building.GetTier() == BuildingTier.Twig) &&
                !HasNearbyFire(building.transform.position))
            {
                float distanceFromOriginal = Vector3.Distance(originalPosition, building.transform.position);
                
                // Don't spread too far from original fire
                if (distanceFromOriginal <= maxSpreadDistance)
                {
                    SpreadFireTo(building.transform.position);
                    break; // Only spread to one location per attempt
                }
            }
        }
    }

    bool HasNearbyFire(Vector3 position)
    {
        // Check if there's already a fire nearby
        Collider[] fires = Physics.OverlapSphere(position, 1f);
        foreach (Collider fire in fires)
        {
            if (fire.GetComponent<FireDamageArea>() != null)
                return true;
        }
        return false;
    }

    void SpreadFireTo(Vector3 position)
    {
        // Create a new fire at the spread location
        GameObject newFire = Instantiate(gameObject, position, Quaternion.identity);
        FireDamageArea newFireArea = newFire.GetComponent<FireDamageArea>();
        
        if (newFireArea != null)
        {
            // Spread fires burn for less time
            float spreadBurnTime = remainingBurnTime * 0.6f;
            newFireArea.Initialize(spreadBurnTime, damagePerSecond * 0.8f, burnRadius * 0.8f, attackerId);
            newFireArea.canSpread = false; // Prevent infinite spreading
        }
        
        Debug.Log($"Fire spread to {position}");
    }

    void AddBurningEffect(BuildingPiece building)
    {
        // Add visual burning effect to the building
        GameObject burningEffect = new GameObject("BurningEffect");
        burningEffect.transform.SetParent(building.transform);
        burningEffect.transform.localPosition = Vector3.up * 0.5f;
        
        // Add small fire particles to the building
        if (fireParticles != null)
        {
            ParticleSystem buildingFire = Instantiate(fireParticles, burningEffect.transform);
            var main = buildingFire.main;
            main.startSize = 0.3f; // Smaller for building fires
            main.maxParticles = 20;
        }
        
        // Remove effect when fire dies out
        StartCoroutine(RemoveBurningEffect(burningEffect, remainingBurnTime));
    }

    IEnumerator RemoveBurningEffect(GameObject effect, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (effect != null)
            Destroy(effect);
    }

    void UpdateFireIntensity()
    {
        float intensityPercent = remainingBurnTime / burnDuration;
        
        // Scale particle effects
        if (fireParticles != null)
        {
            var emission = fireParticles.emission;
            emission.rateOverTime = Mathf.Lerp(5f, 50f, intensityPercent);
        }
        
        if (smokeParticles != null)
        {
            var emission = smokeParticles.emission;
            emission.rateOverTime = Mathf.Lerp(2f, 20f, intensityPercent);
        }
        
        // Scale light intensity
        if (fireLight != null)
        {
            fireLight.intensity = Mathf.Lerp(0.2f, 2f, intensityPercent);
            fireLight.range = Mathf.Lerp(2f, burnRadius * 2f, intensityPercent);
        }
        
        // Scale audio volume
        if (burnAudio != null)
        {
            burnAudio.volume = Mathf.Lerp(0.1f, 0.8f, intensityPercent);
        }
    }

    void ExtinguishFire()
    {
        // Stop all effects
        if (fireParticles != null)
        {
            fireParticles.Stop();
            var emission = fireParticles.emission;
            emission.enabled = false;
        }
        
        if (smokeParticles != null)
        {
            // Keep smoke for a bit longer
            var main = smokeParticles.main;
            main.startLifetime = 2f;
            
            StartCoroutine(DelayedStop(smokeParticles, 3f));
        }
        
        if (fireLight != null)
            fireLight.enabled = false;
            
        if (burnAudio != null)
            burnAudio.Stop();
        
        // Clear burning buildings list
        burningBuildings.Clear();
        
        // Destroy fire object after effects finish
        Destroy(gameObject, 5f);
    }

    IEnumerator DelayedStop(ParticleSystem particles, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (particles != null)
            particles.Stop();
    }

    // Public methods for external control
    public void Extinguish()
    {
        remainingBurnTime = 0f;
        StopAllCoroutines();
        ExtinguishFire();
    }

    public void AddFuel(float extraBurnTime)
    {
        remainingBurnTime += extraBurnTime;
    }

    public float GetRemainingBurnTime()
    {
        return remainingBurnTime;
    }

    public bool IsActive()
    {
        return remainingBurnTime > 0f;
    }

    // Environmental interaction
    void OnTriggerEnter(Collider other)
    {
        // Water extinguishes fire
        if (other.CompareTag("Water"))
        {
            Extinguish();
        }
    }

    // Debug visualization
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, burnRadius);
        
        if (canSpread)
        {
            Gizmos.color = Color.orange;
            Gizmos.DrawWireSphere(transform.position, spreadRadius);
        }
    }
}