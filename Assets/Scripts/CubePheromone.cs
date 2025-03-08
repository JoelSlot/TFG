using System.Collections.Generic;
using NUnit.Framework.Internal;
using System.Collections;
using UnityEngine;
using System;


public class CubePheromone
{
    //These fucking things gotta be differentiated by their pos, id and surface. Damn.
    static int nextId = -1;
    int pathId;
    int pathPos;
    Vector3Int pos;
    bool[] surfaceGroup; //El grupo de esquinas que son true si es encuentran debajo de la superficie
    public CubePheromone prev;
    public CubePheromone next;

    //Crea una nueva feromona como comienzo de un nuevo camino el la posición pherPos
    public CubePheromone(Vector3Int pherPos, Vector3Int pointBelowSurface)
    {
        pathId = GetNextId();
        pathPos = 0;
        //Previo/siguiente se señalan a si mismos =  comienzo/final del camino
        prev = this;
        next = this;
        pos = pherPos;
        makeSurfaceGroup(pointBelowSurface);
    }

    //Crea el siguientd nodo en el camino dado el actual
    public CubePheromone(Vector3Int pherPos, CubePheromone prevPher)
    {
        pathId = prevPher.pathId;
        pathPos = prevPher.pathPos + 1;
        prev = prevPher;
        next = this;
        pos = pherPos;
        makeSurfaceGroup(prevPher);

        prevPher.next = this;

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
        if (pos != other.pos) return false; //check if same pos
        if (pathId != other.pathId) return false; //check if same path
        return CubePaths.CompareGroups(surfaceGroup, other.surfaceGroup);
    }

    //Asigna el grupo de valores de las esquinas del cubo según si están debajo de la superficie de la feromona.
    //Usado para diferenciar nodos en superficies distintas.
    //Versión que usa un punto dado debajo de la superficie.
    public void makeSurfaceGroup(Vector3Int pointUnderSurface)
    {
        surfaceGroup = CubePaths.GetGroup(pointUnderSurface, CubePaths.CubeCornerValues(pos));
    }

    //Versión que usa la dirección de la superficie
    public void makeSurfaceGroup(CubePheromone adyacentCube)
    {
        int newCubeDirIndex = chunk.reverseFaceDirections[pos - adyacentCube.pos]; //obtenemos indice de dir desde adyacente a actual cubo
        Vector3Int pointUnderSurface = CubePaths.TrueCorner(newCubeDirIndex, adyacentCube.surfaceGroup) - chunk.faceDirections[newCubeDirIndex]; //Mediante dicho índice conseguimos uno de los puntos compartidos debajo de la superficie
        makeSurfaceGroup(pointUnderSurface);
    }

    //Devuelve el grupo de valores de esquinas de la superficie
    public bool[] GetSurfaceGroup()
    {
        return this.surfaceGroup;
    }

    //Devuelve el cubo en el que se encuentra la feromona
    public Vector3Int GetPos()
    {
        return pos;
    }

}