using UnityEngine;

public class Throwable : MonoBehaviour
{
    [SerializeField] float delay = 3f;
    [SerializeField] float damageRadius = 20f;
    [SerializeField] float explosionForce = 1200f;

    float countdown;
    bool hasExploded = false;
    public bool hasBeenThrown = false;

    [Header("Network Pickup")]
    [SerializeField]
    private string networkPickupId;

    public string NetworkPickupId => networkPickupId;

    public enum ThrowableType
    {
        None,
        Grenade,
        Smoke_Grenade
    }

    public ThrowableType throwableType;

    private void Start()
    {
        countdown = delay;
    }

    private void Update()
    {
        if (hasBeenThrown)
        {
            countdown -= Time.deltaTime;
            if (countdown <= 0f && !hasExploded)
            {
                Explode();
                hasExploded = true;
            }
        }
    }

    private void Explode()
    {
        GetThrowableEffect();
        Destroy(gameObject);
    }

    private void GetThrowableEffect()
    {
        switch (throwableType)
        {
            case ThrowableType.Grenade:
                GrenadeEffect();
                break;
            case ThrowableType.Smoke_Grenade:
                SmokeGrenadeEffect();
                break;
        }
    }

    private void SmokeGrenadeEffect()
    {
        if (GlobalReferences.Instance != null && GlobalReferences.Instance.smokeGrenadeEffect != null)
        {
            Instantiate(GlobalReferences.Instance.smokeGrenadeEffect, transform.position, transform.rotation);
        }

        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.throwableChannel1.PlayOneShot(SoundManager.Instance.grenadeSound);
        }

        Collider[] colliders = Physics.OverlapSphere(transform.position, damageRadius);

        foreach (Collider objectInRange in colliders)
        {
            Rigidbody rb = objectInRange.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.AddExplosionForce(explosionForce * 0.25f, transform.position, damageRadius);
            }

            // Zombies
            Enemy enemy = objectInRange.GetComponentInParent<Enemy>();
            if (enemy != null)
            {
                enemy.TakeDamage(30);
            }

            // Players (networked damage)
            Player player = objectInRange.GetComponentInParent<Player>();
            if (player != null && !player.isDead)
            {
                player.TakeDamage(10);
            }
        }
    }

    private void GrenadeEffect()
    {
        if (GlobalReferences.Instance != null && GlobalReferences.Instance.grenadeExplosionEffect != null)
        {
            Instantiate(GlobalReferences.Instance.grenadeExplosionEffect, transform.position, transform.rotation);
        }

        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.throwableChannel1.PlayOneShot(SoundManager.Instance.grenadeSound);
        }

        Collider[] colliders = Physics.OverlapSphere(transform.position, damageRadius);

        foreach (Collider objectInRange in colliders)
        {
            Rigidbody rb = objectInRange.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.AddExplosionForce(explosionForce, transform.position, damageRadius);
            }

            // Zombies - full damage
            Enemy enemy = objectInRange.GetComponentInParent<Enemy>();
            if (enemy != null)
            {
                enemy.TakeDamage(100);
            }

            // Players - small networked damage (host + client)
            Player player = objectInRange.GetComponentInParent<Player>();
            if (player != null && !player.isDead)
            {
                player.TakeDamage(20);
            }
        }
    }
}