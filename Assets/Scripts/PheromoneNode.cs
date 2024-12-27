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
            if (prevPheromone != null) outPheromone = new Pheromone(pathId, prevPheromone, pos);
            else outPheromone = new Pheromone(pathId, pos);
            pherDictionary[pos].pherList.Add(outPheromone); //The path is new to the pheromone and is added
        }
        else // a new pheromone object is instantiated, copying the original
        {
            GameObject newNode = Instantiate(OrigNode, pos, Quaternion.identity); // we create a new node gameobject
            newNode.SetActive(true); //Activa el objeto
            PheromoneNode newNodeScript = newNode.GetComponent<PheromoneNode>();//Obtiene el script, la clase del nuevo gameobject
            newNodeScript.pherList = new List<Pheromone>(); //Inicializamos la lista
            if (prevPheromone != null) outPheromone = new Pheromone(pathId, prevPheromone, pos); //Si la feromona previa no es nula crea pheromona nueva con él como último
            else outPheromone = new Pheromone(pathId, pos);//Si es nula, crea feromona sin previa
            newNodeScript.pherList.Add(outPheromone); // Añade la feromona nueva a la lista de los del nodo nuevo
            newNodeScript.pos = pos; //Actualiza pos del nodo nuevo
            pherDictionary.Add(pos, newNodeScript); // Añade nodo al diccionario global
            newNode.name = "Pheromone " + pathId + " " + pos; //cambia nombre del nodo
        }
        Debug.Log("Created pheromone node with pathId: " + pathId + ", pos: " + pos);
        return true;
    }

    public void showPath(int pathId){
        if (this.HasPheromone(pathId, out Pheromone pheromone)){
            pheromone.showPath(false);
        }
    }

}
