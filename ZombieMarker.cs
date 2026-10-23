using UnityEngine;

public class ZombieMarker : MonoBehaviour
{
    [Header("Assign the crystal/stone object")]
    public GameObject markerObject;

    private void Awake()
    {
        if (markerObject != null)
            markerObject.SetActive(false);
    }

    public void ShowMarker(bool show)
    {
        if (markerObject == null) return;
        markerObject.SetActive(show);
    }
}