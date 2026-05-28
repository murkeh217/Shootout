using UnityEngine;

public class JUTPSSpawnFix : MonoBehaviour
{
    // -------------------------------------------------------
    // Inspector references
    // -------------------------------------------------------

    [Header("Player References")]
    [Tooltip("The Rigidbody on your JU TPS Character")]
    public Rigidbody playerRigidbody;

    [Tooltip("The JU TPS Character root GameObject")]
    public GameObject juTPSPlayer;

    // -------------------------------------------------------
    // Unity lifecycle
    // -------------------------------------------------------

    private void Awake()
    {
        // Auto-find the Rigidbody if not assigned in Inspector
        if (playerRigidbody == null && juTPSPlayer != null)
        {
            playerRigidbody = juTPSPlayer.GetComponent<Rigidbody>();

            if (playerRigidbody == null)
                Debug.LogWarning("[SpawnFix] No Rigidbody found on juTPSPlayer.");
        }
    }

    // -------------------------------------------------------
    // Public API — called by RoguelikeGameManager
    // before the dungeon generator moves the player
    // -------------------------------------------------------

    public void ZeroVelocity()
    {
        if (playerRigidbody == null) return;

        playerRigidbody.linearVelocity = Vector3.zero;
        playerRigidbody.angularVelocity = Vector3.zero;

        Debug.Log("[SpawnFix] Player velocity zeroed.");
    }

    // Teleports the player to a specific position safely
    // Use this if you ever need to move the player manually
    // instead of relying on the dungeon generator's spawn
    public void TeleportPlayer(Vector3 position)
    {
        if (playerRigidbody == null || juTPSPlayer == null) return;

        // Zero velocity first — always
        ZeroVelocity();

        // MovePosition is physics-safe for Rigidbody characters
        // Never set transform.position directly on a Rigidbody
        playerRigidbody.MovePosition(position);

        Debug.Log($"[SpawnFix] Player teleported to {position}");
    }

    // Call this if the player gets stuck inside geometry after spawn
    // Nudges them upward until they're clear
    public void EjectFromGeometry()
    {
        if (juTPSPlayer == null) return;

        Vector3 pos = juTPSPlayer.transform.position;
        juTPSPlayer.transform.position = pos + Vector3.up * 0.5f;

        ZeroVelocity();

        Debug.Log("[SpawnFix] Ejected player from geometry.");
    }
}