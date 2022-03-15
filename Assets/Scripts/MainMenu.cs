using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    public void PlayTest()
    {
        SceneManager.LoadScene("LevelTest");
    }

    public void PlayLevel()
    {
        SceneManager.LoadScene("LevelPrototype");
    }

    public void QuitGame()
    {
        Debug.Log("Sei uscito :c");
        Application.Quit();
    }
}
