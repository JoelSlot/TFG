using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.TerrainUtils;

public class chunk
{

    List<Vector3> vertices = new List<Vector3>();
    List<int> triangles = new List<int>();
    List<Color> colors = new();

    //Keep forgetting to use the values divided by 255.
    Color grassGreen = new Color(0, 100f/255f, 0);
    Color brown = new Color(165f/255f, 42f/255f, 42f/255f);

    WorldGen worldGen;

    public GameObject chunkObject;
    MeshFilter meshFilter;
    MeshCollider meshCollider;
    MeshRenderer meshRenderer;

    Vector3Int chunkPosition;


    int x_dim { get { return WorldGen.chunk_x_dim; } }
    int y_dim { get { return WorldGen.chunk_y_dim; } }
    int z_dim { get { return WorldGen.chunk_z_dim; } }
    float isolevel { get { return WorldGen.isolevel; } }

    //index of configuration to use from the TriangleTable list
    int cubeindex;

    // Start is called before the first frame update
    public chunk(Vector3Int _position, WorldGen dad)
    {
        chunkObject = new GameObject();
        chunkPosition = _position;
        chunkObject.name = string.Format("Chunk {0} {1}", _position.x, _position.z);
        //chunkObject.transform.position = chunkPosition;
        worldGen = dad;

        meshFilter = chunkObject.AddComponent<MeshFilter>();
        meshCollider = chunkObject.AddComponent<MeshCollider>();
        meshRenderer = chunkObject.AddComponent<MeshRenderer>();
        meshRenderer.enabled = false;
        //set mesh material
        meshRenderer.material = dad.terrainMaterial;
        //not sure if relevant
        chunkObject.transform.tag = "Terrain";

        CreateMeshData();

        BuildMesh();

    }

    // Update is called once per frame
    void Update()
    {   

    }

    public void show()
    {
        meshRenderer.enabled = true;
    }

    public void hide()
    {
        meshRenderer.enabled = false;
    }

    

    //Funci�n que ejecuta el proceso de marching cubes para un cubo dado su posici�n
    void MarchCube(Vector3Int position)
    {

        //Conseguir valores de los v�rtices del cubo
        float[] cubeVertices = new float[8];
        for (int i = 0; i < 8; i++)
        {
            cubeVertices[i] = WorldGen.SampleTerrain(position + cornerIdToPos[i]);
        }

        //Obtener el �ndice que se usa en las tablas de ejes y tri�ngulos
        int configIndex = 0;
        for (int i = 0; i < 8; i++)
        {
            if (cubeVertices[i] < isolevel)
                configIndex |= 1 << i;
        }

        //Si la malla no pasa por el cubo salimos de la funci�n
        if (configIndex == 0 || configIndex == 255) return;

        //Debug.DrawLine(position - new Vector3(-0.1f, 0.9f, 0.1f), position + new Vector3(0.1f, 1.1f, -0.1f), Color.green, 1000000);
        //Debug.DrawLine(position + new Vector3(0.1f, 0.9f, -0.1f), position + new Vector3(-0.1f, 1.1f, 0.1f), Color.green, 1000000);

        int arrayIndex = 0;
        //Iteramos para cada uno de los 5 posibles tri�ngulos
        for (int i = 0; i < 5; i++)
        {
            
            //Iteramos sobre cada v�rtice del tri�ngulo
            for (int p = 0; p < 3; p++)
            {
                
                //Obtener el siguiente �ndice del array en la tabla de tri�ngulos
                int indice = triangleTable[configIndex, arrayIndex];

                //Si no quedan tri�ngulos en el array dejamos de iterar
                if (indice == -1) return;

                //Obtener los dos v�rtices que forman el eje y sus valores
                Vector3 vert1pos = position + cornerIdToPos[edgeIdToCornerId[indice, 0]];
                Vector3 vert2pos = position + cornerIdToPos[edgeIdToCornerId[indice, 1]];
                float vert1val = WorldGen.SampleTerrain(vert1pos);
                float vert2val = WorldGen.SampleTerrain(vert2pos);

                //Calcular punto del eje que se corta usando interpolaci�n linear
                Vector3 vertPosition = vert1pos + (isolevel - vert1val) * (vert2pos - vert1pos) / (vert2val - vert1val);
                
                //Guardar los v�rtices que forman el tri�ngulo en la malla.
                vertices.Add(vertPosition);
                triangles.Add(vertices.Count - 1);

                arrayIndex++;

            }
            Vector3 v1 = vertices[vertices.Count-3];
            Vector3 v2 = vertices[vertices.Count-2];
            Vector3 v3 = vertices[vertices.Count-1];
            if (Vector3.Angle(Vector3.up, Vector3.Cross(v2-v1, v3-v1)) > 80)
            {
                colors.Add(brown);
                colors.Add(brown);
                colors.Add(brown);
            }
            else
            {
                if (WorldGen.SamplePastTerrain(v1) != WorldGen.SampleTerrain(v1))
                    colors.Add(brown);
                else colors.Add(grassGreen);
                if (WorldGen.SamplePastTerrain(v2) != WorldGen.SampleTerrain(v2))
                    colors.Add(brown);
                else colors.Add(grassGreen);
                if (WorldGen.SamplePastTerrain(v3) != WorldGen.SampleTerrain(v3))
                    colors.Add(brown);
                else colors.Add(grassGreen);
            }

        }

    }

    //Copia todos los valores del terreno del chunk a la memoryMap.
    public void saveChunkTerrainToMemory()
    {
        for (int x = 0; x < x_dim + 1; x++)
            for (int y = 0; y < y_dim + 1; y++)
                for (int z = 0; z < z_dim + 1; z++)
                    WorldGen.memoryMap[ x + chunkPosition.x, 
                                        y + chunkPosition.y,
                                        z + chunkPosition.z] = 
                    WorldGen.terrainMap[x + chunkPosition.x, 
                                        y + chunkPosition.y,
                                        z + chunkPosition.z];
    }


   
    void ClearMeshData()
    {

        vertices.Clear();
        triangles.Clear();
        colors.Clear();

    }

    public void CreateMeshData()
    {
        ClearMeshData();

        //Los limites se incrementan en 1 si en dicha direccion ya no hay mas chunks, y por tanto hay que rellenar su final.
        int x_limit = x_dim;
        if (!WorldGen.chunks.ContainsKey(chunkPosition + Vector3Int.right * x_dim)) x_limit++;
        int y_limit = y_dim;
        if (!WorldGen.chunks.ContainsKey(chunkPosition + Vector3Int.up * y_dim)) y_limit++;
        int z_limit = z_dim;
        if (!WorldGen.chunks.ContainsKey(chunkPosition + Vector3Int.forward * z_dim)) z_limit++;

        for (int x = 0; x < x_limit; x++)
            for (int z = 0; z < z_limit; z++)
                for (int y = 0; y < y_limit; y++)
                    MarchCube(new Vector3Int(x + chunkPosition.x, y + chunkPosition.y, z + chunkPosition.z));

        BuildMesh();
    }


    void BuildMesh()
    {

        Mesh mesh = new Mesh();
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.colors = colors.ToArray();
        mesh.RecalculateNormals();
        meshFilter.mesh = mesh;
        meshCollider.sharedMesh = mesh;
    }

    //Destruye todos los gameobjects incluidos en el chunk.
    public void Destroy()
    {
        if (chunkObject == null) return;
        UnityEngine.Object.DestroyImmediate(chunkObject.GetComponent<MeshFilter>());
        UnityEngine.Object.DestroyImmediate(chunkObject.GetComponent<MeshCollider>());
        UnityEngine.Object.DestroyImmediate(chunkObject.GetComponent<MeshRenderer>());
        UnityEngine.Object.DestroyImmediate(chunkObject);
    }

    /*
    public WorldGen WorldGena
    {
        get => default;
        set
        {
        }
    }
    */

    //tabla de esquinas: las 8 esquinas del cubo
    public static Vector3Int[] cornerIdToPos = new Vector3Int[8]
    {
        new Vector3Int(0, 0, 0),
        new Vector3Int(1, 0, 0),
        new Vector3Int(1, 1, 0),
        new Vector3Int(0, 1, 0),
        new Vector3Int(0, 0, 1),
        new Vector3Int(1, 0, 1),
        new Vector3Int(1, 1, 1),
        new Vector3Int(0, 1, 1)
    };

    //Tabla reversa de esquinas. Dada la posicion de la esquina, devuelve su indice
    public static Dictionary<Vector3Int, int> cornerPosToId = new Dictionary<Vector3Int, int>
    {
        {new Vector3Int(0, 0, 0), 0},
        {new Vector3Int(1, 0, 0), 1},
        {new Vector3Int(1, 1, 0), 2},
        {new Vector3Int(0, 1, 0), 3},
        {new Vector3Int(0, 0, 1), 4},
        {new Vector3Int(1, 0, 1), 5},
        {new Vector3Int(1, 1, 1), 6},
        {new Vector3Int(0, 1, 1), 7}
    };

    //tabla de ejes. Para cada eje los �ndices de los v�rtices que lo forman.
    public static int[,] edgeIdToCornerId = new int[12, 2] {

        {0, 1}, {1, 2}, {3, 2}, {0, 3}, {4, 5}, {5, 6}, {7, 6}, {4, 7}, {0, 4}, {1, 5}, {2, 6}, {3, 7}

    };

    //tabla de caras. Dada indice de cara y pos devuelve esquina de esa cara numero pos
    public static int[,] faceIdToCornerId = new int[6, 4]
    {
        {0, 1, 3, 2}, // back face
        {4, 5, 7, 6}, // forward face
        {1, 2, 5, 6}, // right face
        {0, 3, 4, 7}, // left face
        {2, 3, 6, 7}, // up face
        {0, 1, 4, 5}  // down face
    };

    //tabla de caras. Dado indice de cara devuelve su dir respecto centro del cubo.
    public static Vector3Int[] faceIdToDirTable = new Vector3Int[6]
    {
        Vector3Int.back, // back face
        Vector3Int.forward, // forward face
        Vector3Int.right, // right face
        Vector3Int.left, // left face
        Vector3Int.up, // up face
        Vector3Int.down  // down face
    };

    //tabla reverso de caras. Dada la direccion de una cara, devuelve su índice
    public static Dictionary<Vector3Int, int> dirToFaceIdTable = new Dictionary<Vector3Int, int>()
    {
        {Vector3Int.back, 0},// back face
        {Vector3Int.forward, 1},// forward face
        {Vector3Int.right, 2},// right face
        {Vector3Int.left, 3}, // left face
        {Vector3Int.up, 4},// up face
        {Vector3Int.down, 5}  // down face
    };

    //Configuraciones: las distintas combinaciones de tri�ngulos.
    public static int[,] triangleTable = new int[,]
    {

        {-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1},
        { 0, 8, 3,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1},
        { 0, 1, 9,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1},
        { 1, 8, 3, 9, 8, 1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1},
        { 1, 2,10,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1},
        { 0, 8, 3, 1, 2,10,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1},
        { 9, 2,10, 0, 2, 9,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1},
        { 2, 8, 3, 2,10, 8,10, 9, 8,-1,-1,-1,-1,-1,-1,-1},
        { 3,11, 2,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1},
        { 0,11, 2, 8,11, 0,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1},
        { 1, 9, 0, 2, 3,11,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1},
        { 1,11, 2, 1, 9,11, 9, 8,11,-1,-1,-1,-1,-1,-1,-1},
        { 3,10, 1,11,10, 3,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1},
        { 0,10, 1, 0, 8,10, 8,11,10,-1,-1,-1,-1,-1,-1,-1},
        { 3, 9, 0, 3,11, 9,11,10, 9,-1,-1,-1,-1,-1,-1,-1},
        { 9, 8,10,10, 8,11,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1},
        { 4, 7, 8,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1},
        { 4, 3, 0, 7, 3, 4,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1},
        { 0, 1, 9, 8, 4, 7,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1},
        { 4, 1, 9, 4, 7, 1, 7, 3, 1,-1,-1,-1,-1,-1,-1,-1},
        { 1, 2,10, 8, 4, 7,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1},
        { 3, 4, 7, 3, 0, 4, 1, 2,10,-1,-1,-1,-1,-1,-1,-1},
        { 9, 2,10, 9, 0, 2, 8, 4, 7,-1,-1,-1,-1,-1,-1,-1},
        { 2,10, 9, 2, 9, 7, 2, 7, 3, 7, 9, 4,-1,-1,-1,-1},
        { 8, 4, 7, 3,11, 2,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1},
        {11, 4, 7,11, 2, 4, 2, 0, 4,-1,-1,-1,-1,-1,-1,-1},
        { 9, 0, 1, 8, 4, 7, 2, 3,11,-1,-1,-1,-1,-1,-1,-1},
        { 4, 7,11, 9, 4,11, 9,11, 2, 9, 2, 1,-1,-1,-1,-1},
        { 3,10, 1, 3,11,10, 7, 8, 4,-1,-1,-1,-1,-1,-1,-1},
        { 1,11,10, 1, 4,11, 1, 0, 4, 7,11, 4,-1,-1,-1,-1},
        { 4, 7, 8, 9, 0,11, 9,11,10,11, 0, 3,-1,-1,-1,-1},
        { 4, 7,11, 4,11, 9, 9,11,10,-1,-1,-1,-1,-1,-1,-1},
        { 9, 5, 4,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1},
        { 9, 5, 4, 0, 8, 3,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1},
        { 0, 5, 4, 1, 5, 0,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1},
        { 8, 5, 4, 8, 3, 5, 3, 1, 5,-1,-1,-1,-1,-1,-1,-1},
        { 1, 2,10, 9, 5, 4,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1},
        { 3, 0, 8, 1, 2,10, 4, 9, 5,-1,-1,-1,-1,-1,-1,-1},
        { 5, 2,10, 5, 4, 2, 4, 0, 2,-1,-1,-1,-1,-1,-1,-1},
        { 2,10, 5, 3, 2, 5, 3, 5, 4, 3, 4, 8,-1,-1,-1,-1},
        { 9, 5, 4, 2, 3,11,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1},
        { 0,11, 2, 0, 8,11, 4, 9, 5,-1,-1,-1,-1,-1,-1,-1},
        { 0, 5, 4, 0, 1, 5, 2, 3,11,-1,-1,-1,-1,-1,-1,-1},
        { 2, 1, 5, 2, 5, 8, 2, 8,11, 4, 8, 5,-1,-1,-1,-1},
        {10, 3,11,10, 1, 3, 9, 5, 4,-1,-1,-1,-1,-1,-1,-1},
        { 4, 9, 5, 0, 8, 1, 8,10, 1, 8,11,10,-1,-1,-1,-1},
        { 5, 4, 0, 5, 0,11, 5,11,10,11, 0, 3,-1,-1,-1,-1},
        { 5, 4, 8, 5, 8,10,10, 8,11,-1,-1,-1,-1,-1,-1,-1},
        { 9, 7, 8, 5, 7, 9,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1},
        { 9, 3, 0, 9, 5, 3, 5, 7, 3,-1,-1,-1,-1,-1,-1,-1},
        { 0, 7, 8, 0, 1, 7, 1, 5, 7,-1,-1,-1,-1,-1,-1,-1},
        { 1, 5, 3, 3, 5, 7,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1},
        { 9, 7, 8, 9, 5, 7,10, 1, 2,-1,-1,-1,-1,-1,-1,-1},
        {10, 1, 2, 9, 5, 0, 5, 3, 0, 5, 7, 3,-1,-1,-1,-1},
        { 8, 0, 2, 8, 2, 5, 8, 5, 7,10, 5, 2,-1,-1,-1,-1},
        { 2,10, 5, 2, 5, 3, 3, 5, 7,-1,-1,-1,-1,-1,-1,-1},
        { 7, 9, 5, 7, 8, 9, 3,11, 2,-1,-1,-1,-1,-1,-1,-1},
        { 9, 5, 7, 9, 7, 2, 9, 2, 0, 2, 7,11,-1,-1,-1,-1},
        { 2, 3,11, 0, 1, 8, 1, 7, 8, 1, 5, 7,-1,-1,-1,-1},
        {11, 2, 1,11, 1, 7, 7, 1, 5,-1,-1,-1,-1,-1,-1,-1},
        { 9, 5, 8, 8, 5, 7,10, 1, 3,10, 3,11,-1,-1,-1,-1},
        { 5, 7, 0, 5, 0, 9, 7,11, 0, 1, 0,10,11,10, 0,-1},
        {11,10, 0,11, 0, 3,10, 5, 0, 8, 0, 7, 5, 7, 0,-1},
        {11,10, 5, 7,11, 5,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1},
        {10, 6, 5,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1},
        { 0, 8, 3, 5,10, 6,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1},
        { 9, 0, 1, 5,10, 6,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1},
        { 1, 8, 3, 1, 9, 8, 5,10, 6,-1,-1,-1,-1,-1,-1,-1},
        { 1, 6, 5, 2, 6, 1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1},
        { 1, 6, 5, 1, 2, 6, 3, 0, 8,-1,-1,-1,-1,-1,-1,-1},
        { 9, 6, 5, 9, 0, 6, 0, 2, 6,-1,-1,-1,-1,-1,-1,-1},
        { 5, 9, 8, 5, 8, 2, 5, 2, 6, 3, 2, 8,-1,-1,-1,-1},
        { 2, 3,11,10, 6, 5,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1},
        {11, 0, 8,11, 2, 0,10, 6, 5,-1,-1,-1,-1,-1,-1,-1},
        { 0, 1, 9, 2, 3,11, 5,10, 6,-1,-1,-1,-1,-1,-1,-1},
        { 5,10, 6, 1, 9, 2, 9,11, 2, 9, 8,11,-1,-1,-1,-1},
        { 6, 3,11, 6, 5, 3, 5, 1, 3,-1,-1,-1,-1,-1,-1,-1},
        { 0, 8,11, 0,11, 5, 0, 5, 1, 5,11, 6,-1,-1,-1,-1},
        { 3,11, 6, 0, 3, 6, 0, 6, 5, 0, 5, 9,-1,-1,-1,-1},
        { 6, 5, 9, 6, 9,11,11, 9, 8,-1,-1,-1,-1,-1,-1,-1},
        { 5,10, 6, 4, 7, 8,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1},
        { 4, 3, 0, 4, 7, 3, 6, 5,10,-1,-1,-1,-1,-1,-1,-1},
        { 1, 9, 0, 5,10, 6, 8, 4, 7,-1,-1,-1,-1,-1,-1,-1},
        {10, 6, 5, 1, 9, 7, 1, 7, 3, 7, 9, 4,-1,-1,-1,-1},
        { 6, 1, 2, 6, 5, 1, 4, 7, 8,-1,-1,-1,-1,-1,-1,-1},
        { 1, 2, 5, 5, 2, 6, 3, 0, 4, 3, 4, 7,-1,-1,-1,-1},
        { 8, 4, 7, 9, 0, 5, 0, 6, 5, 0, 2, 6,-1,-1,-1,-1},
        { 7, 3, 9, 7, 9, 4, 3, 2, 9, 5, 9, 6, 2, 6, 9,-1},
        { 3,11, 2, 7, 8, 4,10, 6, 5,-1,-1,-1,-1,-1,-1,-1},
        { 5,10, 6, 4, 7, 2, 4, 2, 0, 2, 7,11,-1,-1,-1,-1},
        { 0, 1, 9, 4, 7, 8, 2, 3,11, 5,10, 6,-1,-1,-1,-1},
        { 9, 2, 1, 9,11, 2, 9, 4,11, 7,11, 4, 5,10, 6,-1},
        { 8, 4, 7, 3,11, 5, 3, 5, 1, 5,11, 6,-1,-1,-1,-1},
        { 5, 1,11, 5,11, 6, 1, 0,11, 7,11, 4, 0, 4,11,-1},
        { 0, 5, 9, 0, 6, 5, 0, 3, 6,11, 6, 3, 8, 4, 7,-1},
        { 6, 5, 9, 6, 9,11, 4, 7, 9, 7,11, 9,-1,-1,-1,-1},
        {10, 4, 9, 6, 4,10,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1},
        { 4,10, 6, 4, 9,10, 0, 8, 3,-1,-1,-1,-1,-1,-1,-1},
        {10, 0, 1,10, 6, 0, 6, 4, 0,-1,-1,-1,-1,-1,-1,-1},
        { 8, 3, 1, 8, 1, 6, 8, 6, 4, 6, 1,10,-1,-1,-1,-1},
        { 1, 4, 9, 1, 2, 4, 2, 6, 4,-1,-1,-1,-1,-1,-1,-1},
        { 3, 0, 8, 1, 2, 9, 2, 4, 9, 2, 6, 4,-1,-1,-1,-1},
        { 0, 2, 4, 4, 2, 6,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1},
        { 8, 3, 2, 8, 2, 4, 4, 2, 6,-1,-1,-1,-1,-1,-1,-1},
        {10, 4, 9,10, 6, 4,11, 2, 3,-1,-1,-1,-1,-1,-1,-1},
        { 0, 8, 2, 2, 8,11, 4, 9,10, 4,10, 6,-1,-1,-1,-1},
        { 3,11, 2, 0, 1, 6, 0, 6, 4, 6, 1,10,-1,-1,-1,-1},
        { 6, 4, 1, 6, 1,10, 4, 8, 1, 2, 1,11, 8,11, 1,-1},
        { 9, 6, 4, 9, 3, 6, 9, 1, 3,11, 6, 3,-1,-1,-1,-1},
        { 8,11, 1, 8, 1, 0,11, 6, 1, 9, 1, 4, 6, 4, 1,-1},
        { 3,11, 6, 3, 6, 0, 0, 6, 4,-1,-1,-1,-1,-1,-1,-1},
        { 6, 4, 8,11, 6, 8,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1},
        { 7,10, 6, 7, 8,10, 8, 9,10,-1,-1,-1,-1,-1,-1,-1},
        { 0, 7, 3, 0,10, 7, 0, 9,10, 6, 7,10,-1,-1,-1,-1},
        {10, 6, 7, 1,10, 7, 1, 7, 8, 1, 8, 0,-1,-1,-1,-1},
        {10, 6, 7,10, 7, 1, 1, 7, 3,-1,-1,-1,-1,-1,-1,-1},
        { 1, 2, 6, 1, 6, 8, 1, 8, 9, 8, 6, 7,-1,-1,-1,-1},
        { 2, 6, 9, 2, 9, 1, 6, 7, 9, 0, 9, 3, 7, 3, 9,-1},
        { 7, 8, 0, 7, 0, 6, 6, 0, 2,-1,-1,-1,-1,-1,-1,-1},
        { 7, 3, 2, 6, 7, 2,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1},
        { 2, 3,11,10, 6, 8,10, 8, 9, 8, 6, 7,-1,-1,-1,-1},
        { 2, 0, 7, 2, 7,11, 0, 9, 7, 6, 7,10, 9,10, 7,-1},
        { 1, 8, 0, 1, 7, 8, 1,10, 7, 6, 7,10, 2, 3,11,-1},
        {11, 2, 1,11, 1, 7,10, 6, 1, 6, 7, 1,-1,-1,-1,-1},
        { 8, 9, 6, 8, 6, 7, 9, 1, 6,11, 6, 3, 1, 3, 6,-1},
        { 0, 9, 1,11, 6, 7,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1},
        { 7, 8, 0, 7, 0, 6, 3,11, 0,11, 6, 0,-1,-1,-1,-1},
        { 7,11, 6,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1},
        { 7, 6,11,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1},
        { 3, 0, 8,11, 7, 6,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1},
        { 0, 1, 9,11, 7, 6,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1},
        { 8, 1, 9, 8, 3, 1,11, 7, 6,-1,-1,-1,-1,-1,-1,-1},
        {10, 1, 2, 6,11, 7,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1},
        { 1, 2,10, 3, 0, 8, 6,11, 7,-1,-1,-1,-1,-1,-1,-1},
        { 2, 9, 0, 2,10, 9, 6,11, 7,-1,-1,-1,-1,-1,-1,-1},
        { 6,11, 7, 2,10, 3,10, 8, 3,10, 9, 8,-1,-1,-1,-1},
        { 7, 2, 3, 6, 2, 7,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1},
        { 7, 0, 8, 7, 6, 0, 6, 2, 0,-1,-1,-1,-1,-1,-1,-1},
        { 2, 7, 6, 2, 3, 7, 0, 1, 9,-1,-1,-1,-1,-1,-1,-1},
        { 1, 6, 2, 1, 8, 6, 1, 9, 8, 8, 7, 6,-1,-1,-1,-1},
        {10, 7, 6,10, 1, 7, 1, 3, 7,-1,-1,-1,-1,-1,-1,-1},
        {10, 7, 6, 1, 7,10, 1, 8, 7, 1, 0, 8,-1,-1,-1,-1},
        { 0, 3, 7, 0, 7,10, 0,10, 9, 6,10, 7,-1,-1,-1,-1},
        { 7, 6,10, 7,10, 8, 8,10, 9,-1,-1,-1,-1,-1,-1,-1},
        { 6, 8, 4,11, 8, 6,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1},
        { 3, 6,11, 3, 0, 6, 0, 4, 6,-1,-1,-1,-1,-1,-1,-1},
        { 8, 6,11, 8, 4, 6, 9, 0, 1,-1,-1,-1,-1,-1,-1,-1},
        { 9, 4, 6, 9, 6, 3, 9, 3, 1,11, 3, 6,-1,-1,-1,-1},
        { 6, 8, 4, 6,11, 8, 2,10, 1,-1,-1,-1,-1,-1,-1,-1},
        { 1, 2,10, 3, 0,11, 0, 6,11, 0, 4, 6,-1,-1,-1,-1},
        { 4,11, 8, 4, 6,11, 0, 2, 9, 2,10, 9,-1,-1,-1,-1},
        {10, 9, 3,10, 3, 2, 9, 4, 3,11, 3, 6, 4, 6, 3,-1},
        { 8, 2, 3, 8, 4, 2, 4, 6, 2,-1,-1,-1,-1,-1,-1,-1},
        { 0, 4, 2, 4, 6, 2,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1},
        { 1, 9, 0, 2, 3, 4, 2, 4, 6, 4, 3, 8,-1,-1,-1,-1},
        { 1, 9, 4, 1, 4, 2, 2, 4, 6,-1,-1,-1,-1,-1,-1,-1},
        { 8, 1, 3, 8, 6, 1, 8, 4, 6, 6,10, 1,-1,-1,-1,-1},
        {10, 1, 0,10, 0, 6, 6, 0, 4,-1,-1,-1,-1,-1,-1,-1},
        { 4, 6, 3, 4, 3, 8, 6,10, 3, 0, 3, 9,10, 9, 3,-1},
        {10, 9, 4, 6,10, 4,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1},
        { 4, 9, 5, 7, 6,11,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1},
        { 0, 8, 3, 4, 9, 5,11, 7, 6,-1,-1,-1,-1,-1,-1,-1},
        { 5, 0, 1, 5, 4, 0, 7, 6,11,-1,-1,-1,-1,-1,-1,-1},
        {11, 7, 6, 8, 3, 4, 3, 5, 4, 3, 1, 5,-1,-1,-1,-1},
        { 9, 5, 4,10, 1, 2, 7, 6,11,-1,-1,-1,-1,-1,-1,-1},
        { 6,11, 7, 1, 2,10, 0, 8, 3, 4, 9, 5,-1,-1,-1,-1},
        { 7, 6,11, 5, 4,10, 4, 2,10, 4, 0, 2,-1,-1,-1,-1},
        { 3, 4, 8, 3, 5, 4, 3, 2, 5,10, 5, 2,11, 7, 6,-1},
        { 7, 2, 3, 7, 6, 2, 5, 4, 9,-1,-1,-1,-1,-1,-1,-1},
        { 9, 5, 4, 0, 8, 6, 0, 6, 2, 6, 8, 7,-1,-1,-1,-1},
        { 3, 6, 2, 3, 7, 6, 1, 5, 0, 5, 4, 0,-1,-1,-1,-1},
        { 6, 2, 8, 6, 8, 7, 2, 1, 8, 4, 8, 5, 1, 5, 8,-1},
        { 9, 5, 4,10, 1, 6, 1, 7, 6, 1, 3, 7,-1,-1,-1,-1},
        { 1, 6,10, 1, 7, 6, 1, 0, 7, 8, 7, 0, 9, 5, 4,-1},
        { 4, 0,10, 4,10, 5, 0, 3,10, 6,10, 7, 3, 7,10,-1},
        { 7, 6,10, 7,10, 8, 5, 4,10, 4, 8,10,-1,-1,-1,-1},
        { 6, 9, 5, 6,11, 9,11, 8, 9,-1,-1,-1,-1,-1,-1,-1},
        { 3, 6,11, 0, 6, 3, 0, 5, 6, 0, 9, 5,-1,-1,-1,-1},
        { 0,11, 8, 0, 5,11, 0, 1, 5, 5, 6,11,-1,-1,-1,-1},
        { 6,11, 3, 6, 3, 5, 5, 3, 1,-1,-1,-1,-1,-1,-1,-1},
        { 1, 2,10, 9, 5,11, 9,11, 8,11, 5, 6,-1,-1,-1,-1},
        { 0,11, 3, 0, 6,11, 0, 9, 6, 5, 6, 9, 1, 2,10,-1},
        {11, 8, 5,11, 5, 6, 8, 0, 5,10, 5, 2, 0, 2, 5,-1},
        { 6,11, 3, 6, 3, 5, 2,10, 3,10, 5, 3,-1,-1,-1,-1},
        { 5, 8, 9, 5, 2, 8, 5, 6, 2, 3, 8, 2,-1,-1,-1,-1},
        { 9, 5, 6, 9, 6, 0, 0, 6, 2,-1,-1,-1,-1,-1,-1,-1},
        { 1, 5, 8, 1, 8, 0, 5, 6, 8, 3, 8, 2, 6, 2, 8,-1},
        { 1, 5, 6, 2, 1, 6,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1},
        { 1, 3, 6, 1, 6,10, 3, 8, 6, 5, 6, 9, 8, 9, 6,-1},
        {10, 1, 0,10, 0, 6, 9, 5, 0, 5, 6, 0,-1,-1,-1,-1},
        { 0, 3, 8, 5, 6,10,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1},
        {10, 5, 6,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1},
        {11, 5,10, 7, 5,11,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1},
        {11, 5,10,11, 7, 5, 8, 3, 0,-1,-1,-1,-1,-1,-1,-1},
        { 5,11, 7, 5,10,11, 1, 9, 0,-1,-1,-1,-1,-1,-1,-1},
        {10, 7, 5,10,11, 7, 9, 8, 1, 8, 3, 1,-1,-1,-1,-1},
        {11, 1, 2,11, 7, 1, 7, 5, 1,-1,-1,-1,-1,-1,-1,-1},
        { 0, 8, 3, 1, 2, 7, 1, 7, 5, 7, 2,11,-1,-1,-1,-1},
        { 9, 7, 5, 9, 2, 7, 9, 0, 2, 2,11, 7,-1,-1,-1,-1},
        { 7, 5, 2, 7, 2,11, 5, 9, 2, 3, 2, 8, 9, 8, 2,-1},
        { 2, 5,10, 2, 3, 5, 3, 7, 5,-1,-1,-1,-1,-1,-1,-1},
        { 8, 2, 0, 8, 5, 2, 8, 7, 5,10, 2, 5,-1,-1,-1,-1},
        { 9, 0, 1, 5,10, 3, 5, 3, 7, 3,10, 2,-1,-1,-1,-1},
        { 9, 8, 2, 9, 2, 1, 8, 7, 2,10, 2, 5, 7, 5, 2,-1},
        { 1, 3, 5, 3, 7, 5,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1},
        { 0, 8, 7, 0, 7, 1, 1, 7, 5,-1,-1,-1,-1,-1,-1,-1},
        { 9, 0, 3, 9, 3, 5, 5, 3, 7,-1,-1,-1,-1,-1,-1,-1},
        { 9, 8, 7, 5, 9, 7,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1},
        { 5, 8, 4, 5,10, 8,10,11, 8,-1,-1,-1,-1,-1,-1,-1},
        { 5, 0, 4, 5,11, 0, 5,10,11,11, 3, 0,-1,-1,-1,-1},
        { 0, 1, 9, 8, 4,10, 8,10,11,10, 4, 5,-1,-1,-1,-1},
        {10,11, 4,10, 4, 5,11, 3, 4, 9, 4, 1, 3, 1, 4,-1},
        { 2, 5, 1, 2, 8, 5, 2,11, 8, 4, 5, 8,-1,-1,-1,-1},
        { 0, 4,11, 0,11, 3, 4, 5,11, 2,11, 1, 5, 1,11,-1},
        { 0, 2, 5, 0, 5, 9, 2,11, 5, 4, 5, 8,11, 8, 5,-1},
        { 9, 4, 5, 2,11, 3,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1},
        { 2, 5,10, 3, 5, 2, 3, 4, 5, 3, 8, 4,-1,-1,-1,-1},
        { 5,10, 2, 5, 2, 4, 4, 2, 0,-1,-1,-1,-1,-1,-1,-1},
        { 3,10, 2, 3, 5,10, 3, 8, 5, 4, 5, 8, 0, 1, 9,-1},
        { 5,10, 2, 5, 2, 4, 1, 9, 2, 9, 4, 2,-1,-1,-1,-1},
        { 8, 4, 5, 8, 5, 3, 3, 5, 1,-1,-1,-1,-1,-1,-1,-1},
        { 0, 4, 5, 1, 0, 5,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1},
        { 8, 4, 5, 8, 5, 3, 9, 0, 5, 0, 3, 5,-1,-1,-1,-1},
        { 9, 4, 5,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1},
        { 4,11, 7, 4, 9,11, 9,10,11,-1,-1,-1,-1,-1,-1,-1},
        { 0, 8, 3, 4, 9, 7, 9,11, 7, 9,10,11,-1,-1,-1,-1},
        { 1,10,11, 1,11, 4, 1, 4, 0, 7, 4,11,-1,-1,-1,-1},
        { 3, 1, 4, 3, 4, 8, 1,10, 4, 7, 4,11,10,11, 4,-1},
        { 4,11, 7, 9,11, 4, 9, 2,11, 9, 1, 2,-1,-1,-1,-1},
        { 9, 7, 4, 9,11, 7, 9, 1,11, 2,11, 1, 0, 8, 3,-1},
        {11, 7, 4,11, 4, 2, 2, 4, 0,-1,-1,-1,-1,-1,-1,-1},
        {11, 7, 4,11, 4, 2, 8, 3, 4, 3, 2, 4,-1,-1,-1,-1},
        { 2, 9,10, 2, 7, 9, 2, 3, 7, 7, 4, 9,-1,-1,-1,-1},
        { 9,10, 7, 9, 7, 4,10, 2, 7, 8, 7, 0, 2, 0, 7,-1},
        { 3, 7,10, 3,10, 2, 7, 4,10, 1,10, 0, 4, 0,10,-1},
        { 1,10, 2, 8, 7, 4,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1},
        { 4, 9, 1, 4, 1, 7, 7, 1, 3,-1,-1,-1,-1,-1,-1,-1},
        { 4, 9, 1, 4, 1, 7, 0, 8, 1, 8, 7, 1,-1,-1,-1,-1},
        { 4, 0, 3, 7, 4, 3,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1},
        { 4, 8, 7,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1},
        { 9,10, 8,10,11, 8,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1},
        { 3, 0, 9, 3, 9,11,11, 9,10,-1,-1,-1,-1,-1,-1,-1},
        { 0, 1,10, 0,10, 8, 8,10,11,-1,-1,-1,-1,-1,-1,-1},
        { 3, 1,10,11, 3,10,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1},
        { 1, 2,11, 1,11, 9, 9,11, 8,-1,-1,-1,-1,-1,-1,-1},
        { 3, 0, 9, 3, 9,11, 1, 2, 9, 2,11, 9,-1,-1,-1,-1},
        { 0, 2,11, 8, 0,11,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1},
        { 3, 2,11,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1},
        { 2, 3, 8, 2, 8,10,10, 8, 9,-1,-1,-1,-1,-1,-1,-1},
        { 9,10, 2, 0, 9, 2,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1},
        { 2, 3, 8, 2, 8,10, 0, 1, 8, 1,10, 8,-1,-1,-1,-1},
        { 1,10, 2,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1},
        { 1, 3, 8, 9, 1, 8,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1},
        { 0, 9, 1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1},
        { 0, 3, 8,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1},
        {-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1}

    };

}
