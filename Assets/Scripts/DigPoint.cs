using System;
using System.Collections.Generic;
using UnityEngine;

public class DigPoint : MonoBehaviour
{

    public WorldGen WG;

    static public GameObject origDigPoint;

    public class digPointData{
        public float value;
        public List<NestPart> parents;
        public bool instantiated;

        public digPointData(float val, NestPart parent)
        {
            value = val;
            parents = new();
            if (parent != null) parents.Add(parent);
            instantiated = false;
        }

        public void update(digPointData newData)
        {
            value = Mathf.Min(newData.value, value);
            foreach (var parent in newData.parents)
                parents.Add(parent);
        }

        public void InstantiatePoint(Vector3 pos)
        {
            if (!instantiated)
            {
                Debug.Log("I HAVE BEEN CREATED AT " + pos);
                GameObject digPoint = Instantiate(DigPoint.origDigPoint, pos, Quaternion.identity);
                digPoint.SetActive(true);
                digPoint.name = "DigPoint (" + pos.x + ", " + pos.y + ", " + pos.z + ")";
                instantiated = true;
            }
        }
    }

    public static Dictionary<Vector3Int, digPointData> digPointDict = new();
    //ORIGINALLY USED A HASHSET TO SEE WITCH ONES HADN?T BEEN INITIALIZED BUT NOW JUST USES TUPLE IN DICT

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void Dig()
    {
        //Comenzamos la lista de puntos a editar con el digPoint mismo
        Vector3Int pos = Vector3Int.RoundToInt(transform.position);
        float val = digPointDict[pos].value;
        List<Tuple<Vector3Int, float>> terrainEdit = new();
        if (WorldGen.SampleTerrain(pos) > val) terrainEdit.Add(new Tuple<Vector3Int, float>(pos, val));
        
        //Miramos todos los digPoints alrededores
        Vector3Int[] directions = {Vector3Int.up, Vector3Int.down, Vector3Int.left, Vector3Int.right, Vector3Int.forward, Vector3Int.back};
        foreach (Vector3Int direction in directions)
        {
            if (digPointDict.ContainsKey(pos + direction))
            {
                digPointData nextDigData = digPointDict[pos + direction];
                if ( nextDigData.value > WorldGen.isolevel) //si es pared lo excavamos y lo eliminamos del diccionario
                {
                    if (WorldGen.SampleTerrain(pos) > nextDigData.value) terrainEdit.Add(new Tuple<Vector3Int, float>(pos + direction, nextDigData.value));
                    digPointDict.Remove(pos + direction);
                }
                else
                {
                    Debug.Log("AM instantiated: " + nextDigData.instantiated);
                    //Inicializamos si no lo está
                    nextDigData.InstantiatePoint(pos + direction);
                    //Quitar un poco de los alrededores
                    float newVal = WorldGen.SampleTerrain(pos + direction) - 0.2f;
                    if (newVal > nextDigData.value) //Si el valor es mayor que el valor min que se queire obtener:
                        if (WorldGen.SampleTerrain(pos) > newVal) terrainEdit.Add(new Tuple<Vector3Int, float>(pos+direction, newVal)); //Lo ponemos al valor obtenido
                }
            }
        }
        digPointDict.Remove(pos);
        if (terrainEdit.Count > 0) WG.EditTerrainSet(terrainEdit);
    }
    
    public Task getObjective()
    {
        return new Task(this);
    }

}

