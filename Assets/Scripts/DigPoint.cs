using System;
using System.Collections.Generic;
using UnityEngine;

public class DigPoint : MonoBehaviour
{

    public WorldGen WG;
    public float desiredVal = 0;
    public int depth = 0;
    public static Dictionary<Vector3Int, Tuple<float, GameObject>> digPointDict = new Dictionary<Vector3Int, Tuple<float, GameObject>>();
    //ORIGINALLY USED A HASHSET TO SEE WITCH ONES HADN?T BEEN INITIALIZED BUT NOW JUST USES TUPLE IN DICT

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void SetDesiredVal(float newVal)
    {
        desiredVal = newVal;
    }

    public float GetDesiredVal(){return desiredVal;}

    public void Dig()
    {
        //Comenzamos la lista de puntos a editar con el digPoint mismo
        Vector3Int pos = Vector3Int.CeilToInt(transform.position);
        float val = digPointDict[pos].Item1;
        List<Tuple<Vector3Int, float>> points = new()
        {
            new(pos, val)
        };
        
        //Miramos todos los digPoints alrededores
        Vector3Int[] directions = {Vector3Int.up, Vector3Int.down, Vector3Int.left, Vector3Int.right, Vector3Int.forward, Vector3Int.back};
        foreach (Vector3Int direction in directions)
        {
            if (digPointDict.ContainsKey(pos + direction))
            {
                float desiredVal = digPointDict[pos + direction].Item1;
                if ( desiredVal > WorldGen.isolevel) //si es pared lo excavamos y lo eliminamos del diccionario
                {
                    points.Add(new Tuple<Vector3Int, float>(pos + direction, digPointDict[pos+direction].Item1));
                    digPointDict.Remove(pos + direction);
                }
                else
                {
                    //Inicializamos si no lo está
                    if(digPointDict[pos + direction].Item2 == null)
                    {
                        GameObject digPoint = Instantiate(transform.gameObject, pos + direction, Quaternion.identity);
                        digPoint.SetActive(true);
                        digPoint.GetComponent<DigPoint>().SetDesiredVal(desiredVal);
                        digPoint.GetComponent<DigPoint>().depth = depth + 1;
                        digPointDict[pos + direction] = new Tuple<float, GameObject>(desiredVal, digPoint);
                    }
                    //Quitar un poco de los alrededores
                    float newVal = WorldGen.SampleTerrain(pos + direction) - 0.2f;
                    if (newVal > digPointDict[pos+direction].Item1) //Si el valor no excede o iguala a el valor que se queire obtener:
                        points.Add(new Tuple<Vector3Int, float>(pos+direction, newVal)); //Lo ponemos al valor obtenido
                }
            }
        }
        digPointDict.Remove(pos);
        WG.EditTerrainSet(points);
    }
    
    public Objective getObjective()
    {
        return new Objective(this);
    }

}

