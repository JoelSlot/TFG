using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering;

public class Nest : MonoBehaviour
{
    public static List<NestPart> NestParts = new();
    private static int lastIndex = 0;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

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
        for (int i = 0; i < 8; i++)
        {
            if (surface.surfaceGroup[i])
                if (PointInNestPart(chunk.cornerTable[i] + surface.pos, type))
                    return true;
        }
        return false;
    }

}
