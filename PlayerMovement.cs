using Unity.Netcode;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerMovement : NetworkBehaviour
{
    private CharacterController controller;
    public Animator animator;

    public float speed = 12f;
    public float gravity = -9.81f * 2;
    public float jumpHeight = 3f;

    public Transform groundCheck;
    public float groundDistance = 0.4f;
    public LayerMask groundMask;

    public Camera playerCamera;

    Vector3 velocity;
    bool isGrounded;
    bool isMoving;

    private Vector3 lastPosition = Vector3.zero;

    //tuesday 6/11/2024 - added a new variable to track the player's spawn index for multiplayer spawning
    private bool savedGameSpawnReady = false;
    private bool savedWavesStarted = false;

    //private bool savedGameSpawnReady = false;

    void Start()
    {
        controller = GetComponent<CharacterController>();
    }

   
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        AssignSpawnIndexIfServer();

        if (IsServer)
        {
            NetworkManager.Singleton.SceneManager.OnLoadEventCompleted +=
                OnInitialMazeLevel1Loaded;

            NetworkManager.Singleton.SceneManager.OnLoadEventCompleted +=
                OnInitialMazeLevel2Loaded;
        }

        if (IsOwner)
        {
            StartCoroutine(WaitForInitialSpawnPoint());
        }

        if (!IsOwner)
        {
            if (playerCamera != null)
            {
                playerCamera.enabled = false;

                AudioListener listener =
                    playerCamera.GetComponent<AudioListener>();

                if (listener != null)
                {
                    listener.enabled = false;
                }
            }

            Transform gunCanvas =
                transform.Find("GunCanvas");

            if (gunCanvas != null)
            {
                gunCanvas.gameObject.SetActive(false);
            }

            Transform multiChat = transform.Find("MultiplayerChat");
            if (multiChat != null)
            {
                multiChat.gameObject.SetActive(false);
            }
        }
    }

 

    void Update()
    {
        if (!IsOwner)
            return;



        if (ChatUI.IsChatOpen)
            return;

        // ---------------------------------------------------------
        // SAVED GAME: DO NOT MOVE OR APPLY GRAVITY YET
        // ---------------------------------------------------------

        if (SaveManager.Instance != null &&
            SaveManager.Instance.IsLoadingSavedGame &&
            !savedGameSpawnReady)
        {
            return;
        }


        // ---------------------------------------------------------
        // NORMAL PLAYER MOVEMENT
        // ---------------------------------------------------------

        isGrounded = Physics.CheckSphere(
            groundCheck.position,
            groundDistance,
            groundMask
        );

        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
        }

        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");

        Vector3 move =
            transform.right * x +
            transform.forward * z;

        controller.Move(
            move * speed * Time.deltaTime
        );

        animator.SetFloat("Horizontal", x);
        animator.SetFloat("Vertical", z);

        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            velocity.y =
                Mathf.Sqrt(
                    jumpHeight * -2f * gravity
                );

            animator.SetTrigger("Jump");
        }

        velocity.y +=
            gravity * Time.deltaTime;

        controller.Move(
            velocity * Time.deltaTime
        );

        isMoving =
            lastPosition != transform.position &&
            isGrounded;

        lastPosition =
            transform.position;
    }

   

    public void RequestSceneChange(string sceneName, string spawnPointName)
    {
        if (!IsOwner) return;
        RequestPlayerTravelServerRpc(sceneName, spawnPointName);
    }

    [ServerRpc]
    private void RequestPlayerTravelServerRpc(
        string sceneName,
        string spawnPointName,
        ServerRpcParams rpcParams = default)
    {
        StartCoroutine(
            TravelToSceneServer(sceneName, spawnPointName)
        );
    }

    private IEnumerator TravelToSceneServer(
        string sceneName,
        string spawnPointName)
    {
        // 1. Load destination scene if needed
        Scene destScene = SceneManager.GetSceneByName(sceneName);

        if (!destScene.IsValid() || !destScene.isLoaded)
        {
            bool loaded = false;

            void OnLoaded(
                string loadedName,
                LoadSceneMode mode,
                List<ulong> completed,
                List<ulong> timedOut)
            {
                if (loadedName == sceneName)
                    loaded = true;
            }

            NetworkManager.Singleton.SceneManager.OnLoadEventCompleted += OnLoaded;
            NetworkManager.Singleton.SceneManager.LoadScene(
                sceneName,
                LoadSceneMode.Additive
            );

            while (!loaded)
                yield return null;

            NetworkManager.Singleton.SceneManager.OnLoadEventCompleted -= OnLoaded;

            destScene = SceneManager.GetSceneByName(sceneName);
        }

        // 2. Find spawn point inside that scene
        GameObject spawnPoint = null;

        if (destScene.IsValid() && destScene.isLoaded)
        {
            foreach (GameObject root in destScene.GetRootGameObjects())
            {
                foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
                {
                    if (t.name == spawnPointName)
                    {
                        spawnPoint = t.gameObject;
                        break;
                    }
                }
                if (spawnPoint != null) break;
            }
        }

        // Fallback
        if (spawnPoint == null)
            spawnPoint = GameObject.Find(spawnPointName);

        if (spawnPoint == null)
        {
            Debug.LogWarning(
                $"Spawn point not found: {spawnPointName} in scene {sceneName}"
            );
            yield break;
        }

        Vector3 pos = spawnPoint.transform.position;
        Quaternion rot = spawnPoint.transform.rotation;

        // 3. Teleport this player
        CharacterController cc = GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;

        transform.SetPositionAndRotation(pos, rot);

        if (cc != null) cc.enabled = true;

        TeleportOwnerClientRpc(pos, rot);

        // 4. Final victory message only when entering Village
        if (sceneName == "Village")
        {
            ShowVictoryMessageClientRpc();
        }
    }

    [ClientRpc]
    private void ShowVictoryMessageClientRpc()
    {
        if (!IsOwner) return;

        BriefingBox briefing =
            GetComponentInChildren<BriefingBox>(true);

        if (briefing != null)
        {
            briefing.PlayCustomMessage(
                "MISSION COMPLETE. YOU SAVED BONANZA. WELL DONE, SOLDIER.",
                5.0f
            );
        }
    }





    [ClientRpc]
    private void TeleportOwnerClientRpc(Vector3 position, Quaternion rotation)
    {
        if (!IsOwner) return;
        CharacterController cc = GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;
        transform.SetPositionAndRotation(position, rotation);
        if (cc != null) cc.enabled = true;
    }

    private bool groupTransitionRunning = false;

    public void RequestLevelClear()
    {
        if (IsOwner) RequestLevelClearServerRpc();
    }



    [ServerRpc]
    private void RequestLevelClearServerRpc(
    ServerRpcParams rpcParams = default)
    {
        if (groupTransitionRunning)
            return;

        ZombieSpawnController zombieController =
            ZombieSpawnController.Instance;

        if (zombieController == null)
        {
            Debug.LogWarning(
                "Level transition blocked: ZombieSpawnController not found."
            );

            return;
        }

        if (!zombieController.IsCurrentLevelComplete)
        {
            Debug.Log(
                "Level transition blocked: the current zombie waves are not complete."
            );

            return;
        }

        if (zombieController.currentWave < 2)
        {
            Debug.Log(
                $"Level transition blocked: current wave is " +
                $"{zombieController.currentWave}, final wave is 2."
            );

            return;
        }

        groupTransitionRunning = true;

        StartCoroutine(ServerCityToJungle());
    }


    private IEnumerator ServerCityToJungle()
    {
        Debug.Log("===== CITY TO JUNGLE STARTED =====");

        yield return StartCoroutine(
            LoadSceneServer("MazeLevel2")
        );

        yield return StartCoroutine(
            LoadSceneServer("Jungle")
        );

        Debug.Log("===== MAZE2 + JUNGLE LOADED =====");

        MovePlayersToSpawn(
            "Jungle",
            "SpawnFromCity"
        );

        yield return StartCoroutine(
            UnloadSceneServer("MazeLevel1")
        );

        yield return StartCoroutine(
            UnloadSceneServer("City")
        );

        yield return new WaitForSeconds(2.5f);

        StartJungleZombieSystem();

        groupTransitionRunning = false;

        Debug.Log(
            "===== CITY TO JUNGLE COMPLETE ====="
        );
    }
    private void StartCityZombieWaves()
    {
        if (!IsServer) return;
        GameObject spawnCenter = GameObject.Find("ZombieSpawnCenter_City");
        Vector3 center = spawnCenter != null ? spawnCenter.transform.position : Vector3.zero;
        if (ZombieSpawnController.Instance != null)
        {
            ZombieSpawnController.Instance.StartCityWaves(center);
            Debug.Log("City waves started from persistent controller");
        }
    }

    private void StartJungleZombieSystem()
    {
        if (!IsServer)
            return;

        Scene jungleScene =
            SceneManager.GetSceneByName("Jungle");

        if (!jungleScene.IsValid() ||
            !jungleScene.isLoaded)
        {
            Debug.LogError(
                "Cannot start Jungle waves because Jungle is not loaded."
            );

            return;
        }

        GameObject spawnCenter = null;

        foreach (GameObject rootObject
            in jungleScene.GetRootGameObjects())
        {
            Transform[] transforms =
                rootObject.GetComponentsInChildren<Transform>(
                    true
                );

            foreach (Transform t in transforms)
            {
                if (t.name == "ZombieSpawnCenter_Jungle")
                {
                    spawnCenter = t.gameObject;
                    break;
                }
            }

            if (spawnCenter != null)
                break;
        }

        if (spawnCenter == null)
        {
            Debug.LogError(
                "ZombieSpawnCenter_Jungle was not found in Jungle."
            );

            return;
        }

        if (ZombieSpawnController.Instance == null)
        {
            Debug.LogError(
                "ZombieSpawnController.Instance is null."
            );

            return;
        }

        Player player =
            GetComponent<Player>();

        WeaponManager weaponManager =
            GetComponentInChildren<WeaponManager>();

        if (weaponManager != null)
        {
            Debug.Log(
                $"JUNGLE START LOADOUT | " +
                $"AK74={weaponManager.totalAK74Ammo} | " +
                $"M249={weaponManager.totalM249Ammo} | " +
                $"M107={weaponManager.totalM107Ammo} | " +
                $"Grenades={weaponManager.lethalsCount} | " +
                $"Smoke={weaponManager.tacticalCount}"
            );
        }

        Debug.Log(
            "Starting normal Jungle zombie waves."
        );

        ZombieSpawnController.Instance.StartJungleWaves(
            spawnCenter.transform.position
        );
    }
    private IEnumerator ServerJungleToCity()
    {
        Debug.Log("===== JUNGLE TO CITY STARTED =====");
        yield return StartCoroutine(LoadSceneServer("MazeLevel1"));
        yield return StartCoroutine(LoadSceneServer("City"));
        Debug.Log("===== MAZE1 + CITY LOADED =====");
        MovePlayersToSpawn("City", "SpawnFromJungle");
        yield return StartCoroutine(UnloadSceneServer("MazeLevel2"));
        yield return StartCoroutine(UnloadSceneServer("Jungle"));
        groupTransitionRunning = false;
        Debug.Log("===== JUNGLE TO CITY COMPLETE =====");
    }

    public void RequestReturnToCity(string spawnPointName)
    {
        if (IsOwner) RequestReturnToCityServerRpc();
    }

    [ServerRpc]
    private void RequestReturnToCityServerRpc(ServerRpcParams rpcParams = default)
    {
        if (groupTransitionRunning) return;
        groupTransitionRunning = true;
        StartCoroutine(ServerJungleToCity());
    }

    private IEnumerator LoadSceneServer(string sceneName)
    {
        bool finished = false;
        void OnSceneLoaded(string loadedSceneName, LoadSceneMode mode, List<ulong> clientsCompleted, List<ulong> clientsTimedOut)
        {
            if (loadedSceneName == sceneName) finished = true;
        }
        NetworkManager.Singleton.SceneManager.OnLoadEventCompleted += OnSceneLoaded;
        NetworkManager.Singleton.SceneManager.LoadScene(sceneName, LoadSceneMode.Additive);
        while (!finished) yield return null;
        NetworkManager.Singleton.SceneManager.OnLoadEventCompleted -= OnSceneLoaded;
    }

    private IEnumerator UnloadSceneServer(string sceneName)
    {
        bool finished = false;
        void OnSceneUnloaded(string unloadedSceneName, LoadSceneMode mode, List<ulong> clientsCompleted, List<ulong> clientsTimedOut)
        {
            if (unloadedSceneName == sceneName) finished = true;
        }
        NetworkManager.Singleton.SceneManager.OnUnloadEventCompleted += OnSceneUnloaded;
        Scene scene = SceneManager.GetSceneByName(sceneName);
        if (scene.IsValid() && scene.isLoaded)
            NetworkManager.Singleton.SceneManager.UnloadScene(scene);
        else
            finished = true;
        while (!finished) yield return null;
        NetworkManager.Singleton.SceneManager.OnUnloadEventCompleted -= OnSceneUnloaded;
    }

    private void MovePlayersToSpawn(string sceneName, string spawnPointName)
    {
        Scene destinationScene = SceneManager.GetSceneByName(sceneName);
        if (!destinationScene.IsValid() || !destinationScene.isLoaded)
        {
            Debug.LogError($"GROUP SPAWN FAILED: destination scene '{sceneName}' is not loaded.");
            return;
        }

        GameObject spawnPoint = null;
        foreach (GameObject rootObject in destinationScene.GetRootGameObjects())
        {
            foreach (Transform t in rootObject.GetComponentsInChildren<Transform>(true))
            {
                if (t.name == spawnPointName) { spawnPoint = t.gameObject; break; }
            }
            if (spawnPoint != null) break;
        }

        if (spawnPoint == null)
        {
            Debug.LogError($"GROUP SPAWN FAILED: '{spawnPointName}' was not found inside scene '{sceneName}'.");
            return;
        }

        Vector3 destinationPosition = spawnPoint.transform.position;
        Quaternion destinationRotation = spawnPoint.transform.rotation;

        foreach (NetworkClient client in NetworkManager.Singleton.ConnectedClientsList)
        {
            if (client.PlayerObject == null) continue;
            PlayerMovement player = client.PlayerObject.GetComponent<PlayerMovement>();
            if (player == null) continue;
            CharacterController cc = player.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;
            player.transform.SetPositionAndRotation(destinationPosition, destinationRotation);
            if (cc != null) cc.enabled = true;
            player.TeleportPlayerClientRpc(destinationPosition, destinationRotation);
        }
    }

    [ClientRpc]
    private void TeleportPlayerClientRpc(Vector3 position, Quaternion rotation)
    {
        if (!IsOwner) return;
        CharacterController cc = GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;
        transform.SetPositionAndRotation(position, rotation);
        if (cc != null) cc.enabled = true;
    }

    public NetworkVariable<int> spawnIndex = new NetworkVariable<int>(-1);
    private static int nextSpawnIndex = 0;

    private void AssignSpawnIndexIfServer()
    {
        if (IsServer)
        {
            spawnIndex.Value = nextSpawnIndex;
            nextSpawnIndex = (nextSpawnIndex + 1) % 4;
        }
    }
    private void OnInitialMazeLevel1Loaded(
    string sceneName,
    LoadSceneMode mode,
    List<ulong> completed,
    List<ulong> timedOut)
    {
        if (sceneName != "MazeLevel1")
            return;

        NetworkManager.Singleton.SceneManager.OnLoadEventCompleted -=
            OnInitialMazeLevel1Loaded;

        Scene city =
            SceneManager.GetSceneByName("City");

        if (!city.IsValid() || !city.isLoaded)
        {
            NetworkManager.Singleton.SceneManager.OnLoadEventCompleted +=
                OnCityLoadedForWaves;

            NetworkManager.Singleton.SceneManager.LoadScene(
                "City",
                LoadSceneMode.Additive
            );
        }
        else
        {
            if (SaveManager.Instance != null &&
                SaveManager.Instance.IsLoadingSavedGame)
            {
                Debug.Log(
                    "Saved Game detected - skipping fresh City waves."
                );

                StartCoroutine(
                    StartSavedCityWavesWhenReady()
                );

                return;
            }

            StartCityZombieWaves();
        }
    }
    private void OnInitialMazeLevel2Loaded(
    string sceneName,
    LoadSceneMode mode,
    List<ulong> completed,
    List<ulong> timedOut)
    {
        if (sceneName != "MazeLevel2")
            return;

        NetworkManager.Singleton.SceneManager.OnLoadEventCompleted -=
            OnInitialMazeLevel2Loaded;

        if (SaveManager.Instance == null ||
            !SaveManager.Instance.IsLoadingSavedGame)
        {
            return;
        }

        Scene jungle =
            SceneManager.GetSceneByName("Jungle");

        if (!jungle.IsValid() || !jungle.isLoaded)
        {
            NetworkManager.Singleton.SceneManager.OnLoadEventCompleted +=
                OnJungleLoadedForSavedGame;

            NetworkManager.Singleton.SceneManager.LoadScene(
                "Jungle",
                LoadSceneMode.Additive
            );
        }
        else
        {
            StartCoroutine(
                StartSavedJungleWavesWhenReady()
            );
        }
    }

    private void OnCityLoadedForWaves(
    string sceneName,
    LoadSceneMode mode,
    List<ulong> completed,
    List<ulong> timedOut)
    {
        if (sceneName != "City")
            return;

        NetworkManager.Singleton.SceneManager.OnLoadEventCompleted -=
            OnCityLoadedForWaves;

        if (SaveManager.Instance != null &&
            SaveManager.Instance.IsLoadingSavedGame)
        {
            Debug.Log(
                "Saved Game detected - skipping fresh City waves."
            );

            StartCoroutine(
                StartSavedCityWavesWhenReady()
            );

            return;
        }

        StartCityZombieWaves();
    }
    private void OnJungleLoadedForSavedGame(
    string sceneName,
    LoadSceneMode mode,
    List<ulong> completed,
    List<ulong> timedOut)
    {
        if (sceneName != "Jungle")
            return;

        NetworkManager.Singleton.SceneManager.OnLoadEventCompleted -=
            OnJungleLoadedForSavedGame;

        if (SaveManager.Instance == null ||
            !SaveManager.Instance.IsLoadingSavedGame)
        {
            return;
        }

        StartCoroutine(
            StartSavedJungleWavesWhenReady()
        );
    }
    private IEnumerator StartSavedCityWavesWhenReady()
    {
        while (!savedGameSpawnReady)
        {
            yield return null;
        }

        if (!IsServer)
            yield break;

        if (savedWavesStarted)
            yield break;

        if (SaveManager.Instance == null ||
            SaveManager.Instance.LoadedSaveData == null)
        {
            Debug.LogWarning(
                "Saved City waves could not start because save data is missing."
            );

            yield break;
        }

        Scene cityScene =
            SceneManager.GetSceneByName("City");

        if (!cityScene.IsValid() || !cityScene.isLoaded)
        {
            Debug.LogWarning(
                "Saved City waves could not start because City is not loaded."
            );

            yield break;
        }

        GameObject spawnCenter =
            null;

        foreach (GameObject rootObject
            in cityScene.GetRootGameObjects())
        {
            Transform[] transforms =
                rootObject.GetComponentsInChildren<Transform>(true);

            foreach (Transform t in transforms)
            {
                if (t.name == "ZombieSpawnCenter_City")
                {
                    spawnCenter = t.gameObject;
                    break;
                }
            }

            if (spawnCenter != null)
                break;
        }

        if (spawnCenter == null)
        {
            Debug.LogError(
                "ZombieSpawnCenter_City was not found."
            );

            yield break;
        }

        savedWavesStarted = true;

        SaveData data =
            SaveManager.Instance.LoadedSaveData;

        Debug.Log(
            $"Starting saved City waves from completed Wave {data.completedWave}."
        );

        if (ZombieSpawnController.Instance == null)
        {
            Debug.LogError(
                "ZombieSpawnController.Instance is null."
            );

            yield break;
        }

        ZombieSpawnController.Instance.StartSavedGameWaves(
            spawnCenter.transform.position,
            data.completedWave
        );
    }
    private IEnumerator StartSavedJungleWavesWhenReady()
    {
        while (!savedGameSpawnReady)
        {
            yield return null;
        }

        if (!IsServer)
            yield break;

        if (savedWavesStarted)
            yield break;

        if (SaveManager.Instance == null ||
            SaveManager.Instance.LoadedSaveData == null)
        {
            Debug.LogWarning(
                "Saved Jungle waves could not start because save data is missing."
            );

            yield break;
        }

        Scene jungleScene =
            SceneManager.GetSceneByName("Jungle");

        if (!jungleScene.IsValid() || !jungleScene.isLoaded)
        {
            Debug.LogWarning(
                "Saved Jungle waves could not start because Jungle is not loaded."
            );

            yield break;
        }

        GameObject spawnCenter =
            null;

        foreach (GameObject rootObject
            in jungleScene.GetRootGameObjects())
        {
            Transform[] transforms =
                rootObject.GetComponentsInChildren<Transform>(true);

            foreach (Transform t in transforms)
            {
                if (t.name == "ZombieSpawnCenter_Jungle")
                {
                    spawnCenter = t.gameObject;
                    break;
                }
            }

            if (spawnCenter != null)
                break;
        }

        if (spawnCenter == null)
        {
            Debug.LogError(
                "ZombieSpawnCenter_Jungle was not found."
            );

            yield break;
        }

        savedWavesStarted = true;

        SaveData data =
            SaveManager.Instance.LoadedSaveData;

        Debug.Log(
            $"Starting saved Jungle waves from completed Wave {data.completedWave}."
        );

        if (ZombieSpawnController.Instance == null)
        {
            Debug.LogError(
                "ZombieSpawnController.Instance is null."
            );

            yield break;
        }

        Debug.Log(
    $"SAVED JUNGLE LOADOUT BEFORE WAVES | " +
    $"AK74={GetComponentInChildren<WeaponManager>().totalAK74Ammo} | " +
    $"M249={GetComponentInChildren<WeaponManager>().totalM249Ammo} | " +
    $"M107={GetComponentInChildren<WeaponManager>().totalM107Ammo} | " +
    $"Grenades={GetComponentInChildren<WeaponManager>().lethalsCount} | " +
    $"Smoke={GetComponentInChildren<WeaponManager>().tacticalCount}"
);

        ZombieSpawnController.Instance.StartSavedGameWaves(
            spawnCenter.transform.position,
            data.completedWave
        );
    }
    private IEnumerator WaitForInitialSpawnPoint()
    {
        while (spawnIndex.Value == -1)
        {
            yield return null;
        }

        // =========================================================
        // SAVED GAME SPAWN
        // =========================================================

        if (SaveManager.Instance != null &&
            SaveManager.Instance.IsLoadingSavedGame &&
            SaveManager.Instance.LoadedSaveData != null)
        {
            // -----------------------------------------------------
            // HIDE OWNER CAMERA
            // -----------------------------------------------------

            if (playerCamera != null)
            {
                playerCamera.enabled = false;

                AudioListener listener =
                    playerCamera.GetComponent<AudioListener>();

                if (listener != null)
                    listener.enabled = false;
            }

            // -----------------------------------------------------
            // HIDE GUN CANVAS
            // -----------------------------------------------------

            Transform gunCanvas =
                transform.Find("GunCanvas");

            if (gunCanvas != null)
            {
                gunCanvas.gameObject.SetActive(false);
            }

            // -----------------------------------------------------
            // HIDE WAVE UI
            // -----------------------------------------------------

            if (ZombieSpawnController.Instance != null)
            {
                ZombieSpawnController.Instance.HideUI();
            }

            string savedLevel =
                SaveManager.Instance.LoadedSaveData.levelName;

            string targetScene;
            string spawnName;

            if (savedLevel == "City")
            {
                targetScene = "City";
                spawnName = "SpawnFromMaze";
            }
            else if (savedLevel == "Jungle")
            {
                targetScene = "Jungle";
                spawnName = "SpawnFromCity";
            }
            else
            {
                Debug.LogWarning(
                    $"Unknown saved level '{savedLevel}'. " +
                    "Using normal spawn point."
                );

                goto NORMAL_SPAWN;
            }

            // -----------------------------------------------------
            // WAIT FOR DESTINATION SCENE
            // -----------------------------------------------------

            Scene targetSceneObject =
                SceneManager.GetSceneByName(targetScene);

            while (!targetSceneObject.IsValid() ||
                   !targetSceneObject.isLoaded)
            {
                targetSceneObject =
                    SceneManager.GetSceneByName(targetScene);

                yield return null;
            }

            // -----------------------------------------------------
            // FIND SAVED GAME SPAWN
            // -----------------------------------------------------

            GameObject savedSpawnPoint = null;

            foreach (
                GameObject rootObject
                in targetSceneObject.GetRootGameObjects()
            )
            {
                foreach (
                    Transform t
                    in rootObject.GetComponentsInChildren<Transform>(true)
                )
                {
                    if (t.name == spawnName)
                    {
                        savedSpawnPoint =
                            t.gameObject;

                        break;
                    }
                }

                if (savedSpawnPoint != null)
                    break;
            }

            if (savedSpawnPoint == null)
            {
                Debug.LogError(
                    $"Saved Game spawn point '{spawnName}' " +
                    $"was not found inside '{targetScene}'."
                );

                goto NORMAL_SPAWN;
            }

            // -----------------------------------------------------
            // TELEPORT PLAYER
            // -----------------------------------------------------

            CharacterController savedCC =
                GetComponent<CharacterController>();

            if (savedCC != null)
                savedCC.enabled = false;

            transform.SetPositionAndRotation( savedSpawnPoint.transform.position, savedSpawnPoint.transform.rotation);

            velocity = Vector3.zero;

            lastPosition =
                transform.position;

            // -----------------------------------------------------
            // HIDE WORLD ITEMS ALREADY PICKED IN SAVED GAME
            // -----------------------------------------------------

            WeaponManager savedItemManager =
                GetComponentInChildren<WeaponManager>();

            if (savedItemManager != null)
            {
                savedItemManager.HideSavedGamePickedItems();
            }

            if (savedCC != null)
                savedCC.enabled = true;
            // -----------------------------------------------------
            // RESTORE SAVED LOADOUT FIRST
            // -----------------------------------------------------

            WeaponManager savedWeaponManager =
                GetComponentInChildren<WeaponManager>();

            if (savedWeaponManager != null)
            {
                yield return StartCoroutine(
                    savedWeaponManager.RestoreSavedLoadout(
                        SaveManager.Instance.LoadedSaveData
                    )
                );
            }

            // -----------------------------------------------------
            // RESTORE SAVED HEALTH
            // -----------------------------------------------------

            Player savedPlayer =
                GetComponent<Player>();

            if (savedPlayer != null)
            {
                savedPlayer.HP =
                    SaveManager.Instance.LoadedSaveData.playerHP;

                if (savedPlayer.playerHealthUI != null)
                {
                    savedPlayer.playerHealthUI.text =
                        $"Health: {savedPlayer.HP}";
                }
            }

            // -----------------------------------------------------
            // SAVED GAME PLAYER IS NOW FULLY READY
            // -----------------------------------------------------

            savedGameSpawnReady = true;
            // -----------------------------------------------------
            // SHOW PLAYER CAMERA
            // -----------------------------------------------------

            if (playerCamera != null)
            {
                playerCamera.enabled = true;

                AudioListener listener =
                    playerCamera.GetComponent<AudioListener>();

                if (listener != null)
                    listener.enabled = true;
            }

            // -----------------------------------------------------
            // SHOW GUN CANVAS
            // -----------------------------------------------------

            if (gunCanvas != null)
            {
                gunCanvas.gameObject.SetActive(true);
            }

            // Wave UI stays hidden here.
            // It will only appear when a wave actually enters cooldown.

            Debug.Log(
                $"SAVED GAME SPAWN READY | " +
                $"Level: {savedLevel} | " +
                $"Scene: {targetScene} | " +
                $"Spawn: {spawnName}"
            );

            yield break;
        }


        // =========================================================
        // NORMAL NEW GAME SPAWN
        // =========================================================

    NORMAL_SPAWN:

        string normalSpawnName =
            "SpawnPoint_" +
            (spawnIndex.Value + 1);

        GameObject spawnPoint = null;

        while (spawnPoint == null)
        {
            spawnPoint =
                GameObject.Find(normalSpawnName);

            yield return null;
        }

        CharacterController cc =
            GetComponent<CharacterController>();

        if (cc != null)
            cc.enabled = false;

        transform.SetPositionAndRotation(
            spawnPoint.transform.position,
            spawnPoint.transform.rotation
        );

        velocity = Vector3.zero;

        lastPosition =
            transform.position;

        if (cc != null)
            cc.enabled = true;

        //wednesday, 9-23-2026: Added this to ensure that the player is fully ready after normal spawn.

        WeaponManager wm =
       GetComponentInChildren<WeaponManager>();

        if (wm != null)
        {
            StartCoroutine(wm.GiveStarterWeapon());
        }
    }


    public void CompleteLevelFromZombieWaves()
    {
        if (!IsServer)
            return;

        ZombieSpawnController zombieController =
            ZombieSpawnController.Instance;

        if (zombieController == null)
        {
            Debug.LogError(
                "ZombieSpawnController not found. " +
                "Cannot complete City waves."
            );

            return;
        }

        if (!zombieController.IsCurrentLevelComplete)
        {
            Debug.LogWarning(
                "City completion requested, " +
                "but the zombie waves are not complete."
            );

            return;
        }

        if (zombieController.currentWave < 2)
        {
            Debug.LogWarning(
                $"City completion requested too early. " +
                $"Current wave: {zombieController.currentWave}"
            );

            return;
        }

        Debug.Log(
            "CITY WAVES COMPLETE | " +
            "Player must use the Jungle portal to continue."
        );

      
    }
}
