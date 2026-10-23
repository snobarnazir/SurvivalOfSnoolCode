using System;
using System.Collections.Generic;

[Serializable]
public class SaveData
{
    // =========================================================
    // CHECKPOINT
    // =========================================================

    public string levelName;
    public int completedWave;


    // =========================================================
    // PLAYER
    // =========================================================

    public int playerHP;


    // =========================================================
    // WEAPONS
    // =========================================================

    public WeaponSaveData slot1;
    public WeaponSaveData slot2;

    public int activeWeaponSlot;


    // =========================================================
    // RESERVE AMMO
    // One ammo pool per weapon type
    // =========================================================

    public int totalAK74Ammo;
    public int totalM249Ammo;
    public int totalM107Ammo;


    // =========================================================
    // LETHAL GRENADE
    // =========================================================

    public string lethalType;
    public int lethalCount;
    public List<string> pickedUpThrowableIds;
    public List<string> pickedUpWeaponIds;
   


    // =========================================================
    // SMOKE GRENADE
    // =========================================================

    public string tacticalType;
    public int tacticalCount;
    public List<string> pickedUpAmmoIds;
    public List<string> pickedUpHealthBoxIds;
}


// =============================================================
// INDIVIDUAL WEAPON SLOT DATA
// =============================================================

[Serializable]
public class WeaponSaveData
{
    public bool hasWeapon;

    public string weaponId;
    public string weaponModel;

    public int magazineAmmo;
}