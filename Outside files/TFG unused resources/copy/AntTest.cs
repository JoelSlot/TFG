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

    public GameObject Cube;
    public Rigidbody Rigidbody;
    public float speed = 0;
    public float turn = 1;
    public Boolean grounded = false;


    // Start is called before the first frame update
    void Start()
    {
        Physics.gravity = new Vector3(0, -3.0F, 0);
        Rigidbody = Cube.GetComponent<Rigidbody>();
    }

    // Update is called once per frame
    void Update()
    {

        if (Input.GetKey(KeyCode.UpArrow)) speed = 0.01f;
        else speed = 0f;
        if (Input.GetKey(KeyCode.LeftArrow)) turn = -0.2f;
        else if (Input.GetKey(KeyCode.RightArrow)) turn = 0.2f;
        else turn = 0;

        Color hitColor;
        int numHits = 0;
        Vector3 normalMedian = Vector3.zero;

        float[] xPos = {0.25f, -0.25f, 0.25f, -0.25f, 0};
        float[] zPos = {0.25f, -0.25f, -0.25f, 0.25f, 0};

        for (int i = 0; i < xPos.Length; i++) {
            if (Physics.Raycast(getRelativePos(xPos[i], 0, zPos[i]), Cube.transform.rotation * new Vector3(0, -0.8f, 0), out RaycastHit hit, 1))
            {
                hitColor = Color.red;
                numHits++;
                normalMedian += hit.normal;
            }
            else hitColor = Color.blue;
            Debug.DrawLine(getRelativePos(xPos[i], 0, zPos[i]), getRelativePos(xPos[i], -0.8f, zPos[i]), hitColor);
        }

        if (numHits > 2) grounded = true; else grounded = false;
        if (numHits != 0) normalMedian /= numHits;

        if (grounded)
        {
            //Rigidbody.velocity = Vector3.zero;
            Rigidbody.angularVelocity = Vector3.zero;
            hitColor = Color.red;
            Rigidbody.AddForce(-normalMedian*40); //USES ADDFORCE INSTEAD OF GRAVITY TO AVOID SLOW EFFECT
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
        





    }

    public Vector3 getRelativePos(float x, float y, float z)
    {
        return Cube.transform.position + Cube.transform.rotation * new Vector3(x, y, z);
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
