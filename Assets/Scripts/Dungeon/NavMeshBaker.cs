using System.Collections;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;

public class NavMeshBaker : MonoBehaviour
{
    [Header("References")]
    public NavMeshSurface surface;

    [Header("Timing")]
    [Tooltip("Seconds to wait after dungeon generates before baking. " +
             "Gives Instantiate calls time to fully register in the scene.")]
    public float bakeDelay = 0.15f;

    private void Awake()
    {
        if (surface == null)
            surface = GetComponent<NavMeshSurface>();

        if (surface == null)
            Debug.LogError("[NavMeshBaker] No NavMeshSurface assigned or found.");
    }

    // -------------------------------------------------------
    // Call this from RoguelikeGameManager after GenDungeon()
    // -------------------------------------------------------
    public IEnumerator BakeAsync()
    {
        if (surface == null) yield break;

        // Remove the previous NavMesh data first
        // Skipping this causes old paths to linger under new geometry
        surface.RemoveData();

        // Wait for bakeDelay seconds — lets all Instantiated
        // floor tiles fully register their mesh colliders
        yield return new WaitForSeconds(bakeDelay);

        // Bake the new NavMesh
        // This call blocks for one frame on mobile — keep bakeDelay
        // small but non-zero to avoid baking before meshes are ready
        surface.BuildNavMesh();

        // One extra frame for the bake to fully commit
        yield return null;

        Debug.Log("[NavMeshBaker] Bake complete.");
    }

    // Synchronous version — only use in editor or loading screens
    // Never call this during normal gameplay on mobile
    public void BakeImmediate()
    {
        if (surface == null) return;
        surface.RemoveData();
        surface.BuildNavMesh();
        Debug.Log("[NavMeshBaker] Immediate bake complete.");
    }
}