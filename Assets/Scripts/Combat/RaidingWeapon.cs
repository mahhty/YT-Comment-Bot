using UnityEngine;
using System.Collections;

public class RaidingWeapon : MonoBehaviour
{
    [Header("Weapon Data")]
    public RaidingWeaponType weaponType;
    public float damage = 100f;
    public float explosionRadius = 5f;
    public float fuseTime = 4f; // For C4 and grenades
    public float burnDuration = 30f; // For Molotovs
    
    [Header("Damage Multipliers by Building Tier")]
    public float twigMultiplier = 2f;
    public float woodMultiplier = 1f;
    public float stoneMultiplier = 0.5f;
    public float metalMultiplier = 0.3f;
    public float hqmMultiplier = 0.2f;
    
    [Header("Special Effects")]
    public float fireMultiplierVsWood = 3f; // Molotovs are very effective vs wood
    public bool canDestroyDoors = true;
    public bool canDamageToolCupboards = false;
    
    [Header("Audio & Visual")]
    public GameObject explosionEffect;
    public GameObject fireEffect;
    public AudioClip explosionSound;
    public AudioClip igniteSound;
    public AudioClip burnSound;
    
    private bool isArmed = false;
    private bool hasExploded = false;
    private Rigidbody rb;
    private PlayerController thrower;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        StartCoroutine(FuseTimer());
    }

    public void Initialize(PlayerController player, Vector3 throwForce)
    {
        thrower = player;
        isArmed = true;
        
        if (rb != null)
        {
            rb.AddForce(throwForce, ForceMode.Impulse);
        }
        
        // Play ignite sound for certain weapons
        if (weaponType == RaidingWeaponType.Molotov && igniteSound != null)
        {
            AudioSource.PlayClipAtPoint(igniteSound, transform.position);
        }
    }

    IEnumerator FuseTimer()
    {
        if (weaponType == RaidingWeaponType.C4)
        {
            // C4 requires manual detonation (for now, auto-detonate after fuse time)
            yield return new WaitForSeconds(fuseTime);
        }
        else if (weaponType == RaidingWeaponType.Rocket)
        {
            // Rockets explode on impact (handled in OnCollisionEnter)
            yield break;
        }
        else
        {
            // Molotovs and grenades explode after fuse time
            yield return new WaitForSeconds(fuseTime);
        }
        
        if (!hasExploded)
        {
            Explode();
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if (!isArmed || hasExploded) return;
        
        // Rockets explode immediately on impact
        if (weaponType == RaidingWeaponType.Rocket)
        {
            Explode();
        }
        // C4 sticks to surfaces
        else if (weaponType == RaidingWeaponType.C4)
        {
            StickToSurface(collision);
        }
        // Molotovs explode on hard impact
        else if (weaponType == RaidingWeaponType.Molotov && collision.relativeVelocity.magnitude > 5f)
        {
            Explode();
        }
    }

    void StickToSurface(Collision collision)
    {
        // Make C4 stick to the surface
        if (rb != null)
        {
            rb.isKinematic = true;
        }
        
        // Attach to the collided object
        transform.SetParent(collision.transform);
        
        // Play stick sound/effect
        Debug.Log("C4 attached to surface");
    }

    void Explode()
    {
        if (hasExploded) return;
        hasExploded = true;
        
        // Create explosion effect
        if (explosionEffect != null)
        {
            GameObject explosion = Instantiate(explosionEffect, transform.position, Quaternion.identity);
            Destroy(explosion, 5f);
        }
        
        // Play explosion sound
        if (explosionSound != null)
        {
            AudioSource.PlayClipAtPoint(explosionSound, transform.position);
        }
        
        // Apply damage to nearby objects
        ApplyExplosionDamage();
        
        // Handle special effects (fire for Molotovs)
        if (weaponType == RaidingWeaponType.Molotov)
        {
            StartFire();
        }
        
        // Destroy the weapon object
        Destroy(gameObject, 0.1f);
    }

    void ApplyExplosionDamage()
    {
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, explosionRadius);
        
        foreach (Collider hitCollider in hitColliders)
        {
            float distance = Vector3.Distance(transform.position, hitCollider.transform.position);
            float damageMultiplier = 1f - (distance / explosionRadius);
            
            // Damage buildings
            BuildingPiece building = hitCollider.GetComponent<BuildingPiece>();
            if (building != null)
            {
                float finalDamage = CalculateBuildingDamage(building, damageMultiplier);
                building.TakeDamage(finalDamage, GetDamageType(), GetAttackerId());
            }
            
            // Damage players
            PlayerController player = hitCollider.GetComponent<PlayerController>();
            if (player != null && player != thrower)
            {
                SurvivalManager survival = player.GetComponent<SurvivalManager>();
                if (survival != null)
                {
                    float playerDamage = damage * damageMultiplier * 0.5f; // Reduced damage to players
                    survival.TakeDamage(playerDamage, "Explosion");
                }
            }
            
            // Damage sleepers
            SleeperController sleeper = hitCollider.GetComponent<SleeperController>();
            if (sleeper != null)
            {
                float sleeperDamage = damage * damageMultiplier * 0.5f;
                sleeper.TakeDamage(sleeperDamage, GetAttackerId());
            }
            
            // Damage tool cupboards (if weapon can)
            if (canDamageToolCupboards)
            {
                ToolCupboard cupboard = hitCollider.GetComponent<ToolCupboard>();
                if (cupboard != null)
                {
                    // Tool cupboards are very resistant
                    float cupboardDamage = damage * damageMultiplier * 0.1f;
                    // Apply damage to cupboard (would need to implement health system for cupboards)
                    Debug.Log($"Tool cupboard took {cupboardDamage} damage");
                }
            }
        }
    }

    float CalculateBuildingDamage(BuildingPiece building, float distanceMultiplier)
    {
        float baseDamage = damage * distanceMultiplier;
        float tierMultiplier = GetTierMultiplier(building.GetTier());
        
        // Special case: Fire damage vs wood
        if (weaponType == RaidingWeaponType.Molotov && building.GetTier() == BuildingTier.Wood)
        {
            tierMultiplier *= fireMultiplierVsWood;
        }
        
        return baseDamage * tierMultiplier;
    }

    float GetTierMultiplier(BuildingTier tier)
    {
        switch (tier)
        {
            case BuildingTier.Twig: return twigMultiplier;
            case BuildingTier.Wood: return woodMultiplier;
            case BuildingTier.Stone: return stoneMultiplier;
            case BuildingTier.Metal: return metalMultiplier;
            case BuildingTier.HQM: return hqmMultiplier;
            default: return 1f;
        }
    }

    DamageType GetDamageType()
    {
        switch (weaponType)
        {
            case RaidingWeaponType.C4:
            case RaidingWeaponType.Rocket:
                return DamageType.Explosive;
            case RaidingWeaponType.Molotov:
                return DamageType.Fire;
            default:
                return DamageType.Explosive;
        }
    }

    string GetAttackerId()
    {
        return thrower != null ? thrower.name : "Unknown";
    }

    void StartFire()
    {
        if (fireEffect != null)
        {
            GameObject fire = Instantiate(fireEffect, transform.position, Quaternion.identity);
            FireDamageArea fireArea = fire.GetComponent<FireDamageArea>();
            
            if (fireArea == null)
                fireArea = fire.AddComponent<FireDamageArea>();
                
            fireArea.Initialize(burnDuration, damage * 0.1f, explosionRadius * 0.5f, GetAttackerId());
        }
    }

    // Static methods for crafting recipes
    public static RaidingWeaponData GetWeaponData(RaidingWeaponType type)
    {
        switch (type)
        {
            case RaidingWeaponType.C4:
                return new RaidingWeaponData
                {
                    name = "Timed Explosive Charge",
                    damage = 550f,
                    cost = new RecipeIngredient[]
                    {
                        new RecipeIngredient { itemData = GetItemData("Explosives"), amount = 20 },
                        new RecipeIngredient { itemData = GetItemData("TechTrash"), amount = 5 },
                        new RecipeIngredient { itemData = GetItemData("Cloth"), amount = 5 }
                    },
                    craftTime = 30f
                };
                
            case RaidingWeaponType.Rocket:
                return new RaidingWeaponData
                {
                    name = "High Velocity Rocket",
                    damage = 400f,
                    cost = new RecipeIngredient[]
                    {
                        new RecipeIngredient { itemData = GetItemData("Explosives"), amount = 10 },
                        new RecipeIngredient { itemData = GetItemData("MetalOre"), amount = 100 },
                        new RecipeIngredient { itemData = GetItemData("LowGradeFuel"), amount = 150 }
                    },
                    craftTime = 20f
                };
                
            case RaidingWeaponType.Molotov:
                return new RaidingWeaponData
                {
                    name = "Molotov Cocktail",
                    damage = 100f,
                    cost = new RecipeIngredient[]
                    {
                        new RecipeIngredient { itemData = GetItemData("LowGradeFuel"), amount = 50 },
                        new RecipeIngredient { itemData = GetItemData("Cloth"), amount = 1 }
                    },
                    craftTime = 5f
                };
                
            default:
                return new RaidingWeaponData { name = "Unknown", damage = 0f };
        }
    }

    static ItemData GetItemData(string itemName)
    {
        // This would fetch actual ItemData from a database/registry
        return new ItemData { itemName = itemName };
    }

    // Debug visualization
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }
}

public enum RaidingWeaponType
{
    C4,
    Rocket,
    Molotov,
    Grenade,
    Satchel
}

[System.Serializable]
public class RaidingWeaponData
{
    public string name;
    public float damage;
    public RecipeIngredient[] cost;
    public float craftTime;
}