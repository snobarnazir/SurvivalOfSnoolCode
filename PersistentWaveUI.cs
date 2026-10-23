using UnityEngine;

public class PersistentWaveUI : MonoBehaviour
{
    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }
}