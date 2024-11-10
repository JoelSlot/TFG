using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class Pheromone : MonoBehaviour
{
    public float radius = 30f;
    public int pathPos = 0;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        int layerId = 7;
        int layerMask = 1 << layerId;
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, radius, layerMask);
        for (int i = 0; i < hitColliders.Length; i++){
            Collider hitCollider = hitColliders[i];

            if (hitCollider != null)
            {

                hitCollider.GetComponent<AntTest>().SensePheromone(gameObject);

            }
            //Do some stuff here
        }
    }
}
