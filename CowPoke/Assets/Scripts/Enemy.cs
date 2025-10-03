using UnityEngine;

/// <summary>
/// Simple enemy that moves toward the player and has a Health component.
/// - Set the player's tag (default "Player") to find the target.
/// - Uses Rigidbody.velocity if present, otherwise moves transform.
/// - Stops when it reaches closeDistance.
/// </summary>
[RequireComponent(typeof(Collider))]
public class Enemy : MonoBehaviour
{
    [Tooltip("Tag used to find the player GameObject.")]
    public string playerTag = "Player";

    [Tooltip("Movement speed in units per second.")]
    public float speed = 2f;

    [Tooltip("Distance at which the enemy will stop moving toward player.")]
    public float closeDistance = 0.6f;

    [Tooltip("Use Rigidbody for movement if present.")]
    public bool useRigidbody = true;

    Transform player;
    Rigidbody rb;
    [Header("Debug (editor)")]
    [Tooltip("Enable debug logging and gizmos for this enemy.")]
    public bool debug = false;
    [Tooltip("Color used to draw health gizmo.")]
    public Color healthColor = Color.red;

    // health tracking for debug logs
    int cachedHealth = int.MinValue;

    void Awake()
    {
        // We'll let the EnemyManager provide the player transform to avoid each enemy doing a Find.
        rb = GetComponent<Rigidbody>();
    }

    void OnEnable()
    {
        // Register with manager so the manager can tick this enemy in a batch loop
        EnemyManager.Instance?.Register(this);
    }

    void OnDisable()
    {
        EnemyManager.Instance?.Unregister(this);
    }

    void Start()
    {
        // In some cases (order of initialization) the manager may not exist yet when OnEnable ran.
        // Try registering again in Start to be safe.
        EnemyManager.Instance?.Register(this);
    }

    /// <summary>
    /// Called by EnemyManager each frame to perform movement. This avoids a per-enemy Update() call.
    /// </summary>
    public void Tick(float deltaTime)
    {
        var playerTransform = EnemyManager.PlayerTransform;
        if (playerTransform == null) return;

        Vector3 toPlayer = playerTransform.position - transform.position;
        toPlayer.y = 0f; // keep movement on XZ plane
        float dist = toPlayer.magnitude;
        if (dist <= closeDistance)
        {
            // reached
            if (rb != null && useRigidbody)
            {
                rb.linearVelocity = Vector3.zero;
            }
            return;
        }

        Vector3 dir = toPlayer.normalized;
        Vector3 vel = dir * speed;

        if (rb != null && useRigidbody)
        {
            rb.linearVelocity = vel;
        }
        else
        {
            transform.position += vel * deltaTime;
        }

        // Debug: check and log health changes
        if (debug)
        {
            var h = GetComponent<Health>();
            if (h != null)
            {
                if (h.currentHealth != cachedHealth)
                {
                    Debug.Log($"[Enemy] '{gameObject.name}' health changed: {cachedHealth} -> {h.currentHealth}");
                    cachedHealth = h.currentHealth;
                }
            }
        }
    }

    void OnDrawGizmosSelected()
    {
        if (!debug) return;
        var h = GetComponent<Health>();
        if (h == null) return;

        // Draw a small health bar above the enemy
        Vector3 pos = transform.position + Vector3.up * 1.2f;
        float width = 0.6f;
        float filled = Mathf.Clamp01((float)h.currentHealth / Mathf.Max(1, h.maxHealth));

        // background
        Gizmos.color = Color.black;
        Gizmos.DrawCube(pos, new Vector3(width, 0.08f, 0.01f));
        // foreground
        Gizmos.color = healthColor;
        Vector3 left = pos - Vector3.right * (width * 0.5f);
        Vector3 filledCenter = left + Vector3.right * (width * filled * 0.5f);
        Gizmos.DrawCube(filledCenter, new Vector3(width * filled, 0.06f, 0.01f));
    }
}
