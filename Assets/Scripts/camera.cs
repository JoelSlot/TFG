using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using System.Linq;
using UnityEditor.Search;
using System.Runtime.InteropServices.WindowsRuntime;

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
    public GameObject chamber;
    List<DigObject> DigObjects =  new List<DigObject>();
    private bool placingDigZone = false;
    private Vector3 digStartPoint;
    private Vector3 digEndPoint;
    public GameObject origDigPoint;

    

    float sphereDistance = 10f;
    float sphereScale = 1f;

    int pathId;

    public enum obj {None, Ant, Grub, digTunnel, digChamber, test}
    public GameObject origAnt; //Base ant that will be copied
    //public GameObject Grub; //Base grub that will be copied
    public obj objectMode = obj.None;
    public Ant SelectedAnt;

    static bool rotateAllowed = false;

    void OnEnable()
    {
        if (focusOnEnable && MainMenu.GameSettings.gameMode == 0) rotateAllowed = true;
    }

    void OnDisable()
    {
        if (rotateAllowed && MainMenu.GameSettings.gameMode == 0) rotateAllowed = false;
    }

    private void Start()
    {
        if (MainMenu.GameSettings.gameMode == 1) sphere.GetComponent<MeshRenderer>().enabled = false;
        else sphere.GetComponent<MeshRenderer>().enabled = true;
        origDigPoint.GetComponent<DigPoint>().WG = WG;
        pathId = Pheromone.getNextPathId();
    }

    void Update()
    {
        ReadInputs();

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

    void lockCursor(bool value)
    {
        Cursor.lockState = value ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !value;
    }

    void MapBuildingMode()
    {
        // Leave cursor lock
        if (Input.GetKeyDown(KeyCode.Escape) && rotateAllowed)
            rotateAllowed = false;
        if (Input.GetMouseButtonDown(0) && !rotateAllowed)
            rotateAllowed = true;
        lockCursor(rotateAllowed);


        //adjust sphere
        sphere.transform.position = Camera.main.transform.position + Camera.main.transform.forward * sphereDistance;
        if (sphere.transform.localScale.x != sphereScale)
        {
            sphere.transform.localScale = new Vector3(sphereScale, sphereScale, sphereScale);
        }

        if (Input.GetMouseButton(0)) terrainEditSphere(sphere.transform.position, sphereScale/2, -0.8f);
        else if (Input.GetMouseButton(1)) terrainEditSphere(sphere.transform.position, sphereScale/2, 0.8f);

        //Locks/unlocks cursor when pressing escape
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (Cursor.lockState == CursorLockMode.Locked)
                Cursor.lockState = CursorLockMode.None;
            else
                Cursor.lockState = CursorLockMode.Locked;
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


    void PlayingMode()
    {
        if (Input.GetMouseButton(1))
        {
            rotateAllowed = true;
            lockCursor(true);
        }
        else
        {
            rotateAllowed = false;
            if (placingDigZone)
            {
                lockCursor(true);
                TakePlacingInputs();
            } 
            else 
            {
                lockCursor(false);
            }
            if (Input.GetMouseButtonDown(0))
            {
                PlayingModeLeftClick();
            }
        }
    }

    bool clickObject(int layer, out RaycastHit hit)
    {
        //get mouse position with a set distance from screen
        Vector3 mousePos = Input.mousePosition;
        mousePos.z = 100f;
        //create a ray and raycast it
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out hit, 1000, layer))
        {
            return true;
        }
        //Debug.DrawRay(ray.origin, ray.direction, Color.white, 10000);
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
        if(Input.GetKey(KeyCode.Comma))             SelectedAnt.LetGo();

        //if (Input.GetKeyDown(KeyCode.DownArrow) && SelectedAnt.placedPheromone != null) SelectedAnt.placedPheromone.ShowPath(false);
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
    void PointsInSphere(Vector3 pos, float radius, out List<Tuple<Vector3Int,float>> points)
    {
        points = new List<Tuple<Vector3Int,float>>();

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
                        points.Add(new Tuple<Vector3Int,float>(new Vector3Int(Mathf.CeilToInt(x + pos.x), Mathf.CeilToInt(y + pos.y), Mathf.CeilToInt(z + pos.z)), (float)(1f - distPoint / radius)/10));
                    }
                }
            }
        }
    }


    public void terrainEditSphere(Vector3 pos, float radius, float degree){
        PointsInSphere(pos, radius, out List<Tuple<Vector3Int, float>> points);
        WG.EditTerrainAdd(points, degree);
    }

    
    public void toDigPoints()
    {
        Dictionary<Vector3Int, float> points = DigObjects.Last().pointsInDigObject();
        foreach(KeyValuePair<Vector3Int, float> entry in points)
        {
            if (DigPoint.digPointDict.ContainsKey(entry.Key)) //Si se encuentra en el diccionario, updatear
            {
                //Actualizamos el valor que se quiere obtener
                float maxDesiredVal = Mathf.Max(entry.Value, DigPoint.digPointDict[entry.Key].Item1);
                GameObject existingObject = DigPoint.digPointDict[entry.Key].Item2;
                DigPoint.digPointDict[entry.Key] = new Tuple<float, GameObject>(maxDesiredVal, existingObject);
            }
            else//si no se encuentra en el diccionario, añadir al diccionario. Si se encuentra adyacente a la superficie y no es parte de la pared lo instanciamos
            {
                GameObject digPoint = null;
                if (WorldGen.IsAboveSurface(entry.Key) && entry.Value < WorldGen.isolevel) //Instanciarlo si está en la superficie y no es pared (no deberia ser posible ambos pero por si acaso)
                {
                    digPoint = Instantiate(origDigPoint, entry.Key, Quaternion.identity);
                    digPoint.SetActive(true);
                    digPoint.GetComponent<DigPoint>().SetDesiredVal(entry.Value);
                }
                else if (entry.Value < WorldGen.isolevel)// Si no se encuentra sobre la superficie y no es pared miramos si algun adyacente si está en superficie
                {
                    Vector3Int[] directions = {Vector3Int.up, Vector3Int.down, Vector3Int.left, Vector3Int.right, Vector3Int.forward, Vector3Int.back};
                    foreach (Vector3Int direction in directions)
                    {
                        //Si algun punto del alrededor se encuentra sobre la superficie
                        if (WorldGen.IsAboveSurface(entry.Key + direction))
                        {
                            digPoint = Instantiate(origDigPoint, entry.Key, Quaternion.identity);
                            digPoint.SetActive(true);
                            digPoint.GetComponent<DigPoint>().SetDesiredVal(entry.Value);
                            break; //Salgo del loop para no mirar el resto de dirs
                        }
                    }
                }
                DigPoint.digPointDict.Add(entry.Key, new Tuple<float, GameObject>(entry.Value, digPoint));
            }
        }
    }

    private void digAllPoints(){
        List<Tuple<Vector3Int, float>> points = new List<Tuple<Vector3Int, float>>();
        foreach (var entry in DigPoint.digPointDict)
        {
            points.Add(new Tuple<Vector3Int, float>(entry.Key, entry.Value.Item1));
        }
        WG.EditTerrainSet(points);
    }


    private void ReadInputs()
    {
        
        //Mouse controls depending on game mode
        if (MainMenu.GameSettings.gameMode == 0)
            MapBuildingMode();
        else
            PlayingMode();
        //to return to main menu
        if (Input.GetKeyDown(KeyCode.Z))
        {
            rotateAllowed = false;
            lockCursor(false);
            SceneManager.LoadSceneAsync(0);

        }
        //Keys to load/save map
        if (Input.GetKeyDown(KeyCode.L))
            WG.LoadMap();
        if (Input.GetKeyDown(KeyCode.O))
            WG.SaveMap();
        //Move the camera
        CameraMovement();
        if (!placingDigZone)
        {
            if (Input.GetKeyDown(KeyCode.Alpha0)){objectMode = obj.None; Debug.Log("Modo none");} //cambiar modo a ninguno
            if (Input.GetKeyDown(KeyCode.Alpha1)){objectMode = obj.Ant; Debug.Log("Modo ant");} //cambiar modo a hormiga
            if (Input.GetKeyDown(KeyCode.Alpha3)){objectMode = obj.digTunnel; Debug.Log("Modo túnel");} //cambiar de modo a construir
            if (Input.GetKeyDown(KeyCode.Alpha4)){objectMode = obj.digChamber; Debug.Log("Modo chamber");} // cambiar de modo a construir 
            if (Input.GetKeyDown(KeyCode.Alpha5)){objectMode = obj.test; Debug.Log("Modo test");} // cambiar de modo a test 
        }
        if (Input.GetKeyDown(KeyCode.Alpha9)){digAllPoints();}
        if (Input.GetKeyDown(KeyCode.C) && SelectedAnt != null) //Cambiar la hormiga seleccionada a modo controlado y viceversa
        { 
            if (SelectedAnt.state != Ant.AIState.Controlled) SelectedAnt.state = Ant.AIState.Controlled;
            else
            {
                SelectedAnt.state = Ant.AIState.Passive;
                SelectedAnt.makingTrail = false;
            }
        }
        if (Input.GetKeyDown(KeyCode.P) && SelectedAnt != null)
        {
            if (SelectedAnt.state == Ant.AIState.Controlled && !SelectedAnt.makingTrail)
            {
                SelectedAnt.makingTrail = true;
                //SelectedAnt.placedPheromone = Pheromone.PlacePheromone(SelectedAnt.origPheromone, SelectedAnt.transform.position, SelectedAnt.transform.up, null);
            }
            else
            {
                SelectedAnt.makingTrail = false;
                //SelectedAnt.placedPheromone = null;
            }
        }
    }

    private Vector3Int relativeHorDir(Vector3 dir, out Vector3 left)
    {
        Vector3Int[] horDirs = {Vector3Int.right, Vector3Int.back, Vector3Int.left};
        float minAngle = Vector3.Angle(dir, Vector3Int.forward);
        Vector3Int shortest = Vector3Int.forward;
        Vector3Int prev = Vector3Int.left;
        left = prev;
        prev = Vector3Int.forward;
        foreach (var hor in horDirs)
        {
            if (Vector3.Angle(dir, hor) < minAngle)
            {
                left = prev;
                minAngle = Vector3.Angle(dir, hor);
                shortest = hor;
            }
            prev = hor;
        }
        return shortest;
    }

    private void TakePlacingInputs()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        DigObject placeDigObject = DigObjects.Last();

        
        //Move the digObject
        float mouseForward = Input.GetAxis("Mouse Y");
        float mouseSideways = Input.GetAxis("Mouse X");
        
        Vector3 dir = camera.transform.forward;
        dir.y = 0;
        Vector3 relFor = relativeHorDir(dir, out Vector3 relLeft);
        Debug.DrawRay(placeDigObject.transform.position, relFor*10, Color.red);
        Debug.DrawRay(placeDigObject.transform.position, relLeft*10, Color.blue);
        Vector3 movement = Vector3.zero;
        if (Mathf.Abs(mouseForward) > 0.1f) movement += relFor * mouseForward;
        if (Mathf.Abs(mouseSideways) > 0.1f) movement -= relLeft * mouseSideways;

        //resize the tunnel
        if (Input.mouseScrollDelta.y > 0)
            movement.y += 0.2f;
        if (Input.mouseScrollDelta.y < 0)
            movement.y -= 0.2f;
        //


        switch (placeDigObject.mode)
        {
            case DigObject.digType.Tunnel:
                if (Input.GetKey(KeyCode.LeftShift))
                {
                    placeDigObject.addPos(new Vector3(0, movement.x, 0));
                    placeDigObject.addRadius(movement.y);
                }
                else
                {
                    placeDigObject.addPos(movement);
                }
            break;
            case DigObject.digType.Chamber:
                if (Input.GetKey(KeyCode.LeftShift))
                    placeDigObject.addPos(movement);
                else
                    placeDigObject.addStartPos(movement);
            break;
        }
        

    }

    private void PlayingModeLeftClick()
    {
        int antLayer = (1 << 7); //capa de hormigas
        int terrainLayer = (1 << 6); //terrain layer
        if (objectMode == obj.None)
        {
            if (clickObject(antLayer, out RaycastHit hit))
            {
                if (SelectedAnt != null) if (SelectedAnt.state == Ant.AIState.Controlled && SelectedAnt != hit.transform.gameObject.GetComponent<Ant>()) SelectedAnt.state = Ant.AIState.Passive; //AL seleccionar una hormiga nueva, se deselecciona la actual cambiando su estado IA a pasivo si estaba siendo controlado
                SelectedAnt = hit.transform.gameObject.GetComponent<Ant>();
                Debug.Log("Selected an ant");
            }
        }
        else if (placingDigZone)
        {
            toDigPoints();
            placingDigZone = false;
        }
        else if (clickObject(terrainLayer, out RaycastHit hit))
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
                case obj.digChamber:
                    if (!placingDigZone)
                    {
                        placingDigZone = true;
                        digStartPoint = hit.point;
                        digEndPoint = hit.point;
                        GameObject digObj = Instantiate(origTunnel, digStartPoint, Quaternion.identity);
                        digObj.SetActive(true);
                        DigObject digObjScript = digObj.GetComponent<DigObject>();
                        digObjScript.setRadius(1);

                        if (objectMode == obj.digTunnel)
                        {
                            digObjScript.setMode(DigObject.digType.Tunnel);
                            digObjScript.setPos(digStartPoint, digEndPoint);
                        }
                        else
                        {
                            digObjScript.setMode(DigObject.digType.Chamber);
                            digObjScript.setPos(digStartPoint, digStartPoint + Vector3.one * 4 - Vector3.up);
                        }


                        
                        digObjScript.setActive(true);
                        setVertPlane();
                        DigObjects.Add(digObjScript);
                        Debug.Log("Set Plane Pos");
                    }
                    break;
                case obj.test:
                    Vector3Int cube = Vector3Int.FloorToInt(hit.point);
                    
                    if (SelectedAnt != null)
                    {
                        CubePaths.GetPathToPoint(SelectedAnt.lastSurface, cube, 100, out var path);
                        SelectedAnt.path = path;
                        SelectedAnt.objective = new(hit.point); 
                        SelectedAnt.state = Ant.AIState.FollowingPath;
                    }

                    CubePaths.DrawCube(cube, Color.red, 20);
                    
                    /*
                    bool[] cornerValues = CubePaths.CubeCornerValues(cube);

                    Vector3Int hitCorner = CubePaths.CornerFromNormal(hit.normal);
                    
                    bool[] groupCornerValues = CubePaths.GetGroup(hitCorner, cornerValues);

                    CubePaths.CubeSurface surface = new CubePaths.CubeSurface(cube, groupCornerValues);
                    List<CubePaths.CubeSurface> adyacentCubes = CubePaths.GetAdyacentCubes(surface, hit.normal);
                    foreach (var adyacentCube in adyacentCubes)
                    {
                        CubePaths.DrawCube(adyacentCube.pos, Color.red, 20);
                    }*/
                    /*
                    Vector3Int belowSurfaceCorner = CubePaths.CornerFromNormal(hit.normal);
                    CubePaths.cubeSurface cubeSurface = new CubePaths.cubeSurface(cube, belowSurfaceCorner);

                    List<CubePheromone> pheromoneList = CubePaths.GetPheromonesOnSurface(cubeSurface);

                    if (pheromoneList.Count == 0) Debug.Log("NO PHEROMONES");
                    else Debug.Log("Pheromones found");
                    */
                    break;
                default:
                    Debug.Log("No valid object mode when clicked");
                break;
            }
        }
    }

}

