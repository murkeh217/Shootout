using System;
using UnityEngine;

public class EnemyHealth : MonoBehaviour, IDamageable
{
    [Header("Stats")]
    public int maxHP = 30;
    private int currentHP;

    // Room listens to this event
    public event Action<EnemyHealth> OnDied;

    private bool isDead = false;

    private void Awake()
    {
        currentHP = maxHP;
    }

    public void TakeDamage(int amount)
    {
        if (isDead) return;

        currentHP -= amount;

        if (currentHP <= 0)
            Die();
    }

    private void Die()
    {
        if (isDead) return;
        isDead = true;

        OnDied?.Invoke(this);

        // Destroy after a short delay so death animation can play
        Destroy(gameObject, 1.5f);
    }
}