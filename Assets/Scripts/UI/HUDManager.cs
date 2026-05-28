using UnityEngine;

public class HUDManager : MonoBehaviour
{
    public void UpdateFloorDisplay(int floor)
    {
        // TODO: update floor number text on screen
        Debug.Log($"[HUD] Floor: {floor}");
    }

    public void UpdateHealthDisplay(int current, int max)
    {
        // TODO: update health bar
        Debug.Log($"[HUD] HP: {current}/{max}");
    }

    public void UpdateAmmoDisplay(int current, int max)
    {
        // TODO: update ammo counter
        Debug.Log($"[HUD] Ammo: {current}/{max}");
    }
}