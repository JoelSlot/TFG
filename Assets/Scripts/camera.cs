using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using System.Linq;

[RequireComponent(typeof(Camera))]
public class FlyCamera : MonoBehaviour
{
    public float acceleration = 50; // how fast you accelerate
    public float accSprintMultiplier = 4; // how much faster you go when "sprinting"
    public float lookSensitivity = 1; // mouse look sensitivity
    public float dampingCoefficient = 5; // how quickly you break to a halt after you stop your input
    public bool focusOnEnable = true; // whether or not to focus and lock cursor immediately on enable
    public Camera camera; // Objeto cámara que se usa ingame


    public Plane projectPlane = new Plane(new Vector3(0,0,1), -10);


    Vector3 velocity; // current velocity

    public Component sphere;
    public WorldGen WG;

    //Variables para sistema de excavacion
    public GameObject  origTunnel;
    List<Tunnel> DigObjects =  new List<Tunnel>();
    private bool confirmDig = false;
    private Vector3 digStartPoint;
    private Vector3 digEndPoint;

    

    float sphereDistance = 10f;
    float sphereScale = 1f;

    int pathId;

    public enum obj {None, Ant, Grub, digTunnel}
    public GameObject origAnt; //Base ant that will be copied
    //public GameObject Grub; //Base grub that will be copied
    public obj objectMode = obj.None;
    public Ant SelectedAnt;

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

        pathId = Pheromone.getNextPathId();

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
        if (Input.GetKeyDown(KeyCode.Alpha0) && !confirmDig){objectMode = obj.None; Debug.Log("Modo none");} //cambiar modo a ninguno
        if (Input.GetKeyDown(KeyCode.Alpha1) && !confirmDig){objectMode = obj.Ant; Debug.Log("Modo ant");} //cambiar modo a hormiga
        if (Input.GetKeyDown(KeyCode.Alpha3) && !confirmDig){objectMode = obj.digTunnel; Debug.Log("Modo escavar");} //cambiar de modo a construir
        if (Input.GetKey(KeyCode.Alpha9) && DigObjects.Count != 0) terrainEditSphere(DigObjects.Last().nextPos(), 2.5f, -1);
        if (Input.GetKeyDown(KeyCode.C) && SelectedAnt != null) //Cambiar la hormiga seleccionada a modo controlado y viceversa
        { 
            if (SelectedAnt.state != Ant.AIState.Controlled) SelectedAnt.state = Ant.AIState.Controlled;
            else
            {
                SelectedAnt.state = Ant.AIState.Passive;
                SelectedAnt.makingTrail = false;
                SelectedAnt.placedPheromone = null;
            }
        }
        if (Input.GetKeyDown(KeyCode.P) && SelectedAnt != null)
        {
            if (SelectedAnt.state == Ant.AIState.Controlled && !SelectedAnt.makingTrail)
            {
                SelectedAnt.makingTrail = true;
                SelectedAnt.placedPheromone = Pheromone.PlacePheromone(SelectedAnt.origPheromone, SelectedAnt.transform.position, SelectedAnt.transform.up, null);
            }
            else
            {
                SelectedAnt.makingTrail = false;
                SelectedAnt.placedPheromone = null;
            }
        }
        if (WorldGen.IsAboveSurface(transform.position))
        {
            camera.backgroundColor = new Color(0,191,255);
        }
        else camera.backgroundColor = Color.black;
    }

    void FixedUpdate()
    {
        if (SelectedAnt != null) AntInputs();
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

        if (Input.GetMouseButton(0)) terrainEditSphere(sphere.transform.position, sphereScale/2, 0.8f);
        else if (Input.GetMouseButton(1)) terrainEditSphere(sphere.transform.position, sphereScale/2, -0.8f);

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
        AddMovement(KeyCode.X, Vector3.down);
        Vector3 direction = moveInput.normalized;
            
        if (Input.GetKey(KeyCode.LeftShift))
            return direction * (acceleration * accSprintMultiplier); // "sprinting"
        return direction * acceleration; // "walking"
    }

    int pathPos = 0;


    void PlayingMode()
    {
        
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (objectMode == obj.digTunnel && confirmDig)
        {
            if (Input.GetKey(KeyCode.LeftShift)) setHorPlane();
            else setVertPlane();

            Vector3 cross = Vector3.Cross(digEndPoint - transform.position, projectPlane.normal);

            if (projectPlane.Raycast(ray, out float distance))
            {
                digEndPoint = ray.GetPoint(distance);
                DigObjects.Last().setPos(digStartPoint, digEndPoint);
            }
        }


        if (Input.GetMouseButton(1))
        {
            rotateAllowed = true;
            /*if (confirmDig && !Input.GetKey(KeyCode.LeftShift))
            {
                setVertPlane();
            }*///Prob not necesary unless its too expensive to recreate plane obj every frame (whyyy would it be????)
        }
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
                        if (SelectedAnt != null) if (SelectedAnt.state == Ant.AIState.Controlled && SelectedAnt != hit.transform.gameObject.GetComponent<Ant>()) SelectedAnt.state = Ant.AIState.Passive; //AL seleccionar una hormiga nueva, se deselecciona la actual cambiando su estado IA a pasivo si estaba siendo controlado
                        SelectedAnt = hit.transform.gameObject.GetComponent<Ant>();
                    }
                }
                else if (objectMode == obj.digTunnel && confirmDig)
                {
                    confirmDig = false;
                }
                else 
                {
                    int clickLayer = (1 << 6); //terrain layer
                    if (clickObject(clickLayer, out RaycastHit hit))
                    {
                        switch (objectMode)
                        {
                            case obj.Ant:
                                if (SelectedAnt != null) if (SelectedAnt.state == Ant.AIState.Controlled) SelectedAnt.state = Ant.AIState.Passive; //AL crear una hormiga nueva, se deselecciona la actual cambiando su estado IA a pasivo si estaba siendo controlado
                                GameObject newAnt = Instantiate(origAnt, hit.point, Quaternion.Euler(hit.normal)); 
                                newAnt.layer = 7;
                                newAnt.SetActive(true);
                                SelectedAnt = newAnt.GetComponent<Ant>();
                                break;
                            case obj.Grub:
                                break;
                            case obj.digTunnel:
                                if (!confirmDig)
                                {
                                    confirmDig = true;
                                    digStartPoint = hit.point;
                                    digEndPoint = hit.point;
                                    GameObject tunnel = Instantiate(origTunnel, digStartPoint, Quaternion.identity);
                                    tunnel.SetActive(true);
                                    Tunnel tunnelScript = tunnel.GetComponent<Tunnel>();
                                    tunnelScript.setActive(true);
                                    tunnelScript.setPos(digStartPoint, digEndPoint);
                                    setVertPlane();
                                    DigObjects.Add(tunnelScript);
                                    Debug.Log("Set Plane Pos");
                                }
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


    void AntInputs() {
        if (SelectedAnt.state != Ant.AIState.Controlled) return;
        if (Input.GetKey(KeyCode.UpArrow))          SelectedAnt.SetWalking(true);
        else                                        SelectedAnt.SetWalking(false);
        if (Input.GetKey(KeyCode.LeftArrow))        SelectedAnt.TurnLeft();
        else if (Input.GetKey(KeyCode.RightArrow))  SelectedAnt.TurnRight();
        else                                        SelectedAnt.DontTurn();

        if (Input.GetKeyDown(KeyCode.DownArrow) && SelectedAnt.placedPheromone != null) SelectedAnt.placedPheromone.ShowPath(false);
    }

    void setVertPlane() //ok so the using planos is not something i came up with inmediately, and eventhen i was gonna use 3 and adjust them. thank goodness i noticed thats not possible and also that it would be simpler to have one that is updating all the time as the camera orientation changes
    {
        //ESTO HA TENIDO MUCHAS ITERACIONES; DEBIDO A LA COMPLEJIDAD MATEMÄTICA.  
        //UN PROBLEMA= CUANDO DIGENDPOINT Y DIGSTARTPOINT SE ALINEAN EN CUANTO A PERSPECTIVA DE LA CAMARA; EL PLANO NO RECIBE 3 PUNTOS LO SUFICIENTEMENTE DISTINTOS
        //Se crea el plano
        Vector3 camToStart = digStartPoint - transform.position;
        Vector3 camToEnd = digEndPoint - transform.position;
        Vector3 camToNearest = Vector3.Project(camToEnd, camToStart);
        Vector3 Nearest = transform.position + camToNearest;
        Debug.DrawRay(transform.position, camToStart, Color.red);
        Debug.DrawRay(transform.position, camToEnd, Color.blue);
        Debug.DrawLine(digEndPoint, Nearest, Color.green);
        Vector3 hor  = Vector3.Cross(camToStart, Vector3.up);
        projectPlane = new Plane(digEndPoint + hor, digEndPoint + Vector3.up*2, digEndPoint);
        Debug.DrawLine(digEndPoint + Vector3.up*2, digEndPoint, Color.magenta);
        Debug.DrawLine(digEndPoint + Vector3.up*2, digEndPoint + hor.normalized *2, Color.magenta);
        Debug.DrawLine(digEndPoint, digEndPoint + hor.normalized *2, Color.magenta);
    }

    void setHorPlane()
    {
        projectPlane = new Plane(digEndPoint, digEndPoint + Vector3.left, digEndPoint + Vector3.forward);
    }

    //GameObject.CreatePrimitive(PrimitiveType.Cilinder)
    void PointsInSphere(Vector3 pos, float radius, out List<Vector3Int> coords, out List<float> dist)
    {
        dist = new List<float>();
        coords = new List<Vector3Int>();

        int radiusCeil = Mathf.CeilToInt(radius);
        for (int x = -radiusCeil; x < radiusCeil; x++)
        {
            for (int y = -radiusCeil; y < radiusCeil; y++)
            {
                for (int z = -radiusCeil; z < radiusCeil; z++)
                {
                    Vector3 point = new Vector3(x, y, z);
                    float distPoint = point.magnitude;
                    if (distPoint <= radius)
                    { //changed it to do ciel with the x and pos added (shouldn't change anything actually, hmm)
                        coords.Add(new Vector3Int(Mathf.CeilToInt(x + pos.x), Mathf.CeilToInt(y + pos.y), Mathf.CeilToInt(z + pos.z)));
                        dist.Add((float)(1f - distPoint / radius)/10);
                    }
                }
            }
        }
    }


    public void terrainEditSphere(Vector3 pos, float radius, float degree){
        PointsInSphere(pos, radius, out List<Vector3Int> coords, out List<float> dist);
        WG.EditTerrain(coords, dist, degree);
    }
}


