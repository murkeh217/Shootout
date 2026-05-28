using UnityEngine;

public class HealthPickup : PickupBase
{
    public RunData runData;
    public int healAmount = 25;

    private void Awake()
    {
        if (runData == null)
        {
            var gm = FindObjectOfType<RoguelikeGameManager>();
            if (gm != null) runData = gm.runData;
        }
    }

    protected override bool TryApply(GameObject player)
    {
        if (runData == null) return false;
        int before = runData.currentHP;
        runData.Heal(healAmount);

        var gm = FindObjectOfType<RoguelikeGameManager>();
        if (gm != null && gm.hudManager != null)
            gm.hudManager.UpdateHealthDisplay(runData.currentHP, runData.GetEffectiveMaxHP());

        return runData.currentHP != before;
    }
}

