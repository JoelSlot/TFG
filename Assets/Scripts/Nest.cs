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
    private static int lastIndex = 0;
    public static int foodCount = 0;

    public static bool NestVisible = false;
    public static bool[] NestPartDisabled = { false, false, false, false };
    public static void WriteVisibleValues()
    {
        Debug.Log("General visible " + NestVisible + "\nTunnel: " + NestPartDisabled[0] + "\nFood: " + NestPartDisabled[1] + "\nEgg: " + NestPartDisabled[2] + "\nQueen: " + NestPartDisabled[3]);
    }

    //All items placed in nest with wrong nestid go here.
    public static HashSet<int> lostPips = new();
    public static HashSet<int> lostEggs = new();



    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }


    public static void UpdateFoodCount()
    {
        foodCount = 0;
        foreach (var part in NestParts)
        {
            foodCount += part.CollectedCornPips.Count;
        }
    }

    public static List<int> GetDisplacedPips()
    {
        List<int> pips = lostPips.ToList();
        foreach (var part in NestParts)
        {
            if (part.mode != NestPart.NestPartType.FoodChamber)
                foreach (int id in part.CollectedCornPips)
                    pips.Add(id);
        }

        return pips;
    }

    public static List<int> GetDisplacedEggs()
    {
        List<int> eggs = lostEggs.ToList();
        foreach (var part in NestParts)
        {
            if (part.mode != NestPart.NestPartType.EggChamber)
                foreach (int id in part.AntEggs)
                    eggs.Add(id);
        }
        Debug.Log("Lost eggs size: " + eggs.Count);

        return eggs;
    }

    public static void RemovePip(int pipId)
    {
        foreach (var part in NestParts)
            if (part.CollectedCornPips.Remove(pipId))
                foodCount--;

        if (lostPips.Remove(pipId))
            foodCount--;
    }
    public static void AddPip(int pipId, int nestPartIndex)
    {
        if (nestPartIndex >= NestParts.Count || nestPartIndex < 0)
            if (lostPips.Add(pipId))
                foodCount++;
            else
            if (NestParts[nestPartIndex].CollectedCornPips.Add(pipId))
                foodCount++;
    }
    public static void RemoveEgg(int antId)
    {
        foreach (var part in NestParts)
            part.AntEggs.Remove(antId);

        lostEggs.Remove(antId);
    }
    public static void AddEgg(int eggId, int nestPartIndex)
    {
        if (nestPartIndex >= NestParts.Count || nestPartIndex < 0)
            lostEggs.Add(eggId);
        else
            NestParts[nestPartIndex].AntEggs.Add(eggId);
    }

    public static bool IsPipInNest(int pipId, out int nestPartIndex) //index is -1 if not in 
    {
        nestPartIndex = -1;
        if (lostPips.Contains(pipId))
            return true;

        nestPartIndex = 0;
        for (; nestPartIndex < NestParts.Count; nestPartIndex++)
            if (NestParts[nestPartIndex].CollectedCornPips.Contains(pipId))
                return true;

        return false;
    }

    public static bool IsEggInNest(int eggId, out int nestPartIndex) //index is -1 if not in 
    {
        nestPartIndex = -1;
        if (lostEggs.Contains(eggId))
            return true;

        nestPartIndex = 0;
        for (; nestPartIndex < NestParts.Count; nestPartIndex++)
            if (NestParts[nestPartIndex].AntEggs.Contains(eggId))
                return true;

        return false;
    }



    public static bool HasNestPart(NestPart.NestPartType type)
    {
        foreach (var part in NestParts)
        {
            if (part.mode == type) return true;
        }
        return false;
    }

    public static bool HasDugNestPart(NestPart.NestPartType type)
    {
        foreach (var part in NestParts)
        {
            if (part.mode == type)
                if (part.HasBeenDug())
                    return true;
        }
        return false;
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
                    objective = Task.DigTask(pos, antId, newPath);
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

        if (!SurfaceInNest(antSurface)) return false;

        List<int> shuffledIds = KnownCornCobs.ToList<int>();
        Shuffle(shuffledIds);

        foreach (int cobId in shuffledIds)
        {
            //Selecciona un indice de los conocidos aleatorio
            if (CornCob.cornCobDictionary.TryGetValue(cobId, out CornCob cob))
            {
                if (!cob.hasCorn())
                {
                    KnownCornCobs.Remove(cobId);
                    continue;
                }
                else if (CubePaths.GetKnownPathToPoint(antSurface, cob.transform.position, 2.8f, out List<CubePaths.CubeSurface> path))
                {
                    objective = new Task(cob.gameObject, TaskType.CollectFromCob, path);
                    return true;
                }
                else
                {
                    //Quitamos de la lista si desde el nido mismo no se puede llegar.
                    KnownCornCobs.Remove(cobId);
                    continue;
                }
            }
            else
            {
                //Quitamos de la lista si es no válido.
                KnownCornCobs.Remove(cobId);
                continue;
            }

        }

        return false;
    }

    public static bool GetNestRelocatePipTask(CubePaths.CubeSurface antSurface, int antId, ref Task objective)
    {
        if (!HasDugNestPart(NestPart.NestPartType.FoodChamber)) return false;

        List<int> displacedPips = GetDisplacedPips();
        Debug.Log("Relocated cornpips: " + displacedPips.Count);
        foreach (int id in displacedPips)
        {
            //if already being relocated go to next
            if (Task.IsCornBeingPickedUp(id)) continue;

            if (CubePaths.GetKnownPathToPoint(antSurface, Corn.cornDictionary[id].transform.position, 1.2f, out List<CubePaths.CubeSurface> newPath))
            {
                objective = Task.GetCornTask(id, antId, newPath);
                return true;
            }
        }
        return false;
    }

    public static bool GetNestRelocateEggTask(CubePaths.CubeSurface antSurface, int antId, ref Task objective)
    {
        if (!HasDugNestPart(NestPart.NestPartType.EggChamber)) return false;

        List<int> displacedEggs = GetDisplacedEggs();
        foreach (int id in displacedEggs)
        {
            //if already being relocated go to next
            if (Task.IsEggBeingPickedUp(id)) continue;

            if (CubePaths.GetKnownPathToPoint(antSurface, Ant.antDictionary[id].transform.position, 1.2f, out List<CubePaths.CubeSurface> newPath))
            {
                objective = Task.GetEggTask(id, antId, newPath);
                return true;
            }
        }
        return false;
    }

    public static bool GetNestRelocateTask(CubePaths.CubeSurface antSurface, int antId, ref Task objective)
    {
        if (GetNestRelocateEggTask(antSurface, antId, ref objective)) return true;
        if (GetNestRelocatePipTask(antSurface, antId, ref objective)) return true;
        return false;
    }

    public static bool PointInNest(Vector3 point)
    {
        int checkedParts = 0;
        for (int i = lastIndex; checkedParts < NestParts.Count; checkedParts++, i++)
        {
            i %= NestParts.Count;
            if (!NestParts[i].HasBeenPlaced()) continue;
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
            if (!NestParts[i].HasBeenPlaced()) continue;
            //Solo miramos las partes del nido con el mismo tipo que hayan sido excavados (si no son túneles)
            if (NestParts[i].mode == type && (type == NestPart.NestPartType.Tunnel || NestParts[i].HasBeenDug()))
            {
                float marchingValue = NestParts[i].getMarchingValue(point);
                if (marchingValue < WorldGen.isolevel) // there was a * 1.05. Why???
                {
                    lastIndex = i;
                    return true;
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


    public static bool GetPointInAnyChamber(out Vector3 point)
    {
        List<int> available = new();
        for (int i = 0; i < NestParts.Count; i++)
        {
            if (NestParts[i].mode != NestPart.NestPartType.Tunnel)
                if (NestParts[i].HasBeenDug() && NestParts[i].gotPoints)
                    available.Add(i);
        }
        point = Vector3.zero;
        if (available.Count == 0) return false;

        int randIndex = rng.Next(available.Count);//get random chamber

        return GetPointInChamber(available[randIndex], out point);

    }

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
        if (index >= NestParts.Count || index < 0) return false;
        if (NestParts[index].mode == NestPart.NestPartType.Tunnel) return false;
        if (!NestParts[index].HasBeenPlaced()) return false;
        if (!NestParts[index].HasBeenDug()) return false;

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

    public static bool CornInAcceptableNestPart(int id)
    {
        if (!IsPipInNest(id, out int partIndex)) return false; //not in nest? unacceptable

        if (!HasDugNestPart(NestPart.NestPartType.FoodChamber)) return true; //in nest, but no foodchamber around? acceptable

        if (partIndex == -1) return false; //if default is lost, ignore.

        if (NestParts[partIndex].mode == NestPart.NestPartType.FoodChamber) return true; //In foodchamber? good

        return false; //theres a foodchamber and it is not in it...
    }
    
    public static bool EggInAcceptableNestPart(int id)
    {
        if (!IsEggInNest(id, out int partIndex)) return false; //not in nest? unacceptable

        if (!HasDugNestPart(NestPart.NestPartType.EggChamber)) return true; //in nest, but no eggchamber around? acceptable

        if (NestParts[partIndex].mode == NestPart.NestPartType.EggChamber) return true; //In eggchamber? good

        return false; //theres a eggchamber and it is not in it...
    }
}
