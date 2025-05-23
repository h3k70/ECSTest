using UnityEngine;

public class CameraTargetSingleton : MonoBehaviour
{
    public static CameraTargetSingleton Instance;

    public void Awake()
    {
        if (Instance != null)
        {
            Debug.LogWarning("Instanse alrady created");
            Destroy(Instance);
            return;
        }

        Instance = this;
    }
}
