using UnityEngine;

/*
public class CubePheromone
{
    //These fucking things gotta be differentiated by their pos, id and surface. Damn.
    static int nextId = -1;
    int pathId;
    int pathPos;
    public CubePaths.CubeSurface surface;
    public CubePheromone prev;
    public CubePheromone next;

    //Crea una nueva feromona como comienzo de un nuevo camino el la posición pherPos
    public CubePheromone(CubePaths.CubeSurface surface)
    {
        pathId = GetNextId();
        pathPos = 0;
        //Previo/siguiente se señalan a si mismos =  comienzo/final del camino
        prev = this;
        next = this;

        this.surface = surface;

        Color[] colors = { Color.red, Color.blue, Color.green };
        CubePaths.DrawCube(surface.pos, colors[pathId % 3], 100000);

    }

    //Crea el siguientd nodo en el camino dado el actual
    public CubePheromone(CubePaths.CubeSurface newSurface, CubePheromone prevPher)
    {
        pathId = prevPher.pathId;
        pathPos = prevPher.pathPos + 1;
        prev = prevPher;
        next = this;

        prevPher.next = this;

        surface = newSurface;

        Color[] colors = { Color.red, Color.blue, Color.green };
        CubePaths.DrawCube(surface.pos, colors[pathId % 3], 100000);
        
    }

    private static int GetNextId()
    {
        nextId += 1;
        return nextId;
    }

    //Devuelve el siguiente nodo del camino dependiendo de si se mueve hacia delante o hacia detrás
    public CubePheromone GetNext(bool forward)
    {
        if (forward) return next;
        else return prev;
    }

    //Devuelve si el nodo es el ultimo del camino dado si se está moviendo hacia delante o hacia detrás
    public bool isLast(bool forward)
    {
        if (forward) return next == this;
        else return prev == this;
    }

    //Devuelve el pathId del nodo
    public int GetPathId() {return pathId;}

    //Devuelve el pathId del nodo
    public int GetPathPos() {return pathPos;}

    //Devuelve si el nodo de pheromona se encuentra en:
    //La misma posición y
    //El mismo camino y
    //La misma superficie
    //Es decir, si son exactamente iguales
    public bool SameCubeSamePathSameSurface(CubePheromone other)
    {
        if (pathId != other.pathId) return false; //check if same path
        return surface.Equals(other.surface);
    }

    public CubePaths.CubeSurface GetSurface()
    {
        return surface;
    }

    //Devuelve el cubo en el que se encuentra la feromona
    public Vector3Int GetPos()
    {
        return surface.pos;
    }

    public bool[] GetSurfaceGroup()
    {
        return surface.surfaceGroup;
    }

}
*/