using UnityEngine;

public static class PerkRoller
{
    // v1: hard-coded perk pool (works even if you don't create perk assets yet).
    // Later you can replace this with a ScriptableObject database.
    private static readonly PerkDefinition[] Builtins = new PerkDefinition[]
    {
        CreateBuiltin("dmg_10", "Hollow Point", "+10% damage", dmg: 1.10f),
        CreateBuiltin("rof_12", "Trigger Happy", "+12% fire rate", rof: 1.12f),
        CreateBuiltin("ms_10", "Lightweight Boots", "+10% move speed", ms: 1.10f),
        CreateBuiltin("hp_15", "Thick Skin", "+15 max HP", hp: 15),
        CreateBuiltin("crit_05", "Lucky Charm", "+5% crit chance", crit: 0.05f),
        CreateBuiltin("dmg_20", "Berserker Rounds", "+20% damage", dmg: 1.20f),
    };

    public static PerkDefinition[] Roll3(RunData runData)
    {
        // deterministic-ish per room clear: use loot seed + current kills
        int seed = runData != null ? runData.GetLootSeed() + runData.totalKills : Random.Range(1, 999999);
        Random.InitState(seed);

        PerkDefinition[] result = new PerkDefinition[3];
        int safety = 0;
        int filled = 0;

        while (filled < 3 && safety++ < 100)
        {
            PerkDefinition perk = Builtins[Random.Range(0, Builtins.Length)];
            if (runData != null && runData.HasPerk(perk.id)) continue;
            if (Contains(result, perk)) continue;
            result[filled++] = perk;
        }

        // If we couldn't find enough unique perks (late-run), allow repeats.
        while (filled < 3)
        {
            result[filled++] = Builtins[Random.Range(0, Builtins.Length)];
        }

        return result;
    }

    private static bool Contains(PerkDefinition[] arr, PerkDefinition perk)
    {
        for (int i = 0; i < arr.Length; i++)
        {
            if (arr[i] == perk) return true;
        }
        return false;
    }

    private static PerkDefinition CreateBuiltin(string id, string name, string desc, float dmg = 1f, float rof = 1f, float ms = 1f, int hp = 0, float crit = 0f)
    {
        // Create at runtime; not an asset on disk, but still a ScriptableObject instance.
        PerkDefinition perk = ScriptableObject.CreateInstance<PerkDefinition>();
        perk.id = id;
        perk.displayName = name;
        perk.description = desc;
        perk.damageMultiplier = dmg;
        perk.fireRateMultiplier = rof;
        perk.moveSpeedMultiplier = ms;
        perk.maxHpBonus = hp;
        perk.critChanceBonus = crit;
        return perk;
    }
}

