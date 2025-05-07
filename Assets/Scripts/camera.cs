using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using System.Linq;
using UnityEditor.Search;
using System.Runtime.InteropServices.WindowsRuntime;
using System.ComponentModel.Design;

[RequireComponent(typeof(Camera))]
public class FlyCamera : MonoBehaviour
{
    public float acceleration = 50; // how fast you accelerate
    public float accSprintMultiplier = 4; // how much faster you go when "sprinting"
    public float lookSensitivity = 1; // mouse look sensitivity
    public float dampingCoefficient = 5; // how quickly you break to a halt after you stop your input
    public bool focusOnEnable = true; // whether or not to focus and lock cursor immediately on enable
    public Camera camera; // Objeto cámara que se usa ingame



    Vector3 velocity; // current velocity

    public Component sphere;
    public WorldGen WG;

    //Variables para sistema de excavacion
   
    
    private bool placingDigZone = false;
    private Vector3 digStartPoint;
    private Vector3 digEndPoint;

    

    float sphereDistance = 10f;
    float sphereScale = 1f;


    public enum obj {None, Ant, Corn, digTunnel, digChamber, test}
    
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
    }

    void Update()
    {
        if (WorldGen.newCameraPosInfo)
        {
            transform.position = WorldGen.camera_pos.ToVector3();
            transform.eulerAngles = WorldGen.camera_euler.ToVector3();
            WorldGen.newCameraPosInfo = false;
        }

        ReadInputs();

        if (WorldGen.IsAboveSurface(transform.position))
        {
            camera.backgroundColor = new Color(0,191,255);
        }
        else camera.backgroundColor = Color.black;
    }

    void FixedUpdate()
    {
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

        if (Input.GetMouseButton(0)) terrainEditSphere(sphere.transform.position, sphereScale/2, -1);
        else if (Input.GetMouseButton(1)) terrainEditSphere(sphere.transform.position, sphereScale/2, 1);

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

    //GameObject.CreatePrimitive(PrimitiveType.Cilinder)
    void PointsInSphere(Vector3 pos, float radius, out List<Tuple<Vector3Int,int>> points)
    {
        points = new List<Tuple<Vector3Int,int>>();

        //check all points in the cube containing the sphere
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
                        points.Add(new Tuple<Vector3Int,int>(new Vector3Int(Mathf.CeilToInt(x + pos.x), Mathf.CeilToInt(y + pos.y), Mathf.CeilToInt(z + pos.z)),Mathf.Clamp(Mathf.RoundToInt((255 - (127.5f * distPoint)/radius)/15), 0, 255)));
                    }
                }
            }
        }
    }


    public void terrainEditSphere(Vector3 pos, float radius, int degree){
        PointsInSphere(pos, radius, out List<Tuple<Vector3Int, int>> points);
        WorldGen.EditTerrainAdd(points, degree);
    }

    
    public void toDigPoints()
    {
        Dictionary<Vector3Int, DigPoint.digPointData> points = Nest.NestParts.Last().pointsInDigObject();
        foreach(var entry in points)
        {
            var newDigPointData = entry.Value;
            var pos = entry.Key;
            //Si se encuentra en el diccionario, updatear
            if (DigPoint.digPointDict.ContainsKey(pos)) DigPoint.digPointDict[pos].update(newDigPointData);
            //si no se encuentra en el diccionario, añadir al diccionario. Si se encuentra adyacente a la superficie y no es parte de la pared lo instanciamos
            else
            {
                //Instanciarlo si está en la superficie y no es pared (no deberia ser posible ambos pero por si acaso)
                if (WorldGen.IsAboveSurface(pos) && newDigPointData.value < WorldGen.isolevel) newDigPointData.InstantiatePoint(pos);
                // Si no se encuentra sobre la superficie y no es pared miramos si algun adyacente si está en superficie
                else if (newDigPointData.value < WorldGen.isolevel)
                {
                    Vector3Int[] directions = {Vector3Int.up, Vector3Int.down, Vector3Int.left, Vector3Int.right, Vector3Int.forward, Vector3Int.back};
                    //Si algun punto del alrededor se encuentra sobre la superficie lo instanciamos
                    foreach (Vector3Int direction in directions)
                        if (WorldGen.IsAboveSurface(pos + direction)) newDigPointData.InstantiatePoint(pos);
                }
                DigPoint.digPointDict.Add(pos, newDigPointData);
            }
        }
    }

    private void digAllPoints(){
        List<Tuple<Vector3Int, int>> points = new();
        foreach (var entry in DigPoint.digPointDict)
        {
            points.Add(new Tuple<Vector3Int, int>(entry.Key, entry.Value.value));
        }
        WorldGen.EditTerrainSet(points);
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
        /*if (Input.GetKeyDown(KeyCode.L))
        {
            WG.LoadMap();
        }*/
        if (Input.GetKeyDown(KeyCode.O) && !placingDigZone) //Make sure to not save when placing.
        {
            WorldGen.camera_pos = new(transform.position);
            WorldGen.camera_euler = new(transform.eulerAngles);
            WG.SaveMap();
        }
        //Move the camera
        CameraMovement();
        if (!placingDigZone)
        {
            if (Input.GetKeyDown(KeyCode.Alpha0)){objectMode = obj.None; Debug.Log("Modo none");} //cambiar modo a ninguno
            if (Input.GetKeyDown(KeyCode.Alpha1)){objectMode = obj.Ant; Debug.Log("Modo ant");} //cambiar modo a hormiga
            if (Input.GetKeyDown(KeyCode.Alpha2)){objectMode = obj.Corn; Debug.Log("Modo corn");} //Cambiar al modo poner comida
            if (Input.GetKeyDown(KeyCode.Alpha3)){objectMode = obj.digTunnel; Debug.Log("Modo túnel");} //cambiar de modo a construir
            if (Input.GetKeyDown(KeyCode.Alpha4)){objectMode = obj.digChamber; Debug.Log("Modo chamber");} // cambiar de modo a construir 
            if (Input.GetKeyDown(KeyCode.Alpha5)){objectMode = obj.test; Debug.Log("Modo test");} // cambiar de modo a test 
        }
        if (Input.GetKeyDown(KeyCode.Alpha9)){digAllPoints();}
        if (Input.GetKeyDown(KeyCode.C) && SelectedAnt != null) //Cambiar la hormiga seleccionada a modo controlado y viceversa
        { 
            SelectedAnt.isControlled = !SelectedAnt.isControlled;
        }
        /*if (Input.GetKeyDown(KeyCode.P) && SelectedAnt != null)
        {
            if (SelectedAnt.isControlled && !SelectedAnt.makingTrail)
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
        */
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

        NestPart placeDigObject = Nest.NestParts.Last();

        
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
            case NestPart.NestPartType.Tunnel:
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
            case NestPart.NestPartType.FoodChamber:
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
                if (SelectedAnt != null) if (SelectedAnt.isControlled && SelectedAnt != hit.transform.gameObject.GetComponent<Ant>()) SelectedAnt.isControlled = false; //AL seleccionar una hormiga nueva, se deselecciona la actual cambiando su estado IA a pasivo si estaba siendo controlado
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
                    if (SelectedAnt != null) if (SelectedAnt.isControlled) SelectedAnt.isControlled = false; //AL crear una hormiga nueva, se deselecciona la actual cambiando su estado IA a pasivo si estaba siendo controlado
                    SelectedAnt = WorldGen.InstantiateAnt(hit.point, Quaternion.Euler(hit.normal));
                    break;
                case obj.Corn:
                        WorldGen.InstantiateCorn(hit.point + hit.normal.normalized*0.3f, Quaternion.Euler(hit.normal));
                    break;
                case obj.digTunnel:
                case obj.digChamber:
                    if (!placingDigZone)
                    {
                        placingDigZone = true;
                        digStartPoint = hit.point;
                        digEndPoint = hit.point;
                        NestPart nestPartScript = WorldGen.InstantiateNestPart(digStartPoint);
                        if (objectMode == obj.digTunnel)
                        {
                            nestPartScript.setMode(NestPart.NestPartType.Tunnel);
                            nestPartScript.setPos(digStartPoint, digEndPoint);
                        }
                        else
                        {
                            nestPartScript.setMode(NestPart.NestPartType.FoodChamber);
                            nestPartScript.setPos(digStartPoint, digStartPoint + Vector3.one * 4 - Vector3.up);
                        }
                    }
                    break;
                    
                case obj.test:
                    Vector3Int cube = Vector3Int.FloorToInt(hit.point);
                    
                    if (SelectedAnt != null)
                    {
                        //CubePaths.GetPathToPoint(SelectedAnt.lastSurface, cube, 100, out var path);
                        //SelectedAnt.path = path;
                        //SelectedAnt.objective = new(hit.point); 
                        //SelectedAnt.state = Ant.AIState.FollowingPath;
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

