using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Weapon;

public class SoundManager : MonoBehaviour
{

    public static SoundManager Instance { get; set; }

    public AudioSource ShootingSoundM1911;
    public AudioSource reloadingSoundM1911;
    public AudioSource emptyMagazineSoundM1911;

    public AudioSource ShootingSoundM249;
    public AudioSource reloadingSoundM249;

    public AudioSource throwableChannel1;
    public AudioClip  grenadeSound;

    public AudioClip zombieWalking;
    public AudioClip zombieChase;
    public AudioClip zombieAttack;
    public AudioClip zombieHurt;
    public AudioClip zombieDeath;

    public AudioSource zombieChannel;

    public AudioClip playerHurt;
    public AudioClip playerDeath;

    public AudioSource playerChannel;
    public AudioClip   gameOverMusic;




    /*private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }
    }*/

    //monday
    /*private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // NEW
        }
    }*/

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
    }

    // Add this Start method right below Awake:
    private void Start()
    {
        // Fetch saved Master Volume and apply it to all scene audio
        AudioListener.volume = PlayerPrefs.GetFloat("MasterVolume", 1f);
    }




    public void PlayShootingSound(WeaponModel weapon)
    {
      switch(weapon)
      {
          case WeaponModel.AK74:
              ShootingSoundM1911.Play();
              break;
          case WeaponModel.M249:
              // Play M249 shooting sound
              ShootingSoundM249.Play();
              break;

            case WeaponModel.M107:
                ShootingSoundM1911.Play();
                break;

        }
    }

    public void PlayReloadingSound(WeaponModel weapon)
    {
        switch (weapon)
        {
            case WeaponModel.AK74:
                reloadingSoundM1911.Play();
                break;
            case WeaponModel.M249:
                // Play Uzi reloading sound
                reloadingSoundM249.Play();
                break;
            case WeaponModel.M107:
                reloadingSoundM1911.Play();
                break;
        }
    }
}
