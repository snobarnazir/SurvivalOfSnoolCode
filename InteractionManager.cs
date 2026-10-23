using System;
using UnityEngine;

public class InteractionManager : MonoBehaviour
{
    public Weapon hoveredWeapon = null;
    public AmmoBox hoveredAmmoBox = null;
    public Throwable hoveredThrowable = null;

    private WeaponManager weaponManager;
    private Camera playerCamera;


private void Awake()
    {
        PlayerMovement playerMovement =
            GetComponentInParent<PlayerMovement>();

        if (playerMovement == null)
        {
            Debug.LogError(
                "InteractionManager: PlayerMovement not found."
            );
            return;
        }

        weaponManager =
            playerMovement.GetComponentInChildren<WeaponManager>(
                true
            );

        playerCamera =
            playerMovement.GetComponentInChildren<Camera>(
                true
            );

        if (weaponManager == null)
        {
            Debug.LogError(
                "InteractionManager: WeaponManager not found inside Player."
            );
        }

        if (playerCamera == null)
        {
            Debug.LogError(
                "InteractionManager: Camera not found inside Player."
            );
        }
    }




    private void Update()
    {
        // Only the local player's interaction system should run.
        PlayerMovement playerMovement =
            GetComponentInParent<PlayerMovement>();

        if (playerMovement != null && !playerMovement.IsOwner)
        {
            return;
        }

        if (weaponManager == null)
        {
            Debug.LogError(
                "InteractionManager: WeaponManager not found on this Player."
            );
            return;
        }

        if (playerCamera == null)
        {
            Debug.LogError(
                "InteractionManager: Player Camera not found."
            );
            return;
        }

        Ray ray = playerCamera.ViewportPointToRay(
            new Vector3(0.5f, 0.5f, 0)
        );
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit))
        {
            GameObject objectHitByRaycast =
                hit.transform.gameObject;

            // =========================
            // WEAPON
            // =========================

            Weapon weapon =
                objectHitByRaycast.GetComponent<Weapon>();

            if (weapon != null && !weapon.isActiveWeapon)
            {
                hoveredWeapon = weapon;

                Outline outline =
                    hoveredWeapon.GetComponent<Outline>();

                if (outline != null)
                {
                    outline.enabled = true;
                }

                if (Input.GetKeyDown(KeyCode.F))
                {
                    Debug.Log(
                        "F Pressed - Picking up weapon"
                    );

                    weaponManager.PickupWeapon(
                        objectHitByRaycast
                    );
                }
            }
            else
            {
                if (hoveredWeapon != null)
                {
                    Outline outline =
                        hoveredWeapon.GetComponent<Outline>();

                    if (outline != null)
                    {
                        outline.enabled = false;
                    }

                    hoveredWeapon = null;
                }
            }
            // =========================
            // AMMO BOX
            // =========================

            AmmoBox ammoBox =
                objectHitByRaycast.GetComponent<AmmoBox>();

            if (ammoBox != null)
            {
                hoveredAmmoBox = ammoBox;

                Outline outline =
                    hoveredAmmoBox.GetComponent<Outline>();

                if (outline != null)
                {
                    outline.enabled = true;
                }

                if (Input.GetKeyDown(KeyCode.F))
                {
                    weaponManager.PickupAmmo(
                        hoveredAmmoBox
                    );
                }
            }
            else
            {
                if (hoveredAmmoBox != null)
                {
                    Outline outline =
                        hoveredAmmoBox.GetComponent<Outline>();

                    if (outline != null)
                    {
                        outline.enabled = false;
                    }

                    hoveredAmmoBox = null;
                }
            }

            //wednesday, 12 June 2024
            // =========================
            // HEALTH BOX
            // =========================

            HealthBox healthBox =
                objectHitByRaycast.GetComponent<HealthBox>();

            if (healthBox != null)
            {
                hoveredHealthBox = healthBox;

                Outline outline =
                    hoveredHealthBox.GetComponent<Outline>();

                if (outline != null)
                {
                    outline.enabled = true;
                }

                if (Input.GetKeyDown(KeyCode.F))
                {
                    weaponManager.PickupHealthBox(hoveredHealthBox);
                }
            }
            else
            {
                if (hoveredHealthBox != null)
                {
                    Outline outline =
                        hoveredHealthBox.GetComponent<Outline>();

                    if (outline != null)
                    {
                        outline.enabled = false;
                    }

                    hoveredHealthBox = null;
                }
            }

            // =========================
            // THROWABLE
            // =========================

            Throwable throwable =
                objectHitByRaycast.GetComponent<Throwable>();

            if (throwable != null)
            {
                hoveredThrowable = throwable;

                Outline outline =
                    hoveredThrowable.GetComponent<Outline>();

                if (outline != null)
                {
                    outline.enabled = true;
                }

                if (Input.GetKeyDown(KeyCode.F))
                {
                    weaponManager.PickupThrowable(
                        hoveredThrowable
                    );
                }
            }
            else
            {
                if (hoveredThrowable != null)
                {
                    Outline outline =
                        hoveredThrowable.GetComponent<Outline>();

                    if (outline != null)
                    {
                        outline.enabled = false;
                    }

                    hoveredThrowable = null;
                }
            }
        }
        else
        {
            // Nothing under the player's crosshair.

            if (hoveredWeapon != null)
            {
                Outline outline =
                    hoveredWeapon.GetComponent<Outline>();

                if (outline != null)
                {
                    outline.enabled = false;
                }

                hoveredWeapon = null;
            }

            if (hoveredAmmoBox != null)
            {
                Outline outline =
                    hoveredAmmoBox.GetComponent<Outline>();

                if (outline != null)
                {
                    outline.enabled = false;
                }

                hoveredAmmoBox = null;
            }

            if (hoveredThrowable != null)
            {
                Outline outline =
                    hoveredThrowable.GetComponent<Outline>();

                if (outline != null)
                {
                    outline.enabled = false;
                }

                hoveredThrowable = null;
            }

            //wednesday, 12 June 2024

            if (hoveredHealthBox != null)
            {
                Outline outline =
                    hoveredHealthBox.GetComponent<Outline>();

                if (outline != null)
                {
                    outline.enabled = false;
                }

                hoveredHealthBox = null;
            }
        }
    }


    //wednesday, 12 June 2024
    public HealthBox hoveredHealthBox = null;
}

/*
 if (hoveredWeapon)
{
  hoveredWeapon.GetComponent<Outline>().enabled=false;
}
 
 */