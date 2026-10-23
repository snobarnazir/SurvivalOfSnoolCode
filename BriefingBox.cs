using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Unity.Netcode;

public class BriefingBox : MonoBehaviour
{
    public TMP_Text briefingText;
    public AudioSource typeSound;

    [Header("Fast Typewriter Settings")]
    public float letterDelay = 0.03f;          // Fast, crisp character speed
    public float soundThrottleDelay = 0.06f;   // Minimum time between sound ticks
    public float maze1InitialDelay = 2.0f;     // 2s delay on spawn before sequence starts
    public float holdTime = 2.5f;             // Time message remains fully visible
    public float pauseBetweenMessages = 0.4f;  // Brief gap between panels

    public enum SequenceType { LevelMaze1, City, Jungle }

    [Header("Active Sequence Selection")]
    public SequenceType defaultSequence = SequenceType.LevelMaze1;

    [Header("1. Level Maze 1 Messages")]
    [TextArea(2, 4)]
    public string[] levelMaze1Messages = new string[]
    {
        "Hello. My name is RABONAS, your guide in this operation. The city of LARBEYA and jungle of NOORSA are under attack.",
        "Retrieve AMMO from the MAZE and bring it to the fight. Defeat the enemy and save the village of BONANZA.",
        "PRESS 'F' to pick up any item. You can carry TWO guns and TWO grenades of each type. PRESS '1' for slot one, '2' for slot two.",
        "PRESS 'G' to throw a GRENADE. PRESS 'T' to throw a SMOKE GRENADE."
    };

    [Header("2. City Messages")]
    [TextArea(2, 4)]
    public string[] cityMessages = new string[]
    {
        "ZOMBIES occupy the center of LARBEYA in TWO WAVES — the second DOUBLE in number.",
        "They can hear you, chase you, and attack you. One spotted may ALERT the others.",
        "Clear both waves, then proceed to NOORSA."
    };

    [Header("3. Jungle Messages")]
    [TextArea(2, 4)]
    public string[] jungleMessages = new string[]
    {
        "You have entered NOORSA.",
        "A second MAZE holds ammo here. Retrieve it as before.",
        "The same enemy awaits. Clear them to reach BONANZA."
    };
    private bool skipRequested = false;
    private float lastSoundTime = 0f;

    private static bool hasPlayedMaze1 = false;
    private static bool hasPlayedCity = false;
    private static bool hasPlayedJungle = false;

    public static void ResetAllBriefings()
    {
        hasPlayedMaze1 = false;
        hasPlayedCity = false;
        hasPlayedJungle = false;
    }

    


    //tuesday
    /* private void Start()
     {
         NetworkObject netObj = GetComponentInParent<NetworkObject>();
         if (netObj != null && !netObj.IsOwner)
         {
             gameObject.SetActive(false);
             return;
         }

         if (defaultSequence == SequenceType.LevelMaze1)
         {
             if (hasPlayedMaze1)
             {
                 gameObject.SetActive(false);
                 return;
             }
             PlaySequence(SequenceType.LevelMaze1, maze1InitialDelay);
         }
     }*/

    private void Start()
    {
        // ---------------------------------------------------------
        // SAVED GAME
        // ---------------------------------------------------------

        if (SaveManager.Instance != null &&
            SaveManager.Instance.IsLoadingSavedGame)
        {
            SaveData savedData =
                SaveManager.Instance.LoadedSaveData;

            if (savedData != null)
            {
                // -------------------------------------------------
                // SAVED AT CITY
                // MazeLevel1 and City briefings have already
                // been encountered.
                // -------------------------------------------------

                if (savedData.levelName == "City")
                {
                    hasPlayedMaze1 = true;
                    hasPlayedCity = true;
                }

                // -------------------------------------------------
                // SAVED AT JUNGLE
                // MazeLevel1, City and Jungle briefings have all
                // already been encountered.
                // -------------------------------------------------

                else if (savedData.levelName == "Jungle")
                {
                    hasPlayedMaze1 = true;
                    hasPlayedCity = true;
                    hasPlayedJungle = true;
                }
            }

            // Saved games never show a briefing on startup.
            gameObject.SetActive(false);
            return;
        }

        // ---------------------------------------------------------
        // NORMAL NETWORK OWNER CHECK
        // ---------------------------------------------------------

        NetworkObject netObj =
            GetComponentInParent<NetworkObject>();

        if (netObj != null &&
            !netObj.IsOwner)
        {
            gameObject.SetActive(false);
            return;
        }

        // ---------------------------------------------------------
        // NORMAL MAZE LEVEL 1 BRIEFING
        // ---------------------------------------------------------

        if (defaultSequence ==
            SequenceType.LevelMaze1)
        {
            if (hasPlayedMaze1)
            {
                gameObject.SetActive(false);
                return;
            }

            PlaySequence(
                SequenceType.LevelMaze1,
                maze1InitialDelay
            );
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Return) ||
            Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            skipRequested = true;
        }
    }

    public void PlaySequence(SequenceType sequence, float startDelay = 0f)
    {
        if (sequence == SequenceType.LevelMaze1 && hasPlayedMaze1) return;
        if (sequence == SequenceType.City && hasPlayedCity) return;
        if (sequence == SequenceType.Jungle && hasPlayedJungle) return;

        if (sequence == SequenceType.LevelMaze1) hasPlayedMaze1 = true;
        else if (sequence == SequenceType.City) hasPlayedCity = true;
        else if (sequence == SequenceType.Jungle) hasPlayedJungle = true;

        string[] targetMessages = levelMaze1Messages;
        if (sequence == SequenceType.City) targetMessages = cityMessages;
        else if (sequence == SequenceType.Jungle) targetMessages = jungleMessages;

        gameObject.SetActive(true);
        StopAllCoroutines();
        StartCoroutine(PlayMessageSequence(targetMessages, startDelay));
    }

    private IEnumerator PlayMessageSequence(string[] messages, float initialDelay)
    {
        if (initialDelay > 0f)
        {
            briefingText.text = "";
            yield return new WaitForSeconds(initialDelay);
        }

        foreach (string currentMsg in messages)
        {
            briefingText.text = "";
            skipRequested = false;

            foreach (char letter in currentMsg)
            {
                if (skipRequested) break;

                briefingText.text += letter;

                // Play sound blip only if enough time has passed since last tick
                if (typeSound != null && letter != ' ' && Time.time >= lastSoundTime + soundThrottleDelay)
                {
                    typeSound.PlayOneShot(typeSound.clip);
                    lastSoundTime = Time.time;
                }

                yield return new WaitForSeconds(letterDelay);
            }

            briefingText.text = currentMsg;

            // Reading hold phase (press space to instantly skip remaining hold time)
            float holdTimer = 0f;
            skipRequested = false;
            while (holdTimer < holdTime && !skipRequested)
            {
                holdTimer += Time.deltaTime;
                yield return null;
            }

            briefingText.text = "";
            yield return new WaitForSeconds(pauseBetweenMessages);
        }

        gameObject.SetActive(false);
    }

    public void PlayCustomMessage(string messageText, float displayTime = 3.0f)
    {
        gameObject.SetActive(true);
        StopAllCoroutines();
        StartCoroutine(PlaySingleMessage(messageText, displayTime));
    }

    private IEnumerator PlaySingleMessage(string textToDisplay, float displayTime)
    {
        briefingText.text = "";
        skipRequested = false;

        foreach (char letter in textToDisplay)
        {
            if (skipRequested) break;

            briefingText.text += letter;

            if (typeSound != null && letter != ' ' && Time.time >= lastSoundTime + soundThrottleDelay)
            {
                typeSound.PlayOneShot(typeSound.clip);
                lastSoundTime = Time.time;
            }

            yield return new WaitForSeconds(letterDelay);
        }

        briefingText.text = textToDisplay;

        float timer = 0f;
        skipRequested = false;
        while (timer < displayTime && !skipRequested)
        {
            timer += Time.deltaTime;
            yield return null;
        }

        briefingText.text = "";
        gameObject.SetActive(false);
    }


    

}