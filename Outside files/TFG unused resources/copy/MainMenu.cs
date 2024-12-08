using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    //Guarda los settings del juego
    public static class GameSettings
    {
        public static int gameMode= 0;
            //0: map builder
            //1: map tester
    }

    public void StartMapBuilder()
    {
        GameSettings.gameMode = 0;
        SceneManager.LoadSceneAsync("MapBuilder");
    }

    public void StartMapTester()
    {
        GameSettings.gameMode = 1;
        SceneManager.LoadSceneAsync("MapBuilder");
    }

    public void QuitGame()
    {
        Application.Quit();
    }



}
