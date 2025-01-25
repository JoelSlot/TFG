using System.Collections.Generic;
using UnityEngine;
public class Pheromone : MonoBehaviour
{
    public int pathId;
    public int age;
    public Vector3 pos;
    private Pheromone previous;
    private Pheromone next;
    public bool aux = false;
    public Vector3 upDir;
    private bool isLast = true;
    private bool isFirst = true;
    static Dictionary<int, Pheromone> pheromonePaths = new Dictionary<int, Pheromone>(); //Diccionario de todas las primeras pheromonas de los caminos de pheromona
    static int nextPathId = 0;

    public Pheromone(int pathId, Pheromone previous, Vector3Int pos, Vector3 dir){
        this.pathId = pathId;
        this.previous = previous;
        previous.next = this;
        this.pos = pos;
        upDir = dir;
        age = 100;
        isFirst = false;
        isLast = true;
    }

    public Pheromone(int pathId, Vector3Int pos, Vector3 dir){
        this.pathId = pathId;
        previous = null;
        this.pos = pos;
        upDir = dir;
        age = 100;
        isFirst = true;
        isLast = true;
    }

    public Pheromone(){
        this.pathId = -1;
        age = 100;
    }

    //Obtiene la siguiente pheromona del camino.
    //Si este no existe, devuelve falso.
    //Si existe, la retorna mediante out y devuelve falso
    public bool GetNext(bool forwards, out Pheromone next){
        if (forwards)
        {
            next = this.next;
            if (isLast) return false;
            return true;
        }
        else
        {
            next = this.previous;
            if (isFirst) return false;
            return true;
        }
    }
    //
    public void SetNext(Pheromone next)
    {
        if (next == this) return;
        if (next == null)
        {
            this.next = null;
            isLast = true;
            return;
        }
        this.next = next;
        isLast = false;
    }
    public void SetPrevious(Pheromone prev)
    {
        if (prev == this) return;
        if (prev == null)
        {
            this.previous = null;
            isFirst = true;
            return;
        }
        previous = prev;
        isFirst = false;
    }
    //El deconstructor se asegura que los nodos siguiente y previo, 
    //si le tienen a él mismo como previo y siguiente respectivamente,
    //se pongan a null
    ~Pheromone(){
        if (previous != null) if (previous.next == this) previous.SetNext(null);
        if (next != null) if (next.previous == this) next.SetPrevious(null);
    }


    public void ShowPath(){
        bool reachedStart = false;
        if (previous == null) reachedStart = true;
        if (reachedStart){
            if (next != null)
            {
                Debug.DrawLine(pos, next.pos, Color.green, 100000);
                next.ShowPath(true);
            }
            return;
        }
        else
        {
            previous.ShowPath(false);
        }
    }
    public void ShowPath(bool reachedStart){
        if (previous == null) reachedStart = true;
        if (reachedStart){
            if (next != null)
            {
                Debug.DrawLine(pos, next.pos, Color.green, 100000);
                next.ShowPath(true);
            }
            return;
        }
        else
        {
            previous.ShowPath(false);
        }
    }
    
    public bool IsEnd(bool movingForward)
    {
        if (movingForward && isLast) return true;
        if (!movingForward && isFirst) return true;
        return false;
    }

    public static int getNextPathId()
    {
        return nextPathId++;
    }

    static public Pheromone PlacePheromone(GameObject origPheromone, Vector3 pos, Vector3 dir, Pheromone prevPheromone)
    {
        GameObject newPheromoneObj = Instantiate(origPheromone, pos, Quaternion.Euler(dir)); // we create a new node gameobject
        Debug.DrawRay(pos, dir, Color.green, 10000);
        newPheromoneObj.SetActive(true); //Activa el objeto
        Pheromone outPheromone = newPheromoneObj.GetComponent<Pheromone>();//Obtiene el script, la clase del nuevo gameobject
        outPheromone.SetPrevious(prevPheromone); //works even if its null
        outPheromone.SetNext(null);
        if (prevPheromone != null){
            prevPheromone.SetNext(outPheromone); //Añadir nueva pheromona a la previa como siguiente
            outPheromone.pathId = prevPheromone.pathId;
            Debug.DrawRay(pos, prevPheromone.upDir, Color.green*Color.black, 10000);
        }
        else
        {
            outPheromone.pathId = Pheromone.getNextPathId();
            pheromonePaths.Add(outPheromone.pathId, outPheromone); //si es el comienzo de un camino nuevo, lo añade al diccionario
        }
        outPheromone.pos = pos; //Actualiza pos del nodo nuevo
        outPheromone.upDir = dir;
        newPheromoneObj.name = "Pheromone " + outPheromone.pathId + " " + outPheromone.pos; //cambia nombre del nodo´
        return outPheromone;
    }

    static public Pheromone PlaceAux(GameObject origPheromone, Vector3Int pos, Vector3Int dir, Pheromone prevPheromone)
    {
        GameObject newPheromoneObj = Instantiate(origPheromone, pos, Quaternion.Euler(dir)); // we create a new node gameobject
        Debug.DrawRay(pos, dir, Color.green, 10000);
        newPheromoneObj.SetActive(true); //Activa el objeto
        Pheromone outPheromone = newPheromoneObj.GetComponent<Pheromone>();
        outPheromone.SetPrevious(prevPheromone); //works even if its null
        outPheromone.SetNext(prevPheromone);
        outPheromone.pathId = prevPheromone.pathId;
        outPheromone.pos = pos; //Actualiza pos del nodo nuevo
        outPheromone.upDir = dir;
        newPheromoneObj.name = "Pheromone " + outPheromone.pathId + " " + outPheromone.pos; //cambia nombre del nodo´
        return outPheromone;
    }
}