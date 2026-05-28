using UnityEngine;

[CreateAssetMenu(fileName = "RunData", menuName = "Roguelike/RunData")]
public class RunData : ScriptableObject
{
    [Header("Run seed & floor")]
    public int seed;
    public int floor;

    [Header("Player state")]
    public int currentHP;
    public int maxHP = 100;

    [Header("Inventory")]
    public int currency;
    public int ammoSlot1;
    public int ammoSlot2;

    [Header("Run stats")]
    public int totalKills;
    public int floorsCleared;
    public float runStartTime;

    [Header("Perks (in-run)")]
    public string[] acquiredPerkIds = new string[0];

    [Header("Perk modifiers (aggregated)")]
    public float damageMultiplier = 1f;
    public float fireRateMultiplier = 1f;
    public float moveSpeedMultiplier = 1f;
    public int maxHpBonus = 0;
    public float critChanceBonus = 0f;

    // -------------------------------------------------------
    // Call this when the player starts a brand new run
    // -------------------------------------------------------
    public void StartNewRun()
    {
        seed = Random.Range(1, 999999);
        floor = 0;
        ResetPerks();
        currentHP = GetEffectiveMaxHP();
        currency = 0;
        ammoSlot1 = 30;
        ammoSlot2 = 0;
        totalKills = 0;
        floorsCleared = 0;
        runStartTime = Time.time;

        Debug.Log($"[RunData] New run started. Seed: {seed}");
    }

    // -------------------------------------------------------
    // Call this when moving to the next floor
    // -------------------------------------------------------
    public void AdvanceFloor()
    {
        floor++;
        floorsCleared++;
        Debug.Log($"[RunData] Advanced to floor {floor}");
    }

    // -------------------------------------------------------
    // Call this on permadeath — wipes everything
    // -------------------------------------------------------
    public void WipeRun()
    {
        seed = 0;
        floor = 0;
        currentHP = 0;
        currency = 0;
        ammoSlot1 = 0;
        ammoSlot2 = 0;
        totalKills = 0;
        floorsCleared = 0;
        runStartTime = 0;
        ResetPerks();

        Debug.Log("[RunData] Run wiped — permadeath.");
    }

    // -------------------------------------------------------
    // Convenience helpers
    // -------------------------------------------------------

    // Returns the dungeon seed for a specific floor
    // Each floor gets a unique seed derived from the master seed
    public int GetFloorSeed()
    {
        return seed + (floor * 100);
    }

    // Returns the enemy spawn seed for the current floor
    public int GetEnemySeed()
    {
        return seed + (floor * 100) + 1;
    }

    // Returns the loot seed for the current floor
    public int GetLootSeed()
    {
        return seed + (floor * 100) + 2;
    }

    public bool IsAlive()
    {
        return currentHP > 0;
    }

    public void TakeDamage(int amount)
    {
        currentHP = Mathf.Max(0, currentHP - amount);
    }

    public void Heal(int amount)
    {
        currentHP = Mathf.Min(GetEffectiveMaxHP(), currentHP + amount);
    }

    public int GetEffectiveMaxHP()
    {
        return Mathf.Max(1, maxHP + maxHpBonus);
    }

    public bool HasPerk(string perkId)
    {
        if (string.IsNullOrEmpty(perkId)) return false;
        for (int i = 0; i < acquiredPerkIds.Length; i++)
        {
            if (acquiredPerkIds[i] == perkId) return true;
        }
        return false;
    }

    public void ApplyPerk(PerkDefinition perk)
    {
        if (perk == null || string.IsNullOrEmpty(perk.id)) return;
        if (HasPerk(perk.id)) return;

        // record
        int oldLen = acquiredPerkIds != null ? acquiredPerkIds.Length : 0;
        string[] next = new string[oldLen + 1];
        for (int i = 0; i < oldLen; i++) next[i] = acquiredPerkIds[i];
        next[oldLen] = perk.id;
        acquiredPerkIds = next;

        // apply stats
        damageMultiplier *= perk.damageMultiplier;
        fireRateMultiplier *= perk.fireRateMultiplier;
        moveSpeedMultiplier *= perk.moveSpeedMultiplier;
        maxHpBonus += perk.maxHpBonus;
        critChanceBonus += perk.critChanceBonus;

        // keep HP within new max
        currentHP = Mathf.Min(currentHP, GetEffectiveMaxHP());
    }

    public void ResetPerks()
    {
        acquiredPerkIds = new string[0];
        damageMultiplier = 1f;
        fireRateMultiplier = 1f;
        moveSpeedMultiplier = 1f;
        maxHpBonus = 0;
        critChanceBonus = 0f;
    }

    public void AddKill()
    {
        totalKills++;
    }

    public void AddCurrency(int amount)
    {
        currency += amount;
    }

    public bool SpendCurrency(int amount)
    {
        if (currency < amount) return false;
        currency -= amount;
        return true;
    }

    // Returns a readable run duration string for the death screen
    public string GetRunDuration()
    {
        float elapsed = Time.time - runStartTime;
        int minutes = Mathf.FloorToInt(elapsed / 60f);
        int seconds = Mathf.FloorToInt(elapsed % 60f);
        return $"{minutes}m {seconds}s";
    }
}