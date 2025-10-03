using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Projectile with an explicit initialization method.
/// Instantiate a prefab with this component, then call Init(direction, speed, damage, owner, optionalLifeTime).
/// The projectile moves using Rigidbody if present (linearVelocity) otherwise moves transform.
/// Destroys itself after lifetime expires or on hit (configurable). Applies damage to a `Health` component on hit.
/// </summary>
public class Projectile : MonoBehaviour
{
    [Tooltip("Seconds before the projectile is automatically destroyed (default used when Init doesn't override).")]
    public float lifeTime = 3f;

    [Tooltip("Used only as a fallback when no Rigidbody is present and Init didn't set velocity.")]
    public float speed = 15f;

    [Tooltip("If true the projectile will be destroyed when it hits something.")]
    public bool destroyOnHit = true;

    [Tooltip("Which layers the projectile can hit. Default = Everything.")]
    public LayerMask hitLayers = ~0;

    [Header("Debug (editor)")]
    [Tooltip("Enable debug logs and gizmos for this projectile.")]
    public bool debug = false;
    [Tooltip("Color used for debug gizmos.")]
    public Color debugColor = Color.yellow;

    // runtime state
    int damage = 1;
    GameObject owner;
    float timer;
    Vector3 velocity;
    Rigidbody rb;
    Collider[] ownColliders;
    List<Collider> ignoredOwnerColliders;
    bool appliedIgnoreThisFrame = false;
    List<Collider> ignoredProjectileColliders;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        // ownColliders will be (re)captured in Init to be robust against prefab instantiation order
        ownColliders = GetComponentsInChildren<Collider>(true);
    }

    void OnEnable()
    {
        // ensure timer has a sensible default in case Init isn't called
        timer = lifeTime;
    }

    /// <summary>
    /// Initialize the projectile. Call this right after Instantiate.
    /// dir must be normalized.
    /// </summary>
    public void Init(Vector3 dir, float initSpeed, int initDamage, GameObject owner = null, float lifeOverride = -1f)
    {
        this.owner = owner;
        this.damage = initDamage;

        timer = lifeOverride > 0f ? lifeOverride : lifeTime;

        if (rb != null)
        {
            // use physics velocity
            rb.linearVelocity = dir * initSpeed;
            // ensure transform faces direction for visuals
            transform.forward = dir;
        }
        else
        {
            velocity = dir * initSpeed;
            transform.forward = dir;
        }

        if (debug)
        {
            Debug.Log($"[Projectile] Init: '{gameObject.name}' owner={(owner?owner.name:"null")} dmg={damage} speed={initSpeed} life={timer}");
        }

        // Ignore collisions between this projectile's colliders and the owner's colliders so owner can walk through their own bullets.
        if (owner != null)
        {
            // recapture own colliders here to handle timing when children are added later during instantiation
            ownColliders = GetComponentsInChildren<Collider>(true);
            var ownerCols = owner.GetComponentsInChildren<Collider>(true);

            if (debug)
            {
                int pCount = ownColliders == null ? 0 : ownColliders.Length;
                int oCount = ownerCols == null ? 0 : ownerCols.Length;
                Debug.Log($"[Projectile] Ignoring collisions: projectile colliders={pCount}, owner colliders={oCount}");
                if (pCount > 0)
                {
                    foreach (var c in ownColliders) if (c != null) Debug.Log($"[Projectile] proj collider: {c.name}");
                }
                if (oCount > 0)
                {
                    foreach (var c in ownerCols) if (c != null) Debug.Log($"[Projectile] owner collider: {c.name} (owner={owner.name})");
                }
            }

            if (ownerCols != null && ownerCols.Length > 0 && ownColliders != null && ownColliders.Length > 0)
            {
                ignoredOwnerColliders = new List<Collider>(ownerCols.Length);
                foreach (var oCol in ownerCols)
                {
                    if (oCol == null) continue;
                    ignoredOwnerColliders.Add(oCol);
                    foreach (var pCol in ownColliders)
                    {
                        if (pCol == null) continue;
                        Physics.IgnoreCollision(pCol, oCol, true);
                    }
                }

                // Some setups (rare) require physics to settle; schedule a retry next frame to ensure ignores are applied.
                appliedIgnoreThisFrame = true;
                StartCoroutine(ApplyIgnoreNextFrame(owner));
            }
            else if (debug)
            {
                Debug.LogWarning($"[Projectile] No colliders found to ignore between projectile and owner '{owner.name}'");
            }
        }

        // Also ignore collisions with other active projectiles to prevent bullets colliding with each other.
    var others = GameObject.FindObjectsByType<Projectile>(FindObjectsSortMode.None);
        if (others != null && others.Length > 0 && ownColliders != null && ownColliders.Length > 0)
        {
            ignoredProjectileColliders = new List<Collider>();
            foreach (var otherProj in others)
            {
                if (otherProj == null || otherProj == this) continue;
                var otherCols = otherProj.GetComponentsInChildren<Collider>(true);
                if (otherCols == null || otherCols.Length == 0) continue;
                foreach (var oCol in otherCols)
                {
                    if (oCol == null) continue;
                    ignoredProjectileColliders.Add(oCol);
                    foreach (var pCol in ownColliders)
                    {
                        if (pCol == null) continue;
                        Physics.IgnoreCollision(pCol, oCol, true);
                    }
                }
            }
            if (debug) Debug.Log($"[Projectile] Ignored collisions with {ignoredProjectileColliders.Count} colliders from other projectiles");
        }
    }

    System.Collections.IEnumerator ApplyIgnoreNextFrame(GameObject ownerObj)
    {
        yield return null; // wait one frame
        if (ownerObj == null) yield break;
        var ownerCols = ownerObj.GetComponentsInChildren<Collider>(true);
        ownColliders = GetComponentsInChildren<Collider>(true);
        if (ownerCols != null && ownerCols.Length > 0 && ownColliders != null && ownColliders.Length > 0)
        {
            foreach (var oCol in ownerCols)
            {
                if (oCol == null) continue;
                foreach (var pCol in ownColliders)
                {
                    if (pCol == null) continue;
                    Physics.IgnoreCollision(pCol, oCol, true);
                }
            }
            if (debug) Debug.Log($"[Projectile] Re-applied IgnoreCollision for owner {ownerObj.name} on next frame");
        }
    }

    void Update()
    {
        timer -= Time.deltaTime;
        if (timer <= 0f)
        {
            Destroy(gameObject);
            return;
        }

        if (rb == null)
        {
            transform.position += velocity * Time.deltaTime;
        }
    }

    void OnTriggerEnter(Collider other)
    {
        HandleHit(other.gameObject);
    }

    void OnCollisionEnter(Collision collision)
    {
        HandleHit(collision.gameObject);
    }

    void HandleHit(GameObject other)
    {
        if (other == null) return;

        // ignore owner and its children
        if (owner != null)
        {
            if (other == owner) return;
            if (other.transform.IsChildOf(owner.transform)) return;
        }

        if (((1 << other.layer) & hitLayers) == 0) return; // layer not included

        var health = other.GetComponent<Health>();
        if (health != null)
        {
            health.TakeDamage(damage);
            if (debug) Debug.Log($"[Projectile] '{gameObject.name}' hit '{other.name}' and dealt {damage} damage. Remaining health: {health.currentHealth}");
        }
        else
        {
            if (debug) Debug.Log($"[Projectile] '{gameObject.name}' hit '{other.name}' (no Health component)");
        }

        if (destroyOnHit)
        {
            if (debug) Debug.Log($"[Projectile] '{gameObject.name}' destroyed on hit");
            Destroy(gameObject);
        }
    }

    void OnDrawGizmos()
    {
        if (!debug) return;
        Gizmos.color = debugColor;
        Gizmos.DrawSphere(transform.position, 0.08f);
        // draw a velocity ray
        if (rb != null)
        {
            Gizmos.DrawLine(transform.position, transform.position + rb.linearVelocity.normalized * 0.4f);
        }
        else
        {
            Gizmos.DrawLine(transform.position, transform.position + velocity.normalized * 0.4f);
        }
    }

    void OnDisable()
    {
        // Restore ignored collisions created at Init so we don't permanently disable them in the physics system.
        if (ignoredOwnerColliders != null && ownColliders != null)
        {
            foreach (var oCol in ignoredOwnerColliders)
            {
                if (oCol == null) continue;
                foreach (var pCol in ownColliders)
                {
                    if (pCol == null) continue;
                    Physics.IgnoreCollision(pCol, oCol, false);
                }
            }
            ignoredOwnerColliders = null;
        }
        // Restore projectile-to-projectile ignores
        if (ignoredProjectileColliders != null && ownColliders != null)
        {
            foreach (var otherCol in ignoredProjectileColliders)
            {
                if (otherCol == null) continue;
                foreach (var pCol in ownColliders)
                {
                    if (pCol == null) continue;
                    Physics.IgnoreCollision(pCol, otherCol, false);
                }
            }
            ignoredProjectileColliders = null;
        }
    }
}
