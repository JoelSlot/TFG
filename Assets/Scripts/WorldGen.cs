using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using static UnityEngine.Awaitable;
using UnityEngine.AI;
using UnityEngine.UIElements;

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
    }

    /*
     * 
     */
    public void GenerateChunks()
    {
        //Itera sobre todas las secciones de tama�o de chunk del mapa
        for (int x = 0; x < num_chunks_x; x++)
        {
            for (int y = 0; y < num_chunks_y; y++)
            {
                for (int z = 0; z < num_chunks_z; z++)
                {
                    //Obtiene posici�n del chunk
                    Vector3Int newPos = new Vector3Int(x * chunk_x_dim, y * chunk_y_dim, z * chunk_z_dim);
                    //A�ade nuevo chunk y su posici�n al diccionario
                    chunks.Add(newPos, new chunk(newPos, this));
                    //aplica transformaci�n de WorldGen al chunk y situa chunk en capa 6.
                    chunks[newPos].chunkObject.transform.SetParent(transform);
                    chunks[newPos].chunkObject.layer = 6;
                }
            }
        }

        Debug.Log(string.Format("{0} by {1} by {2} world generated", num_chunks_x, num_chunks_y, num_chunks_z));

    }


    /*
     * Crea el terreno. 
     */
    public void PopulateTerrainMap()
    {
        //Itera sobre todos los puntos del campo escalar
        for (int x = 0; x < x_dim + 1; x++)
        {
            for (int y = 0; y < y_dim + 1; y++)
            {
                for (int z = 0; z < z_dim + 1; z++)
                {
                    if (z == 0 || z == z_dim || x == 0 || x == x_dim)
                        terrainMap[x, y, z] = 1f;
                    else if (y == 10)
                        terrainMap[x, y, z] = 0.3f;
                    else if (y < 10)
                        terrainMap[x,y,z] = 0f;
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
                h.Add(new Vector3Int((points[i].x / chunk_x_dim) * chunk_x_dim, 0, (points[i].z / chunk_z_dim) * chunk_z_dim));
                bool x_0 = false;
                //mirar si est� justo entre dos chunks en x y no al principio
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
            createMeshData();

    }
    */
    public static float SampleTerrain(Vector3Int point)
    {
        if (point.x < 0 || point.x >= x_dim || point.y < 0 || point.y >= y_dim || point.z < 0 || point.z >= z_dim) return 1;
        return terrainMap[point.x, point.y, point.z];
    }
    public static float SampleTerrain(int x, int y, int z)
    {
        if (x < 0 || x >= x_dim || y < 0 || y >= y_dim || z < 0 || z >= z_dim) return 1;
        return terrainMap[x,y,z];
    }
    public static float SampleTerrain(Vector3 point)
    {
        if (point.x < 0 || point.x >= x_dim || point.y < 0 || point.y >= y_dim || point.z < 0 || point.z >= z_dim) return 1;

        // las esquinas del cubo en el que se encuentra el punto
        int x0 = Mathf.FloorToInt(point.x);
        int x1 = x0 + 1;
        int y0 = Mathf.FloorToInt(point.y);
        int y1 = y0 + 1;
        int z0 = Mathf.FloorToInt(point.z);
        int z1 = z0 + 1;

        // pos relativa dentro del cubo
        float xd = point.x - x0;
        float yd = point.y - y0;
        float zd = point.z - z0;

        // Interpolar por eje x
        float c00 = Mathf.Lerp(SampleTerrain(x0, y0, z0), SampleTerrain(x1, y0, z0), xd);
        float c01 = Mathf.Lerp(SampleTerrain(x0, y0, z1), SampleTerrain(x1, y0, z1), xd);
        float c10 = Mathf.Lerp(SampleTerrain(x0, y1, z0), SampleTerrain(x1, y1, z0), xd);
        float c11 = Mathf.Lerp(SampleTerrain(x0, y1, z1), SampleTerrain(x1, y1, z1), xd);

        // Interpolar por eje y
        float c0 = Mathf.Lerp(c00, c10, yd);
        float c1 = Mathf.Lerp(c01, c11, yd);

        // Interpolar por eje z
        return Mathf.Lerp(c0, c1, zd);
    }
    public static bool IsAboveSurface(Vector3 point)
    {
        return !(isolevel > SampleTerrain(point));
    }
    public static bool IsAboveSurface(Vector3Int point)
    {
        return !(isolevel > SampleTerrain(point));
    }
    public static Vector3 SurfaceDirection(Vector3Int point)
    {
        if (!IsAboveSurface(point)) return Vector3.up;
        Vector3 direction = new Vector3(0,0,0);
        if (!IsAboveSurface(new Vector3(point.x-1, point.y, point.z))) direction.x -=1;
        if (!IsAboveSurface(new Vector3(point.x+1, point.y, point.z))) direction.x +=1;
        if (!IsAboveSurface(new Vector3(point.x, point.y-1, point.z))) direction.y -=1;
        if (!IsAboveSurface(new Vector3(point.x, point.y+1, point.z))) direction.y +=1;
        if (!IsAboveSurface(new Vector3(point.x, point.y, point.z-1))) direction.z -=1;
        if (!IsAboveSurface(new Vector3(point.x, point.y, point.z+1))) direction.z +=1;
        if (!(direction.x == 0 && direction.y == 0 && direction.z == 0)) return direction.normalized;
        if (!IsAboveSurface(new Vector3(point.x-1, point.y-1, point.z))) {direction.x -=1; direction.y -=1;}
        if (!IsAboveSurface(new Vector3(point.x-1, point.y+1, point.z))) {direction.x -=1; direction.y +=1;}
        if (!IsAboveSurface(new Vector3(point.x-1, point.y, point.z-1))) {direction.x -=1; direction.z -=1;}
        if (!IsAboveSurface(new Vector3(point.x-1, point.y, point.z+1))) {direction.x -=1; direction.z +=1;}
        if (!IsAboveSurface(new Vector3(point.x+1, point.y-1, point.z))) {direction.x +=1; direction.y -=1;}
        if (!IsAboveSurface(new Vector3(point.x+1, point.y+1, point.z))) {direction.x +=1; direction.y +=1;}
        if (!IsAboveSurface(new Vector3(point.x+1, point.y, point.z-1))) {direction.x +=1; direction.z -=1;}
        if (!IsAboveSurface(new Vector3(point.x+1, point.y, point.z+1))) {direction.x +=1; direction.z +=1;}
        if (!IsAboveSurface(new Vector3(point.x, point.y-1, point.z-1))) {direction.y -=1; direction.z -=1;}
        if (!IsAboveSurface(new Vector3(point.x, point.y-1, point.z+1))) {direction.y -=1; direction.z +=1;}
        if (!IsAboveSurface(new Vector3(point.x, point.y+1, point.z-1))) {direction.y +=1; direction.z -=1;}
        if (!IsAboveSurface(new Vector3(point.x, point.y+1, point.z+1))) {direction.y +=1; direction.z +=1;}
        return direction.normalized;
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


        num_chunks_x = Mathf.FloorToInt(x_dim / chunk_x_dim);
        num_chunks_y = Mathf.FloorToInt(y_dim / chunk_y_dim);
        num_chunks_z = Mathf.FloorToInt(z_dim / chunk_z_dim);

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
                for (int z = 0; z < z_dim + 1; z++)
                {
                    writer.Write(" " + terrainMap[x, y, z].ToString());
                }
            }
        }
        writer.Close();
        Debug.Log("Saved succesfully!");
    }
}
