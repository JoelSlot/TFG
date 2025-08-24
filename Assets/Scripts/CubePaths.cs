using System.Collections.Generic;
using Utils;
using UnityEngine;
using System;
using System.Linq;
using System.Security.Authentication.ExtendedProtection;
using FluentBehaviourTree;

public class CubePaths : MonoBehaviour
{

    //Diccionario de todas las pheromonas. Son indexadas según su posición, para que una hormiga pueda fácilmente acceder a las pheromonas en los cubos alrededores.
    //Ya que pueden haber pheromonas de caminos distintos en el mismo cubo, y pueden haber múltiples superficies con pheromonas del mismo camino en un mismo cubo, se guarda una lista de todas aquellas que se encuentran en cada cubo
    public static Dictionary<Vector3Int, int> cubePheromones = new();

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }

    public static void PlacePheromone(Vector3Int pos)
    {
        //thought it would be more complicated but... This should update existing entries as well.
        cubePheromones[pos] = 100;
    }

    /*
    Dado dos grupos de valores de esquinas, devuelve si son iguales
    */
    public static bool CompareGroups(bool[] G1, bool[] G2)
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

    public static CubeSurface GetAdyacentSurface(CubeSurface origSurface, int faceIndex)
    {
        Vector3Int dir = chunk.faceIdToDirTable[faceIndex]; //Get dir
        bool[] newCornerValues = CubeCornerValues(origSurface.pos + dir); //Get new cube cornerValues
        Vector3Int newSurfaceCorner = TrueCorner(faceIndex, origSurface.surfaceGroup) - dir; //Get corner value
        bool[] newGroupCornerValues = GetGroup(newSurfaceCorner, newCornerValues);
        return new CubeSurface(origSurface.pos + dir, newGroupCornerValues);
    }

    /*
    Dado una superficie de un cubo, devuelve los cubos adyacentes que conectan con esa superficie.

    surface: superficie de la que se buscan los adyacentes

    forwardDir: Dirección en la que está mirando la hormiga.

    return: Lista de superficies adyacentes, ordenados en el que está más hacia la dirección dada hasta la que menos.
    */
    public static List<CubeSurface> GetAdyacentSurfaces(CubeSurface surface, Vector3 forwardDir) // cube contains the cube pos and one of the points below
    {
        List<CubeSurface> adyacentCubes = new List<CubeSurface>();

        List<int> index = new List<int> { 0, 1, 2, 3, 4, 5 };

        index.Sort((x, y) => (int)(Vector3.Angle(forwardDir, chunk.faceIdToDirTable[x]) - Vector3.Angle(forwardDir, chunk.faceIdToDirTable[y])));

        for (int i = 0; i < 6; i++)
        {
            if (FaceXOR(index[i], surface.surfaceGroup))
            {
                adyacentCubes.Add(GetAdyacentSurface(surface, index[i]));
            }
        }

        return adyacentCubes;
    }

    public static List<CubeSurface> GetAdyacentSurfaces(CubeSurface surface) // cube contains the cube pos and one of the points below
    {
        List<CubeSurface> adyacentCubes = new List<CubeSurface>();

        for (int i = 0; i < 6; i++)
        {
            if (FaceXOR(i, surface.surfaceGroup))
            {
                adyacentCubes.Add(GetAdyacentSurface(surface, i));
            }
        }

        return adyacentCubes;
    }

    /*
    Dado la posición de un cubo, devuelve para todas las esquinas si esa esquina se encuentra debajo del terreno o no.

    cube: posición del cubo que se quiere mirar.

    return: array de 8 bools, uno por cada esquina en orden según el cornerTable de la clase Chunk. Si un bool es true, su esquina se encuentra sobre del terreno y viceversa.
    */
    public static bool[] CubeCornerValues(Vector3Int cube)
    {
        bool[] cornerValues = new bool[8];
        for (int i = 0; i < 8; i++)
            cornerValues[i] = WorldGen.IsAboveSurface(cube + chunk.cornerIdToPos[i]);
        return cornerValues;
    }

    /*
    TrueCorner sirve para devolver una de las esquinas debajo de la superficie que comparten dos cubos. Dado la cara del cubo principal que conecta con el otro cubo, y los valores de las esquinas del cubo principal, el primer valor sobre la superficie de la cara indicada es devuelto.

    faceIndex: El índice de la cara que conecta el cubo 1 con el cubo 2.
    cornerValues: Los valores de las esquinas del cubo 1 respecto a la superficie que conecta los dos cubos.

    return: una de las esquinas de la cara dada que esté sobre la superficie
    */
    public static Vector3Int TrueCorner(int faceIndex, bool[] cornerValues)
    {
        if (cornerValues[chunk.faceIdToCornerId[faceIndex, 0]]) return chunk.cornerIdToPos[chunk.faceIdToCornerId[faceIndex, 0]];
        if (cornerValues[chunk.faceIdToCornerId[faceIndex, 1]]) return chunk.cornerIdToPos[chunk.faceIdToCornerId[faceIndex, 1]];
        if (cornerValues[chunk.faceIdToCornerId[faceIndex, 2]]) return chunk.cornerIdToPos[chunk.faceIdToCornerId[faceIndex, 2]];
        return chunk.cornerIdToPos[chunk.faceIdToCornerId[faceIndex, 3]];
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
        /*Debug.Log(
            chunk.faceIndexes[faceIndex, 0] + ": " + cornerValues[chunk.faceIndexes[faceIndex, 0]] + ", " +
            chunk.faceIndexes[faceIndex, 1] + ": " + cornerValues[chunk.faceIndexes[faceIndex, 1]] + ", " +
            chunk.faceIndexes[faceIndex, 2] + ": " + cornerValues[chunk.faceIndexes[faceIndex, 2]] + ", " +
            chunk.faceIndexes[faceIndex, 3] + ": " + cornerValues[chunk.faceIndexes[faceIndex, 3]] + ", " );*/
        return !(
            cornerValues[chunk.faceIdToCornerId[faceIndex, 0]] == cornerValues[chunk.faceIdToCornerId[faceIndex, 1]] &&
            cornerValues[chunk.faceIdToCornerId[faceIndex, 0]] == cornerValues[chunk.faceIdToCornerId[faceIndex, 2]] &&
            cornerValues[chunk.faceIdToCornerId[faceIndex, 0]] == cornerValues[chunk.faceIdToCornerId[faceIndex, 3]]);
    }

    /*
    Agrupa los puntos según si están conectados todos. Se le pasan los valores de cubo según si están debajo o encima del terreno y uno de las esquinas sobre la superficie que se quiere identificar.
    Devuelve un array de bools donde son true los que se encuentran sobre la superficie dada, y falso los que no.

    antCorner: Uno de los puntos sobre la superficie.
    cornerValues: array de bool con los valores de las esquinas del cubo. True si sobre el terreno, false si no.

    return: array de bool con los valores de las esquinas del cubo. True si sobre la superficie dada, false si no.
    */
    public static bool[] GetGroup(Vector3Int antCorner, bool[] cornerValues)
    {
        bool[] group = new bool[8]; //Valor defecto de bool es falso (CONFIRMED)
        group[chunk.cornerPosToId[antCorner]] = true; //BIG ISSUE BY FORGETTING THIS
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
                    if (cornerValues[chunk.cornerPosToId[adyCorner]]) // si está sobre el suelo
                    {
                        group[chunk.cornerPosToId[adyCorner]] = true; // Añadimos la esquina al grupo
                        cornersToCheck.Enqueue(adyCorner); // Y lo preparamos para mirar sus adyacentes
                    }
                }
            }
        }
        return group;
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
    CornerFromNormal devuelve la esquina más sobre la superficie dado la normal de la superficie. Se consigue cogiendo la esquina cuya dirección hacia el centro del cubo se parezca más a la inversa de la normal.

    normal: Vector3 dirección de la normal de la superficie.

    return: Vector3Int coordenada de una de las esquinas sobre la superficie.
    */
    public static Vector3Int CornerFromNormal(Vector3 normal)
    {
        Vector3Int returnCorner = Vector3Int.zero;
        float minAngle = 180;
        foreach (Vector3Int corner in chunk.cornerIdToPos)
        {
            float angle = Vector3.Angle(-normal, CornerNormal(corner));
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

    //Dado la superficie actual y la dir en la que se quiere mover devuelve el punto a seguir para llegar
    public static Vector3 GetMovementGoal(CubeSurface surface, Vector3Int dir)
    {
        //Si la dirección es inválida, ponemos el gol sobre el centro del cubo actual más la dirección por 400. Deberia no quedarse justo encima ni debajo de la hormiga con esa distancia.
        if (!chunk.dirToFaceIdTable.ContainsKey(dir))
        {
            //Debug.Log("Fucked up dir-------------------------------" + dir.x + ", " + dir.y + ", " + dir.z);
            return surface.pos + Vector3.one / 2 + dir * 400;
        }

        DrawSurface(surface, Color.black, 1);
        DrawCube(surface.pos + dir, Color.red, 1);

        Vector3 goal = surface.pos + Vector3.one * 0.5f; //poner gol en centro del cubo actual de la hormiga

        Vector3Int reverseDir = dir * -1;
        int numBelowSurfReverse = 0;
        for (int i = 0; i < 4; i++)
        {
            int cornerId = chunk.faceIdToCornerId[chunk.dirToFaceIdTable[reverseDir], i];
            if (surface.surfaceGroup[cornerId])
                numBelowSurfReverse++;
        }

        int countedCorners = 0;
        Vector3 medianDir = dir;
        for (int i = 0; i < 4; i++)
        {
            int cornerId = chunk.faceIdToCornerId[chunk.dirToFaceIdTable[dir], i];
            if ((numBelowSurfReverse > 2 && surface.surfaceGroup[cornerId]) || (numBelowSurfReverse < 2 && !surface.surfaceGroup[cornerId]))
            {
                medianDir += chunk.cornerIdToPos[cornerId] - Vector3.one * 0.5f;
                countedCorners++;
            }
        }

        medianDir /= countedCorners + 1;

        //Debug.DrawRay(goal, medianDir * 400, Color.blue);
        return goal + medianDir * 400;
    }

    //Dado la superficie actual y las dos siguientes direcciones, devuelve el punto a seguir
    public static Vector3 GetMovementGoal(CubeSurface surface, Vector3Int dir1, Vector3Int dir2)
    {

        //Si la dirección es inválida, ponemos el gol sobre el centro del cubo actual más la dirección por 400. Deberia no quedarse justo encima ni debajo de la hormiga con esa distancia.
        if (!chunk.dirToFaceIdTable.TryGetValue(dir1, out int faceIndex))
        {
            //Debug.Log("Fucked up dir-------------------------------" + dir1.x + ", " + dir1.y + ", " + dir1.z);
            return surface.pos + Vector3.one / 2 + dir1 * 400;
        }

        DrawSurface(surface, Color.black, 1);
        DrawCube(surface.pos + dir1, Color.red, 1);
        DrawCube(surface.pos + dir1 + dir2, Color.green, 1);

        Vector3 center = surface.pos + Vector3.one * 0.5f; //poner gol en centro del cubo actual de la hormiga
        Vector3 localCenter = Vector3.one * 0.5f;

        Vector3Int reverseDir = dir1 * -1;
        int numBelowSurfReverse = 0;
        for (int i = 0; i < 4; i++)
        {
            int cornerId = chunk.faceIdToCornerId[chunk.dirToFaceIdTable[reverseDir], i];
            if (surface.surfaceGroup[cornerId])
                numBelowSurfReverse++;
        }

        int countedCorners = 0;
        Vector3 medianDir = dir1;
        Vector3 backFaceToSurfaceDir = Vector3.zero;
        for (int i = 0; i < 4; i++)
        {
            int cornerId = chunk.faceIdToCornerId[chunk.dirToFaceIdTable[dir1], i];
            if ((numBelowSurfReverse > 2 && surface.surfaceGroup[cornerId]) || (numBelowSurfReverse < 2 && !surface.surfaceGroup[cornerId]))
            {
                medianDir += chunk.cornerIdToPos[cornerId] - localCenter;
                countedCorners++;
            }
            if (numBelowSurfReverse == 2 && surface.surfaceGroup[cornerId])
            {
                backFaceToSurfaceDir += chunk.cornerIdToPos[cornerId] - (Vector3)reverseDir * 0.5f;
            }
        }

        medianDir /= countedCorners + 1;

        //Debug.DrawRay(center, medianDir * 400, Color.blue);
        Vector3 goal1 = center + medianDir * 400;

        //Si la segunda dir no es válida o las dos direcciones son iguales
        if (!chunk.dirToFaceIdTable.ContainsKey(dir2) || dir1 == dir2 || dir1 == dir2 * -1)
        {
            if (!chunk.dirToFaceIdTable.ContainsKey(dir2))
                //Debug.Log("Fucked up dir2-------------------------------" + dir2.x + ", " + dir2.y + ", " + dir2.z);
                return goal1;
        }

        //Mirar si la segunda direccion se opone a la direccion del primer gol
        if (Math.Sign(dir2.x) * Math.Sign(medianDir.x) == -1)
        {
            //Debug.Log("Using 1 dir after normal check x: " + Math.Sign(dir2.x) + " vs " + Math.Sign(medianDir.x));
            return goal1;
        }
        if (Math.Sign(dir2.y) * Math.Sign(medianDir.y) == -1)
        {
            //Debug.Log("Using 1 dir after normal check y: " + Math.Sign(dir2.y) + " vs " + Math.Sign(medianDir.y));
            return goal1;
        }
        if (Math.Sign(dir2.z) * Math.Sign(medianDir.z) == -1)
        {
            //Debug.Log("Using 1 dir after normal check z: " + Math.Sign(dir2.z) + " vs " + Math.Sign(medianDir.z));
            return goal1;
        }

        //Debug.Log("COmparing backSurfaceDir to dir2. BacksurfaceDir = " + backFaceToSurfaceDir.x + ", " + backFaceToSurfaceDir.y + ", " + backFaceToSurfaceDir.z);
        //Debug.Log("dir2: " + dir2.x + ", " + dir2.y + ", " + dir2.z);

        if (numBelowSurfReverse == 2)
        {
            //Mirar si la segunda direccion se opone a la direccion del primer gol
            if (Math.Sign(dir2.x) * Math.Sign(backFaceToSurfaceDir.x) != 0)
            {
                //Debug.Log("Using 1 dir after backface check x: " + Math.Sign(dir2.x));
                return goal1;
            }
            if (Math.Sign(dir2.y) * Math.Sign(backFaceToSurfaceDir.y) != 0)
            {
                //Debug.Log("Using 1 dir after backface check y: " + Math.Sign(dir2.y));
                return goal1;
            }
            if (Math.Sign(dir2.z) * Math.Sign(backFaceToSurfaceDir.z) != 0)
            {
                //Debug.Log("Using 1 dir after backface check z: " + Math.Sign(dir2.z));
                return goal1;
            }
        }

        DrawCube(surface.pos + dir1 + dir2, Color.blue, 1);

        //Debug.DrawRay(center, (medianDir + dir2) * 400, Color.yellow);
        return center + (medianDir + dir2) * 400;
    }


    //Función que dado dos cubos y la subSuperficie del primero, devuelve si esa superficie conecta directamente con el segundo cubo
    public static bool DoesSurfaceConnect(CubeSurface surface1, Vector3Int cube2)
    {
        Vector3Int dir = cube2 - surface1.pos; //La dir al segundo cubo //This was reversed, so it caused problems
        //Debug.Log("pos: " + cube2 + " - " + surface1.pos);
        if (!chunk.dirToFaceIdTable.TryGetValue(dir, out int dirIndex))
        {
            //Debug.Log("Not adyacent");
            return false; //Si no son dayacente falso
        }
        //DrawFace(surface1.pos, dirIndex, Color.black, 10000);
        if (!FaceXOR(dirIndex, surface1.surfaceGroup))
        {
            //Debug.Log("FaceXor negative");
            return false;
        }
        return true;
    }


    public struct CubeSurface
    {
        public bool[] surfaceGroup;
        public Vector3Int pos;
        public CubeSurface(Vector3Int newPos, Vector3Int aboveSurfacePoint)
        {
            surfaceGroup = GetGroup(aboveSurfacePoint, CubeCornerValues(newPos));
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

        public CubeSurface(GameData.SurfaceInfo info)
        {
            pos = info.pos.ToVector3Int();
            surfaceGroup = GameData.ConvertByteToBoolArray(info.group);
        }

        public override bool Equals(object obj)
        {
            if (!(obj is CubeSurface))
                return false;

            CubeSurface mys = (CubeSurface)obj;
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

        public int Count()
        {
            int count = 0;
            for (int i = 0; i < 8; i++)
                if (surfaceGroup[i]) count++;

            return count;
        }

        public bool IsNull() //when you create a new surface without arguments, all it's groupvalues are false. this checks if its the case
        {
            foreach (var value in surfaceGroup)
                if (value)
                    return false;
            return true;
        }
    }

    public static float DistToPoint(Vector3 pos, Vector3 point)
    {
        return (point - pos).magnitude; //1.2f
    }

    public static bool PointIsInCube(Vector3Int pos, Vector3 point)
    {
        if (point.x < pos.x || point.x > pos.x + 1) return false;
        if (point.y < pos.y || point.y > pos.y + 1) return false;
        if (point.z < pos.z || point.z > pos.z + 1) return false;
        return true;
    }


    public static bool GetPathToPoint(CubeSurface start, Vector3 objective, int lengthLimit, out List<CubeSurface> path)
    { ///UHHH NO PATHW EHN GO TO FOOD SO LIKE DRAW CUBES WHEN MAKING FINDING PATH SOU KNWO WHAT IS HAPPENIGN-
        path = new List<CubeSurface>();

        PriorityQueue<CubeSurface, float> frontera = new();
        frontera.Enqueue(start, 0);
        Dictionary<CubeSurface, CubeSurface> previo = new();
        Dictionary<CubeSurface, float> coste = new();
        Dictionary<CubeSurface, int> longitud = new();

        previo[start] = start;
        coste[start] = 0;
        longitud[start] = 0;


        float heuristic(Vector3 point, Vector3 objective)
        {
            return Mathf.Abs(point.x - objective.x) + Mathf.Abs(point.z - objective.z) + Mathf.Abs(point.z - objective.z);
        }

        bool gotToPoint = false;

        CubeSurface reachedSurface = start;

        while (frontera.Count > 0)
        {
            CubeSurface current = frontera.Dequeue();

            if (current.pos == objective)
            {
                reachedSurface = current;
                break;
            }

            if (DistToPoint(current.pos + Vector3.one * 0.5f, objective) < 1.2f)
            {
                reachedSurface = current;
                gotToPoint = true;
                break;
            }

            List<CubeSurface> adyacentSurfaces = GetAdyacentSurfaces(current);

            foreach (var son in adyacentSurfaces)
            {
                float newCost = coste[current] + 1;
                int newLength = longitud[current] + 1;
                //if (!CompareGroups(son.surfaceGroup, current.surfaceGroup)) newCost += 1;
                bool updateOrInsert = false;
                if (!coste.TryGetValue(son, out float prevCost)) updateOrInsert = true;
                else if (newCost < prevCost) updateOrInsert = true;

                if (newLength > lengthLimit) updateOrInsert = false;


                if (updateOrInsert)
                {
                    //DrawCube(son.pos, Color.black, 5);
                    coste[son] = newCost;
                    longitud[son] = newLength;
                    float prioridad = newCost + heuristic(son.pos, objective);
                    frontera.Enqueue(son, prioridad);
                    previo[son] = current;
                }
            }
        }



        while (!reachedSurface.Equals(start))
        {
            //DrawCube(reachedSurface.pos, Color.blue, 40);
            //DrawSurface(reachedSurface, Color.black, 40);
            path.Insert(0, reachedSurface); //DONT USE APPEND EVER AGAIN YOU STUPID FUCING 
            reachedSurface = previo[reachedSurface];
        }
        //DrawCube(reachedSurface.pos, Color.blue, 40);

        //Debug.Log("found path length: " + path.Count);
        return gotToPoint;
    }


    public static bool GetKnownPathToPoint(CubeSurface start, Vector3 objective, float acceptableDistance, out List<CubeSurface> path)
    { ///UHHH NO PATHW EHN GO TO FOOD SO LIKE DRAW CUBES WHEN MAKING FINDING PATH SOU KNWO WHAT IS HAPPENIGN-
        path = new List<CubeSurface>();

        PriorityQueue<Tuple<CubeSurface, int>, float> frontera = new();
        frontera.Enqueue(new(start, 0), 0);
        Dictionary<CubeSurface, CubeSurface> previo = new();
        Dictionary<CubeSurface, float> coste = new();
        Dictionary<CubeSurface, int> longitud = new();

        previo[start] = start;
        coste[start] = 0;
        longitud[start] = 0;


        float heuristic(Vector3 point, Vector3 objective)
        {
            return Mathf.Abs(point.x - objective.x) + Mathf.Abs(point.z - objective.z) + Mathf.Abs(point.z - objective.z);
        }

        bool gotToPoint = false;

        CubeSurface reachedSurface = start;

        while (frontera.Count > 0)
        {
            (CubeSurface current, int range) = frontera.Dequeue();

            if (current.pos == objective)
            {
                reachedSurface = current;
                gotToPoint = true;
                break;
            }

            if (DistToPoint(current.pos + Vector3.one * 0.5f, objective) < acceptableDistance)
            {
                reachedSurface = current;
                gotToPoint = true;
                break;
            }

            List<CubeSurface> adyacentSurfaces = GetAdyacentSurfaces(current);
            range++;

            foreach (var son in adyacentSurfaces)
            {
                float newCost = coste[current] + 1;
                int newLength = longitud[current] + 1;
                //if (!CompareGroups(son.surfaceGroup, current.surfaceGroup)) newCost += 1;
                bool updateOrInsert = false;
                if (!coste.TryGetValue(son, out float prevCost)) updateOrInsert = true;
                else if (newCost < prevCost) updateOrInsert = true;

                if (cubePheromones.ContainsKey(current.pos) || Nest.SurfaceInNest(current))
                    range = 0;

                if (range > 1)
                    updateOrInsert = false;

                if (updateOrInsert)
                {
                    //DrawCube(son.pos, Color.black, 5);
                    coste[son] = newCost;
                    longitud[son] = newLength;
                    float prioridad = newCost + heuristic(son.pos, objective);
                    frontera.Enqueue(new(son, range), prioridad);
                    previo[son] = current;
                }
            }
        }

        if (!gotToPoint) return false;

        while (!reachedSurface.Equals(start))
        {
            //DrawCube(reachedSurface.pos, Color.blue, 40);
            //DrawSurface(reachedSurface, Color.black, 40);
            path.Insert(0, reachedSurface); //DONT USE APPEND EVER AGAIN YOU STUPID FUCING 
            reachedSurface = previo[reachedSurface];
        }
        //DrawCube(reachedSurface.pos, Color.blue, 40);

        //Debug.Log("found path length: " + path.Count);
        return gotToPoint;
    }


    public static bool GetPathToSurface(CubeSurface start, CubeSurface objective, int lengthLimit, out List<CubeSurface> path)
    {
        //Debug.Log("Finding path...");
        path = new List<CubeSurface>();

        PriorityQueue<CubeSurface, float> frontera = new();
        frontera.Enqueue(start, 0);
        Dictionary<CubeSurface, CubeSurface> previo = new();
        Dictionary<CubeSurface, float> coste = new();
        Dictionary<CubeSurface, int> longitud = new();

        previo[start] = start;
        coste[start] = 0;
        longitud[start] = 0;


        float heuristic(Vector3Int point, Vector3Int objective)
        {
            return Mathf.Abs(point.x - objective.x) + Mathf.Abs(point.z - objective.z) + Mathf.Abs(point.z - objective.z);
        }

        bool pathExists = false;

        CubeSurface reachedSurface = start;

        while (frontera.Count > 0)
        {
            CubeSurface current = frontera.Dequeue();

            if (current.Equals(objective))
            {
                reachedSurface = current;
                pathExists = true;
                break;
            }

            List<CubeSurface> adyacentSurfaces = GetAdyacentSurfaces(current);

            foreach (var son in adyacentSurfaces)
            {
                float newCost = coste[current] + 1;
                int newLength = longitud[current] + 1;
                //if (!CompareGroups(son.surfaceGroup, current.surfaceGroup)) newCost += 1;
                bool updateOrInsert = false;
                if (!coste.TryGetValue(son, out float prevCost)) updateOrInsert = true;
                else if (newCost < prevCost) updateOrInsert = true;

                if (newLength > lengthLimit) updateOrInsert = false;


                if (updateOrInsert)
                {
                    //DrawCube(son.pos, Color.black, 5);
                    coste[son] = newCost;
                    longitud[son] = newLength;
                    float prioridad = newCost + heuristic(son.pos, objective.pos);
                    frontera.Enqueue(son, prioridad);
                    previo[son] = current;
                }
            }
        }

        while (!reachedSurface.Equals(start))
        {
            //DrawCube(reachedSurface.pos, Color.blue, 4);
            //DrawSurface(reachedSurface, Color.black, 40);
            path.Insert(0, reachedSurface); //DONT USE APPEND EVER AGAIN YOU STUPID FUCING IDIOT
            reachedSurface = previo[reachedSurface];
        }

        //Debug.Log("found path length: " + path.Count);
        //if (!pathExists) Debug.Log("Not found!");
        return pathExists;
    }


    public static bool GetKnownPathToMapPart(CubeSurface start, NestPart.NestPartType type, out List<CubeSurface> path)
    {
        //Debug.Log("Finding path...");
        path = new List<CubeSurface>();

        PriorityQueue<Tuple<CubeSurface, int>, float> frontera = new();
        frontera.Enqueue(new(start, 0), 0);
        Dictionary<CubeSurface, CubeSurface> previo = new();
        Dictionary<CubeSurface, float> coste = new();

        previo[start] = start;
        coste[start] = 0;

        bool pathExists = false;

        CubeSurface reachedMapPart = start;

        while (frontera.Count > 0)
        {
            (CubeSurface current, int range) = frontera.Dequeue();

            if (Nest.SurfaceInNest(current) || cubePheromones.ContainsKey(current.pos)) //Si es dentro del nido o feromonas rango es 0
                range = 0;

            if (Nest.SurfaceInNestPart(current, type) && (type != NestPart.NestPartType.Outside || range > 0))
            {
                reachedMapPart = current;
                pathExists = true;
                break;
            }

            HashSet<CubeSurface> identifiableSurfaces = new();


            if (range < 2)
            {
                List<CubeSurface> adySurfaces = GetAdyacentSurfaces(current);
                foreach (var son in adySurfaces) identifiableSurfaces.Add(son);
                range++;
            }

            foreach (var son in identifiableSurfaces)
            {
                float newCost = coste[current] + 1;
                bool updateOrInsert = false;
                if (!coste.TryGetValue(son, out float prevCost)) updateOrInsert = true;
                else if (newCost < prevCost) updateOrInsert = true;

                if (updateOrInsert)
                {
                    //DrawCube(son.pos, Color.black, 5);
                    coste[son] = newCost;
                    float prioridad = newCost;
                    frontera.Enqueue(new(son, range), prioridad);
                    previo[son] = current;
                }
            }
        }

        if (!pathExists) return false;

        while (!reachedMapPart.Equals(start))
        {
            //DrawCube(reachedMapPart.pos, Color.blue, 4);
            //DrawSurface(reachedMapPart, Color.black, 40);
            path.Insert(0, reachedMapPart);
            reachedMapPart = previo[reachedMapPart];
        }
        path.Insert(0, reachedMapPart);

        //Debug.Log("found path length: " + path.Count);
        //if (!pathExists) Debug.Log("Not found!");
        return pathExists;
    }

    public static bool GetExplorePath(CubeSurface antSurface, Vector3 antForward, out List<CubeSurface> path)
    {
        List<CubeSurface> sensedRange = new();
        Dictionary<CubeSurface, CubeSurface> checkedSurfaces = new();
        path = new();
        HashSet<Vector3Int> nearbyPheromones = new();

        //Debug.Log("Looking for surface");

        int range = UnityEngine.Random.Range(4, 10);

        for (int r = 0; r < range; r++)
        {
            sensedRange = GetNextSurfaceRange(antSurface, antForward, sensedRange, ref checkedSurfaces); //Initial one.
            if (r < 8) foreach (var surface in sensedRange) if (cubePheromones.ContainsKey(surface.pos)) nearbyPheromones.Add(surface.pos);
            if (sensedRange.Count == 0)
                return false;
        }


        //We check the two ranges beyond to get any pheromones to influence what dir we are going
        List<CubeSurface> beyondRange = GetNextSurfaceRange(antSurface, antForward, sensedRange, ref checkedSurfaces); //fourth.
        foreach (var surface in beyondRange) if (cubePheromones.ContainsKey(surface.pos)) nearbyPheromones.Add(surface.pos);
        beyondRange = GetNextSurfaceRange(antSurface, antForward, beyondRange, ref checkedSurfaces);
        foreach (var surface in beyondRange) if (cubePheromones.ContainsKey(surface.pos)) nearbyPheromones.Add(surface.pos);


        //Conseguir media de todos las feromonas cercanas.
        int i = 0;
        Vector3 medium = Vector3.zero;
        foreach (var pos in nearbyPheromones)
        {
            i++;
            medium += pos;
        }
        if (i != 0) medium /= i;


        //Escoger el más lejano a las feromonas
        float maxScore = float.MinValue;
        CubeSurface chosen = sensedRange[0];
        List<CubeSurface> candidates = new();
        foreach (CubeSurface potentialEnd in sensedRange)
        {
            float score = unexploredScore(potentialEnd, medium, nearbyPheromones);
            if (score > maxScore)
            {
                candidates = new()
                {
                    potentialEnd
                };
                maxScore = score;
            }
            else if (score == maxScore)
            {
                candidates.Add(potentialEnd);
            }
        }
        int randIndex = UnityEngine.Random.Range(0, candidates.Count - 1);
        chosen = candidates[randIndex];

        path = new();

        //convertir a un path
        while (!chosen.Equals(antSurface))
        {
            //DrawCube(chosen.pos, Color.green, 4);
            //DrawSurface(chosen, Color.black, 4);
            path.Insert(0, chosen); //DONT USE APPEND EVER AGAIN YOU STUPID FUCING IDIOT
            chosen = checkedSurfaces[chosen];
        }

        return true;

    }


    public static bool GetLostPath(CubeSurface antSurface, Vector3 antForward, out List<CubeSurface> path)
    {
        List<CubeSurface> sensedRange = new();
        Dictionary<CubeSurface, CubeSurface> checkedSurfaces = new();
        path = new();

        //Debug.Log("Looking for surface");

        int range = UnityEngine.Random.Range(3, 6);

        for (int r = 0; r < range; r++)
        {
            sensedRange = GetNextSurfaceRange(antSurface, antForward, sensedRange, ref checkedSurfaces); //Initial one.
            if (sensedRange.Count == 0)
                return false;
        }

        //Conseguir media de todos las feromonas cercanas.
        Vector3 medium = Vector3.zero;

        //ESCOGER SUPERFICIE AL QUE IR
        CubeSurface chosen = sensedRange[0];
        bool foundNest = false;
        //SI alguno se encuentra dentro del nido, jackpot.
        // Incluimos todos los checked. Digamos que el interior del nido es como una feromona, por tanto el rango.
        foreach (var surface in checkedSurfaces.Keys)
        {
            if (Nest.SurfaceInNest(surface))
            {
                chosen = surface;
                foundNest = true;
            }
        }
        if (!foundNest)
        {
            int randIndex = UnityEngine.Random.Range(0, sensedRange.Count - 1);
            chosen = sensedRange[randIndex];
        }

        path = new();

        //convertir a un path
        while (!chosen.Equals(antSurface))
        {
            //DrawCube(chosen.pos, Color.green, 4);
            //DrawSurface(chosen, Color.black, 4);
            path.Insert(0, chosen); //DONT USE APPEND EVER AGAIN YOU STUPID FUCING IDIOT
            chosen = checkedSurfaces[chosen];
        }

        return true;

    }

    public static float unexploredScore(CubeSurface surface, Vector3 medium, HashSet<Vector3Int> nearbyPhers)
    {
        //No interesa explorar dentro del nido 
        if (Nest.SurfaceInNest(surface)) return int.MinValue;

        //NO queremos exporar debajo del mapa.
        if (surface.pos.y < 2) return -100;

        float penalty = 0;
        if (nearbyPhers.Contains(surface.pos)) penalty += 5;
        int count = surface.Count();
        if (count == 1 && count == 7) penalty += 2; // evitar cubos con
        if (count == 2 && count == 6) penalty += 1; // poca superficie.
        return Mathf.Abs(medium.x - surface.pos.x) + Mathf.Abs(medium.y - surface.pos.y) + Mathf.Abs(medium.z - surface.pos.z) - penalty;
    }


    public static List<CubeSurface> GetNextSurfaceRange(CubeSurface antSurface, Vector3 antForward, List<CubeSurface> currentRange, ref Dictionary<CubePaths.CubeSurface, CubePaths.CubeSurface> checkedSurfaces)
    {
        List<CubeSurface> nextRange = new();

        //Si el rango está empezando se coge la superficie de la hormiga
        if (currentRange.Count == 0)
        {
            nextRange.Add(antSurface);
            checkedSurfaces.TryAdd(antSurface, antSurface);
            return nextRange;
        }

        //Si el rango es la superficie de la hormiga se cogen los adyacentes (para poner sus firststep)
        if (currentRange[0].Equals(antSurface))
        {
            if (currentRange.Count != 1) Debug.Log("Error: Range includes more than just ant initial position");

            List<CubeSurface> adyacentCubes = GetAdyacentSurfaces(antSurface, antForward);
            foreach (var son in adyacentCubes)
            {
                nextRange.Add(son);
                checkedSurfaces.Add(son, antSurface);
                //CubePaths.DrawCube(son.pos, Color.magenta, 1);
            }
            return nextRange;
        }


        //Si el rango es mayor que todo eso se procede como debido
        foreach (var currentSurface in currentRange)
        {
            List<CubeSurface> adyacentCubes = GetAdyacentSurfaces(currentSurface, antForward);

            foreach (var son in adyacentCubes)
            {
                if (!checkedSurfaces.ContainsKey(son))
                {
                    nextRange.Add(son);
                    checkedSurfaces.Add(son, currentSurface);
                    //CubePaths.DrawCube(son.pos, Color.green, 1);
                }
            }
        }
        return nextRange;
    }



    /*
    Debuja el cubo dado, en el color dado, durando el tiempo dado
    */
    public static void DrawCube(Vector3Int cube, Color color, int time)
    {
        for (int i = 0; i < 12; i++)
        {
            Debug.DrawLine(cube + chunk.cornerIdToPos[chunk.edgeIdToCornerId[i, 0]], cube + chunk.cornerIdToPos[chunk.edgeIdToCornerId[i, 1]], color, time);
        }
    }

    public static void DrawCube(Vector3Int cube, Color color)
    {
        for (int i = 0; i < 12; i++)
        {
            Debug.DrawLine(cube + chunk.cornerIdToPos[chunk.edgeIdToCornerId[i, 0]], cube + chunk.cornerIdToPos[chunk.edgeIdToCornerId[i, 1]], color);
        }
    }


    public static void DrawSurface(CubeSurface cubeSurface, Color color, int time)
    {
        for (int i = 0; i < 8; i++)
        {
            if (!cubeSurface.surfaceGroup[i])
                Debug.DrawLine(cubeSurface.pos + Vector3.one / 2, cubeSurface.pos + chunk.cornerIdToPos[i], color, time);
        }
    }

    public static void DrawFace(Vector3Int pos, int faceId, Color color, int time)
    {
        for (int i = 0; i < 4; i++)
        {
            Debug.DrawLine(pos + chunk.cornerIdToPos[chunk.faceIdToCornerId[faceId, i % 4]], pos + chunk.cornerIdToPos[chunk.faceIdToCornerId[faceId, (i + 1) % 4]], color, time);
        }
    }


    //Sistema de edad de la feromona
    //Cada 3 segundos las feromonas pierden un punto de edad.
    //Si llegan a 0 dejan de existir
    //al tener 100 de base, duran 100 * 3s = 5 min
    float timeCounter = 0;
    void FixedUpdate()
    {
        timeCounter += Time.fixedDeltaTime;
        if (timeCounter >= 3) //Cada tres segundos
        {
            timeCounter = 0;
            List<Vector3Int> keys = new();
            foreach (var (key, value) in cubePheromones)
            {
                keys.Add(key);
            }
            foreach (var key in keys)
            {
                cubePheromones[key] -= 1;
                if (cubePheromones[key] <= 0)
                    cubePheromones.Remove(key);
            }
        }
    }

    //For now only compares under the surface points. If the terrain changed by addition it will not be detected. Should never happen tho.
    public static bool hasSurfaceChanged(CubeSurface original)
    {
        for (int cornerId = 0; cornerId < 8; cornerId++)
        {
            if (!original.surfaceGroup[cornerId])
                if (WorldGen.IsAboveSurface(original.pos + chunk.cornerIdToPos[cornerId]))
                    return true;
        }

        return false;
    }

    //returns true if same surface, changedSurface indicates whether it has changed.
    public static bool CheckSurfaceSimilarity(CubeSurface detected, CubeSurface compared, out bool changedSurface)
    {
        changedSurface = false;
        bool sameSurface = false;
        if (detected.pos != compared.pos) return false;

        for (int cornerId = 0; cornerId < 8; cornerId++)
        {
            if (!detected.surfaceGroup[cornerId] && !compared.surfaceGroup[cornerId]) //point is below boths surfaces. They are/used to be the same
                sameSurface = true;
            else if (detected.surfaceGroup[cornerId] != compared.surfaceGroup[cornerId]) //points differ
                changedSurface = true;
        }

        return sameSurface;
    }

    public static bool IsSmallSingularTriangle(CubeSurface surface, out Vector3Int corner)
    {
        Vector3Int belowCorner = Vector3Int.zero;
        Vector3Int aboveCorner = Vector3Int.zero;
        int aboveCount = 0;
        corner = Vector3Int.zero;

        for (int cornerId = 0; cornerId < 8; cornerId++)
        {
            if (surface.surfaceGroup[cornerId])
            {
                aboveCount++;
                aboveCorner = chunk.cornerIdToPos[cornerId];
            }
            else
                belowCorner = chunk.cornerIdToPos[cornerId];
        }

        if (aboveCount == 7)
        {
            corner = belowCorner;
        }
        else if (aboveCount == 1)
        {
            corner = aboveCorner;
        }
        else return false;

        List<Vector3Int> adyacentCorners = AdyacentCorners(corner);

        corner = corner + surface.pos;
        foreach (var ady in adyacentCorners)
        {
            //check if distances to isolevel make it closer to the corner. If not , the traingle isn't that small
            if (Mathf.Abs(WorldGen.SampleTerrain(ady + surface.pos) - WorldGen.isolevel) <= Mathf.Abs(WorldGen.SampleTerrain(corner) - WorldGen.isolevel))
                return false;
        }


        return true;
    }

    //Pone el 
    public static BehaviourTreeStatus SetGoalFromPath(CubeSurface antSurface, Vector3 antForward, ref Task objective, ref bool needNew, ref Vector3 goal)
    {

        if (objective.path.Count == 0)
        {
            //Debug.Log("Path completed");
            return BehaviourTreeStatus.Success;
        }//Para evitar seguir camino nonexistente.

        if (antSurface.Equals(objective.path.Last()))
        {
            objective.path = new();
            return BehaviourTreeStatus.Success;
        }

        if (!needNew)
                return BehaviourTreeStatus.Running;

        needNew = false;


        //Obtener indice de superficie de hormiga en la lista de pasos. Devuelve -1 si no encima de camino
        int antSurfacePathPos = objective.path.FindIndex(x => x.Equals(antSurface));
        if (antSurfacePathPos != -1)
        {

            if (antSurfacePathPos == objective.path.Count - 1)
            {
                Debug.Log("Reached end of path");
                objective.path = new();
                return BehaviourTreeStatus.Success;
            }//Para mirar si se ha llegado al final

            else if (hasSurfaceChanged(objective.path[antSurfacePathPos + 1]))
            {
                if (Task.RecalculateTaskPath(antSurface, ref objective))
                {
                    needNew = true;
                    return SetGoalFromPath(antSurface, antForward, ref objective, ref needNew, ref goal);
                }
                else return BehaviourTreeStatus.Failure;
            } //check if next position in path has changed in real world.

            else if (antSurfacePathPos == objective.path.Count - 2)
            {
                //Debug.Log("Reached second to last surface of path");

                if (IsSmallSingularTriangle(objective.path.Last(), out Vector3Int corner))
                {
                    objective.path = new();
                    return BehaviourTreeStatus.Success;
                }//If last step is tiny triangle, just finish the damn path rather than get stuck

                Vector3Int dir = objective.path.Last().pos - antSurface.pos;
                goal = GetMovementGoal(antSurface, dir);
                return BehaviourTreeStatus.Running;
            } //check if two away from path

            else
            {
                //Debug.Log("On path, following next two steps");
                Vector3Int dir1 = objective.path[antSurfacePathPos + 1].pos - antSurface.pos;
                Vector3Int dir2 = objective.path[antSurfacePathPos + 2].pos - objective.path[antSurfacePathPos + 1].pos;
                goal = GetMovementGoal(antSurface, dir1, dir2);
                return BehaviourTreeStatus.Running;
            }

        }

        int range = 0;
        Dictionary<CubeSurface, CubeSurface> previousSurfaces = new();
        List<CubeSurface> sensedRange = GetNextSurfaceRange(antSurface, antForward, new(), ref previousSurfaces);
        //IR AUMENTANDO RANGO HASTA QUE RANGO CONTENGA PARTE DEL CAMINO, Y DEVOLVER INDICE DEL BLOQUE ENCONTRADO
        while (antSurfacePathPos == -1 && range < 5)
        {
            range++;
            sensedRange = GetNextSurfaceRange(antSurface, antForward, sensedRange, ref previousSurfaces);

            for (int i = 0; i < objective.path.Count; i += 1)
            {
                foreach (var rangeSurface in sensedRange)
                {
                    if (rangeSurface.Equals(objective.path[i]))
                    {
                        antSurfacePathPos = i;
                        break;
                    }
                }
            }
        }

        if (range < 2) //Si hemos encontrado el camino justo al lado, miramos un rango más
        {
            sensedRange = GetNextSurfaceRange(antSurface, antForward, sensedRange, ref previousSurfaces);
            bool foundBetter = false;
            for (int i = antSurfacePathPos + 1; i < objective.path.Count; i += 1) //Miramos si hay superficie más avanzada que la detectada
            {
                foreach (var rangeSurface in sensedRange)
                {
                    if (rangeSurface.Equals(objective.path[i]))
                    {
                        antSurfacePathPos = i;
                        foundBetter = true;
                        break;
                    }
                }
            }
            if (foundBetter) range++; //If we did find a better one on next range, save that range
            //if (foundBetter) Debug.Log("Found better in next range!");
        }


        //Si no se ha encontrado el camino, hemos fallado
        if (antSurfacePathPos == -1)
        {
            //Debug.Log("not found");
            objective.path = new();
            return BehaviourTreeStatus.Failure;
        }

        CubeSurface found = objective.path[antSurfacePathPos];

        if (range == 1) //Si estamos justo al lado del camino
        {
            if (antSurfacePathPos == objective.path.Count - 1)
            {
                //Debug.Log("Next to last surface of path");
                Vector3Int dir = objective.path.Last().pos - antSurface.pos;

                if (IsSmallSingularTriangle(objective.path.Last(), out Vector3Int corner))
                {
                    objective.path = new();
                    return BehaviourTreeStatus.Success;
                }//If last step is tiny triangle, just finish the damn path rather than get stuck

                goal = GetMovementGoal(antSurface, dir);
                return BehaviourTreeStatus.Running;
            }

            else
            {
                //Debug.Log("Next to a surface of the path");
                Vector3Int dir1 = objective.path[antSurfacePathPos].pos - antSurface.pos;
                Vector3Int dir2 = objective.path[antSurfacePathPos + 1].pos - objective.path[antSurfacePathPos].pos;
                goal = GetMovementGoal(antSurface, dir1, dir2);
                return BehaviourTreeStatus.Running;
            }
        }


        //If not next to path, get next two surfaces to get there
        CubeSurface nextSurface = previousSurfaces[found];
        CubeSurface nextNextSurface = found;
        while (!previousSurfaces[nextSurface].Equals(antSurface))
        {
            nextNextSurface = nextSurface;
            nextSurface = previousSurfaces[nextSurface];
        }

        {
            //Debug.Log("Going to path");
            Vector3Int dir1 = nextSurface.pos - antSurface.pos;
            Vector3Int dir2 = nextNextSurface.pos - nextSurface.pos;
            goal = GetMovementGoal(antSurface, dir1, dir2);
            return BehaviourTreeStatus.Running;
        }

    }

}
