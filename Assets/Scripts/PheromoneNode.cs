using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using pheromoneClass;

public class PheromoneNode : MonoBehaviour
{
    public List<Pheromone> pherList = new List<Pheromone>(); //Pheromones placed here
    public Vector3Int pos;
    static Dictionary<Vector3Int, PheromoneNode> pherDictionary = new Dictionary<Vector3Int, PheromoneNode>();
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

    public bool HasPheromone(int pathId, out Pheromone pheromone)
    {
        pheromone = null;
        foreach (Pheromone item in pherList) //iterates over all pheromone ids in the node
        {
            if (item.pathId == pathId)
            {
                pheromone = item;
                return true;
            }
        }
        return false;
    }

    public Pheromone GetNewestPheromone()
    {
        Pheromone youngest = pherList[0];
        foreach (Pheromone item in pherList) //iterates over all pheromone ids in the pos, and if it finds the given one it sets its age to 100
        {
            if (item.age < youngest.age)
            {
                youngest = item;
            }
        }
        return youngest;
    }

    //Places or updates a pheromone node. returns true if the path was new to the coordinate, false if it updated an existing one with the same step
    public bool PlacePheromone(GameObject OrigNode, Vector3Int pos, int pathId, Pheromone prevPheromone, out Pheromone outPheromone)
    {
        if (pherDictionary.ContainsKey(pos)) //if the pos already has a pheromone object, the data is added to the existing one
        {
            if (pherDictionary[pos].HasPheromone(pathId, out outPheromone)) //the pheromone already has that path, it is updated
            {
                outPheromone.age += 10;
                return false;
            }
            if (prevPheromone != null) outPheromone = new Pheromone(pathId, prevPheromone, pos, pherDictionary[pos].transform.forward);
            else outPheromone = new Pheromone(pathId, pos, pherDictionary[pos].transform.forward);
            pherDictionary[pos].pherList.Add(outPheromone); //The path is new to the pheromone and is added
        }
        else // a new pheromone object is instantiated, copying the original
        {
            Vector3 direction = WorldGen.SurfaceDirection(pos);
            GameObject newNode = Instantiate(OrigNode, pos, Quaternion.Euler(direction)); // we create a new node gameobject
            Debug.DrawRay(pos, direction, Color.green, 10000);
            newNode.SetActive(true); //Activa el objeto
            PheromoneNode newNodeScript = newNode.GetComponent<PheromoneNode>();//Obtiene el script, la clase del nuevo gameobject
            newNodeScript.pherList = new List<Pheromone>(); //Inicializamos la lista
            if (prevPheromone != null) outPheromone = new Pheromone(pathId, prevPheromone, pos, direction); //Si la feromona previa no es nula crea pheromona nueva con él como último
            else outPheromone = new Pheromone(pathId, pos, direction);//Si es nula, crea feromona sin previa
            newNodeScript.pherList.Add(outPheromone); // Añade la feromona nueva a la lista de los del nodo nuevo
            newNodeScript.pos = pos; //Actualiza pos del nodo nuevo
            pherDictionary.Add(pos, newNodeScript); // Añade nodo al diccionario global
            newNode.name = "Pheromone " + pathId + " " + pos; //cambia nombre del nodo
        }
        Debug.Log("Created pheromone node with pathId: " + pathId + ", pos: " + pos);
        return true;
    }

    //Places an aux pheromone. A simple pheromone whose previous and next are both the given pheromone. 
    public Pheromone PlaceAux(GameObject OrigNode, Vector3Int pos, Pheromone lastStepped)
    {
        Pheromone outPheromone;
        if (pherDictionary.ContainsKey(pos)) //if the pos already has a pheromone object, the data is added to the existing one
        {
            if (pherDictionary[pos].HasPheromone(lastStepped.pathId, out outPheromone)) //the pheromone already has that path, it is updated
            {
                outPheromone.age += 10;
                return outPheromone;
            }
            outPheromone = new Pheromone(lastStepped.pathId, pos, pherDictionary[pos].transform.forward);
            outPheromone.SetNext(lastStepped);
            outPheromone.SetPrevious(lastStepped);
            outPheromone.aux = true;
            pherDictionary[pos].pherList.Add(outPheromone); //The path is new to the pheromone and is added
        }
        else // a new pheromone object is instantiated, copying the original
        {
            Vector3 direction = WorldGen.SurfaceDirection(pos);
            GameObject newNode = Instantiate(OrigNode, pos, Quaternion.Euler(direction)); // we create a new node gameobject
            newNode.SetActive(true); //Activa el objeto
            Debug.DrawRay(pos, direction, Color.green, 10000);
            PheromoneNode newNodeScript = newNode.GetComponent<PheromoneNode>();//Obtiene el script, la clase del nuevo gameobject
            newNodeScript.pherList = new List<Pheromone>(); //Inicializamos la lista
            outPheromone = new Pheromone(lastStepped.pathId, pos, direction); //Se crea la nueva feromona
            outPheromone.SetNext(lastStepped);
            outPheromone.SetPrevious(lastStepped);
            outPheromone.aux = true;
            newNodeScript.pherList.Add(outPheromone); // Añade la feromona nueva a la lista de los del nodo nuevo
            newNodeScript.pos = pos; //Actualiza pos del nodo nuevo
            pherDictionary.Add(pos, newNodeScript); // Añade nodo al diccionario global
            newNode.name = "Pheromone " + lastStepped.pathId + " " + pos; //cambia nombre del nodo
        }
        Debug.Log("Created aux pheromone node with pathId: " + lastStepped.pathId + ", pos: " + pos);
        return outPheromone;
    }


}
