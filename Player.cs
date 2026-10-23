using System.Collections;
using UnityEngine;
using TMPro;
using Unity.Netcode;

public class Player : NetworkBehaviour
{



    // Old code can still use player.HP = 80; and player.HP
 

    [Header("Health")]
    public NetworkVariable<int> networkHP = new NetworkVariable<int>(
    100,
    NetworkVariableReadPermission.Everyone,
    NetworkVariableWritePermission.Server
);

    

    public GameObject bloodyScreen;
    public TextMeshProUGUI playerHealthUI;
    public GameObject gameOverUI;

    public bool isDead;



    public int HP
    {
        get => networkHP.Value;
        set
        {
            if (IsServer)
            {
                networkHP.Value = Mathf.Clamp(value, 0, 100);
            }
            else
            {
                // Client asks server to set HP (for save load etc.)
                SetHPServerRpc(value);
            }
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void SetHPServerRpc(int value)
    {
        networkHP.Value = Mathf.Clamp(value, 0, 100);
    }



    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        networkHP.OnValueChanged += OnHPChanged;

        if (IsOwner && playerHealthUI != null)
        {
            playerHealthUI.text = $"Health: {networkHP.Value}";
        }
    }

    public override void OnNetworkDespawn()
    {
        networkHP.OnValueChanged -= OnHPChanged;
        base.OnNetworkDespawn();
    }

    private void OnHPChanged(int oldHP, int newHP)
    {
        if (!IsOwner) return;

        if (playerHealthUI != null)
        {
            playerHealthUI.text = $"Health: {newHP}";
        }
    }

    // =========================================================
    // DAMAGE
    // =========================================================

    public void TakeDamage(int damageAmount)
    {
        if (isDead) return;

        if (IsServer)
        {
            ApplyDamage(damageAmount);
        }
        else
        {
            TakeDamageServerRpc(damageAmount);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void TakeDamageServerRpc(int damageAmount)
    {
        ApplyDamage(damageAmount);
    }

    private void ApplyDamage(int damageAmount)
    {
        if (!IsServer || isDead) return;

        int newHP = networkHP.Value - damageAmount;
        if (newHP < 0) newHP = 0;

        networkHP.Value = newHP;

        if (newHP <= 0)
        {
            isDead = true;
            PlayerDeadClientRpc();
        }
        else
        {
            PlayerHitClientRpc();
        }
    }

    [ClientRpc]
    private void PlayerHitClientRpc()
    {
        if (!IsOwner) return;

        StartCoroutine(BloodyScreenEffect());

        if (SoundManager.Instance != null && SoundManager.Instance.playerHurt != null)
        {
            SoundManager.Instance.playerChannel.PlayOneShot(SoundManager.Instance.playerHurt);
        }
    }

    [ClientRpc]
    private void PlayerDeadClientRpc()
    {
        isDead = true;
        PlayerDead();
    }

    private void PlayerDead()
    {
        if (IsOwner)
        {
            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.playerChannel.PlayOneShot(SoundManager.Instance.playerDeath);
                SoundManager.Instance.playerChannel.clip = SoundManager.Instance.gameOverMusic;
                SoundManager.Instance.playerChannel.PlayDelayed(2f);
            }

            StartCoroutine(ShowGameOverUI());

            MouseMovement mouse = GetComponent<MouseMovement>();
            if (mouse != null) mouse.enabled = false;

            PlayerMovement movement = GetComponent<PlayerMovement>();
            if (movement != null) movement.enabled = false;

            if (playerHealthUI != null)
                playerHealthUI.gameObject.SetActive(false);

            ScreenFader fader = GetComponent<ScreenFader>();
            if (fader != null) fader.StartFade();
        }

        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        foreach (Renderer r in renderers)
        {
            r.enabled = false;
        }

        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = false;

        if (IsServer)
        {
            StartCoroutine(RemovePlayerAfterDeath());
        }
    }

    private IEnumerator RemovePlayerAfterDeath()
    {
        yield return new WaitForSeconds(10f);

        NetworkObject netObj = GetComponent<NetworkObject>();
        if (netObj != null && netObj.IsSpawned)
        {
            netObj.Despawn(true);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private IEnumerator ShowGameOverUI()
    {
        yield return new WaitForSeconds(1f);

        if (gameOverUI != null)
            gameOverUI.SetActive(true);
    }

    private IEnumerator BloodyScreenEffect()
    {
        if (bloodyScreen != null && !bloodyScreen.activeInHierarchy)
        {
            bloodyScreen.SetActive(true);
        }

        yield return new WaitForSeconds(15f);

        if (bloodyScreen != null && bloodyScreen.activeInHierarchy)
        {
            bloodyScreen.SetActive(false);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (isDead) return;

        if (other.CompareTag("ZombieHand"))
        {
            ZombieHand hand = other.GetComponent<ZombieHand>();
            if (hand != null)
            {
                TakeDamage(hand.damage);
            }
        }
    }

    // =========================================================
    // HEAL
    // =========================================================

    public void HealToFull()
    {
        if (isDead) return;

        if (IsServer)
        {
            ApplyHealToFull();
        }
        else
        {
            HealToFullServerRpc();
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void HealToFullServerRpc()
    {
        ApplyHealToFull();
    }

    private void ApplyHealToFull()
    {
        if (!IsServer || isDead) return;

        networkHP.Value = 100;
    }

    public bool IsHealthFull()
    {
        return networkHP.Value >= 100;
    }
}