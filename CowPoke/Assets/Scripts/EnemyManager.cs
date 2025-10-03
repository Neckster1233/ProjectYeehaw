using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Central manager that updates enemies in a single loop to avoid per-enemy Update() overhead.
/// - Keep this component on a persistent GameObject in the scene (e.g. GameManager).
/// - It finds the player by tag and exposes PlayerTransform for enemies to use.
/// </summary>
public class EnemyManager : MonoBehaviour
{
    public static EnemyManager Instance { get; private set; }
    public static Transform PlayerTransform { get; set; }

    List<Enemy> enemies = new List<Enemy>(256);

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(this);
            return;
        }

        var p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) PlayerTransform = p.transform;
    }

    void Update()
    {
        float dt = Time.deltaTime;
        // iterate with for-loop to avoid allocations
        for (int i = 0; i < enemies.Count; i++)
        {
            var e = enemies[i];
            if (e != null && e.gameObject.activeInHierarchy)
            {
                e.Tick(dt);
            }
        }
    }

    public void Register(Enemy e)
    {
        if (e == null) return;
        if (enemies.Contains(e)) return;
        enemies.Add(e);
    }

    public void Unregister(Enemy e)
    {
        if (e == null) return;
        enemies.Remove(e);
    }
}
