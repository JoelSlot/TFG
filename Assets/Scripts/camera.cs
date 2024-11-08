using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

[RequireComponent(typeof(Camera))]
public class FlyCamera : MonoBehaviour
{
    public float acceleration = 50; // how fast you accelerate
    public float accSprintMultiplier = 4; // how much faster you go when "sprinting"
    public float lookSensitivity = 1; // mouse look sensitivity
    public float dampingCoefficient = 5; // how quickly you break to a halt after you stop your input
    public bool focusOnEnable = true; // whether or not to focus and lock cursor immediately on enable

    Vector3 velocity; // current velocity

    public Component sphere;
    public WorldGen WG;

    float sphereDistance = 10f;
    float sphereScale = 1f;

    static bool rotateAllowed
    {
        get => UnityEngine.Cursor.lockState == CursorLockMode.Locked;
        set
        {
            UnityEngine.Cursor.lockState = value ? CursorLockMode.Locked : CursorLockMode.None;
            UnityEngine.Cursor.visible = value == false;
        }
    }

    void OnEnable()
    {
        if (focusOnEnable) rotateAllowed = true;
    }

    void OnDisable()
    {
        rotateAllowed = false;
    }

    private void Start()
    {
        if (MainMenu.GameSettings.gameMode == 1) sphere.GetComponent<MeshRenderer>().enabled = false;
        else sphere.GetComponent<MeshRenderer>().enabled = true;
    }

    void Update()
    {

        //Mouse controls depending on game mode
        if (MainMenu.GameSettings.gameMode == 0)
            MapBuildingMode();
        else
            PlayingMode();

        //to return to main menu
        if (Input.GetKeyDown(KeyCode.Z))
            SceneManager.LoadSceneAsync(0);
        //Keys to load/save map
        if (Input.GetKeyDown(KeyCode.L))
            WG.LoadMap();
        if (Input.GetKeyDown(KeyCode.O))
            WG.SaveMap();
        //Move the camera
        CameraMovement();
        // Leave cursor lock
        if (Input.GetKeyDown(KeyCode.Escape))
            rotateAllowed = false;
        if (Input.GetMouseButtonDown(0))
            rotateAllowed = true;

        
    }

    void CameraMovement()
    {
        // Position
        velocity += GetAccelerationVector() * Time.deltaTime;

        // Rotation
        if (rotateAllowed)
        { 
            Vector2 mouseDelta = lookSensitivity * new Vector2(Input.GetAxis("Mouse X"), -Input.GetAxis("Mouse Y"));
            Quaternion rotation = transform.rotation;
            Quaternion horiz = Quaternion.AngleAxis(mouseDelta.x, Vector3.up);
            Quaternion vert = Quaternion.AngleAxis(mouseDelta.y, Vector3.right);
            transform.rotation = horiz * rotation * vert;
        }
        // Physics
        velocity = Vector3.Lerp(velocity, Vector3.zero, dampingCoefficient * Time.deltaTime);
        transform.position += velocity * Time.deltaTime;

        
    }

    void MapBuildingMode()
    {
        
        //adjust sphere
        sphere.transform.position = Camera.main.transform.position + Camera.main.transform.forward * sphereDistance;
        if (sphere.transform.localScale.x != sphereScale)
        {
            sphere.transform.localScale = new Vector3(sphereScale, sphereScale, sphereScale);
        }

        if ((Input.GetMouseButton(0) || Input.GetMouseButton(1)))
        {

            List<float> dist = new List<float>();
            List<Vector3Int> coords = new List<Vector3Int>();

            int radiusCeil = Mathf.CeilToInt(sphereScale / 2);
            for (int x = -radiusCeil; x < radiusCeil; x++)
            {
                for (int y = -radiusCeil; y < radiusCeil; y++)
                {
                    for (int z = -radiusCeil; z < radiusCeil; z++)
                    {
                        float distPoint = (x * x + y * y + z * z);
                        float distEdge = radiusCeil * radiusCeil;
                        if (distPoint <= distEdge)
                        {
                            coords.Add(new Vector3Int(x + Mathf.CeilToInt(sphere.transform.position.x), y + Mathf.CeilToInt(sphere.transform.position.y), z + Mathf.CeilToInt(sphere.transform.position.z)));
                            dist.Add((float)(1f - distPoint / distEdge)/10);
                        }
                    }
                }
            }
            if (Input.GetMouseButton(0))
                WG.EditTerrain(coords, dist, true);
            else
                WG.EditTerrain(coords, dist, false);

        }

        //Locks/unlocks cursor when pressing escape
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (UnityEngine.Cursor.lockState == CursorLockMode.Locked)
                UnityEngine.Cursor.lockState = CursorLockMode.None;
            else
                UnityEngine.Cursor.lockState = CursorLockMode.Locked;
        }

        if (Input.GetKey(KeyCode.Q))
            sphereDistance += 0.1f;
        if (Input.GetKey(KeyCode.E))
            sphereDistance -= 0.1f;

        if (Input.mouseScrollDelta.y > 0)
            sphereScale += 0.1f;
        if (Input.mouseScrollDelta.y < 0)
            sphereScale -= 0.1f;

        

        
    }

    Vector3 GetAccelerationVector()
    {
        Vector3 moveInput = default;
        //function to simplify code
        void AddMovement(KeyCode key, Vector3 dir)
        {
            if (Input.GetKey(key))
                moveInput += dir;
        }
        AddMovement(KeyCode.W, Vector3.forward);
        AddMovement(KeyCode.S, Vector3.back);
        AddMovement(KeyCode.D, Vector3.right);
        AddMovement(KeyCode.A, Vector3.left);
        //We want the horizontal movement to take into acount local direction but ignore y movement
        moveInput = transform.TransformVector(moveInput);
        moveInput.y = 0;
        //Up and down movement globally
        AddMovement(KeyCode.Space, Vector3.up);
        AddMovement(KeyCode.LeftControl, Vector3.down);
        Vector3 direction = moveInput.normalized;
            
        if (Input.GetKey(KeyCode.LeftShift))
            return direction * (acceleration * accSprintMultiplier); // "sprinting"
        return direction * acceleration; // "walking"
    }

    void PlayingMode()
    {

        if (Input.GetMouseButton(1))
            rotateAllowed = true;
        else rotateAllowed = false;

    }

}