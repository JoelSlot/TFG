using System;
using System.Collections.Generic;
using NUnit.Framework.Constraints;
using Unity.Multiplayer.Center.Common;
using UnityEngine;
using UnityEngine.Animations;

public class Corn : MonoBehaviour
{


    public static Dictionary<int, Corn> cornDictionary = new();

    public int id;

    public int antId = -1; //Id of the ant going to pick it up

    public MeshRenderer renderer;


    void OnDestroy()
    {
        Nest.RemovePip(id);
        cornDictionary.Remove(id);
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        //Puts the corn back into nest if falling to void.
        if (transform.parent == null)
        {
            if (transform.position.y < 0)
                if (Nest.IsPipInNest(id, out int partIndex))
                    if (Nest.GetPointInChamber(partIndex, out Vector3 point))
                        transform.position = point + Vector3.up;
        }

    }

    public static int getNextId()
    {
        int index = 0;
        while (true)
        {
            if (!cornDictionary.ContainsKey(index)) return index;
            index++;
        }
    }

    public static void registerCorn(Corn newCorn)
    {
        if (cornDictionary.ContainsValue(newCorn)) return;
        newCorn.id = getNextId();
        cornDictionary.Add(newCorn.id, newCorn);

    }

    public static bool registerCorn(Corn newCorn, int id)
    {
        if (cornDictionary.ContainsKey(id)) return false;
        newCorn.id = id;
        cornDictionary.Add(id, newCorn);
        return true;
    }

    public int holderAntIndex()
    {
        Ant holder = GetHolder();
        if (holder != null)
        {
            return holder.id;
        }
        return -1;
    }

    public Ant GetHolder()
    {
        Transform parent = transform.parent;
        if (parent == null) return null;
        while (parent.parent != null)
        {
            parent = parent.parent;
        }
        if (parent != null) return parent.GetComponent<Ant>();
        return null;
    }

    public static void PlaceCorn(CubePaths.CubeSurface antSurface, GameObject cornObj, Ant ant, bool inNest)
    {
        Corn corn = cornObj.GetComponent<Corn>();
        if (corn == null)
        {
            Debug.Log("WRONG_______-----------------------------------------------------------------------");
            return;
        }

        cornObj.AddComponent<Rigidbody>();
        cornObj.GetComponent<BoxCollider>().enabled = true;
        if (inNest)
        {
            //Añadir pepita al nido. Si no se encuentra la hormiga en una cámara de comida, se encontrará en id -1 y tendrá que ser movido
            int nestPartId = Nest.GetSurfaceChamberIndex(antSurface);
            Nest.AddPip(corn.id, nestPartId);
            Vector3 dir;
            if (nestPartId != -1)
            {
                Vector3 chamberCenter = Nest.NestParts[nestPartId].getStartPos();
                dir = (chamberCenter - corn.transform.position).normalized;
            }
            else
                dir = (ant.transform.up * 2 + ant.transform.position - corn.transform.position).normalized;

            while (!WorldGen.IsAboveSurface(corn.transform.position - dir * 0.3f) && !WorldGen.IsAboveSurface(corn.transform.position))
            {
                corn.transform.position += dir * 0.3f;
            }
        }

    }

    public void Show()
    {
        renderer.enabled = true;
    }

    public void Hide()
    {
        renderer.enabled = false;
    }
    

}
