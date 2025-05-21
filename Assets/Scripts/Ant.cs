using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using Utils;
using FluentBehaviourTree;


public class Ant : MonoBehaviour
{
    public GameObject antObj;
    public Rigidbody Rigidbody;
    public BoxCollider PherSenseRange;
    public GameObject carriedObject = null; // the head bone


    //Variables for pheromone paths
    //public GameObject origPheromone;
    
    private Vector3Int lastCube; // Created at start, no need to save.
    CubePaths.CubeSurface antSurface; // Updated at start of every frame, no need to save.
    Vector3 normalMedian; //Is updated at the start of every update, no need to save
    private CubePheromone placedPher = null; //Last placed pheromone by ant
    IBehaviourTreeNode tree;
    



    //Datos que hay que guardar y cargar
    public int id;
    public Task objective = Task.NoTask();
    public bool isControlled = false;
    public int followingPheromone = -1; //if -1, not following a pheromone
    public int creatingPheromone = -1; //id of the pheormone the ant is creating. -1 if none
    

    //el animador
    private Animator Animator;

    //Static values
    public static float speed_per_second = 2f;
    public static float degrees_per_second = 67.5f;
    public static float speed = 0;
    public static float tiltSpeed = 10;
    public static float sep = 0.35f;

    //static ant dictionary
    public static Dictionary<int, Ant> antDictionary = new();

    public static int getNextId()
    {
        int index = 0;
        while (true)
        {
            if (!antDictionary.ContainsKey(index)) return index;
            index++;
        }
    }

    public static void registerAnt(Ant newAnt)
    {
        if (antDictionary.ContainsValue(newAnt)) return;
        newAnt.id = getNextId();
        antDictionary.Add(newAnt.id, newAnt);
    }

    public static bool registerAnt(Ant newAnt, int id)
    {
        if (antDictionary.ContainsKey(id)) return false;
        newAnt.id = id;
        antDictionary.Add(id, newAnt);
        return true;
    }

    public bool IsHolding()
    {
        return Animator.GetBool("Holding");
    }


    void OnDestroy()
    {
        Debug.Log("Destroyed me");
        antDictionary.Remove(id);
    }

    // Start is called before the first frame update
    void Start()
    {
        
        Rigidbody = antObj.GetComponent<Rigidbody>(); //El rigidbody se registra
        Animator = antObj.GetComponent<Animator>(); //El Animator se registra
        PherSenseRange = antObj.GetComponent<BoxCollider>(); //El boxCollider se registra
        SetWalking(false); //El estado por defecto no camina
        Animator.SetBool("grounded", true); //El estado por defecto se encuentra en la tierra
        Animator.enabled = true; //Se habilita el animator

        UpdateHolding();


        lastCube = Vector3Int.FloorToInt(transform.position);

        var builder = new BehaviourTreeBuilder();
        this.tree = builder
            .Sequence("Main") //Todo: rename forgetful to normal and normal to forgetful.
                .Selector("Get task if none")
                    .Condition("I have a task?", t => { return !objective.isTaskType(TaskType.None); })

                    //If im holding food, go send food.
                    .Sequence("If carrying bring to food chamber") //To do: expand this into giving food to larva?
                        .Condition("Carrying food check", t => IsHolding())
                        .Selector("Check where ant is")
                            .Sequence("If not in nest go to nest")
                                .Condition("Am I out of nest?", t => !Nest.SurfaceInNest(antSurface))
                                .Do("Set to go to nest", t => { objective = Task.GoInsideTask(antSurface); Debug.Log("Task is go inside"); return BehaviourTreeStatus.Success; })
                            .End()
                            .Sequence("If in nest go to food chamber")
                                .Condition("Am i not in food chamber?", t => !Nest.SurfaceInNestPart(antSurface, NestPart.NestPartType.FoodChamber))
                                .Do("Set to go to food chamber", t => { objective = Task.GoToNestPartTask(antSurface, NestPart.NestPartType.FoodChamber); Debug.Log("Task is go to food chamber"); return BehaviourTreeStatus.Success; })
                            .End()
                            .Do("If reached chamber put down", t => { Debug.Log("Putting down"); Animator.SetBool("Put down", true); return BehaviourTreeStatus.Success; })
                        .End()
                    .End()

                    .Do("Get requested task", t => RequestTask())
                    .Do("Sense nearby task", t => SenseTask())
                    //Go foraging
                    //Go explore
                    .Sequence("If in nest go outside")
                        .Condition("Am I in nest?", t => Nest.SurfaceInNest(antSurface))
                        .Do("Set to go outside", t => { objective = Task.GoOutsideTask(antSurface); Debug.Log("Task is go inside"); return BehaviourTreeStatus.Success; })
                    .End()
                .End()

                .Selector("Do tasks")

                    .Sequence("Pick up corn routine")
                        .Condition("My task is picking up corn?", t => objective.isTaskType(TaskType.GetCorn))
                        .Condition("Is my task valid", t => objective.isValid(this))
                        .Sequence("Pick up sequence")
                            .Do("Go to food", t => FollowObjectivePath())
                            .Do("Align with food", t => Align(objective.getPos()))
                            .Do("Wait for pickup", t => { Debug.Log("Waiting"); return BehaviourTreeStatus.Running; })
                        .End()
                    .End()

                    .Sequence("Pick from cob routine")
                        .Condition("My task is picking from cob?", t => objective.isTaskType(TaskType.CollectFromCob))
                        .Condition("Is my task valid", t => objective.isValid(this))
                        .Sequence("Pick up sequence")
                            .Do("Go to food", t => FollowObjectivePath())
                            .Do("Align with food", t => Align(objective.getPos()))
                            .Do("Wait for pickup", t => { Debug.Log("Waiting"); return BehaviourTreeStatus.Running; })
                        .End()
                    .End()

                    .Sequence("Dig routine")
                        .Condition("My task is digging?", t => objective.isTaskType(TaskType.DigPoint))
                        .Condition("Is my task valid", t => objective.isValid(this))
                        .Sequence("Dig sequence")
                            .Do("Go to digPoint", t => FollowObjectivePath())
                            .Do("Align with digPoint", t => Align(objective.getPos()))
                            .Do("Wait for dig", t => { Debug.Log("Waiting"); return BehaviourTreeStatus.Running; })
                        .End()
                    .End()

                    .Sequence("Go outside")
                        .Condition("Is my task going outside?", t => objective.isTaskType(TaskType.GoOutside))
                        .Do("Follow objective path", t => FollowObjectivePath())
                        .Selector("Complete objective if outside or finished path")
                            .Condition("Im still inside and with complete path?", t => Nest.SurfaceInNest(antSurface) && objective.path.Count != 0)
                            .Do("Complete objective", t => { objective = Task.NoTask(); return BehaviourTreeStatus.Success; })
                        .End()
                    .End()


                    .Sequence("Just follow path")
                        .Condition("Is a path following task?", t => { return objective.isTaskType(TaskType.GoInside) || objective.isTaskType(TaskType.GoToChamber) || objective.isTaskType(TaskType.GoToTunnel); })
                        .Do(".", t => { Debug.Log("Hey i made it");  return BehaviourTreeStatus.Success; })
                        .Do("Follow objective path", t => FollowObjectivePath())
                        .Do("Objective complete or failed", t => { objective = Task.NoTask();  Debug.Log("REACHED OR FAILED");  return BehaviourTreeStatus.Success; })                        
                    .End()
                        
                .End()
                
            .End()
            .Build();
    }

    
    Vector3Int nextPosDraw = Vector3Int.zero;

    // Update is called once per frame
    void FixedUpdate()
    {
        if (SenseGround(out int numHits, out bool[] rayCastHits, out float[] rayCastDist, out bool changedSurface))
        {
            Rigidbody.useGravity = false;

            DontTurn();
            SetWalking(false);

            if (isControlled){
                AntInputs();
                objective = Task.NoTask();
            }
            else tree.Tick(new TimeData(Time.deltaTime));

            //Debug.Log("Task type: " + objective.TaskToString());
        
            if (changedSurface)
                if (!Nest.SurfaceInNest(antSurface))
                    if (objective.type != TaskType.GoInside && objective.type != TaskType.GoToChamber && objective.type != TaskType.GoToTunnel)
                        PlacePheromone(rayCastHits, antSurface);

            ApplyMovement(normalMedian, rayCastHits, rayCastDist);

            CubePaths.DrawCube(nextPosDraw, Color.blue);
            CubePaths.DrawCube(antSurface.pos, Color.black);
        }
        else Rigidbody.useGravity = true;

        if (!objective.isTaskType(TaskType.None)) Debug.DrawLine(transform.position, objective.getPos(), Color.black);

    }

    //Failure if lost path or wrong taskType. Success if reached end. Running if in progress.
    private BehaviourTreeStatus FollowObjectivePath()
    {
        if (!objective.isTaskType(TaskType.None))
        {
            float dist = CubePaths.DistToPoint(this.transform.position, objective.getPos());
            if (dist < 1.5f && !objective.isTaskType(TaskType.CollectFromCob)) return BehaviourTreeStatus.Success;
            if (dist < 3f && objective.isTaskType(TaskType.CollectFromCob)) return BehaviourTreeStatus.Success;

            BehaviourTreeStatus status = SetGoalFromPath(antSurface, out Vector3 goal);
            if (status == BehaviourTreeStatus.Running) FollowGoal(normalMedian, goal);
            else {DontTurn(); SetWalking(false);}
            return status;
        }
        else return BehaviourTreeStatus.Failure;
        
    }

    private BehaviourTreeStatus SenseTask()
    {
        int digPointMask = 1 << 9;
        int cornMask = 1 << 10;
        int cornCobMask = 1 << 11;

        //BUscamos colisiones con todos los objetos comida y punto de excavación alrededor de la hormiga
        int layermask = digPointMask + cornMask + cornCobMask; //Capa de comida y digpoint
        PriorityQueue<GameObject, float> sensedItems = new();
        int maxColliders = 100;
        Collider[] hitColliders = new Collider[maxColliders];
        int numColliders = Physics.OverlapSphereNonAlloc(transform.position, 5, hitColliders, layermask);
        for (int i = 0; i < numColliders; i++)
        {
            sensedItems.Enqueue(hitColliders[i].gameObject, Vector3.Distance(hitColliders[i].transform.position, transform.position));
        }

        int minLength = int.MaxValue;
        int minDigPointScore = -1;
        Task newTask = null;
        bool foundDigPoint = false;
        while (sensedItems.Count > 0)
        {
            GameObject sensedItem = sensedItems.Dequeue();
            //DigPoints tienen prioridad sobre comida
            if (sensedItem.gameObject.layer != 9 && foundDigPoint) break;
            //Solo a los que se puede llegar son considerados -> si el camino de un considerado es vacio, ya se está
            if (CubePaths.GetPathToPoint(antSurface, Vector3Int.RoundToInt(sensedItem.transform.position), 10, out List<CubePaths.CubeSurface> newPath))
            {
                int objLayer = sensedItem.gameObject.layer;
                if (objLayer == 9) //9 is digpoint layer
                {
                    //Si es primera vez que encontramos digpoint, reseteamos el valor minimo de camino (Nos da igual que el del digpoint sea mayor que el menor de comidas encontrado)
                    if (!foundDigPoint) { foundDigPoint = true; minLength = int.MaxValue; }

                    int newScore = DigPoint.ReachableScore(Vector3Int.RoundToInt(sensedItem.transform.position));

                    //Thanks to this sistem, priorities are:
                    //1. Having a high reachable score
                    //2. Being a short path
                    if (newScore > minDigPointScore)
                    {
                        newTask = new Task(sensedItem, TaskType.DigPoint, newPath);
                        minDigPointScore = newScore;
                        minLength = newPath.Count;
                    }
                    else if (newScore == minDigPointScore)
                    {
                        if (newPath.Count < minLength)
                        {
                            newTask = new Task(sensedItem, TaskType.DigPoint, newPath);
                            minLength = newPath.Count;
                        }
                    }
                    
                    
                }
                else if (objLayer == 10) //10 is corn layer
                {
                    if (newPath.Count < minLength) newTask = new Task(sensedItem, TaskType.GetCorn, newPath);
                }
                else if (objLayer == 11) //11 is cornCobLayer
                {
                    //Only count it if it has corn left
                    if (!sensedItem.gameObject.GetComponent<CornCob>().hasCorn()) break;
                    //Set current pheromonePath to found corn!
                    if (newPath.Count < minLength) newTask = new Task(sensedItem, TaskType.CollectFromCob, newPath);
                }
                else
                {
                    Debug.Log("Wrong layer: " + sensedItem.gameObject.layer + " vs dipoint " + digPointMask + " vs food " + cornMask);
                }
            }
        }

        if (newTask != null)
        {
            objective = newTask;
            return BehaviourTreeStatus.Success;
        }
        return BehaviourTreeStatus.Failure;
    }

    private bool CheckTaskValidity()
    {
        if (!objective.isTaskType(TaskType.None))
            if (objective.isValid(this)) return true;
        
        return false;
    }

    //If there is a requested task, it is selected. Otherwise return failure.
    private BehaviourTreeStatus RequestTask()
    {
        if (Task.ToDo.TryDequeue(out objective)) return BehaviourTreeStatus.Success;

        objective = Task.NoTask();
        return BehaviourTreeStatus.Failure;
    }

    BehaviourTreeStatus Align(Vector3 point)
    {
        //Debug.Log("Alinging");
        Vector3 relativeGoal = Rigidbody.transform.InverseTransformPoint(point);
        relativeGoal.y = 0;
        float horAngle = Vector3.Angle(Vector3.forward, relativeGoal);

        //Decidir si girar
        if (horAngle > 5)
        {
            if (relativeGoal.x > 0) TurnRight();
            else TurnLeft();
            return BehaviourTreeStatus.Running;
        }
        DontTurn();

        if (objective.isTaskType(TaskType.GetCorn) || objective.isTaskType(TaskType.CollectFromCob))
        {
            Animator.SetBool("Pick up", true);
        }
        if (objective.isTaskType(TaskType.DigPoint)) 
        {
            Animator.SetBool("Dig", true);
        }

        return BehaviourTreeStatus.Success;
    }

    private void Update()
    {
    }

    public Vector3 GetRelativePos(float x, float y, float z)
    {
        return Rigidbody.position + antObj.transform.rotation * new Vector3(x, y, z);
    }

    int senseTimer = 0;

    public void PickupEvent() //function called by the animation
    {

        Animator.SetBool("Pick up", false);

        Debug.Log("I GOT TO PICK UP");
        if (objective.isTaskType(TaskType.None))
        {
            Debug.Log("Fail");
            Animator.SetTrigger("Pick up fail");
            return;
        }

        if (objective.isValid(this) && objective.isTaskType(TaskType.GetCorn))
        {
            Debug.Log("Valid");
            GameObject food = objective.GetFood();
            SetToHold(food);
            UpdateHolding();
        }
        //gets the cornCob, then a random corn pip from the cob that becomes held.
        else if (objective.isValid(this) && objective.isTaskType(TaskType.CollectFromCob))
        {
            Debug.Log("Cob valid");
            GameObject cob = objective.GetFood();
            GameObject food = cob.GetComponent<CornCob>().getRandomCornObject();
            SetToHold(food); //CornPip is removed from cornCob here
            UpdateHolding();
        }
        else
        {
            Debug.Log("Not valid or changed i guess???");
            Animator.SetTrigger("Pick up fail");
        }

        objective = Task.NoTask();
    }

    public void PutDownAction()
    {
        //carriedObject.
        foreach(Transform child in carriedObject.transform)
        {
            child.gameObject.AddComponent<Rigidbody>();
            child.GetComponent<BoxCollider>().enabled = true;
        }
        carriedObject.transform.DetachChildren();
        Animator.SetBool("Put down", false);
        UpdateHolding();
    }

    public void UpdateHolding()
    {
        if (carriedObject.transform.childCount == 0) Animator.SetBool("Holding", false);
        else Animator.SetBool("Holding", true);
    }

    //Manual put down function when controlling ant
    public void LetGo()
    {
        if (IsHolding()) Animator.SetBool("Put down", true);
    }


    public void SetToHold(GameObject obj)
    {
        //If it is attached to a cornCob, remove it from said dictionary.
        CornCob parentCob = null;
        if (obj.transform.parent != null) parentCob = obj.transform.parent.gameObject.GetComponent<CornCob>();
        if (parentCob != null)
        {
            int key = -1;
            int cornId = obj.GetComponent<Corn>().id;
            foreach(var (pos, id) in parentCob.cornCobCornDict) if (id == cornId) {key = pos; break;};
            parentCob.cornCobCornDict.Remove(key);
        }

        obj.transform.SetParent(carriedObject.transform);
        Destroy(obj.GetComponent<Rigidbody>());
        obj.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
        obj.GetComponent<BoxCollider>().enabled = false;
    }

    public void DigEvent()
    {
        Animator.SetBool("Dig", false);

        if (objective.isTaskType(TaskType.None)) return;
        //if (!objective.isTaskType(TaskType.DigPoint)) return;

        if (objective.isValid(this))
        {
            DigPoint thePoint = objective.GetDigPoint();
            if(thePoint != null)
            {
                thePoint.Dig();
                DigPoint.digPointDict.Remove(Vector3Int.RoundToInt(thePoint.transform.position));
                Destroy(thePoint.gameObject);
            }
            objective = Task.NoTask();
        }
        else objective = Task.NoTask();
    }
  
    void RandomMovement()
    {
        speed = speed_per_second * Time.fixedDeltaTime;
        Animator.SetBool("walking", true);
    }

    public float minAngle = 60f;

    //La idea inicial es coger el plano x-z sobre el que se encuentra la hormiga, luego proyectar el punto del objeto pheromona sobre �l.
    //Dependiendo de donde se encuentra en el plano ajustar la direcci�n y decidir si moverse hacia delante.
    void FollowGoal(Vector3 hitNormal, Vector3 goal)
    {

        //Obtenemos los datos de distancia hacia la pheromona
        Vector3 relativeGoal = Rigidbody.transform.InverseTransformPoint(goal); //relative position of the goal to the ant
        relativeGoal.y = 0; //Para calcular la distancia en el plano horizontal se quita el valor y
        float horAngle = Vector3.Angle(Vector3.forward, relativeGoal);


        //MOST IMPORTANT CHANGE: MAKES EVERYTHING A LOT SMOOTHER.
        Vector3 goalVector = goal - transform.position;
        Vector3 proyectedGoal = Vector3.ProjectOnPlane(goalVector, hitNormal);
        Vector3 proyectedForward = Vector3.ProjectOnPlane(transform.forward, hitNormal);
        horAngle = Vector3.Angle(proyectedGoal, proyectedForward);

        Debug.DrawLine(transform.position, goal, Color.red, 0.35f);
    
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

  /*  private CubePheromone ChoosePheromone(List<CubePheromone> sensedPhers)
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
            if (followingPheromone != -1)
            {
                if (sensedPhers[i].GetPathId() == followingPheromone)
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
    */

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

    private bool SenseGround(out int numHits, out bool[] rayCastHits, out float[] rayCastDist, out bool changedSurface)
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
        Quaternion deltaRotation = Quaternion.Euler(new Vector3(0,Animator.GetInteger("turning") * degrees_per_second * Time.fixedDeltaTime,0));
        Rigidbody.MoveRotation(Rigidbody.rotation * deltaRotation); //Rotate ant

        //MOVE ANT FORWARD
        Vector3 proyectedVector = Vector3.ProjectOnPlane(Rigidbody.rotation * Vector3.forward, surfaceNormalMedian); //Project movement over terrain
        Rigidbody.position = Rigidbody.position + proyectedVector * speed; //Move forward

        //
        AdjustAntToGround(rayCastHits, rayCastDist, deltaRotation);

    }

    private void PlacePheromone(bool[] rayCastHits, CubePaths.CubeSurface antSurface)
    {
        //DECIDE SI PONER PHEROMONA AAAAAAAAAAAAAAA---------------------------------------------------------
        if (rayCastHits[4]) //si se crea camino y el raycast principal ve suelo
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
                //si ha sido un salto corto, se rellena la distancia con la pheromona
                if (CubePaths.GetPathToSurface(placedPher.GetSurface(), antSurface, 3, out List<CubePaths.CubeSurface> path))
                {
                    Debug.Log("DISCONECTED BUT NOT BY MUCH");
                    CubePheromone prev = placedPher;
                    foreach (var surface in path)
                    {
                        prev = CubePaths.ContinuePheromoneTrail(surface, prev);
                    }
                    placedPher = prev;
                }
                else
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


    //Si hay Comida cerca, pone como task recoger la comida con el path hacia la comida
    //Si no hay, devuelve false.
    private Task SenseFood()
    {
        int layermask = 1 << 10;
        PriorityQueue<GameObject, float> sensedFood = new();
        int maxColliders = 100;
        Collider[] hitColliders = new Collider[maxColliders];
        int numColliders = Physics.OverlapSphereNonAlloc(transform.position, 5, hitColliders, layermask);
        for (int i = 0; i < numColliders; i++)
        {
            GameObject food = hitColliders[i].transform.gameObject;
            sensedFood.Enqueue(hitColliders[i].transform.gameObject, 100 - Vector3.Distance(food.transform.position, transform.position));
        }

        int minLength = int.MaxValue;
        Task newTask = null;

        while (sensedFood.Count > 0)
        {
            GameObject food = sensedFood.Dequeue();
            List<CubePaths.CubeSurface> newPath;

            bool isReachable = CubePaths.GetPathToPoint(antSurface, Vector3Int.RoundToInt(food.transform.position), 10, out newPath);
            
            if (isReachable && newPath.Count < minLength)
            {         
                //newTask = new Task(food.GetComponent<Corn>().id);
                objective.path = newPath;
                minLength = objective.path.Count;
            }

        }

        return newTask;
    }

    //Si hay digpoints alcanzables cerca, devuelve true y pone el path de la hormiga al camino hacia el digpoint más cercano alcanzable.
    //Si no hay, devuelve false.
    private Task SenseDigPoints()
    {
        int layermask = 1 << 9;
        PriorityQueue<GameObject, float> sensedDigPoints = new();
        int maxColliders = 100;
        Collider[] hitColliders = new Collider[maxColliders];
        int numColliders = Physics.OverlapSphereNonAlloc(transform.position, 5, hitColliders, layermask);
        for (int i = 0; i < numColliders; i++)
        {
            DigPoint digPoint = hitColliders[i].gameObject.GetComponent<DigPoint>();
            sensedDigPoints.Enqueue(hitColliders[i].gameObject, 100 - Vector3.Distance(digPoint.transform.position, transform.position));
        }

        //int minDepth = int.MaxValue;
        int minLength = int.MaxValue;

        Task newTask = null;

        while (sensedDigPoints.Count > 0)
        {
            GameObject digObject = sensedDigPoints.Dequeue();
            DigPoint digPoint = digObject.GetComponent<DigPoint>();
            List<CubePaths.CubeSurface> newPath;

            //Solo a los que se puede llegar son considerados -> si el camino de un considerado es vacio, ya se está
            bool isReachable = CubePaths.GetPathToPoint(antSurface, Vector3Int.RoundToInt(digPoint.transform.position), 10, out newPath);
            
            if (isReachable && newPath.Count < minLength)
                //((digPoint.depth < minDepth) ||
                //(digPoint.depth == minDepth && newPath.Count < minLength)))
            {         
                newTask = new Task(digPoint.transform.position);
                newTask.path = newPath;
                //minDepth = digPoint.depth;
                minLength = objective.path.Count;
            }

        }

        return newTask;
    }

    //Pone el 
    private BehaviourTreeStatus SetGoalFromPath(CubePaths.CubeSurface antSurface, out Vector3 goal)
    {

        goal = Vector3.zero;

        //Debug.Log("SettingGoal");
        if (objective.path.Count == 0)
        {
            Debug.Log("Path completed");
            return BehaviourTreeStatus.Success; 
        }//Para evitar seguir camino nonexistente.


        List<CubePaths.CubeSurface> sensedRange = new();
        int goalIndex = -1;
        int range = 0;
        Dictionary<CubePaths.CubeSurface, CubePaths.CubeSurface> checkedSurfaces = new();
        
        sensedRange = GetNextSurfaceRange(antSurface, sensedRange, ref checkedSurfaces); //Initial one.

        var sameCube = sensedRange[0];

        if (sameCube.Equals(objective.path.Last()))
        {
            Debug.Log("Same");
            objective.path = new();
            return BehaviourTreeStatus.Success;
        }

        //IR AUMENTANDO RANGO HASTA QUE RANGO CONTENGA PARTE DEL CAMINO, Y DEVOLVER INDICE DEL BLOQUE ENCONTRADO
        while (goalIndex == -1 && range < 5)
        {
            range++;
            sensedRange = GetNextSurfaceRange(antSurface, sensedRange, ref checkedSurfaces);

            for (int i = 0; i < objective.path.Count; i += 1)
            {
                if (sensedRange.Exists(x => x.Equals(objective.path[i]))) goalIndex = i;
            }
        }

        if (goalIndex == -1)
        {
            Debug.Log("not found");
            objective.path = new();
            return BehaviourTreeStatus.Failure;
        }

        CubePaths.CubeSurface firstStep = objective.path[goalIndex];

        //Debug.Log("First step: " + firstStep.pos + " at range " + range);

        Vector3Int dir = firstStep.pos - antSurface.pos;

        if (goalIndex < objective.path.Count - 1) goal = CubePaths.GetMovementGoal(antSurface, dir, objective.path[goalIndex+1].pos - objective.path[goalIndex].pos);
        else goal = CubePaths.GetMovementGoal(antSurface, dir);

        nextPosDraw = firstStep.pos;

        return BehaviourTreeStatus.Running;
    }
 
    //Devuelve true si ha encontrado una pheromona
    /*private bool SensePheromones(CubePaths.CubeSurface antSurface, out Vector3 goal) 
    {
        goal = Vector3.zero;
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

        goal = CubePaths.GetMovementGoal(antSurface, dir);

        nextPosDraw = firstStep.pos;

        return true;

    }*/

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

    void AntInputs() {
        //if (SelectedAnt.state != Ant.AIState.Controlled) return;
        if (Input.GetKey(KeyCode.UpArrow))          SetWalking(true);
        else                                        SetWalking(false);
        if (Input.GetKey(KeyCode.LeftArrow))        TurnLeft();
        else if (Input.GetKey(KeyCode.RightArrow))  TurnRight();
        else                                        DontTurn();
        //if(Input.GetKey(KeyCode.Comma))             LetGo();

        //if (Input.GetKeyDown(KeyCode.DownArrow) && SelectedAnt.placedPheromone != null) SelectedAnt.placedPheromone.ShowPath(false);
    }

    public AnimatorStateInfo GetAnimatorStateInfo()
    {
        return Animator.GetCurrentAnimatorStateInfo(0);
    }

    /* Da igual lo que probe, play no pone a la hormiga en ese state, simplemente juega el state y se queda pillado hasta que interactuar con el animator lo lleva al state original inmediatamente.
    private void playLoadedAnimation()
    {
        if (loadedAnimHashPath != -1)
            if (Animator != null)
                Animator.Play(loadedAnimHashPath, 0, loadedAnimHashPath);
            else Debug.Log("NO ANIMATOR");
    }
    */

}
