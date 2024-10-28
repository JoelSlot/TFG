using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using static UnityEngine.Awaitable;
using UnityEngine.AI;

public class WorldGen : MonoBehaviour
{

    public static float isolevel = 0.5f;
    public static int xdim = 100;
    public static int ydim = 50;
    public static int zdim = 100;
    public static int chunk_x = 10;
    public static int chunk_y = 50;
    public static int chunk_z = 10;

    int world_x;
    int world_z;

    public static float[,,] terrainMap;
    Dictionary<Vector3Int, chunk> chunks = new Dictionary<Vector3Int, chunk>();


    // Start is called before the first frame update
    void Start()
    {

        terrainMap = new float[xdim + 1, ydim + 1, zdim + 1];
        world_x = Mathf.FloorToInt(xdim / chunk_x);
        world_z = Mathf.FloorToInt(zdim / chunk_z);
        PopulateTerrainMap();
        GenerateChunks();
        CreateNavMesh();
    }

    public void GenerateChunks() 
    {

        for (int x = 0;  x < world_x; x++) {
            for (int z = 0; z < world_z; z++) {
                Vector3Int newPos = new Vector3Int(x * chunk_x, 0, z * chunk_z);
                chunks.Add(newPos, new chunk(newPos, this));
                chunks[newPos].chunkObject.transform.SetParent(transform);
                chunks[newPos].chunkObject.layer = 6;
            }
        }

        Debug.Log(string.Format("{0} by {1} world generated", world_x, world_z));

    }


    /*
     * Crea el terreno. 
     */
    public void PopulateTerrainMap()
    {

        for (int x = 0; x < xdim + 1; x++)
        {
            for (int y = 0; y < ydim + 1; y++)
            {
                for (int z = 0; z < zdim + 1; z++)
                {
                    if (z == 0 || z == zdim + 1 || x == 0 || x == xdim + 1)
                        terrainMap[x, y, z] = 1f;
                    else if (y < 10)
                        terrainMap[x, y, z] = 0f;
                    else
                        terrainMap[x, y, z] = 1f;
                }
            }
        }


        Debug.Log(string.Format("Terrain generated"));

    }

    public void CreateNavMesh()
    {

    }
    
    public void EditTerrain(List<Vector3Int> points, List<float> val, bool add)
    {
        var h = new HashSet<Vector3Int>();

        for (var i = 0; i < points.Count; i++)
        {
            if (points[i].x < xdim && points[i].y < ydim && points[i].z < zdim && points[i].x >= 0 && points[i].y >= 0 && points[i].z >= 0)
            {
                if (add)
                    terrainMap[points[i].x, points[i].y, points[i].z] -= val[i] * 0.8f;
                else
                    terrainMap[points[i].x, points[i].y, points[i].z] += val[i] * 0.8f;
                if (terrainMap[points[i].x, points[i].y, points[i].z] < 0)
                    terrainMap[points[i].x, points[i].y, points[i].z] = 0f;
                if (terrainMap[points[i].x, points[i].y, points[i].z] > 1)
                    terrainMap[points[i].x, points[i].y, points[i].z] = 1f;
                h.Add(new Vector3Int((points[i].x / chunk_x) * chunk_x, 0, (points[i].z / chunk_z ) * chunk_z));
                bool x_0 = false;
                //mirar si está justo entre dos chunks en x y no al principio
                if (points[i].x % chunk_x == 0 && points[i].x != 0)
                {
                    x_0 = true;
                    h.Add(new Vector3Int((points[i].x / chunk_x - 1) * chunk_x, 0, (points[i].z / chunk_z) * chunk_z));
                }
                bool z_0 = false;
                if (points[i].z % chunk_z == 0 && points[i].z != 0)
                {
                    z_0 = true;
                    h.Add(new Vector3Int((points[i].x / chunk_x) * chunk_x, 0, (points[i].z / chunk_z - 1) * chunk_z));
                }
                if (x_0 && z_0)
                {
                    h.Add(new Vector3Int((points[i].x / chunk_x - 1) * chunk_x, 0, (points[i].z / chunk_z - 1) * chunk_z));
                }
            }
        }
        //check what chunks it affects
        foreach (Vector3Int point in h)
        {
            chunks[point].CreateMeshData();
        }

    }
    /*
    public void RemoveTerrain(List<Vector3Int> points, List<float> val)
    {
        bool changed = false;

        for (var i = 0; i < points.Count; i++)
        {
            if (points[i].x < xdim && points[i].y < ydim && points[i].z < zdim && points[i].x >= 0 && points[i].y >= 0 && points[i].z >= 0)
            {
                terrainMap[points[i].x, points[i].y, points[i].z] -= val[i];
                if (terrainMap[points[i].x, points[i].y, points[i].z] < 0)
                    terrainMap[points[i].x, points[i].y, points[i].z] = 0f;
                changed = true;
            }
        }
        if (changed)
            CreateMeshData();

    }
    */
    public float SampleTerrain(Vector3Int point)
    {
        return terrainMap[point.x, point.y, point.z];
    }

    public void LoadMap()
    {
        Debug.Log("Starting loading");

        string line = File.ReadAllText("map.txt");
        string[] values = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);


        xdim = int.Parse(values[0]);
        ydim = int.Parse(values[1]);
        zdim = int.Parse(values[2]);

        terrainMap = new float[xdim + 1, ydim + 1, zdim + 1];
        Debug.Log("Reading files");
        int index = 3;
        for (int x = 0; x < xdim + 1; x++)
        {
            for (int y = 0; y < ydim + 1; y++)
            {
                for (int z = 0; z < zdim + 1; z++)
                {
                    terrainMap[x, y, z] = float.Parse(values[index]);
                    index++;
                }
            }
        }


        world_x = Mathf.FloorToInt(xdim / chunk_x);
        world_z = Mathf.FloorToInt(zdim / chunk_z);

        foreach (var item in chunks.Values)
        {
            item.Destroy();
        }
        chunks.Clear();

        GenerateChunks();
        Debug.Log("Loaded succesfully!");
        GC.Collect();

    }

    public void SaveMap()
    {
        Debug.Log("Starting save");
        StreamWriter writer = new StreamWriter("map.txt");
        writer.Write(xdim.ToString() + " " + ydim.ToString() + " " + zdim.ToString());
        for (int x = 0; x < xdim + 1; x++)
        {
            for (int y = 0; y < ydim + 1; y++)
            {
                for (int z = 0; z < zdim + 1; z++) {
                    writer.Write(" " + terrainMap[x, y, z].ToString());
                }
            }
        }
        writer.Close();
        Debug.Log("Saved succesfully!");
    }
}
