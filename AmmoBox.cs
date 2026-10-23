using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AmmoBox : MonoBehaviour
{

    [Header("Network Pickup")]
    [SerializeField]
    private string networkPickupId;

    public string NetworkPickupId => networkPickupId;
    public int ammoAmount = 50;
    public AmmoType ammoType;

    public enum  AmmoType
    {
        AK74Ammo,
        M249Ammo,
        M107Ammo
    }
}
