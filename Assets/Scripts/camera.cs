using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using static UnityEditor.PlayerSettings;
using pheromoneClass;
using NUnit.Framework.Constraints;

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

    int pathId;

    public enum obj { None, Ant, Grub}
    public GameObject Ant; //Base ant that will be copied
    public GameObject pheromoneNode; //Base pheromone that will be copied
    //public GameObject Grub; //Base grub that will be copied
    public obj objectMode = obj.None;
    public AntTest SelectedAnt;

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

        pathId = pheromoneNode.GetComponent<PheromoneNode>().getNextPathId();
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
        if (Input.GetKeyDown(KeyCode.Alpha0)){objectMode = obj.None; Debug.Log("Modo none");} //cambiar modo a ninguno
        if (Input.GetKeyDown(KeyCode.Alpha1)){objectMode = obj.Ant; Debug.Log("Modo ant");} //cambiar modo a hormiga
        if (Input.GetKeyDown(KeyCode.P) && SelectedAnt != null) //Cambiar la hormiga seleccionada a modo controlado y viceversa
        { 
            if (SelectedAnt.state != AntTest.AIState.Controlled) SelectedAnt.state = AntTest.AIState.Controlled;
            else SelectedAnt.state = AntTest.AIState.Passive;
        }
        
    }

    void FixedUpdate()
    {
        if (SelectedAnt != null) antInputs();
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
        //Add wasd movement
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

    int pathPos = 0;

    void PlayingMode()
    {
        if (Input.GetMouseButton(1))
            rotateAllowed = true;
        else
        {
            rotateAllowed = false;
            if (Input.GetMouseButtonDown(0))
            {
                if (objectMode == obj.None)
                {
                    int clickLayer = (1 << 7); //capa de hormigas
                    if (clickObject(clickLayer, out RaycastHit hit))
                    {
                        if (SelectedAnt != null) if (SelectedAnt.state == AntTest.AIState.Controlled && SelectedAnt != hit.transform.gameObject.GetComponent<AntTest>()) SelectedAnt.state = AntTest.AIState.Passive; //AL seleccionar una hormiga nueva, se deselecciona la actual cambiando su estado IA a pasivo si estaba siendo controlado
                        SelectedAnt = hit.transform.gameObject.GetComponent<AntTest>();
                    }
                }
                else 
                {
                    int clickLayer = (1 << 6); //terrain layer
                    if (clickObject(clickLayer, out RaycastHit hit))
                    {
                        switch (objectMode)
                        {
                            case obj.Ant:
                                if (SelectedAnt != null) if (SelectedAnt.state == AntTest.AIState.Controlled) SelectedAnt.state = AntTest.AIState.Passive; //AL crear una hormiga nueva, se deselecciona la actual cambiando su estado IA a pasivo si estaba siendo controlado
                                GameObject newAnt = Instantiate(Ant, hit.point, Quaternion.Euler(hit.normal)); 
                                newAnt.layer = 7;
                                newAnt.SetActive(true);
                                SelectedAnt = newAnt.GetComponent<AntTest>();
                                break;
                            case obj.Grub:
                                break;
                            default:
                                Debug.Log("No valid object mode when clicked");
                            break;
                        }
                    }
                }

            }
                
        }


    }


    bool clickObject(int layer, out RaycastHit hit)
    {
        //get mouse position with a set distance from screen
        Vector3 mousePos = Input.mousePosition;
        mousePos.z = 100f;
        mousePos = Camera.main.ScreenToWorldPoint(mousePos);
        //create a ray and raycast it
        //Debug.DrawRay(transform.position, mousePos - transform.position, Color.blue);
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out hit, 1000, layer))
        {
            return true;
        }
        return false;
    }

    //en esta funcion se miran todos los vertices del cubo que contienen el punto hit, por los que se iteran
    //usando los bits de un num de 3 bits
    Vector3Int NearestSurfacePoint(Vector3 hit)
    {
        //Mathf ceil or floor function
        int MCF(float value, int it, int bitPos)
        {
            if ((it & (1 << bitPos)) != 0)//Si el bit num bitPos es 1, devolvemos floor
                return Mathf.FloorToInt(value);
            else return Mathf.CeilToInt(value);//sino ceil
        }

        int FlipBit(int value, int bitPos)
        {
            return value ^ (1 << bitPos);
        }
        Vector3 nearest = hit;
        float distance = 10f; //no vertex can be further than 2, but 10 just in case

        //Miramos todos los v�rtices del cubo, y nos quedamos con el m�s cercano que cumple:
        // - Est� fuera del terreno, para que la hormiga pueda llegar
        // - Es adjacente a un punto dentro del terreno, para que no est� demasiado lejos de la superficie como para que no llegue la hormiga
        for (int i = 0; i < 8; i++)
        {
            Vector3Int cubeCorner = new Vector3Int(MCF(hit.x, i, 0), MCF(hit.y, i, 1), MCF(hit.z, i, 2));//obtener siguiente v�rtice del cubo que contiene el punto
            if (WorldGen.IsAboveSurface(cubeCorner))
                if (Vector3.Distance(hit, cubeCorner) < distance) //Si la distancia del v�rtice alpunto es menor que el escogido m�s cercano hasta ahora
                {
                    for (int j = 0; j < 3; j++)//miramos todos los v�rtices del cubo conectados al actual
                    {
                        int i2 = FlipBit(i, j);//Representaci�n binaria del siguiente v�rtice adjacente
                        Vector3 adjacentCorner = new Vector3(MCF(hit.x, i2, 0), MCF(hit.y, i2, 1), MCF(hit.z, i2, 2)); //siguiente v�rtice adjacente
                        if (!WorldGen.IsAboveSurface(new Vector3(MCF(hit.x, i2, 0), MCF(hit.y, i2, 1), MCF(hit.z, i2, 2)))) //Si alguno de los v�rtices est� debajo de la superficie podemos tomar este v�rtice
                        {
                            nearest = cubeCorner ;
                            distance = Vector3.Distance(hit, cubeCorner);
                        }

                    }
                }
        }
        return new Vector3Int(Mathf.FloorToInt(nearest.x), Mathf.FloorToInt(nearest.y), Mathf.FloorToInt(nearest.z));
    }


    void antInputs() {
        if (SelectedAnt.state != AntTest.AIState.Controlled) return;
        if (Input.GetKey(KeyCode.UpArrow))          SelectedAnt.SetWalking(true);
        else                                        SelectedAnt.SetWalking(false);
        if (Input.GetKey(KeyCode.LeftArrow))        SelectedAnt.TurnLeft();
        else if (Input.GetKey(KeyCode.RightArrow))  SelectedAnt.TurnRight();
        else                                        SelectedAnt.DontTurn();

        if (Input.GetKeyDown(KeyCode.DownArrow) && SelectedAnt.placedPheromone != null) SelectedAnt.placedPheromone.ShowPath(false);
    }


}


