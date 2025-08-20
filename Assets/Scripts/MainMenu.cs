using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    //Guarda los settings del juego
    public static class GameSettings
    {
        public static int gameMode = 0;
        //0: map builder
        //1: map tester
        public static bool newMap = false; //if true, means player starts new map in playing mode.
        public static string fileName = "flat";
    }

    public void StartMapBuilder()
    {
        GameSettings.gameMode = 0;
        SceneManager.LoadSceneAsync("Map");
    }

    public void StartMapTester()
    {
        GameSettings.gameMode = 1;
        GameSettings.fileName = "none"; // did not assign this bfore. Also i accidentally wrote None after.
        SceneManager.LoadSceneAsync("Map");
    }

    public void LoadMapTester()
    {
        GameSettings.gameMode = 1;
        GameSettings.fileName = "Encoded.xml";
        SceneManager.LoadSceneAsync("Map");
    }

    public void QuitGame()
    {
        Application.Quit();
    }



}
