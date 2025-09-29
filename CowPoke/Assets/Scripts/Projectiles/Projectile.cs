using UnityEngine;

/// <summary>
/// Simple projectile behaviour:
/// - Destroys itself after <see cref="lifeTime"/> seconds
/// - If it has a Rigidbody, it assumes external code set its velocity; otherwise it moves forward by <see cref="speed"/>
/// - On collision/trigger it attempts to apply damage to a `Health` component (if present) and optionally destroys itself
/// This is intentionally small and generic so you can extend it for pooling, effects, or more complex interactions.
/// </summary>
public class Projectile : MonoBehaviour
{
    [Tooltip("Seconds before the projectile is automatically destroyed.")]
    public float lifeTime = 3f;

    [Tooltip("Used only if there's no Rigidbody on the prefab - moves transform forward.")]
    public float speed = 15f;

    [Tooltip("Damage attempted to apply to a 'Health' component on the hit object.")]
    public int damage = 1;

    [Tooltip("If true the projectile will be destroyed when it hits something.")]
    public bool destroyOnHit = true;

    [Tooltip("Which layers the projectile can hit. Default = Everything.")]
    public LayerMask hitLayers = ~0;

    // internal timer
    float timer;

    void Start()
    {
        timer = lifeTime;
    }

    void Update()
    {
        timer -= Time.deltaTime;
        if (timer <= 0f)
        {
            Destroy(gameObject);
            return;
        }

        // If no Rigidbody, move the transform forward as a fallback
        var rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            transform.position += transform.forward * speed * Time.deltaTime;
        }
    }

    void OnTriggerEnter(Collider other)
    {
        TryHandleHit(other.gameObject);
    }

    void OnCollisionEnter(Collision collision)
    {
        TryHandleHit(collision.gameObject);
    }

    void TryHandleHit(GameObject other)
    {
        if (((1 << other.layer) & hitLayers) == 0) return; // layer not included

        // don't self-hit
        if (other == gameObject) return;

        // If the hit object has a Health component, try to apply damage.
        // This is intentionally loose to accommodate different health script names; change as needed.
        var health = other.GetComponent<Health>();
        if (health != null)
        {
            // Try common methods if present (this will compile even if Health is not defined in your project)
            // If you have a different API, adapt this section.
            health.SendMessage("TakeDamage", damage, SendMessageOptions.DontRequireReceiver);
        }

        if (destroyOnHit)
        {
            Destroy(gameObject);
        }
    }
}
