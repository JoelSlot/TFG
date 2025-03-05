using System.Collections.Generic;
using NUnit.Framework.Internal;
using System.Collections;
using UnityEngine;
using System;

public class CubePaths : MonoBehaviour
{
    public class cubePheromone
    {
        //These fucking things gotta be differentiated by their pos, id and surface. Damn.
        static int nextId = -1;
        int pathId;
        int pathPos;
        Vector3Int pos;
        bool[] surfaceGroup; //El grupo de esquinas que son true si es encuentran debajo de la superficie
        public cubePheromone prev;
        public cubePheromone next;

        //Crea una nueva feromona como comienzo de un nuevo camino el la posición pherPos
        public cubePheromone(Vector3Int pherPos, Vector3Int pointBelowSurface)
        {
            pathId = GetNextId();
            pathPos = 0;
            //Previo/siguiente se señalan a si mismos =  comienzo/final del camino
            prev = this;
            next = this;
            pos = pherPos;
            makeSurfaceGroup(pointBelowSurface);
        }

        //Crea el siguientd nodo en el camino dado el actual
        public cubePheromone(Vector3Int pherPos, cubePheromone prevPher)
        {
            pathId = prevPher.pathId;
            pathPos = prevPher.pathPos + 1;
            prev = prevPher;
            next = this;
            pos = pherPos;
            makeSurfaceGroup(prevPher);

            prevPher.next = this;

        }

        private static int GetNextId()
        {
            nextId += 1;
            return nextId;
        }

        //Devuelve el siguiente nodo del camino dependiendo de si se mueve hacia delante o hacia detrás
        public cubePheromone GetNext(bool forward)
        {
            if (forward) return next;
            else return prev;
        }

        //Devuelve si el nodo es el ultimo del camino dado si se está moviendo hacia delante o hacia detrás
        public bool isLast(bool forward)
        {
            if (forward) return next == this;
            else return prev == this;
        }

        //Devuelve el pathId del nodo
        public int GetPathId() {return pathId;}

        //Devuelve si el nodo de pheromona se encuentra en:
        //La misma posición y
        //El mismo camino y
        //La misma superficie
        //Es decir, si son exactamente iguales
        public bool SameCubeSamePathSameSurface(cubePheromone other)
        {
            if (pos != other.pos) return false; //check if same pos
            if (pathId != other.pathId) return false; //check if same path
            return CompareGroups(surfaceGroup, other.surfaceGroup);
        }

        //Asigna el grupo de valores de las esquinas del cubo según si están debajo de la superficie de la feromona.
        //Usado para diferenciar nodos en superficies distintas.
        //Versión que usa un punto dado debajo de la superficie.
        public void makeSurfaceGroup(Vector3Int pointUnderSurface)
        {
            surfaceGroup = GetGroup(pointUnderSurface, CubeCornerValues(pos));
        }

        //Versión que usa la dirección de la superficie
        public void makeSurfaceGroup(cubePheromone adyacentCube)
        {
            int newCubeDirIndex = chunk.reverseFaceDirections[pos - adyacentCube.pos]; //obtenemos indice de dir desde adyacente a actual cubo
            Vector3Int pointUnderSurface = TrueCorner(newCubeDirIndex, adyacentCube.surfaceGroup) - chunk.faceDirections[newCubeDirIndex]; //Mediante dicho índice conseguimos uno de los puntos compartidos debajo de la superficie
            makeSurfaceGroup(pointUnderSurface);
        }

        //Devuelve el grupo de valores de esquinas de la superficie
        public bool[] GetSurfaceGroup()
        {
            return this.surfaceGroup;
        }

        //Devuelve el cubo en el que se encuentra la feromona
        public Vector3Int GetPos()
        {
            return pos;
        }

    }

    //Diccionario de todas las pheromonas. Son indexadas según su posición, para que una hormiga pueda fácilmente acceder a las pheromonas en los cubos alrededores.
    //Ya que pueden haber pheromonas de caminos distintos en el mismo cubo, y pueden haber múltiples superficies con pheromonas del mismo camino en un mismo cubo, se guarda una lista de todas aquellas que se encuentran en cada cubo
    public static Dictionary<Vector3Int, List<cubePheromone>> cubePherDict = new Dictionary<Vector3Int, List<cubePheromone>>();
    //El diccionario de caminos, contiene los primeros nodos de los caminos indexados por su pathId
    public static Dictionary<int, cubePheromone> pathDict = new Dictionary<int, cubePheromone>();

    /*
    Comienza un nuevo camino de pheromonas creando el primer miembro y colocandolo en el mapa.

    pos: las coordinadas del cubo en el que se crea la feromona
    surfaceNormal: la normal de la superficie sobre la que se coloca la feromona

    return: la feromona nueva creada
    */
    public static cubePheromone StartPheromoneTrail(Vector3Int pos, Vector3 surfaceNormal)
    {
        cubePheromone newPher = new cubePheromone(pos, CornerFromNormal(surfaceNormal));
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
    public static cubePheromone ContinuePheromoneTrail(Vector3Int pos, cubePheromone prevPher)
    {
        cubePheromone newPher = new cubePheromone(pos, prevPher);
        PlacePheromone(pos, newPher);
        return newPher;
    }

    
    /*
    Coloca una feromona en la posición indicada, reemplazando la feromona que ya se encuentra en dicho lugar si tiene la misma pathId y está en la misma superficie

    pos: La posición del cubo en la que se coloca la feromona
    newPher: el objecto cubePheromone que debe colocarse en la posicion dada
    */
    private static void PlacePheromone(Vector3Int pos, cubePheromone newPher)
    {
        List<cubePheromone> pheromones;
        if (cubePherDict.TryGetValue(pos, out pheromones)) //Si ya hay feromonas en el cubo indicado: 
        {
            bool replaced = false;                          
            for (int i = 0; i < pheromones.Count; i++)
            {
                cubePheromone oldPher = cubePherDict[pos][i];
                if (oldPher.SameCubeSamePathSameSurface(newPher))   //Si una de las feromonas del cubo tiene la misma superficie y pathId
                {
                    newPher.prev = oldPher.prev;                        //Desconectar el camino inutil
                    cubePherDict[pos][i] = newPher;                     //Reemplazar el cubePheromone viejo
                    replaced = true;
                    break;
                }
            }
            if (!replaced) pheromones.Add(newPher);                 //Si no ha sido reemplazado uno, añadir a lista
        }
        else                                                //Si no había ya alguna pheromona, añadir
        {
            pheromones = new List<cubePheromone>(){newPher};
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
    public static List<cubePheromone> GetPheromonesOnSurface(Vector3Int surfaceCube, Vector3Int belowSurfaceCorner)
    {
        bool[] surfaceGroup = GetGroup(belowSurfaceCorner, CubeCornerValues(surfaceCube));
        List<cubePheromone> sameSurfacePheromones = new List<cubePheromone>();

        if (cubePherDict.TryGetValue(surfaceCube, out List<cubePheromone> pheromones))
        {
            //Debug.Log("There is good shit here:" + pheromones.Count);
            foreach (var pheromone in pheromones)
            {
                if (CompareGroups(surfaceGroup, pheromone.GetSurfaceGroup()))
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
    Dado un cubo y una superficie de ese cubo, devuelve los cubos adyacentes que conectan con esa superficie.

    cube: Posición del cubo que contiene la superficie.
    surfaceNormal: La normal de la superficie, usada para señalar a cual de los posiblemente múltiples superficies del cubo se trata de buscar los adyacentes

    return: Lista de pares de valores, siendo la primera de cada par la posición de un cubo adyacente, y la segunda del par una de las esquinas debajo de la superficie de ese cubo adyacente.
    */
    public static List<Tuple<Vector3Int, Vector3Int>> GetAdyacentCubes(Vector3Int cube, Vector3 surfaceNormal)
    {
        List<Tuple<Vector3Int, Vector3Int>> adyacentCubes = new List<Tuple<Vector3Int, Vector3Int>>();

        bool[] cornerValues = CubeCornerValues(cube);
        
        Vector3Int antCorner = CornerFromNormal(surfaceNormal);
        
        bool[] groupCornerValues = GetGroup(antCorner, cornerValues);
        
        for (int i = 0; i < 6; i++)
        {
            if (FaceXOR(i, groupCornerValues))
            {
                //Debug.DrawLine(cube + new Vector3(0.5f, 0.5f, 0.5f), cube + new Vector3(0.5f, 0.5f, 0.5f) + chunk.faceDirections[i], Color.black, 10);
                adyacentCubes.Add(new Tuple<Vector3Int, Vector3Int>(cube + chunk.faceDirections[i], TrueCorner(i, groupCornerValues)));
            }
        }

        return adyacentCubes;
    }

    /*
    Dado una superficie de un cubo, devuelve los cubos adyacentes que conectan con esa superficie.

    cubeAndPoint: Par que contiene la pos del cubo y una esquina del cubo que se encuentra debajo de la superficie.

    return: Lista de pares de valores, siendo la primera de cada par la posición de un cubo adyacente, y la segunda del par una de las esquinas debajo de la superficie de ese cubo adyacente.
    */
    public static List<Tuple<Vector3Int, Vector3Int>> GetAdyacentCubes(Tuple<Vector3Int, Vector3Int> cubeAndPoint) // cube contains the cube pos and one of the points below
    {
        List<Tuple<Vector3Int, Vector3Int>> adyacentCubes = new List<Tuple<Vector3Int, Vector3Int>>();

        bool[] cornerValues = CubeCornerValues(cubeAndPoint.Item1);

        bool[] groupCornerValues = GetGroup(cubeAndPoint.Item2, cornerValues);
        
        for (int i = 0; i < 6; i++)
        {
            if (FaceXOR(i, groupCornerValues))
            {
                //Debug.DrawLine(cube.Item1 + new Vector3(0.5f, 0.5f, 0.5f), cube.Item1 + new Vector3(0.5f, 0.5f, 0.5f) + chunk.faceDirections[i], Color.black, 10);
                adyacentCubes.Add(new Tuple<Vector3Int, Vector3Int>(cubeAndPoint.Item1 + chunk.faceDirections[i], TrueCorner(i, groupCornerValues)));
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
    public static Vector3 GetMovementGoal(Vector3Int cubePos, bool[] surfaceGroup, Vector3Int dir) //faceIndex points to face with same index as dirIndex
    {
        int faceIndex = chunk.reverseFaceDirections[dir];

        Vector3 goal = Vector3.zero;
        int num = 0;
        for (int i = 0; i < 4; i++)
        {
            int cornerIndex = chunk.faceIndexes[faceIndex, i];
            if (!surfaceGroup[cornerIndex]) //Si el punto no se encuentra bajo la superficie
            {
                goal += chunk.cornerTable[cornerIndex];
                num++;
            }
        }
        goal = cubePos + goal / num;
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
            Debug.DrawLine(cube + chunk.cornerTable[chunk.edgeIndexes[i,0]], cube + chunk.cornerTable[chunk.edgeIndexes[i,1]], color, time);
        }
    }

    
    //Función que dado dos cubos y la subSuperficie del primero, devuelve si esa superficie conecta directamente con el segundo cubo
    public static bool DoesSurfaceConnect(Vector3Int pos1, bool[] belowSurfaceValues, Vector3Int pos2)
    {
        Vector3Int dir = pos2 - pos1; //La dir al segundo cubo
        int dirIndex;
        if (!chunk.reverseFaceDirections.TryGetValue(dir, out dirIndex)) return false; //Si no son dayacente falso
        if (FaceXOR(dirIndex, belowSurfaceValues)) return true;
        return false;

    }
}
