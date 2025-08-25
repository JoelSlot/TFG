using System;
using System.Collections.Generic;
using UnityEngine;

public class DigPoint : MonoBehaviour
{

    //Remember that this class only exists to be a fisicial object. All the relevant info is in digPointData.
    //But it is still necesary


    public static Dictionary<Vector3Int, digPointData> digPointDict = new();
    public static HashSet<Vector3Int> availableDigPoints = new();

    public ParticleSystem particles;


    public class digPointData
    {
        public int value { get; set; }
        public DigPoint digPoint = null;
        public int antId { get; set; } //Id of the ant that is getting the digpoint

        private digPointData()
        {

        }

        public digPointData(int val)
        {
            value = val;
            digPoint = null;
            antId = -1;
        }

        public void update(digPointData newData)
        {
            value = Mathf.Min(newData.value, value);
        }

        public void InstantiatePoint(Vector3Int pos, bool loaded) //loaded bool so that not readded to availableDigPoints when loaded
        {
            if (digPoint == null)
            {
                //Debug.Log("I HAVE BEEN CREATED AT " + pos);
                digPoint = WorldGen.InstantiateDigPoint(pos);
                //Add to available digpoints
                if (!loaded) availableDigPoints.Add(Vector3Int.RoundToInt(digPoint.transform.position));
            }
        }

    }

    void OnDestroy()
    {
        digPointDict.Remove(Vector3Int.RoundToInt(transform.position));
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    private void RemoveDigPointAssignedAnt(Vector3Int pos)
    {
        if (!digPointDict.TryGetValue(pos, out var digPointData)) return;
        int antId = digPointData.antId;
        if (antId != -1)
            if (Ant.antDictionary.TryGetValue(antId, out Ant ant))
                if (ant.objective.digPointId == pos)
                    ant.objective = Task.NoTask();
    }

    private void DigEffectOnAdyacent(Vector3Int key, ref List<Tuple<Vector3Int, int>> terrainEdit)
    {
        //Si el punto adyacente existe
        if (digPointDict.ContainsKey(key))
        {
            digPointData nextDigData = digPointDict[key];
            //Si el punto no influye el terreno y se encuentra en el aire, lo eliminamos y editamos el mapa
            if (IsPointless(key) && WorldGen.IsAboveSurface(key))
            {
                terrainEdit.Add(new Tuple<Vector3Int, int>(key, nextDigData.value));
                digPointDict.Remove(key);
                Destroy(nextDigData.digPoint.gameObject);
            }
            //Si al excavarlo aun se encontraría bajo suelo, es pared. Lo excavamos entonces de gratis.
            else if (nextDigData.value > WorldGen.isolevel)
            {
                if (WorldGen.SampleTerrain(key) > nextDigData.value) terrainEdit.Add(new Tuple<Vector3Int, int>(key, nextDigData.value));
                digPointDict.Remove(key);
            }
            //si no es ni pared ni inútil, le quitamos un poco de valor para que sea mas natural la excavación.
            else
            {
                nextDigData.InstantiatePoint(key, false);
                //Quitar un poco de los alrededores
                int newVal = WorldGen.SampleTerrain(key) - 2;
                if (newVal > nextDigData.value) //Si el valor es mayor que el valor min que se queire obtener:
                    //sampled terrain with original pos for some reason. Error or intentional? We'll see in testing now that it's "fixed"
                    if (WorldGen.SampleTerrain(key) > newVal) terrainEdit.Add(new Tuple<Vector3Int, int>(key, newVal)); //Lo ponemos al valor obtenido
            }
        }
    }

    public void Dig()
    {
        //Comenzamos la lista de puntos a editar con el digPoint mismo
        Vector3Int pos = Vector3Int.RoundToInt(transform.position);
        if (!digPointDict.ContainsKey(pos))
        {
            Destroy(this.gameObject);
            return;  
        }
        int val = digPointDict[pos].value;
        List<Tuple<Vector3Int, int>> terrainEdit = new();
        if (WorldGen.SampleTerrain(pos) > val) terrainEdit.Add(new Tuple<Vector3Int, int>(pos, val));

        //Eliminamos el task de la hormiga que lo está excavando (para casos de autoexcavación)
        RemoveDigPointAssignedAnt(pos);

        //Miramos todos los digPoints alrededores
        Vector3Int[] directions = { Vector3Int.up, Vector3Int.down, Vector3Int.left, Vector3Int.right, Vector3Int.forward, Vector3Int.back };
        foreach (Vector3Int direction in directions)
        {
            Vector3Int ady = pos + direction;
            DigEffectOnAdyacent(ady, ref terrainEdit);
        }
        digPointDict.Remove(pos);

        if (terrainEdit.Count > 0) WorldGen.EditTerrainSet(terrainEdit);
    }

    public void BigDig()
    {
        this.Dig();
        
        Vector3Int pos = Vector3Int.RoundToInt(transform.position);
        Vector3Int[] directions = { Vector3Int.up, Vector3Int.down, Vector3Int.left, Vector3Int.right, Vector3Int.forward, Vector3Int.back };
        foreach (Vector3Int direction in directions)
        {
            Vector3Int ady = pos + direction;
            if (digPointDict.ContainsKey(ady))
            {
                if (digPointDict[ady].digPoint != null)
                    digPointDict[ady].digPoint.Dig();
            }
        }
    }

    public void BigFaultyDig()
    {
        //Comenzamos la lista de puntos a editar con el digPoint mismo
        Vector3Int pos = Vector3Int.RoundToInt(transform.position);
        int val = digPointDict[pos].value;
        List<Tuple<Vector3Int, int>> terrainEdit = new();
        if (WorldGen.SampleTerrain(pos) > val)
            terrainEdit.Add(new Tuple<Vector3Int, int>(pos, val));


        Vector3Int[] directions = { Vector3Int.up, Vector3Int.down, Vector3Int.left, Vector3Int.right, Vector3Int.forward, Vector3Int.back };
        HashSet<Vector3Int> checkedPos = new() { pos };
        foreach (Vector3Int direction in directions)
        {
            Vector3Int adyacent = pos + direction;
            if (digPointDict.ContainsKey(adyacent))
            {
                val = digPointDict[adyacent].value;
                if (WorldGen.SampleTerrain(adyacent) > val)
                {
                    terrainEdit.Add(new Tuple<Vector3Int, int>(adyacent, val));
                    foreach (Vector3Int direction2 in directions)
                    {
                        Vector3Int adyacent2 = adyacent + direction;
                        if (checkedPos.Contains(adyacent2)) continue; //exit if already checked
                        checkedPos.Add(adyacent2);
                        DigEffectOnAdyacent(adyacent2, ref terrainEdit);
                    }
                }
                //destroy the adyacent digpoints
                Destroy(digPointDict[adyacent].digPoint.gameObject);
                digPointDict.Remove(adyacent);
            }
        }




        //Eliminamos el task de la hormiga que lo está excavando (para casos de autoexcavación)
        RemoveDigPointAssignedAnt(pos);

        //Miramos todos los digPoints alrededores
        foreach (Vector3Int direction in directions)
        {
            Vector3Int key = pos + direction;
            //Si el punto adyacente existe
            if (digPointDict.ContainsKey(key))
            {
                digPointData nextDigData = digPointDict[key];
                //Si el punto no influye el terreno y se encuentra en el aire, lo eliminamos y editamos el mapa
                if (IsPointless(key) && WorldGen.IsAboveSurface(key))
                {
                    terrainEdit.Add(new Tuple<Vector3Int, int>(key, nextDigData.value));
                    digPointDict.Remove(key);
                    Destroy(nextDigData.digPoint.gameObject);
                }
                //Si al excavarlo aun se encontraría bajo suelo, es pared. Lo excavamos entonces de gratis.
                else if (nextDigData.value > WorldGen.isolevel)
                {
                    if (WorldGen.SampleTerrain(pos) > nextDigData.value) terrainEdit.Add(new Tuple<Vector3Int, int>(pos + direction, nextDigData.value));
                    digPointDict.Remove(key);
                }
                //si no es ni pared ni inútil, le quitamos un poco de valor para que sea mas natural la excavación.
                else
                {
                    nextDigData.InstantiatePoint(key, false);
                    //Quitar un poco de los alrededores
                    int newVal = WorldGen.SampleTerrain(key) - 2;
                    if (newVal > nextDigData.value) //Si el valor es mayor que el valor min que se queire obtener:
                        if (WorldGen.SampleTerrain(pos) > newVal) terrainEdit.Add(new Tuple<Vector3Int, int>(pos + direction, newVal)); //Lo ponemos al valor obtenido
                }
            }
        }
        digPointDict.Remove(pos);


        if (terrainEdit.Count > 0) WorldGen.EditTerrainSet(terrainEdit);
    }

    //Used to score digPoints on how reachable they are.
    public static int ReachableScore(Vector3Int pos)
    {
        int score = 0;
        Vector3Int[] directions = { Vector3Int.up, Vector3Int.down, Vector3Int.left, Vector3Int.right, Vector3Int.forward, Vector3Int.back };
        foreach (Vector3Int direction in directions)
        {
            Vector3Int adj = pos + direction;

            if (WorldGen.IsAboveSurface(adj)) score++;
        }
        return score;
    }

    //No tiene sentido crear un digpoint donde no va a tener un efecto: que el y todos sus alrededores ya están sobre la superficie.
    public static bool IsPointless(Vector3Int pos)
    {
        if (!WorldGen.IsAboveSurface(pos)) return false;
        Vector3Int[] directions = { Vector3Int.up, Vector3Int.down, Vector3Int.left, Vector3Int.right, Vector3Int.forward, Vector3Int.back };
        foreach (Vector3Int direction in directions)
        {
            Vector3Int adj = pos + direction;
            if (!WorldGen.IsAboveSurface(adj)) return false;
        }

        return true;
    }


    public static bool Separated(Vector3Int pos)
    {
        if (!digPointDict.ContainsKey(pos))
            return true;

        Queue<Vector3Int> frontier = new();
        frontier.Enqueue(pos);
        HashSet<Vector3Int> checkedPos = new();
        Vector3Int[] directions = { Vector3Int.up, Vector3Int.down, Vector3Int.left, Vector3Int.right, Vector3Int.forward, Vector3Int.back };

        while (frontier.Count > 0)
        {
            Vector3Int currentPos = frontier.Dequeue();
            checkedPos.Add(currentPos);
            foreach (Vector3Int direction in directions)
            {
                Vector3Int son = currentPos + direction;

                if (checkedPos.Contains(son)) continue;

                if (digPointDict.ContainsKey(son)) frontier.Enqueue(son);
                else if (!WorldGen.IsAboveSurface(son)) return false;
            }

        }
        return true;
    }

    int counter = 0;

    void FixedUpdate()
    {
        counter++;

        if (counter > 100)
        {
            counter = 0;
            Vector3Int pos = Vector3Int.RoundToInt(transform.position);
            if (Separated(pos))
            {
                Dig();
                Destroy(this.gameObject);
            }
        }

        //Disable/enable particles
        if (Emitter.enabled && particles.isStopped)
            particles.Play();
        else if (!Emitter.enabled && particles.isPlaying)
            particles.Stop();
    }

}

