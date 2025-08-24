using UnityEngine;
using System.Collections.Generic;
using Utils;
using FluentBehaviourTree;
using Unity.VisualScripting;
using System;


public class Ant : MonoBehaviour
{
    public GameObject antObj;
    public Rigidbody Rigidbody;
    public CapsuleCollider terrainCapCollider;
    public CapsuleCollider antCapCollider;
    public GameObject carriedObject = null; // the head bone
    public string taskName = "";
    public GameObject staticEgg;
    public GameObject eggAnim;
    private Animator eggAnimator;


    //el animador
    private Animator Animator;

    //Variables for pheromone paths
    //public GameObject origPheromone;

    private Vector3Int lastCube; // Created at start, no need to save.
    private TaskType lastTaskType; // created at start, no need to save.
    private Vector3 goal = Vector3.zero;
    private bool resetGoal = true;
    CubePaths.CubeSurface antSurface; // Updated at start of every frame, no need to save.
    Vector3 normalMedian; //Is updated at the start of every update, no need to save
    IBehaviourTreeNode tree;


    //Datos que hay que guardar y cargar
    public int id;
    public int antId = -1; //Id of the ant that is picking it up if it is still an egg
    public int age = 0;
    public Task objective = Task.NoTask();
    public Outline outline;
    public bool IsControlled = false; //outline will be enabled/disabled depending on 
    public int Counter = 0; //Counter of how long the ant is lost before checking if it can go home.
    public HashSet<int> discoveredCobs = new(); //Cobs discovered outside of nest.



    //Static values
    public static float speed_per_second = 2.5f;
    public static float degrees_per_second = 67.5f;
    public static float speed = 0;
    public static float tiltSpeed = 10;
    public static float sep = 0.35f;
    //Distances to objectives to act
    public static float cornCobDistance = 3;
    public static float digPointDistance = 1.2f;

    //valor para gestionar cargar hormigas nacidas/no nacidas
    public bool born { get; set; }


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
        Nest.RemoveEgg(id);
        Nest.antsBringingQueenFood.Remove(id);
        if (FlyCamera.SelectedAnt != null)
            if (FlyCamera.SelectedAnt.id == id)
                FlyCamera.SelectedAnt = null;

        WorldGen.updateAntCounter = true;

    }

    // Start is called before the first frame update
    void Start()
    {

        Rigidbody = antObj.GetComponent<Rigidbody>(); //El rigidbody se registra
        Rigidbody.centerOfMass = new Vector3(0, 0.05f, 0); //aplicamos centro de masa
        Animator = antObj.GetComponent<Animator>(); //El Animator se registra
        Animator.enabled = true; //Se habilita el animator
        Animator.SetBool("grounded", true); //El estado por defecto se encuentra en la tierra
        if (!born)
        {
            Animator.speed = 0; //To pause it the speed is set to 0.
            antCapCollider.enabled = false;
        }
        else
        {
            eggAnim.SetActive(false);
            staticEgg.SetActive(false);
            Animator.SetBool("Born", true);
            Animator.speed = 1;
            antCapCollider.enabled = true;
        }

        eggAnimator = eggAnim.GetComponent<Animator>();

        UpdateHolding();
        SetWalking(false); //El estado por defecto no camina

        //Modificador del tamaño de la hormiga según su edad
        transform.localScale = Vector3.one * Mathf.Clamp01(0.25f + (0.75f * (age + ageUpdateCounter / 100f) / 200f));

        lastCube = Vector3Int.FloorToInt(transform.position);
        lastTaskType = TaskType.None;


        var builder = new BehaviourTreeBuilder();
        this.tree = builder
            .Sequence("Main")
                .Selector("Get task if none")
                    .Condition("I have a task?", t => { return !objective.isTaskType(TaskType.None); })

                    //If im holding food, bring to nest.
                    .Sequence("If carrying food bring to food chamber") //To do: expand this into giving food to larva?
                        .Condition("Carrying food check", t => IsHoldingFood())
                        .Do("Get task for carrying food", t => CarryFood())
                    .End()

                    //this exists to remove ant from the list of ants getting food for queen if it failed.
                    .Do("Remove ant from bringing food duty", t => { Nest.antsBringingQueenFood.Remove(id); return BehaviourTreeStatus.Failure; })

                    .Sequence("If carrying egg bring to egg chamber") //To do: expand this into giving food to larva?
                        .Condition("Carrying food check", t => IsHoldingEgg())
                        .Do("Get task for carrying food", t => CarryEgg())
                    .End()

                    .Do("Sense nearby task", t => SenseTask())
                    .Do("Get requested task", t => Nest.GetNestTask(antSurface, id, ref objective))

                    .Sequence("Go outside")
                        .Condition("Am inside of nest?", t => Nest.SurfaceInNest(antSurface))
                        .Do("Set to go outside", t => { objective = Task.GoOutsideTask(antSurface); Debug.Log("Task is go outside"); return BehaviourTreeStatus.Success; })
                    .End()

                    .Sequence("If outside start exploring")
                        .Condition("Exit if in nest", t => !Nest.SurfaceInNest(antSurface))
                        .Do("Set to explore", t => { objective = Task.ExploreTask(antSurface, transform.forward, out Counter); Debug.Log("Outside, gonna explore"); return BehaviourTreeStatus.Success; })
                    .End()
                .End()

                .Selector("Do tasks")

                    .Sequence("Pick up item routine")
                        .Condition("My task is picking up item?", t => objective.isTaskType(TaskType.GetCorn) || objective.isTaskType(TaskType.CollectFromCob) || objective.isTaskType(TaskType.GetEgg))
                        .Condition("Is my task valid", t => objective.isValid(ref objective))
                        .Sequence("Pick up sequence")
                            .Do("Go to item", t => FollowTaskPath())
                            .Do("Align with item", t => Align(objective.getPos()))
                            .Do("Wait for pickup", t => { /*Debug.Log("Waiting");*/ return BehaviourTreeStatus.Running; })
                        .End()
                    .End()


                    .Sequence("Dig routine")
                        .Condition("My task is digging?", t => objective.isTaskType(TaskType.Dig))
                        .Condition("Is my task valid", t => objective.isValid(ref objective))
                        .Sequence("Dig sequence")
                            .Do("Go to digPoint", t => FollowTaskPath())
                            .Do("Align with digPoint", t => Align(objective.getPos()))
                            .Do("Wait for dig", t => BehaviourTreeStatus.Running)
                        .End()
                    .End()

                    .Sequence("Just follow path")
                        .Condition("Is a path following task?", t => { return objective.isTaskType(TaskType.GoInside) || objective.isTaskType(TaskType.GoToChamber) || objective.isTaskType(TaskType.GoToTunnel) || objective.isTaskType(TaskType.GoOutside); })
                        //.Do(".", t => { Debug.Log("Hey i made it"); return BehaviourTreeStatus.Success; })
                        .Do("Follow objective path", t => FollowTaskPath())
                        .Do("Objective complete or failed", t => { objective = Task.NoTask(); /*Debug.Log("REACHED OR FAILED");*/ return BehaviourTreeStatus.Success; })
                    .End()

                    .Sequence("Explore")
                        .Condition("Is my task exploring?", t => objective.isTaskType(TaskType.Explore))
                        .Do("Follow objective path", t => FollowTaskPath())
                        .Do("Objective complete or failed", t => CheckExploreStatus())
                    .End()

                    .Sequence("Lost")
                        .Condition("Am i lost?", t => objective.isTaskType(TaskType.Lost))
                        .Condition("Make sure im ACTUALLY lost", t => AmIStillLost())
                        .Do("Stop brining queen food if i am", t=> { Nest.antsBringingQueenFood.Remove(id);  return BehaviourTreeStatus.Success; })
                        .Do("Follow objective path", t => FollowTaskPath())
                        .Do("Check if in nest", t => CheckLostStatus())
                    .End()

                    .Sequence("Waiting")
                        .Condition("Am i waiting?", t => objective.isTaskType(TaskType.Wait))
                        .Do("Decrease waiting counter", t => { Counter -= 1; return BehaviourTreeStatus.Success; })
                        .Selector("Terminar espera si contador es 0")
                            .Condition("contador es mayor que 0?", t => Counter > 0)
                            .Do("Terminar tarea de espera", t => { objective = Task.NoTask(); Counter = 0; return BehaviourTreeStatus.Success; })
                        .End()
                    .End()

                .End()

            .End()
            .Build();
    }


    private bool BringFoodToQueen() //returns true if the ant manages to get next step in bringing food to queen. false if not tasked or failed
    {
        if (!Nest.antsBringingQueenFood.Contains(id)) return false;

        int queenChamberIndex = Nest.GetFirstDugNestPartIndex(NestPart.NestPartType.QueenChamber);

        if (queenChamberIndex == -1) //if no dug chamber for queen, stop bringing and exit
        {
            Nest.antsBringingQueenFood.Remove(id); //relieve of duty if the queen chamber disapeared.
            return false;
        }

        if (Nest.SurfaceInNestPart(antSurface, queenChamberIndex)) //if in first queen chamber, put down
        {
            PutDown();
            return true;
        }
        else //if not in first queen chamber, go there
        {
            Debug.Log("3");
            objective = Task.GoToNestPartTask(antSurface, queenChamberIndex);

            if (objective.isTaskType(TaskType.Lost)) //If you didnt find path, stop bringing.
            {
                Nest.antsBringingQueenFood.Remove(id);
                return false;
            }

            return true;
        }
        
    }

    private BehaviourTreeStatus CarryFood()
    {
        if (!Nest.SurfaceInNest(antSurface)) //go to nest if not in nest.
        {
            Debug.Log("1");
            objective = Task.GoInsideTask(antSurface);
            //Debug.Log("Task is go inside");
            return BehaviourTreeStatus.Success;
        }

        if (BringFoodToQueen()) return BehaviourTreeStatus.Success;


        if (Nest.HasDugNestPart(NestPart.NestPartType.FoodChamber)) //if nest has food chamber
        {
            if (Nest.SurfaceInNestPart(antSurface, NestPart.NestPartType.FoodChamber))
            {
                Debug.Log("2");
                return PutDown();
            }
            else
            {
                Debug.Log("3");
                objective = Task.GoToNestPartTask(antSurface, NestPart.NestPartType.FoodChamber);
                //Debug.Log("Task is go to food chamber");
                return BehaviourTreeStatus.Success;
            }
        }
        else if (!Nest.SurfaceInNestPart(antSurface, NestPart.NestPartType.Tunnel)) //if not in tunnel
        {
            Debug.Log("4");
            return PutDown();
        }
        else
        {
            //Debug.Log("5");
            objective = Task.GoToAnyChamber(antSurface);
            return BehaviourTreeStatus.Success;
        }
    }

    private BehaviourTreeStatus CarryEgg()
    {
        if (!Nest.SurfaceInNest(antSurface)) //go to nest if not in nest.
        {
            objective = Task.GoInsideTask(antSurface);
            //Debug.Log("Task is go inside");
            return BehaviourTreeStatus.Success;
        }

        if (Nest.HasDugNestPart(NestPart.NestPartType.EggChamber)) //if nest has egg chamber
        {
            if (Nest.SurfaceInNestPart(antSurface, NestPart.NestPartType.EggChamber))
            {
                return PutDown();
            }
            else
            {
                objective = Task.GoToNestPartTask(antSurface, NestPart.NestPartType.EggChamber);
                //Debug.Log("Task is go to food chamber");
                return BehaviourTreeStatus.Success;
            }
        }
        else if (Nest.HasDugNestPart(NestPart.NestPartType.QueenChamber)) //if nest has queen chamber
        {
            if (Nest.SurfaceInNestPart(antSurface, NestPart.NestPartType.QueenChamber))
            {
                return PutDown();
            }
            else
            {
                objective = Task.GoToNestPartTask(antSurface, NestPart.NestPartType.QueenChamber);
                //Debug.Log("Task is go to food chamber");
                return BehaviourTreeStatus.Success;
            }

        }
        else if (!Nest.SurfaceInNestPart(antSurface, NestPart.NestPartType.Tunnel)) //if not in tunnel
        {
            return PutDown();
        }
        else
        {
            objective = Task.GoToAnyChamber(antSurface);
            return BehaviourTreeStatus.Success;
        }
    }
        
    private BehaviourTreeStatus PutDown()
    {
        //Debug.Log("Putting down");
        Animator.SetBool("Put down", true);
        return BehaviourTreeStatus.Success;
    }


    Vector3Int nextPosDraw = Vector3Int.zero;
    int ageUpdateCounter = 0;
    public int neededCounterDisplay = 0;
    // Update is called once per frame
    void FixedUpdate()
    {


        if (lastTaskType != objective.type)
        {
            resetGoal = true;
            lastTaskType = objective.type;
        }

        Debug.DrawLine(transform.position, goal, Color.yellow);

        if (!IsBeingHeld()) //Do not age when being held
        {
            //Sistema de edad
            ageUpdateCounter += 1;

            //the age accel/decel system.
            int neededCounter = 100;
            if (!born && antDictionary.Count <= 4)
                neededCounter = 20;
            else if (!born)
                neededCounter = 100 + antDictionary.Count * 3;

            //To check it in editor
            neededCounterDisplay = neededCounter;
            
            if (ageUpdateCounter > neededCounter)
            {
                age++;
                ageUpdateCounter = 0;
            }
            
            transform.localScale = Vector3.one * Mathf.Clamp01(0.25f + (0.75f * (age + ageUpdateCounter / 100f) / 200f));
        }


        //Añadido para poder supervisar el estado en el que se encuentra la hormiga.
        taskName = objective.TaskToString();

        if (Animator.speed == 0) //Cuando la hormiga no ha nacido aún:
        {
            Rigidbody.useGravity = true;
            if (age > 100)
            {
                Animator.speed = 1;
                staticEgg.SetActive(false);
                eggAnim.SetActive(true);
                eggAnimator.enabled = true;
                antCapCollider.enabled = true;
                //eggPos = eggAnim.transform.position;
                //eggDir = eggAnim.transform.eulerAngles;
                //Instead of recording its pos and updating it so it doesn't move with the ant, 
                //which has proven to be not ... , just remove it from heirarchy.
                eggAnim.transform.SetParent(null);

                Nest.RemoveEgg(id); //Remove from egg dictionnary
            }

        }
        //Si la hormiga ya ha nacido:
        else if (SenseGround(out int numHits, out bool[] rayCastHits, out float[] rayCastDist, out bool changedSurface))
        {

            if (changedSurface)
            {
                if (Nest.SurfaceInNest(antSurface))
                {
                    //Si hemos llegado al nido habiendo descubierto mazorcas, lo compartimos en el nido.
                    if (discoveredCobs.Count > 0)
                    {
                        Nest.KnownCornCobs.AddRange(discoveredCobs);
                        discoveredCobs = new();
                    }
                }
                else  if (MainMenu.GameSettings.gameMode != 0) //only place if in playing mode.
                    CubePaths.PlacePheromone(antSurface.pos);

                resetGoal = true;
            }


            Rigidbody.useGravity = false;

            DontTurn();
            SetWalking(false);

            
            var stateInfo = Animator.GetCurrentAnimatorStateInfo(0);
            if (stateInfo.IsTag("noMove") || MainMenu.GameSettings.gameMode == 0)
            {
                SetWalking(false);
                DontTurn();
            }
            else if (IsControlled)
            {
                AntInputs();
                objective = Task.NoTask();
            }
            else
            {
                tree.Tick(new TimeData(Time.deltaTime));
            }


            ApplyMovement(normalMedian, rayCastHits, rayCastDist);

            //CubePaths.DrawCube(nextPosDraw, Color.blue);
            //CubePaths.DrawCube(antSurface.pos, Color.black);
        }
        else
        {
            Rigidbody.useGravity = true;
            Ant.CatchOutOfBounds(this.gameObject);
        }

        if (!objective.isTaskType(TaskType.None)) Debug.DrawLine(transform.position, objective.getPos(), Color.black);

        //Desactivar el huevo unos segundos despues de nacer.
        if (eggAnim.activeSelf && age > 108)
            eggAnim.SetActive(false);

    }

    //Failure if lost path or wrong taskType. Success if reached end. Running if in progress.
    private BehaviourTreeStatus FollowTaskPath()
    {
        if (!objective.isTaskType(TaskType.None))
        {
            float dist = CubePaths.DistToPoint(transform.position, objective.getPos());
            if (dist < 1.5f && !objective.isTaskType(TaskType.GetCorn)) return BehaviourTreeStatus.Success;
            if (dist < 3f && objective.isTaskType(TaskType.CollectFromCob)) return BehaviourTreeStatus.Success;
            if (dist < 2f && objective.isTaskType(TaskType.GoToChamber)) return BehaviourTreeStatus.Success;

            BehaviourTreeStatus status = CubePaths.SetGoalFromPath(antSurface, transform.forward, ref objective, ref resetGoal, ref goal);

            if (status != BehaviourTreeStatus.Running)
            {
                DontTurn();
                SetWalking(false);
                return status;
            }

            FollowGoal(normalMedian, goal, 70f);
            return status;

        }
        else return BehaviourTreeStatus.Failure;

    }

    //Función de gestion de estado explore
    private BehaviourTreeStatus CheckExploreStatus()
    {
        //Si no se ha encontrado task cerca y por tanto task no ha cambiado
        if (SenseTask() == BehaviourTreeStatus.Failure)
        {
            Counter--;
            //Si hemos explorado la distancia que queríamos, volvemos a casa.
            if (Counter < 1)
            {
                Counter = 0; //Just to be extra sure.
                objective = Task.GoInsideTask(antSurface);
            }
            else
            {
                objective = Task.ExploreTask(antSurface, transform.forward, out int Unimportant);
            }
        }
        else Counter = 0;

        return BehaviourTreeStatus.Success;
    }

    private bool AmIStillLost()
    {
        //Si hemos llegado al nido ya no estamos perdidos.
        if (Nest.SurfaceInNest(antSurface))
        {
            //Debug.Log("In nest");
            Counter = 0;
            objective = Task.NoTask();
            return false;
        }
        return true;
    }

    private BehaviourTreeStatus CheckLostStatus()
    {

        Counter++;
        //Si llevamos ya un rato perdidos, igual nos hemos topado con un camino de pheromonas que nos lleva a casa.
        if (Counter > 10)
        {
            Counter = 0;
            objective = Task.GoInsideTask(antSurface);
        }
        else
            objective = Task.LostTask(antSurface, transform.forward);

        return BehaviourTreeStatus.Success;
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
        int numColliders = Physics.OverlapSphereNonAlloc(transform.position, 7, hitColliders, layermask);
        for (int i = 0; i < numColliders; i++)
        {
            sensedItems.Enqueue(hitColliders[i].gameObject, Vector3.Distance(hitColliders[i].transform.position, transform.position));
        }

        int minLength = int.MaxValue;
        int minDigPointScore = -1;
        Task newTask = Task.NoTask();
        bool foundDigPoint = false;
        while (sensedItems.Count > 0)
        {
            GameObject sensedItem = sensedItems.Dequeue();
            //Asegurarnos de que el objeto no ha sido eliminado.
            if (sensedItem == null) continue;
            //DigPoints tienen prioridad sobre comida. Si se mira un objeto no DigPoint habiendo detectado ya uno, se ignora el objeto actual
            if (sensedItem.gameObject.layer != 9 && foundDigPoint) continue;
            //Comida dentro de una cámara de comida no se recoge
            if (sensedItem.gameObject.layer == 10 && Nest.PointInNestPart(sensedItem.transform.position, NestPart.NestPartType.FoodChamber)) continue;
            //Solo a los que se puede llegar son considerados -> si el camino de un considerado es vacio, ya se está
            if (CubePaths.GetPathToPoint(antSurface, Vector3Int.RoundToInt(sensedItem.transform.position), 10, out List<CubePaths.CubeSurface> newPath))
            {
                int objLayer = sensedItem.gameObject.layer;
                if (objLayer == 9) //9 is digpoint layer
                {
                    Vector3Int pos = Vector3Int.RoundToInt(sensedItem.transform.position);

                    //Si ya una hormiga lo va a excavar saltarlo
                    if (Task.IsDigPointBeingDug(pos)) continue;

                    //Si es primera vez que encontramos digpoint, reseteamos el valor minimo de camino (Nos da igual que el del digpoint sea mayor que el menor de comidas encontrado)
                    if (!foundDigPoint) { foundDigPoint = true; minLength = int.MaxValue; }

                    int newScore = DigPoint.ReachableScore(pos);

                    //Thanks to this sistem, priorities are:
                    //1. Having a high reachable score
                    //2. Being a short path
                    if (newScore > minDigPointScore)
                    {
                        newTask = new Task(sensedItem, TaskType.Dig, newPath);
                        DigPoint.digPointDict[pos].antId = id; //Marcamos el id de la hormiga
                        minDigPointScore = newScore;
                        minLength = newPath.Count;
                    }
                    else if (newScore == minDigPointScore)
                    {
                        if (newPath.Count < minLength)
                        {
                            newTask = new Task(sensedItem, TaskType.Dig, newPath);
                            DigPoint.digPointDict[pos].antId = id; //Marcamos el id de la hormiga
                            minLength = newPath.Count;
                        }
                    }


                }
                else if (objLayer == 10) //10 is corn layer
                {
                    if (newPath.Count < minLength)
                    {
                        if (!sensedItem.TryGetComponent<Corn>(out var cornScript)) continue;

                        //Si ya una hormiga lo va a recoger
                        if (Task.IsCornBeingPickedUp(cornScript)) continue;
                        if (Nest.CornInAcceptableNestPart(cornScript.id)) continue;

                        newTask = new Task(sensedItem, TaskType.GetCorn, newPath);
                        cornScript.antId = id;

                    }
                }
                else if (objLayer == 11) //11 is cornCobLayer
                {
                    //make sure it has script.
                    if (!sensedItem.TryGetComponent<CornCob>(out var cobScript)) continue;
                    //Only count it if it has corn left
                    if (!cobScript.hasCorn()) continue;
                    //Set current pheromonePath to found corn!
                    if (newPath.Count < minLength) newTask = new Task(sensedItem, TaskType.CollectFromCob, newPath);

                    //añadimos el cob a descubiertos por hormiga.
                    discoveredCobs.Add(newTask.itemId);
                    //Lo enseñamos si está escondido
                    if (cobScript.IsHidden()) cobScript.Show();
                }
                else
                {
                    //Debug.Log("Wrong layer: " + sensedItem.gameObject.layer + " vs dipoint " + digPointMask + " vs food " + cornMask);
                }
            }
        }

        if (!newTask.isTaskType(TaskType.None))
        {
            objective = newTask;
            return BehaviourTreeStatus.Success;
        }
        return BehaviourTreeStatus.Failure;
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

        if (objective.isTaskType(TaskType.GetCorn) || objective.isTaskType(TaskType.CollectFromCob) || objective.isTaskType(TaskType.GetEgg))
        {
            Animator.SetBool("Pick up", true);
        }
        if (objective.isTaskType(TaskType.Dig))
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

    public void PickupEvent() //function called by the animation
    {

        Animator.SetBool("Pick up", false);

        //Debug.Log("I GOT TO PICK UP");
        if (objective.isTaskType(TaskType.None))
        {
            //Debug.Log("Fail");
            Animator.SetTrigger("Pick up fail");
            return;
        }

        if (objective.isValid(ref objective) && objective.isTaskType(TaskType.GetCorn))
        {
            //Debug.Log("Valid");
            GameObject food = objective.GetItem();
            SetToHold(food);
            Nest.RemovePip(objective.itemId); //remove pip from nest if in it
            UpdateHolding();
        }
        //gets the cornCob, then a random corn pip from the cob that becomes held.
        else if (objective.isValid(ref objective) && objective.isTaskType(TaskType.CollectFromCob))
        {
            //Debug.Log("Cob valid");
            GameObject cob = objective.GetItem();
            GameObject food = cob.GetComponent<CornCob>().getRandomCornObject();
            if (food != null)
                SetToHold(food); //CornPip is removed from cornCob here
            UpdateHolding();
        }
        if (objective.isValid(ref objective) && objective.isTaskType(TaskType.GetEgg))
        {
            //Debug.Log("Valid");
            GameObject egg = objective.GetItem();
            SetToHold(egg);
            Nest.RemoveEgg(objective.itemId); //remove pip from nest if in it
            UpdateHolding();
        }
        else
        {
            //Debug.Log("Not valid or changed i guess???");
            Animator.SetTrigger("Pick up fail");
        }

        WorldGen.updateCornCounter = true;
        WorldGen.updateAntCounter = true;

        objective = Task.NoTask();
    }

    public void PutDownAction()
    {
        Animator.SetBool("Put down", false);
        if (!Animator.GetBool("grounded")) return; //So as to not do stuff when in midair: not having valid surface

        Vector3 mouthPos = carriedObject.transform.position;

        bool inNest = Nest.PointInNest(transform.position);

        //carriedObject.
        foreach (Transform child in carriedObject.transform)
        {

            Corn corn = child.gameObject.GetComponent<Corn>();
            Ant antEgg = child.gameObject.GetComponent<Ant>();
            if (corn != null) Corn.PlaceCorn(antSurface, child.gameObject, this, inNest);
            else if (antEgg != null) Ant.PlaceAntEgg(antSurface, child.gameObject, this, inNest);

        }
        carriedObject.transform.DetachChildren();
        UpdateHolding();
        WorldGen.updateCornCounter = true;
        WorldGen.updateAntCounter = true;
    }

    private static void PlaceAntEgg(CubePaths.CubeSurface antSurface, GameObject eggObj, Ant ant, bool inNest)
    {
        Ant egg = eggObj.GetComponent<Ant>();
        if (egg == null)
        {
            Debug.Log("WRONG_______-----------------------------------------------------------------------");
            return;
        }

        egg.Rigidbody.isKinematic = false;
        if (inNest)
        {
            //Añadir pepita al nido. Si no se encuentra la hormiga en una cámara de comida, se encontrará en id -1 y tendrá que ser movido
            int nestPartId = Nest.GetSurfaceNestPartIndex(antSurface, NestPart.NestPartType.EggChamber);
            Nest.AddEgg(egg.id, nestPartId);
            Vector3 dir;
            if (nestPartId != -1)
            {
                Vector3 chamberCenter = Nest.NestParts[nestPartId].getStartPos();
                dir = (chamberCenter - egg.transform.position).normalized;
            }
            else
                dir = (ant.transform.up * 2 + ant.transform.position - egg.transform.position).normalized;

            while (!WorldGen.IsAboveSurface(egg.transform.position - dir * 0.3f))
            {
                egg.transform.position += dir * 0.3f;
            }
        }
    }

    public void UpdateHolding()
    {
        if (carriedObject.transform.childCount == 0) Animator.SetBool("Holding", false);
        else Animator.SetBool("Holding", true);
    }

    //Manual put down function when controlling ant
    public void LetGo()
    {
        if (IsHolding()) Animator.SetTrigger("Put down");
    }

    public bool IsHoldingEgg()
    {
        
        UpdateHolding();
        if (IsHolding())
            if (carriedObject.transform.GetComponentInChildren<Ant>() != null) return true;
        return false;
    }

    public bool IsHoldingFood()
    {
        
        UpdateHolding();
        if (IsHolding())
            if (carriedObject.transform.GetComponentInChildren<Corn>() != null) return true;
        return false;
    }


    public void SetToHold(GameObject obj)
    {
        Corn corn = obj.GetComponent<Corn>();
        if (corn != null) SetToHoldCorn(corn);
        Ant egg = obj.GetComponent<Ant>();
        if (egg != null)
            if (!egg.born) SetToHoldEgg(egg);
        
    }

    private void SetToHoldEgg(Ant egg)
    {
        egg.transform.SetParent(carriedObject.transform);
        egg.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
        egg.Rigidbody.isKinematic = true;
        egg.antCapCollider.enabled = false;
    }

    private void SetToHoldCorn(Corn corn)
    {
        //If it is attached to a cornCob, remove it from said dictionary.
        CornCob parentCob = null;
        if (corn.transform.parent != null) parentCob = corn.transform.parent.gameObject.GetComponent<CornCob>();
        if (parentCob != null)
            if (!parentCob.RemoveCorn(corn.id)) Debug.Log("I DID NOT MANAGE TO REMOVE FROM CORNCOB");
        

        corn.transform.SetParent(carriedObject.transform);
        Destroy(corn.gameObject.GetComponent<Rigidbody>()); //check if ant rigidbody has special settings
        corn.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
        BoxCollider box = corn.gameObject.GetComponent<BoxCollider>();
        if (box != null) box.enabled = false;
    }

    public void DigEvent()
    {
        Animator.SetBool("Dig", false);

        if (objective.isTaskType(TaskType.None)) return;
        //if (!objective.isTaskType(TaskType.DigPoint)) return;

        if (objective.isValid(ref objective))
        {
            DigPoint thePoint = objective.GetDigPoint();
            if (thePoint != null)
            {
                thePoint.Dig();
                DigPoint.digPointDict.Remove(Vector3Int.RoundToInt(thePoint.transform.position));
                Destroy(thePoint.gameObject);
            }
            objective = Task.NoTask();
        }
        else objective = Task.NoTask();
    }

    //La idea inicial es coger el plano x-z sobre el que se encuentra la hormiga, luego proyectar el punto del objeto pheromona sobre �l.
    //Dependiendo de donde se encuentra en el plano ajustar la direcci�n y decidir si moverse hacia delante.
    void FollowGoal(Vector3 hitNormal, Vector3 goal, float minAngle)
    {

        //Obtenemos los datos de distancia hacia la pheromona
        Vector3 relativeGoal = Rigidbody.transform.InverseTransformPoint(goal); //relative position of the goal to the ant
        relativeGoal.y = 0; //Para calcular la distancia en el plano horizontal se quita el valor y

        //MOST IMPORTANT CHANGE: MAKES EVERYTHING A LOT SMOOTHER.
        Vector3 goalVector = goal - transform.position;
        Vector3 proyectedGoal = Vector3.ProjectOnPlane(goalVector, hitNormal);
        Vector3 proyectedForward = Vector3.ProjectOnPlane(transform.forward, hitNormal);
        float horAngle = Vector3.Angle(proyectedGoal, proyectedForward);

        //Debug.DrawLine(transform.position, goal, Color.red, 0.35f);

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

    public void SetWalking(bool walk)
    {
        if (walk)
        {
            speed = speed_per_second * Time.fixedDeltaTime * transform.localScale.x;
            Animator.SetBool("walking", true);
        }
        else
        {
            speed = 0f;
            Animator.SetBool("walking", false);
        }
    }

    public void TurnRight()
    {
        Animator.SetInteger("turning", 1);
    }

    public void TurnLeft()
    {
        Animator.SetInteger("turning", -1);
    }

    public void DontTurn()
    {
        Animator.SetInteger("turning", 0);
    }

    private bool SenseGround(out int numHits, out bool[] rayCastHits, out float[] rayCastDist, out bool changedSurface)
    {
        Color hitColor;
        numHits = 0;
        normalMedian = Vector3.zero;
        //El orden de los raycasts es importante. Son atras derecha -> atras izquierda -> delante izquierda -> delante derecha -> centro
        float[] xPos = { sep, -sep, -sep, sep, 0 };
        float[] zPos = { -sep, -sep, sep, sep, 0 };
        float yPos = 0.5f;
        rayCastHits = new bool[] { false, false, false, false, false };
        rayCastDist = new float[] { 0f, 0f, 0f, 0f, 0f };
        Vector3 hitNormal = new Vector3(0, 0, 0);
        Vector3Int hitCubePos = new Vector3Int(0, 0, 0);
        int raycastLayer = (1 << 6); //layer del terreno
        for (int i = 0; i < xPos.Length; i++)
        {
            //HE ESTADO USANDO MAL ESTA FUNCIÓN. RAYCASTLAYER ESTABA FUNCIONANDO COMO MAXDISTANCE
            if (Physics.Raycast(GetRelativePos(xPos[i], yPos, zPos[i]), Rigidbody.rotation * new Vector3(0, yPos - 0.8f, 0), out RaycastHit hit, 0.8f, raycastLayer))
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
            //Debug.DrawRay(GetRelativePos(xPos[i], yPos, zPos[i]), Rigidbody.rotation * new Vector3(0, yPos - 0.8f, 0), hitColor);
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
        Rigidbody.AddForce(-surfaceNormalMedian * 40); //USES ADDFORCE INSTEAD OF GRAVITY TO AVOID SLOW EFFECT

        //ROTATE ANT
        Quaternion deltaRotation = Quaternion.Euler(new Vector3(0, Animator.GetInteger("turning") * degrees_per_second * Time.fixedDeltaTime, 0));
        Rigidbody.MoveRotation(Rigidbody.rotation * deltaRotation); //Rotate ant

        //MOVE ANT FORWARD
        Vector3 proyectedVector = Vector3.ProjectOnPlane(Rigidbody.rotation * Vector3.forward, surfaceNormalMedian); //Project movement over terrain
        //Rigidbody.position = Rigidbody.position + proyectedVector * speed; //Move forward
        Rigidbody.MovePosition(Rigidbody.position + proyectedVector * speed);

        //
        AdjustAntToGround(rayCastHits, rayCastDist, deltaRotation);

    }


    private void AdjustAntToGround(bool[] rayCastHits, float[] rayCastDist, Quaternion deltaRotation)
    {
        if (!rayCastHits[4]) // Si el raycast central no ve el terreno, hay que ajustar rápidamente su orientación en la dirección correcta hasta que si lo vea.
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
        else //Si el raycast central si ve terreno, la hormiga ajusta solo si la diferencia es notable entre los raycast de las esquinas esquinas
        {
            float xRotation = 0;
            float zRotation = 0;
            if (rayCastHits[0] && rayCastHits[1] && rayCastDist[0] < rayCastDist[1] * 1.8f) zRotation += tiltSpeed * 0.1f;
            if (rayCastHits[1] && rayCastHits[0] && rayCastDist[1] < rayCastDist[0] * 1.8f) zRotation -= tiltSpeed * 0.1f;
            if (rayCastHits[1] && rayCastHits[2] && rayCastDist[1] < rayCastDist[2] * 1.8f) xRotation += tiltSpeed * 0.1f;
            if (rayCastHits[2] && rayCastHits[1] && rayCastDist[2] < rayCastDist[1] * 1.8f) xRotation -= tiltSpeed * 0.1f;
            if (rayCastHits[2] && rayCastHits[3] && rayCastDist[2] < rayCastDist[3] * 1.8f) zRotation -= tiltSpeed * 0.1f;
            if (rayCastHits[3] && rayCastHits[2] && rayCastDist[3] < rayCastDist[2] * 1.5f) zRotation += tiltSpeed * 0.1f;
            if (rayCastHits[3] && rayCastHits[0] && rayCastDist[3] < rayCastDist[0] * 1.8f) xRotation -= tiltSpeed * 0.1f;
            if (rayCastHits[0] && rayCastHits[3] && rayCastDist[0] < rayCastDist[3] * 1.8f) xRotation += tiltSpeed * 0.1f;
            deltaRotation = Quaternion.Euler(new Vector3(xRotation, 0, zRotation));
            Rigidbody.MoveRotation(Rigidbody.rotation * deltaRotation);
        }
    }

    void AntInputs()
    {
        if (Input.GetKey(KeyCode.W)) SetWalking(true);
        else SetWalking(false);
        if (Input.GetKey(KeyCode.A)) TurnLeft();
        else if (Input.GetKey(KeyCode.D)) TurnRight();
        else DontTurn();
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

    public bool IsBeingHeld()
    {
        if (transform.parent != null) return true;
        else return false;
    }

    public int holderAntIndex()
    {
        Ant holder = GetHolder();
        if (holder != null)
        {
            return holder.id;
        }
        return -1;
    }

    public Ant GetHolder()
    {
        Transform parent = transform.parent;
        if (parent == null) return null;
        while (parent.parent != null)
        {
            parent = parent.parent;
        }
        if (parent != null) return parent.GetComponent<Ant>();
        return null;
    }

    public static void CatchOutOfBounds(GameObject ant)
    {
        if (!WorldGen.IsAboveSurface(ant.transform.position) || ant.transform.position.y < 0) //Si ha atravesado el suelo o está fuera del mapa
            {
                float minDist = float.MaxValue;
                Vector3 telePos = Vector3.back; //player cant place things at this coord.
                foreach (var part in Nest.NestParts)
                {
                    if (part.HasBeenDug() && part.HasBeenPlaced())
                    {
                        float distance = Vector3.Distance(part.getStartPos(), ant.transform.position);
                        if (distance < minDist)
                        {
                            minDist = distance;
                            telePos = part.getStartPos();
                        }
                    }
                }
                if (!telePos.Equals(Vector3.back)) ant.transform.position = telePos;
                else
                {
                    ant.transform.position = new(WorldGen.x_dim / 2, WorldGen.y_dim, WorldGen.z_dim / 2); //Set it to above enclosure
                }
            }
    }

}
