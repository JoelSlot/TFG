using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class Pheromone : MonoBehaviour
{
    public List<(int, int, int)> pathIds = new List<(int, int, int)>(); //Ids, pathpos and age of pheromones placed here
    public Vector3Int pos;

    static Dictionary<Vector3Int, Pheromone> pherDictionary = new Dictionary<Vector3Int, Pheromone>();
    static int nextPathId = 0;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {

    }

    public int getNextPathId()
    {
        return nextPathId++;
    }

    public bool ContainsPathId(int pathId, out int pos)
    {
        for (int i = 0; i < pathIds.Count; i++) //iterates over all pheromone ids in the pos, and if it finds the given one it sets its age to 100
        {
            if (pathIds[i].Item1 == pathId)
            {
                pos = i;
                return true;
            }
        }
        pos = -1;
        return false;
    }

    public (int, int, int) GetNewestPath()
    {
        (int, int, int) youngest = pathIds[0];
        for (int i = 1; i < pathIds.Count; i++) //iterates over all pheromone ids in the pos, and if it finds the given one it sets its age to 100
        {

            if (pathIds[i].Item3 < youngest.Item3)
            {
                youngest = pathIds[i];
            }
        }
        return youngest;
    }

    //Places or updates a pheromone node. returns true if the path was new to the coordinate, false if it updated an existing one with the same step
    public bool PlacePheromone(GameObject pheromone, Vector3Int pos, Quaternion direction, int pathId, int pathPos, out GameObject outPheromone)
    {
        if (pherDictionary.ContainsKey(pos)) //if the pos already has a pheromone object, the data is added to the existing one
        {
            outPheromone = pherDictionary[pos].gameObject;
            if (pherDictionary[pos].ContainsPathId(pathId, out int Id)) //the pheromone already has that path, it is updated
            {
                int prevPathPos = pherDictionary[pos].pathIds[Id].Item2;
                pherDictionary[pos].pathIds[Id] = (pathId, pathPos, 0);
                return false;
            }
            pherDictionary[pos].pathIds.Add((pathId, pathPos, 0)); //The path is new to the pheromone and is added
        }
        else // a new pheromone object is instantiated, copying the original
        {
            outPheromone = Instantiate(pheromone, pos, direction);
            outPheromone.SetActive(true);
            Pheromone script = outPheromone.GetComponent<Pheromone>();
            script.pathIds = new List<(int, int, int)>
            {
                (pathId, pathPos, 0)
            };
            script.pos = pos;
            pherDictionary.Add(pos, script);
            outPheromone.name = "Pheromone " + pathId + " " + pathPos;
        }
        Debug.Log("Created pheromone node with pathId: " + pathId + ", pos: " + pathPos);
        return true;
    }

}
