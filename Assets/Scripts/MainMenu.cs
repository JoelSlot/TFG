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
    }

    public GameObject saveFilePanel;
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
                GameSettings.newMap = false;
                GameSettings.SaveSlot = slot;
                GameSettings.gameMode = 1;
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

}
