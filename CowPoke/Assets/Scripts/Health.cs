using UnityEngine;

/// <summary>
/// Small, reusable Health component.
/// - Use TakeDamage(int) to apply damage (Projectile.SendMessage("TakeDamage", damage) will call this)
/// - Call Heal(int) to restore health
/// - Die() is invoked when health reaches zero; by default the GameObject is destroyed.
/// </summary>
public class Health : MonoBehaviour
{
    [Tooltip("Maximum hit points.")]
    public int maxHealth = 3;

    [Tooltip("Current hit points (for debugging you can set this in inspector).")]
    public int currentHealth = -1;

    [Tooltip("If true the GameObject will be destroyed when health reaches zero.")]
    public bool destroyOnDeath = true;

    [Tooltip("Optional effect prefab to spawn when this object dies.")]
    public GameObject deathEffectPrefab;

    public bool IsDead => currentHealth <= 0;

    void Awake()
    {
        if (currentHealth < 0) currentHealth = maxHealth;
    }

    /// <summary>
    /// Apply damage. This method signature is compatible with SendMessage from Projectile.
    /// </summary>
    public void TakeDamage(int amount)
    {
        if (IsDead) return;
        currentHealth -= amount;
        if (currentHealth <= 0)
        {
            Die();
        }
    }

    public void Heal(int amount)
    {
        if (IsDead) return;
        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
    }

    public void Die()
    {
        // spawn effect if provided
        if (deathEffectPrefab != null)
        {
            Instantiate(deathEffectPrefab, transform.position, Quaternion.identity);
        }

        // optionally destroy
        if (destroyOnDeath)
        {
            Destroy(gameObject);
        }
    }
}
