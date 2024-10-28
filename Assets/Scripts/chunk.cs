using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using static UnityEditor.Progress;

public class chunk
{

    List<Vector3> vertices = new List<Vector3>();
    List<int> triangles = new List<int>();

    WorldGen father;

    public GameObject chunkObject;
    MeshFilter meshFilter;
    MeshCollider meshCollider;
    MeshRenderer meshRenderer;

    Vector3Int chunkPosition;


    int xdim { get { return WorldGen.chunk_x; } }
    int ydim { get { return WorldGen.chunk_y; } }
    int zdim { get { return WorldGen.chunk_z; } }
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
        father = dad;

        meshFilter = chunkObject.AddComponent<MeshFilter>();
        meshCollider = chunkObject.AddComponent<MeshCollider>();
        meshRenderer = chunkObject.AddComponent<MeshRenderer>();
        //set mesh material
        meshRenderer.material = father.GetComponent<MeshRenderer>().material;
        //not sure if relevant
        chunkObject.transform.tag = "Terrain";

        CreateMeshData();

    }

    // Update is called once per frame
    void Update()
    {

        

    }

    

    int GetCubeConfig(float[] cube)
    {

        int configIndex = 0;
        for (int i = 0; i < 8; i++)
        {
            if (cube[i] > isolevel)
                configIndex |= 1 << i;
        }

        return configIndex;

    }

    //
    void marchCube(Vector3Int position)
    {

        //sample terrain values at each corner of the cube

        float[] cube = new float[8];
        for (int i = 0; i < 8; i++)
        {
            cube[i] = father.SampleTerrain(position + GameData.CornerTable[i]);
        }

        int configIndex = GetCubeConfig(cube);

        //There are no triangles, we won't draw it
        if (configIndex == 0 || configIndex == 255) return;

        int edgeIndex = 0;
        //maximum of 5 triangles in each cube
        for (int i = 0; i < 5; i++) 
        {
        
            //3 vertices on each triangle
            for (int p = 0; p < 3; p++)
            {

                int indice = GameData.TriangleTable[configIndex, edgeIndex];

                if (indice == -1) return;


                //getting the midpoint of the vertice (between two edges) of the cube
                Vector3 vert1pos = position + GameData.CornerTable[GameData.EdgeIndexes[indice, 0]];
                Vector3 vert2pos = position + GameData.CornerTable[GameData.EdgeIndexes[indice, 1]];

                //position of current triangle vertex
                Vector3 vertPosition;
                
                
                //get terrain values at each end of edge
                float vert1val = cube[GameData.EdgeIndexes[indice, 0]];
                float vert2val = cube[GameData.EdgeIndexes[indice, 1]];

                //calculate their difference
                float difference = vert2val - vert1val;


                if (difference == 0)
                    difference = isolevel;
                else
                    difference = (isolevel - vert1val) / difference;


                //calculate the point along the edge that passes through
                vertPosition = vert1pos + ((vert2pos - vert1pos) * difference);

                vertices.Add(vertPosition);
                triangles.Add(vertices.Count - 1);
                edgeIndex++;

            }

        }

    }

    

    

    void ClearMeshData()
    {

        vertices.Clear();
        triangles.Clear();

    }

    public void CreateMeshData()
    {

        ClearMeshData();

        for (int x = 0; x < xdim; x++)
        {
            for (int z = 0; z < zdim; z++)
            {
                for (int y = 0; y < ydim; y++)
                {

                    marchCube(new Vector3Int(x + chunkPosition.x, y + chunkPosition.y, z + chunkPosition.z));
                    
                }
            }
            
        }

        BuildMesh();

    }


    void BuildMesh()
    {

        Mesh mesh = new Mesh();
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.RecalculateNormals();
        meshFilter.mesh = mesh;
        meshCollider.sharedMesh = mesh;
    }

    //Destruye todos los gameobjects incluidos en el chunk.
    public void Destroy()
    {
        UnityEngine.Object.DestroyImmediate(chunkObject.GetComponent<MeshFilter>());
        UnityEngine.Object.DestroyImmediate(chunkObject.GetComponent<MeshCollider>());
        UnityEngine.Object.DestroyImmediate(chunkObject.GetComponent<MeshRenderer>());
        UnityEngine.Object.DestroyImmediate(chunkObject);
    }

}
