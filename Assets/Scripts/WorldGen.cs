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
using UnityEngine.Rendering;
using TMPro;
using UnityEngine.Rendering.Universal;
using NUnit.Framework.Constraints;

public class WorldGen : MonoBehaviour
{

    public static string mapName;
    public static float playTime;


    public static float isolevel = 127.5f;
    public static int x_dim = 200;
    public static int y_dim = 50;
    public static int z_dim = 200;
    public static int chunk_x_dim = 10;
    public static int chunk_y_dim = 50;
    public static int chunk_z_dim = 10;

    int num_chunks_x;
    int num_chunks_y;
    int num_chunks_z;

    public static bool hasWon = false; //if haswon is true, won't check for victory enymore
    public static bool updateCameraPos = false;
    public static bool updateNestVisibility = false;
    public static bool updateAntCounter = false;
    public static bool updateCornCounter = false;
    public static GameData.serializableVector3 camera_pos = new(Vector3.zero);
    public static GameData.serializableVector3 camera_euler = new(Vector3.zero);

    public static int[,,] terrainMap;
    public static int[,,] memoryMap;
    public static Dictionary<Vector3Int, chunk> chunks = new Dictionary<Vector3Int, chunk>();

    public Material terrainMaterial;

    public GameObject origAnt;
    public static GameObject originalAnt;

    public GameObject origQueen;
    public static GameObject originalQueen;

    public GameObject origNestPart;
    public static GameObject originalNestPart;

    public GameObject origCorn;
    public static GameObject originalCorn;

    public GameObject origCornCob;
    public static GameObject originalCornCob;

    public GameObject origDigPoint;
    public static GameObject originalDigPoint;

    public GameObject origPheromoneParticles;
    public static GameObject originalPheromoneParticles;

    public GameObject mapBox;

    // Start is called before the first frame update
    void Start()
    {

        //Turn all non static members into static ones:
        originalAnt = origAnt;
        originalQueen = origQueen;
        originalNestPart = origNestPart;
        originalCorn = origCorn;
        originalDigPoint = origDigPoint;
        originalCornCob = origCornCob;
        originalPheromoneParticles = origPheromoneParticles;

        CleanChunks();

        if (MainMenu.GameSettings.newMap)
        {
            if (MainMenu.GameSettings.flatMap)
                StartFlatMap();
            else if (!StartMap(MainMenu.GameSettings.fileName))
                StartFlatMap();
        }
        else
        {
            LoadSaveFile(MainMenu.GameSettings.SaveSlot, "default", 0);
        }
        Physics.gravity = new Vector3(0, -15.0F, 0);



        updateNestVisibility = true;

        updateAntCounter = true;
        updateCornCounter = true;
    }

    /*
     * 
     */
    public void GenerateChunks()
    {
        num_chunks_x = Mathf.FloorToInt(x_dim / chunk_x_dim);
        num_chunks_y = Mathf.FloorToInt(y_dim / chunk_y_dim);
        num_chunks_z = Mathf.FloorToInt(z_dim / chunk_z_dim);
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

        foreach ((var pos, var chunk) in chunks)
        {
            chunk.show();
        }

        Debug.Log(string.Format("{0} by {1} by {2} world generated", num_chunks_x, num_chunks_y, num_chunks_z));

    }


    /*
     * Crea el terreno. 
     */
    public void PopulateFlatMap(int height)
    {
        //Itera sobre todos los puntos del campo escalar
        for (int x = 0; x < x_dim + 1; x++)
        {
            for (int y = 0; y < y_dim + 1; y++)
            {
                for (int z = 0; z < z_dim + 1; z++)
                {
                    if (z == 0 || z == z_dim + 1 || x == 0 || x == x_dim + 1)
                        terrainMap[x, y, z] = 0;
                    else if (y == 0)
                        terrainMap[x, y, z] = 0;
                    else if (y == height)
                        terrainMap[x, y, z] = 200;
                    else if (y < height)
                        terrainMap[x, y, z] = 255;
                    else
                        terrainMap[x, y, z] = 0;

                    memoryMap[x, y, z] = terrainMap[x, y, z];
                }
            }
        }

        camera_pos = new(new Vector3(x_dim / 2, height + 5, z_dim / 2));
        updateCameraPos = true;

        Debug.Log(string.Format("Terrain generated"));

    }

    public void PopulateNoiseMap()
    {
        //Itera sobre todos los puntos del campo escalar
        for (int x = 0; x < x_dim + 1; x++)
        {
            for (int y = 0; y < y_dim + 1; y++)
            {
                for (int z = 0; z < z_dim + 1; z++)
                {

                    float perlinValue = Mathf.PerlinNoise(x / (float)x_dim, z / (float)z_dim);
                    float relativeY = (float)y / (float)y_dim;
                    float value = relativeY - perlinValue;



                    if (z == 0 || z == z_dim || x == 0 || x == x_dim)
                        terrainMap[x, y, z] = 0;
                    else //if (y == Mathf.FloorToInt(height) || y == Mathf.CeilToInt(height))
                        terrainMap[x, y, z] = Mathf.Clamp(255 - Mathf.FloorToInt(value * 255), 0, 255);
                    memoryMap[x, y, z] = terrainMap[x, y, z];
                }
            }
        }

        ApplyTerrainRedundancy();
        ApplyPastTerrainRedundancy();

        camera_pos = new(new Vector3(x_dim / 2, 35, z_dim / 2));
        updateCameraPos = true;

        Debug.Log(string.Format("Terrain generated"));

    }


    public static void EditTerrainAdd(List<Tuple<Vector3Int, int>> points, int degree)
    {
        var affectedChunks = new HashSet<Vector3Int>();

        for (var i = 0; i < points.Count; i++)
        {
            Vector3Int point = points[i].Item1;
            float val = points[i].Item2;
            if (InRange(point))
            {
                terrainMap[point.x, point.y, point.z] = Mathf.Clamp(terrainMap[point.x, point.y, point.z] + Mathf.RoundToInt(val * degree), 0, 255);
                Vector3Int mainChunk = new Vector3Int((point.x / chunk_x_dim) * chunk_x_dim, (point.y / chunk_y_dim) * chunk_y_dim, (point.z / chunk_z_dim) * chunk_z_dim);
                affectedChunks.Add(mainChunk);
                bool x_0 = false;
                //mirar si est� justo entre dos chunks en x y no al principio
                if (point.x % chunk_x_dim == 0 && point.x != 0)
                {
                    x_0 = true;
                    affectedChunks.Add(mainChunk + Vector3Int.left * chunk_x_dim);
                }
                bool z_0 = false;
                if (point.z % chunk_z_dim == 0 && point.z != 0)
                {
                    z_0 = true;
                    affectedChunks.Add(mainChunk + Vector3Int.back * chunk_z_dim);
                }
                if (x_0 && z_0)
                {
                    affectedChunks.Add(mainChunk + Vector3Int.left * chunk_x_dim + Vector3Int.back * chunk_z_dim);
                }
                bool y_0 = false;
                if (point.y % chunk_y_dim == 0 && point.y != 0)
                {
                    y_0 = true;
                    affectedChunks.Add(mainChunk + Vector3Int.down * chunk_y_dim);
                }
                if (x_0 && y_0)
                {
                    affectedChunks.Add(mainChunk + Vector3Int.left * chunk_x_dim + Vector3Int.down * chunk_y_dim);
                }
                if (y_0 && z_0)
                {
                    affectedChunks.Add(mainChunk + Vector3Int.down * chunk_y_dim + Vector3Int.back * chunk_z_dim);
                }
                if (x_0 && y_0 && z_0)
                {
                    affectedChunks.Add(mainChunk + Vector3Int.left * chunk_x_dim + Vector3Int.down * chunk_y_dim + Vector3Int.back * chunk_z_dim);
                }
            }
        }
        //check what chunks it affects
        foreach (Vector3Int point in affectedChunks)
        {
            if (chunks.ContainsKey(point))
            {
                //If in terrain edit mode, set memoryMap to copy of terrainMap
                if (MainMenu.GameSettings.gameMode == 0)
                    chunks[point].saveChunkTerrainToMemory();
                chunks[point].CreateMeshData();
            }
        }

    }
    public static void EditTerrainSet(List<Tuple<Vector3Int, int>> points)
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
            if (WorldGen.chunks.ContainsKey(point))
            {
                //If in terrain edit mode, set memoryMap to copy of terrainMap
                if (MainMenu.GameSettings.gameMode == 0)
                    chunks[point].saveChunkTerrainToMemory();
                chunks[point].CreateMeshData();
            }
        }

    }

    public static bool InRange(Vector3Int point)
    {
        if (point.x < 1 || point.x > x_dim || point.y < 1 || point.y > y_dim || point.z < 1 || point.z > z_dim) return false;
        return true;
    }
    public static bool InRange(Vector3 point)
    {
        if (point.x < 1 || point.x > x_dim || point.y < 1 || point.y > y_dim || point.z < 1 || point.z > z_dim) return false;
        return true;
    }
    public static int SampleTerrain(Vector3Int point)
    {
        if (!InRange(point))
            return 0;
        return terrainMap[point.x, point.y, point.z];
    }
    public static int SampleTerrain(int x, int y, int z)
    {
        if (!InRange(new Vector3Int(x, y, z))) return 0;
        return terrainMap[x, y, z];
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
    public static int SamplePastTerrain(Vector3Int point)
    {
        if (!InRange(point)) return 0;
        return memoryMap[point.x, point.y, point.z];
    }
    public static int SamplePastTerrain(int x, int y, int z)
    {
        if (!InRange(new Vector3Int(x, y, z))) return 0;
        return memoryMap[x, y, z];
    }
    public static int SamplePastTerrain(Vector3 point)
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
        float c00 = Mathf.Lerp(SamplePastTerrain(x0, y0, z0), SamplePastTerrain(x1, y0, z0), xd);
        float c01 = Mathf.Lerp(SamplePastTerrain(x0, y0, z1), SamplePastTerrain(x1, y0, z1), xd);
        float c10 = Mathf.Lerp(SamplePastTerrain(x0, y1, z0), SamplePastTerrain(x1, y1, z0), xd);
        float c11 = Mathf.Lerp(SamplePastTerrain(x0, y1, z1), SamplePastTerrain(x1, y1, z1), xd);

        // Interpolar por eje y
        float c0 = Mathf.Lerp(c00, c10, yd);
        float c1 = Mathf.Lerp(c01, c11, yd);

        // Interpolar por eje z
        return Mathf.RoundToInt(Mathf.Lerp(c0, c1, zd));
    }
    public static bool WasAboveSurface(Vector3Int point)
    {
        return isolevel > SamplePastTerrain(point);
    }
    public static bool WasAboveSurface(Vector3 point)
    {
        return isolevel > SamplePastTerrain(point);
    }

    public static Vector3 SurfaceDirection(Vector3Int point)
    {
        if (!IsAboveSurface(point)) return Vector3.up;
        Vector3 direction = new Vector3(0, 0, 0);
        if (!IsAboveSurface(new Vector3(point.x - 1, point.y, point.z))) direction.x -= 1;
        if (!IsAboveSurface(new Vector3(point.x + 1, point.y, point.z))) direction.x += 1;
        if (!IsAboveSurface(new Vector3(point.x, point.y - 1, point.z))) direction.y -= 1;
        if (!IsAboveSurface(new Vector3(point.x, point.y + 1, point.z))) direction.y += 1;
        if (!IsAboveSurface(new Vector3(point.x, point.y, point.z - 1))) direction.z -= 1;
        if (!IsAboveSurface(new Vector3(point.x, point.y, point.z + 1))) direction.z += 1;
        if (!(direction.x == 0 && direction.y == 0 && direction.z == 0)) return direction.normalized;
        if (!IsAboveSurface(new Vector3(point.x - 1, point.y - 1, point.z))) { direction.x -= 1; direction.y -= 1; }
        if (!IsAboveSurface(new Vector3(point.x - 1, point.y + 1, point.z))) { direction.x -= 1; direction.y += 1; }
        if (!IsAboveSurface(new Vector3(point.x - 1, point.y, point.z - 1))) { direction.x -= 1; direction.z -= 1; }
        if (!IsAboveSurface(new Vector3(point.x - 1, point.y, point.z + 1))) { direction.x -= 1; direction.z += 1; }
        if (!IsAboveSurface(new Vector3(point.x + 1, point.y - 1, point.z))) { direction.x += 1; direction.y -= 1; }
        if (!IsAboveSurface(new Vector3(point.x + 1, point.y + 1, point.z))) { direction.x += 1; direction.y += 1; }
        if (!IsAboveSurface(new Vector3(point.x + 1, point.y, point.z - 1))) { direction.x += 1; direction.z -= 1; }
        if (!IsAboveSurface(new Vector3(point.x + 1, point.y, point.z + 1))) { direction.x += 1; direction.z += 1; }
        if (!IsAboveSurface(new Vector3(point.x, point.y - 1, point.z - 1))) { direction.y -= 1; direction.z -= 1; }
        if (!IsAboveSurface(new Vector3(point.x, point.y - 1, point.z + 1))) { direction.y -= 1; direction.z += 1; }
        if (!IsAboveSurface(new Vector3(point.x, point.y + 1, point.z - 1))) { direction.y += 1; direction.z -= 1; }
        if (!IsAboveSurface(new Vector3(point.x, point.y + 1, point.z + 1))) { direction.y += 1; direction.z += 1; }
        return direction.normalized;
    }


    //Para guardar espacio, esta funcion se puede usar para obtener el valor por defecto 0 o 255 de un punto si no impacta el mapa.
    public static int GetRedundantTerrainSample(Vector3Int pos)
    {
        int val = SampleTerrain(pos);
        if (val == 0 || val == 255) return val;

        bool isAbove = IsAboveSurface(pos);
        Vector3Int[] directions = { Vector3Int.up, Vector3Int.down, Vector3Int.left, Vector3Int.right, Vector3Int.forward, Vector3Int.back };
        foreach (Vector3Int direction in directions)
        {
            if (isAbove != IsAboveSurface(pos + direction))
                return val;
        }
        if (isAbove) return 0;
        return 255;
    }

    public static int GetRedundantPastTerrainSample(Vector3Int pos)
    {
        int val = SamplePastTerrain(pos);
        if (val == 0 || val == 255) return val;

        bool isAbove = WasAboveSurface(pos);
        Vector3Int[] directions = { Vector3Int.up, Vector3Int.down, Vector3Int.left, Vector3Int.right, Vector3Int.forward, Vector3Int.back };
        foreach (Vector3Int direction in directions)
        {
            if (isAbove != WasAboveSurface(pos + direction))
                return val;
        }
        if (isAbove) return 0;
        return 255;
    }

    public static void ApplyTerrainRedundancy()
    {
        //Itera sobre todos los puntos del campo escalar
        for (int x = 0; x < x_dim + 1; x++)
            for (int y = 0; y < y_dim + 1; y++)
                for (int z = 0; z < z_dim + 1; z++)
                    terrainMap[x, y, z] = GetRedundantTerrainSample(new(x, y, z));
    }

    public void SetMapCube()
    {
        if (MainMenu.GameSettings.gameMode == 0)
        {
            mapBox.SetActive(true);
            Vector3 value = new((x_dim + 1f), y_dim + 1, z_dim + 1);
            mapBox.transform.position = value / 2f;
            mapBox.transform.localScale = value;
        }
        else mapBox.SetActive(false);
    }

    public static void ApplyPastTerrainRedundancy()
    {
        //Itera sobre todos los puntos del campo escalar
        for (int x = 0; x < x_dim + 1; x++)
            for (int y = 0; y < y_dim + 1; y++)
                for (int z = 0; z < z_dim + 1; z++)
                    memoryMap[x, y, z] = GetRedundantPastTerrainSample(new(x, y, z));
    }

    public void LoadSaveFile(int saveSlot, string mapName, float playTime)
    {
        Debug.Log("Starting loading");

        WorldGen.mapName = mapName;

        string saveFile = "SaveData/Save" + saveSlot + "/data.xml";

        //XML
        var serializer = new SharpSerializer();
        GameData loadedData = (GameData)serializer.Deserialize(saveFile);

        //BINARY
        //var serializer = new SharpSerializer(true);
        //GameData loadedData = (GameData)serializer.Deserialize("GameData.bin");

        // or with the same usage as for the burst mode
        //var settings = new SharpSerializerBinarySettings(BinarySerializationMode.SizeOptimized);
        //var sizeOptimizedSerializer2 = new SharpSerializer(settings);

        //GameData loadedData = (GameData)sizeOptimizedSerializer2.Deserialize("GameDataOptimized.bin");

        loadedData.LoadMap();

        //Crear nuevos chunks
        GenerateChunks();

        //Generar objetos del juego
        loadedData.LoadGameObjects();

        Debug.Log("Loaded succesfully!");
        GC.Collect();

        WorldGen.playTime = playTime;

        SetMapCube();

    }

    public void StartFlatMap()
    {
        x_dim = MainMenu.GameSettings.x_chunks * chunk_x_dim;
        y_dim = MainMenu.GameSettings.y_chunks * chunk_y_dim;
        z_dim = MainMenu.GameSettings.z_chunks * chunk_z_dim;

        terrainMap = new int[x_dim + 1, y_dim + 1, z_dim + 1];
        memoryMap = new int[x_dim + 1, y_dim + 1, z_dim + 1];
        PopulateFlatMap(MainMenu.GameSettings.height);
        GenerateChunks();
        Nest.NestParts = new();
        Nest.KnownCornCobs = new();
        CubePaths.cubePheromones = new();

        if (MainMenu.GameSettings.height > 0) mapName = "flat";
        else mapName = "empty";
        playTime = 0;

        SetMapCube();
    }

    public bool StartMap(string mapName)
    {
        WorldGen.mapName = mapName;
        Debug.Log(mapName);
        Debug.Log("Starting loading");
        string mapDir = "";

        if (File.Exists("SaveData/CustomMaps/" + mapName + ".xml"))
        {
            mapDir = "SaveData/CustomMaps/" + mapName + ".xml";
        }
        else if (File.Exists("SaveData/Maps/" + mapName + ".xml"))
        {
            mapDir = "SaveData/Maps/" + mapName + ".xml";
        }
        else return false;

        //XML
        var serializer = new SharpSerializer();
        GameData loadedData = (GameData)serializer.Deserialize(mapDir);

        //BINARY
        //var serializer = new SharpSerializer(true);
        //GameData loadedData = (GameData)serializer.Deserialize("GameData.bin");

        // or with the same usage as for the burst mode
        //var settings = new SharpSerializerBinarySettings(BinarySerializationMode.SizeOptimized);
        //var sizeOptimizedSerializer2 = new SharpSerializer(settings);

        //GameData loadedData = (GameData)sizeOptimizedSerializer2.Deserialize("GameDataOptimized.bin");

        loadedData.LoadMap();

        //Crear nuevos chunks
        GenerateChunks();

        //Generar objetos del juego
        loadedData.LoadGameObjects();

        Debug.Log("Loaded succesfully!");
        GC.Collect();

        WorldGen.playTime = 0;
        SetMapCube();
        return true;

    }


    public void SaveGame(int saveSlot)
    {
        Debug.Log("Starting save");

        ApplyTerrainRedundancy();
        ApplyPastTerrainRedundancy();

        GameData newData = GameData.Save();

        SaveManager.SaveInfo newInfo = SaveManager.SaveInfo.SaveGameInfo();

        saveSlot = Mathf.Clamp(saveSlot, 1, 3);

        //1st attempt
        var serializer = new SharpSerializer(); //this is not binary

        serializer.Serialize(newData, "SaveData/Save" + saveSlot + "/data.xml");
        serializer.Serialize(newInfo, "SaveData/Save" + saveSlot + "/info.xml");

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


    public bool SaveMap(string saveName)
    {

        if (saveName == "")
        {
            //message could not save
            return false;
        }

        // else if  check if same name as other map
        string[] saves = Directory.GetFiles("SaveData/Maps", "*.xml");
        foreach (string map in saves)
        {
            Debug.Log(map);
            string trimmed = map.Remove(map.Length - 4);
            trimmed = trimmed.Remove(0, 14);
            Debug.Log(trimmed);

            if (trimmed.Equals(saveName))
            {
                Debug.Log("That name already exists");
                return false;
            }
        }


        Debug.Log("Starting save");

        ApplyTerrainRedundancy();
        //memoryMap = terrainMap;

        GameData newData = GameData.Save();

        //1st attempt
        var serializer = new SharpSerializer(); //this is not binary
        serializer.Serialize(newData, "SaveData/CustomMaps/" + saveName + ".xml");

        //second
        //var serializer = new SharpSerializer(true); //this is binary
        //serializer.Serialize(newData, "GameData.bin");

        //third
        // or with the same usage as for the burst mode
        //var settings = new SharpSerializerBinarySettings(BinarySerializationMode.SizeOptimized);
        //var sizeOptimizedSerializer2 = new SharpSerializer(settings);

        //sizeOptimizedSerializer2.Serialize(newData, "GameDataOptimized.bin");

        Debug.Log("Saved succesfully!");

        return true;
    }


    public static NestPart InstantiateNestPart(Vector3 originPoint)
    {
        GameObject nestObj = Instantiate(originalNestPart, originPoint, Quaternion.identity);
        nestObj.SetActive(true);
        NestPart nestPartScript = nestObj.GetComponent<NestPart>();
        nestPartScript.setRadius(1);
        nestPartScript.setActive(true);
        nestPartScript.gotPoints = false;
        Nest.NestParts.Add(nestPartScript);
        return nestPartScript;
    }

    public static NestPart InstantiateNestPart(GameData.NestPartInfo info)
    {
        GameObject nestObj = Instantiate(originalNestPart, info.startPos.ToVector3(), Quaternion.identity);
        nestObj.SetActive(true);
        NestPart nestPartScript = nestObj.GetComponent<NestPart>();
        nestPartScript.setMode(NestPart.IndexToNestPartType(info.mode));
        nestPartScript.SetPos(info.startPos.ToVector3(), info.endPos.ToVector3());
        nestPartScript.setRadius(info.radius);
        nestPartScript.setActive(true);
        nestPartScript.CollectedCornPips = info.cornPips;
        nestPartScript.AntEggs = info.antEggs;

        foreach (var serPos in info.digPointsLeft)
            nestPartScript.digPointsLeft.Add(serPos.ToVector3Int());

        //Se asume que el objeto ya ha sido colocado.
        nestPartScript.gotPoints = true;
        //nestPartScript.SetVisible(false);


        Nest.NestParts.Add(nestPartScript);
        return nestPartScript;
    }

    public static Ant InstantiateAnt(Vector3 pos, Quaternion orientation, bool born)
    {
        GameObject newAnt = Instantiate(originalAnt, pos, orientation);
        newAnt.layer = 7;
        newAnt.SetActive(true);
        Ant newAntScript = newAnt.GetComponent<Ant>();
        Ant.registerAnt(newAntScript);
        newAntScript.born = born;
        if (!born)
        {
            newAntScript.age = 0;
            newAntScript.staticEgg.SetActive(true);
        }
        else
            newAntScript.age = 100;

        newAnt.name = "Ant " + newAntScript.id;

        return newAntScript;
    }

    public static Ant InstantiateAnt(GameData.AntInfo antInfo)
    {
        Vector3 pos = antInfo.pos.ToVector3();
        Quaternion orientation = antInfo.orientation.ToQuaternion();

        GameObject newAnt = Instantiate(originalAnt, pos, orientation);
        newAnt.layer = 7;
        newAnt.SetActive(true);
        Ant newAntScript = newAnt.GetComponent<Ant>();

        newAntScript.id = antInfo.id;
        newAntScript.antId = antInfo.antId;
        newAntScript.age = antInfo.age;
        if (antInfo.age < 100)
        {
            newAntScript.staticEgg.SetActive(true);
            newAntScript.antCapCollider.enabled = false;
            newAntScript.born = false;
        }
        else newAntScript.born = true;

        newAntScript.objective = new Task(antInfo.objective);
        newAntScript.IsControlled = antInfo.isControlled;
        newAntScript.Counter = antInfo.Counter;
        newAntScript.discoveredCobs = antInfo.discoveredCobs;

        //Informamos tambien si la hormiga ha sido nacida o no.
        if (antInfo.age > 100) newAntScript.born = true;
        else newAntScript.born = false;

        newAnt.name = "Ant " + antInfo.id;

        Ant.antDictionary.Add(antInfo.id, newAntScript);

        return newAntScript;
    }

    public static AntQueen InstantiateQueen(Vector3 pos, Quaternion orientation)
    {
        GameObject newQueen = Instantiate(originalQueen, pos, orientation);
        newQueen.layer = 7;
        newQueen.SetActive(true);
        AntQueen newQueenScript = newQueen.GetComponent<AntQueen>();

        newQueen.name = "Ant Queen";
        AntQueen.Queen = newQueenScript;

        return newQueenScript;
    }

    public static void InstantiateQueen(GameData.QueenInfo queenInfo)
    {
        if (AntQueen.Queen != null) return;
        Vector3 pos = queenInfo.pos.ToVector3();
        Quaternion orientation = queenInfo.orientation.ToQuaternion();

        GameObject newQueen = Instantiate(originalQueen, pos, orientation);
        newQueen.layer = 7;
        newQueen.SetActive(true);
        AntQueen newQueenScript = newQueen.GetComponent<AntQueen>();

        newQueenScript.objective = new Task(queenInfo.objective);
        newQueenScript.Counter = queenInfo.Counter;
        newQueenScript.Energy = queenInfo.Energy;

        newQueen.name = "Ant Queen";
        AntQueen.Queen = newQueenScript;
    }

    public static Corn InstantiateCorn(Vector3 pos, Quaternion orientation)
    {
        GameObject newCorn = Instantiate(originalCorn, pos, orientation);
        newCorn.SetActive(true);
        newCorn.layer = 10; //Food layer0
        Corn newCornScript = newCorn.GetComponent<Corn>();
        Corn.registerCorn(newCornScript);
        newCorn.name = "Corn " + newCornScript.id;
        return newCornScript;
    }

    public static Corn InstantiateCorn(GameData.CornInfo cornInfo)
    {
        Vector3 pos = cornInfo.pos.ToVector3();
        Quaternion orientation = cornInfo.orientation.ToQuaternion();

        GameObject newCorn = Instantiate(originalCorn, pos, orientation);
        newCorn.SetActive(true);
        newCorn.layer = 10; //Food layer0
        Corn newCornScript = newCorn.GetComponent<Corn>();

        newCornScript.id = cornInfo.id;
        newCornScript.antId = cornInfo.antId;
        newCorn.name = "Corn " + cornInfo.id;

        Corn.cornDictionary.Add(cornInfo.id, newCornScript);
        return newCornScript;
    }

    public static DigPoint InstantiateDigPoint(Vector3Int pos)
    {
        GameObject newDigPoint = Instantiate(originalDigPoint, pos, Quaternion.identity);

        newDigPoint.layer = 9;
        newDigPoint.SetActive(true);
        newDigPoint.name = "DigPoint (" + pos.x + ", " + pos.y + ", " + pos.z + ")";

        DigPoint newDigPointScript = newDigPoint.GetComponent<DigPoint>();
        return newDigPointScript;
    }

    public static CornCob InstantiateCornCob(Vector3 pos, Quaternion orientation, int numPips)
    {
        GameObject cornCobObj = Instantiate(originalCornCob, pos, orientation);
        cornCobObj.SetActive(true);
        CornCob cornCob = cornCobObj.GetComponent<CornCob>();
        CornCob.registerCorn(cornCob);
        numPips = Math.Clamp(numPips, 0, CornCob.numCornSpots);
        List<int> cornSpots = Enumerable.Range(0, CornCob.numCornSpots).ToList();
        Nest.Shuffle(cornSpots);
        for (int i = 0; i < numPips; i++)
        {
            Corn newCorn = InstantiateCorn(cornCobObj.transform.position, Quaternion.identity);
            cornCob.cornCobCornDict.Add(cornSpots[i], newCorn.id);
        }
        return cornCob;
    }

    public static CornCob InstantiateCornCob(GameData.CornCobInfo cornCobInfo)
    {
        GameObject cornCobObj = Instantiate(originalCornCob);
        cornCobObj.SetActive(true);
        cornCobObj.transform.position = cornCobInfo.pos.ToVector3();
        cornCobObj.transform.rotation = cornCobInfo.orientation.ToQuaternion();
        CornCob cornCob = cornCobObj.GetComponent<CornCob>();
        if (!CornCob.registerCorn(cornCob, cornCobInfo.id)) Debug.Log("NON VALID ID CORNCOB");
        cornCob.cornCobCornDict = cornCobInfo.cornCobCornDict;
        return cornCob;
    }


    public static ParticleSystem InstantiatePheromoneParticles(Vector3 pos)
    {
        GameObject pherParticlesObj = Instantiate(originalPheromoneParticles, pos, Quaternion.identity);
        pherParticlesObj.SetActive(true);
        return pherParticlesObj.GetComponent<ParticleSystem>();
    }


    public static void CleanChunks()
    {
        //eliminar chunks existentes
        foreach (var item in chunks.Values)
        {
            item.Destroy();
        }
        chunks.Clear();
    }


    public GameObject victoryPanel; 

    private int counter = 0;

    public void FixedUpdate()
    {
        playTime += Time.fixedDeltaTime;

        if (!hasWon)
        {
            counter++;
            if (counter > 100)
            {
                counter = 0;
                if (Won())
                {
                    hasWon = true;
                    victoryPanel.SetActive(true);
                }
            }
        }
    }

    //if no more corn on corncobs returns true.
    private bool Won()
    {
        foreach ((var id, var cob) in CornCob.cornCobDictionary)
        {
            if (cob != null)
            {
                if (cob.hasCorn())
                    return false;
            }
        }

        return true;
    }

}
