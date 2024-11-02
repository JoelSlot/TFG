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
    public static int x_dim = 100;
    public static int y_dim = 50;
    public static int z_dim = 100;
    public static int chunk_x_dim = 10;
    public static int chunk_y_dim = 50;
    public static int chunk_z_dim = 10;

    int num_chunks_x;
    int num_chunks_y;
    int num_chunks_z;

    public static float[,,] terrainMap;
    Dictionary<Vector3Int, chunk> chunks = new Dictionary<Vector3Int, chunk>();


    // Start is called before the first frame update
    void Start()
    {

        terrainMap = new float[x_dim + 1, y_dim + 1, z_dim + 1];
        num_chunks_x = Mathf.FloorToInt(x_dim / chunk_x_dim);
        num_chunks_y = Mathf.FloorToInt(y_dim / chunk_y_dim);
        num_chunks_z = Mathf.FloorToInt(z_dim / chunk_z_dim);
        PopulateTerrainMap();
        GenerateChunks();
        CreateNavMesh();
    }

    public void GenerateChunks() 
    {

        for (int x = 0;  x < world_x; x++) {
            for (int z = 0; z < world_z; z++) {
                Vector3Int newPos = new Vector3Int(x * chunk_x_dim, 0, z * chunk_z_dim);
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

        for (int x = 0; x < x_dim + 1; x++)
        {
            for (int y = 0; y < y_dim + 1; y++)
            {
                for (int z = 0; z < z_dim + 1; z++)
                {
                    if (z == 0 || z == z_dim + 1 || x == 0 || x == x_dim + 1)
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

    
    public void EditTerrain(List<Vector3Int> points, List<float> val, bool add)
    {
        var h = new HashSet<Vector3Int>();

        for (var i = 0; i < points.Count; i++)
        {
            if (points[i].x < x_dim && points[i].y < y_dim && points[i].z < z_dim && points[i].x >= 0 && points[i].y >= 0 && points[i].z >= 0)
            {
                if (add)
                    terrainMap[points[i].x, points[i].y, points[i].z] -= val[i] * 0.8f;
                else
                    terrainMap[points[i].x, points[i].y, points[i].z] += val[i] * 0.8f;
                if (terrainMap[points[i].x, points[i].y, points[i].z] < 0)
                    terrainMap[points[i].x, points[i].y, points[i].z] = 0f;
                if (terrainMap[points[i].x, points[i].y, points[i].z] > 1)
                    terrainMap[points[i].x, points[i].y, points[i].z] = 1f;
                h.Add(new Vector3Int((points[i].x / chunk_x_dim) * chunk_x_dim, 0, (points[i].z / chunk_z_dim ) * chunk_z_dim));
                bool x_0 = false;
                //mirar si está justo entre dos chunks en x y no al principio
                if (points[i].x % chunk_x_dim == 0 && points[i].x != 0)
                {
                    x_0 = true;
                    h.Add(new Vector3Int((points[i].x / chunk_x_dim - 1) * chunk_x_dim, 0, (points[i].z / chunk_z_dim) * chunk_z_dim));
                }
                bool z_0 = false;
                if (points[i].z % chunk_z_dim == 0 && points[i].z != 0)
                {
                    z_0 = true;
                    h.Add(new Vector3Int((points[i].x / chunk_x_dim) * chunk_x_dim, 0, (points[i].z / chunk_z_dim - 1) * chunk_z_dim));
                }
                if (x_0 && z_0)
                {
                    h.Add(new Vector3Int((points[i].x / chunk_x_dim - 1) * chunk_x_dim, 0, (points[i].z / chunk_z_dim - 1) * chunk_z_dim));
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
            if (points[i].x < x_dim && points[i].y < y_dim && points[i].z < z_dim && points[i].x >= 0 && points[i].y >= 0 && points[i].z >= 0)
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


        x_dim = int.Parse(values[0]);
        y_dim = int.Parse(values[1]);
        z_dim = int.Parse(values[2]);

        terrainMap = new float[x_dim + 1, y_dim + 1, z_dim + 1];
        Debug.Log("Reading files");
        int index = 3;
        for (int x = 0; x < x_dim + 1; x++)
        {
            for (int y = 0; y < y_dim + 1; y++)
            {
                for (int z = 0; z < z_dim + 1; z++)
                {
                    terrainMap[x, y, z] = float.Parse(values[index]);
                    index++;
                }
            }
        }


        world_x = Mathf.FloorToInt(x_dim / chunk_x_dim);
        world_z = Mathf.FloorToInt(z_dim / chunk_z_dim);

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
        writer.Write(x_dim.ToString() + " " + y_dim.ToString() + " " + z_dim.ToString());
        for (int x = 0; x < x_dim + 1; x++)
        {
            for (int y = 0; y < y_dim + 1; y++)
            {
                for (int z = 0; z < z_dim + 1; z++) {
                    writer.Write(" " + terrainMap[x, y, z].ToString());
                }
            }
        }
        writer.Close();
        Debug.Log("Saved succesfully!");
    }
}
