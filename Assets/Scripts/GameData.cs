using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Polenter.Serialization;
using UnityEngine;

public class GameData 
{

    //Fist i could not serialize cuz static. Then cuz they were fields and the xml would be empty. Finally they are properties.

    public byte[] terrainMapStream {get; set;}
    public int x_dim {get; set;}
    public int y_dim {get; set;}
    public int z_dim {get; set;}
    
    public int chunk_x_dim {get; set;}
    public int chunk_y_dim {get; set;}
    public int chunk_z_dim  {get; set;}

    public GameData()
    {
        x_dim = WorldGen.x_dim;
        y_dim = WorldGen.y_dim;
        z_dim = WorldGen.z_dim;

        chunk_x_dim = WorldGen.chunk_x_dim;
        chunk_y_dim = WorldGen.chunk_y_dim;
        chunk_z_dim = WorldGen.chunk_z_dim;

        terrainMapStream = EnCode(WorldGen.terrainMap, x_dim, y_dim, z_dim).ToArray();
    }

    public void Load()
    {
        WorldGen.x_dim = x_dim;
        WorldGen.y_dim = y_dim;
        WorldGen.z_dim = z_dim;

        WorldGen.chunk_x_dim = chunk_x_dim;
        WorldGen.chunk_y_dim = chunk_y_dim;
        WorldGen.chunk_z_dim = chunk_z_dim;

        WorldGen.terrainMap = Decode(new MemoryStream(terrainMapStream), x_dim, y_dim, z_dim);
        Debug.Log("length: " + terrainMapStream.Count());
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