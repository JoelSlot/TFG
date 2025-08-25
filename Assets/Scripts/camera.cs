using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using System.Linq;
using TMPro;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Unity.VisualScripting;

[RequireComponent(typeof(Camera))]
public class FlyCamera : MonoBehaviour
{
    public float acceleration = 50; // how fast you accelerate
    public float accSprintMultiplier = 4; // how much faster you go when "sprinting"
    public float lookSensitivity = 1; // mouse look sensitivity
    public float dampingCoefficient = 5; // how quickly you break to a halt after you stop your input
    public bool focusOnEnable = true; // whether or not to focus and lock cursor immediately on enable
    public Vector3 cameraAntVector = new(); //Distance camera has from ant
    public Camera camera; // Objeto cámara que se usa ingame

    public static bool cameraUnderground = false;
    public static bool cameraInNest = false;

    Vector3 velocity; // current velocity

    public WorldGen WG;

    //Variables para sistema de excavacion


    private bool placingDigZone = false;
    private Vector3 digStartPoint;
    private Vector3 digEndPoint;



    public Component sphere;
    public MeshRenderer sphereRenderer;
    float sphereDistance = 5f;
    float sphereScale = 2f;


    public enum obj { None, EditTerrain, Ant, AntQueen, CornPip, CornCob, Erase, digTunnel, digChamber, test }

    public obj objectMode = obj.None;
    public static Ant SelectedAnt = null;
    public static bool SelectedQueen = false;
    public EventSystem eventSystem;

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
        sphereRenderer.enabled = false;
        CornAmountPanel.SetActive(false);
        if (MainMenu.GameSettings.gameMode == 0) //mapbuildmode
        {
            MapEditPanel.SetActive(true);
            MapEditMenuPanel.SetActive(true);
            playingModePanel.SetActive(false);
        }
        else
        {
            MapEditPanel.SetActive(false);
            MapEditMenuPanel.SetActive(false);
            playingModePanel.SetActive(true);
        }
        
        Nest.NestVisible = false;
        applyNestMode();

        //activate/deactivate control button depending on selected ant status
        if (SelectedAnt == null && !SelectedQueen) ControlButton.SetActive(false);
        else if (SelectedAnt != null)
            ControlButton.SetActive(!SelectedAnt.IsControlled);
        else if (SelectedQueen)
            ControlButton.SetActive(!AntQueen.IsControlled);
        else ControlButton.SetActive(true);
    }

    private bool IsControllingAnt()
    {
        if (SelectedAnt != null)
            return SelectedAnt.IsControlled;
        if (SelectedQueen)
            return AntQueen.IsControlled;
        return false;
    }

    void Update()
    {
        if (WorldGen.updateCameraPos)
        {
            transform.position = WorldGen.camera_pos.ToVector3();
            transform.eulerAngles = WorldGen.camera_euler.ToVector3();
            WorldGen.updateCameraPos = false;
        }

        if (WorldGen.updateNestVisibility)
        {
            applyNestMode();
            WorldGen.updateNestVisibility = false;
        }

        if (WorldGen.updateAntCounter)
        {
            updateAntCounter();
            WorldGen.updateAntCounter = false;
        }

        if (WorldGen.updateCornCounter)
        {
            updateCornCounter();
            WorldGen.updateCornCounter = false;
        }

        ReadInputs();

        if (WorldGen.IsAboveSurface(transform.position))
        {
            cameraUnderground = false;
            camera.backgroundColor = new Color(0, 191, 255);
        }
        else
        {
            cameraUnderground = true;
            camera.backgroundColor = Color.black;
        }

        if (!WorldGen.WasAboveSurface(transform.position) && !cameraUnderground)
        {
            if (!cameraInNest && Nest.NestVisible)
                HideNest();
            cameraInNest = true;
        }
        else
        {
            if (cameraInNest && Nest.NestVisible)
                ShowNest();
            cameraInNest = false;
        }

    }

    void DefaultCameraMovement()
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

    void ControlCameraMovement()
    {
        GameObject ant = null;
        if (SelectedAnt != null)
        {
            if (SelectedAnt.IsControlled)
                ant = SelectedAnt.gameObject;
        }

        if (SelectedQueen)
        {
            if (AntQueen.IsControlled)
                ant = AntQueen.Queen.gameObject;
        }

        if (ant == null) return;

        
        Vector3 changedAntVector = cameraAntVector - cameraAntVector.normalized * Input.mouseScrollDelta.y * 0.2f;
        if (changedAntVector.magnitude < 1) changedAntVector = cameraAntVector.normalized;
        else if (changedAntVector.magnitude > 20) changedAntVector = cameraAntVector.normalized * 20;

        transform.position = ant.transform.position + changedAntVector;

        transform.RotateAround(ant.transform.position, Vector3.up, Input.GetAxis("Mouse X"));
        if (Input.GetAxis("Mouse Y") > 0)
            if (Vector3.Angle(Vector3.up, changedAntVector) > 5)
                transform.RotateAround(ant.transform.position, transform.right, -Input.GetAxis("Mouse Y"));
        if (Input.GetAxis("Mouse Y") < 0)
            if (Vector3.Angle(Vector3.down, changedAntVector) > 5)
                transform.RotateAround(ant.transform.position, transform.right, -Input.GetAxis("Mouse Y"));

        cameraAntVector = transform.position - ant.transform.position;

        transform.LookAt(ant.transform.position);

    }

    void lockCursor(bool value)
    {
        UnityEngine.Cursor.lockState = value ? CursorLockMode.Locked : CursorLockMode.None;
        UnityEngine.Cursor.visible = !value;
    }

    void MapBuildingMode()
    {

        //deactivates objectmode if pressing esc
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            objectMode = obj.None;
            ColorMapControlButtons();
        }

        //activates/deactivates camera movement depending on rightclick or terraineditmode
        if (objectMode == obj.EditTerrain)
            rotateAllowed = true;
        else if (Input.GetMouseButton(1))
            rotateAllowed = true;
        else rotateAllowed = false;

        lockCursor(rotateAllowed);

        SphereControls();

        //rightClick
        if (Input.GetMouseButtonDown(0) && objectMode != obj.EditTerrain)
        {
            MapEditModeLeftClick();
        }

    }

    public void SphereControls()
    {
        if (objectMode != obj.EditTerrain)
        {
            sphereRenderer.enabled = false;
            sphereDistance = 10f;
            sphereScale = 1f;
            return;
        }

        sphereRenderer.enabled = true;
        //adjust sphere
        sphere.transform.position = Camera.main.transform.position + Camera.main.transform.forward * sphereDistance;
        if (sphere.transform.localScale.x != sphereScale)
        {
            sphere.transform.localScale = new Vector3(sphereScale, sphereScale, sphereScale);
        }

        if (Input.GetMouseButton(0)) terrainEditSphere(sphere.transform.position, sphereScale / 2, -1);
        else if (Input.GetMouseButton(1)) terrainEditSphere(sphere.transform.position, sphereScale / 2, 1);

        int magnitude = 1;
        if (Input.GetKey(KeyCode.LeftShift))
            magnitude = 2;

        if (Input.GetKey(KeyCode.Q))
                sphereDistance += 0.1f * magnitude;
        if (Input.GetKey(KeyCode.E))
            sphereDistance -= 0.1f * magnitude;

        if (Input.mouseScrollDelta.y > 0)
            sphereScale += 0.1f * magnitude;
        if (Input.mouseScrollDelta.y < 0)
            sphereScale -= 0.1f * magnitude;

        if (sphereScale < 1) sphereScale = 1;
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

                if (!Input.GetMouseButton(1)) //ignore inputs when rotating camera
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

    void ControllingMode()
    {
        lockCursor(true);
        rotateAllowed = false;

        //from here on inputs
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (SelectedAnt != null)
                SelectedAnt.IsControlled = false;
            AntQueen.IsControlled = false;
            ControlButton.SetActive(true);
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
                            nearest = cubeCorner;
                            distance = Vector3.Distance(hit, cubeCorner);
                        }

                    }
                }
        }
        return new Vector3Int(Mathf.FloorToInt(nearest.x), Mathf.FloorToInt(nearest.y), Mathf.FloorToInt(nearest.z));
    }

    //GameObject.CreatePrimitive(PrimitiveType.Cilinder)
    void PointsInSphere(Vector3 pos, float radius, out List<Tuple<Vector3Int, int>> points)
    {
        points = new List<Tuple<Vector3Int, int>>();

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
                        points.Add(new Tuple<Vector3Int, int>(new Vector3Int(Mathf.CeilToInt(x + pos.x), Mathf.CeilToInt(y + pos.y), Mathf.CeilToInt(z + pos.z)), Mathf.Clamp(Mathf.RoundToInt((255 - (127.5f * distPoint) / radius) / 15), 0, 255)));
                    }
                }
            }
        }
    }

    //Edita todos los puntos dentro de la esfera dada
    public void terrainEditSphere(Vector3 pos, float radius, int degree)
    {
        //Obtiene los puntos dentro de la esfera y sus valores según su cercanía al centro
        PointsInSphere(pos, radius, out List<Tuple<Vector3Int, int>> points);
        //Aplica los cambios de valores a los puntos
        WorldGen.EditTerrainAdd(points, degree);
    }


    public void toDigPoints()
    {
        Dictionary<Vector3Int, DigPoint.digPointData> points = Nest.NestParts.Last().pointsInDigObject();
        foreach (var entry in points)
        {
            var newDigPointData = entry.Value;
            var pos = entry.Key;
            //Si se encuentra en el diccionario, updatear
            if (DigPoint.digPointDict.ContainsKey(pos)) DigPoint.digPointDict[pos].update(newDigPointData);
            //si no se encuentra en el diccionario, añadir al diccionario. Si se encuentra adyacente a la superficie y no es parte de la pared lo instanciamos
            else
            {
                //Instanciarlo si está en la superficie y no es pared (no deberia ser posible ambos pero por si acaso)
                if (WorldGen.IsAboveSurface(pos) && newDigPointData.value < WorldGen.isolevel) newDigPointData.InstantiatePoint(pos, false);
                // Si no se encuentra sobre la superficie y no es pared miramos si algun adyacente si está en superficie
                else if (newDigPointData.value < WorldGen.isolevel)
                {
                    Vector3Int[] directions = { Vector3Int.up, Vector3Int.down, Vector3Int.left, Vector3Int.right, Vector3Int.forward, Vector3Int.back };
                    //Si algun punto del alrededor se encuentra sobre la superficie lo instanciamos
                    foreach (Vector3Int direction in directions)
                        if (WorldGen.IsAboveSurface(pos + direction)) newDigPointData.InstantiatePoint(pos, false);
                }
                DigPoint.digPointDict.Add(pos, newDigPointData);
            }
        }
    }

    private void digAllPoints()
    {
        List<Vector3Int> keys = new List<Vector3Int>(DigPoint.digPointDict.Keys);
        foreach (var key in keys)
        {
            if (DigPoint.digPointDict.ContainsKey(key))
                if (DigPoint.digPointDict[key].digPoint != null)
                {
                    GameObject digPointObject = DigPoint.digPointDict[key].digPoint.gameObject;
                    DigPoint.digPointDict[key].digPoint.Dig();
                    DigPoint.digPointDict.Remove(Vector3Int.RoundToInt(key));
                    Destroy(digPointObject);
                }
        }
    }

    public void SelectAnt(Ant newAnt)
    {
        if (newAnt.Equals(SelectedAnt)) return;
        DeselectAnt();
        SelectedAnt = newAnt;
        SelectedAnt.outline.enabled = true;
        ControlButton.SetActive(true);
    }

    public void SelectQueen()
    {
        if (SelectedQueen) return;
        DeselectAnt();
        SelectedQueen = true;
        AntQueen.Queen.outline.enabled = true;
        ControlButton.SetActive(true);
    }

    public void DeselectAnt()
    {
        if (SelectedAnt != null)
        {
            SelectedAnt.outline.enabled = false;
            SelectedAnt.IsControlled = false;
        }
        if (SelectedQueen)
        {
            AntQueen.Queen.outline.enabled = false;
            AntQueen.IsControlled = false;
        }
        SelectedAnt = null;
        SelectedQueen = false;
        ControlButton.SetActive(false);
    }

    public void GoToMenu()
    {

        rotateAllowed = false;
        lockCursor(false);
        SceneManager.LoadSceneAsync(0);
    }

    private void ReadInputs()
    {
        //Mouse controls depending on game mode
        if (MainMenu.GameSettings.gameMode == 0)
        {
            if (!eventSystem.IsPointerOverGameObject())
                MapBuildingMode();
            //Move the camera
            DefaultCameraMovement();
        }
        else if (IsControllingAnt())
        {
            if (!eventSystem.IsPointerOverGameObject())
                ControllingMode();
            ControlCameraMovement();
        }
        else
        {
            if (!eventSystem.IsPointerOverGameObject())
                PlayingMode();
            //Move the camera
            DefaultCameraMovement();
        }

        //Keys to load/save map
        /*if (Input.GetKeyDown(KeyCode.L))
        {
            WG.LoadMap();
        }*/
        if (!placingDigZone)
        {
            if (Input.GetKeyDown(KeyCode.Alpha0)) { objectMode = obj.None; Debug.Log("Modo none"); } //cambiar modo a ninguno
            if (Input.GetKeyDown(KeyCode.Alpha1)) { objectMode = obj.Ant; Debug.Log("Modo ant"); } //cambiar modo a hormiga
            if (Input.GetKeyDown(KeyCode.Alpha2)) { objectMode = obj.AntQueen; Debug.Log("Modo antqueen"); } //cambiar modo a hormiga reina
            if (Input.GetKeyDown(KeyCode.Alpha3)) { objectMode = obj.CornCob; Debug.Log("Modo corn"); } //Cambiar al modo poner comida
            //if (Input.GetKeyDown(KeyCode.Alpha4)) { objectMode = obj.digTunnel; Debug.Log("Modo túnel"); } //cambiar de modo a construir
            //if (Input.GetKeyDown(KeyCode.Alpha5)) { objectMode = obj.digChamber; Debug.Log("Modo chamber"); } // cambiar de modo a construir 
            if (Input.GetKeyDown(KeyCode.Alpha6)) { objectMode = obj.test; Debug.Log("Modo test"); } // cambiar de modo a test 
            if (Input.GetKeyDown(KeyCode.Alpha4)) { AntQueen.Queen.GiveBirth(); }
        }
        if (Input.GetKeyDown(KeyCode.Alpha9)) { digAllPoints(); }
    }

    private Vector3Int relativeHorDir(Vector3 dir, out Vector3 left)
    {
        Vector3Int[] horDirs = { Vector3Int.right, Vector3Int.back, Vector3Int.left };
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
        Vector3 relFor = this.transform.forward;
        relFor.y = 0;
        Vector3 relRight = this.transform.right;
        relRight.y = 0;
        Vector3 movement = Vector3.zero;
        if (Mathf.Abs(mouseForward) > 0.1f) movement += relFor * mouseForward;
        if (Mathf.Abs(mouseSideways) > 0.1f) movement += relRight * mouseSideways;


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
                    placeDigObject.AddPos(new(movement.x, 0, movement.z));
                    placeDigObject.addRadius(movement.y);
                }
                else
                {
                    placeDigObject.AddPos(movement);
                }
                break;
            default:
                if (Input.GetKey(KeyCode.LeftShift))
                    placeDigObject.AddPos(movement);
                else
                    placeDigObject.AddStartPos(movement);
                break;
        }

    }
    public static Vector3 RandomOrthogonal(Vector3 input)
    {
        Vector3 output = UnityEngine.Random.onUnitSphere;
        Vector3.OrthoNormalize(ref input, ref output);
        return output;
    }

    private void MapEditModeLeftClick()
    {
        int antLayer = (1 << 7); //capa de hormigas
        int terrainLayer = (1 << 6); //terrain layer
        int cornCobLayer = (1 << 11);
        int cornLayer = 1 << 10;
        switch (objectMode)
        {
            case obj.Ant:
                if (clickObject(terrainLayer, out RaycastHit hit))
                {
                    WorldGen.InstantiateAnt(hit.point, Quaternion.LookRotation(RandomOrthogonal(hit.normal), hit.normal), true);
                }
                break;
            case obj.AntQueen:
                if (AntQueen.Queen == null)
                {
                    if (clickObject(terrainLayer, out hit))
                        WorldGen.InstantiateQueen(hit.point, Quaternion.LookRotation(RandomOrthogonal(hit.normal), hit.normal));
                }
                //Else message
                break;
            case obj.CornCob:
                if (clickObject(terrainLayer, out hit))
                {
                    var cobScript = WorldGen.InstantiateCornCob(hit.point + hit.normal.normalized * 3f, Quaternion.Euler(new Vector3(90, 0, 0)), (int)CornAmountSlider.value);
                    //cobScript.Hide();
                }
                break;
            case obj.CornPip:
                if (clickObject(terrainLayer, out hit))
                {
                    var cobScript = WorldGen.InstantiateCorn(hit.point + hit.normal.normalized * 3f, Quaternion.Euler(new Vector3(90, 0, 0)));
                    //cobScript.Hide();
                }
                break;
            case obj.Erase:
                GameObject destroyable = null;
                if (clickObject(antLayer, out hit))
                    destroyable = hit.transform.gameObject;
                else if (clickObject(cornCobLayer, out hit))
                    destroyable = hit.transform.gameObject;
                else if (clickObject(cornLayer, out hit))
                    destroyable = hit.transform.gameObject;

                //get most parent object.
                if (destroyable != null)
                    while (destroyable.transform.parent != null)
                        destroyable = destroyable.transform.parent.gameObject;

                Destroy(destroyable);
                break;
        }
    }

    private void PlayingModeLeftClick()
    {
        int antLayer = (1 << 7); //capa de hormigas
        int terrainLayer = (1 << 6); //terrain layer
        int nestLayer = (1 << 12); //capa de las partes del nido
        if (placingDigZone)
        {
            //Si se esta pulsando tmbien shift, eliminar la parte que se está colocando.
            if (Input.GetKey(KeyCode.LeftShift) || !Nest.NestParts.Last().IsValidPosition())
            {
                NestPart ErasePart = Nest.NestParts.Last();
                Nest.NestParts.Remove(ErasePart);
                Destroy(ErasePart.gameObject);
                Destroy(ErasePart);
            }
            else
            {
                toDigPoints();
                Nest.NestParts.Last().setKinematic(true);
                UpdateNestPartCountText();

                //Make the placed nestpart invisible if it's type's visibility is disabled.
                if (Nest.NestPartDisabled[NestPart.NestPartTypeToIndex(Nest.NestParts.Last().mode)])
                    Nest.NestParts.Last().Hide();
            }
            placingDigZone = false;
            placingTypeIndex = -1;
            objectMode = obj.None;
            ColorNestPlacingButtons();

        }
        else
        {
            switch (objectMode)
            {
                case obj.None:
                    if (Nest.NestVisible && clickObject(nestLayer, out RaycastHit hit)) //si en modo vision de nido buscar nido
                    {
                        DeselectAnt();


                        NestPart hitPart = hit.transform.gameObject.GetComponent<NestPart>();
                        if (hitPart.mode != NestPart.NestPartType.Tunnel && hitPart != selectedNestPart)
                        {
                            selectedNestPart = hitPart;
                            dropDownMenuObj.SetActive(true);
                            typeMenu.value = NestPart.NestPartTypeToIndex(hitPart.mode) - 1;
                            hitPart.Show();
                        }
                        else
                        {
                            if (selectedNestPart != null)
                                if (Nest.NestPartDisabled[NestPart.NestPartTypeToIndex(selectedNestPart.mode)])
                                    selectedNestPart.Hide();
                            selectedNestPart = null;
                            dropDownMenuObj.SetActive(false);
                        }

                    }
                    else
                    {
                        if (selectedNestPart != null)
                            if (Nest.NestPartDisabled[NestPart.NestPartTypeToIndex(selectedNestPart.mode)])
                                selectedNestPart.Hide();
                        selectedNestPart = null;
                        dropDownMenuObj.SetActive(false);

                        if (clickObject(antLayer, out hit))
                        {
                            DeselectAnt();
                            if (hit.transform.gameObject.TryGetComponent<Ant>(out Ant ant))
                                SelectAnt(ant);
                            else if (hit.transform.gameObject.TryGetComponent<AntQueen>(out AntQueen queen))
                                SelectQueen();
                            //Debug.Log("Selected an ant");
                        }
                        else
                            DeselectAnt();
                    }
                    break;
                case obj.digTunnel:
                case obj.digChamber:
                    if (clickObject(nestLayer, out hit))
                    {
                        NestPart script = hit.transform.gameObject.GetComponent<NestPart>();
                        if (script.mode == NestPart.NestPartType.Tunnel && objectMode == obj.digTunnel) //get center of tunnel when placing on tunnel
                        {
                            digStartPoint = NestPart.ProjectPointLine(hit.point, script.getStartPos(), script.getEndPos());
                            digEndPoint = hit.point;
                        }
                        else
                        {
                            digStartPoint = hit.point;
                            digEndPoint = hit.point + Vector3.up;
                        }

                        placingDigZone = true;
                        NestPart nestPartScript = WorldGen.InstantiateNestPart(digStartPoint);
                        if (objectMode == obj.digTunnel)
                        {
                            nestPartScript.setMode(NestPart.NestPartType.Tunnel);
                            nestPartScript.SetPos(digStartPoint, digEndPoint);
                        }
                        else
                        {
                            nestPartScript.setMode(NestPart.IndexToNestPartType(placingTypeIndex));
                            nestPartScript.SetPos(digStartPoint, digStartPoint + Vector3.one * 4 - Vector3.up);
                            nestPartScript.setKinematic(false);
                        }
                    }
                    else if (Nest.NestParts.Count == 0 && clickObject(terrainLayer, out hit))
                    {
                        placingDigZone = true;
                        digStartPoint = hit.point;
                        digEndPoint = hit.point;
                        NestPart nestPartScript = WorldGen.InstantiateNestPart(digStartPoint);
                        if (objectMode == obj.digTunnel)
                        {
                            nestPartScript.setMode(NestPart.NestPartType.Tunnel);
                            nestPartScript.SetPos(digStartPoint, digEndPoint);
                        }
                        else
                        {
                            nestPartScript.setMode(NestPart.IndexToNestPartType(placingTypeIndex));
                            nestPartScript.SetPos(digStartPoint, digStartPoint + Vector3.one * 4 - Vector3.up);
                            nestPartScript.setKinematic(false);
                        }
                    }
                    break;
                case obj.Ant:
                    if (clickObject(terrainLayer, out hit))
                    {
                        WorldGen.InstantiateAnt(hit.point, Quaternion.Euler(hit.normal), true);
                    }
                    break;
                case obj.AntQueen:
                    if (clickObject(terrainLayer, out hit))
                        WorldGen.InstantiateQueen(hit.point, Quaternion.Euler(hit.normal));
                    break;
                case obj.CornCob:
                    if (clickObject(terrainLayer, out hit))
                    {
                        var cobScript = WorldGen.InstantiateCornCob(hit.point + hit.normal.normalized * 3f, Quaternion.Euler(new Vector3(90, 0, 0)), 130);
                        //cobScript.Hide();
                    }
                    break;
                case obj.test:
                    if (clickObject(terrainLayer, out hit))
                    {
                        Vector3Int cube = Vector3Int.FloorToInt(hit.point);

                        /*if (SelectedAnt != null)
                        {
                            //CubePaths.GetPathToPoint(SelectedAnt.lastSurface, cube, 100, out var path);
                            //SelectedAnt.path = path;
                            //SelectedAnt.objective = new(hit.point); 
                            //SelectedAnt.state = Ant.AIState.FollowingPath;
                        }*/

                        CubePaths.CubeSurface clickedSurface = new(cube, hit.normal);

                        CubePaths.GetKnownPathToMapPart(clickedSurface, NestPart.NestPartType.FoodChamber, out List<CubePaths.CubeSurface> path);

                        CubePaths.DrawCube(cube, Color.red, 20);
                    }
                    break;
                default:
                    Debug.Log("no mode");
                    break;
            }

        }


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

    }


    //PlayingMode UI
    public GameObject playingModePanel;

    //UI NEST VISIBILITY SECTION------------------------------------------------------------------------------


    public GameObject nestPartVisibilityPanel;


    public void ShowNest()
    {
        for (int i = 0; i < Nest.NestParts.Count; i++)
            if (!Nest.NestPartDisabled[NestPart.NestPartTypeToIndex(Nest.NestParts[i].mode)])
                Nest.NestParts[i].Show();

        Debug.Log("Showing nest");
    }

    public void HideNest()
    {
        for (int i = 0; i < Nest.NestParts.Count; i++)
            Nest.NestParts[i].Hide();
        Debug.Log("Hiding nest");
    }

    public void HideNest(NestPart.NestPartType type)
    {
        for (int i = 0; i < Nest.NestParts.Count; i++)
            if (Nest.NestParts[i].mode == type)
                Nest.NestParts[i].Hide();
    }

    public void NestModeButton()
    {
        Nest.NestVisible = !Nest.NestVisible;
        applyNestMode();
    }

    public void toggleNestPartVisibility(int i)
    {
        if (Nest.NestPartDisabled[i])
        {
            Nest.NestPartDisabled[i] = false;
            ShowNest();
        }
        else
        {
            Nest.NestPartDisabled[i] = true;
            HideNest(NestPart.IndexToNestPartType(i));
            if (selectedNestPart != null)
                selectedNestPart.Show();
        }
        Nest.WriteVisibleValues();
    }

    //UI NEST PLACING CONTROLS

    public int placingTypeIndex = -1; //-1 means none
    public UnityEngine.UI.Button TunnelButton;
    public TextMeshProUGUI TunnelText;
    public UnityEngine.UI.Button FoodChamberButton;
    public TextMeshProUGUI FoodChamberText;
    public UnityEngine.UI.Button EggChamberButton;
    public TextMeshProUGUI EggChamberText;
    public UnityEngine.UI.Button QueenChamberButton;
    public TextMeshProUGUI QueenChamberText;


    //Function to call from ui, when pressing one of the placing nest part buttons.
    public void togglePlacingButton(int TypeIndex)
    {
        if (placingTypeIndex == TypeIndex)
        {
            placingTypeIndex = -1;
            objectMode = obj.None;
        }
        else
        {
            if (TypeIndex == 0) objectMode = obj.digTunnel; //tunnel index
            else objectMode = obj.digChamber;

            placingTypeIndex = TypeIndex;
        }

        //Deselect nestpart, and hide if necesary.
        if (selectedNestPart != null)
        {
            if (Nest.NestPartDisabled[NestPart.NestPartTypeToIndex(selectedNestPart.mode)])
                selectedNestPart.Hide();
            selectedNestPart = null;
            dropDownMenuObj.SetActive(false);
        }

        ColorNestPlacingButtons();
    }

    public void SetButtonColor(UnityEngine.UI.Button button, Color color)
    {
        ColorBlock cb = button.colors;
        cb.normalColor = color;
        cb.selectedColor = color;
        button.colors = cb;
    }

    //Sets the colors of the buttons used to add nest parts
    public void ColorNestPlacingButtons()
    {
        if (placingTypeIndex == 0)
            SetButtonColor(TunnelButton, Color.gray);
        else
            SetButtonColor(TunnelButton, Color.white);

        if (placingTypeIndex == 1)
            SetButtonColor(FoodChamberButton, Color.gray);
        else
            SetButtonColor(FoodChamberButton, Color.white);

        if (placingTypeIndex == 2)
            SetButtonColor(EggChamberButton, Color.gray);
        else
            SetButtonColor(EggChamberButton, Color.white);

        if (placingTypeIndex == 3)
            SetButtonColor(QueenChamberButton, Color.gray);
        else
            SetButtonColor(QueenChamberButton, Color.white);
    }

    //Updates the nest count values on the nest panel
    public void UpdateNestPartCountText()
    {
        int[] count = new int[4] { 0, 0, 0, 0 };
        foreach (var part in Nest.NestParts)
            count[NestPart.NestPartTypeToIndex(part.mode)]++;

        TunnelText.text = count[0].ToString();
        FoodChamberText.text = count[1].ToString();
        EggChamberText.text = count[2].ToString();
        QueenChamberText.text = count[3].ToString();
    }

    //UI PANEL VISIBILITY TOGGLE
    public void applyNestMode()
    {
        placingTypeIndex = -1;
        selectedNestPart = null;
        dropDownMenuObj.SetActive(false);
        //Disable placing object mode
        if (objectMode == obj.digTunnel || objectMode == obj.digChamber) objectMode = obj.None;

        dropDownMenuObj.SetActive(false);
        if (Nest.NestVisible)
        {
            ShowNest();
            nestPartVisibilityPanel.SetActive(true);
            UpdateNestPartCountText();
            ColorNestPlacingButtons();
        }
        else
        {
            HideNest();
            nestPartVisibilityPanel.SetActive(false);
        }
    }

    //UI SAVE BUTTON
    public GameObject GameSavePanel;
    public GameObject MapSavePanel;

    public void SaveGameButton()
    {
        if (!placingDigZone)
            GameSavePanel.SetActive(true);
    }

    public void SaveMapButton()
    {
        if (!placingDigZone)
            MapSavePanel.SetActive(true);
    }


    public void ExitSavePanel()
    {
        GameSavePanel.SetActive(false);
        MapSavePanel.SetActive(false);
    }

    //UI CONTROL ANT BUTTON

    public GameObject ControlButton;
    public void ControlAntButton()
    {
        ControlButton.SetActive(false);
        if (SelectedAnt != null)
        {
            SelectedAnt.IsControlled = true;
            cameraAntVector = (SelectedAnt.transform.up - SelectedAnt.transform.forward) * 4;
        }
        else if (SelectedQueen)
        {
            AntQueen.IsControlled = true;
            cameraAntVector = (AntQueen.Queen.transform.up - AntQueen.Queen.transform.forward) * 4;
        }
    }



    //SELECTED NEST PART CONTROLS

    public TMP_Dropdown typeMenu; //Value 0 is foodchamber, 1 is eggchamber and 2 is queenchamber
    public GameObject dropDownMenuObj; //To disable the entire dropdown menu object.
    public static NestPart selectedNestPart = null;


    public void DropDownMenuSelect()
    {
        if (selectedNestPart == null) return; //this should not happen. But just in case.
        if (selectedNestPart.mode == NestPart.NestPartType.Tunnel) return; //should not happen either.

        selectedNestPart.mode = NestPart.IndexToNestPartType(typeMenu.value + 1);
        UpdateNestPartCountText();
        WorldGen.updateAntCounter = true;
        WorldGen.updateCornCounter = true;
    }

    //Map creation control buttons and functions

    public GameObject MapEditPanel;
    public GameObject MapEditMenuPanel;
    public GameObject CornAmountPanel;
    public UnityEngine.UI.Slider CornAmountSlider;
    public TextMeshProUGUI SliderText;

    public UnityEngine.UI.Image CornCobButton;
    public UnityEngine.UI.Image CornPipButton;
    public UnityEngine.UI.Image AntButton;
    public UnityEngine.UI.Image AntQueenButton;
    public UnityEngine.UI.Image EraseButton;
    public UnityEngine.UI.Image TerrainEditButton;

    public GameObject controlPanel;

    public void ColorMapControlButtons()
    {
        if (objectMode == obj.Ant)
            AntButton.color = Color.gray;
        else
            AntButton.color = Color.white;

        if (objectMode == obj.AntQueen)
            AntQueenButton.color = Color.grey;
        else
            AntQueenButton.color = Color.white;

        if (objectMode == obj.CornCob)
        {
            CornCobButton.color = Color.gray;
            CornAmountPanel.SetActive(true);
        }
        else
        {
            CornCobButton.color = Color.white;
            CornAmountPanel.SetActive(false);
        }

        if (objectMode == obj.CornPip)
            CornPipButton.color = Color.gray;
        else
            CornPipButton.color = Color.white;

        if (objectMode == obj.Erase)
            EraseButton.color = Color.gray;
        else
            EraseButton.color = Color.white;

        if (objectMode == obj.EditTerrain)
        {
            TerrainEditButton.color = Color.gray;
            controlPanel.SetActive(true);
        }
        else
        {
            TerrainEditButton.color = Color.white;
            controlPanel.SetActive(false);
        }
    }

    //convert int to map edit mode. For use in buttons.
    private obj intToMapEditObj(int i)
    {
        switch (i)
        {
            case 0: return obj.Ant;
            case 1: return obj.AntQueen;
            case 2: return obj.EditTerrain;
            case 3: return obj.Erase;
            case 4: return obj.CornPip;
            case 5: return obj.CornCob;
        }
        return obj.None;
    }

    public void EditModeButton(int objInt)
    {
        obj mode = intToMapEditObj(objInt);
        if (objectMode == mode)
            objectMode = obj.None;
        else
            objectMode = mode;

        ColorMapControlButtons();
    }

    public void CornCobSliderAction()
    {
        SliderText.SetText(CornAmountSlider.value.ToString());
    }



    //SCORE AND COUNT SYSTEM

    public TextMeshProUGUI antCounterText;
    public TextMeshProUGUI cornCounterText;

    public void updateCornCounter()
    {
        if (!Nest.HasDugNestPart(NestPart.NestPartType.FoodChamber))
        {
            cornCounterText.SetText(": ?");
            return;
        }

        cornCounterText.SetText(": " + Nest.GetCornCount());
    }

    public void updateAntCounter()
    {
        if (!Nest.HasDugNestPart(NestPart.NestPartType.EggChamber))
        {
            antCounterText.SetText(": ?");
            return;
        }

        antCounterText.SetText(": " + Ant.antDictionary.Count);
    }

    public void FixedUpdate()
    {
        //Debug.Log(Mathf.FloorToInt(this.transform.position.x) + ", " + Mathf.FloorToInt(this.transform.position.y) + ", " + Mathf.FloorToInt(this.transform.position.z));
    }


}

