using System.Collections;
using UnityEngine;

public class Weapon : MonoBehaviour
{
    public bool isActiveWeapon;
    public int weaponDamage;

    [Header("Network Pickup")]
    [SerializeField]
    private string networkPickupId;


    //wednesday 9-23-2026
    public void SetNetworkPickupId(string id)
    {
        networkPickupId = id;
    }

    public string NetworkPickupId =>
        networkPickupId;

    public GameObject bulletPrefab;
    public Transform bulletSpawn;
    public float bulletVelocity = 100f;
    public float bulletPrefabLifeTime = 3f;

    public bool isShooting;
    public bool readyToShoot;

    private bool allowReset = true;

    public float shootingDelay = 2f;
    public int bulletsPerBurst = 3;
    public int burstBulletLeft;
    public float spreadIntensity;

    public GameObject muzzleEffect;

    internal Animator animator;

    public float reloadTime;
    public int magazineSize;
    public int bulletsLeft;
    public bool isReloading;

    public Vector3 spawnPosition;
    public Vector3 spawnRotation;

    private WeaponManager ownerWeaponManager;
    private Camera ownerCamera;

    public enum WeaponModel
    {
        AK74,
        M249,
        M107
    }

    public WeaponModel thisWeaponModel;

    public enum ShootingMode
    {
        Single,
        Burst,
        Auto
    }

    public ShootingMode currentShootingMode;


    //friday 9-25-2026
    [HideInInspector] public bool keepSavedMagazine = false;
    private void Awake()
    {
        readyToShoot = true;
        burstBulletLeft = bulletsPerBurst;

        animator = GetComponent<Animator>();

        //friday
        //bulletsLeft = magazineSize;
        if (!keepSavedMagazine)
        {
            bulletsLeft = magazineSize;
        }
    }

    public void SetOwner(
        WeaponManager manager,
        Camera playerCamera)
    {
        ownerWeaponManager = manager;
        ownerCamera = playerCamera;
    }

    private void Update()
    {
        if (!isActiveWeapon)
        {
            return;
        }

        if (ownerWeaponManager == null ||
            ownerCamera == null)
        {
            return;
        }

        // IMPORTANT:
        // Only the player who owns this WeaponManager
        // is allowed to process keyboard/mouse input.
        //
        // This prevents the host's gun and client's gun
        // from both firing when one player clicks.
        if (!ownerWeaponManager.IsOwner)
        {
            return;
        }

        if (ChatUI.IsChatOpen)
        {
            return;
        }

        if (bulletsLeft == 0 && isShooting)
        {
            if (SoundManager.Instance != null &&
                SoundManager.Instance.emptyMagazineSoundM1911 != null)
            {
                SoundManager.Instance
                    .emptyMagazineSoundM1911
                    .Play();
            }
        }

        if (currentShootingMode == ShootingMode.Auto)
        {
            isShooting =
                Input.GetKey(KeyCode.Mouse0);
        }
        else if (
            currentShootingMode == ShootingMode.Single ||
            currentShootingMode == ShootingMode.Burst)
        {
            isShooting =
                Input.GetKeyDown(KeyCode.Mouse0);
        }

        if (Input.GetKeyDown(KeyCode.R) &&
            bulletsLeft < magazineSize &&
            !isReloading &&
            ownerWeaponManager.CheckAmmoLeftFor(
                thisWeaponModel) > 0)
        {
            Reload();
        }

        if (readyToShoot &&
            !isShooting &&
            !isReloading &&
            bulletsLeft <= 0 &&
            ownerWeaponManager.CheckAmmoLeftFor(
                thisWeaponModel) > 0)
        {
            Reload();
        }

        if (readyToShoot &&
            isShooting &&
            bulletsLeft > 0)
        {
            burstBulletLeft = bulletsPerBurst;
            FireWeapon();
        }
    }

    private void FireWeapon()
    {
        bulletsLeft--;

        if (muzzleEffect != null)
        {
            ParticleSystem particle =
                muzzleEffect.GetComponent<ParticleSystem>();

            if (particle != null)
            {
                particle.Play();
            }
        }

        if (animator != null)
        {
            animator.SetTrigger("RECOIL");
        }

        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlayShootingSound(
                thisWeaponModel
            );
        }
        //monday
        Enemy.AlertZombiesInArea(transform.position, 30f);

        readyToShoot = false;

        Vector3 shootingDirection =
            CalculateDirectionAndSpread().normalized;

        if (bulletPrefab != null &&
            bulletSpawn != null)
        {
            GameObject bullet =
                Instantiate(
                    bulletPrefab,
                    bulletSpawn.position,
                    Quaternion.identity
                );

            Bullet bul =
                bullet.GetComponent<Bullet>();

            /*if (bul != null)
            {
                bul.bulletDamage =
                    weaponDamage;
            }*/

            if (bul != null)
            {
                bul.bulletDamage = weaponDamage;

                Player owner = GetComponentInParent<Player>();
                if (owner != null)
                {
                    bul.ownerPlayer = owner;
                    bul.ownerClientId = owner.OwnerClientId;
                }
            }

            bullet.transform.forward =
                shootingDirection;

            Rigidbody bulletRb =
                bullet.GetComponent<Rigidbody>();

            if (bulletRb != null)
            {
                bulletRb.useGravity = false;

                bulletRb.AddForce(
                    shootingDirection *
                    bulletVelocity,
                    ForceMode.Impulse
                );
            }

            StartCoroutine(
                DestroyBulletAfterTime(
                    bullet,
                    bulletPrefabLifeTime
                )
            );
        }

        if (allowReset)
        {
            Invoke(
                nameof(ResetShot),
                shootingDelay
            );

            allowReset = false;
        }

        if (currentShootingMode ==
                ShootingMode.Burst &&
            burstBulletLeft > 1)
        {
            burstBulletLeft--;

            Invoke(
                nameof(FireWeapon),
                shootingDelay
            );
        }


    }

    private void Reload()
    {
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlayReloadingSound(
                thisWeaponModel
            );
        }

        if (animator != null)
        {
            animator.SetTrigger("RELOAD");
        }

        isReloading = true;

        Invoke(
            nameof(ReloadCompleted),
            reloadTime
        );
    }

    private void ReloadCompleted()
    {
        if (ownerWeaponManager == null)
        {
            return;
        }

        int bulletsNeeded =
            magazineSize - bulletsLeft;

        int ammoAvailable =
            ownerWeaponManager.CheckAmmoLeftFor(
                thisWeaponModel
            );

        int bulletsToLoad =
            Mathf.Min(
                bulletsNeeded,
                ammoAvailable
            );

        bulletsLeft += bulletsToLoad;

        ownerWeaponManager.DecreaseTotalAmmo(
            bulletsToLoad,
            thisWeaponModel
        );

        isReloading = false;
    }

    private void ResetShot()
    {
        readyToShoot = true;
        allowReset = true;
    }

    public Vector3 CalculateDirectionAndSpread()
    {
        if (ownerCamera == null)
        {
            return transform.forward;
        }

        Ray ray =
            ownerCamera.ViewportPointToRay(
                new Vector3(
                    0.5f,
                    0.5f,
                    0f
                )
            );

        RaycastHit hit;

        Vector3 targetPoint;

        if (Physics.Raycast(
            ray,
            out hit))
        {
            targetPoint = hit.point;
        }
        else
        {
            targetPoint = ray.GetPoint(100);
        }

        Vector3 direction =
            targetPoint -
            bulletSpawn.position;

        float x =
            Random.Range(
                -spreadIntensity,
                spreadIntensity
            );

        float y =
            Random.Range(
                -spreadIntensity,
                spreadIntensity
            );

        return direction +
               new Vector3(x, y, 0);
    }

    private IEnumerator DestroyBulletAfterTime(
        GameObject bullet,
        float delay)
    {
        yield return new WaitForSeconds(delay);

        if (bullet != null)
        {
            Destroy(bullet);
        }
    }



}





