using System;
using System.Collections.Generic;
using System.Linq;
using FluentBehaviourTree;
using Polenter.Serialization.Core;
using Unity.Collections;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public class Nest : MonoBehaviour
{
    public static List<NestPart> NestParts = new();
    public static HashSet<int> KnownCornCobs = new();
    public static Dictionary<int, int> CollectedCornPips = new(); //key is corn id, value is room index
    private static int lastIndex = 0;
    public int foodCount = 0;

    public static bool NestVisible = false;
    public static bool[] NestPartDisabled = { false, false, false, false };
    public static void WriteVisibleValues()
    {
        Debug.Log("General visible " + NestVisible + "\nTunnel: " + NestPartDisabled[0] + "\nFood: " + NestPartDisabled[1] + "\nEgg: " + NestPartDisabled[2] + "\nQueen: " + NestPartDisabled[3]);
    }



    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        foodCount = CollectedCornPips.Count;
    }


    public static BehaviourTreeStatus GetNestTask(CubePaths.CubeSurface antSurface, int antId, ref Task objective)
    {
        List<string> checkOrder = new List<string> { "dig", "corn", "cob" };
        Shuffle(checkOrder);

        foreach (var function in checkOrder)
        {
            switch (function)
            {
                case "dig":
                    if (GetNestDigTask(antSurface, antId, ref objective))
                    {
                        Debug.Log("Got a dig task");
                        return BehaviourTreeStatus.Success;
                    }
                    break;
                case "corn":
                    if (GetNestRelocateTask(antSurface, antId, ref objective))
                    {
                        Debug.Log("Got a relocate task");
                        return BehaviourTreeStatus.Success;
                    }
                    break;
                case "cob":
                    if (GetNestCollectTask(antSurface, ref objective))
                    {
                        Debug.Log("Got a collect task");
                        return BehaviourTreeStatus.Success;
                    }
                    break;
            }
        }

        Debug.Log("Did not get a requested task");
        return BehaviourTreeStatus.Failure;
    }

    public static bool GetNestDigTask(CubePaths.CubeSurface antSurface, int antId, ref Task objective)
    {
        objective = Task.NoTask();

        if (SurfaceInNest(antSurface))
        {
            List<Vector3Int> available = DigPoint.availableDigPoints.ToList();
            Debug.Log("Number of available digpoints: " + available.Count);
            int i = 0;
            foreach (var pos in available) //Miramos cada posicion
            {
                i++;
                Debug.Log("Checking number " + i);
                //Si ya encontramos un camino a uno, salir
                if (!objective.isTaskType(TaskType.None)) continue;

                //Si no exisste el digPointData eliminamos de available
                if (!DigPoint.digPointDict.ContainsKey(pos))
                {
                    DigPoint.availableDigPoints.Remove(pos);
                    continue;
                }

                //If already being dug, go to next
                if (Task.IsDigPointBeingDug(pos)) continue;

                //find path to it. If no path, remove from available
                if (CubePaths.GetKnownPathToPoint(antSurface, pos, 1.2f, out List<CubePaths.CubeSurface> newPath))
                {
                    objective = Task.DigTask(pos, newPath);
                    //Mark that digpoints new ant
                    DigPoint.digPointDict[pos].antId = antId;
                }
                else DigPoint.availableDigPoints.Remove(pos);
            }
            Debug.Log("Number of available digpoints post check: " + DigPoint.availableDigPoints.Count);

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
                if (CubePaths.GetKnownPathToPoint(antSurface, cob.transform.position, 3, out List<CubePaths.CubeSurface> path))
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

    public static bool GetNestRelocateTask(CubePaths.CubeSurface antSurface, int antId, ref Task objective)
    {
        foreach ((int cornId, int partId) in CollectedCornPips)
        {
            bool relocatable = false;
            if (partId == -1) relocatable = true;
            else if (NestParts[partId].mode != NestPart.NestPartType.FoodChamber) relocatable = true;

            if (relocatable)
            {
                //if already being relocated go to next
                if (Task.IsCornBeingPickedUp(cornId)) continue;

                if (CubePaths.GetKnownPathToPoint(antSurface, Corn.cornDictionary[cornId].transform.position, 1.2f, out List<CubePaths.CubeSurface> newPath))
                {
                    objective = Task.GetCornTask(cornId, antId, newPath);
                    return true;
                }

            }

        }
        return false;
    }

    public static bool PointInNest(Vector3 point)
    {
        int checkedParts = 0;
        for (int i = lastIndex; checkedParts < NestParts.Count; checkedParts++, i++)
        {
            i %= NestParts.Count;
            float marchingValue = NestParts[i].getMarchingValue(point);
            if (marchingValue < WorldGen.isolevel)
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
                if (PointInNest(chunk.cornerIdToPos[i] + surface.pos))
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
                if (marchingValue < WorldGen.isolevel) // there was a * 1.05. Why???
                {
                    //disregard undug chambers
                    if (type == NestPart.NestPartType.Tunnel || NestParts[i].HasBeenDug())
                    {
                        lastIndex = i;
                        return true;
                    }
                }
            }
        }

        return false;
    }

    public static bool PointInNestPart(Vector3 point, int nestPartIndex)
    {
        if (nestPartIndex >= NestParts.Count) { Debug.Log("out of range"); return false; }
        if (!NestParts[nestPartIndex].gotPoints) { Debug.Log("not got points"); return false; }

        float marchingValue = NestParts[nestPartIndex].getMarchingValue(point);
        //Debug.Log("Type: " + NestPart.NestPartTypeToIndex(NestParts[nestPartIndex].mode));
        //Debug.Log("value: " + marchingValue);
        if (marchingValue < WorldGen.isolevel * 1.05f) // there was a * 1.05. Why???
        {
            //Debug.Log("Marching value cool");
            //disregard undug chambers
            if (NestParts[nestPartIndex].HasBeenDug())
                return true;
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
                if (PointInNestPart(chunk.cornerIdToPos[i] + surface.pos, type))
                    return true;
        }
        return false;
    }

    public static int GetCubeNestPart(Vector3Int cubePos, NestPart.NestPartType mode)
    {
        for (int i = 0; i < NestParts.Count; i++)
        {
            if (NestParts[i].mode == mode && NestParts[i].HasBeenDug())
            {
                for (int cornerId = 0; cornerId < 8; cornerId++)
                {
                    float marchingValue = NestParts[i].getMarchingValue(cubePos + chunk.cornerIdToPos[cornerId]);
                    if (marchingValue < WorldGen.isolevel)
                    {
                        return i;
                    }
                }
            }
        }
        return -1;
    }




    //for shuffleing lists, used to randomly select nest task and points in chambers
    private static System.Random rng = new System.Random();


    public static bool GetPointInChamber(NestPart.NestPartType type, out Vector3 point)
    {
        List<int> available = new();
        for (int i = 0; i < NestParts.Count; i++)
        {
            if (NestParts[i].mode == type)
                if (NestParts[i].HasBeenDug() && NestParts[i].gotPoints)
                    available.Add(i);
        }
        point = Vector3.zero;
        if (available.Count == 0) return false;
        
        int randIndex = rng.Next(available.Count);//get random chamber

        return GetPointInChamber(available[randIndex], out point);

    }

    public static bool GetPointInChamber(int index, out Vector3 point)
    {
        point = Vector3.zero;
        if (index >= NestParts.Count) return false;
        if (NestParts[index].mode == NestPart.NestPartType.Tunnel) return false;

        int terrainLayer = (1 << 6); //terrain layer
        for (int i = 0; i < 1000; i++) //Just in case it gets stuck, i guess.
        {
            Vector3 center = NestParts[index].getStartPos();
            Vector3 dim = NestParts[index].getEndPos() - center;//Get dimensions of chamber

            float x = (rng.Next((int)(Mathf.Abs(dim.x) * 100)) / 100f - dim.x / 2) * 0.6f;
            float z = (rng.Next((int)(Mathf.Abs(dim.z) * 100)) / 100f - dim.z / 2) * 0.6f;


            if (NestParts[index].getMarchingValue(new(x + center.x, center.y, z + center.z)) < WorldGen.isolevel * 0.60f) //if in center of chamber
                if (Physics.Raycast(new Vector3(x + center.x, center.y, z + center.z), Vector3.down * (Mathf.Abs(dim.y) + 0.3f), out RaycastHit hit, Mathf.Abs(dim.y) + 0.3f, terrainLayer))
                {
                    point = hit.point;

                    Debug.DrawLine(new Vector3(x + center.x, center.y, z + center.z), point, Color.red, 100);
                    if (PointInNestPart(point + Vector3.up * 0.1f, index))
                    {
                        Debug.DrawLine(center, point, Color.black, 100);
                        return true;
                    }
                }
        }
        return false;
    }

    public static void Shuffle<T>(IList<T> list)
    {
        int n = list.Count;
        while (n > 1)
        {
            n--;
            int k = rng.Next(n + 1);
            T value = list[k];
            list[k] = list[n];
            list[n] = value;
        }
    }

}
