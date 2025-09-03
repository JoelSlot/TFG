using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Common;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using Polenter.Serialization;
using Polenter.Serialization.Core;
using Unity.VisualScripting;
using UnityEngine;

[Serializable]
public class GameData
{

    //Fist i could not serialize cuz static. Then cuz they were fields and the xml would be empty. Finally they are properties.
    //Also they have to be public.

    public byte[] terrainMapStream { get; set; }
    public byte[] terrainMemoryStream { get; set; }
    public int x_dim { get; set; }
    public int y_dim { get; set; }
    public int z_dim { get; set; }

    public int chunk_x_dim { get; set; }
    public int chunk_y_dim { get; set; }
    public int chunk_z_dim { get; set; }


    public bool hasWon { get; set; }
    public float playTime { get; set; }

    public serializableVector3 camera_pos { get; set; }
    public serializableVector3 camera_euler { get; set; }
    public QueenInfo queenInfo { get; set; }
    public HashSet<AntInfo> antInfoDict { get; set; }
    public HashSet<CornInfo> cornInfoDict { get; set; }
    public Dictionary<int, int> cornHeldAntDict { get; set; } //Key is corn index, value is ant index
    public Dictionary<int, int> eggHeldAntDict { get; set; } //Key is egg index, value is holder index
    public HashSet<int> cornLostInNestDict { get; set; }
    public HashSet<int> eggsLostInNestDict { get; set; }
    public HashSet<int> antsBringingQueenFoodDict { get; set; }
    public HashSet<CornCobInfo> cornCobInfoDict { get; set; }
    public Dictionary<Vector3Int, DigPoint.digPointData> digPointDict { get; set; }
    public HashSet<serializableVector3Int> availableDigPointInfoDict { get; set; }
    public HashSet<serializableVector3Int> initializedDigPoints { get; set; }
    public HashSet<NestPartInfo> nestPartInfoDict { get; set; }
    public HashSet<int> knownCornCobs { get; set; }
    public Dictionary<Vector3Int, int> pheromones { get; set; }

    /* hashset gave:
    <Items>
        <Reference id="12" />
        <Reference id="16" />
        <Reference id="18" />
        <Reference id="20" />
        <Reference id="36" />
        <Reference id="42" />
        <Reference id="46" />
        <Reference id="74" />
        <Reference id="76" />
        <Reference id="5" />
      </Items>
    So i guess im using a list for the initializedDigPoints
    with list getting thsi:
    <Collection name="initializedDigPoints">
      <Properties>
        <Simple name="Capacity" value="16" />
      </Properties>
      <Items>
        <Reference id="12" />
        <Reference id="16" />
        <Reference id="18" />
        <Reference id="20" />
        <Reference id="22" />
        <Reference id="36" />
        <Reference id="40" />
        <Reference id="42" />
        <Reference id="44" />
        <Reference id="46" />
        <Reference id="82" />
        <Reference id="5" />
      </Items>
    </Collection>
    So no luck

    throws InvalidOperationException: Property type is not defined. Property: "" execption

    Apparenlty vector3 does not return name.
    So solution? create another serializable class... despite Vector3Int working in other places

    now using serializableVector3Int it works!
     */
    public void SaveAnts()
    {
        antInfoDict = new();
        eggHeldAntDict = new();
        foreach (var (id, ant) in Ant.antDictionary)
        {
            antInfoDict.Add(AntInfo.ToData(ant));
            int holderAntIndex = ant.holderAntIndex();
            if (holderAntIndex != -1)
            {
                eggHeldAntDict.Add(id, holderAntIndex);
            }
        }
        //Debug.Log("Num ants: " + antInfoDict.Count);
    }

    public void SaveQueens()
    {
        queenInfo = QueenInfo.ToData(AntQueen.Queen);
    }

    //Save all corn to the dictionary, and add links between held coins and the ants holding them
    public void SaveCorn()
    {
        cornInfoDict = new();
        cornHeldAntDict = new();
        foreach (var (id, corn) in Corn.cornDictionary)
        {
            cornInfoDict.Add(CornInfo.ToData(corn));
            int holderAntIndex = corn.holderAntIndex();
            if (holderAntIndex != -1)
            {
                cornHeldAntDict.Add(id, holderAntIndex);
            }
        }
    }

    public void SaveCornCobs()
    {
        cornCobInfoDict = new();
        foreach (var (id, cornCob) in CornCob.cornCobDictionary)
        {
            cornCobInfoDict.Add(CornCobInfo.ToData(cornCob));
        }
    }

    public void SaveDigPoints()
    {
        digPointDict = DigPoint.digPointDict;

        availableDigPointInfoDict = new();
        foreach (var pos in DigPoint.availableDigPoints)
        {
            availableDigPointInfoDict.Add(new serializableVector3Int(pos));
        }

        initializedDigPoints = new();
        foreach (var (id, digPointData) in digPointDict)
        {
            if (digPointData.digPoint != null)
            {
                initializedDigPoints.Add(new serializableVector3Int(id));
            }
        }
    }

    public void SaveNestParts()
    {
        knownCornCobs = Nest.KnownCornCobs;
        nestPartInfoDict = new();
        foreach (var nestPart in Nest.NestParts)
        {
            nestPartInfoDict.Add(NestPartInfo.ToData(nestPart));
        }
        Debug.Log("Num nestParts: " + nestPartInfoDict.Count);
        cornLostInNestDict = Nest.lostPips;
        eggsLostInNestDict = Nest.lostEggs;
        antsBringingQueenFoodDict = Nest.antsBringingQueenFood;

    }

    public void SavePhermones()
    {
        pheromones = CubePaths.cubePheromones;
    }

    [Serializable]
    public class serializableVector3Int
    {
        public int x { get; set; }
        public int y { get; set; }
        public int z { get; set; }

        public serializableVector3Int(Vector3Int original)
        {
            x = original.x;
            y = original.y;
            z = original.z;
        }

        private serializableVector3Int() { }

        public Vector3Int ToVector3Int()
        {
            return new Vector3Int(x, y, z);
        }
    }

    [Serializable]
    public class serializableVector3
    {
        public float x { get; set; }
        public float y { get; set; }
        public float z { get; set; }

        public serializableVector3(Vector3 original)
        {
            x = original.x;
            y = original.y;
            z = original.z;
        }

        private serializableVector3() { }

        public Vector3 ToVector3()
        {
            return new Vector3(x, y, z);
        }
    }

    [Serializable]
    public class serializableQuaternion
    {
        public float x { get; set; }
        public float y { get; set; }
        public float z { get; set; }
        public float w { get; set; }

        public serializableQuaternion(Quaternion original)
        {
            x = original.x;
            y = original.y;
            z = original.z;
            w = original.w;
        }

        private serializableQuaternion() { }

        public Quaternion ToQuaternion()
        {
            return new Quaternion(x, y, z, w);
        }
    }

    [Serializable]
    public class CornInfo
    {
        public int id { get; set; }
        public int antId { get; set; }
        public serializableVector3 pos { get; set; }
        public serializableQuaternion orientation { get; set; }


        private CornInfo()
        {

        }

        public static CornInfo ToData(Corn corn)
        {
            CornInfo info = new();
            info.id = corn.id;
            info.antId = corn.antId;
            info.pos = new(corn.transform.position);
            info.orientation = new(corn.transform.rotation);
            return info;
        }
    }

    [Serializable]
    public class CornCobInfo
    {
        public int id { get; set; }
        public serializableVector3 pos { get; set; }
        public serializableQuaternion orientation { get; set; }
        public Dictionary<int, int> cornCobCornDict { get; set; }

        private CornCobInfo()
        {

        }

        public static CornCobInfo ToData(CornCob cornCob)
        {
            CornCobInfo info = new();
            info.id = cornCob.id;
            info.pos = new(cornCob.transform.position);
            info.orientation = new(cornCob.transform.rotation);
            info.cornCobCornDict = cornCob.cornCobCornDict;
            return info;
        }
    }

    [Serializable]
    public class AntInfo
    {
        public int id { get; set; }
        public int antId { get; set; }
        public int age { get; set; }
        public TaskInfo objective { get; set; } //This was task, but since it didnt serialize the task's enum and shit properly
        public bool isControlled { get; set; }
        public serializableVector3 pos { get; set; }
        public serializableQuaternion orientation { get; set; }
        public bool isHolding { get; set; }
        public int Counter { get; set; }
        public HashSet<int> discoveredCobs { get; set; }

        private AntInfo()
        {

        }

        public static AntInfo ToData(Ant ant)
        {
            AntInfo info = new();
            info.id = ant.id;
            info.antId = ant.antId;
            info.age = ant.age;
            info.objective = TaskInfo.ToData(ant.objective);
            info.isControlled = ant.IsControlled;
            info.pos = new(ant.transform.position);
            info.orientation = new(ant.transform.rotation);
            info.Counter = ant.Counter;
            info.discoveredCobs = ant.discoveredCobs;

            info.isHolding = ant.IsHolding();

            return info;
        }

    }

    [Serializable]
    public class QueenInfo
    {
        public TaskInfo objective { get; set; } //This was task, but since it didnt serialize the task's enum and shit properly
        public serializableVector3 pos { get; set; }
        public serializableQuaternion orientation { get; set; }
        public int Counter { get; set; }
        public int Energy { get; set; }

        private QueenInfo()
        {

        }

        public static QueenInfo ToData(AntQueen queen)
        {
            QueenInfo info = new();
            info.objective = TaskInfo.ToData(queen.objective);
            info.pos = new(queen.transform.position);
            info.orientation = new(queen.transform.rotation);
            info.Counter = queen.Counter;
            info.Energy = queen.Energy;

            return info;
        }

    }

    [Serializable]
    public class TaskInfo
    {
        public Vector3Int digPointId { get; set; }
        public int foodId { get; set; }
        public serializableVector3 pos { get; set; }
        public int typeIndex { get; set; }
        public List<SurfaceInfo> path { get; set; }



        private TaskInfo()
        {

        }

        public static TaskInfo ToData(Task task)
        {
            TaskInfo info = new();
            info.digPointId = task.digPointId;
            info.foodId = task.itemId;
            info.pos = new(task.pos);
            info.typeIndex = Task.TypeToIndex(task.type);

            info.path = new();
            foreach (var surface in task.path)
            {
                info.path.Add(SurfaceInfo.ToData(surface));
            }

            return info;
        }

    }

    [Serializable]
    public class NestPartInfo
    {
        public serializableVector3 startPos { get; set; }
        public serializableVector3 endPos { get; set; }
        public float radius { get; set; }
        public int mode { get; set; }
        public HashSet<serializableVector3Int> digPointsLeft { get; set; }
        public HashSet<int> cornPips { get; set; }
        public HashSet<int> antEggs { get; set; }

        private NestPartInfo()
        {

        }

        public static NestPartInfo ToData(NestPart nestPart)
        {
            NestPartInfo info = new()
            {
                mode = NestPart.NestPartTypeToIndex(nestPart.getMode()),
                startPos = new(nestPart.getStartPos()),
                endPos = new(nestPart.getEndPos()),
                radius = nestPart.getRadius(),
                antEggs = nestPart.AntEggs,
                cornPips = nestPart.CollectedCornPips,

                digPointsLeft = new()
            };

            foreach (var pos in nestPart.digPointsLeft)
                info.digPointsLeft.Add(new serializableVector3Int(pos));


            return info;
        }
    }

    [Serializable]
    public class SurfaceInfo
    {
        public Byte group { get; set; }
        public serializableVector3Int pos { get; set; }

        private SurfaceInfo()
        {

        }

        public static SurfaceInfo ToData(CubePaths.CubeSurface surface)
        {
            SurfaceInfo info = new()
            {
                group = ConvertBoolArrayToByte(surface.surfaceGroup),
                pos = new(surface.pos)
            };

            return info;
        }
    }


    public GameData()
    {

    }

    public static GameData Save()
    {
        GameData data = new();

        data.x_dim = WorldGen.x_dim;
        data.y_dim = WorldGen.y_dim;
        data.z_dim = WorldGen.z_dim;

        data.chunk_x_dim = WorldGen.chunk_x_dim;
        data.chunk_y_dim = WorldGen.chunk_y_dim;
        data.chunk_z_dim = WorldGen.chunk_z_dim;

        data.hasWon = WorldGen.hasWon;
        data.playTime = WorldGen.playTime;

        data.camera_pos = WorldGen.camera_pos;
        data.camera_euler = WorldGen.camera_euler;

        data.terrainMapStream = data.EnCode(WorldGen.terrainMap, data.x_dim, data.y_dim, data.z_dim).ToArray();
        data.terrainMemoryStream = data.EnCode(WorldGen.memoryMap, data.x_dim, data.y_dim, data.z_dim).ToArray();

        data.SaveQueens();

        data.SaveAnts();

        data.SaveCorn();

        data.SaveCornCobs();

        data.SaveDigPoints();

        data.SaveNestParts();

        data.SavePhermones();

        return data;
    }

    public void LoadMap()
    {
        WorldGen.x_dim = x_dim;
        WorldGen.y_dim = y_dim;
        WorldGen.z_dim = z_dim;

        WorldGen.chunk_x_dim = chunk_x_dim;
        WorldGen.chunk_y_dim = chunk_y_dim;
        WorldGen.chunk_z_dim = chunk_z_dim;

        WorldGen.hasWon = hasWon;

        WorldGen.updateCameraPos = true;
        WorldGen.camera_pos = camera_pos;
        WorldGen.camera_euler = camera_euler;

        WorldGen.terrainMap = Decode(new MemoryStream(terrainMapStream), x_dim, y_dim, z_dim);
        WorldGen.memoryMap = Decode(new MemoryStream(terrainMemoryStream), x_dim, y_dim, z_dim);

    }

    public void LoadGameObjects()
    {
        
        WorldGen.InstantiateQueen(queenInfo);

        foreach (AntInfo info in antInfoDict)
        {
            Ant ant = WorldGen.InstantiateAnt(info);
            if (eggHeldAntDict.ContainsKey(ant.id))
            {
                Ant holderAnt = Ant.antDictionary[eggHeldAntDict[ant.id]];
                holderAnt.SetToHold(ant.gameObject);
            }
        }

        foreach (CornInfo info in cornInfoDict)
        {
            Corn corn = WorldGen.InstantiateCorn(info);
            if (cornHeldAntDict.ContainsKey(corn.id))
            {
                Ant holderAnt = Ant.antDictionary[cornHeldAntDict[corn.id]];
                holderAnt.SetToHold(corn.gameObject);
            }
        }

        DigPoint.digPointDict = digPointDict;
        foreach (var id in initializedDigPoints)
        {
            DigPoint.digPointDict[id.ToVector3Int()].InstantiatePoint(id.ToVector3Int(), true);
        }

        foreach (var serializablePos in availableDigPointInfoDict)
        {
            DigPoint.availableDigPoints.Add(serializablePos.ToVector3Int());
        }


        if (Nest.KnownCornCobs == null) Debug.Log("preexisting was null cob");
        Nest.KnownCornCobs = knownCornCobs;
        if (knownCornCobs == null) Debug.Log("Saved was null cob");

        //I suspect that this was the missing ingredient to solving the error:
        //No encontré ningun otro sitio donde se instancia a new, asi que debe de ser que
        //no es reseteao la lista de partes al cargar partida en una sesion donde ya se jugó.
        Nest.NestParts = new(); //this
        foreach (NestPartInfo info in nestPartInfoDict)
        {
            WorldGen.InstantiateNestPart(info);
        }

        WorldGen.updateNestVisibility = true;

        Nest.lostPips = cornLostInNestDict;
        Nest.lostEggs = eggsLostInNestDict;
        Nest.antsBringingQueenFood = antsBringingQueenFoodDict;

        CubePaths.cubePheromones = pheromones;


        foreach (CornCobInfo info in cornCobInfoDict)
        {
            CornCob cornCob = WorldGen.InstantiateCornCob(info);
            if (MainMenu.GameSettings.gameMode == 1)
            {
                if (!Nest.KnownCornCobs.Contains(cornCob.id))
                {
                    bool known = false;
                    foreach ((var antId, var antScript) in Ant.antDictionary)
                        if (antScript.discoveredCobs.Contains(cornCob.id))
                            known = true;
                    if (!known)
                        cornCob.Hide();
                }
            }
        }

    }


    private MemoryStream EnCode(int[,,] terrainMap, int x_dim, int y_dim, int z_dim)
    {
        MemoryStream mainStream = new();
        MemoryStream localStream = new();
        BitArray tagByte = new(new bool[8]); //Create a byte at 0
        int i = 0; //Number of entries placed. When reaching 8, 
        int repeatCount;
        int currentValue;

        int x = 0;
        int y = 0;
        int z = 0;

        bool nextPos()
        {
            z++;
            if (z > z_dim) { z = 0; y++; }
            if (y > y_dim) { y = 0; x++; }
            if (x > x_dim) { return false; }
            return true;
        }

        bool end = false;
        bool nextBlock;


        while (!end)
        {
            //Obtain repeatCount number of same values
            currentValue = Mathf.Clamp(terrainMap[x, y, z], 0, 255);
            nextBlock = false;
            repeatCount = 1;
            while (!nextBlock)
            {
                end = !nextPos();
                if (end) nextBlock = true;
                else if  (repeatCount == 255) nextBlock = true;
                else if  (terrainMap[x, y, z] != currentValue) nextBlock = true;
                else     repeatCount++;
            }

            if (repeatCount > 1)
            {
                tagByte[i] = true;
                localStream.Write(new byte[1] { Convert.ToByte(repeatCount) });
            }

            localStream.Write(new byte[1] { Convert.ToByte(currentValue) });

            i++; //next tag

            if (i == 8 || end)
            {
                byte[] bytes = new byte[1];
                tagByte.CopyTo(bytes, 0);
                mainStream.Write(bytes);
                localStream.WriteTo(mainStream);

                i = 0;
                localStream = new();
                tagByte = new(new bool[8]);
            }
        }

        return mainStream;
    }

    int[,,] Decode(MemoryStream data, int x_dim, int y_dim, int z_dim)
    {
        int[,,] decodedMap = new int[x_dim + 1, y_dim + 1, z_dim + 1];

        int x = 0;
        int y = 0;
        int z = 0;

        bool nextPos()
        {
            z++;
            if (z > z_dim) { z = 0; y++; }
            if (y > y_dim) { y = 0; x++; }
            if (x > x_dim) { return false; }
            return true;
        }

        BitArray tagByte;
        bool end = false;
        int max = 0;

        while (!end && max < x_dim * y_dim * z_dim * 2)
        {
            max++;
            byte[] byteVal = new byte[1] { Convert.ToByte(data.ReadByte()) };
            tagByte = new BitArray(byteVal);


            for (int i = 0; i < 8; i++)
            {
                int count = 1;

                if (tagByte[i]) count = data.ReadByte();

                int val = data.ReadByte();

                for (; count > 0; count--)
                {
                    decodedMap[x, y, z] = val;
                    //Debug.Log("Pos: " + x +", "+ y +", "+ z + ". new: " + decodedMap[x,y,z] + ". old: " + WorldGen.terrainMap[x,y,z]);
                    if (!nextPos())
                    {
                        end = true;
                        break;
                    }

                }
                if (end) break;
            }
        }

        Debug.Log("THIS TOOK " + max + " TIMES TO FIX");


        return decodedMap;

    }


    private byte SetBit(byte nByte, int index, bool value)
    {
        int bitIndex = 7 - index % 8;
        byte mask = (byte)(1 << bitIndex);

        nByte = (byte)(value ? (nByte | mask) : (nByte & ~mask));
        return nByte;
    }

    public static byte ConvertBoolArrayToByte(bool[] source)
    {
        byte result = 0;
        // This assumes the array never contains more than 8 elements!
        int index = 8 - source.Length;

        // Loop through the array
        foreach (bool b in source)
        {
            // if the element is 'true' set the bit at that position
            if (b)
                result |= (byte)(1 << (7 - index));

            index++;
        }

        return result;
    }

    public static bool[] ConvertByteToBoolArray(byte b)
    {
        // prepare the return result
        bool[] result = new bool[8];

        // check each bit in the byte. if 1 set to true, if 0 set to false
        for (int i = 0; i < 8; i++)
            result[i] = (b & (1 << i)) != 0;

        // reverse the array
        Array.Reverse(result);

        return result;
    }

}
    /*
        bool isFirst = true;
        bool sameValue;
        bool valueRepeated;

        //THis whole section is the draw.io graph for encoding.
        for (int x = 0; x < x_dim + 1; x++)
        {
            for (int y = 0; y < y_dim + 1; y++)
            {
                for (int z = 0; z < z_dim + 1; z++)
                {
                    newValue = Convert.ToByte(terrainMap[x,y,z]);

                    if (isFirst)
                    {
                        isFirst = false;
                        repeatCount = 1;
                    }
                    else
                    {
                        if (newValue.Equals(prevValue))
                        {
                            sameValue = true;
                            repeatCount++;
                        }
                        else
                            sameValue = false;
                        
                        if (repeatCount > 1) valueRepeated = true;
                        else valueRepeated = false;


                        if ((!sameValue && valueRepeated) || (sameValue && repeatCount == 255))
                        {
                            tagByte = SetBit(tagByte, i, true);
                            localStream.WriteByte(Convert.ToByte(repeatCount));
                        }

                        if (!sameValue || repeatCount == 255)
                        {
                            localStream.WriteByte(prevValue);
                            repeatCount = 1;
                            i++;

                            if (i == 8) // completed a data block
                            {
                                mainStream.WriteByte(tagByte);
                                localStream.WriteTo(mainStream);
                                localStream = new();
                                tagByte = Convert.ToByte(0);
                                i = 0;
                            }
                        }
                    }
                    
                    prevValue = newValue;

                }
            }
        }

        if (repeatCount > 1)
        {
            tagByte = SetBit(tagByte, i, true);
            localStream.WriteByte(Convert.ToByte(repeatCount));
        }
        
        localStream.WriteByte(prevValue);
        mainStream.WriteByte(tagByte);
        localStream.WriteTo(mainStream);
        */