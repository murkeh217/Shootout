using UnityEngine;

public class ExitTrigger : MonoBehaviour
{
    public RoguelikeGameManager gameManager;

    private void Awake()
    {
        if (gameManager == null)
            gameManager = FindObjectOfType<RoguelikeGameManager>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        gameManager?.OnPlayerReachedExit();
    }
}

