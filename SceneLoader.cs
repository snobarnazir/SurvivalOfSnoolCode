using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    public void GoToScene(string sceneName, string spawnPointName)
    {
        StartCoroutine(LoadAndSpawn(sceneName, spawnPointName));
    }

    private IEnumerator LoadAndSpawn(string sceneName, string spawnPointName)
    {
        AsyncOperation load = SceneManager.LoadSceneAsync(sceneName);

        while (!load.isDone)
        {
            yield return null;
        }

        GameObject spawnPoint = GameObject.Find(spawnPointName);
        if (spawnPoint != null)
        {
            CharacterController cc = GetComponent<CharacterController>();

            if (cc != null) cc.enabled = false; // NEW - disable before moving

            transform.position = spawnPoint.transform.position;
            transform.rotation = spawnPoint.transform.rotation;

            if (cc != null) cc.enabled = true; // NEW - re-enable after moving
        }
    }
}