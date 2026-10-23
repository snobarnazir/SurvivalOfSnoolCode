
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HUDManager : MonoBehaviour
{
    public static HUDManager Instance { get; set; }

    private GameObject localWeaponPanel;
    private bool hudInitialized = false;
    private WeaponManager localWeaponManager;

    [Header("Ammo")]
    public TextMeshProUGUI magazineAmmoUI;
    public TextMeshProUGUI totalAmmoUI;

    private bool magazineUIConnected = false;
    private bool totalAmmoUIConnected = false;
    private bool ammoTypeUIConnected = false;
    private bool activeWeaponUIConnected = false;
    private bool unActiveWeaponUIConnected = false;
    private bool lethalUIConnected = false;
    private bool lethalAmountUIConnected = false;
    private bool tacticalUIConnected = false;
    private bool tacticalAmountUIConnected = false;

    public Image ammoTypeUI;

    [Header("Weapon")]
    public Image activeWeaponUI;
    public Image unActiveWeaponUI;

    [Header("Throwables")]
    public Image lethalUI;
    public TextMeshProUGUI lethalAmountUI;
    public Image tacticalUI;
    public TextMeshProUGUI tacticalAmountUI;

    public Sprite emptySlot;
    public Sprite greySlot;


   
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        DontDestroyOnLoad(gameObject);
    }


    //tuesday
    /*private void Update()
    {
        // Find the local player's WeaponManager.
        if (localWeaponManager == null ||
            !BelongsToLocalPlayer(localWeaponManager))
        {
            localWeaponManager = FindLocalWeaponManager();

            if (localWeaponManager == null)
            {
                return;
            }
        }

        // Make sure the player has an active weapon slot.
        if (localWeaponManager.activeWeaponSlot == null)
        {
            return;
        }

        // Find the local PlayerMovement.
        PlayerMovement playerMovement =
            localWeaponManager.GetComponentInParent<PlayerMovement>();

        if (playerMovement == null)
        {
            return;
        }

        Transform player = playerMovement.transform;

        // Find this player's HUD.
        Transform panel =
            player.Find("GunCanvas/WeaponPanel");

        if (panel == null)
        {
            return;
        }

        localWeaponPanel = panel.gameObject;
        hudInitialized = true;


        // =========================================================
        // MAGAZINE AMMO
        // =========================================================

        if (!magazineUIConnected)
        {
            Transform magazine =
                panel.Find("MagazineAmmo");

            if (magazine != null)
            {
                magazineAmmoUI =
                    magazine.GetComponent<TextMeshProUGUI>();

                magazineUIConnected =
                    magazineAmmoUI != null;
            }
        }


        // =========================================================
        // TOTAL AMMO
        // =========================================================

        if (!totalAmmoUIConnected)
        {
            Transform totalAmmo =
                panel.Find("TotalAmmo");

            if (totalAmmo != null)
            {
                totalAmmoUI =
                    totalAmmo.GetComponent<TextMeshProUGUI>();

                totalAmmoUIConnected =
                    totalAmmoUI != null;
            }
        }


        // =========================================================
        // AMMO TYPE
        // =========================================================

        if (!ammoTypeUIConnected)
        {
            Transform ammoType =
                panel.Find("AmmoType");

            if (ammoType != null)
            {
                ammoTypeUI =
                    ammoType.GetComponent<Image>();

                ammoTypeUIConnected =
                    ammoTypeUI != null;
            }
        }


        // =========================================================
        // ACTIVE WEAPON IMAGE
        // =========================================================

        if (!activeWeaponUIConnected)
        {
            Transform activeWeaponImage =
                panel.Find("ActiveWeapon");

            if (activeWeaponImage != null)
            {
                activeWeaponUI =
                    activeWeaponImage.GetComponent<Image>();

                activeWeaponUIConnected =
                    activeWeaponUI != null;
            }
        }


        // =========================================================
        // INACTIVE WEAPON IMAGE
        // =========================================================

        if (!unActiveWeaponUIConnected)
        {
            Transform inactiveWeaponImage =
                panel.Find("UnActiveWeapon");

            if (inactiveWeaponImage != null)
            {
                unActiveWeaponUI =
                    inactiveWeaponImage.GetComponent<Image>();

                unActiveWeaponUIConnected =
                    unActiveWeaponUI != null;
            }
        }


        // =========================================================
        // LETHAL
        // =========================================================

        if (!lethalUIConnected)
        {
            Transform lethal =
                panel.Find("Lethal");

            if (lethal != null)
            {
                lethalUI =
                    lethal.GetComponent<Image>();

                lethalUIConnected =
                    lethalUI != null;
            }
        }


        // =========================================================
        // LETHAL AMOUNT
        // =========================================================

        if (!lethalAmountUIConnected)
        {
            Transform lethalAmount =
                panel.Find("Lethal/LethalAmount");

            if (lethalAmount != null)
            {
                lethalAmountUI =
                    lethalAmount.GetComponent<TextMeshProUGUI>();

                lethalAmountUIConnected =
                    lethalAmountUI != null;
            }
        }


        // =========================================================
        // TACTICAL
        // =========================================================

        if (!tacticalUIConnected)
        {
            Transform tactical =
                panel.Find("Tactical");

            if (tactical != null)
            {
                tacticalUI =
                    tactical.GetComponent<Image>();

                tacticalUIConnected =
                    tacticalUI != null;
            }
        }


        // =========================================================
        // TACTICAL AMOUNT
        // =========================================================

        if (!tacticalAmountUIConnected)
        {
            Transform tacticalAmount =
                panel.Find("Tactical/TacticalAmount");

            if (tacticalAmount != null)
            {
                tacticalAmountUI =
                    tacticalAmount.GetComponent<TextMeshProUGUI>();

                tacticalAmountUIConnected =
                    tacticalAmountUI != null;
            }
        }


        // =========================================================
        // WAIT UNTIL HUD IS CONNECTED
        // =========================================================

        if (!magazineUIConnected ||
            !totalAmmoUIConnected ||
            !ammoTypeUIConnected ||
            !activeWeaponUIConnected ||
            !unActiveWeaponUIConnected ||
            !lethalUIConnected ||
            !lethalAmountUIConnected ||
            !tacticalUIConnected ||
            !tacticalAmountUIConnected)
        {
            return;
        }


        // =========================================================
        // GET ACTIVE WEAPON
        // =========================================================

        Weapon currentActiveWeapon =
            localWeaponManager.activeWeaponSlot
            .GetComponentInChildren<Weapon>();


        // =========================================================
        // GET INACTIVE WEAPON
        // =========================================================

        GameObject inactiveSlot =
            GetUnActiveWeaponSlot();

        Weapon currentInactiveWeapon = null;

        if (inactiveSlot != null)
        {
            currentInactiveWeapon =
                inactiveSlot.GetComponentInChildren<Weapon>();
        }


        // =========================================================
        // UPDATE WEAPON HUD
        // =========================================================

        if (currentActiveWeapon != null)
        {
            

            // Magazine number.
            magazineAmmoUI.text = $"{currentActiveWeapon.bulletsLeft}";



            // Total ammo.
            int totalAmmo =
                localWeaponManager.CheckAmmoLeftFor(
                    currentActiveWeapon.thisWeaponModel
                );

            totalAmmoUI.text =
                $"{totalAmmo}";


            // Weapon model.
            Weapon.WeaponModel model =
                currentActiveWeapon.thisWeaponModel;


            // Ammo icon.
            Sprite ammoSprite =
                GetAmmoSprite(model);

            if (ammoSprite != null)
            {
                ammoTypeUI.sprite =
                    ammoSprite;
            }


            // Active weapon icon.
            Sprite weaponSprite =
                GetWeaponSprite(model);

            if (weaponSprite != null)
            {
                activeWeaponUI.sprite =
                    weaponSprite;
            }


            // Inactive weapon icon.
            if (currentInactiveWeapon != null)
            {
                Sprite inactiveSprite =
                    GetWeaponSprite(
                        currentInactiveWeapon.thisWeaponModel
                    );

                if (inactiveSprite != null)
                {
                    unActiveWeaponUI.sprite =
                        inactiveSprite;
                }
            }
            else
            {
                unActiveWeaponUI.sprite =
                    emptySlot;
            }
        }
        else
        {
            // No active weapon.
            magazineAmmoUI.text = "";
            totalAmmoUI.text = "";

            ammoTypeUI.sprite =
                emptySlot;

            activeWeaponUI.sprite =
                emptySlot;

            unActiveWeaponUI.sprite =
                emptySlot;
        }


        // =========================================================
        // THROWABLE HUD
        // =========================================================

        if (localWeaponManager.lethalsCount <= 0)
        {
            lethalUI.sprite =
                greySlot;
        }

        if (localWeaponManager.tacticalCount <= 0)
        {
            tacticalUI.sprite =
                greySlot;
        }
    }*/


    private void Update()
    {
        // =========================================================
        // FIND THE LOCAL PLAYER'S WEAPON MANAGER
        // =========================================================

        WeaponManager wm =
            FindLocalWeaponManager();

        if (wm == null)
        {
            return;
        }

        localWeaponManager =
            wm;

        // =========================================================
        // FIND THE LOCAL PLAYER
        // =========================================================

        PlayerMovement playerMovement =
            wm.GetComponentInParent<PlayerMovement>();

        if (playerMovement == null)
        {
            return;
        }

        // =========================================================
        // FIND THIS PLAYER'S HUD
        // =========================================================

        Transform panel =
            playerMovement.transform.Find(
                "GunCanvas/WeaponPanel"
            );

        if (panel == null)
        {
            return;
        }

        localWeaponPanel =
            panel.gameObject;

        // =========================================================
        // GET HUD REFERENCES DIRECTLY
        // =========================================================

        Transform magazineTransform =
            panel.Find("MagazineAmmo");

        Transform totalAmmoTransform =
            panel.Find("TotalAmmo");

        Transform ammoTypeTransform =
            panel.Find("AmmoType");

        Transform activeWeaponTransform =
            panel.Find("ActiveWeapon");

        Transform inactiveWeaponTransform =
            panel.Find("UnActiveWeapon");

        Transform lethalTransform =
            panel.Find("Lethal");

        Transform lethalAmountTransform =
            panel.Find("Lethal/LethalAmount");

        Transform tacticalTransform =
            panel.Find("Tactical");

        Transform tacticalAmountTransform =
            panel.Find("Tactical/TacticalAmount");

        // =========================================================
        // CONNECT UI
        // =========================================================

        if (magazineTransform != null)
        {
            magazineAmmoUI =
                magazineTransform.GetComponent<TextMeshProUGUI>();
        }

        if (totalAmmoTransform != null)
        {
            totalAmmoUI =
                totalAmmoTransform.GetComponent<TextMeshProUGUI>();
        }

        if (ammoTypeTransform != null)
        {
            ammoTypeUI =
                ammoTypeTransform.GetComponent<Image>();
        }

        if (activeWeaponTransform != null)
        {
            activeWeaponUI =
                activeWeaponTransform.GetComponent<Image>();
        }

        if (inactiveWeaponTransform != null)
        {
            unActiveWeaponUI =
                inactiveWeaponTransform.GetComponent<Image>();
        }

        if (lethalTransform != null)
        {
            lethalUI =
                lethalTransform.GetComponent<Image>();
        }

        if (lethalAmountTransform != null)
        {
            lethalAmountUI =
                lethalAmountTransform.GetComponent<TextMeshProUGUI>();
        }

        if (tacticalTransform != null)
        {
            tacticalUI =
                tacticalTransform.GetComponent<Image>();
        }

        if (tacticalAmountTransform != null)
        {
            tacticalAmountUI =
                tacticalAmountTransform.GetComponent<TextMeshProUGUI>();
        }

        // =========================================================
        // GET ACTIVE WEAPON
        // =========================================================

        Weapon activeWeapon = null;

        if (wm.activeWeaponSlot != null)
        {
            activeWeapon =
                wm.activeWeaponSlot
                .GetComponentInChildren<Weapon>();
        }

        // =========================================================
        // UPDATE AMMO
        // =========================================================

        if (activeWeapon != null)
        {
            if (magazineAmmoUI != null)
            {
                magazineAmmoUI.text =
                    activeWeapon.bulletsLeft.ToString();
            }

            int reserveAmmo = 0;

            switch (activeWeapon.thisWeaponModel)
            {
                case Weapon.WeaponModel.AK74:

                    reserveAmmo =
                        wm.totalAK74Ammo;

                    break;

                case Weapon.WeaponModel.M249:

                    reserveAmmo =
                        wm.totalM249Ammo;

                    break;

                case Weapon.WeaponModel.M107:

                    reserveAmmo =
                        wm.totalM107Ammo;

                    break;
            }

            if (totalAmmoUI != null)
            {
                totalAmmoUI.text =
                    reserveAmmo.ToString();
            }

            // -----------------------------------------------------
            // AMMO ICON
            // -----------------------------------------------------

            if (ammoTypeUI != null)
            {
                Sprite ammoSprite =
                    GetAmmoSprite(
                        activeWeapon.thisWeaponModel
                    );

                if (ammoSprite != null)
                {
                    ammoTypeUI.sprite =
                        ammoSprite;
                }
            }

            // -----------------------------------------------------
            // ACTIVE WEAPON ICON
            // -----------------------------------------------------

            if (activeWeaponUI != null)
            {
                Sprite weaponSprite =
                    GetWeaponSprite(
                        activeWeapon.thisWeaponModel
                    );

                if (weaponSprite != null)
                {
                    activeWeaponUI.sprite =
                        weaponSprite;
                }
            }
        }
        else
        {
            if (magazineAmmoUI != null)
            {
                magazineAmmoUI.text = "0";
            }

            if (totalAmmoUI != null)
            {
                totalAmmoUI.text = "0";
            }

            if (ammoTypeUI != null)
            {
                ammoTypeUI.sprite =
                    emptySlot;
            }

            if (activeWeaponUI != null)
            {
                activeWeaponUI.sprite =
                    emptySlot;
            }
        }

        // =========================================================
        // INACTIVE WEAPON
        // =========================================================

        GameObject inactiveSlot =
            GetUnActiveWeaponSlot();

        Weapon inactiveWeapon = null;

        if (inactiveSlot != null)
        {
            inactiveWeapon =
                inactiveSlot.GetComponentInChildren<Weapon>();
        }

        if (unActiveWeaponUI != null)
        {
            if (inactiveWeapon != null)
            {
                Sprite inactiveSprite =
                    GetWeaponSprite(
                        inactiveWeapon.thisWeaponModel
                    );

                if (inactiveSprite != null)
                {
                    unActiveWeaponUI.sprite =
                        inactiveSprite;
                }
            }
            else
            {
                unActiveWeaponUI.sprite =
                    emptySlot;
            }
        }

        // =========================================================
        // GRENADE COUNT
        // =========================================================

        if (lethalAmountUI != null)
        {
            lethalAmountUI.text =
                wm.lethalsCount.ToString();
        }

        // =========================================================
        // SMOKE COUNT
        // =========================================================

        if (tacticalAmountUI != null)
        {
            tacticalAmountUI.text =
                wm.tacticalCount.ToString();
        }

        // =========================================================
        // GRENADE ICON
        // =========================================================

        if (lethalUI != null)
        {
            if (wm.lethalsCount > 0)
            {
                GameObject grenade =
                    Resources.Load<GameObject>("Grenade");

                if (grenade != null)
                {
                    SpriteRenderer renderer =
                        grenade.GetComponent<SpriteRenderer>();

                    if (renderer != null)
                    {
                        lethalUI.sprite =
                            renderer.sprite;
                    }
                }
            }
            else
            {
                lethalUI.sprite =
                    greySlot;
            }
        }

        // =========================================================
        // SMOKE ICON
        // =========================================================

        if (tacticalUI != null)
        {
            if (wm.tacticalCount > 0)
            {
                GameObject smokeGrenade =
                    Resources.Load<GameObject>("Smoke_Grenade");

                if (smokeGrenade != null)
                {
                    SpriteRenderer renderer =
                        smokeGrenade.GetComponent<SpriteRenderer>();

                    if (renderer != null)
                    {
                        tacticalUI.sprite =
                            renderer.sprite;
                    }
                }
            }
            else
            {
                tacticalUI.sprite =
                    greySlot;
            }
        }
    }

    private WeaponManager FindLocalWeaponManager()
    {
        WeaponManager[] allManagers =
            FindObjectsByType<WeaponManager>(
                FindObjectsSortMode.None
            );

        foreach (WeaponManager wm in allManagers)
        {
            if (BelongsToLocalPlayer(wm))
            {
                return wm;
            }
        }

        return null;
    }


    private bool BelongsToLocalPlayer(
        WeaponManager wm)
    {
        if (wm == null)
        {
            return false;
        }

        PlayerMovement playerMovement =
            wm.GetComponentInParent<PlayerMovement>();

        if (playerMovement == null)
        {
            return false;
        }

        return playerMovement.IsOwner;
    }


    private Sprite GetWeaponSprite(
        Weapon.WeaponModel model)
    {
        GameObject weaponObject = null;

        switch (model)
        {
            case Weapon.WeaponModel.AK74:
                weaponObject =
                    Resources.Load<GameObject>("AK74_Weapon");
                break;

            case Weapon.WeaponModel.M249:
                weaponObject =
                    Resources.Load<GameObject>("M249_Weapon");
                break;

            case Weapon.WeaponModel.M107:
                weaponObject =
                    Resources.Load<GameObject>("M107_Weapon");
                break;
        }

        if (weaponObject == null)
        {
            return null;
        }

        SpriteRenderer renderer =
            weaponObject.GetComponent<SpriteRenderer>();

        if (renderer == null)
        {
            return null;
        }

        return renderer.sprite;
    }


    private Sprite GetAmmoSprite(
        Weapon.WeaponModel model)
    {
        GameObject ammoObject = null;

        switch (model)
        {
            case Weapon.WeaponModel.AK74:
                ammoObject =
                    Resources.Load<GameObject>("AK74_Ammo");
                break;

            case Weapon.WeaponModel.M249:
                ammoObject =
                    Resources.Load<GameObject>("M249_Ammo");
                break;

            case Weapon.WeaponModel.M107:
                ammoObject =
                    Resources.Load<GameObject>("M107_Ammo");
                break;
        }

        if (ammoObject == null)
        {
            return null;
        }

        SpriteRenderer renderer =
            ammoObject.GetComponent<SpriteRenderer>();

        if (renderer == null)
        {
            return null;
        }

        return renderer.sprite;
    }


    private GameObject GetUnActiveWeaponSlot()
    {
        if (localWeaponManager == null)
        {
            return null;
        }

        if (localWeaponManager.weaponSlots == null)
        {
            return null;
        }

        foreach (
            GameObject weaponSlot
            in localWeaponManager.weaponSlots)
        {
            if (weaponSlot != null &&
                weaponSlot !=
                localWeaponManager.activeWeaponSlot)
            {
                return weaponSlot;
            }
        }

        return null;
    }


    internal void UpdateThrowablesUI()
    {
        if (localWeaponManager == null)
        {
            return;
        }

        if (lethalAmountUI != null)
        {
            lethalAmountUI.text =
                $"{localWeaponManager.lethalsCount}";
        }

        if (tacticalAmountUI != null)
        {
            tacticalAmountUI.text =
                $"{localWeaponManager.tacticalCount}";
        }


        if (lethalUI != null)
        {
            switch (
                localWeaponManager.equippedLethalType)
            {
                case Throwable.ThrowableType.Grenade:

                    GameObject grenade =
                        Resources.Load<GameObject>(
                            "Grenade"
                        );

                    if (grenade != null)
                    {
                        SpriteRenderer renderer =
                            grenade.GetComponent<SpriteRenderer>();

                        if (renderer != null)
                        {
                            lethalUI.sprite =
                                renderer.sprite;
                        }
                    }

                    break;
            }
        }


        if (tacticalUI != null)
        {
            switch (
                localWeaponManager.equippedTacticalType)
            {
                case Throwable.ThrowableType.Smoke_Grenade:

                    GameObject smokeGrenade =
                        Resources.Load<GameObject>(
                            "Smoke_Grenade"
                        );

                    if (smokeGrenade != null)
                    {
                        SpriteRenderer renderer =
                            smokeGrenade.GetComponent<SpriteRenderer>();

                        if (renderer != null)
                        {
                            tacticalUI.sprite =
                                renderer.sprite;
                        }
                    }

                    break;
            }
        }
    }
}

