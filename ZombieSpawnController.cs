using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Unity.Netcode;

public class ZombieSpawnController : NetworkBehaviour
{
    public static ZombieSpawnController Instance;

    [Header("Wave Settings")]
    public int initialZombiePerWave = 2;
    public int currentZombiePerWave;
    public float spawnDelay = 0.5f;
    public float waveCooldown = 10.0f;
    public int currentWave = 0;

    public NetworkVariable<bool> inCooldown = new NetworkVariable<bool>(false);
    public NetworkVariable<float> cooldownCounter = new NetworkVariable<float>(0f);

    public List<Enemy> currentZombiesAlive = new List<Enemy>();
    private int nextZombieId = 0;
    public GameObject zombiePrefab;

    [Header("UI")]
    public TextMeshProUGUI TileWaveOver;
    public TextMeshProUGUI cooldownCounterUI;

    public bool isWaveActive = false;
    private float waveStartTime = 0f;
    private const float MIN_WAVE_TIME = 8f;
    private const int LAST_WAVE = 2;

    private string currentSceneName = "";
    private Vector3 currentSpawnCenter;
    private bool levelComplete = false;

    public bool IsCurrentLevelComplete => levelComplete;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void Start()
    {
        currentZombiePerWave = initialZombiePerWave;
        HideUI();
    }

    //Tuesday: Added methods to start waves for different levels
    /* public void StartCityWaves(Vector3 spawnCenter)
     {
         if (!IsServer) return;
         currentSceneName = "City";
         currentSpawnCenter = spawnCenter;
         ResetAndStart();
     }
    public void StartJungleWaves(Vector3 spawnCenter)
    {
        if (!IsServer) return;
        currentSceneName = "Jungle";
        currentSpawnCenter = spawnCenter;
        ResetAndStart();
    }
    */
    public void StartCityWaves(Vector3 spawnCenter)
    {
        if (!IsServer)
            return;

        if (SaveManager.Instance != null &&
            SaveManager.Instance.IsLoadingSavedGame)
        {
            Debug.Log(
                "Saved Game active - normal City waves blocked."
            );

            return;
        }

        currentSceneName = "City";
        currentSpawnCenter = spawnCenter;

        ResetAndStart();
    }

    public void StartJungleWaves(Vector3 spawnCenter)
    {
        if (!IsServer)
            return;

        currentSceneName = "Jungle";
        currentSpawnCenter = spawnCenter;

        ResetAndStart();

        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.IsLoadingSavedGame = false;
        }
    }


    private void ResetAndStart()
    {
        if (!IsServer) return;

        StopAllCoroutines();
        currentWave = 0;
        currentZombiePerWave = initialZombiePerWave;
        currentZombiesAlive.Clear();
        inCooldown.Value = false;
        cooldownCounter.Value = 0f;
        isWaveActive = false;
        levelComplete = false;
        HideUIClientRpc();
        StartCoroutine(DelayedStart());
    }

    private IEnumerator DelayedStart()
    {
        HideUIClientRpc();
        yield return new WaitForSeconds(1.0f);
        HideUIClientRpc();
        inCooldown.Value = false;
        cooldownCounter.Value = 0f;
        yield return new WaitForSeconds(1.5f);
        HideUIClientRpc();
        StartNextWave();
    }


    //Tuesday: Added a method to start the next wave after a delay
    /*private void StartNextWave()
    {
        if (!IsServer || levelComplete) return;
        currentZombiesAlive.Clear();
        currentWave++;
        isWaveActive = false;
        StartCoroutine(SpawnWave());
    }*/

    private void StartNextWave()
    {
        if (!IsServer)
            return;

        if (levelComplete)
            return;

        // NEVER allow a wave after the final wave.
        if (currentWave >= LAST_WAVE)
        {
            levelComplete = true;
            isWaveActive = false;
            inCooldown.Value = false;
            cooldownCounter.Value = 0f;

            HideUIClientRpc();

            Debug.Log(
                $"WAVE LIMIT REACHED | Level={currentSceneName} | " +
                $"Wave={currentWave}"
            );

            return;
        }

        currentZombiesAlive.Clear();

        currentWave++;

        isWaveActive = false;

        StartCoroutine(SpawnWave());
    }

    private IEnumerator SpawnWave()
    {
        if (!IsServer) yield break;

        for (int i = 0; i < currentZombiePerWave; i++)
        {
            Vector3 spawnOffset = new Vector3(Random.Range(-2f, 2f), 0f, Random.Range(-2f, 2f));
            Vector3 spawnPosition = currentSpawnCenter + spawnOffset;
            GameObject zombieGO = Instantiate(zombiePrefab, spawnPosition, Quaternion.identity);
            NetworkObject netObj = zombieGO.GetComponent<NetworkObject>();
            if (netObj != null) netObj.Spawn(true);

            var netTransform = zombieGO.GetComponent<Unity.Netcode.Components.NetworkTransform>();
            if (netTransform != null)
                netTransform.Teleport(spawnPosition, zombieGO.transform.rotation, zombieGO.transform.localScale);

            Enemy enemy = zombieGO.GetComponent<Enemy>();
            if (enemy != null)
            {
                enemy.zombieId = nextZombieId++;
                currentZombiesAlive.Add(enemy);
            }
            yield return new WaitForSeconds(spawnDelay);
        }

        isWaveActive = true;
        waveStartTime = Time.time;
    }

    /* private void Update()
     {
         if (!IsServer) return;

         for (int i = currentZombiesAlive.Count - 1; i >= 0; i--)
         {
             if (currentZombiesAlive[i] == null || currentZombiesAlive[i].isDead)
                 currentZombiesAlive.RemoveAt(i);
         }

         bool canFinish = isWaveActive && currentZombiesAlive.Count == 0 && !inCooldown.Value && currentWave <= LAST_WAVE && Time.time - waveStartTime >= MIN_WAVE_TIME;
         if (canFinish)
         {
             isWaveActive = false;

             //Tuesday: Save the checkpoint when the wave is completed
             SaveCurrentCheckpoint();


             if (currentWave >= LAST_WAVE)
             {
                 levelComplete = true;
                 inCooldown.Value = false;
                 cooldownCounter.Value = 0f;
                 HideUIClientRpc();
             }
             else
             {
                 StartCoroutine(WaveCooldown());
             }
         }

         if (inCooldown.Value)
             cooldownCounter.Value = Mathf.Max(0f, cooldownCounter.Value - Time.deltaTime);
     }*/

    private void Update()
    {
        if (!IsServer)
            return;

        for (int i = currentZombiesAlive.Count - 1; i >= 0; i--)
        {
            if (currentZombiesAlive[i] == null ||
                currentZombiesAlive[i].isDead)
            {
                currentZombiesAlive.RemoveAt(i);
            }
        }

        bool canFinish =
            isWaveActive &&
            currentZombiesAlive.Count == 0 &&
            !inCooldown.Value &&
            Time.time - waveStartTime >= MIN_WAVE_TIME;

        if (canFinish)
        {
            isWaveActive = false;

            SaveCurrentCheckpoint();

            // =========================================================
            // FINAL WAVE
            // =========================================================

            if (currentWave >= LAST_WAVE)
            {
                levelComplete = true;

                inCooldown.Value = false;
                cooldownCounter.Value = 0f;

                HideUIClientRpc();

                Debug.Log(
                    $"FINAL WAVE COMPLETE | " +
                    $"Level={currentSceneName} | " +
                    $"Wave={currentWave}"
                );

                PlayerMovement playerMovement =
                    FindObjectOfType<PlayerMovement>();

                if (playerMovement != null)
                {
                    playerMovement.CompleteLevelFromZombieWaves();
                }

                return;
            }

            // =========================================================
            // NORMAL WAVE
            // =========================================================

            StartCoroutine(WaveCooldown());
        }

        if (inCooldown.Value)
        {
            cooldownCounter.Value =
                Mathf.Max(
                    0f,
                    cooldownCounter.Value - Time.deltaTime
                );
        }
    }

    private IEnumerator RequestLevelTransitionAfterFinalWave()
    {
        yield return new WaitForSeconds(0.5f);

        PlayerMovement playerMovement =
            FindObjectOfType<PlayerMovement>();

        if (playerMovement == null)
        {
            Debug.LogError(
                "PlayerMovement not found. Cannot start level transition."
            );

            yield break;
        }

        Debug.Log(
            $"FINAL WAVE CLEARED | Requesting transition from {currentSceneName}"
        );

        playerMovement.RequestLevelClear();
    }

    private void LateUpdate()
    {
        if (!IsSpawned)
            return;

        bool shouldShow =
            inCooldown.Value &&
            cooldownCounter.Value > 0.1f &&
            !IsCurrentLevelComplete;

        if (TileWaveOver != null)
        {
            if (TileWaveOver.gameObject.activeSelf != shouldShow)
                TileWaveOver.gameObject.SetActive(shouldShow);
        }

        if (cooldownCounterUI != null)
        {
            if (cooldownCounterUI.gameObject.activeSelf != shouldShow)
                cooldownCounterUI.gameObject.SetActive(shouldShow);

            cooldownCounterUI.text =
                Mathf.CeilToInt(cooldownCounter.Value).ToString();
        }
    }

    private IEnumerator WaveCooldown()
    {
        inCooldown.Value = true;
        cooldownCounter.Value = waveCooldown;
        yield return new WaitForSeconds(waveCooldown);
        inCooldown.Value = false;
        cooldownCounter.Value = 0f;
        currentZombiePerWave *= 2;
        StartNextWave();
    }

    private void SetUI(bool show)
    {
        if (TileWaveOver != null) TileWaveOver.gameObject.SetActive(show);
        if (cooldownCounterUI != null) cooldownCounterUI.gameObject.SetActive(show);
    }

    //Tuesday: Hide the UI when the level is complete
    // private void HideUI() => SetUI(false);

    public void HideUI() => SetUI(false);

    [ClientRpc]
    private void HideUIClientRpc() => HideUI();


    //Tuesday
    private void SaveCurrentCheckpoint()
    {
        if (SaveManager.Instance == null)
        {
            Debug.LogWarning(
                "SaveManager not found. Cannot save checkpoint."
            );

            return;
        }

        PlayerMovement playerMovement =
            FindObjectOfType<PlayerMovement>();

        if (playerMovement == null)
        {
            Debug.LogWarning(
                "PlayerMovement not found. Cannot save checkpoint."
            );

            return;
        }

        Player player =
            playerMovement.GetComponent<Player>();

        WeaponManager weaponManager =
            playerMovement.GetComponentInChildren<WeaponManager>();

        if (player == null || weaponManager == null)
        {
            Debug.LogWarning(
                "Player or WeaponManager not found. Cannot save checkpoint."
            );

            return;
        }

        SaveData data =
            new SaveData();

        // =========================================================
        // CHECKPOINT
        // =========================================================

        data.levelName =
            currentSceneName;

        data.completedWave =
            currentWave;


        // =========================================================
        // PLAYER HEALTH
        // =========================================================

        data.playerHP =
            player.HP;


        // =========================================================
        // ACTIVE WEAPON SLOT
        // =========================================================

        data.activeWeaponSlot =
            weaponManager.weaponSlots.IndexOf(
                weaponManager.activeWeaponSlot
            );


        // =========================================================
        // SLOT 1
        // =========================================================

        data.slot1 =
            CreateWeaponSaveData(
                weaponManager,
                0
            );


        // =========================================================
        // SLOT 2
        // =========================================================

        data.slot2 =
            CreateWeaponSaveData(
                weaponManager,
                1
            );


        // =========================================================
        // RESERVE AMMO
        // =========================================================

        data.totalAK74Ammo =
            weaponManager.totalAK74Ammo;

        data.totalM249Ammo =
            weaponManager.totalM249Ammo;

        data.totalM107Ammo =
            weaponManager.totalM107Ammo;


        // =========================================================
        // GRENADE
        // =========================================================

        data.lethalType =
            weaponManager.equippedLethalType.ToString();

        data.lethalCount =
            weaponManager.lethalsCount;


        // =========================================================
        // SMOKE GRENADE
        // =========================================================

        data.tacticalType =
            weaponManager.equippedTacticalType.ToString();

        data.tacticalCount =
            weaponManager.tacticalCount;

        // =========================================================
        // PICKED-UP WORLD ITEMS
        // =========================================================

        data.pickedUpWeaponIds =
            weaponManager.GetPickedUpWeaponIdsForSave();

        data.pickedUpThrowableIds =
            weaponManager.GetPickedUpThrowableIdsForSave();

        // =========================================================
        // PICKED-UP AmmoBoxes and HealthBoxes
        // =========================================================
        data.pickedUpAmmoIds =
    weaponManager.GetPickedUpAmmoIdsForSave();

        data.pickedUpHealthBoxIds =
            weaponManager.GetPickedUpHealthBoxIdsForSave();


        // =========================================================
        // WRITE SAVE FILE
        // =========================================================

        SaveManager.Instance.SaveGame(
            data
        );

        //wednesday: Added a check to show "GAME SAVED" only in single player mode
        // ===== SHOW "GAME SAVED" ONLY IN SINGLE PLAYER =====
        bool isSinglePlayer = NetworkManager.Singleton == null
                              || NetworkManager.Singleton.ConnectedClientsList.Count <= 1;

        if (isSinglePlayer && playerMovement != null)
        {
            BriefingBox briefing = playerMovement.GetComponentInChildren<BriefingBox>(true);
            if (briefing != null)
            {
                briefing.PlayCustomMessage("GAME SAVED", 2.5f);
            }
        }

        Debug.Log(
            $"CHECKPOINT SAVED | " +
            $"Level: {data.levelName} | " +
            $"Wave: {data.completedWave} | " +
            $"HP: {data.playerHP} | " +
            $"AK74 Ammo: {data.totalAK74Ammo} | " +
            $"M249 Ammo: {data.totalM249Ammo} | " +
            $"M107 Ammo: {data.totalM107Ammo} | " +
            $"Grenades: {data.lethalCount} | " +
            $"Smoke: {data.tacticalCount}"
        );
    }

    private WeaponSaveData CreateWeaponSaveData(
     WeaponManager weaponManager,
     int slotIndex)
    {
        WeaponSaveData data = new WeaponSaveData();

        if (weaponManager.weaponSlots == null ||
            slotIndex < 0 ||
            slotIndex >= weaponManager.weaponSlots.Count)
        {
            data.hasWeapon = false;
            return data;
        }

        GameObject slot = weaponManager.weaponSlots[slotIndex];

        if (slot == null)
        {
            data.hasWeapon = false;
            return data;
        }

        // More reliable than GetChild(0)
        Weapon weapon =
            slot.GetComponentInChildren<Weapon>(true);

        if (weapon == null)
        {
            data.hasWeapon = false;
            return data;
        }

        data.hasWeapon = true;
        data.weaponId = weapon.NetworkPickupId;
        data.weaponModel = weapon.thisWeaponModel.ToString();
        data.magazineAmmo = weapon.bulletsLeft;

        Debug.Log(
            $"SAVE SLOT {slotIndex + 1} | {data.weaponModel} | " +
            $"Mag={data.magazineAmmo}/{weapon.magazineSize}"
        );

        return data;
    }

    //tuesday: Added a method to reset the wave system when the player dies
    public void StartSavedGameWaves(
      Vector3 spawnCenter,
      int completedWave)
    {
        if (!IsServer)
            return;

        if (SaveManager.Instance == null ||
            SaveManager.Instance.LoadedSaveData == null)
        {
            Debug.LogWarning(
                "Saved game data is missing."
            );

            return;
        }

        currentSceneName =
            SaveManager.Instance.LoadedSaveData.levelName;

        currentSpawnCenter = spawnCenter;

        StopAllCoroutines();

        currentZombiesAlive.Clear();

        inCooldown.Value = false;
        cooldownCounter.Value = 0f;

        isWaveActive = false;
        levelComplete = false;

        // Never allow an invalid save to start beyond Wave 2.
        completedWave =
            Mathf.Clamp(
                completedWave,
                0,
                LAST_WAVE
            );

        currentWave = completedWave;

        // If Wave 2 was already completed,
        // the level is finished. Do NOT start Wave 3.
        if (currentWave >= LAST_WAVE)
        {
            levelComplete = true;

            HideUIClientRpc();

            Debug.Log(
                $"SAVED GAME ALREADY COMPLETE | " +
                $"Level={currentSceneName} | " +
                $"Completed Wave={currentWave}"
            );

            return;
        }

        currentZombiePerWave =
            initialZombiePerWave *
            (int)Mathf.Pow(
                2,
                currentWave
            );

        HideUIClientRpc();

        Debug.Log(
            $"SAVED GAME WAVES STARTING | " +
            $"Level={currentSceneName} | " +
            $"Completed Wave={currentWave} | " +
            $"Next Wave={currentWave + 1} | " +
            $"Zombies={currentZombiePerWave}"
        );

        StartCoroutine(
            StartSavedWaveAfterDelay()
        );
    }
    private IEnumerator StartSavedWaveAfterDelay()
    {
        yield return new WaitForSeconds(1.0f);

        StartNextWave();
    }
}
