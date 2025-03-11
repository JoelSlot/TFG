using System.Collections.Generic;
using Utils;
using UnityEngine;
using System;
using System.Linq;

public class CubePaths : MonoBehaviour
{

    //Diccionario de todas las pheromonas. Son indexadas según su posición, para que una hormiga pueda fácilmente acceder a las pheromonas en los cubos alrededores.
    //Ya que pueden haber pheromonas de caminos distintos en el mismo cubo, y pueden haber múltiples superficies con pheromonas del mismo camino en un mismo cubo, se guarda una lista de todas aquellas que se encuentran en cada cubo
    public static Dictionary<Vector3Int, List<CubePheromone>> cubePherDict = new Dictionary<Vector3Int, List<CubePheromone>>();
    //El diccionario de caminos, contiene los primeros nodos de los caminos indexados por su pathId
    public static Dictionary<int, CubePheromone> pathDict = new Dictionary<int, CubePheromone>();

    /*
    Comienza un nuevo camino de pheromonas creando el primer miembro y colocandolo en el mapa.

    pos: las coordinadas del cubo en el que se crea la feromona
    surfaceNormal: la normal de la superficie sobre la que se coloca la feromona

    return: la feromona nueva creada
    */
    public static CubePheromone StartPheromoneTrail(Vector3Int pos, Vector3 surfaceNormal)
    {
        CubePheromone newPher = new CubePheromone(pos, CornerFromNormal(surfaceNormal));
        PlacePheromone(pos, newPher);
        pathDict.Add(newPher.GetPathId(), newPher); // añadir primer pher del nuevo camino al dictionario
        return newPher;
    }

    /*
    Continúa un camino de feromonas ya existente, conectando el dado con el generado.

    pos: las coordinadas del cubo en el que se crea la feromona
    prevPher: la feromona previa
    
    return: la feromona nueva creada
    */
    public static CubePheromone ContinuePheromoneTrail(Vector3Int pos, CubePheromone prevPher)
    {
        CubePheromone newPher = new CubePheromone(pos, prevPher);
        PlacePheromone(pos, newPher);
        return newPher;
    }

    
    /*
    Coloca una feromona en la posición indicada, reemplazando la feromona que ya se encuentra en dicho lugar si tiene la misma pathId y está en la misma superficie

    pos: La posición del cubo en la que se coloca la feromona
    newPher: el objecto CubePheromone que debe colocarse en la posicion dada
    */
    private static void PlacePheromone(Vector3Int pos, CubePheromone newPher)
    {
        List<CubePheromone> pheromones;
        if (cubePherDict.TryGetValue(pos, out pheromones)) //Si ya hay feromonas en el cubo indicado: 
        {
            bool replaced = false;                          
            for (int i = 0; i < pheromones.Count; i++)
            {
                CubePheromone oldPher = cubePherDict[pos][i];
                if (oldPher.SameCubeSamePathSameSurface(newPher))   //Si una de las feromonas del cubo tiene la misma superficie y pathId
                {
                    newPher.prev = oldPher.prev;                        //Desconectar el camino inutil
                    cubePherDict[pos][i] = newPher;                     //Reemplazar el CubePheromone viejo
                    replaced = true;
                    break;
                }
            }
            if (!replaced) pheromones.Add(newPher);                 //Si no ha sido reemplazado uno, añadir a lista
        }
        else                                                //Si no había ya alguna pheromona, añadir
        {
            pheromones = new List<CubePheromone>(){newPher};
            cubePherDict.Add(pos, pheromones);
        }

        DrawCube(pos, Color.black, 100000);
    }

    /*
    Dado un cubo y los valores de las esquinas respecto están debajo de la superficie indicada devuelve las pheromonas que se encuentran sobre dicha superficie

    surfaceCube: Pos del cubo que contiene la superficie
    belowSurfaceCorner: valores de las esquinas del cubo que indican la superficie.

    return: Lista de feromonas en la superficie.
    */
    public static List<CubePheromone> GetPheromonesOnSurface(CubeSurface surface)
    {
        List<CubePheromone> sameSurfacePheromones = new List<CubePheromone>();

        if (cubePherDict.TryGetValue(surface.pos, out List<CubePheromone> pheromones))
        {
            //Debug.Log("There is good shit here:" + pheromones.Count);
            foreach (var pheromone in pheromones)
            {
                if (CompareGroups(surface.surfaceGroup, pheromone.GetSurfaceGroup()))
                {
                    sameSurfacePheromones.Add(pheromone);
                }
            }
        }
        //Debug.Log("Returning " + sameSurfacePheromones.Count + " pheromones in cube " + surfaceCube);
        return sameSurfacePheromones;
    } 

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    /*
    Dado dos grupos de valores de esquinas, devuelve si son iguales
    */
    public static bool CompareGroups (bool[] G1, bool[] G2) 
    { 
        /*Debug.Log(G1[0] + "-" + G2[0] + ", " +
                  G1[1] + "-" + G2[1] + ", " + 
                  G1[2] + "-" + G2[2] + ", " + 
                  G1[3] + "-" + G2[3] + ", " + 
                  G1[4] + "-" + G2[4] + ", "+ 
                  G1[5] + "-" + G2[5] + ", " + 
                  G1[6] + "-" + G2[6] + ", "+ 
                  G1[7] + "-" + G2[7] + "?"
        );*/
        for (int i = 0; i < 8; i++) //chek if same surface
            if (G1[i] != G2[i]) return false;
        //Debug.Log("TRUE!!!!!");
        return true; 
    }

    /*
    Dado una superficie de un cubo, devuelve los cubos adyacentes que conectan con esa superficie.

    surface: superficie de la que se buscan los adyacentes

    forwardDir: Dirección en la que está mirando la hormiga.

    return: Lista de superficies adyacentes, ordenados en el que está más hacia la dirección dada hasta la que menos.
    */
    public static List<CubeSurface> GetAdyacentCubes(CubeSurface surface, Vector3 forwardDir) // cube contains the cube pos and one of the points below
    {
        List<CubeSurface> adyacentCubes = new List<CubeSurface>();

        List<int> index = new List<int>{0,1,2,3,4,5};

        index.Sort((x, y) => (int)(Vector3.Angle(forwardDir, chunk.faceDirections[x]) - Vector3.Angle(forwardDir, chunk.faceDirections[y])));
        
        for (int i = 0; i < 6; i++)
        {
            if (FaceXOR(index[i], surface.surfaceGroup))
            {
                Vector3Int dir = chunk.faceDirections[index[i]]; //Get dir
                bool[] newCornerValues = CubeCornerValues(surface.pos + dir); //Get new cube cornerValues
                Vector3Int newSurfaceCorner = TrueCorner(index[i], surface.surfaceGroup) - dir; //Get corner value
                bool[] newGroupCornerValues = GetGroup(newSurfaceCorner, newCornerValues);
                adyacentCubes.Add(new CubeSurface(surface.pos + dir, newGroupCornerValues));
            }
        }

        return adyacentCubes;
    }

    public static List<CubeSurface> GetAdyacentCubes(CubeSurface surface) // cube contains the cube pos and one of the points below
    {
        List<CubeSurface> adyacentCubes = new List<CubeSurface>();

        for (int i = 0; i < 6; i++)
        {
            if (FaceXOR(i, surface.surfaceGroup))
            {
                Vector3Int dir = chunk.faceDirections[i]; //Get dir
                bool[] newCornerValues = CubeCornerValues(surface.pos + dir); //Get new cube cornerValues
                Vector3Int newSurfaceCorner = TrueCorner(i, surface.surfaceGroup) - dir; //Get corner value
                bool[] newGroupCornerValues = GetGroup(newSurfaceCorner, newCornerValues);
                adyacentCubes.Add(new CubeSurface(surface.pos + dir, newGroupCornerValues));
            }
        }

        return adyacentCubes;
    }

    /*
    Dado la posición de un cubo, devuelve para todas las esquinas si esa esquina se encuentra debajo del terreno o no.

    cube: posición del cubo que se quiere mirar.

    return: array de 8 bools, uno por cada esquina en orden según el cornerTable de la clase Chunk. Si un bool es true, su esquina se encuentra debajo del terreno y viceversa.
    */
    public static bool[] CubeCornerValues(Vector3Int cube)
    {
        bool[] cornerValues = new bool[8];
        for (int i = 0; i < 8; i++)
            cornerValues[i] = !WorldGen.IsAboveSurface(cube + chunk.cornerTable[i]);
        return cornerValues;
    }

    /*
    TrueCorner sirve para devolver una de las esquinas debajo de la superficie que comparten dos cubos. Dado la cara del cubo principal que conecta con el otro cubo, y los valores de las esquinas del cubo principal, el primer valor debajo de la superficie de la cara indicada es devuelto.

    faceIndex: El índice de la cara que conecta el cubo 1 con el cubo 2.
    cornerValues: Los valores de las esquinas del cubo 1 respecto a la superficie que conecta los dos cubos.

    return: La posición dentro del cubo 2 de una de las esquinas del cubo 1 que conecta los dos cubos y se encuentra debajo de la superficie.
    */
    public static Vector3Int TrueCorner(int faceIndex, bool[] cornerValues)
    {
        if (cornerValues[chunk.faceIndexes[faceIndex,0]]) return chunk.cornerTable[chunk.faceIndexes[faceIndex,0]];
        if (cornerValues[chunk.faceIndexes[faceIndex,1]]) return chunk.cornerTable[chunk.faceIndexes[faceIndex,1]];
        if (cornerValues[chunk.faceIndexes[faceIndex,2]]) return chunk.cornerTable[chunk.faceIndexes[faceIndex,2]];
        return chunk.cornerTable[chunk.faceIndexes[faceIndex,3]];
    }


    /*
    FaceXor comprueba si una superficie de un cubo corta con la cara dada. Esto es útil, porque si una cara es cortada por la superficie, significa que la superficie corta el cubo adyacente conectado por la cara dada.
    Se puede hablar sobre cómo se simplificó una función inicialmente muy compleja con el objetivo de encontrar los cubos adyacentes por los que pasa la superficie.

    faceIndex: Int que indica la cara del cubo del que se quiere ver si la superficie lo corta.
    cornerValues: Los valores de la esquina del cubo según se encuentran debajo de la superficie o no.

    return: Bool que es true si la cara es cortada por la superficie.
    */
    private static bool FaceXOR(int faceIndex, bool[] cornerValues)
    {
        return !(
            cornerValues[chunk.faceIndexes[faceIndex, 0]] == cornerValues[chunk.faceIndexes[faceIndex, 1]] && 
            cornerValues[chunk.faceIndexes[faceIndex, 0]] == cornerValues[chunk.faceIndexes[faceIndex, 2]] && 
            cornerValues[chunk.faceIndexes[faceIndex, 0]] == cornerValues[chunk.faceIndexes[faceIndex, 3]] );
    }

    /*
    Agrupa los puntos según si están conectados todos. Se le pasan los valores de cubo según si están debajo o encima del terreno y uno de las esquinas debajo de la superficie que se quiere identificar.
    Devuelve todos los puntos debajo de la superficie conectados con el punto pasado.

    antCorner: Uno de los puntos debajo de la superficie.
    cornerValues: array de bool con los valores de las esquinas del cubo. True si debajo del terreno, false si no.

    return: array de bool con los valores de las esquinas del cubo. True si debajo de la superficie dada, false si no.
    */
    public static bool[] GetGroup(Vector3Int antCorner, bool[] cornerValues)
    {
        bool[] group = new bool[8]; //Valor defecto de bool es falso (CONFIRMED)
        group[chunk.reverseCornerTable[antCorner]] = true; //BIG ISSUE BY FORGETTING THIS
        HashSet<Vector3Int> checkedCorners = new HashSet<Vector3Int>();
        Queue<Vector3Int> cornersToCheck = new Queue<Vector3Int>();
        cornersToCheck.Enqueue(antCorner);

        while (cornersToCheck.Count > 0)
        {
            Vector3Int corner = cornersToCheck.Dequeue(); //Cogemos el siguiente
            checkedCorners.Add(corner); //anotamos que lo miramos
            foreach (Vector3Int adyCorner in AdyacentCorners(corner))
            {
                if (!checkedCorners.Contains(adyCorner)) // si no lo hemos mirado
                {
                    if (cornerValues[chunk.reverseCornerTable[adyCorner]]) // si está debajo del suelo
                    {
                        group[chunk.reverseCornerTable[adyCorner]] = true; // Añadimos la esquina al grupo
                        cornersToCheck.Enqueue(adyCorner); // Y lo preparamos para mirar sus adyacentes
                    }
                }
            }
        }
        for (int i = 0; i < 8; i++) //Quedarse con los debajo del suelo y en el grupo
            cornerValues[i] = cornerValues[i] && group[i];

        return cornerValues;
    }

    /*
    Devuelve las esquinas de un cubo adyacentes a la esquina dada. Usada en la función GetGroup

    corner: la esquina del que se quieren conseguir los adyacentes

    return: Lista de coordinadas Vector3 de las esquinas adyacentes.
    */
    private static List<Vector3Int> AdyacentCorners(Vector3Int corner)
    {
        return new List<Vector3Int>(){
            new Vector3Int(Mathf.Abs(corner.x-1), corner.y, corner.z),
            new Vector3Int(corner.x, Mathf.Abs(corner.y-1), corner.z),
            new Vector3Int(corner.x, corner.y, Mathf.Abs(corner.z-1))
            };
        
    }

    /*
    CornerFromNormal devuelve la esquina más debajo de la superficie dado la normal de la superficie. Se consigue cogiendo la esquina cuya dirección hacia el centro del cubo se parezca más a la normal.

    normal: Vector3 dirección de la normal de la superficie.

    return: Vector3Int coordenada de una de las esquinas debajo de la superficie.
    */
    public static Vector3Int CornerFromNormal(Vector3 normal)
    {
        Vector3Int returnCorner = Vector3Int.zero;
        float minAngle = 180;
        foreach(Vector3Int corner in chunk.cornerTable)
        {
            float angle = Vector3.Angle(normal, CornerNormal(corner));
            if (angle < minAngle)
            {
                minAngle = angle;
                returnCorner = corner;
            }
        }
        return returnCorner;
    }

    /*Dado una de las esquinas de un cubo como las de chunk.cornerTable devuelve la dir de él hacia el centro */
    public static Vector3Int CornerNormal(Vector3Int corner)
    {
        Vector3Int opposite = new Vector3Int(Mathf.Abs(corner.x - 1), Mathf.Abs(corner.y - 1), Mathf.Abs(corner.z - 1));
        return opposite - corner;
    }

    //Dado el cubo actual, la dir en la que se quiere mover y el grupo subSuperficie de la superficie actual, devuelve el punto a seguir para llegar
    public static Vector3 GetMovementGoal(CubeSurface surface, Vector3Int dir) //faceIndex points to face with same index as dirIndex
    {
        int faceIndex = chunk.reverseFaceDirections[dir];

        Vector3 goal = Vector3.zero;
        int num = 0;
        for (int i = 0; i < 4; i++)
        {
            int cornerIndex = chunk.faceIndexes[faceIndex, i];
            if (!surface.surfaceGroup[cornerIndex]) //Si el punto no se encuentra bajo la superficie
            {
                goal += chunk.cornerTable[cornerIndex];
                num++;
            }
        }
        goal = surface.pos + goal / num;
        Debug.DrawLine(goal, goal + chunk.faceDirections[faceIndex], Color.blue, 10);
        return goal + chunk.faceDirections[faceIndex];
    }

    /*
    Debuja el cubo dado, en el color dado, durando el tiempo dado
    */
    public static void DrawCube(Vector3Int cube, Color color, int time)
    {
        for (int i = 0; i < 12; i++)
        {
            Debug.DrawLine(cube + chunk.cornerTable[chunk.edgeIndexes[i, 0]], cube + chunk.cornerTable[chunk.edgeIndexes[i, 1]], color, time);
        }
    }

    
    //Función que dado dos cubos y la subSuperficie del primero, devuelve si esa superficie conecta directamente con el segundo cubo
    public static bool DoesSurfaceConnect(CubeSurface surface1, Vector3Int cube2)
    {
        Vector3Int dir = surface1.pos - cube2; //La dir al segundo cubo
        int dirIndex;
        if (!chunk.reverseFaceDirections.TryGetValue(dir, out dirIndex)) return false; //Si no son dayacente falso
        return FaceXOR(dirIndex, surface1.surfaceGroup);
    }


    public struct CubeSurface
    {
        public bool[] surfaceGroup;
        public Vector3Int pos;
        public CubeSurface(Vector3Int newPos, Vector3Int belowSurfacePoint)
        {
            surfaceGroup = GetGroup(belowSurfacePoint, CubeCornerValues(newPos));
            pos = newPos;
        }
        public CubeSurface(Vector3Int newPos, Vector3 surfaceNormals)
        {
            surfaceGroup = GetGroup(CornerFromNormal(surfaceNormals), CubeCornerValues(newPos));
            pos = newPos;
        }
        public CubeSurface(Vector3Int newPos, bool[] newSurfaceGroup)
        {
            pos = newPos;
            surfaceGroup = newSurfaceGroup;
        }
        public override bool Equals(object obj)
        {
            if (!(obj is CubeSurface))
                return false;

            CubeSurface mys = (CubeSurface) obj;
            // compare elements here

            if (pos != mys.pos) return false;

            for (int i = 0; i < 8; i++)
                if (surfaceGroup[i] != mys.surfaceGroup[i]) return false;
            
            return true;
        }
        public override int GetHashCode()
        {
            return pos.x + pos.y * 1000 + pos.z * 1000000;
        }
    }

    static void DrawSurface(CubeSurface cubeSurface, Color color, int time)
    {
        for (int i = 0; i < 8; i++)
        {
            if (cubeSurface.surfaceGroup[i])
                Debug.DrawLine(cubeSurface.pos + Vector3.one/2, cubeSurface.pos + chunk.cornerTable[i], color, time);
        }
    }


    public static List<CubeSurface> PathToDigPoint(CubeSurface start, Vector3Int objective)
    {
        List<CubeSurface> path = new List<CubeSurface>();

        PriorityQueue<CubeSurface, float> frontera = new PriorityQueue<CubeSurface, float>();
        frontera.Enqueue(start, 0);
        Dictionary<CubeSurface, CubeSurface> previo = new Dictionary<CubeSurface, CubeSurface>();
        Dictionary<CubeSurface, float> coste = new Dictionary<CubeSurface, float>();

        previo[start] = start;
        coste[start] = 0;


        float heuristic(Vector3Int point, Vector3Int objective)
        {
            return Mathf.Abs(point.x - objective.x) + Mathf.Abs(point.z - objective.z) + Mathf.Abs(point.z - objective.z);
        }

        CubeSurface reachedSurface = start;

        while (frontera.Count > 0 && reachedSurface.pos != objective)
        {
            CubeSurface current = frontera.Dequeue();

            if (current.pos == objective)
                break;
            
            List<CubeSurface> adyacentSurfaces = GetAdyacentCubes(current);

            foreach(var son in adyacentSurfaces)
            {
                float newCost = coste[current] + 1;
                if (!CompareGroups(son.surfaceGroup, current.surfaceGroup)) newCost += 1;
                bool updateOrInsert = false;
                if (!coste.TryGetValue(son, out float prevCost)) updateOrInsert = true;
                else if (newCost < prevCost) updateOrInsert = true;

                if(updateOrInsert)
                {
                    //DrawCube(son.pos, Color.black, 5);
                    coste[son] = newCost;
                    float prioridad = newCost + heuristic(son.pos, objective);
                    frontera.Enqueue(son, prioridad);
                    previo[son] = current;

                    if (son.pos == objective) reachedSurface = son;
                }
            }
        }

        while (!reachedSurface.Equals(start))
        {
            DrawCube(reachedSurface.pos, Color.blue, 40);
            DrawSurface(reachedSurface, Color.black, 40);
            path.Append(reachedSurface);
            reachedSurface = previo[reachedSurface];
        }

        path.Append(reachedSurface);
        DrawCube(reachedSurface.pos, Color.blue, 20);

        return path;
    }

}
