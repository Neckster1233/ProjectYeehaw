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

    void Awake()
    {
        GameObject p = GameObject.FindGameObjectWithTag(playerTag);
        if (p != null) player = p.transform;
        rb = GetComponent<Rigidbody>();
    }

    void Update()
    {
        if (player == null) return;

        Vector3 toPlayer = player.position - transform.position;
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
            transform.position += vel * Time.deltaTime;
        }
    }
}
