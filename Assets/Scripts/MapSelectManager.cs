using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MapSelectManager : MonoBehaviour
{

    public TMP_Dropdown dropDown;
    public GameObject mapSelectPanel;
    public GameObject loadingScreen;
    List<string> nameList;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void OnEnable()
    {
        var defaultList = GetDefaultMaps();
        var customList = GetCustomMaps();
        dropDown.ClearOptions();
        nameList = new();

        foreach (var dir in defaultList)
        {
            nameList.Add(RemoveDefaultExcess(dir));
        }
        foreach (var dir in customList)
        {
            nameList.Add(RemoveCustomExcess(dir));
        }

        if (nameList.Count == 0)
            nameList.Add("No maps available");

        dropDown.AddOptions(nameList);
    }

    public void confirmButton()
    {
        if (dropDown.options.Count > 0)
        {
            MainMenu.GameSettings.fileName = nameList[dropDown.value];
            Debug.Log(MainMenu.GameSettings.fileName);
            MainMenu.GameSettings.newMap = true;
            MainMenu.GameSettings.flatMap = false;

            mapSelectPanel.SetActive(false);
            loadingScreen.SetActive(true);

            SceneManager.LoadSceneAsync("Map");

        }
        //else Debug.Log("no count");
    }

    private List<string> GetDefaultMaps()
    {
        return Directory.GetFiles("SaveData/Maps", "*.xml").ToList();
    }

    private string RemoveDefaultExcess(string map)
    {
        string trimmed = map.Remove(map.Length - 4);
        trimmed = trimmed.Remove(0, 14);
        return trimmed;
    }

    private List<string> GetCustomMaps()
    {
        return Directory.GetFiles("SaveData/CustomMaps", "*.xml").ToList();
    }

    private string RemoveCustomExcess(string map)
    {
        string trimmed = map.Remove(map.Length - 4);
        trimmed = trimmed.Remove(0, 20);
        return trimmed;
    }

}
