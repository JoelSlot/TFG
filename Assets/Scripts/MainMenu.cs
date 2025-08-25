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
        //0: building map mode
        //1: playing map mode
        public static bool newMap = false; //if true, load a preset map
        public static int SaveSlot = 1; //the saveslot to load
        public static string fileName = "flat";

        //variables to be used when generating new world.
        public static bool flatMap = false;
        public static int x_chunks = 5;
        public static int y_chunks = 1;
        public static int z_chunks = 5;
        public static int height = 30;
    }

    public GameObject saveFilePanel;
    public GameObject LoadingScreen;
    public SaveManager saveInfoManager;


    public void LoadSaveFileMenuOpen()
    {
        saveFilePanel.SetActive(true);
    }

    public void HideSaveFileMenuClose()
    {
        saveFilePanel.SetActive(false);
    }

    public void LoadSlot(int slot)
    {
        if (slot > 0 && 4 > slot)
            if (saveInfoManager.SaveExists[slot - 1])
            {
                
                saveFilePanel.SetActive(false);
                LoadingScreen.SetActive(true);

                GameSettings.newMap = false;
                GameSettings.SaveSlot = slot;
                GameSettings.gameMode = 1;
                GameSettings.flatMap = false;


                SceneManager.LoadSceneAsync("Map");
            }

    }

    public void SetPlayingMode(int mode)
    {
        GameSettings.gameMode = Mathf.Clamp(mode, 0, 1);
    }

    public void QuitGame()
    {
        Application.Quit();
    }

    void Start()
    {
        System.IO.Directory.CreateDirectory("SaveData/CustomMaps");
        System.IO.Directory.CreateDirectory("SaveData/Maps");
        System.IO.Directory.CreateDirectory("SaveData/Save1");
        System.IO.Directory.CreateDirectory("SaveData/Save2");
        System.IO.Directory.CreateDirectory("SaveData/Save3");
    }

}
