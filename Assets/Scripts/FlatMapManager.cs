using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class FlatMapManager : MonoBehaviour
{

    public UnityEngine.UI.Slider sliderX;
    public int valX;
    public TextMeshProUGUI textX;
    public UnityEngine.UI.Slider sliderY;
    public int valY;
    public TextMeshProUGUI textY;
    public UnityEngine.UI.Slider sliderZ;
    public int valZ;
    public TextMeshProUGUI textZ;
    public UnityEngine.UI.Slider sliderH;
    public int valH;
    public TextMeshProUGUI textH;

    public void sliderValueChange()
    {
        valX = (int)sliderX.value;
        textX.SetText(valX.ToString());
        valY = (int)sliderY.value;
        textY.SetText(valY.ToString());
        valZ = (int)sliderZ.value;
        textZ.SetText(valZ.ToString());

        sliderH.maxValue = valY * WorldGen.chunk_y_dim + 1;
        valH = Mathf.Clamp((int)sliderH.value, 0, (int)sliderH.maxValue);
        textH.SetText(valH.ToString());
    }

    public void confirmButton()
    {
        MainMenu.GameSettings.x_chunks = valX;
        MainMenu.GameSettings.y_chunks = valY;
        MainMenu.GameSettings.z_chunks = valZ;
        MainMenu.GameSettings.height = valH;
        MainMenu.GameSettings.flatMap = true;
        MainMenu.GameSettings.newMap = true;
        SceneManager.LoadSceneAsync("Map");
    }

    void OnEnable()
    {
        sliderValueChange();
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
