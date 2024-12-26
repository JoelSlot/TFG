using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using static UnityEngine.ParticleSystem;
using UnityEngine.Rendering;
using static UnityEditor.Experimental.GraphView.GraphView;
using System;


public class AntTest : MonoBehaviour
{

    public GameObject Ant;

    public Rigidbody Rigidbody;
    public BoxCollider PherSenseRange;
    public float speed = 0;
    public float turn = 1;
    public float tiltSpeed = 10;
    public float sep = 0.35f;

    //Variables for pheromone paths
    private int pathId = -1; //Id that path being created
    private int pathPos = -1; //Position of path being created
    private int followId = -1; //Id of path being followed
    private int followPos = -1; //position of path being followed
    private UnityEngine.Quaternion prevUpAngle; //Previous angle of pheromone
    public GameObject origPheromone;
    public GameObject placedPheromone = null;
    public GameObject followingPheromone = null;
    public Pheromone Pheromone; //the script file of the original pheromone that will be used to access functions.
    public bool followingForwards = true;
    private Vector3Int prevPherPos; //position of previously placed pheromone

    public enum AIState
    {
        Exploring,
        Following,
        Controlled
    }

    public AIState state = AIState.Following;
    //el animador
    private Animator Animator;

    public float speed_per_second = 1;
    public float degrees_per_second = 45;


    // Start is called before the first frame update
    void Start()
    {
        Physics.gravity = new Vector3(0, -3.0F, 0);
        Rigidbody = Ant.GetComponent<Rigidbody>();
        Animator = Ant.GetComponent<Animator>();
        PherSenseRange = Ant.GetComponent<BoxCollider>();
        Debug.Log(Animator);
        Animator.SetBool("walking", false);
        Animator.SetBool("grounded", true);
        Animator.enabled = true;
        Pheromone = origPheromone.GetComponent<Pheromone>();
        Vector3 p = Ant.transform.position + Ant.transform.up.normalized;
        prevPherPos = new Vector3Int(Mathf.RoundToInt(p.x), Mathf.RoundToInt(p.y), Mathf.RoundToInt(p.z));
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.P))
        {
            if (state == AIState.Following) state = AIState.Controlled;
            else state = AIState.Following;
        }
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        

        //Behaviour based on AI state
        if (state == AIState.Controlled) catchInputs();
        else AIMovement();

        //Deciding and executing the placement of a new pheromone node.
        Vector3 p = Ant.transform.position + Ant.transform.up.normalized/2;
        Vector3Int newPherPos = new Vector3Int(Mathf.RoundToInt(p.x), Mathf.RoundToInt(p.y), Mathf.RoundToInt(p.z));
        if (Animator.GetBool("walking") && newPherPos != prevPherPos && state == AIState.Controlled) //For now only placed when controlled
        {
            prevPherPos = newPherPos;
            if (placedPheromone == null) pathId = Pheromone.getNextPathId(); //we get the next path id for this new path
            if (Pheromone.PlacePheromone(origPheromone, prevPherPos, Ant.transform.rotation, pathId, pathPos, out placedPheromone))
                pathPos++;
            Debug.Log(pathPos);
        }
        
        turn = Animator.GetInteger("turning") * degrees_per_second * Time.fixedDeltaTime;

        Color hitColor;
        int numHits = 0;
        Vector3 normalMedian = Vector3.zero;
        //El orden de los raycasts es importante. Son atras derecha -> atras izquierda -> delante izquierda -> delante derecha -> centro
        float[] xPos = {sep, -sep, -sep, sep, 0};
        float[] zPos = {-sep, -sep, sep, sep, 0};
        float yPos = 0.5f;
        Boolean[] rayCastHits = { false, false, false, false, false};

        for (int i = 0; i < xPos.Length; i++) {
            if (Physics.Raycast(getRelativePos(xPos[i], yPos, zPos[i]), Rigidbody.rotation * new Vector3(0, yPos - 0.8f, 0), out RaycastHit hit, 1))
            {
                hitColor = Color.red;
                numHits++;
                normalMedian += hit.normal;
                rayCastHits[i] = true;
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
            hitColor = Color.red;
            Rigidbody.AddForce(-normalMedian*40); //USES ADDFORCE INSTEAD OF GRAVITY TO AVOID SLOW EFFECT
            Physics.gravity = Vector3.zero;

            Quaternion deltaRotation = Quaternion.Euler(new Vector3(0,turn,0));
            Rigidbody.MoveRotation(Rigidbody.rotation * deltaRotation); //Rotate ant

            //Cuando la hormiga no detecta terreno con su raycast principal, es rotado hacia el terreno seg�n los raycasts que aun detectan superficies
            if (!rayCastHits[4])
            {
                float xRotation = 0;
                float zRotation = 0;
                if (rayCastHits[0] && !rayCastHits[1]) zRotation += tiltSpeed * 0.5f;
                if (rayCastHits[1] && !rayCastHits[0]) zRotation -= tiltSpeed * 0.5f;
                if (rayCastHits[1] && !rayCastHits[2]) xRotation += tiltSpeed * 0.5f;
                if (rayCastHits[2] && !rayCastHits[1]) xRotation -= tiltSpeed * 0.5f;
                if (rayCastHits[2] && !rayCastHits[3]) zRotation -= tiltSpeed * 0.5f;
                if (rayCastHits[3] && !rayCastHits[2]) zRotation += tiltSpeed * 0.5f;
                if (rayCastHits[3] && !rayCastHits[0]) xRotation -= tiltSpeed * 0.5f;
                if (rayCastHits[0] && !rayCastHits[3]) xRotation += tiltSpeed * 0.5f;
                deltaRotation = Quaternion.Euler(new Vector3(xRotation, 0, zRotation));
                Rigidbody.MoveRotation(Rigidbody.rotation * deltaRotation);
            }
            Vector3 proyectedVector = Vector3.ProjectOnPlane(Rigidbody.rotation * Vector3.forward, normalMedian); //Project movement over terrain
            Rigidbody.position = Rigidbody.position + proyectedVector * speed; //Move forward

        }
        //Si no est� grounded
        else
        {
            hitColor = Color.blue;
            Physics.gravity = new Vector3(0, -3.0F, 0);
        }
        

        //Debug.Log("Walking: " + Animator.GetBool("walking") + ", falling: " + Animator.GetBool("falling"));



    }

    public Vector3 getRelativePos(float x, float y, float z)
    {
        return Rigidbody.position + Ant.transform.rotation * new Vector3(x, y, z);
    }

    void catchInputs() {
        if (Input.GetKey(KeyCode.UpArrow))
        {
            speed = speed_per_second * Time.fixedDeltaTime;
            Animator.SetBool("walking", true);
        }
        else
        {
            speed = 0f;
            Animator.SetBool("walking", false);
        }
        if (Input.GetKey(KeyCode.LeftArrow)) Animator.SetInteger("turning", -1);
        else if (Input.GetKey(KeyCode.RightArrow)) Animator.SetInteger("turning", 1);
        else Animator.SetInteger("turning", 0);
    }

    void AIMovement() {

        if (state == AIState.Following)
        {
            ConsiderPheromones();
            if (followingPheromone != null)
                FollowPheromone(followingPheromone);
            else
            {
                Animator.SetInteger("turning", 0);
                speed = 0f;
                Animator.SetBool("walking", false);
            }
        }
        else if (state == AIState.Exploring)
        {
            RandomMovement();
        }
    }

    void RandomMovement()
    {
        speed = speed_per_second * Time.fixedDeltaTime;
        Animator.SetBool("walking", true);
    }

    //La idea inicial es coger el plano x-z sobre el que se encuentra la hormiga, luego proyectar el punto del objeto pheromona sobre �l.
    //Dependiendo de donde se encuentra en el plano ajustar la direcci�n y decidir si moverse hacia delante.
    void FollowPheromone(GameObject pheromone)
    {
        Vector3 pherRel = Rigidbody.transform.InverseTransformPoint(pheromone.transform.position); //relative position of the pheromone to the ant
        float yDist = pherRel.y; 
        pherRel.y = 0; //We remove the y angle
        float angle = Vector3.Angle(Vector3.forward, pherRel);
        float distance = pherRel.magnitude;

        if (distance < Mathf.Abs(yDist) && Mathf.Abs(yDist) > 1.5f) //Si el nodo se encuentra m�s lejos verticalmente que horizontalmente, asumimos que no se sabe llegar y reseleccionamos nodo
        {

            followingPheromone = null;
            ConsiderPheromones();
            if (followingPheromone == null) state = AIState.Controlled; //Esto deber� ser exploring cuando ese sea arreglado ese estado
            return;
        }

        if (angle > 10 && distance > 1f)
        {
            if (pherRel.x > 0) Animator.SetInteger("turning", 1);
            else Animator.SetInteger("turning", -1);
        }
        else Animator.SetInteger("turning", 0);

        if (pherRel.z > 0.8 && distance > 0.3f)
        {
            speed = speed_per_second * Time.fixedDeltaTime;
            Animator.SetBool("walking", true);
        }
        else
        {
            speed = 0f;
            Animator.SetBool("walking", false);
        }

        //Plane horPlane = new Plane(transform.up.normalized, transform.position); //Obtener el plano horizontal de la hormiga
        //Vector3 objective = horPlane.ClosestPointOnPlane(pheromone.transform.position); //proyectar la posicion de la pheromona sobre el plano.
    }


    List<GameObject> detectedPheromones = new List<GameObject>();

    private void OnTriggerEnter(Collider other)
    {
        detectedPheromones.Add(other.gameObject);
        //Debug.Log("Size: " + detectedPheromones.Count);
    }

    private void OnTriggerExit(Collider other)
    {
        detectedPheromones.Remove(other.gameObject);
    }

    private void ConsiderPheromones() 
    {
        foreach (GameObject newPheromone in detectedPheromones)
        {
            Pheromone pherScript = newPheromone.GetComponent<Pheromone>();
            if (followingPheromone == null)
            {
                if (followId == -1) //If there is not specific ID being followed by the ant it picks one
                {
                    followingPheromone = newPheromone; //Seleccionamos el nodo actual como el que se seguir�
                    (int, int, int) pathData = pherScript.GetNewestPath(); //Obtenemos el paso del camino mas reciente guardado en el nodo
                    followId = pathData.Item1; //Guardamos la ID del camino para seguirlo
                    followPos = pathData.Item2; //Guardamos la posici�n en el camino del nodo
                    Debug.Log("new followId: " + followId + ", followPos: " + followPos); 
                }
                else //If there is a specific Id being followed with the following pheromone being null, we look for one with the matchin id
                //this is essentially a soft reset for when the ant is stuck (pher above or below ant can be avoided by calling this after setting the followed to null)
                {
                    if (pherScript.ContainsPathId(followId, out int pos))
                    {
                        followingPheromone = newPheromone;
                        followPos = pherScript.pathIds[pos].Item2;
                    }
                }
            }
            else
            {
                if (pherScript.ContainsPathId(followId, out int pos))
                {
                    //Debug.Log("My pos: " + pathPos + ", its pos: " + pherScript.pathIds[pos].Item2);
                    if ((followingForwards && pherScript.pathIds[pos].Item2 > followPos) || (!followingForwards && pherScript.pathIds[pos].Item2 < followPos) || followPos == -1)
                    {
                        followingPheromone = newPheromone;
                        followPos = pherScript.pathIds[pos].Item2;
                        Debug.Log("new followId: " + followId + ", followPos: " + followPos);
                    }
                }
                //else Debug.Log("Nope");
            }
        }
        if (followingPheromone == null && detectedPheromones.Count > 0) 
        {
            followId = -1;
            ConsiderPheromones();
        }


    }

    /*
    static public bool ArcCast(Vector3 center, Quaternion rotation, float xAngle, float yAngle, float radius, int resolution, int parts, LayerMask layer, out RaycastHit hit)
    {
        rotation *= Quaternion.Euler(-xAngle / 2, yAngle, 0);

        for (int i = 0; i < parts; i++)
        {
            Vector3 A = center + rotation * Vector3.forward * radius;
            rotation *= Quaternion.Euler(-xAngle / resolution, 0, 0);
            Vector3 B = center + rotation * Vector3.forward * radius;
            Vector3 AB = B - A;

            
            
            if (Physics.Raycast(A, AB, out hit, AB.magnitude * 1.001f, layer))
            {
                Debug.DrawLine(A, hit.point, Color.blue, Time.deltaTime);
                return true;
            }
            Debug.DrawLine(A, B, Color.red, Time.deltaTime);
        }

        hit = new RaycastHit();
        return false;
    }

    float arcXAngle = 270;
        float arcRadius = 2.5f;
        int arcResolution = 16;
        int arcParts = 6;
        int layerMask = 1 << 6;

        for (int i = 1; i <= 6; i++)
        {
            if (ArcCast(Cube.transform.position, Cube.transform.rotation, arcXAngle,i*360f/6, arcRadius, arcResolution, arcParts, layerMask, out RaycastHit hit))
            {
                //Cube.transform.rotation = Quaternion.FromToRotation(Cube.transform.up, hit.normal) * Cube.transform.rotation;
                Debug.Log("Collided");
            }
        }
    */
}
