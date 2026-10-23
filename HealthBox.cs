using UnityEngine;

public class HealthBox : MonoBehaviour
{
    [Header("Network Pickup")]
    [SerializeField]
    private string networkPickupId;

    public string NetworkPickupId => networkPickupId;
}