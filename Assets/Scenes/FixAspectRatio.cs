using UnityEngine;

public class FixAspectRatio : MonoBehaviour
{
    void Start()
	{
		Screen.SetResolution ((int)Screen.width, (int)Screen.height, true);
	}
}
