using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Common;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using Polenter.Serialization;
using Polenter.Serialization.Core;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;


public class SaveManager : MonoBehaviour
{

    public WorldGen WG;

    public TextMeshProUGUI Save1Values;
    public TextMeshProUGUI Save2Values;
    public TextMeshProUGUI Save3Values;

    public TextMeshProUGUI[] SaveValues;

    public TextMeshProUGUI Save1Time;
    public TextMeshProUGUI Save2Time;
    public TextMeshProUGUI Save3Time;

    public TextMeshProUGUI[] SaveTime;

    public bool[] SaveExists = {false, false, false};


    public void Start()
    {

    }

    public void OnEnable()
    {
        SaveValues = new TextMeshProUGUI[] { Save1Values, Save2Values, Save3Values };
        SaveTime = new TextMeshProUGUI[] {Save1Time, Save2Time, Save3Time};
        for (int i = 1; i < 4; i++)
            UpdateSaveText(i);
    }

    public void UpdateSaveText(int slot)
    {
        slot = Mathf.Clamp(slot, 1, 3);

        if (DeserializeValues(slot, out SaveInfo info))
        {
            SaveExists[slot - 1] = true;
            SaveValues[slot - 1].SetText(info.mapName + "\n" + info.antCount + "\n" + info.cornCount);
            SaveTime[slot - 1].SetText("Time\n" + TimeString(info.playTime));
        }
        else SaveExists[slot - 1] = false;
    }

    public static string TimeString(float totalSeconds)
    {
        int hours = Mathf.FloorToInt(totalSeconds / 3600);
        int minutes = Mathf.FloorToInt(totalSeconds / 60) - hours * 60;
        int seconds = Mathf.FloorToInt(totalSeconds) - minutes * 60 - hours * 3600;

        string output = "";
        if (hours < 10) output = output + "0";
        output = output + hours + ":";
        if (minutes < 10) output = output + "0";
        output = output + minutes + ":";
        if (seconds < 10) output = output + "0";
        output = output + seconds;

        return output;
    }

    public bool DeserializeValues(int slot, out SaveInfo info)
    {
        slot = Mathf.Clamp(slot, 0, 3);
        try
        {
            var serializer = new SharpSerializer();
            info = (SaveInfo)serializer.Deserialize("SaveData/Save" + slot + "/info.xml");
            if (info == null) return false;
            return true;
        }
        catch (Exception e)
        {
            Debug.Log("No save file " + slot);
        }
        info = new();
        return false;
    }



    [Serializable]
    public class SaveInfo
    {
        public string mapName { get; set; }
        public int cornCount { get; set; }
        public int antCount { get; set; }
        public float playTime { get; set; }

        public SaveInfo()
        {

        }

        public static SaveInfo SaveGameInfo()
        {
            SaveInfo newInfo = new()
            {
                mapName = WorldGen.mapName,
                playTime = WorldGen.playTime,
                cornCount = Nest.GetCornCount(),
                antCount = Ant.antDictionary.Count
            };
            return newInfo;
        }

    }
}
