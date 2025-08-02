using System;
using System.Collections.Generic;
using System.Linq;
using FluentBehaviourTree;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering;

public class Nest : MonoBehaviour
{
    public static List<NestPart> NestParts = new();
    public static HashSet<int> KnownCornCobs = new();
    private static int lastIndex = 0;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }

    public static BehaviourTreeStatus GetNestTask(CubePaths.CubeSurface antSurface, ref Task objective)
    {
        if (GetNestDigTask(antSurface, ref objective))
            return BehaviourTreeStatus.Success;
        if (GetNestCollectTask(antSurface, ref objective))
            return BehaviourTreeStatus.Success;
        return BehaviourTreeStatus.Failure;
    }

    public static bool GetNestDigTask(CubePaths.CubeSurface antSurface, ref Task objective)
    {
        objective = Task.NoTask();

        if (SurfaceInNest(antSurface))
        {
            List<Vector3Int> available = DigPoint.availableDigPoints.ToList();
            foreach (var pos in available) //Miramos cada posicion
            {
                //Si ya encontramos un camino a uno, salir
                if (objective.isTaskType(TaskType.None)) break;

                //Si no exisste el digPointData eliminamos de available
                if (!DigPoint.digPointDict.ContainsKey(pos))
                {
                    DigPoint.availableDigPoints.Remove(pos);
                    break;
                }

                int antId = DigPoint.digPointDict[pos].antId;
                //Si ya lo tiene otra hormiga ignorarlo
                if (antId != -1)
                {
                    if (Ant.antDictionary.TryGetValue(antId, out Ant digPointsAnt))
                    {
                        if (digPointsAnt.objective.isTaskType(TaskType.Dig))
                            if (digPointsAnt.objective.digPointId == pos)
                                break;
                    }
                }

                //find path to it. If no path, remove from available
                if (CubePaths.GetKnownPathToPoint(antSurface, pos, 1.2f, out List<CubePaths.CubeSurface> newPath))
                    objective = Task.DigTask(pos, newPath);
                else DigPoint.availableDigPoints.Remove(pos);
            }

            //Devolvemos failure si no hemos entontrado una tarea
            if (objective.isTaskType(TaskType.None))
                return false;

            return true;
        }
        else return false;
    }

    public static bool GetNestCollectTask(CubePaths.CubeSurface antSurface, ref Task objective)
    {
        objective = Task.NoTask();

        if (KnownCornCobs.Count > 0 && SurfaceInNest(antSurface) && objective.isTaskType(TaskType.None))
        {
            //Selecciona un indice de los conocidos aleatorio
            int cornIndex = KnownCornCobs.ElementAt(UnityEngine.Random.Range(0, KnownCornCobs.Count));
            if (CornCob.cornCobDictionary.TryGetValue(cornIndex, out CornCob cob))
            {
                if (CubePaths.GetKnownPathToPoint(antSurface, cob.transform.position, 5, out List<CubePaths.CubeSurface> path))
                {
                    objective = new Task(cob.gameObject, TaskType.CollectFromCob, path);
                    return true;
                }
                else
                {
                    //Quitamos de la lista si desde el nido mismo no se puede llegar.
                    KnownCornCobs.Remove(cornIndex);
                    Debug.Log("Please dont loop");
                    return GetNestCollectTask(antSurface, ref objective);
                }
            }
            else
            {
                //Quitamos de la lista si es no válido.
                KnownCornCobs.Remove(cornIndex);
                Debug.Log("Please dont loop");
                return GetNestCollectTask(antSurface, ref objective);
            }

        }
        else return false;
    }

    public static bool PointInNest(Vector3 point)
    {
        int checkedParts = 0;
        for (int i = lastIndex; checkedParts < NestParts.Count; checkedParts++, i++)
        {
            i %= NestParts.Count;
            float marchingValue = NestParts[i].getMarchingValue(point);
            if (marchingValue < WorldGen.isolevel * 1.05)
            {
                lastIndex = i;
                return true;
            }
        }

        return false;
    }

    public static bool SurfaceInNest(CubePaths.CubeSurface surface)
    {
        for (int i = 0; i < 8; i++)
        {
            if (surface.surfaceGroup[i])
                if (PointInNest(chunk.cornerTable[i] + surface.pos))
                    return true;
        }
        return false;
    }

    public static bool PointInNestPart(Vector3 point, NestPart.NestPartType type)
    {
        if (type == NestPart.NestPartType.Outside) return !PointInNest(point);

        int checkedParts = 0;
        for (int i = lastIndex; checkedParts < NestParts.Count; checkedParts++, i++)
        {
            i %= NestParts.Count;
            //Solo miramos las partes del nido con el mismo tipo.
            if (NestParts[i].mode == type)
            {
                float marchingValue = NestParts[i].getMarchingValue(point);
                if (marchingValue < WorldGen.isolevel * 1.05)
                {
                    lastIndex = i;
                    return true;
                }
            }
        }

        return false;
    }

    public static bool SurfaceInNestPart(CubePaths.CubeSurface surface, NestPart.NestPartType type)
    {
        //Added this line because this, used in GetPathToMapPart would give a positive of being outside with
        //even one point being outside despite having other points inside. But the same goes for outside, so
        //cubes would be both outside and inside when using surfaceInNestPart.
        //The default way i want it to work is on point inside = inside. Just like in SurfaceInNest().
        //This is what caused the ant to be stuck going outside on a cube that is partially inside and
        //outside.
        if (type == NestPart.NestPartType.Outside) return !SurfaceInNest(surface);
        
        for (int i = 0; i < 8; i++)
        {
            if (surface.surfaceGroup[i])
                if (PointInNestPart(chunk.cornerTable[i] + surface.pos, type))
                    return true;
        }
        return false;
    }

}
