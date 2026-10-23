using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.Collections;
using UnityEngine;
using static Weapon;

public class WeaponManager : NetworkBehaviour
{
    public static WeaponManager Instance { get; set; }

    public List<GameObject> weaponSlots;
    public GameObject activeWeaponSlot;

    private Camera playerCamera;
    private InteractionManager interactionManager;

    [Header("Ammo")]
    public int totalAK74Ammo = 0;
    public int totalM249Ammo = 0;
    public int totalM107Ammo = 0;

    [Header("Throwables General")]
    public float throwForce = 10f;
    public GameObject throwableSpawn;
    public float forceMultiplier = 0;
    public float forceMultiplierLimit = 2f;

    [Header("Lethals")]
    public int maxLethals = 2;
    public int lethalsCount = 0;
    public Throwable.ThrowableType equippedLethalType;
    public GameObject grenadePrefab;

    [Header("Tacticals")]
    public int tacticalCount = 0;
    public Throwable.ThrowableType equippedTacticalType;
    public GameObject smokeGrenadePrefab;
    public int maxTacticals = 2;

    private static readonly HashSet<string> pickedUpWeaponIds =
        new HashSet<string>();

    private static readonly HashSet<string> pickedUpThrowableIds =
        new HashSet<string>();

    private static readonly HashSet<string> pickedUpAmmoIds =
        new HashSet<string>();

    private Dictionary<AmmoBox.AmmoType, AmmoBox> heldAmmoBoxes =
        new Dictionary<AmmoBox.AmmoType, AmmoBox>();

    // =========================================================
    // NETWORK WEAPON STATE
    // =========================================================

    private NetworkVariable<int> networkActiveWeaponSlot =
        new NetworkVariable<int>(
            0,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Owner
        );

    private NetworkVariable<FixedString128Bytes> networkSlot1WeaponId =
        new NetworkVariable<FixedString128Bytes>(
            "",
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Owner
        );

    private NetworkVariable<FixedString128Bytes> networkSlot2WeaponId =
        new NetworkVariable<FixedString128Bytes>(
            "",
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Owner
        );


    //wednesday, 19 june 2024
    private static readonly HashSet<string> pickedUpHealthBoxIds =
    new HashSet<string>();

    // =========================================================
    // UNITY
    // =========================================================

    private void Awake()
    {
        PlayerMovement playerMovement =
            GetComponentInParent<PlayerMovement>();

        if (playerMovement != null)
        {
            playerCamera =
                playerMovement.GetComponentInChildren<Camera>(true);

            interactionManager =
                playerMovement.GetComponentInChildren<InteractionManager>(true);
        }

        if (playerCamera == null)
        {
            Debug.LogWarning(
                "WeaponManager: Player camera not found."
            );
        }

        if (interactionManager == null)
        {
            Debug.LogWarning(
                "WeaponManager: InteractionManager not found."
            );
        }
    }

    private void Start()
    {
        if (weaponSlots != null &&
            weaponSlots.Count > 0)
        {
            activeWeaponSlot = weaponSlots[0];
        }

        equippedLethalType =
            Throwable.ThrowableType.None;

        equippedTacticalType =
            Throwable.ThrowableType.None;
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        networkActiveWeaponSlot.OnValueChanged +=
            OnNetworkActiveWeaponSlotChanged;

        networkSlot1WeaponId.OnValueChanged +=
            OnNetworkSlotWeaponChanged;

        networkSlot2WeaponId.OnValueChanged +=
            OnNetworkSlotWeaponChanged;

        if (IsOwner)
        {
            SyncNetworkWeaponSlots();

            RequestPickedUpWeaponsServerRpc();
            RequestPickedUpThrowablesServerRpc();
            RequestPickedUpAmmoServerRpc();
            //wednesday, 19 june 2024
            RequestPickedUpHealthBoxesServerRpc();
        }

        StartCoroutine(
            ApplyNetworkWeaponStateAfterSpawn()
        );
    }

    public override void OnDestroy()
    {
        networkActiveWeaponSlot.OnValueChanged -=
            OnNetworkActiveWeaponSlotChanged;

        networkSlot1WeaponId.OnValueChanged -=
            OnNetworkSlotWeaponChanged;

        networkSlot2WeaponId.OnValueChanged -=
            OnNetworkSlotWeaponChanged;

        base.OnDestroy();
    }

    // =========================================================
    // NETWORK WEAPON SYNCHRONIZATION
    // =========================================================

    private void SyncNetworkWeaponSlots()
    {
        if (!IsOwner)
        {
            return;
        }

        networkSlot1WeaponId.Value =
            GetWeaponIdInSlot(0);

        networkSlot2WeaponId.Value =
            GetWeaponIdInSlot(1);
    }

    private string GetWeaponIdInSlot(
        int slotIndex)
    {
        if (weaponSlots == null ||
            slotIndex < 0 ||
            slotIndex >= weaponSlots.Count)
        {
            return string.Empty;
        }

        GameObject slot =
            weaponSlots[slotIndex];

        if (slot == null ||
            slot.transform.childCount == 0)
        {
            return string.Empty;
        }

        Weapon weapon =
            slot.transform
            .GetChild(0)
            .GetComponent<Weapon>();

        if (weapon == null)
        {
            return string.Empty;
        }

        return weapon.NetworkPickupId;
    }

    private void OnNetworkActiveWeaponSlotChanged(
        int oldSlot,
        int newSlot)
    {
        if (IsOwner)
        {
            return;
        }

        StartCoroutine(
            ApplyNetworkWeaponStateAfterSpawn()
        );
    }

    private void OnNetworkSlotWeaponChanged(
        FixedString128Bytes oldId,
        FixedString128Bytes newId)
    {
        if (IsOwner)
        {
            return;
        }

        StartCoroutine(
            ApplyNetworkWeaponStateAfterSpawn()
        );
    }

    private IEnumerator ApplyNetworkWeaponStateAfterSpawn()
    {
        for (int i = 0; i < 30; i++)
        {
            yield return null;

            if (IsOwner)
            {
                yield break;
            }

            string slot1Id =
                networkSlot1WeaponId.Value.ToString();

            string slot2Id =
                networkSlot2WeaponId.Value.ToString();

            ApplyNetworkWeaponToSlot(
                0,
                slot1Id
            );

            ApplyNetworkWeaponToSlot(
                1,
                slot2Id
            );

            ApplyRemoteWeaponSlot(
                networkActiveWeaponSlot.Value
            );

            bool slot1Ready =
                string.IsNullOrEmpty(slot1Id) ||
                HasWeaponInPlayerHierarchy(slot1Id);

            bool slot2Ready =
                string.IsNullOrEmpty(slot2Id) ||
                HasWeaponInPlayerHierarchy(slot2Id);

            if (slot1Ready && slot2Ready)
            {
                yield break;
            }
        }
    }

    private bool HasWeaponInPlayerHierarchy(
        string weaponId)
    {
        if (string.IsNullOrEmpty(weaponId))
        {
            return false;
        }

        Weapon[] weapons =
            GetComponentsInChildren<Weapon>(
                true
            );

        foreach (Weapon weapon in weapons)
        {
            if (weapon != null &&
                weapon.NetworkPickupId == weaponId)
            {
                return true;
            }
        }

        return false;
    }

    // =========================================================
    // APPLY ACTIVE SLOT
    // =========================================================

    private void ApplyRemoteWeaponSlot(
        int slotNumber)
    {
        if (weaponSlots == null ||
            weaponSlots.Count == 0)
        {
            return;
        }

        if (slotNumber < 0 ||
            slotNumber >= weaponSlots.Count)
        {
            return;
        }

        activeWeaponSlot =
            weaponSlots[slotNumber];

        for (int i = 0;
            i < weaponSlots.Count;
            i++)
        {
            GameObject slot =
                weaponSlots[i];

            if (slot == null)
            {
                continue;
            }

            bool isActive =
                i == slotNumber;

            slot.SetActive(isActive);

            if (slot.transform.childCount > 0)
            {
                Weapon weapon =
                    slot.transform
                    .GetChild(0)
                    .GetComponent<Weapon>();

                if (weapon != null)
                {
                    weapon.isActiveWeapon =
                        isActive;
                }
            }
        }
    }

    // =========================================================
    // APPLY WEAPON TO SPECIFIC SLOT
    // =========================================================

    private void ApplyNetworkWeaponToSlot(
        int slotIndex,
        string weaponId)
    {
        if (weaponSlots == null ||
            slotIndex < 0 ||
            slotIndex >= weaponSlots.Count)
        {
            return;
        }

        GameObject slot =
            weaponSlots[slotIndex];

        if (slot == null)
        {
            return;
        }

        // Empty network slot.
        if (string.IsNullOrEmpty(weaponId))
        {
            if (slot.transform.childCount > 0)
            {
                Weapon existingWeapon =
                    slot.transform
                    .GetChild(0)
                    .GetComponent<Weapon>();

                if (existingWeapon != null)
                {
                    existingWeapon.isActiveWeapon = false;

                    if (existingWeapon.animator != null)
                    {
                        existingWeapon.animator.enabled = false;
                    }

                    existingWeapon.transform.SetParent(
                        null,
                        true
                    );
                }
            }

            return;
        }

        // First search inside THIS player's hierarchy.
        Weapon targetWeapon = null;

        Weapon[] ownWeapons =
            GetComponentsInChildren<Weapon>(
                true
            );

        foreach (Weapon weapon in ownWeapons)
        {
            if (weapon != null &&
                weapon.NetworkPickupId == weaponId)
            {
                targetWeapon = weapon;
                break;
            }
        }

        // If not already inside this player,
        // search world weapons.
        if (targetWeapon == null)
        {
            Weapon[] allWeapons =
                FindObjectsByType<Weapon>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None
                );

            PlayerMovement thisPlayer =
                GetComponentInParent<PlayerMovement>();

            foreach (Weapon weapon in allWeapons)
            {
                if (weapon == null ||
                    weapon.NetworkPickupId != weaponId)
                {
                    continue;
                }

                PlayerMovement weaponPlayer =
                    weapon.GetComponentInParent<PlayerMovement>();

                // Never steal another player's weapon.
                if (weaponPlayer != null &&
                    weaponPlayer != thisPlayer)
                {
                    continue;
                }

                targetWeapon = weapon;
                break;
            }
        }

        //friday
        /*if (targetWeapon == null)
        {
            return;
        }*/

        if (targetWeapon == null)
        {
            // Starter gun (and any ID-only weapon) does not exist in the world.
            // Spawn a visual copy for remote players.
            targetWeapon = SpawnRemoteWeaponVisual(weaponId, slot);
            if (targetWeapon == null)
            {
                return;
            }
        }

        // Already in the correct slot.
        if (targetWeapon.transform.parent ==
            slot.transform)
        {
            targetWeapon.transform.localPosition =
                targetWeapon.spawnPosition;

            targetWeapon.transform.localRotation =
                Quaternion.Euler(
                    targetWeapon.spawnRotation
                );
        }
        else
        {
            // If the slot currently contains another weapon,
            // don't destroy it. The network state determines
            // what belongs there.
            targetWeapon.transform.SetParent(
                slot.transform,
                false
            );

            targetWeapon.transform.localPosition =
                targetWeapon.spawnPosition;

            targetWeapon.transform.localRotation =
                Quaternion.Euler(
                    targetWeapon.spawnRotation
                );
        }

        targetWeapon.isActiveWeapon =
            slotIndex ==
            networkActiveWeaponSlot.Value;

        Outline outline =
            targetWeapon.GetComponent<Outline>();

        if (outline != null)
        {
            outline.enabled = false;
        }

        if (targetWeapon.animator != null)
        {
            targetWeapon.animator.enabled = true;
        }

        targetWeapon.SetOwner(
            this,
            playerCamera
        );

        targetWeapon.gameObject.SetActive(true);
    }

    // =========================================================
    // UPDATE
    // =========================================================
    /*
    private void Update()
    {
        PlayerMovement playerMovement =
            GetComponentInParent<PlayerMovement>();

        if (playerMovement != null &&
            !playerMovement.IsOwner)
        {
            return;
        }

        if (weaponSlots == null ||
            weaponSlots.Count == 0)
        {
            return;
        }

        foreach (GameObject weaponSlot in weaponSlots)
        {
            if (weaponSlot == null)
            {
                continue;
            }

            weaponSlot.SetActive(
                weaponSlot == activeWeaponSlot
            );
        }

        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            SwitchActiveSlot(0);
        }

        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            SwitchActiveSlot(1);
        }

        if (Input.GetKey(KeyCode.G) ||
            Input.GetKey(KeyCode.T))
        {
            forceMultiplier +=
                Time.deltaTime;

            if (forceMultiplier >
                forceMultiplierLimit)
            {
                forceMultiplier =
                    forceMultiplierLimit;
            }
        }

        if (Input.GetKeyUp(KeyCode.G))
        {
            if (lethalsCount > 0)
            {
                ThrowLethal();
            }

            forceMultiplier = 0;
        }

        if (Input.GetKeyUp(KeyCode.T))
        {
            if (tacticalCount > 0)
            {
                ThrowTactical();
            }

            forceMultiplier = 0;
        }
    }
    */

    private void Update()
    {
        PlayerMovement playerMovement =
            GetComponentInParent<PlayerMovement>();

        if (playerMovement != null &&
            !playerMovement.IsOwner)
        {
            return;
        }

        // Block gun switch + grenades while chat is open
        if (ChatUI.IsChatOpen)
        {
            return;
        }

        if (weaponSlots == null ||
            weaponSlots.Count == 0)
        {
            return;
        }

        foreach (GameObject weaponSlot in weaponSlots)
        {
            if (weaponSlot == null)
            {
                continue;
            }

            weaponSlot.SetActive(
                weaponSlot == activeWeaponSlot
            );
        }

        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            SwitchActiveSlot(0);
        }

        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            SwitchActiveSlot(1);
        }

        if (Input.GetKey(KeyCode.G) ||
            Input.GetKey(KeyCode.T))
        {
            forceMultiplier +=
                Time.deltaTime;

            if (forceMultiplier >
                forceMultiplierLimit)
            {
                forceMultiplier =
                    forceMultiplierLimit;
            }
        }

        if (Input.GetKeyUp(KeyCode.G))
        {
            if (lethalsCount > 0)
            {
                ThrowLethal();
            }

            forceMultiplier = 0;
        }

        if (Input.GetKeyUp(KeyCode.T))
        {
            if (tacticalCount > 0)
            {
                ThrowTactical();
            }

            forceMultiplier = 0;
        }
    }
    // =========================================================
    // GUN PICKUP
    // =========================================================

    private string GetWeaponPickupId(
        GameObject pickedupWeapon)
    {
        if (pickedupWeapon == null)
        {
            return string.Empty;
        }

        Weapon weapon =
            pickedupWeapon.GetComponent<Weapon>();

        if (weapon == null)
        {
            return string.Empty;
        }

        return weapon.NetworkPickupId;
    }

    public void PickupWeapon(
        GameObject pickedupWeapon)
    {
        if (!IsOwner)
        {
            return;
        }

        if (activeWeaponSlot == null ||
            pickedupWeapon == null)
        {
            return;
        }

        Weapon weapon =
            pickedupWeapon.GetComponent<Weapon>();

        if (weapon == null)
        {
            return;
        }

        string pickupId =
            GetWeaponPickupId(
                pickedupWeapon
            );

        if (string.IsNullOrEmpty(pickupId))
        {
            Debug.LogWarning(
                "WeaponManager: Weapon has no Network Pickup Id."
            );

            return;
        }

        string droppedWeaponId =
            string.Empty;

        Vector3 droppedPosition =
            Vector3.zero;

        Quaternion droppedRotation =
            Quaternion.identity;

        if (activeWeaponSlot.transform.childCount > 0)
        {
            Weapon currentWeapon =
                activeWeaponSlot.transform
                .GetChild(0)
                .GetComponent<Weapon>();

            if (currentWeapon != null &&
                currentWeapon.gameObject !=
                pickedupWeapon)
            {
                droppedWeaponId =
                    currentWeapon.NetworkPickupId;

                droppedPosition =
                    pickedupWeapon.transform.position;

                droppedRotation =
                    pickedupWeapon.transform.rotation;
            }
        }

        AddWeaponIntoActiveSlot(
            pickedupWeapon
        );

        // Synchronize BOTH slots.
        SyncNetworkWeaponSlots();

        networkActiveWeaponSlot.Value =
            weaponSlots.IndexOf(
                activeWeaponSlot
            );

        NotifyWeaponPickupServerRpc(
            pickupId,
            droppedWeaponId,
            droppedPosition,
            droppedRotation
        );
    }

    [ServerRpc]
    private void NotifyWeaponPickupServerRpc(
        string pickupId,
        string droppedWeaponId,
        Vector3 droppedPosition,
        Quaternion droppedRotation,
        ServerRpcParams rpcParams = default)
    {
        if (string.IsNullOrEmpty(pickupId))
        {
            return;
        }

        pickedUpWeaponIds.Add(
            pickupId
        );

        if (!string.IsNullOrEmpty(
                droppedWeaponId))
        {
            pickedUpWeaponIds.Remove(
                droppedWeaponId
            );
        }

        UpdateWeaponVisualsClientRpc(
            pickupId,
            droppedWeaponId,
            droppedPosition,
            droppedRotation,
            rpcParams.Receive.SenderClientId
        );
    }

    [ClientRpc]
    private void UpdateWeaponVisualsClientRpc(
        string pickedWeaponId,
        string droppedWeaponId,
        Vector3 droppedPosition,
        Quaternion droppedRotation,
        ulong pickerClientId)
    {
        if (NetworkManager.Singleton.LocalClientId ==
            pickerClientId)
        {
            return;
        }

        if (!string.IsNullOrEmpty(
                droppedWeaponId))
        {
            RestoreDroppedWeapon(
                droppedWeaponId,
                droppedPosition,
                droppedRotation
            );
        }

        HideWorldWeaponById(
            pickedWeaponId
        );
    }

    private void RestoreDroppedWeapon(
        string weaponId,
        Vector3 position,
        Quaternion rotation)
    {
        Weapon[] allWeapons =
            FindObjectsByType<Weapon>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None
            );

        foreach (Weapon weapon in allWeapons)
        {
            if (weapon == null ||
                weapon.NetworkPickupId != weaponId)
            {
                continue;
            }

            PlayerMovement weaponPlayer =
                weapon.GetComponentInParent<PlayerMovement>();

            if (weaponPlayer != null)
            {
                continue;
            }

            weapon.transform.SetParent(
                null,
                true
            );

            weapon.transform.position =
                position;

            weapon.transform.rotation =
                rotation;

            weapon.isActiveWeapon =
                false;

            if (weapon.animator != null)
            {
                weapon.animator.enabled =
                    false;
            }

            Outline outline =
                weapon.GetComponent<Outline>();

            if (outline != null)
            {
                outline.enabled =
                    true;
            }

            weapon.gameObject.SetActive(
                true
            );

            break;
        }
    }

    private void HideWorldWeaponById(
        string pickupId)
    {
        Weapon[] allWeapons =
            FindObjectsByType<Weapon>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None
            );

        foreach (Weapon weapon in allWeapons)
        {
            if (weapon == null)
            {
                continue;
            }

            if (weapon.NetworkPickupId !=
                pickupId)
            {
                continue;
            }

            // Never hide a weapon already being held
            // by a player.
            PlayerMovement weaponPlayer =
                weapon.GetComponentInParent<PlayerMovement>();

            if (weaponPlayer != null)
            {
                continue;
            }

            weapon.gameObject.SetActive(
                false
            );

            break;
        }
    }

    // =========================================================
    // GUN LATE JOIN
    // =========================================================

    [ServerRpc]
    private void RequestPickedUpWeaponsServerRpc(
        ServerRpcParams rpcParams = default)
    {
        foreach (string pickupId in
                 pickedUpWeaponIds)
        {
            SendPickedUpWeaponToClientRpc(
                pickupId,
                rpcParams.Receive.SenderClientId
            );
        }
    }

    [ClientRpc]
    private void SendPickedUpWeaponToClientRpc(
        string pickupId,
        ulong targetClientId)
    {
        if (NetworkManager.Singleton.LocalClientId !=
            targetClientId)
        {
            return;
        }

        HideWorldWeaponById(
            pickupId
        );

        StartCoroutine(
            ApplyNetworkWeaponStateAfterSpawn()
        );
    }

    // =========================================================
    // WEAPON EQUIPPING / DROPPING
    // =========================================================

    private void AddWeaponIntoActiveSlot(
        GameObject pickedupWeapon)
    {
        DropCurrentWeapon(
            pickedupWeapon
        );

        pickedupWeapon.transform.SetParent(
            activeWeaponSlot.transform,
            false
        );

        Weapon weapon =
            pickedupWeapon.GetComponent<Weapon>();

        if (weapon == null)
        {
            return;
        }

        weapon.SetOwner(
            this,
            playerCamera
        );

        pickedupWeapon.transform.localPosition =
            weapon.spawnPosition;

        pickedupWeapon.transform.localRotation =
            Quaternion.Euler(
                weapon.spawnRotation
            );

        weapon.isActiveWeapon =
            true;

        Outline outline =
            pickedupWeapon.GetComponent<Outline>();

        if (outline != null)
        {
            outline.enabled = false;
        }

        if (weapon.animator != null)
        {
            weapon.animator.enabled = true;
        }
    }

    private void DropCurrentWeapon(
        GameObject pickedupWeapon)
    {
        if (activeWeaponSlot == null ||
            activeWeaponSlot.transform.childCount == 0)
        {
            return;
        }

        GameObject weaponToDrop =
            activeWeaponSlot.transform
            .GetChild(0)
            .gameObject;

        if (weaponToDrop ==
            pickedupWeapon)
        {
            return;
        }

        Weapon weapon =
            weaponToDrop.GetComponent<Weapon>();

        if (weapon != null)
        {
            weapon.isActiveWeapon =
                false;

            if (weapon.animator != null)
            {
                weapon.animator.enabled =
                    false;
            }
        }

        Transform worldParent =
            pickedupWeapon.transform.parent;

        weaponToDrop.transform.SetParent(
            worldParent,
            true
        );

        weaponToDrop.transform.position =
            pickedupWeapon.transform.position;

        weaponToDrop.transform.rotation =
            pickedupWeapon.transform.rotation;

        Outline outline =
            weaponToDrop.GetComponent<Outline>();

        if (outline != null)
        {
            outline.enabled = true;
        }
    }

    // =========================================================
    // SWITCH ACTIVE SLOT
    // =========================================================

    public void SwitchActiveSlot(
        int slotNumber)
    {
        if (!IsOwner)
        {
            return;
        }

        if (weaponSlots == null ||
            slotNumber < 0 ||
            slotNumber >= weaponSlots.Count)
        {
            return;
        }

        // Turn off current weapon.
        if (activeWeaponSlot != null &&
            activeWeaponSlot.transform.childCount > 0)
        {
            Weapon currentWeapon =
                activeWeaponSlot.transform
                .GetChild(0)
                .GetComponent<Weapon>();

            if (currentWeapon != null)
            {
                currentWeapon.isActiveWeapon =
                    false;
            }
        }

        // Change slot.
        activeWeaponSlot =
            weaponSlots[slotNumber];

        // Turn on the weapon that is ACTUALLY
        // inside this slot.
        if (activeWeaponSlot != null &&
            activeWeaponSlot.transform.childCount > 0)
        {
            Weapon newWeapon =
                activeWeaponSlot.transform
                .GetChild(0)
                .GetComponent<Weapon>();

            if (newWeapon != null)
            {
                newWeapon.isActiveWeapon =
                    true;
            }
        }

        // Synchronize both slots.
        SyncNetworkWeaponSlots();

        // Synchronize active slot.
        networkActiveWeaponSlot.Value =
            slotNumber;
    }

    // =========================================================
    // AMMO
    // =========================================================

    public bool CanPickupAmmo(
        AmmoBox ammo)
    {
        if (ammo == null)
        {
            return false;
        }

        bool hasMatchingWeapon =
            false;

        foreach (GameObject slot in weaponSlots)
        {
            if (slot != null &&
                slot.transform.childCount > 0)
            {
                Weapon weapon =
                    slot.transform
                    .GetChild(0)
                    .GetComponent<Weapon>();

                if (weapon != null &&
                    WeaponModelMatchesAmmoType(
                        weapon.thisWeaponModel,
                        ammo.ammoType))
                {
                    hasMatchingWeapon =
                        true;

                    break;
                }
            }
        }

        if (!hasMatchingWeapon)
        {
            return false;
        }

        if (heldAmmoBoxes.ContainsKey(
                ammo.ammoType) &&
            heldAmmoBoxes[ammo.ammoType] != null)
        {
            return false;
        }

        return true;
    }

    private bool WeaponModelMatchesAmmoType(
        Weapon.WeaponModel weaponModel,
        AmmoBox.AmmoType ammoType)
    {
        switch (weaponModel)
        {
            case Weapon.WeaponModel.AK74:
                return ammoType ==
                    AmmoBox.AmmoType.AK74Ammo;

            case Weapon.WeaponModel.M249:
                return ammoType ==
                    AmmoBox.AmmoType.M249Ammo;

            case Weapon.WeaponModel.M107:
                return ammoType ==
                    AmmoBox.AmmoType.M107Ammo;

            default:
                return false;
        }
    }

    internal void PickupAmmo(
        AmmoBox ammo)
    {
        if (!IsOwner ||
            ammo == null ||
            !CanPickupAmmo(ammo))
        {
            return;
        }

        string pickupId =
            ammo.NetworkPickupId;

        if (string.IsNullOrEmpty(
                pickupId))
        {
            return;
        }

        heldAmmoBoxes[ammo.ammoType] =
            ammo;

        ammo.gameObject.SetActive(
            false
        );

        switch (ammo.ammoType)
        {
            case AmmoBox.AmmoType.AK74Ammo:
                totalAK74Ammo +=
                    ammo.ammoAmount;
                break;

            case AmmoBox.AmmoType.M249Ammo:
                totalM249Ammo +=
                    ammo.ammoAmount;
                break;

            case AmmoBox.AmmoType.M107Ammo:
                totalM107Ammo +=
                    ammo.ammoAmount;
                break;
        }

        NotifyAmmoPickupServerRpc(
            pickupId
        );
    }

    [ServerRpc]
    private void NotifyAmmoPickupServerRpc(
        string pickupId,
        ServerRpcParams rpcParams = default)
    {
        if (string.IsNullOrEmpty(
                pickupId))
        {
            return;
        }

        pickedUpAmmoIds.Add(
            pickupId
        );

        HideAmmoPickupClientRpc(
            pickupId,
            rpcParams.Receive.SenderClientId
        );
    }

    [ClientRpc]
    private void HideAmmoPickupClientRpc(
        string pickupId,
        ulong pickerClientId)
    {
        if (NetworkManager.Singleton.LocalClientId ==
            pickerClientId)
        {
            return;
        }

        HideWorldAmmoById(
            pickupId
        );
    }

    private void HideWorldAmmoById(
        string pickupId)
    {
        AmmoBox[] allAmmoBoxes =
            FindObjectsByType<AmmoBox>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None
            );

        foreach (AmmoBox ammo in allAmmoBoxes)
        {
            if (ammo != null &&
                ammo.NetworkPickupId ==
                pickupId)
            {
                ammo.gameObject.SetActive(
                    false
                );

                break;
            }
        }
    }

    [ServerRpc]
    private void RequestPickedUpAmmoServerRpc(
        ServerRpcParams rpcParams = default)
    {
        foreach (string pickupId in
                 pickedUpAmmoIds)
        {
            SendPickedUpAmmoToClientRpc(
                pickupId,
                rpcParams.Receive.SenderClientId
            );
        }
    }

    [ClientRpc]
    private void SendPickedUpAmmoToClientRpc(
        string pickupId,
        ulong targetClientId)
    {
        if (NetworkManager.Singleton.LocalClientId !=
            targetClientId)
        {
            return;
        }

        HideWorldAmmoById(
            pickupId
        );
    }



    //wednesday, 19 june 2024

    // =========================================================
    // HEALTH BOX
    // =========================================================

    public bool CanPickupHealthBox(HealthBox healthBox)
    {
        if (healthBox == null) return false;

        Player player = GetComponentInParent<Player>();
        if (player == null) return false;

        // Only allow pickup if health is NOT full
        return !player.IsHealthFull();
    }

    public void PickupHealthBox(HealthBox healthBox)
    {
        if (!IsOwner || healthBox == null || !CanPickupHealthBox(healthBox))
            return;

        string pickupId = healthBox.NetworkPickupId;
        if (string.IsNullOrEmpty(pickupId))
            return;

        // Heal the player
        Player player = GetComponentInParent<Player>();
        if (player != null)
        {
            player.HealToFull();
        }

        // Hide locally
        healthBox.gameObject.SetActive(false);

        // Tell server so everyone else also hides it
        NotifyHealthBoxPickupServerRpc(pickupId);
    }

    [ServerRpc]
    private void NotifyHealthBoxPickupServerRpc(
        string pickupId,
        ServerRpcParams rpcParams = default)
    {
        if (string.IsNullOrEmpty(pickupId)) return;

        pickedUpHealthBoxIds.Add(pickupId);

        HideHealthBoxPickupClientRpc(
            pickupId,
            rpcParams.Receive.SenderClientId
        );
    }

    [ClientRpc]
    private void HideHealthBoxPickupClientRpc(
        string pickupId,
        ulong pickerClientId)
    {
        // Picker already hid it locally
        if (NetworkManager.Singleton.LocalClientId == pickerClientId)
            return;

        HideWorldHealthBoxById(pickupId);
    }

    private void HideWorldHealthBoxById(string pickupId)
    {
        HealthBox[] allBoxes =
            FindObjectsByType<HealthBox>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None
            );

        foreach (HealthBox box in allBoxes)
        {
            if (box != null && box.NetworkPickupId == pickupId)
            {
                box.gameObject.SetActive(false);
                break;
            }
        }
    }

    [ServerRpc]
    private void RequestPickedUpHealthBoxesServerRpc(
        ServerRpcParams rpcParams = default)
    {
        foreach (string pickupId in pickedUpHealthBoxIds)
        {
            SendPickedUpHealthBoxToClientRpc(
                pickupId,
                rpcParams.Receive.SenderClientId
            );
        }
    }

    [ClientRpc]
    private void SendPickedUpHealthBoxToClientRpc(
        string pickupId,
        ulong targetClientId)
    {
        if (NetworkManager.Singleton.LocalClientId != targetClientId)
            return;

        HideWorldHealthBoxById(pickupId);
    }


    // =========================================================
    // THROWABLE PICKUP
    // =========================================================

    public void PickupThrowable(
        Throwable throwable)
    {
        if (!IsOwner ||
            throwable == null)
        {
            return;
        }

        string pickupId =
            throwable.NetworkPickupId;

        if (string.IsNullOrEmpty(
                pickupId))
        {
            return;
        }

        switch (throwable.throwableType)
        {
            case Throwable.ThrowableType.Grenade:

                PickupThrowableAsLethal(
                    Throwable.ThrowableType.Grenade,
                    pickupId
                );

                break;

            case Throwable.ThrowableType.Smoke_Grenade:

                PickupThrowableAsTacticles(
                    Throwable.ThrowableType.Smoke_Grenade,
                    pickupId
                );

                break;
        }
    }

    private void PickupThrowableAsLethal(
        Throwable.ThrowableType lethal,
        string pickupId)
    {
        if (equippedLethalType != lethal &&
            equippedLethalType !=
            Throwable.ThrowableType.None)
        {
            return;
        }

        if (lethalsCount >= maxLethals)
        {
            return;
        }

        equippedLethalType =
            lethal;

        lethalsCount++;

        if (interactionManager != null &&
            interactionManager.hoveredThrowable != null)
        {
            Destroy(
                interactionManager
                .hoveredThrowable.gameObject
            );

            interactionManager.hoveredThrowable =
                null;
        }

        NotifyThrowablePickupServerRpc(
            pickupId
        );

        if (HUDManager.Instance != null)
        {
            HUDManager.Instance
                .UpdateThrowablesUI();
        }
    }

    private void PickupThrowableAsTacticles(
        Throwable.ThrowableType smoke_Grenade,
        string pickupId)
    {
        if (equippedTacticalType !=
                smoke_Grenade &&
            equippedTacticalType !=
                Throwable.ThrowableType.None)
        {
            return;
        }

        if (tacticalCount >= maxTacticals)
        {
            return;
        }

        equippedTacticalType =
            smoke_Grenade;

        tacticalCount++;

        if (interactionManager != null &&
            interactionManager.hoveredThrowable != null)
        {
            Destroy(
                interactionManager
                .hoveredThrowable.gameObject
            );

            interactionManager.hoveredThrowable =
                null;
        }

        NotifyThrowablePickupServerRpc(
            pickupId
        );

        if (HUDManager.Instance != null)
        {
            HUDManager.Instance
                .UpdateThrowablesUI();
        }
    }

    [ServerRpc]
    private void NotifyThrowablePickupServerRpc(
        string pickupId,
        ServerRpcParams rpcParams = default)
    {
        if (string.IsNullOrEmpty(
                pickupId))
        {
            return;
        }

        pickedUpThrowableIds.Add(
            pickupId
        );

        HideThrowablePickupClientRpc(
            pickupId,
            rpcParams.Receive.SenderClientId
        );
    }

    [ClientRpc]
    private void HideThrowablePickupClientRpc(
        string pickupId,
        ulong pickerClientId)
    {
        if (NetworkManager.Singleton.LocalClientId ==
            pickerClientId)
        {
            return;
        }

        HideWorldThrowableById(
            pickupId
        );
    }

    private void HideWorldThrowableById(
        string pickupId)
    {
        Throwable[] allThrowables =
            FindObjectsByType<Throwable>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None
            );

        foreach (Throwable throwable in
                 allThrowables)
        {
            if (throwable == null)
            {
                continue;
            }

            if (throwable.NetworkPickupId !=
                pickupId)
            {
                continue;
            }

            if (throwable.hasBeenThrown)
            {
                continue;
            }

            throwable.gameObject.SetActive(
                false
            );

            break;
        }
    }

    [ServerRpc]
    private void RequestPickedUpThrowablesServerRpc(
        ServerRpcParams rpcParams = default)
    {
        foreach (string pickupId in
                 pickedUpThrowableIds)
        {
            SendPickedUpThrowableToClientRpc(
                pickupId,
                rpcParams.Receive.SenderClientId
            );
        }
    }

    [ClientRpc]
    private void SendPickedUpThrowableToClientRpc(
        string pickupId,
        ulong targetClientId)
    {
        if (NetworkManager.Singleton.LocalClientId !=
            targetClientId)
        {
            return;
        }

        HideWorldThrowableById(
            pickupId
        );
    }

    // =========================================================
    // AMMO TOTALS
    // =========================================================

    internal void DecreaseTotalAmmo(
        int bulletsToDecrease,
        Weapon.WeaponModel thisWeaponModel)
    {
        switch (thisWeaponModel)
        {
            case Weapon.WeaponModel.AK74:
                totalAK74Ammo -=
                    bulletsToDecrease;
                break;

            case Weapon.WeaponModel.M249:
                totalM249Ammo -=
                    bulletsToDecrease;
                break;

            case Weapon.WeaponModel.M107:
                totalM107Ammo -=
                    bulletsToDecrease;
                break;
        }

        AmmoBox.AmmoType ammoType =
            GetAmmoTypeForWeapon(
                thisWeaponModel
            );

        if (CheckAmmoLeftFor(
                thisWeaponModel) <= 0 &&
            heldAmmoBoxes.ContainsKey(
                ammoType))
        {
            if (heldAmmoBoxes[ammoType] != null)
            {
                Destroy(
                    heldAmmoBoxes[ammoType].gameObject
                );
            }

            heldAmmoBoxes.Remove(
                ammoType
            );
        }
    }

    private AmmoBox.AmmoType GetAmmoTypeForWeapon(
        Weapon.WeaponModel weaponModel)
    {
        switch (weaponModel)
        {
            case Weapon.WeaponModel.AK74:
                return AmmoBox.AmmoType.AK74Ammo;

            case Weapon.WeaponModel.M249:
                return AmmoBox.AmmoType.M249Ammo;

            default:
                return AmmoBox.AmmoType.M107Ammo;
        }
    }

    public int CheckAmmoLeftFor(
        Weapon.WeaponModel thisWeaponModel)
    {
        switch (thisWeaponModel)
        {
            case Weapon.WeaponModel.AK74:
                return totalAK74Ammo;

            case Weapon.WeaponModel.M249:
                return totalM249Ammo;

            case Weapon.WeaponModel.M107:
                return totalM107Ammo;

            default:
                return 0;
        }
    }

    // =========================================================
    // THROWING
    // =========================================================

    private void ThrowLethal()
    {
        GameObject lethalPrefab =
            GetThrowablePrefab(
                equippedLethalType
            );

        if (lethalPrefab == null ||
            playerCamera == null ||
            throwableSpawn == null)
        {
            return;
        }

        GameObject throwable =
            Instantiate(
                lethalPrefab,
                throwableSpawn.transform.position,
                playerCamera.transform.rotation
            );

        Rigidbody rb =
            throwable.GetComponent<Rigidbody>();

        if (rb != null)
        {
            rb.AddForce(
                playerCamera.transform.forward *
                (throwForce * forceMultiplier),
                ForceMode.Impulse
            );
        }

        Throwable throwableComponent =
            throwable.GetComponent<Throwable>();

        if (throwableComponent != null)
        {
            throwableComponent.hasBeenThrown =
                true;
        }

        lethalsCount--;

        if (lethalsCount <= 0)
        {
            equippedLethalType =
                Throwable.ThrowableType.None;
        }

        if (HUDManager.Instance != null)
        {
            HUDManager.Instance
                .UpdateThrowablesUI();
        }
    }

    private void ThrowTactical()
    {
        GameObject tacticalPrefab =
            GetThrowablePrefab(
                equippedTacticalType
            );

        if (tacticalPrefab == null ||
            playerCamera == null ||
            throwableSpawn == null)
        {
            return;
        }

        GameObject throwable =
            Instantiate(
                tacticalPrefab,
                throwableSpawn.transform.position,
                playerCamera.transform.rotation
            );

        Rigidbody rb =
            throwable.GetComponent<Rigidbody>();

        if (rb != null)
        {
            rb.AddForce(
                playerCamera.transform.forward *
                (throwForce * forceMultiplier),
                ForceMode.Impulse
            );
        }

        Throwable throwableComponent =
            throwable.GetComponent<Throwable>();

        if (throwableComponent != null)
        {
            throwableComponent.hasBeenThrown =
                true;
        }

        tacticalCount--;

        if (tacticalCount <= 0)
        {
            equippedTacticalType =
                Throwable.ThrowableType.None;
        }

        if (HUDManager.Instance != null)
        {
            HUDManager.Instance
                .UpdateThrowablesUI();
        }
    }

    private GameObject GetThrowablePrefab(
        Throwable.ThrowableType throwableType)
    {
        switch (throwableType)
        {
            case Throwable.ThrowableType.Grenade:
                return grenadePrefab;

            case Throwable.ThrowableType.Smoke_Grenade:
                return smokeGrenadePrefab;
        }

        return null;
    }


    //tuesday
    public IEnumerator RestoreSavedLoadout(SaveData data)
    {
        if (data == null)
        {
            Debug.LogWarning("WeaponManager: No save data to restore.");
            yield break;
        }


        Debug.Log(
         $"LOAD DATA CHECK | slot1 mag={data.slot1?.magazineAmmo} | slot2 mag={data.slot2?.magazineAmmo}"
       );

        yield return null;
        yield return null;

        // =========================================================
        // RESERVE AMMO
        // =========================================================

        totalAK74Ammo = data.totalAK74Ammo;
        totalM249Ammo = data.totalM249Ammo;
        totalM107Ammo = data.totalM107Ammo;

        Debug.Log(
    $"RESTORED RESERVE AMMO | " +
    $"AK74={totalAK74Ammo} | " +
    $"M249={totalM249Ammo} | " +
    $"M107={totalM107Ammo} | " +
    $"Grenades={lethalsCount} | " +
    $"Smoke={tacticalCount}"
);

        // =========================================================
        // GRENADE
        // =========================================================

        lethalsCount = Mathf.Clamp(
            data.lethalCount,
            0,
            maxLethals
        );

        if (System.Enum.TryParse(
                data.lethalType,
                out Throwable.ThrowableType loadedLethalType))
        {
            equippedLethalType = loadedLethalType;
        }
        else
        {
            equippedLethalType =
                Throwable.ThrowableType.None;
        }


        // =========================================================
        // SMOKE GRENADE
        // =========================================================

        tacticalCount = Mathf.Clamp(
            data.tacticalCount,
            0,
            maxTacticals
        );

        if (System.Enum.TryParse(
                data.tacticalType,
                out Throwable.ThrowableType loadedTacticalType))
        {
            equippedTacticalType = loadedTacticalType;
        }
        else
        {
            equippedTacticalType =
                Throwable.ThrowableType.None;
        }


        // =========================================================
        // RESTORE SLOT 1
        // =========================================================

        yield return StartCoroutine(
            RestoreSavedWeaponToSlot(
                data.slot1,
                0
            )
        );


        // =========================================================
        // RESTORE SLOT 2
        // =========================================================

        yield return StartCoroutine(
            RestoreSavedWeaponToSlot(
                data.slot2,
                1
            )
        );


        // =========================================================
        // ACTIVE SLOT
        // =========================================================

        int savedActiveSlot =
            Mathf.Clamp(
                data.activeWeaponSlot,
                0,
                weaponSlots.Count - 1
            );

        if (weaponSlots.Count > 0)
        {
            activeWeaponSlot = weaponSlots[savedActiveSlot];

            for (int i = 0; i < weaponSlots.Count; i++)
            {
                if (weaponSlots[i] == null)
                    continue;

                bool active = (i == savedActiveSlot);
                weaponSlots[i].SetActive(active);

                Weapon weapon =
                    weaponSlots[i].GetComponentInChildren<Weapon>(true);

                if (weapon != null)
                {
                    weapon.isActiveWeapon = active;
                    weapon.SetOwner(this, playerCamera);

                    if (weapon.animator != null)
                    {
                        weapon.animator.enabled = true;
                    }

                    // Re-apply magazine again on the active gun (safety)
                    if (active)
                    {
                        WeaponSaveData src =
                            (i == 0) ? data.slot1 : data.slot2;

                        if (src != null && src.hasWeapon)
                        {
                            weapon.bulletsLeft =
                                Mathf.Clamp(
                                    src.magazineAmmo,
                                    0,
                                    weapon.magazineSize
                                );
                        }

                        weapon.readyToShoot = true;
                        weapon.isReloading = false;
                    }
                }
            }
        }


        // =========================================================
        // NETWORK STATE
        // =========================================================

        SyncNetworkWeaponSlots();

        networkActiveWeaponSlot.Value =
            savedActiveSlot;


        // =========================================================
        // REFRESH HUD
        // =========================================================

        if (HUDManager.Instance != null)
        {
            HUDManager.Instance.UpdateThrowablesUI();
        }

        yield return null;
        yield return null;

        ApplySavedMagazine(data.slot1, 0);
        ApplySavedMagazine(data.slot2, 1);

        Debug.Log("RestoreSavedLoadout: magazine re-applied.");

        // ===== DELAYED RE-APPLY (after City/UI settles) =====
        yield return new WaitForSeconds(2.5f);

        ApplySavedMagazine(data.slot1, 0);
        ApplySavedMagazine(data.slot2, 1);

        Debug.Log("Delayed magazine re-apply after scene settle.");

        Debug.Log(
            "Saved loadout restored successfully."
        );
    }



    private IEnumerator RestoreSavedWeaponToSlot(
    WeaponSaveData savedWeapon,
    int slotIndex)
    {
        if (savedWeapon == null || !savedWeapon.hasWeapon)
            yield break;

        if (weaponSlots == null ||
            slotIndex < 0 ||
            slotIndex >= weaponSlots.Count)
            yield break;

        GameObject slot = weaponSlots[slotIndex];
        if (slot == null)
            yield break;

        GameObject weaponPrefab = null;

        if (System.Enum.TryParse(savedWeapon.weaponModel, out Weapon.WeaponModel savedModel))
        {
            switch (savedModel)
            {
                case Weapon.WeaponModel.AK74:
                    weaponPrefab = AK74Prefab;
                    break;
                case Weapon.WeaponModel.M249:
                    weaponPrefab = M249Prefab;
                    break;
                case Weapon.WeaponModel.M107:
                    weaponPrefab = M107Prefab;
                    break;
            }
        }

        if (weaponPrefab == null)
        {
            Debug.LogError(
                "Cannot restore saved weapon. Prefab not assigned for: " +
                savedWeapon.weaponModel
            );
            yield break;
        }

        // Clear slot
        if (slot.transform.childCount > 0)
        {
            Destroy(slot.transform.GetChild(0).gameObject);
        }

        yield return null; // wait destroy

        GameObject newWeaponObject =
            Instantiate(weaponPrefab, slot.transform);

        Weapon restoredWeapon =
            newWeaponObject.GetComponent<Weapon>();

        if (restoredWeapon == null)
        {
            Destroy(newWeaponObject);
            yield break;
        }

        // ID (normal gun for save/pickup)
        if (!string.IsNullOrEmpty(savedWeapon.weaponId))
        {
            restoredWeapon.SetNetworkPickupId(savedWeapon.weaponId);
        }

        newWeaponObject.transform.localPosition =
            restoredWeapon.spawnPosition;

        newWeaponObject.transform.localRotation =
            Quaternion.Euler(restoredWeapon.spawnRotation);

        restoredWeapon.SetOwner(this, playerCamera);

        // Magazine from save (AFTER Awake, so it is not overwritten wrongly)
        restoredWeapon.bulletsLeft =
            Mathf.Clamp(
                savedWeapon.magazineAmmo,
                0,
                restoredWeapon.magazineSize
            );

        restoredWeapon.keepSavedMagazine = true;

        restoredWeapon.isReloading = false;
        restoredWeapon.readyToShoot = true;
        restoredWeapon.isActiveWeapon = false;

        if (restoredWeapon.animator != null)
        {
            restoredWeapon.animator.enabled = true;
        }

        Outline outline =
            restoredWeapon.GetComponent<Outline>();

        if (outline != null)
            outline.enabled = false;

        newWeaponObject.SetActive(true);

        Debug.Log(
            $"Restored weapon | Slot {slotIndex + 1} | " +
            $"{savedWeapon.weaponModel} | Mag {restoredWeapon.bulletsLeft}"
        );

        yield return null;
    }

    [Header("Weapon Prefabs")]
    public GameObject AK74Prefab;
    public GameObject M249Prefab;
    public GameObject M107Prefab;

    public List<string> GetPickedUpThrowableIds()
    {
        return new List<string>(
            pickedUpThrowableIds
        );
    }

    public static void RestorePickedUpThrowableIds(
    List<string> savedIds)
    {
        pickedUpThrowableIds.Clear();

        if (savedIds == null)
        {
            return;
        }

        foreach (string id in savedIds)
        {
            if (!string.IsNullOrEmpty(id))
            {
                pickedUpThrowableIds.Add(id);
            }
        }

        Debug.Log(
            $"Restored picked-up throwable IDs: " +
            $"{pickedUpThrowableIds.Count}"
        );
    }

    public List<string> GetPickedUpWeaponIdsForSave()
    {
        return new List<string>(
            pickedUpWeaponIds
        );
    }

    public List<string> GetPickedUpThrowableIdsForSave()
    {
        return new List<string>(
            pickedUpThrowableIds
        );
    }

    public static void RestorePickedUpIdsForSavedGame(
        List<string> savedWeaponIds,
        List<string> savedThrowableIds)
    {
        pickedUpWeaponIds.Clear();
        pickedUpThrowableIds.Clear();

        if (savedWeaponIds != null)
        {
            foreach (string id in savedWeaponIds)
            {
                if (!string.IsNullOrEmpty(id))
                {
                    pickedUpWeaponIds.Add(id);
                }
            }
        }

        if (savedThrowableIds != null)
        {
            foreach (string id in savedThrowableIds)
            {
                if (!string.IsNullOrEmpty(id))
                {
                    pickedUpThrowableIds.Add(id);
                }
            }
        }

        Debug.Log(
            $"SAVED GAME PICKUP IDS RESTORED | " +
            $"Weapons={pickedUpWeaponIds.Count} | " +
            $"Throwables={pickedUpThrowableIds.Count}"
        );
    }

    public void HideSavedGamePickedItems()
    {
        // =========================================================
        // HIDE SAVED PICKED WEAPONS
        // =========================================================

        foreach (string pickupId in pickedUpWeaponIds)
        {
            if (!string.IsNullOrEmpty(pickupId))
            {
                HideWorldWeaponById(
                    pickupId
                );
            }
        }

        // =========================================================
        // HIDE SAVED PICKED THROWABLES
        // =========================================================

        foreach (string pickupId in pickedUpThrowableIds)
        {
            if (!string.IsNullOrEmpty(pickupId))
            {
                HideWorldThrowableById(
                    pickupId
                );
            }
        }

        // =========================================================
        // HIDE SAVED PICKED AMMO BOXES
        // =========================================================

        foreach (string pickupId in pickedUpAmmoIds)
        {
            if (!string.IsNullOrEmpty(pickupId))
            {
                HideWorldAmmoById(
                    pickupId
                );
            }
        }

        // =========================================================
        // HIDE SAVED PICKED HEALTH BOXES
        // =========================================================

        foreach (string pickupId in pickedUpHealthBoxIds)
        {
            if (!string.IsNullOrEmpty(pickupId))
            {
                HideWorldHealthBoxById(
                    pickupId
                );
            }
        }

        Debug.Log(
            "SAVED GAME PICKED ITEMS HIDDEN | " +
            $"Weapons={pickedUpWeaponIds.Count} | " +
            $"Throwables={pickedUpThrowableIds.Count} | " +
            $"Ammo={pickedUpAmmoIds.Count} | " +
            $"Health={pickedUpHealthBoxIds.Count}"
        );
    }


    public List<string> GetPickedUpAmmoIdsForSave()
    {
        return new List<string>(
            pickedUpAmmoIds
        );
    }

    public List<string> GetPickedUpHealthBoxIdsForSave()
    {
        return new List<string>(
            pickedUpHealthBoxIds
        );
    }
    public static void RestorePickedUpAmmoAndHealthIdsForSavedGame(
    List<string> savedAmmoIds,
    List<string> savedHealthBoxIds)
    {
        pickedUpAmmoIds.Clear();
        pickedUpHealthBoxIds.Clear();

        if (savedAmmoIds != null)
        {
            foreach (string id in savedAmmoIds)
            {
                if (!string.IsNullOrEmpty(id))
                {
                    pickedUpAmmoIds.Add(id);
                }
            }
        }

        if (savedHealthBoxIds != null)
        {
            foreach (string id in savedHealthBoxIds)
            {
                if (!string.IsNullOrEmpty(id))
                {
                    pickedUpHealthBoxIds.Add(id);
                }
            }
        }

        Debug.Log(
            $"SAVED GAME AMMO/HEALTH IDS RESTORED | " +
            $"AmmoBoxes={pickedUpAmmoIds.Count} | " +
            $"HealthBoxes={pickedUpHealthBoxIds.Count}"
        );
    }

//wednesday 9-23-2026
    // =========================================================
    // STARTER WEAPON (NEW GAME ONLY)
    // =========================================================

    public IEnumerator GiveStarterWeapon()
    {
        if (SaveManager.Instance != null &&
            SaveManager.Instance.IsLoadingSavedGame)
        {
            yield break;
        }

        if (!IsOwner)
            yield break;

        if (weaponSlots == null || weaponSlots.Count == 0)
            yield break;

        if (weaponSlots[0] != null &&
            weaponSlots[0].transform.childCount > 0)
        {
            yield break;
        }

        if (AK74Prefab == null)
        {
            Debug.LogError("WeaponManager: AK74Prefab not assigned.");
            yield break;
        }

        yield return null;
        yield return null;

        GameObject slot = weaponSlots[0];
        activeWeaponSlot = slot;

        GameObject newWeaponObject =
            Instantiate(AK74Prefab, slot.transform);

        Weapon weapon =
            newWeaponObject.GetComponent<Weapon>();

        if (weapon == null)
        {
            Destroy(newWeaponObject);
            yield break;
        }

        newWeaponObject.transform.localPosition =
            weapon.spawnPosition;

        newWeaponObject.transform.localRotation =
            Quaternion.Euler(weapon.spawnRotation);

        weapon.SetOwner(this, playerCamera);

        // Make it a normal gun with a unique ID (pickup + save)
        if (string.IsNullOrEmpty(weapon.NetworkPickupId))
        {
            weapon.SetNetworkPickupId(
                "starter_ak74_" + OwnerClientId + "_" + System.Guid.NewGuid().ToString("N").Substring(0, 8)
            );
        }

        weapon.isActiveWeapon = true;
        weapon.readyToShoot = true;
        weapon.isReloading = false;

        if (weapon.animator != null)
            weapon.animator.enabled = true;

        Outline outline =
            newWeaponObject.GetComponent<Outline>();

        if (outline != null)
            outline.enabled = false;

        if (totalAK74Ammo <= 0)
            totalAK74Ammo = 30;

        for (int i = 0; i < weaponSlots.Count; i++)
        {
            if (weaponSlots[i] != null)
                weaponSlots[i].SetActive(i == 0);
        }

        SyncNetworkWeaponSlots();
        networkActiveWeaponSlot.Value = 0;

        Debug.Log("Starter AK74 given to player.");
    }

  



    private void ApplySavedMagazine(WeaponSaveData savedWeapon, int slotIndex)
    {
        if (savedWeapon == null || !savedWeapon.hasWeapon)
            return;

        if (weaponSlots == null ||
            slotIndex < 0 ||
            slotIndex >= weaponSlots.Count ||
            weaponSlots[slotIndex] == null)
            return;

        Weapon weapon =
            weaponSlots[slotIndex].GetComponentInChildren<Weapon>(true);

        if (weapon == null)
            return;

        weapon.bulletsLeft =
            Mathf.Clamp(
                savedWeapon.magazineAmmo,
                0,
                weapon.magazineSize
            );

        weapon.keepSavedMagazine = true;

        Debug.Log(
            $"LOAD SLOT {slotIndex + 1} | {weapon.thisWeaponModel} | " +
            $"Mag={weapon.bulletsLeft}/{weapon.magazineSize}"
        );
    }

    private Weapon SpawnRemoteWeaponVisual(string weaponId, GameObject slot)
    {
        if (string.IsNullOrEmpty(weaponId) || slot == null)
            return null;

        GameObject prefab = null;

        // Starter AK from GiveStarterWeapon
        if (weaponId.StartsWith("starter_ak74"))
        {
            prefab = AK74Prefab;
        }
        // Optional: extend later for other starters
        else if (weaponId.Contains("AK74") || weaponId.Contains("ak74"))
        {
            prefab = AK74Prefab;
        }

        if (prefab == null)
            return null;

        // Clear slot first
        if (slot.transform.childCount > 0)
        {
            Destroy(slot.transform.GetChild(0).gameObject);
        }

        GameObject newWeaponObject =
            Instantiate(prefab, slot.transform);

        Weapon weapon =
            newWeaponObject.GetComponent<Weapon>();

        if (weapon == null)
        {
            Destroy(newWeaponObject);
            return null;
        }

        weapon.SetNetworkPickupId(weaponId);

        newWeaponObject.transform.localPosition =
            weapon.spawnPosition;

        newWeaponObject.transform.localRotation =
            Quaternion.Euler(weapon.spawnRotation);

        weapon.SetOwner(this, playerCamera);
        weapon.isActiveWeapon =
            (slot == activeWeaponSlot);

        if (weapon.animator != null)
            weapon.animator.enabled = true;

        Outline outline =
            newWeaponObject.GetComponent<Outline>();

        if (outline != null)
            outline.enabled = false;

        newWeaponObject.SetActive(true);

        Debug.Log(
            $"Remote weapon visual spawned | Id={weaponId}"
        );

        return weapon;
    }
}






