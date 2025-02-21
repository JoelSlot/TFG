using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;

public class DigObject : MonoBehaviour
{

    public GameObject cilinder;
    public GameObject startSphere;
    public GameObject endSphere;

    public enum digType {Tunnel,Chamber}

    public digType mode = digType.Tunnel;

    private Vector3 dir = Vector3.up;
    private Vector3 startPos = Vector3.zero;
    private Vector3 endPos = Vector3.up;
    private float radius = 1;
    public float interval;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void setMode(digType newMode)
    {
        if (newMode == mode) return;
        mode = newMode;
        if (newMode == digType.Tunnel)
        {
            startSphere.GetComponent<MeshRenderer>().enabled = true;
            endSphere.GetComponent<MeshRenderer>().enabled = true;
            cilinder.GetComponent<MeshRenderer>().enabled = true;
            transform.localRotation = Quaternion.Euler(dir);
            setPos(transform.position, endPos);
        }
        else if (newMode == digType.Chamber)
        {
            startSphere.GetComponent<MeshRenderer>().enabled = true;
            endSphere.GetComponent<MeshRenderer>().enabled = false;
            cilinder.GetComponent<MeshRenderer>().enabled = false;
            transform.localRotation = Quaternion.Euler(Vector3.up);
        }
    }

    public void setPos(Vector3 start, Vector3 end)
    {
        endPos = end;
        startPos = start;
        if (mode == digType.Tunnel)
        {
            dir = end - start;
            transform.position = startPos;
            startSphere.transform.localPosition = Vector3.zero;
            endSphere.transform.localPosition = Vector3.up * dir.magnitude;
            cilinder.transform.localPosition = Vector3.up * dir.magnitude / 2;
            cilinder.transform.localScale = new Vector3(radius*2, dir.magnitude/2, radius*2);
            transform.up = dir.normalized;
        }
        else if (mode == digType.Chamber)
        {
            Vector3 distance = endPos - startPos;
            transform.position = startPos + distance/2;
            startSphere.transform.localPosition = Vector3.zero;
            startSphere.transform.localScale = new Vector3(Mathf.Abs(distance.x), Mathf.Abs(distance.y), Mathf.Abs(distance.z));
            transform.up = Vector3.up;
            float a = Mathf.Abs(distance.x)/2;
            float b = Mathf.Abs(distance.y)/2;
            float c = Mathf.Abs(distance.z)/2;
            Debug.Log("a: " + a + ", b: " + b + ", c: " + c);
        }
    }

    public void setRadius(float newRadius) //LIMITED TO AVOID UNREALISTIC TUNNELS
    {
        if (newRadius < 1 || newRadius > 2 || mode != digType.Tunnel) return; //Min and max radius or wrong mode
        radius = newRadius;
        startSphere.transform.localScale = Vector3.one * radius * 2;
        endSphere.transform.localScale = Vector3.one * radius * 2;
        cilinder.transform.localScale = new Vector3(radius*2, dir.magnitude/2, radius*2);        
    }

    public float getRadius()
    {
        return radius;
    }


    public void setActive(Boolean active) 
    {
        startSphere.SetActive(active);
        endSphere.SetActive(active);
        cilinder.SetActive(active);
    }

    private bool gotPoints = false;

    public Dictionary<Vector3Int, float> pointsInDigObject(){
        if (gotPoints) return new Dictionary<Vector3Int, float>();
        else gotPoints = true;
        cilinder.GetComponent<MeshRenderer>().enabled = false;
        startSphere.GetComponent<MeshRenderer>().enabled = false;
        endSphere.GetComponent<MeshRenderer>().enabled = false;


        Dictionary<Vector3Int, float> points = new Dictionary<Vector3Int, float>();
        HashSet<Vector3Int> checkedPoints = new HashSet<Vector3Int>();
        Queue<Vector3Int> pointsToCheck = new Queue<Vector3Int>();
        Vector3Int start = Vector3Int.FloorToInt(transform.position);
        Vector3 startPos = transform.position;

        float desiredVal = getMarchingValue(start);
        checkedPoints.Add(start);
        pointsToCheck.Enqueue(start);
        points.Add(start, desiredVal); // used distance for a good while wondering why one of the center points was the wrong value when diggin the cave

        Vector3Int[] directions = {Vector3Int.up, Vector3Int.down, Vector3Int.left, Vector3Int.right, Vector3Int.forward, Vector3Int.back};

        while (pointsToCheck.Count > 0)
        {
            Vector3Int father = pointsToCheck.Dequeue();
            foreach (Vector3Int direction in directions)
            {
                Vector3Int son = father + direction;
                if (!checkedPoints.Contains(son)) // si no se ha visto aun
                { 
                    desiredVal = getMarchingValue(son);
                    if (desiredVal < WorldGen.isolevel)
                    {
                        pointsToCheck.Enqueue(son);
                    }
                    //Check to see if it would increase empty val
                    if (desiredVal < WorldGen.SampleTerrain(son)) points.Add(son, desiredVal); 
                    checkedPoints.Add(son);
                }
            }
        }
        
        Debug.Log("After while");
        return points;
    }

    float getMarchingValue(Vector3Int pos)
    {
        if (mode == digType.Tunnel)
        {
            float dist = DistancePointLine(pos, transform.position, endPos);
            return Mathf.Clamp01(dist / (2*radius));
        }
        else //if (mode == digType.Chamber)
        {
            return EllipseDistance(pos);
        }
    }

    // Calculate distance between a point and a line.
    public static float DistancePointLine(Vector3 point, Vector3 lineStart, Vector3 lineEnd)
    {
        return Vector3.Magnitude(ProjectPointLine(point, lineStart, lineEnd) - point);
    }

    // Project /point/ onto a line.
    public static Vector3 ProjectPointLine(Vector3 point, Vector3 lineStart, Vector3 lineEnd)
    {
        Vector3 relativePoint = point - lineStart;
        Vector3 lineDirection = lineEnd - lineStart;
        float length = lineDirection.magnitude;
        Vector3 normalizedLineDirection = lineDirection;
        if (length > .000001f)
            normalizedLineDirection /= length;
        float dot = Vector3.Dot(normalizedLineDirection, relativePoint);
        dot = Mathf.Clamp(dot, 0.0F, length);

        return lineStart + normalizedLineDirection * dot;
    }

    //Función que devuelve 0.5 dado un punto en el eje del elipse, menor de 0.5 cuanto mas se aleja del centro y 1 en el centro
    float EllipseDistance(Vector3Int point)
    {
        Vector3 distance = endPos - startPos;
        float a = Mathf.Abs(distance.x)/2;
        float b = Mathf.Abs(distance.y)/2;
        float c = Mathf.Abs(distance.z)/2;
        Vector3 localPos = point - transform.position;
        //Debug.DrawLine(transform.position, point, Color.white, 100000);
        float x = localPos.x;
        float y = localPos.y;
        float z = localPos.z;
        //float wrongValue = (x*x/a*a + y*y/b*b + z*z/c*c); //I HAVE BEEN USING THIS FOR A WHOLE DAY AND A HALF WONDERING WHY IT DIDNT WORK AAAAAAAA
        float value = ((x*x)/(a*a) + (y*y)/(b*b) + (z*z)/(c*c));
        //float value2 = 
        //Debug.Log("a: " + a + ", b: " + b + ", c: " + c + ", x: " + x + ", y: " + y + ", z: " + z + " result: " + value);
        return value/2;
    }


}
