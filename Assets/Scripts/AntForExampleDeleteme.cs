using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AntForExampleDeleteme : MonoBehaviour
{

    public GameObject Ant;
    public Rigidbody Rigidbody;
    public float speed = 0;
    public float turn = 1;
    //el animador

    public float speed_per_second = 1;
    public float degrees_per_second = 45;
    public bool grounded;


    // Start is called before the first frame update
    void Start()
    {
        Physics.gravity = new Vector3(0, -3.0F, 0);
        Rigidbody = Ant.GetComponent<Rigidbody>();
    }

    // Update is called once per frame
    void Update()
    {

        Debug.Log("its " + 0.01f / Time.deltaTime);

        if (Input.GetKey(KeyCode.UpArrow))
        {
            speed = speed_per_second * Time.deltaTime;
        }
        else
        {
            speed = 0f;
        }
        int dir;
        if (Input.GetKey(KeyCode.LeftArrow)) dir = -1;
        else if (Input.GetKey(KeyCode.RightArrow)) dir = 1;
        else dir = 0;

        turn = dir * degrees_per_second * Time.deltaTime;

        Color hitColor;
        int numHits = 0;
        Vector3 normalMedian = Vector3.zero;

        float[] xPos = { 0.25f, -0.25f, 0.25f, -0.25f, 0 };
        float[] zPos = { 0.25f, -0.25f, -0.25f, 0.25f, 0 };
        float yPos = 0f;

        for (int i = 0; i < xPos.Length; i++)
        {
            if (Physics.Raycast(getRelativePos(xPos[i], yPos, zPos[i]), Ant.transform.rotation * new Vector3(0, yPos - 0.8f, 0), out RaycastHit hit, 1))
            {
                hitColor = Color.red;
                numHits++;
                normalMedian += hit.normal;
            }
            else hitColor = Color.blue;
            Debug.DrawLine(getRelativePos(xPos[i], yPos, zPos[i]), getRelativePos(xPos[i], yPos - 0.8f, zPos[i]), hitColor);
        }

        //REMEMBER TO COMMENT HOW YOU CHANGED FROM LOCAL BOOL TO THE ANIMATOR ONE
        if (numHits > 1)
        {
            grounded = true;
        }
        else
        {
            grounded = false;
        }
        if (numHits != 0) normalMedian /= numHits;

        if (grounded)
        {
            //Rigidbody.velocity = Vector3.zero;
            Rigidbody.angularVelocity = Vector3.zero;
            hitColor = Color.red;
            Rigidbody.AddForce(-normalMedian * 40); //USES ADDFORCE INSTEAD OF GRAVITY TO AVOID SLOW EFFECT
            Physics.gravity = Vector3.zero;

            //Rigidbody.rotation = Quaternion.FromToRotation(Vector3.up, normalMedian);

            Rigidbody.rotation *= Quaternion.Euler(0, turn, 0);

            Vector3 proyectedVector = Vector3.ProjectOnPlane(Rigidbody.rotation * Vector3.forward, normalMedian);

            Rigidbody.position = Rigidbody.position + proyectedVector * speed;

            //Debug.DrawLine(centerHit.point, centerHit.point + normalMedian, Color.yellow);

            //Stop the ant from moving and rotating on its own

        }
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
