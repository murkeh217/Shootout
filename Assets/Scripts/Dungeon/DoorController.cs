using System.Collections;
using UnityEngine;

public class DoorController : MonoBehaviour
{
    // -------------------------------------------------------
    // Inspector settings
    // -------------------------------------------------------

    [Header("Door Behaviour")]
    [Tooltip("If true, uses a physical barrier. If false, uses an invisible wall.")]
    public bool useVisualDoor = true;

    [Tooltip("How fast the door rises when unlocking")]
    public float openSpeed = 3f;

    [Tooltip("Y position when locked (blocking the corridor)")]
    public float lockedY = 0f;

    [Tooltip("Y position when unlocked (sunk into floor, out of the way)")]
    public float unlockedY = -3f;

    // -------------------------------------------------------
    // Internal
    // -------------------------------------------------------

    private Collider doorCollider;
    private bool isLocked = false;
    private Coroutine moveCoroutine;

    private void Awake()
    {
        doorCollider = GetComponent<Collider>();

        // Start unlocked — locks only when player enters the room
        SetPositionImmediate(unlockedY);
    }

    // -------------------------------------------------------
    // Public API — called by Room
    // -------------------------------------------------------

    public void Lock()
    {
        isLocked = true;

        if (moveCoroutine != null) StopCoroutine(moveCoroutine);
        moveCoroutine = StartCoroutine(MoveTo(lockedY));

        if (doorCollider != null)
            doorCollider.enabled = true;
    }

    public void Unlock()
    {
        isLocked = false;

        if (moveCoroutine != null) StopCoroutine(moveCoroutine);
        moveCoroutine = StartCoroutine(MoveTo(unlockedY));

        // Keep collider on during animation, disable after
        StartCoroutine(DisableColliderAfterMove());
    }

    // -------------------------------------------------------
    // Internal
    // -------------------------------------------------------

    private IEnumerator MoveTo(float targetY)
    {
        Vector3 start = transform.position;
        Vector3 target = new Vector3(start.x, targetY, start.z);
        float elapsed = 0f;
        float duration = Mathf.Abs(targetY - start.y) / openSpeed;

        // Avoid division by zero if already at target
        if (duration < 0.01f)
        {
            transform.position = target;
            yield break;
        }

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            transform.position = Vector3.Lerp(start, target, elapsed / duration);
            yield return null;
        }

        transform.position = target;
    }

    private IEnumerator DisableColliderAfterMove()
    {
        // Wait for the door to fully sink before disabling collider
        float duration = Mathf.Abs(unlockedY - lockedY) / openSpeed;
        yield return new WaitForSeconds(duration + 0.1f);

        if (!isLocked && doorCollider != null)
            doorCollider.enabled = false;
    }

    private void SetPositionImmediate(float y)
    {
        Vector3 pos = transform.position;
        pos.y = y;
        transform.position = pos;
    }
}