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


public class GameData 
{

    //Fist i could not serialize cuz static. Then cuz they were fields and the xml would be empty. Finally they are properties.

    public byte[] terrainMapStream {get; set;}
    public byte[] terrainMemoryStream {get; set;}
    public int x_dim {get; set;}
    public int y_dim {get; set;}
    public int z_dim {get; set;}
    
    public int chunk_x_dim {get; set;}
    public int chunk_y_dim {get; set;}
    public int chunk_z_dim  {get; set;}

    public serializableVector3 camera_pos {get; set;}
    public serializableVector3 camera_euler {get; set;}
    public HashSet<AntInfo> antInfoDict {get; set;}
    public HashSet<CornInfo> cornInfoDict {get; set;}
    public Dictionary<int, int> cornHeldAntDict {get; set;} //Key is corn index, value is ant index
    public Dictionary<Vector3Int, DigPoint.digPointData> digPointDict {get; set;}
    public HashSet<serializableVector3Int> initializedDigPoints {get; set;}
    public HashSet<NestPartInfo> nestPartInfoDict {get; set;}
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
    public void saveAnts()
    {
        antInfoDict = new();
        foreach(var (id, ant) in Ant.antDictionary)
        {
            antInfoDict.Add(AntInfo.ToData(ant));
        }
        Debug.Log("Num ants: " + antInfoDict.Count);
    }

    //Save all corn to the dictionary, and add links between held coins and the ants holding them
    public void saveCorn()
    {
        cornInfoDict = new();
        cornHeldAntDict = new();
        foreach(var (id, corn) in Corn.cornDictionary)
        {
            cornInfoDict.Add(CornInfo.ToData(corn));
            int holderAntIndex = corn.holderAntIndex();
            if (holderAntIndex != -1)
            {
                cornHeldAntDict.Add(id, holderAntIndex);
            }
        }
    }

    public void saveDigPoints()
    {
        digPointDict = DigPoint.digPointDict;
        initializedDigPoints = new();
        foreach(var (id, digPointData) in digPointDict)
        {
            if (digPointData.digPoint != null)
            {
                initializedDigPoints.Add(new serializableVector3Int(id));
            }
        }
    }

    public void saveNestParts()
    {
        nestPartInfoDict = new();
        foreach(var nestPart in Nest.NestParts)
        {
            nestPartInfoDict.Add(NestPartInfo.ToData(nestPart));
        }
        Debug.Log("Num nestParts: " + nestPartInfoDict.Count);
    }

    [Serializable]
    public class serializableVector3Int
    {
        public int x {get; set;}
        public int y {get; set;}
        public int z {get; set;}

        public serializableVector3Int(Vector3Int original)
        {
            x = original.x;
            y = original.y;
            z = original.z;
        }

        private serializableVector3Int(){}

        public Vector3Int ToVector3Int()
        {
            return new Vector3Int(x, y, z);
        }
    }

    [Serializable]
    public class serializableVector3
    {
        public float x {get; set;}
        public float y {get; set;}
        public float z {get; set;}

        public serializableVector3(Vector3 original)
        {
            x = original.x;
            y = original.y;
            z = original.z;
        }

        private serializableVector3(){}

        public Vector3 ToVector3()
        {
            return new Vector3(x, y, z);
        }
    }

    [Serializable]
    public class serializableQuaternion
    {
        public float x {get; set;}
        public float y {get; set;}
        public float z {get; set;}
        public float w {get; set;}

        public serializableQuaternion(Quaternion original)
        {
            x = original.x;
            y = original.y;
            z = original.z;
            w = original.w;
        }

        private serializableQuaternion() {}

        public Quaternion ToQuaternion()
        {
            return new Quaternion(x, y, z, w);
        }
    }

    [Serializable]
    public class CornInfo
    {
        public int id {get; set;}
        public serializableVector3 pos {get; set;}
        public serializableQuaternion orientation {get; set;}
        

        private CornInfo()
        {

        }

        public static CornInfo ToData(Corn corn)
        {
            CornInfo info = new();
            info.id = corn.id;
            info.pos = new (corn.transform.position);
            info.orientation = new(corn.transform.rotation);
            return info;
        }
    }

    [Serializable]
    public class AntInfo
    {
        public int id {get; set;}
        public TaskInfo objective {get; set;} //This was task, but since it didnt serialize the task's enum and shit properly
        public bool isControlled {get; set;}
        public int followingPheromone {get; set;}
        public int creatingPheromone {get; set;}
        public serializableVector3 pos {get; set;}
        public serializableQuaternion orientation {get; set;}
        public bool isHolding {get; set;}

        private AntInfo()
        {

        }

        public static AntInfo ToData(Ant ant)
        {
            AntInfo info = new();
            info.id = ant.id;
            info.objective = TaskInfo.ToData(ant.objective);
            info.isControlled = ant.isControlled;
            info.followingPheromone = ant.followingPheromone;
            info.creatingPheromone = ant.creatingPheromone;
            info.pos = new (ant.transform.position);
            info.orientation = new(ant.transform.rotation);

            info.isHolding = ant.IsHolding();

            return info;
        }

    }

    [Serializable]
    public class TaskInfo
    {        
        public Vector3Int digPointId {get; set;}
        public int foodId {get; set;}
        public serializableVector3 pos {get; set;}
        public int typeIndex {get; set;}



        private TaskInfo()
        {

        }

        public static TaskInfo ToData(Task task)
        {
            TaskInfo info = new();
            info.digPointId = task.digPointId;
            info.foodId = task.foodId;
            info.pos = new(task.pos);
            info.typeIndex = Task.TypeToIndex(task.type);
            return info;
        }

    }

    [Serializable]
    public class NestPartInfo
    {
        public serializableVector3 startPos {get; set;}
        public serializableVector3 endPos {get; set;}
        public float radius {get; set;}
        public int mode {get; set;}

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
                radius = nestPart.getRadius()
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

        data.camera_pos = WorldGen.camera_pos;
        data.camera_euler = WorldGen.camera_euler;

        data.terrainMapStream = data.EnCode(WorldGen.terrainMap, data.x_dim, data.y_dim, data.z_dim).ToArray();
        data.terrainMemoryStream = data.EnCode(WorldGen.MemoryMap, data.x_dim, data.y_dim, data.z_dim).ToArray();

        data.saveAnts();

        data.saveCorn();

        data.saveDigPoints();

        data.saveNestParts();

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

        WorldGen.newCameraPosInfo = true;
        WorldGen.camera_pos = camera_pos;
        WorldGen.camera_euler = camera_euler;

        WorldGen.terrainMap = Decode(new MemoryStream(terrainMapStream), x_dim, y_dim, z_dim);
        WorldGen.MemoryMap = Decode(new MemoryStream(terrainMemoryStream), x_dim, y_dim, z_dim);

    }

    public void LoadGameObjects()
    {
        foreach (AntInfo info in antInfoDict)
        {
            WorldGen.InstantiateAnt(info);
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
            
            DigPoint.digPointDict[id.ToVector3Int()].InstantiatePoint(id.ToVector3Int());
        }

        foreach (NestPartInfo info in nestPartInfoDict)
        {
            WorldGen.InstantiateNestPart(info);
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
            if (z > z_dim){z = 0; y++;}
            if (y > y_dim){y = 0; x++;}
            if (x > x_dim){return false;}
            return true;
        }

        bool end = false;
        bool nextBlock;
        

        while (!end)
        {
            //Obtain repeatCount number of same values
            currentValue = terrainMap[x,y,z];
            nextBlock = false;
            repeatCount = 1;
            while (!nextBlock)
            {
                end = !nextPos();
                if (end) nextBlock = true;
                else if (repeatCount == 255) nextBlock = true;
                else if (terrainMap[x,y,z] != currentValue) nextBlock = true;
                else
                {
                    repeatCount++;
                }
            }

            if (repeatCount > 1)
            {
                tagByte[i] = true;
                localStream.Write(new byte[1]{Convert.ToByte(repeatCount)});
            }

            localStream.Write(new byte[1]{Convert.ToByte(currentValue)});

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
            if (z > z_dim){z = 0; y++;}
            if (y > y_dim){y = 0; x++;}
            if (x > x_dim){return false;}
            return true;
        }

        BitArray tagByte = new(255);
        bool end = false;
        int max = 0;

        while(!end && max < x_dim * y_dim * z_dim * 2)
        {
            max++;
            byte[] byteVal = new byte[1]{Convert.ToByte(data.ReadByte())};
            tagByte = new BitArray(byteVal);


            for (int i = 0; i < 8; i++)
            {
                int count = 1;

                if (tagByte[i]) count = data.ReadByte();

                int val = data.ReadByte();

                for ( ; count > 0; count--)
                {
                    decodedMap[x,y,z] = val;
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
        int bitIndex =  7 - index % 8;
        byte mask = (byte)(1 << bitIndex);

        nByte = (byte)(value ? (nByte | mask) : (nByte & ~mask));
        return nByte;
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