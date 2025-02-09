using System;
using Unity.VisualScripting;
using UnityEngine;

public class Tunnel : MonoBehaviour
{

    public GameObject cilinder;
    public GameObject startSphere;
    public GameObject endSphere;

    Vector3 dir;
    private float length;
    private float progress = 0;
    public float interval;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void setPos(Vector3 start, Vector3 end)
    {
        dir = end - start;
        transform.position = start;
        startSphere.transform.localPosition = Vector3.zero;
        endSphere.transform.localPosition = Vector3.up * dir.magnitude;
        cilinder.transform.localPosition = Vector3.up * dir.magnitude / 2;
        cilinder.transform.localScale = new Vector3(2, dir.magnitude/2, 2);
        transform.up = dir.normalized;
        length = dir.magnitude;
    }


    public void setActive(Boolean active) 
    {
        startSphere.SetActive(active);
        endSphere.SetActive(active);
        cilinder.SetActive(active);
    }

    public Vector3 nextPos()
    {
        if (length <= progress)
            return transform.position + dir;
        Vector3 pos = transform.position + dir.normalized * progress;
        progress += interval;
        return pos;
    }

}
