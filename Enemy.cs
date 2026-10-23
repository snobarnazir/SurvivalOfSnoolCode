using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Unity.Netcode;

public class Enemy : NetworkBehaviour
{
    [SerializeField] private int HP = 100;
    private Animator animator;
    private NavMeshAgent navAgent;

    [Header("Zombie Sound")]
    public float zombieSoundRange = 29f;

    [Header("Horde Alert Settings")]
    public float hordeAlertRadius = 45f;

    [Header("Network & State")]
    public int zombieId = -1;
    public bool isDead = false;

    //monday
    [Header("Desperation Settings")]
    public int maxHP = 100;
    [Range(1f, 3f)] public float maxDesperationMultiplier = 2.3f; // much stronger


    // === NETWORKED ALERT FLAG ===
    public NetworkVariable<bool> isAlerted = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    // =========================================================
    // START
    // =========================================================

    private void Start()
    {
        //monday
        maxHP = HP; // store original HP



        animator = GetComponent<Animator>();
        navAgent = GetComponent<NavMeshAgent>();

        // Only Server/Host drives NavMesh pathfinding
        if (!IsServer && navAgent != null)
        {
            navAgent.enabled = false;
        }
    }

    // =========================================================
    // CAN LOCAL PLAYER HEAR THIS ZOMBIE?
    // =========================================================

    public bool CanLocalPlayerHearZombie()
    {
        Camera mainCamera = Camera.main;

        if (mainCamera == null)
        {
            AudioListener listener = FindFirstObjectByType<AudioListener>();
            if (listener == null) return false;

            float distToListener = Vector3.Distance(transform.position, listener.transform.position);
            return distToListener <= zombieSoundRange;
        }

        float distance = Vector3.Distance(transform.position, mainCamera.transform.position);
        return distance <= zombieSoundRange;
    }

    // =========================================================
    // ZOMBIE AUDIO HELPERS
    // =========================================================

    public void PlayZombieDeathSound()
    {
        if (!CanLocalPlayerHearZombie()) return;

        if (SoundManager.Instance != null && SoundManager.Instance.zombieDeath != null)
        {
            AudioSource.PlayClipAtPoint(SoundManager.Instance.zombieDeath, transform.position, 1.0f);
        }
    }

    public void PlayZombieHurtSound()
    {
        if (!CanLocalPlayerHearZombie()) return;

        if (SoundManager.Instance != null && SoundManager.Instance.zombieHurt != null)
        {
            AudioSource.PlayClipAtPoint(SoundManager.Instance.zombieHurt, transform.position, 1.0f);
        }
    }

    // =========================================================
    // DAMAGE & NETWORK DESPAWN
    // =========================================================

    [ServerRpc(RequireOwnership = false)]
    public void TakeDamageServerRpc(int damageAmount)
    {
        TakeDamage(damageAmount);
    }

    public void TakeDamage(int damageAmount)
    {
        if (isDead) return;

        HP -= damageAmount;

        if (HP <= 0)
        {
            isDead = true;

            int randomValue = Random.Range(0, 2);
            if (animator != null)
            {
                animator.SetTrigger(randomValue == 0 ? "DIE1" : "DIE2");
            }

            PlayZombieDeathSound();

            if (navAgent != null)
                navAgent.enabled = false;

            if (IsServer)
            {
                StartCoroutine(DespawnZombieAfterDelay(3.0f));
            }
        }
        else
        {
            if (animator != null) animator.SetTrigger("DAMAGE");
            PlayZombieHurtSound();
        }
    }

    private IEnumerator DespawnZombieAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        NetworkObject netObj = GetComponent<NetworkObject>();
        if (netObj != null && netObj.IsSpawned)
            netObj.Despawn(true);
        else
            Destroy(gameObject);
    }



    //monaday
    // Returns 1.0 at full HP → up to maxDesperationMultiplier at near death
    public float GetDesperationMultiplier()
    {
        if (maxHP <= 0) return 1f;

        float hpPercent = Mathf.Clamp01((float)HP / maxHP);
        float multiplier = Mathf.Lerp(maxDesperationMultiplier, 1f, hpPercent);

        // Temporary debug - remove later
        if (IsServer && Time.frameCount % 60 == 0) // print every second
        {
            Debug.Log($"Zombie HP: {HP}/{maxHP} | Multiplier: {multiplier:F2}");
        }

        return multiplier;
    }



    // =========================================================
    // GUNSHOT ALERT SYSTEM
    // =========================================================

    public void OnHearGunshot(Vector3 gunshotPosition)
    {
        if (isDead || !IsServer) return;

        isAlerted.Value = true;

        if (animator != null)
        {
            animator.SetBool("isChasing", true);
            animator.SetBool("isPatroling", false);
            animator.SetBool("isAttacking", false);
        }
    }

    public static void AlertZombiesInArea(Vector3 soundOrigin, float hearDistance)
    {
        Enemy[] allEnemies = FindObjectsByType<Enemy>(FindObjectsSortMode.None);

        foreach (Enemy enemy in allEnemies)
        {
            if (enemy == null || !enemy.gameObject.activeInHierarchy || enemy.isDead)
                continue;

            float distance = Vector3.Distance(enemy.transform.position, soundOrigin);

            if (distance <= hearDistance)
            {
                enemy.OnHearGunshot(soundOrigin);
            }
        }
    }

    // =========================================================
    // NEAREST PLAYER
    // =========================================================

    public static Transform GetNearestPlayer(Vector3 fromPosition)
    {
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");

        Transform nearest = null;
        float nearestDistance = Mathf.Infinity;

        foreach (GameObject playerObj in players)
        {
            float distance = Vector3.Distance(fromPosition, playerObj.transform.position);
            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearest = playerObj.transform;
            }
        }

        return nearest;
    }

    // =========================================================
    // GIZMOS
    // =========================================================

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, 2.5f);          // Attack

        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, 18f);           // Detection

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, 21f);           // Stop chasing

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, 30f);           // Gunshot hear

        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, zombieSoundRange);

        Gizmos.color = new Color(1f, 0.5f, 0f, 0.75f);
        Gizmos.DrawWireSphere(transform.position, hordeAlertRadius); // Horde alert
    }

    // =========================================================
    // DIRECT HORDE ALERT SYSTEM (FIXED)
    // =========================================================

    public void ForceAlertToPlayer(Vector3 playerPosition)
    {
        if (isDead || !IsServer) return;

        // Set networked flag
        isAlerted.Value = true;

        if (animator != null)
        {
            animator.SetBool("isChasing", true);
            animator.SetBool("isPatroling", false);
            animator.SetBool("isAttacking", false);
        }

        if (navAgent != null && navAgent.enabled)
        {
            navAgent.SetDestination(playerPosition);
        }
    }

    // This is the method called from ChaseState
    public static void AlertNearbyZombies(Vector3 chaserPosition, Vector3 targetPlayerPosition, float alertRadius)
    {
        // Only the server should run this
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer)
            return;

        Enemy[] allEnemies = FindObjectsByType<Enemy>(FindObjectsSortMode.None);

        foreach (Enemy enemy in allEnemies)
        {
            if (enemy == null || !enemy.gameObject.activeInHierarchy || enemy.isDead)
                continue;

            // Skip the zombie that is already chasing (optional but cleaner)
            float distanceToChaser = Vector3.Distance(enemy.transform.position, chaserPosition);

            if (distanceToChaser <= alertRadius && distanceToChaser > 0.1f)
            {
                enemy.ForceAlertToPlayer(targetPlayerPosition);
            }
        }
    }
}