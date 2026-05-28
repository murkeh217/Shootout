using UnityEngine;

public class AmmoPickup : PickupBase
{
    public RunData runData;
    public int ammoAmount = 15;
    public bool slot1 = true;

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

        if (slot1) runData.ammoSlot1 += ammoAmount;
        else runData.ammoSlot2 += ammoAmount;

        return true;
    }
}

