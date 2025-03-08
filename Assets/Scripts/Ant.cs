using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using static UnityEngine.ParticleSystem;
using UnityEngine.Rendering;
using static UnityEditor.Experimental.GraphView.GraphView;
using System;
using System.Linq;


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
    private int pathId = -1; //if -1, not following a path
    private Vector3 pheromoneGoal = Vector3.zero;
    private Vector3Int lastCube;
    private CubePheromone placedPher = null;
    public int stucktimer = 0;



    public enum AIState
    {
        Exploring,
        Following,
        Controlled,
        Passive
    }

    public AIState state = AIState.Following;
    //el animador
    private Animator Animator;
    public float speed_per_second = 2f;
    public float degrees_per_second = 67.5f;


    // Start is called before the first frame update
    void Start()
    {
        Physics.gravity = new Vector3(0, -15.0F, 0); //Se activa gravedad por defecto
        Rigidbody = antObj.GetComponent<Rigidbody>(); //El rigidbody se registra
        Animator = antObj.GetComponent<Animator>(); //El Animator se registra
        PherSenseRange = antObj.GetComponent<BoxCollider>(); //El boxCollider se registra
        Animator.SetBool("walking", false); //El estado por defecto no camina
        Animator.SetBool("grounded", true); //El estado por defecto se encuentra en la tierra
        Animator.enabled = true; //Se habilita el animator

        lastCube = Vector3Int.FloorToInt(transform.position);
    }

    private void Update()
    {
    }


    // Update is called once per frame
    void FixedUpdate()
    {
        SenseGround(out int numHits, out Vector3 normalMedian, out bool[] rayCastHits, out float[] rayCastDist, out Vector3Int hitCubePos, out Vector3 hitNormal);

        AIMovement(lastCube != hitCubePos, hitCubePos, hitNormal);
        
        DecideToPlacePheromone(rayCastHits, hitCubePos, hitNormal);

        ApplyMovement(normalMedian, rayCastHits, rayCastDist);
        
    }

    /*
    private void DecidePlacePheromone(bool[] rayCastHits, Vector3 hitNormal)
    {
        if (makingTrail && rayCastHits[4])
        {
            float distance = Vector3.Distance(placedPheromone.pos, antObj.transform.position);
            if (Animator.GetBool("walking") && ( distance > 3 || ( distance > 0.5 && Vector3.Angle(placedPheromone.upDir, hitNormal) > 30))) //Only place when in new spot
            {
                placedPheromone = Pheromone.PlacePheromone(origPheromone, antObj.transform.position, hitNormal, placedPheromone);
            }
        }
    }
    +*/
    public Vector3 getRelativePos(float x, float y, float z)
    {
        return Rigidbody.position + antObj.transform.rotation * new Vector3(x, y, z);
    }

    int senseTimer = 0;
    
    void AIMovement(bool movedCube, Vector3Int hitCube, Vector3 hitNormal) {

        if (movedCube && state != AIState.Controlled) //If changed cube check next goal
        {
            Debug.Log("Changed cube");
            pheromoneGoal = Vector3.zero;
            SensePheromones(hitCube, hitNormal);
        }

        if (pheromoneGoal != Vector3.zero && state == AIState.Following) FollowPheromone();
        else if (state == AIState.Exploring)
        {
            RandomMovement();
        }
        else if (state == AIState.Passive)
        {
            senseTimer++;
            Animator.SetInteger("turning", 0);
            speed = 0f;
            Animator.SetBool("walking", false);
            if (senseTimer == 100 && hitNormal != Vector3.zero)
            {
                SensePheromones(hitCube, hitNormal); //This might happen when hitNormal isn't sensed??
                senseTimer = 0; 
            }
        }
        //MODO CONTROLADO LO GESTIONA LA CÁMARA
        turn = Animator.GetInteger("turning") * degrees_per_second * Time.fixedDeltaTime;
    }

    bool specialHelp = false;

    void RandomMovement()
    {
        speed = speed_per_second * Time.fixedDeltaTime;
        Animator.SetBool("walking", true);
    }

    //La idea inicial es coger el plano x-z sobre el que se encuentra la hormiga, luego proyectar el punto del objeto pheromona sobre �l.
    //Dependiendo de donde se encuentra en el plano ajustar la direcci�n y decidir si moverse hacia delante.
    void FollowPheromone()
    {
        //Obtenemos los datos de distancia hacia la pheromona
        Vector3 relativeGoal = Rigidbody.transform.InverseTransformPoint(pheromoneGoal); //relative position of the goal to the ant
        float distance = Vector3.Magnitude(relativeGoal);
        relativeGoal.y = 0; //Para calcular la distancia en el plano horizontal se quita el valor y
        float horAngle = Vector3.Angle(Vector3.forward, relativeGoal);
        float horDistance = relativeGoal.magnitude;

        float minAngle = 30f;

        Debug.DrawLine(transform.position, transform.position + relativeGoal, Color.black, 0.35f);
        Debug.DrawRay(antObj.transform.position, (relativeGoal - antObj.transform.position)*2, Color.red);
    
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

    struct objectiveSurface
    {
        public CubePaths.cubeSurface objective;
        public CubePaths.cubeSurface firstStep;
        public objectiveSurface(CubePaths.cubeSurface newSurface, CubePaths.cubeSurface newStep)
        {
            objective = newSurface;
            firstStep = newStep;
        }
    }
    
    void DrawSurface(CubePaths.cubeSurface cubeSurface, Color color, int time)
    {
        for (int i = 0; i < 8; i++)
        {
            if (cubeSurface.surfaceGroup[i])
                Debug.DrawLine(cubeSurface.pos + Vector3.one/2, cubeSurface.pos + chunk.cornerTable[i], color, time);
        }
    }

    List<objectiveSurface> GetNextSurfaceRange(CubePaths.cubeSurface antSurface, List<objectiveSurface> currentRange, ref HashSet<CubePaths.cubeSurface> checkedSurfaces)
    {
        List<objectiveSurface> nextRange = new List<objectiveSurface>();

        //Si el rango está empezando se coge la superficie de la hormiga
        if (currentRange.Count == 0)
        {
            nextRange.Add(new objectiveSurface(antSurface, antSurface));
            checkedSurfaces.Add(antSurface);
            return nextRange;
        }

        //Si el rango es la superficie de la hormiga se cogen los adyacentes (para poner sus firststep)
        if(currentRange[0].objective.Equals(antSurface)) 
        {
            Debug.Log(currentRange[0].objective.pos + " vs " + currentRange[0].firstStep.pos);
            if (currentRange.Count != 1) Debug.Log("YOU FUCKED UPPPPP-------------------------");

            List<CubePaths.cubeSurface> adyacentCubes = CubePaths.GetAdyacentCubes(currentRange[0].objective, transform.forward);
            foreach (var son in adyacentCubes)
            {
                nextRange.Add(new objectiveSurface(son, son));
                checkedSurfaces.Add(son);
            }
            return nextRange;
        }


        //Si el rango es mayor que todo eso se procede como debido
        foreach (var currentSurface in currentRange)
        {
            List<CubePaths.cubeSurface> adyacentCubes = CubePaths.GetAdyacentCubes(currentSurface.objective, transform.forward);
            
            foreach (var son in adyacentCubes)
            {
                if (!checkedSurfaces.Contains(son))
                {
                    nextRange.Add(new objectiveSurface(son, currentSurface.firstStep));
                    checkedSurfaces.Add(son);
                }
            }
        }
        return nextRange;
    }


    private void SensePheromones(Vector3Int currentCube, Vector3 hitNormal) 
    {
        CubePaths.cubeSurface antSurface = new CubePaths.cubeSurface(currentCube, CubePaths.CornerFromNormal(hitNormal));
        List<objectiveSurface> sensedRange = new List<objectiveSurface>();
        HashSet<CubePaths.cubeSurface> checkedSurfaces = new HashSet<CubePaths.cubeSurface>();
        bool haveGoal = false;
        int range = -1;
        CubePheromone objectivePher = null;

        Color[] colors = {Color.blue, Color.magenta, Color.red, Color.black, Color.blue, Color.black, Color.blue, Color.black, Color.blue, Color.black, Color.blue};

        while (!haveGoal && range < 5)
        {
            range++;
            sensedRange = GetNextSurfaceRange(antSurface, sensedRange, ref checkedSurfaces);

            //Put all pheromones on a list
            List<CubePheromone> sensedPheromones = new List<CubePheromone>();
            foreach (var surface in sensedRange)
            {
                CubePaths.DrawCube(surface.objective.pos, colors[range], 2);
                if (CubePaths.cubePherDict.TryGetValue(surface.objective.pos, out List<CubePheromone> surfacePhers))
                    sensedPheromones.AddRange(surfacePhers);
            }
            //

            objectivePher = ChoosePheromone(sensedPheromones);
            if (objectivePher != null) haveGoal = true;
        }

        //Si no se ha encontrado objetivo, nos volvemos pasivos.
        if (!haveGoal)
        {
            state = AIState.Passive;
            Debug.Log("NO PHEROMONES FOUND/CHOSEN");
            return;
        }

        //Si la pheromona está en la superficie actual seguimos su camino
        if (range == 0)
        {
            if (objectivePher.isLast(followingForwards)) followingForwards = !followingForwards;
            if (objectivePher.isLast(followingForwards))
            {
                state = AIState.Passive;
                Debug.Log("SINGLE PHEROMONE PATH; IM FUCKING STUCKKKKK");
                return;
            }

            objectivePher = objectivePher.GetNext(followingForwards);
        }
        
        
        Vector3Int dir = objectivePher.GetPos() - antSurface.pos;
        pheromoneGoal = CubePaths.GetMovementGoal(antSurface.pos, antSurface.surfaceGroup, dir);
        state = AIState.Following;


    }

    

    private CubePheromone ChoosePheromone(List<CubePheromone> sensedPhers)
    {
        CubePheromone chosenPheromone = null;
        int pathVal = 0;
        if (!followingForwards) pathVal = int.MaxValue;

        bool isFurther(int value)
        {
            if (followingForwards) return pathVal < value;
            else return pathVal > value;
        }

        for (int i = 0; i < sensedPhers.Count(); i++)
        {
            if (pathId != -1)
            {
                if (sensedPhers[i].GetPathId() == pathId)
                    if (isFurther(sensedPhers[i].GetPathPos()))
                    {
                        chosenPheromone = sensedPhers[i];
                        pathVal = sensedPhers[i].GetPathPos();
                    }
            }
            else
            {
                if (isFurther(sensedPhers[i].GetPathPos()))
                {
                    chosenPheromone = sensedPhers[i];
                    pathVal = sensedPhers[i].GetPathPos();
                }
            }
        }
        return chosenPheromone;
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

    //ERROR: The raycasts go way to far, making the ant put down pheromones on the wrong places.
    private void SenseGround(out int numHits, out Vector3 normalMedian, out bool[] rayCastHits, out float[] rayCastDist, out Vector3Int hitCubePos, out Vector3 hitNormal)
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
        hitNormal = new Vector3(0, 0, 0); 
        hitCubePos = new Vector3Int(0,0,0);
        int raycastLayer = (1 << 6); //layer del terreno
        for (int i = 0; i < xPos.Length; i++) {
            //HE ESTADO USANDO MAL ESTA FUNCIÓN. RAYCASTLAYER ESTABA FUNCIONANDO COMO MAXDISTANCE
            if (Physics.Raycast(getRelativePos(xPos[i], yPos, zPos[i]), Rigidbody.rotation * new Vector3(0, yPos - 0.8f, 0),  out RaycastHit hit, 0.8f, raycastLayer))
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
            Debug.DrawRay(getRelativePos(xPos[i], yPos, zPos[i]), Rigidbody.rotation * new Vector3(0, yPos - 0.8f, 0), hitColor);
        }

        //REMEMBER TO COMMENT HOW YOU CHANGED FROM LOCAL BOOL TO THE ANIMATOR ONE
        if (numHits > 0)
        {
            Animator.SetBool("grounded", true);
        }
        else
        {
            Animator.SetBool("grounded", false);
        }
        if (numHits != 0) normalMedian /= numHits;
    }

    private void ApplyMovement(Vector3 surfaceNormalMedian, bool[] rayCastHits, float[] rayCastDist)
    {
        if (Animator.GetBool("grounded"))
        {
            //REMOVE MOVEMENT EFFECTS
            Rigidbody.angularVelocity = Vector3.zero;
            Rigidbody.AddForce(-surfaceNormalMedian*40); //USES ADDFORCE INSTEAD OF GRAVITY TO AVOID SLOW EFFECT
            Physics.gravity = Vector3.zero;

            //ROTATE ANT
            Quaternion deltaRotation = Quaternion.Euler(new Vector3(0,turn,0));
            Rigidbody.MoveRotation(Rigidbody.rotation * deltaRotation); //Rotate ant

            //MOVE ANT FORWARD
            Vector3 proyectedVector = Vector3.ProjectOnPlane(Rigidbody.rotation * Vector3.forward, surfaceNormalMedian); //Project movement over terrain
            Rigidbody.position = Rigidbody.position + proyectedVector * speed; //Move forward

            AdjustAntToGround(rayCastHits, rayCastDist, deltaRotation);

        }
        //Si no esta grounded
        else
        {
            Physics.gravity = new Vector3(0, -15.0F, 0);
        }
    }

    private void DecideToPlacePheromone(bool[] rayCastHits, Vector3Int hitCubePos, Vector3 hitNormal)
    {
        //DECIDE SI PONER PHEROMONA AAAAAAAAAAAAAAA---------------------------------------------------------
        if (makingTrail && rayCastHits[4]) //si se crea camino y el raycast principal ve suelo
        {
            if (lastCube != hitCubePos)
            {
                if (placedPher == null) //La hormiga no ha empezado aún su camino
                {
                    placedPher = CubePaths.StartPheromoneTrail(hitCubePos, hitNormal);
                }
                else if (CubePaths.DoesSurfaceConnect(placedPher.GetPos(), placedPher.GetSurfaceGroup(), hitCubePos)) //Si se ha llegado a un nuevo cubo adyacente
                {   //Might somehow fuck up if ant moves to adyacent cube on unreachable surface SOMEHOW. 
                    placedPher = CubePaths.ContinuePheromoneTrail(hitCubePos, placedPher); 
                }
                else //Si la hormiga se ha separado de su anterior camino
                {
                    placedPher = CubePaths.StartPheromoneTrail(hitCubePos, hitNormal);
                }
            }
        }
        lastCube = hitCubePos;
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


}
