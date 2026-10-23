using UnityEngine;

public class Bullet : MonoBehaviour
{
    public int bulletDamage;

    // Set by Weapon.FireWeapon only — does not affect aim
    [HideInInspector] public Player ownerPlayer;
    [HideInInspector] public ulong ownerClientId = ulong.MaxValue;

    private void Start()
    {
        // Ignore physical collision with the shooter (does not change direction)
        if (ownerPlayer != null)
        {
            Collider[] bulletCols = GetComponentsInChildren<Collider>();
            Collider[] ownerCols = ownerPlayer.GetComponentsInChildren<Collider>();

            foreach (Collider b in bulletCols)
            {
                foreach (Collider o in ownerCols)
                {
                    if (b != null && o != null)
                        Physics.IgnoreCollision(b, o, true);
                }
            }
        }
    }

    private void OnCollisionEnter(Collision objectWeHit)
    {
        GameObject hitObject = objectWeHit.gameObject;

        // ===== PLAYER =====
        Player hitPlayer = hitObject.GetComponentInParent<Player>();
        if (hitPlayer != null)
        {
            // Do not damage the shooter
            if (IsOwner(hitPlayer))
            {
                return;
            }

            if (!hitPlayer.isDead)
            {
                hitPlayer.TakeDamage(bulletDamage);
            }

            CreateBloodSprayEffect(objectWeHit);
            Destroy(gameObject);
            return;
        }

        // ===== ENEMY =====
        if (hitObject.CompareTag("Enemy"))
        {
            Enemy enemy = hitObject.GetComponentInParent<Enemy>();
            if (enemy != null)
            {
                enemy.TakeDamageServerRpc(bulletDamage);
            }

            CreateBloodSprayEffect(objectWeHit);
            Destroy(gameObject);
            return;
        }

        // ===== TARGET =====
        if (hitObject.CompareTag("Target"))
        {
            CreateBulletImpactEffect(objectWeHit);
            Destroy(gameObject);
            return;
        }

        // ===== WALL =====
        if (hitObject.CompareTag("Wall"))
        {
            CreateBulletImpactEffect(objectWeHit);
            Destroy(gameObject);
            return;
        }
    }

    private bool IsOwner(Player hitPlayer)
    {
        if (hitPlayer == null)
            return false;

        if (ownerPlayer != null && hitPlayer == ownerPlayer)
            return true;

        if (ownerClientId != ulong.MaxValue &&
            hitPlayer.OwnerClientId == ownerClientId)
            return true;

        return false;
    }

    private void CreateBloodSprayEffect(Collision objectWeHit)
    {
        if (GlobalReferences.Instance == null ||
            GlobalReferences.Instance.bloodSprayEffect == null)
            return;

        ContactPoint contact = objectWeHit.contacts[0];

        GameObject bloodSprayPrefab = Instantiate(
            GlobalReferences.Instance.bloodSprayEffect,
            contact.point,
            Quaternion.LookRotation(contact.normal)
        );

        bloodSprayPrefab.transform.SetParent(objectWeHit.gameObject.transform);
    }

    private void CreateBulletImpactEffect(Collision objectWeHit)
    {
        if (GlobalReferences.Instance == null ||
            GlobalReferences.Instance.bulletImpactEffectPrefab == null)
            return;

        ContactPoint contact = objectWeHit.contacts[0];

        GameObject hole = Instantiate(
            GlobalReferences.Instance.bulletImpactEffectPrefab,
            contact.point,
            Quaternion.LookRotation(contact.normal)
        );

        hole.transform.SetParent(objectWeHit.gameObject.transform);
    }
}