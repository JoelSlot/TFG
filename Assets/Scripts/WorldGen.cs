using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using static UnityEngine.Awaitable;
using UnityEngine.AI;
using UnityEngine.UIElements;
using Unity.VisualScripting;
using System.Linq;
using Polenter.Serialization;

public class WorldGen : MonoBehaviour
{

    public static float isolevel = 127.5f;
    public static int x_dim = 100;
    public static int y_dim = 50;
    public static int z_dim = 100;
    public static int chunk_x_dim = 10;
    public static int chunk_y_dim = 50;
    public static int chunk_z_dim = 10;

    int num_chunks_x;
    int num_chunks_y;
    int num_chunks_z;

    public static int[,,] terrainMap;
    Dictionary<Vector3Int, chunk> chunks = new Dictionary<Vector3Int, chunk>();


    // Start is called before the first frame update
    void Start()
    {

        terrainMap = new int[x_dim + 1, y_dim + 1, z_dim + 1];
        num_chunks_x = Mathf.FloorToInt(x_dim / chunk_x_dim);
        num_chunks_y = Mathf.FloorToInt(y_dim / chunk_y_dim);
        num_chunks_z = Mathf.FloorToInt(z_dim / chunk_z_dim);
        PopulateTerrainMap();
        GenerateChunks();
        Physics.gravity = new Vector3(0, -15.0F, 0);
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
                        terrainMap[x, y, z] = 0;
                    else if (y == 30)
                        terrainMap[x, y, z] = 200;
                    else if (y == 0)
                        terrainMap[x,y,z] = 0;
                    else if (y < 30)
                        terrainMap[x,y,z] = 255;
                    else
                        terrainMap[x, y, z] = 0;
                }
            }
        }


        Debug.Log(string.Format("Terrain generated"));

    }


    public void EditTerrainAdd(List<Tuple<Vector3Int, int>> points, int degree)
    {
        var affectedChunks = new HashSet<Vector3Int>();

        for (var i = 0; i < points.Count; i++)
        {
            Vector3Int point = points[i].Item1;
            float val = points[i].Item2;
            if (InRange(point))
            {
                terrainMap[point.x, point.y, point.z] += Mathf.RoundToInt(val * degree);
                if (terrainMap[point.x, point.y, point.z] < 0)
                    terrainMap[point.x, point.y, point.z] = 0;
                if (terrainMap[point.x, point.y, point.z] > 255)
                    terrainMap[point.x, point.y, point.z] = 255;

                affectedChunks.Add(new Vector3Int((point.x / chunk_x_dim) * chunk_x_dim, 0, (point.z / chunk_z_dim) * chunk_z_dim));
                bool x_0 = false;
                //mirar si est� justo entre dos chunks en x y no al principio
                if (point.x % chunk_x_dim == 0 && point.x != 0)
                {
                    x_0 = true;
                    affectedChunks.Add(new Vector3Int((point.x / chunk_x_dim - 1) * chunk_x_dim, 0, (point.z / chunk_z_dim) * chunk_z_dim));
                }
                bool z_0 = false;
                if (point.z % chunk_z_dim == 0 && point.z != 0)
                {
                    z_0 = true;
                    affectedChunks.Add(new Vector3Int((point.x / chunk_x_dim) * chunk_x_dim, 0, (point.z / chunk_z_dim - 1) * chunk_z_dim));
                }
                if (x_0 && z_0)
                {
                    affectedChunks.Add(new Vector3Int((point.x / chunk_x_dim - 1) * chunk_x_dim, 0, (point.z / chunk_z_dim - 1) * chunk_z_dim));
                }
            }
        }
        //check what chunks it affects
        foreach (Vector3Int point in affectedChunks)
        {
            chunks[point].CreateMeshData();
        }

    }
    public void EditTerrainSet(List<Tuple<Vector3Int, int>> points)
    {
        var affectedChunks = new HashSet<Vector3Int>();

        for (var i = 0; i < points.Count; i++)
        {
            Vector3Int point = points[i].Item1;
            float val = points[i].Item2;
            if (InRange(point))
            {
                terrainMap[point.x, point.y, point.z] = Mathf.RoundToInt(Mathf.Clamp(val, 0, 255));
                affectedChunks.Add(new Vector3Int((point.x / chunk_x_dim) * chunk_x_dim, 0, (point.z / chunk_z_dim) * chunk_z_dim));
                bool x_0 = false;
                //mirar si est� justo entre dos chunks en x y no al principio
                if (point.x % chunk_x_dim == 0 && point.x != 0)
                {
                    x_0 = true;
                    affectedChunks.Add(new Vector3Int((point.x / chunk_x_dim - 1) * chunk_x_dim, 0, (point.z / chunk_z_dim) * chunk_z_dim));
                }
                bool z_0 = false;
                if (point.z % chunk_z_dim == 0 && point.z != 0)
                {
                    z_0 = true;
                    affectedChunks.Add(new Vector3Int((point.x / chunk_x_dim) * chunk_x_dim, 0, (point.z / chunk_z_dim - 1) * chunk_z_dim));
                }
                if (x_0 && z_0)
                {
                    affectedChunks.Add(new Vector3Int((point.x / chunk_x_dim - 1) * chunk_x_dim, 0, (point.z / chunk_z_dim - 1) * chunk_z_dim));
                }
            }
        }
        //check what chunks it affects
        foreach (Vector3Int point in affectedChunks)
        {
            chunks[point].CreateMeshData();
        }

    }

    public static bool InRange(Vector3Int point)
    {
        if (point.x < 0 || point.x >= x_dim || point.y < 0 || point.y >= y_dim || point.z < 0 || point.z >= z_dim) return false;
        return true;
    }
    public static bool InRange(Vector3 point)
    {
        if (point.x < 0 || point.x >= x_dim || point.y < 0 || point.y >= y_dim || point.z < 0 || point.z >= z_dim) return false;
        return true;
    }
    public static int SampleTerrain(Vector3Int point)
    {
        if (!InRange(point)) return 0;
        return terrainMap[point.x, point.y, point.z];
    }
    public static int SampleTerrain(int x, int y, int z)
    {
        if (!InRange(new Vector3Int(x, y, z))) return 0;
        return terrainMap[x,y,z];
    }
    public static int SampleTerrain(Vector3 point)
    {
        if (!InRange(point)) return 0;

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
        return Mathf.RoundToInt(Mathf.Lerp(c0, c1, zd));
    }

    public static bool IsAboveSurface(Vector3 point)
    {
        return isolevel > SampleTerrain(point);
    }
    public static bool IsAboveSurface(Vector3Int point)
    {
        return isolevel > SampleTerrain(point);
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

        //XML
        var serializer = new SharpSerializer();
        GameData loadedData = (GameData)serializer.Deserialize("Encoded.xml");

        //BINARY
        //var serializer = new SharpSerializer(true);
        //GameData loadedData = (GameData)serializer.Deserialize("GameData.bin");

        // or with the same usage as for the burst mode
        //var settings = new SharpSerializerBinarySettings(BinarySerializationMode.SizeOptimized);
        //var sizeOptimizedSerializer2 = new SharpSerializer(settings);

        //GameData loadedData = (GameData)sizeOptimizedSerializer2.Deserialize("GameDataOptimized.bin");

        loadedData.Load();

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


        
        GameData newData = new();

        //1st attempt
        var serializer = new SharpSerializer(); //this is not binary
        serializer.Serialize(newData, "Encoded.xml");

        //second
        //var serializer = new SharpSerializer(true); //this is binary
        //serializer.Serialize(newData, "GameData.bin");
        
        //third
        // or with the same usage as for the burst mode
        //var settings = new SharpSerializerBinarySettings(BinarySerializationMode.SizeOptimized);
        //var sizeOptimizedSerializer2 = new SharpSerializer(settings);

        //sizeOptimizedSerializer2.Serialize(newData, "GameDataOptimized.bin");

        Debug.Log("Saved succesfully!");
    }


    

}
