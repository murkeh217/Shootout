using UnityEngine;

public class CurrencyPickup : PickupBase
{
    public RunData runData;
    public int amount = 10;

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
        runData.AddCurrency(amount);
        return true;
    }
}

