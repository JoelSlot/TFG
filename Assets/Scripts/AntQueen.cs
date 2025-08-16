using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using Utils;
using FluentBehaviourTree;
using Unity.VisualScripting;
using System;
using System.Net;


public class AntQueen : MonoBehaviour
{
    public GameObject queenObj;
    public Rigidbody Rigidbody;
    public CapsuleCollider terrainCapCollider;
    public CapsuleCollider antCapCollider;
    public GameObject carriedObject; // the head bone
    public GameObject abdomenBone; //the abdomen bone
    public string taskName = "";
    //public GameObject staticEgg;
    //public GameObject eggAnim;
    //private Animator eggAnimator;


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

    public static HashSet<AntQueen> antQueenSet = new();


    //Datos que hay que guardar y cargar
    public Task objective = Task.NoTask();
    public int Counter = 0; //Counter of how long the ant is lost before checking if it can go home.
    public int Energy = 0; //increases when eating, decreases to give birth


    //Static values
    public static float speed_per_second = 2f;
    public static float degrees_per_second = 67.5f;
    public static float speed = 0;
    public static float tiltSpeed = 10;
    public static float sep = 0.35f;


    public bool IsHolding()
    {
        return Animator.GetBool("Holding");
    }


    // Start is called before the first frame update
    void Start()
    {

        Rigidbody = queenObj.GetComponent<Rigidbody>(); //El rigidbody se registra
        Rigidbody.centerOfMass = new(0, 0.05f, 0);
        Animator = queenObj.GetComponent<Animator>(); //El Animator se registra
        Animator.enabled = true; //Se habilita el animator
        Animator.SetBool("grounded", true); //El estado por defecto se encuentra en la tierra


        UpdateHolding();
        SetWalking(false); //El estado por defecto no camina

        lastCube = Vector3Int.FloorToInt(transform.position);
        lastTaskType = TaskType.None;

        var builder = new BehaviourTreeBuilder();
        this.tree = builder
            .Sequence("Main")

                .Selector("Decide what to do")
                    .Condition("Exit if doing something", t => !objective.isTaskType(TaskType.None))

                    .Sequence("Chill if in queen chamber")
                        .Condition("Exit if not in queen chamber", t => Nest.SurfaceInNestPart(antSurface, Nest.GetFirstDugNestPartIndex(NestPart.NestPartType.QueenChamber)))
                        .Do("Either walk, eat, give birth or chill", t => GetQueenChamberTask(antSurface, ref objective))
                    .End()

                    .Sequence("If not in chamber, go there or do other stuff.")
                        .Condition("Exit if there isn't a queen chamber", t => Nest.HasDugNestPart(NestPart.NestPartType.QueenChamber))
                        .Do("Set task to go to queen chamber", t => { objective = Task.GoToNestPartTask(antSurface, Nest.GetFirstDugNestPartIndex(NestPart.NestPartType.QueenChamber)); return BehaviourTreeStatus.Success; })
                    .End()

                    .Do("Get nearby dig task", t => SenseDigTask())
                    .Condition("Get a task from the nest", t => Nest.GetNestDigTask(antSurface, -2, ref objective))
                    .Do("Did not find anything to do", t => { Debug.Log("Didnt find stuff to do as queen"); return BehaviourTreeStatus.Success; })

                .End()
                .Selector("Do tasks")

                    .Sequence("Eat routine")
                        .Condition("My task is eating?", t => objective.isTaskType(TaskType.Eat))
                        .Condition("Is my task valid", t => objective.isValid(ref objective))
                        .Sequence("Eat sequence")
                            .Do("Go to food", t => FollowTaskPath())
                            .Do("Align with food", t => Align(objective.getPos()))
                            .Do("Wait eating", t => BehaviourTreeStatus.Running)
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
                        .Do("Follow objective path", t => FollowTaskPath())
                        .Do("Check if in nest", t => CheckLostStatus())
                    .End()

                    .Sequence("Waiting")
                        .Condition("Am i waiting?", t => objective.isTaskType(TaskType.Wait))
                        .Do("Decrease waiting counter", t => { Counter -= 1; return BehaviourTreeStatus.Success; })
                        .Selector("Terminar espera si contador es 0")
                            .Condition("Exit if counter is above 0", t => Counter > 0)
                            .Do("Eat if there's food, otherwise get noTask", t => GetEatFoodInChamberTask())
                        .End()
                    .End()

                .End()

            .End()
            .Build();
    }


    // Update is called once per frame
    void FixedUpdate()
    {

        if (lastTaskType != objective.type)
        {
            resetGoal = true;
            lastTaskType = objective.type;
        }

        Debug.DrawLine(transform.position, goal, Color.yellow);


        //Añadido para poder supervisar el estado en el que se encuentra la hormiga.
        taskName = objective.TaskToString();

        if (SenseGround(out int numHits, out bool[] rayCastHits, out float[] rayCastDist, out bool changedSurface))
        {

            if (changedSurface)
                resetGoal = true;

            Rigidbody.useGravity = false;

            DontTurn();
            SetWalking(false);


            var stateInfo = Animator.GetCurrentAnimatorStateInfo(0);
            if (stateInfo.IsTag("noMove"))
            {
                //DigEvent
                //nothing to do here. The ant didnt do anything on dig, but that was because i forgot
                //To copy the pickup anim into a dig anim again. whoops.
                //After copying pickup and adding it as dig, added event
                //Shortenede anim but looked bad, so readjusted it
                //then had to move the event again because adjusting anim length changes event time.
            }
            else tree.Tick(new TimeData(Time.deltaTime));



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
            if (dist < 2f && objective.isTaskType(TaskType.Dig)) return BehaviourTreeStatus.Success;

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
        if (SenseDigTask() == BehaviourTreeStatus.Failure)
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

    private BehaviourTreeStatus SenseDigTask()
    {
        int digPointMask = 1 << 9;

        PriorityQueue<DigPoint, float> sensedItems = new();
        int maxColliders = 100;
        Collider[] hitColliders = new Collider[maxColliders];
        int numColliders = Physics.OverlapSphereNonAlloc(transform.position, 7, hitColliders, digPointMask);
        for (int i = 0; i < numColliders; i++)
        {
            sensedItems.Enqueue(hitColliders[i].gameObject.GetComponent<DigPoint>(), Vector3.Distance(hitColliders[i].transform.position, transform.position));
        }

        int minLength = int.MaxValue;
        int minDigPointScore = -1;
        Task newTask = Task.NoTask();
        while (sensedItems.Count > 0)
        {
            DigPoint sensedPoint = sensedItems.Dequeue();
            //Solo a los que se puede llegar son considerados -> si el camino de un considerado es vacio, ya se está
            if (CubePaths.GetPathToPoint(antSurface, Vector3Int.RoundToInt(sensedPoint.transform.position), 10, out List<CubePaths.CubeSurface> newPath))
            {
                Vector3Int pos = Vector3Int.RoundToInt(sensedPoint.transform.position);
                if (Task.IsDigPointBeingDug(pos)) continue;

                int newScore = DigPoint.ReachableScore(pos);

                //Thanks to this sistem, priorities are:
                //1. Having a high reachable score
                //2. Being a short path
                if (newScore > minDigPointScore)
                {
                    newTask = Task.DigTask(pos, -1, newPath);
                    minDigPointScore = newScore;
                    minLength = newPath.Count;
                }
                else if (newScore == minDigPointScore)
                {
                    if (newPath.Count < minLength)
                    {
                        newTask = Task.DigTask(pos, -1, newPath);
                        minLength = newPath.Count;
                    }

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

        if (objective.isTaskType(TaskType.Eat))
        {
            Animator.SetBool("Eat", true);
        }
        if (objective.isTaskType(TaskType.Dig))
        {
            Animator.SetBool("Dig", true);
        }

        return BehaviourTreeStatus.Success;
    }

    void OnDestroy()
    {
        Debug.Log("Destroyed me");
        antQueenSet.Remove(this);
    }

    public Vector3 GetRelativePos(float x, float y, float z)
    {
        return Rigidbody.position + queenObj.transform.rotation * new Vector3(x, y, z);
    }

    public void EatEvent() //function called by the animation
    {
        Animator.SetBool("Eat", false);

        //Debug.Log("I GOT TO PICK UP");
        if (!objective.isTaskType(TaskType.Eat))
        {
            //Debug.Log("Fail");
            Animator.SetTrigger("Pick up fail");
            return;
        }

        if (objective.isValid(ref objective))
        {
            //Debug.Log("Valid");
            GameObject food = objective.GetItem();
            Destroy(food); //corn destroy removes it from dictionaries
            Energy++;
        }

        objective = Task.NoTask();
    }

    public bool GiveBirth()
    {
        if (Energy < 5) return false;
        else Energy -= 5;

        Vector3 eggPos = abdomenBone.transform.position;

        Ant newBorn = WorldGen.InstantiateAnt(eggPos, transform.rotation, false);

        if (Nest.PointInNest(transform.position))
        {
            int nestPartId = Nest.GetSurfaceChamberIndex(antSurface);

            Nest.AddEgg(newBorn.id, nestPartId);
            if (nestPartId != -1)
            {
                Vector3 chamberCenter = Nest.NestParts[nestPartId].getStartPos();
                Vector3 dir = (chamberCenter - eggPos).normalized;
                while (!WorldGen.IsAboveSurface(newBorn.transform.position - dir * 0.3f) && !WorldGen.IsAboveSurface(newBorn.transform.position))
                {
                    newBorn.transform.position += dir * 0.3f;
                }
            }
            else
            {
                Vector3 dir = (transform.up * 2 + transform.position - eggPos).normalized;
                while (!WorldGen.IsAboveSurface(newBorn.transform.position - dir * 0.3f) && !WorldGen.IsAboveSurface(newBorn.transform.position))
                {
                    newBorn.transform.position += dir * 0.3f;
                }
            }
        }

        return true;
    }

    public void PutDownAction()
    {
        Animator.SetBool("Put down", false);
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

    public void DigEvent()
    {
        Animator.SetBool("Dig", false);

        if (objective.isTaskType(TaskType.None)) return;

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
            speed = speed_per_second * Time.fixedDeltaTime;
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



    public AnimatorStateInfo GetAnimatorStateInfo()
    {
        return Animator.GetCurrentAnimatorStateInfo(0);
    }


    private BehaviourTreeStatus GetQueenChamberTask(CubePaths.CubeSurface antSurface, ref Task objective)
    {
        //Just in case we somehow call this function when not in first dug queen chamber
        int nestPartId = Nest.GetFirstDugNestPartIndex(NestPart.NestPartType.QueenChamber);
        if (nestPartId != -1)
            if (GiveBirth())
            {
                objective = Task.GoToNestPartTask(antSurface, Nest.GetFirstDugNestPartIndex(NestPart.NestPartType.QueenChamber));
                if (objective.isTaskType(TaskType.Lost)) objective = Task.WaitTask(this, UnityEngine.Random.Range(100, 200));
                return BehaviourTreeStatus.Success;
            }

        objective = Task.WaitTask(this, UnityEngine.Random.Range(500, 1500));

        return BehaviourTreeStatus.Success;

    }

    private BehaviourTreeStatus GetEatFoodInChamberTask()
    {
        int firstQueenChamberIndex = Nest.GetFirstDugNestPartIndex(NestPart.NestPartType.QueenChamber);
        if (Nest.SurfaceInNestPart(antSurface, firstQueenChamberIndex))
        {
            foreach (int cornId in Nest.NestParts[firstQueenChamberIndex].CollectedCornPips) //for each pip in chamber
            {
                //if already being picked up go to next
                if (Task.IsCornBeingPickedUp(cornId)) continue;
                if (CubePaths.GetKnownPathToPoint(antSurface, Corn.cornDictionary[cornId].transform.position, 1.2f, out List<CubePaths.CubeSurface> newPath))
                {
                    objective = Task.GetEatTask(cornId, -2, newPath);
                    return BehaviourTreeStatus.Success;
                }
            }
        }
        objective = Task.NoTask();
        return BehaviourTreeStatus.Success;
    }

    


}
