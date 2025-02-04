using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using static UnityEngine.ParticleSystem;
using UnityEngine.Rendering;
using static UnityEditor.Experimental.GraphView.GraphView;
using System;


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
    public GameObject origPheromone;
    public bool makingTrail = false;
    private UnityEngine.Quaternion prevUpAngle; //Previous angle of pheromone
    public Pheromone placedPheromone = null;
    public Pheromone followingPheromone = null;
    public Pheromone lastSteppedPheromone = null;
    private bool followingForwards = false;
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
    }

    private void Update()
    {
    }


    // Update is called once per frame
    void FixedUpdate()
    {
        AIMovement();

        turn = Animator.GetInteger("turning") * degrees_per_second * Time.fixedDeltaTime;

        Color hitColor;
        int numHits = 0;
        Vector3 normalMedian = Vector3.zero;
        //El orden de los raycasts es importante. Son atras derecha -> atras izquierda -> delante izquierda -> delante derecha -> centro
        float[] xPos = {sep, -sep, -sep, sep, 0};
        float[] zPos = {-sep, -sep, sep, sep, 0};
        float yPos = 0.5f;
        bool[] rayCastHits = { false, false, false, false, false};
        float[] rayCastDist = {0f,0f,0f,0f,0f};
        Vector3 hitNormal = new Vector3(0, 0, 0); 
        int raycastLayer = (1 << 6); //layer del terreno
        for (int i = 0; i < xPos.Length; i++) {
            if (Physics.Raycast(getRelativePos(xPos[i], yPos, zPos[i]), Rigidbody.rotation * new Vector3(0, yPos - 0.8f, 0),  out RaycastHit hit, raycastLayer))
            {
                hitColor = Color.red;
                numHits++;
                normalMedian += hit.normal;
                rayCastHits[i] = true;
                rayCastDist[i] = hit.distance;
                hitNormal = hit.normal;
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

        if (Animator.GetBool("grounded"))
        {
            //Rigidbody.velocity = Vector3.zero;
            Rigidbody.angularVelocity = Vector3.zero;
            Rigidbody.AddForce(-normalMedian*40); //USES ADDFORCE INSTEAD OF GRAVITY TO AVOID SLOW EFFECT
            Physics.gravity = Vector3.zero;

            Quaternion deltaRotation = Quaternion.Euler(new Vector3(0,turn,0));
            Rigidbody.MoveRotation(Rigidbody.rotation * deltaRotation); //Rotate ant

            //Cuando la hormiga no detecta terreno con su raycast principal, es rotado hacia el terreno seg�n los raycasts que aun detectan superficies
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
            Vector3 proyectedVector = Vector3.ProjectOnPlane(Rigidbody.rotation * Vector3.forward, normalMedian); //Project movement over terrain
            Rigidbody.position = Rigidbody.position + proyectedVector * speed; //Move forward

        }
        //Si no esta grounded
        else
        {
            Physics.gravity = new Vector3(0, -15.0F, 0);
        }
        

        //Debug.Log("Walking: " + Animator.GetBool("walking") + ", falling: " + Animator.GetBool("falling"));

        
        //Deciding and executing the placement of a new pheromone node.
        if (makingTrail && rayCastHits[4])
        {
            float distance = Vector3.Distance(placedPheromone.pos, antObj.transform.position);
            if (Animator.GetBool("walking") && ( distance > 3 || ( distance > 0.5 && Vector3.Angle(placedPheromone.upDir, hitNormal) > 30))) //Only place when in new spot
            {
                placedPheromone = Pheromone.PlacePheromone(origPheromone, antObj.transform.position, hitNormal, placedPheromone);
            }
        }

    }

    public Vector3 getRelativePos(float x, float y, float z)
    {
        return Rigidbody.position + antObj.transform.rotation * new Vector3(x, y, z);
    }

    int senseTimer = 0;
    
    void AIMovement() {
        if (state == AIState.Following)
        {
            if (followingPheromone == null) SensePheromones();
            if (followingPheromone != null)
                FollowPheromone();
            else
            {
                Animator.SetInteger("turning", 0);
                speed = 0f;
                Animator.SetBool("walking", false);
                state = AIState.Passive;
            }
        }
        else if (state == AIState.Exploring)
        {
            RandomMovement();
        }
        else if (state == AIState.Passive)
        {
            if (senseTimer == 0)
            {
                SensePheromones();
                if (followingPheromone != null) state = AIState.Following;
            }
            senseTimer += 1;
            if (senseTimer > 10) senseTimer = 0;
        }
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
        //Si no hay pheromona que seguir, la hormiga se queda quieta
        if (followingPheromone == null)
        {
            speed = 0f;
            Animator.SetBool("walking", false);
            return;
        }

        float distance = Vector3.Magnitude(followingPheromone.pos - antObj.transform.position);
        float neededUpAngle = 35f;
        float upAngle = Vector3.Angle(antObj.transform.up, followingPheromone.upDir);

        if ((distance < 3 && !followingPheromone.IsEnd(followingForwards) && upAngle < neededUpAngle/2) || (distance < 1f && distance > 0.1f && upAngle < neededUpAngle) || (distance <= 0.1f))//Si la pheromona se encuentra cerca
        {

            SetWalking(false);
            if (followingPheromone.GetNext(followingForwards, out Pheromone nextPher))
            {
                followingPheromone = nextPher;
                FollowPheromone();

            }
            else
            { // llegar al final
                if (followingPheromone.GetNext(!followingForwards, out Pheromone prevPher)) //Si hay pheromona previa
                {
                    followingPheromone = prevPher;
                    followingForwards = !followingForwards;
                    FollowPheromone();
                }
                else
                {
                    state = AIState.Passive;
                    followingPheromone = null;
                }
            }
            return;
        }


        Vector3 objective = followingPheromone.pos;

        //if (specialHelp)
        /*if (Vector3.Distance(antObj.transform.position, objective) < 0.5f && upAngle > neededUpAngle)
        {
            
            if (Vector3.Dot(objective - antObj.transform.position, followingPheromone.upDir) < 0) //si se miran los vectores
            {
                objective = followingPheromone.pos + (followingPheromone.upDir*-100);
                Debug.DrawRay(followingPheromone.pos, followingPheromone.upDir*-10, Color.magenta, 5);
            }
            else
            {
                objective = followingPheromone.upDir + (followingPheromone.upDir*100);
                Debug.DrawRay(followingPheromone.pos, followingPheromone.upDir*10, Color.magenta, 5f);
            }
        }*/


        //Obtenemos los datos de distancia hacia la pheromona
        Vector3 pherRel = Rigidbody.transform.InverseTransformPoint(objective); //relative position of the pheromone to the ant
        distance = Vector3.Magnitude(pherRel);
        float yDist = pherRel.y; 
        pherRel.y = 0; //Para calcular la distancia en el plano horizontal se quita el valor y
        float horAngle = Vector3.Angle(Vector3.forward, pherRel);
        float horDistance = pherRel.magnitude;

        float minAngle = 30f;

        Debug.DrawLine(transform.position, objective, Color.black, 0.35f);
        //Debug.DrawRay(antObj.transform.position, Vector3.forward*2, Color.red);
        //Debug.DrawLine(antObj.transform.position, pherRel, Color.blue);
        Debug.DrawRay(antObj.transform.position, (objective - antObj.transform.position)*2, Color.red);
        Debug.DrawRay(antObj.transform.position, followingPheromone.upDir*2, Color.blue);
    
        //Decidir si girar
        if (horAngle > 5)
        {
            if (pherRel.x > 0) TurnRight();
            else TurnLeft();
        }
        else DontTurn();

        //Decidir si avanzar
        if ((distance > 1 && horAngle < minAngle) || (distance < 1 && horAngle < 5)) SetWalking(true);
        else SetWalking(false);
        Debug.Log("Angle: " + horAngle + ",\t" + upAngle + "\t Distance: " + distance);
    

    }

    private void SensePheromones() 
    {
        int layerMask = 1 << 8;
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, 4, layerMask);
        for (int i = 0; i < hitColliders.Length; i++){
            Collider hitCollider = hitColliders[i];

            if (hitCollider != null)
            {
                followingPheromone = hitCollider.GetComponent<Pheromone>();
                return;
            }
        }
        
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

}
