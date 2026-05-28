using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    public RunData runData;  // same asset dragged in
    public RoguelikeGameManager gameManager;

    public void TakeDamage(int amount)
    {
        if (runData == null) return;

        runData.TakeDamage(amount);

        if (gameManager == null)
            gameManager = FindObjectOfType<RoguelikeGameManager>();

        if (gameManager != null && gameManager.hudManager != null)
            gameManager.hudManager.UpdateHealthDisplay(runData.currentHP, runData.GetEffectiveMaxHP());

        if (runData.currentHP <= 0)
            Die();
    }

    private void Die()
    {
        if (gameManager == null)
            gameManager = FindObjectOfType<RoguelikeGameManager>();

        // Delegate permadeath + UI flow to the game manager.
        if (gameManager != null)
            gameManager.OnPlayerDied();
    }
}