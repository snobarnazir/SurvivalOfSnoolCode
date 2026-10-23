using System.IO;
using UnityEngine;

public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance;

    private string saveFilePath;
    public bool IsLoadingSavedGame { get; set; }

    public SaveData LoadedSaveData { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        saveFilePath = Path.Combine(
            Application.persistentDataPath,
            "savegame.json"
        );
    }

    // =========================================================
    // SAVE
    // =========================================================

    public void SetLoadedSaveData(SaveData data)
    {
        LoadedSaveData = data;
        IsLoadingSavedGame = true;
    }
    public void SaveGame(SaveData data)
    {
        if (data == null)
        {
            Debug.LogWarning(
                "SaveManager: Cannot save null data."
            );

            return;
        }

        string json =
            JsonUtility.ToJson(
                data,
                true
            );

        //wednesday 12:00 PM
        /*try
        {
            File.WriteAllText(
                saveFilePath,
                json
            );

            // IMPORTANT:
            // Keep the in-memory save synchronized
            // with the file we just wrote.
            LoadedSaveData = data;

            Debug.Log(
                $"SaveManager: Game saved successfully. " +
                $"Level: {data.levelName} | " +
                $"Completed Wave: {data.completedWave}"
            );
        }*/
        try
        {
            File.WriteAllText(
                saveFilePath,
                json
            );

            // Keep in-memory save synchronized
            LoadedSaveData = data;

            Debug.Log(
                $"SaveManager: Game saved successfully. " +
                $"Level: {data.levelName} | " +
                $"Completed Wave: {data.completedWave}"
            );

        }
        catch (System.Exception e)
        {
            Debug.LogError(
                $"SaveManager: Failed to save game. {e.Message}"
            );
        }
    }

    // =========================================================
    // LOAD
    // =========================================================

    public SaveData LoadGame()
    {
        if (!File.Exists(saveFilePath))
        {
            Debug.Log("SaveManager: No save file found.");
            return null;
        }

        string json = File.ReadAllText(saveFilePath);

        SaveData data =
            JsonUtility.FromJson<SaveData>(json);

        Debug.Log(
            "SaveManager: Game loaded successfully."
        );

        return data;
    }

    // =========================================================
    // CHECK IF SAVE EXISTS
    // =========================================================

    public bool HasSaveGame()
    {
        return File.Exists(saveFilePath);
    }

    // =========================================================
    // DELETE SAVE
    // =========================================================

    public void DeleteSaveGame()
    {
        if (File.Exists(saveFilePath))
        {
            File.Delete(saveFilePath);

            Debug.Log(
                "SaveManager: Save game deleted."
            );
        }
    }

    // =========================================================
    // GET SAVE FILE PATH
    // =========================================================

    public string GetSaveFilePath()
    {
        return saveFilePath;
 
    }

    public void StartNewGame()
    {
        DeleteSaveGame();

        LoadedSaveData = null;
        IsLoadingSavedGame = false;

        Debug.Log("NEW GAME STARTED | Saved game cleared.");
    }
}