using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;

public class MapSaveManager : MonoBehaviour
{
    public WorldGen WG;
    public TMP_InputField inputField;
    public string mapName = "Insert map name";
    public GameObject errorPanel;
    public TextMeshProUGUI errorPanelTextBox;

    public bool noMapName()
    {
        return mapName == "Insert map name" || mapName.Trim() == "";
    }

    void OnEnable()
    {
        inputField.SetTextWithoutNotify(mapName);
    }

    public void CancelButtonPress()
    {
        errorPanel.SetActive(false);
        this.gameObject.SetActive(false);
    }

    public void AcceptButtonPress()
    {
        if (errorPanel.activeInHierarchy) return;

        mapName = inputField.text;

        if (AntQueen.antQueenSet.Count != 1)
        {
            errorPanel.SetActive(true);
            errorPanelTextBox.SetText("Map must have an ant queen");
            return;
        }

        if (noMapName())
        {
            errorPanel.SetActive(true);
            errorPanelTextBox.SetText("Please insert a name for the map");
            return;
        }

        if (!WG.SaveMap(mapName))
        {
            errorPanel.SetActive(true);
            errorPanelTextBox.SetText("Invalid name");
            return;
        }

        
        errorPanel.SetActive(false);
        this.gameObject.SetActive(false);
    }

    public void ErrorPanelButton()
    {
        errorPanel.SetActive(false);
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
