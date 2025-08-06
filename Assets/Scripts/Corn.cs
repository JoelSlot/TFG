using System.Collections.Generic;
using Unity.Multiplayer.Center.Common;
using UnityEngine;
using UnityEngine.Animations;

public class Corn : MonoBehaviour
{


    public static Dictionary<int, Corn> cornDictionary = new();

    public int id;

    public int antId = -1; //Id of the ant going to pick it up


    void OnDestroy()
    {
        cornDictionary.Remove(id);
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
        
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
}
