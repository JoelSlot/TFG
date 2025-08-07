using System;
using System.Collections.Generic;
using UnityEngine;

public class DigPoint : MonoBehaviour
{

    //Remember that this class only exists to be a fisicial object. All the relevant info is in digPointData.
    //But it is still necesary


    public static Dictionary<Vector3Int, digPointData> digPointDict = new();
    public static HashSet<Vector3Int> availableDigPoints = new();


    public class digPointData
    {
        public int value { get; set; }
        public DigPoint digPoint = null;
        public int antId { get; set; } //Id of the ant that is getting the digpoint

        private digPointData()
        {

        }

        public digPointData(int val)
        {
            value = val;
            digPoint = null;
            antId = -1;
        }

        public void update(digPointData newData)
        {
            value = Mathf.Min(newData.value, value);
        }

        public void InstantiatePoint(Vector3Int pos, bool loaded) //loaded bool so that not readded to availableDigPoints when loaded
        {
            if (digPoint == null)
            {
                //Debug.Log("I HAVE BEEN CREATED AT " + pos);
                digPoint = WorldGen.InstantiateDigPoint(pos);
                //Add to available digpoints
                if (!loaded) availableDigPoints.Add(Vector3Int.RoundToInt(digPoint.transform.position));
            }
        }
    }

    void OnDestroy()
    {
        digPointDict.Remove(Vector3Int.RoundToInt(transform.position));
    }

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
        int val = digPointDict[pos].value;
        List<Tuple<Vector3Int, int>> terrainEdit = new();
        if (WorldGen.SampleTerrain(pos) > val) terrainEdit.Add(new Tuple<Vector3Int, int>(pos, val));

        //Miramos todos los digPoints alrededores
        Vector3Int[] directions = { Vector3Int.up, Vector3Int.down, Vector3Int.left, Vector3Int.right, Vector3Int.forward, Vector3Int.back };
        foreach (Vector3Int direction in directions)
        {
            Vector3Int key = pos + direction;
            if (digPointDict.ContainsKey(key))
            {
                digPointData nextDigData = digPointDict[key];
                if (IsPointless(key)) //Si ahora da igual excavar el punto, lo eliminamos y editamos el mapa
                {
                    terrainEdit.Add(new Tuple<Vector3Int, int>(key, nextDigData.value));
                    digPointDict.Remove(key);
                    Destroy(nextDigData.digPoint.gameObject);
                }
                else if (nextDigData.value > WorldGen.isolevel) //si es pared lo excavamos y lo eliminamos del diccionario
                {
                    if (WorldGen.SampleTerrain(pos) > nextDigData.value) terrainEdit.Add(new Tuple<Vector3Int, int>(pos + direction, nextDigData.value));
                    digPointDict.Remove(key);
                }
                else
                {
                    //Debug.Log("AM instantiated: " + nextDigData.digPoint != null);
                    //Inicializamos si no lo está
                    nextDigData.InstantiatePoint(key, false);
                    //Quitar un poco de los alrededores
                    int newVal = WorldGen.SampleTerrain(key) - 2;
                    if (newVal > nextDigData.value) //Si el valor es mayor que el valor min que se queire obtener:
                        if (WorldGen.SampleTerrain(pos) > newVal) terrainEdit.Add(new Tuple<Vector3Int, int>(pos + direction, newVal)); //Lo ponemos al valor obtenido
                }
            }
        }
        digPointDict.Remove(pos);


        if (terrainEdit.Count > 0) WorldGen.EditTerrainSet(terrainEdit);
    }

    //Used to score digPoints on how reachable they are.
    public static int ReachableScore(Vector3Int pos)
    {
        int score = 0;
        Vector3Int[] directions = { Vector3Int.up, Vector3Int.down, Vector3Int.left, Vector3Int.right, Vector3Int.forward, Vector3Int.back };
        foreach (Vector3Int direction in directions)
        {
            Vector3Int adj = pos + direction;

            if (WorldGen.IsAboveSurface(adj)) score++;
        }
        return score;
    }

    //No tiene sentido crear un digpoint donde no va a tener un efecto: que el y todos sus alrededores ya están sobre la superficie.
    public static bool IsPointless(Vector3Int pos)
    {
        if (!WorldGen.IsAboveSurface(pos)) return false;
        Vector3Int[] directions = { Vector3Int.up, Vector3Int.down, Vector3Int.left, Vector3Int.right, Vector3Int.forward, Vector3Int.back };
        foreach (Vector3Int direction in directions)
        {
            Vector3Int adj = pos + direction;
            if (!WorldGen.IsAboveSurface(adj)) return false;
        }

        return true;
    }

}

