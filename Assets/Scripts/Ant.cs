using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using Utils;
using NUnit.Framework;


public class Ant : MonoBehaviour
{

    public GameObject antObj;
    public Rigidbody Rigidbody;
    public BoxCollider PherSenseRange;
    public float speed = 0;
    public float turn = 1;
    public float tiltSpeed = 10;
    public float sep = 0.35f;

    //Variables for pheromone paths
    //public GameObject origPheromone;
    public bool makingTrail = false;
    private bool followingForwards = false;
    private int pherId = -1; //if -1, not following a pheromone
    private Vector3Int lastCube;
    public CubePaths.CubeSurface lastSurface;
    private CubePheromone placedPher = null; //Last placed pheromone by ant
    public List<CubePaths.CubeSurface> path = new(); //Path the ant will follow if in followingPath mode
    public GameObject digObjective = null;
    private Vector3 nextGoal; //Point in space the ant will try to move to.
    public bool haveGoal = false; //only use in AIMovement case
    public int stucktimer = 0;


    public enum AIState
    {
        Exploring,
        FollowingPher,
        FollowingPath, //Following a
        DiggingAnim,
        Controlled,
        Passive
    }

    public AIState state = AIState.FollowingPher;
    //el animador
    private Animator Animator;
    public float speed_per_second = 2f;
    public float degrees_per_second = 67.5f;

    public enum followState
    {
        Reached,
        Lost,
        Following
    }


    // Start is called before the first frame update
    void Start()
    {
        Physics.gravity = new Vector3(0, -15.0F, 0); //Se activa gravedad por defecto
        Rigidbody = antObj.GetComponent<Rigidbody>(); //El rigidbody se registra
        Animator = antObj.GetComponent<Animator>(); //El Animator se registra
        PherSenseRange = antObj.GetComponent<BoxCollider>(); //El boxCollider se registra
        SetWalking(false); //El estado por defecto no camina
        Animator.SetBool("grounded", true); //El estado por defecto se encuentra en la tierra
        Animator.enabled = true; //Se habilita el animator

        lastCube = Vector3Int.FloorToInt(transform.position);
    }

    private void Update()
    {
    }

    int pathTimer = 0;
    // Update is called once per frame
    void FixedUpdate()
    {
        if (SenseGround(out int numHits, out Vector3 normalMedian, out bool[] rayCastHits, out float[] rayCastDist, out CubePaths.CubeSurface antSurface, out bool changedSurface))
        {
            Rigidbody.useGravity = false;

            AIBehaviour(changedSurface, antSurface);
        
            if (changedSurface) DecideToPlacePheromone(rayCastHits, antSurface);

            ApplyMovement(normalMedian, rayCastHits, rayCastDist);

            lastSurface = antSurface;
        }
        else Rigidbody.useGravity = true;

        if (digObjective != null) Debug.DrawLine(transform.position, digObjective.transform.position, Color.black);

    }

    public Vector3 GetRelativePos(float x, float y, float z)
    {
        return Rigidbody.position + antObj.transform.rotation * new Vector3(x, y, z);
    }

    int senseTimer = 0;
    
    void AIBehaviour(bool movedCube, CubePaths.CubeSurface antSurface) {

        switch (state)
        {
            //Si estamos siguiendo una feromona, buscamos siguiente paso al cambiar de cubo.
            //La gestión de cambiar de following a no following al perderse no se hace aqui?
            case AIState.FollowingPher:

                if (movedCube)
                {
                    if (SenseDigPoint(antSurface))
                    {
                        if (path.Count == 0) state = AIState.DiggingAnim;
                        else
                        {
                            haveGoal = true;
                            state = AIState.FollowingPath;
                            SetGoalFromPath(antSurface);
                        }
                    }
                    else
                        haveGoal = SensePheromones(antSurface);
                }

                if (haveGoal) FollowGoal();
                else state = AIState.Passive;
            break;

            case AIState.FollowingPath:
                followState followState = followState.Following;
                if (movedCube || !haveGoal) followState = SetGoalFromPath(antSurface);

                switch (followState){

                    case followState.Reached:
                        haveGoal = false;
                        state = AIState.DiggingAnim;
                    break;

                    case followState.Lost:
                        haveGoal = false;
                        state = AIState.Passive;
                    break;

                    case followState.Following:
                        haveGoal = true;
                        FollowGoal();
                    break;
                }
            break;

            case AIState.Exploring:
                RandomMovement();
            break;

            case AIState.Passive:

                SetWalking(false);
                DontTurn();

                senseTimer++;
                if (senseTimer == 100)
                {
                    senseTimer = 0;
                    if (SenseDigPoint(antSurface))
                    {
                        Debug.Log("THE PATH LENG IS " + path.Count);
                        if (path.Count == 0) state = AIState.DiggingAnim;
                        else
                        {
                            haveGoal = true;
                            state = AIState.FollowingPath;
                            SetGoalFromPath(antSurface);
                        }
                    }
                    else if (SensePheromones(antSurface))
                    {
                        haveGoal = true;
                        state = AIState.FollowingPher;
                        FollowGoal();
                    }
                }
            break;

            case AIState.DiggingAnim:
                if (digObjective == null) state = AIState.Passive;
                else
                {
                    if (!IsDigging())
                    {
                        SetWalking(false);
                        if (Align(digObjective.transform.position)) SetDig(true);
                    }
                }
            break;

            default:
            break;
        }
        //MODO CONTROLADO LO GESTIONA LA CÁMARA
        turn = Animator.GetInteger("turning") * degrees_per_second * Time.fixedDeltaTime;
    }

    public void PerformDig()
    {
        SetDig(false);
        if (digObjective == null) return;

        digObjective.GetComponent<DigPoint>().Dig();
        Destroy(digObjective);
        digObjective = null;
    }

    //Devuelve true si la hormiga llega alinearse con el punto
    bool Align(Vector3 point)
    {
        Vector3 relativeGoal = Rigidbody.transform.InverseTransformPoint(point);
        relativeGoal.y = 0;
        float horAngle = Vector3.Angle(Vector3.forward, relativeGoal);

        //Decidir si girar
        if (horAngle > 5)
        {
            if (relativeGoal.x > 0) TurnRight();
            else TurnLeft();
            return false;
        }
        DontTurn();
        return true;
    }

    void RandomMovement()
    {
        speed = speed_per_second * Time.fixedDeltaTime;
        Animator.SetBool("walking", true);
    }

    //La idea inicial es coger el plano x-z sobre el que se encuentra la hormiga, luego proyectar el punto del objeto pheromona sobre �l.
    //Dependiendo de donde se encuentra en el plano ajustar la direcci�n y decidir si moverse hacia delante.
    void FollowGoal()
    {

        if (!haveGoal)
        {
            DontTurn();
            SetWalking(false);
            Debug.Log("I DON THAVE A GOAL");
            return;
        }

        //Obtenemos los datos de distancia hacia la pheromona
        Vector3 relativeGoal = Rigidbody.transform.InverseTransformPoint(nextGoal); //relative position of the goal to the ant
        relativeGoal.y = 0; //Para calcular la distancia en el plano horizontal se quita el valor y
        float horAngle = Vector3.Angle(Vector3.forward, relativeGoal);

        float minAngle = 35f;

        Debug.DrawLine(transform.position, nextGoal, Color.red, 0.35f);
    
        //Decidir si girar
        if (horAngle > 5)
        {
            if (relativeGoal.x > 0) TurnRight();
            else TurnLeft();
        }
        else DontTurn();

        //Decidir si avanzar
        if (horAngle < minAngle) SetWalking(true);
        else SetWalking(false);
    }

    private CubePheromone ChoosePheromone(List<CubePheromone> sensedPhers)
    {
        CubePheromone chosenPher = null;
        int pathVal = 0;
        if (!followingForwards) pathVal = int.MaxValue;

        bool isFurther(int value)
        {
            if (followingForwards) return pathVal < value;
            else return pathVal > value;
        }

        for (int i = 0; i < sensedPhers.Count(); i++)
        {
            if (pherId != -1)
            {
                if (sensedPhers[i].GetPathId() == pherId)
                    if (isFurther(sensedPhers[i].GetPathPos()))
                    {
                        chosenPher = sensedPhers[i];
                        pathVal = sensedPhers[i].GetPathPos();
                    }
            }
            else
            {
                if (isFurther(sensedPhers[i].GetPathPos()))
                {
                    chosenPher = sensedPhers[i];
                    pathVal = sensedPhers[i].GetPathPos();
                }
            }
        }
        return chosenPher;
    }

    public void SetWalking(bool walk){
        if (walk)
        {
            speed = speed_per_second * Time.fixedDeltaTime;
            Animator.SetBool("walking", true);
        }
        else
        {
            speed = 0f;
            Animator.SetBool("walking", false);
        }
    }

    public void TurnRight(){
        Animator.SetInteger("turning", 1);
    }

    public void TurnLeft(){
        Animator.SetInteger("turning", -1);
    }

    public void DontTurn(){
        Animator.SetInteger("turning", 0);
    }

    public void SetDig(bool dig)
    {
        if (dig)
        {
            SetWalking(false);
            DontTurn();
            Animator.SetBool("Digging", true);
        }
        else Animator.SetBool("Digging", false);
    }

    public bool IsDigging()
    {
        return Animator.GetBool("Digging");
    }

    private bool SenseGround(out int numHits, out Vector3 normalMedian, out bool[] rayCastHits, out float[] rayCastDist, out CubePaths.CubeSurface antSurface, out bool changedSurface)
    {
        Color hitColor;
        numHits = 0;
        normalMedian = Vector3.zero;
        //El orden de los raycasts es importante. Son atras derecha -> atras izquierda -> delante izquierda -> delante derecha -> centro
        float[] xPos = {sep, -sep, -sep, sep, 0};
        float[] zPos = {-sep, -sep, sep, sep, 0};
        float yPos = 0.5f;
        rayCastHits = new bool[]{ false, false, false, false, false};
        rayCastDist = new float[]{0f,0f,0f,0f,0f};
        Vector3 hitNormal = new Vector3(0, 0, 0); 
        Vector3Int hitCubePos = new Vector3Int(0,0,0);
        int raycastLayer = (1 << 6); //layer del terreno
        for (int i = 0; i < xPos.Length; i++) {
            //HE ESTADO USANDO MAL ESTA FUNCIÓN. RAYCASTLAYER ESTABA FUNCIONANDO COMO MAXDISTANCE
            if (Physics.Raycast(GetRelativePos(xPos[i], yPos, zPos[i]), Rigidbody.rotation * new Vector3(0, yPos - 0.8f, 0),  out RaycastHit hit, 0.8f, raycastLayer))
            {
                hitColor = Color.red;
                numHits++;
                normalMedian += hit.normal;
                rayCastHits[i] = true;
                rayCastDist[i] = hit.distance;
                hitNormal = hit.normal;
                hitCubePos = Vector3Int.FloorToInt(hit.point);
            }
            else hitColor = Color.blue;
            Debug.DrawRay(GetRelativePos(xPos[i], yPos, zPos[i]), Rigidbody.rotation * new Vector3(0, yPos - 0.8f, 0), hitColor);
        }


        //REMEMBER TO COMMENT HOW YOU CHANGED FROM LOCAL BOOL TO THE ANIMATOR ONE
        if (numHits > 0)
        {
            antSurface = new CubePaths.CubeSurface(hitCubePos, hitNormal);
            changedSurface = lastCube != hitCubePos;
            lastCube = hitCubePos;

            Animator.SetBool("grounded", true);
            normalMedian /= numHits;
            return true;
        }
        else
        {
            antSurface = new CubePaths.CubeSurface();
            changedSurface = true;

            Animator.SetBool("grounded", false);
            return false;
        }
    }

    private void ApplyMovement(Vector3 surfaceNormalMedian, bool[] rayCastHits, float[] rayCastDist)
    {
        //REMOVE MOVEMENT EFFECTS
        Rigidbody.angularVelocity = Vector3.zero;

        //APPLY LOCAL GRAVITY
        Rigidbody.AddForce(-surfaceNormalMedian*40); //USES ADDFORCE INSTEAD OF GRAVITY TO AVOID SLOW EFFECT

        //ROTATE ANT
        Quaternion deltaRotation = Quaternion.Euler(new Vector3(0,turn,0));
        Rigidbody.MoveRotation(Rigidbody.rotation * deltaRotation); //Rotate ant

        //MOVE ANT FORWARD
        Vector3 proyectedVector = Vector3.ProjectOnPlane(Rigidbody.rotation * Vector3.forward, surfaceNormalMedian); //Project movement over terrain
        Rigidbody.position = Rigidbody.position + proyectedVector * speed; //Move forward

        //
        AdjustAntToGround(rayCastHits, rayCastDist, deltaRotation);

    }

    private void DecideToPlacePheromone(bool[] rayCastHits, CubePaths.CubeSurface antSurface)
    {
        //DECIDE SI PONER PHEROMONA AAAAAAAAAAAAAAA---------------------------------------------------------
        if (makingTrail && rayCastHits[4]) //si se crea camino y el raycast principal ve suelo
        {
            if (placedPher == null) //La hormiga no ha empezado aún su camino
            {
                placedPher = CubePaths.StartPheromoneTrail(antSurface);
            }
            else if (CubePaths.DoesSurfaceConnect(placedPher.GetSurface(), antSurface.pos)) //Si se ha llegado a un nuevo cubo adyacente
            {   //Might somehow fuck up if ant moves to adyacent cube on unreachable surface SOMEHOW. 
                placedPher = CubePaths.ContinuePheromoneTrail(antSurface, placedPher); 
            }
            else //Si la hormiga se ha separado de su anterior camino
            {
                placedPher = CubePaths.StartPheromoneTrail(antSurface);
            }
        }
    }

    
    private void AdjustAntToGround(bool[] rayCastHits, float[] rayCastDist, Quaternion deltaRotation)
    {
        if (!rayCastHits[4])
            {
                float xRotation = 0;
                float zRotation = 0;
                if (rayCastHits[0] && !rayCastHits[1]) zRotation += tiltSpeed * 0.7f;
                if (rayCastHits[1] && !rayCastHits[0]) zRotation -= tiltSpeed * 0.7f;
                if (rayCastHits[1] && !rayCastHits[2]) xRotation += tiltSpeed * 0.7f;
                if (rayCastHits[2] && !rayCastHits[1]) xRotation -= tiltSpeed * 0.7f;
                if (rayCastHits[2] && !rayCastHits[3]) zRotation -= tiltSpeed * 0.7f;
                if (rayCastHits[3] && !rayCastHits[2]) zRotation += tiltSpeed * 0.7f;
                if (rayCastHits[3] && !rayCastHits[0]) xRotation -= tiltSpeed * 0.7f;
                if (rayCastHits[0] && !rayCastHits[3]) xRotation += tiltSpeed * 0.7f;
                deltaRotation = Quaternion.Euler(new Vector3(xRotation, 0, zRotation));
                Rigidbody.MoveRotation(Rigidbody.rotation * deltaRotation);
            }
            else
            {
                float xRotation = 0;
                float zRotation = 0;
                if (rayCastHits[0] && rayCastHits[1] && rayCastDist[0] < rayCastDist[1]*1.8f) zRotation += tiltSpeed * 0.1f;
                if (rayCastHits[1] && rayCastHits[0] && rayCastDist[1] < rayCastDist[0]*1.8f) zRotation -= tiltSpeed * 0.1f;
                if (rayCastHits[1] && rayCastHits[2] && rayCastDist[1] < rayCastDist[2]*1.8f) xRotation += tiltSpeed * 0.1f;
                if (rayCastHits[2] && rayCastHits[1] && rayCastDist[2] < rayCastDist[1]*1.8f) xRotation -= tiltSpeed * 0.1f;
                if (rayCastHits[2] && rayCastHits[3] && rayCastDist[2] < rayCastDist[3]*1.8f) zRotation -= tiltSpeed * 0.1f;
                if (rayCastHits[3] && rayCastHits[2] && rayCastDist[3] < rayCastDist[2]*1.5f) zRotation += tiltSpeed * 0.1f;
                if (rayCastHits[3] && rayCastHits[0] && rayCastDist[3] < rayCastDist[0]*1.8f) xRotation -= tiltSpeed * 0.1f;
                if (rayCastHits[0] && rayCastHits[3] && rayCastDist[0] < rayCastDist[3]*1.8f) xRotation += tiltSpeed * 0.1f;
                deltaRotation = Quaternion.Euler(new Vector3(xRotation, 0, zRotation));
                Rigidbody.MoveRotation(Rigidbody.rotation * deltaRotation);
            }
    }

    //Si hay digpoints alcanzables cerca, devuelve true y pone el path de la hormiga al camino hacia el digpoint más cercano alcanzable.
    //Si no hay, devuelve false.
    private bool SenseDigPoint(CubePaths.CubeSurface antSurface)
    {
        int layermask = 1 << 9;
        PriorityQueue<GameObject, float> sensedDigPoints = new();
        int maxColliders = 100;
        Collider[] hitColliders = new Collider[maxColliders];
        int numColliders = Physics.OverlapSphereNonAlloc(transform.position, 5, hitColliders, layermask);
        for (int i = 0; i < numColliders; i++)
        {
            DigPoint digPoint = hitColliders[i].transform.gameObject.GetComponent<DigPoint>();
            sensedDigPoints.Enqueue(hitColliders[i].transform.gameObject, 100 - Vector3.Distance(digPoint.transform.position, transform.position));
        }

        int minDepth = int.MaxValue;
        int minLength = int.MaxValue;

        while (sensedDigPoints.Count > 0)
        {
            GameObject digObject = sensedDigPoints.Dequeue();
            DigPoint digPoint = digObject.GetComponent<DigPoint>();
            List<CubePaths.CubeSurface> newPath;

            bool isReachable = CubePaths.PathToPoint(antSurface, Vector3Int.FloorToInt(digPoint.transform.position), 10, out newPath);
            
            if (isReachable && 
                ((digPoint.depth < minDepth) ||
                (digPoint.depth == minDepth && newPath.Count < minLength)))
            {         
                path = newPath;
                digObjective = digObject;
                minDepth = digPoint.depth;
                minLength = path.Count;
            }

        }

        return minDepth != int.MaxValue;
    }

    //Pone el 
    private followState SetGoalFromPath(CubePaths.CubeSurface antSurface)
    {
        Debug.Log("SettingGoal");

        if (path.Count == 0)
        {
            Debug.Log("no path to follow");
            return followState.Lost; 
        }//Para evitar seguir camino nonexistente.

        List<CubePaths.CubeSurface> sensedRange = new();
        int goalIndex = -1;
        int range = 0;
        Dictionary<CubePaths.CubeSurface, CubePaths.CubeSurface> checkedSurfaces = new();
        
        sensedRange = GetNextSurfaceRange(antSurface, sensedRange, ref checkedSurfaces); //Initial one.

        var sameCube = sensedRange[0];

        if (sameCube.Equals(path.Last()))
        {
            Debug.Log("Same");
            path = new();
            return followState.Reached;
        }

        //IR AUMENTANDO RANGO HASTA QUE RANGO CONTENGA PARTE DEL CAMINO, Y DEVOLVER INDICE DEL BLOQUE ENCONTRADO
        while (goalIndex == -1 && range < 5)
        {
            range++;
            sensedRange = GetNextSurfaceRange(antSurface, sensedRange, ref checkedSurfaces);

            for (int i = 0; i < path.Count; i += 1)
            {
                if (sensedRange.Exists(x => x.Equals(path[i]))) goalIndex = i;
            }
        }

        if (goalIndex == -1)
        {
            Debug.Log("not found");
            path = new();
            digObjective = null;
            return followState.Lost;
        }

        CubePaths.CubeSurface firstStep = path[goalIndex];

        Debug.Log("First step: " + firstStep.pos);

        Vector3Int dir = firstStep.pos - antSurface.pos;

        if (goalIndex < path.Count - 1) CubePaths.DrawCube(path[goalIndex+1].pos, Color.cyan, 2);//dir = dir + path[goalIndex+1].pos - path[goalIndex].pos;

        nextGoal = CubePaths.GetMovementGoal(antSurface, dir);

        return followState.Following;
    }

    
    //Devuelve true si ha encontrado una pheromona
    private bool SensePheromones(CubePaths.CubeSurface antSurface) 
    {
        List<CubePaths.CubeSurface> sensedRange = new();
        Dictionary<CubePaths.CubeSurface, CubePaths.CubeSurface> checkedSurfaces = new();
        bool foundGoal = false;
        int range = -1;

        CubePheromone objectivePher = null;
        CubePaths.CubeSurface firstStep = new();

        Color[] colors = {Color.blue, Color.magenta, Color.red, Color.black, Color.blue, Color.black, Color.blue, Color.black, Color.blue, Color.black, Color.blue};

        while (!foundGoal && range < 5)
        {
            range++;
            sensedRange = GetNextSurfaceRange(antSurface, sensedRange, ref checkedSurfaces);

            //Put all pheromones on a list
            List<CubePheromone> sensedPheromones = new();
            foreach (var surface in sensedRange)
            {
                //CubePaths.DrawSurface(surface, colors[range], 2);
                //CubePaths.DrawCube(surface.pos, colors[range], 2);
                if (CubePaths.cubePherDict.TryGetValue(surface.pos, out List<CubePheromone> surfacePhers))
                    sensedPheromones.AddRange(surfacePhers);
            }
            //

            objectivePher = ChoosePheromone(sensedPheromones);
            if (objectivePher != null)
            {
                foundGoal = true;
                firstStep = objectivePher.surface;
            }
        }

        //Si no se ha encontrado objetivo, devolvemos falso.
        if (!foundGoal)
        {
            //("NO PHEROMONES FOUND/CHOSEN");
            return false;
        }

        //Si la pheromona está en la superficie actual seguimos su camino
        if (range == 0)
        {
            if (objectivePher.isLast(followingForwards))
            {
                followingForwards = !followingForwards;
                //Debug.Log("Switched following");
            }
            if (objectivePher.isLast(followingForwards))
            {
                Debug.Log("SINGLE PHEROMONE PATH; IM FUCKING STUCKKKKK");
                return false;
            }

            firstStep = objectivePher.GetNext(followingForwards).GetSurface();
        }
        else
            while (!checkedSurfaces[firstStep].Equals(firstStep))
            {
                firstStep = checkedSurfaces[firstStep];
            }
        
        Vector3Int dir = firstStep.pos - antSurface.pos;

        nextGoal = CubePaths.GetMovementGoal(antSurface, dir);

        return true;

    }

    List<CubePaths.CubeSurface> GetNextSurfaceRange(CubePaths.CubeSurface antSurface, List<CubePaths.CubeSurface> currentRange, ref Dictionary<CubePaths.CubeSurface, CubePaths.CubeSurface> checkedSurfaces)
    {
        List<CubePaths.CubeSurface> nextRange = new();

        //Si el rango está empezando se coge la superficie de la hormiga
        if (currentRange.Count == 0)
        {
            nextRange.Add(antSurface);
            checkedSurfaces.Add(antSurface, antSurface);
            return nextRange;
        }

        //Si el rango es la superficie de la hormiga se cogen los adyacentes (para poner sus firststep)
        if(currentRange[0].Equals(antSurface)) 
        {
            if (currentRange.Count != 1) Debug.Log("YOU FUCKED UPPPPP-------------------------");

            List<CubePaths.CubeSurface> adyacentCubes = CubePaths.GetAdyacentCubes(currentRange[0], transform.forward);
            foreach (var son in adyacentCubes)
            {
                nextRange.Add(son);
                checkedSurfaces.Add(son, son);
                //CubePaths.DrawCube(son.pos, Color.magenta, 1);
            }
            return nextRange;
        }


        //Si el rango es mayor que todo eso se procede como debido
        foreach (var currentSurface in currentRange)
        {
            List<CubePaths.CubeSurface> adyacentCubes = CubePaths.GetAdyacentCubes(currentSurface, transform.forward);
            
            foreach (var son in adyacentCubes)
            {
                if (!checkedSurfaces.ContainsKey(son))
                {
                    nextRange.Add(son);
                    checkedSurfaces.Add(son, currentSurface);
                    //CubePaths.DrawCube(son.pos, Color.green, 1);
                }
            }
        }
        return nextRange;
    }

}
