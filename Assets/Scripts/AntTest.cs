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
    public float speed = 0;
    public float turn = 1;
    public float tiltSpeed = 10;
    public float sep = 0.35f;

    enum AIState
    {
        Exploring,
        Following,
        Controlled
    }

    private AIState state = AIState.Following;
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
        Debug.Log(Animator);
        Animator.SetBool("walking", false);
        Animator.SetBool("grounded", true);
        Animator.enabled = true;
    }

    // Update is called once per frame
    void FixedUpdate()
    {

        if (state == AIState.Controlled) catchInputs();
        else AIMovement();
        
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

            //Cuando la hormiga no detecta terreno con su raycast principal, es rotado hacia el terreno según los raycasts que aun detectan superficies
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
        //Si no está grounded
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



    public GameObject pheromone = null;

    public bool noPheromone()
    {
        if (pheromone == null) return true;
        else return false;
    }

    public void SensePheromone(GameObject newPheromone)
    {
        if (pheromone == null) pheromone = newPheromone;
        else if (pheromone.GetComponent<Pheromone>().pathPos < newPheromone.GetComponent<Pheromone>().pathPos)
            pheromone = newPheromone;
    }

    void AIMovement() {

        if (state == AIState.Following)
        {
            if (pheromone != null)
                FollowPheromone(pheromone);
            else
            {
                Animator.SetInteger("turning", 0);
                speed = 0f;
                Animator.SetBool("walking", false);
            }
        }
    }

    //La idea inicial es coger el plano x-z sobre el que se encuentra la hormiga, luego proyectar el punto del objeto pheromona sobre él.
    //Dependiendo de donde se encuentra en el plano ajustar la dirección y decidir si moverse hacia delante.
    void FollowPheromone(GameObject pheromone)
    {
        Vector3 pherRel = Rigidbody.transform.InverseTransformPoint(pheromone.transform.position);
        float yDist = pherRel.y;
        pherRel.y = 0;
        float angle = Vector3.Angle(Vector3.forward, pherRel);
        float distance = Vector3.Distance(pheromone.transform.position, Rigidbody.transform.position);
        Debug.Log(distance);

        if (angle > 10 && distance > 1f)
        {
            if (pherRel.x > 0) Animator.SetInteger("turning", 1);
            else Animator.SetInteger("turning", -1);
        }
        else Animator.SetInteger("turning", 0);

        if ((pherRel.z > 0.8 || yDist > 1) && distance > 0.3f)
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
