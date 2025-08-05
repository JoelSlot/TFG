using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class NestPart : MonoBehaviour
{

    public GameObject cilinder;
    public GameObject startSphere;
    public GameObject endSphere;
    public Material normalMaterial;
    public Material errorMaterial;
    public Material unDugMaterial;
    public Rigidbody rigidBody;

    public MeshRenderer cilinderRenderer;
    public MeshRenderer startSphereRenderer;
    public MeshRenderer endSphereRenderer;

    public enum NestPartType { Tunnel, FoodChamber, Outside }

    public static int NestPartTypeToIndex(NestPartType type)
    {
        switch (type)
        {
            case NestPartType.Tunnel: return 0;
            case NestPartType.FoodChamber: return 1;
            case NestPartType.Outside: return 2;
        }
        return -1;
    }

    public static NestPartType IndexToNestPartType(int index)
    {
        switch (index)
        {
            case 0: return NestPartType.Tunnel;
            case 1: return NestPartType.FoodChamber;
            case 3: return NestPartType.Outside;
        }
        Debug.Log("ERROR");
        return NestPartType.Tunnel;
    }


    public NestPartType mode = NestPartType.Tunnel;

    private Vector3 dir = Vector3.up;
    private Vector3 startPos = Vector3.zero;
    private Vector3 endPos = Vector3.up;
    private float radius = 1;
    public float interval;
    public bool gotPoints = false; //whether the points have been extracted yet.
    public HashSet<Vector3Int> digPointsLeft = new();


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        //Set the material render order
        errorMaterial.renderQueue = 3001;
        normalMaterial.renderQueue = 3002;
        unDugMaterial.renderQueue = 3003;
    }

    public void Show()
    {
        cilinderRenderer.enabled = true;
        startSphereRenderer.enabled = true;
        endSphereRenderer.enabled = true;
    }

    public void Hide()
    {
        cilinderRenderer.enabled = false;
        startSphereRenderer.enabled = false;
        endSphereRenderer.enabled = false;
    }

    // Update is called once per frame
    void Update()
    {
        if (!gotPoints)
            if (IsValidPosition())
                setMaterial(normalMaterial);
            else
                setMaterial(errorMaterial);
        else if (HasBeenDug())
            setMaterial(normalMaterial);
        else
            setMaterial(unDugMaterial);

    }

    public void setMode(NestPartType newMode)
    {
        //if (newMode == mode) return; This was causing the toggleCollision to not run...
        mode = newMode;
        if (newMode == NestPartType.Tunnel)
        {
            startSphere.SetActive(true);
            endSphere.SetActive(true);
            cilinder.SetActive(true);
            transform.localRotation = Quaternion.Euler(dir);
            SetPos(transform.position, endPos);
        }
        else if (newMode == NestPartType.FoodChamber)
        {
            startSphere.SetActive(true);
            endSphere.SetActive(false);
            cilinder.SetActive(false);
            transform.localRotation = Quaternion.Euler(Vector3.up);
        }
    }

    public NestPartType getMode() { return mode; }

    public void SetPos(Vector3 start, Vector3 end)
    {
        endPos = end;
        startPos = start;
        if (mode == NestPartType.Tunnel)
        {
            dir = end - start;

            if (dir.magnitude > 20)
            {
                end = start + dir.normalized * 20;
                endPos = end;
                dir = end - start;
            }

            startSphere.transform.localPosition = Vector3.zero;
            endSphere.transform.localPosition = Vector3.up * dir.magnitude;
            cilinder.transform.localPosition = Vector3.up * dir.magnitude / 2;
            cilinder.transform.localScale = new Vector3(radius * 2, dir.magnitude / 2, radius * 2);
            transform.up = dir.normalized;
            rigidBody.MovePosition(startPos);
        }
        else if (mode == NestPartType.FoodChamber)
        //The chamber uses startpos as its center, and endPos as one of it's corners
        {
            endPos.x = Mathf.Clamp(endPos.x, startPos.x + 5, startPos.x + 20);
            endPos.y = Mathf.Clamp(endPos.y, startPos.y + 5, startPos.y + 15);
            endPos.z = Mathf.Clamp(endPos.z, startPos.z + 5, startPos.z + 20);

            Vector3 distance = endPos - startPos;
            startSphere.transform.localPosition = Vector3.zero;
            startSphere.transform.localScale = new Vector3(Mathf.Abs(distance.x), Mathf.Abs(distance.y), Mathf.Abs(distance.z));


            rigidBody.MovePosition(startPos);

            transform.up = Vector3.up;
            float a = Mathf.Abs(distance.x);
            float b = Mathf.Abs(distance.y);
            float c = Mathf.Abs(distance.z);
            //Debug.Log("a: " + a + ", b: " + b + ", c: " + c);
        }
    }

    public Vector3 getStartPos()
    {
        return startPos;
    }

    public Vector3 getEndPos()
    {
        return endPos;
    }


    public void AddPos(Vector3 add)
    {
        SetPos(startPos, endPos + add);
    }

    public void AddStartPos(Vector3 add)
    {
        SetPos(startPos + add, endPos + add);
    }

    public void setMaterial(Material material)
    {
        Renderer[] childRenders = GetComponentsInChildren<Renderer>();

        foreach (var renderer in childRenders)
        {
            renderer.material = material;
            //renderer.material.renderQueue = Overlay;
        }
    }

    public void setRadius(float newRadius) //LIMITED TO AVOID UNREALISTIC TUNNELS
    {
        if (newRadius < 1 || newRadius > 2 || mode != NestPartType.Tunnel) return; //Min and max radius or wrong mode
        radius = newRadius;
        startSphere.transform.localScale = 2 * radius * Vector3.one;
        endSphere.transform.localScale = 2 * radius * Vector3.one;
        cilinder.transform.localScale = new Vector3(radius * 2, dir.magnitude / 2, radius * 2);
    }

    public void addRadius(float addRadius) //LIMITED TO AVOID UNREALISTIC TUNNELS
    {
        setRadius(radius + addRadius);
    }

    public float getRadius()
    {
        return radius;
    }

    public Vector3 GetDir() { return dir; }

    public bool HasBeenDug()
    {
        List<Vector3Int> positions = digPointsLeft.ToList();
        foreach (var pos in positions)
        {
            if (!DigPoint.digPointDict.ContainsKey(pos))
                digPointsLeft.Remove(pos);
            else
                return false;
        }

        if (digPointsLeft.Count > 0)
            return false;

        return true;
    }

    public void setActive(bool active)
    {
        if (active == false)
        {
            startSphere.SetActive(active);
            endSphere.SetActive(active);
            cilinder.SetActive(active);
        }
        else
        {
            if (mode == NestPartType.Tunnel)
            {
                startSphere.SetActive(true);
                endSphere.SetActive(true);
                cilinder.SetActive(true);
                transform.localRotation = Quaternion.Euler(dir);
                SetPos(transform.position, endPos);
            }
            else if (mode == NestPartType.FoodChamber)
            {
                startSphere.SetActive(true);
                endSphere.SetActive(false);
                cilinder.SetActive(false);
            }
        }
    }

    public void SetVisible(bool visible)
    {
        cilinder.GetComponent<MeshRenderer>().enabled = visible;
        startSphere.GetComponent<MeshRenderer>().enabled = visible;
        endSphere.GetComponent<MeshRenderer>().enabled = visible;
    }

    //Devuelve los puntos que componen el área que se quiere excavar.
    public Dictionary<Vector3Int, DigPoint.digPointData> pointsInDigObject()
    {
        if (gotPoints) return new Dictionary<Vector3Int, DigPoint.digPointData>();
        else gotPoints = true;

        //SetVisible(false);

        Dictionary<Vector3Int, DigPoint.digPointData> points = new();
        HashSet<Vector3Int> checkedPoints = new();
        Queue<Vector3Int> pointsToCheck = new();
        Vector3Int start = Vector3Int.FloorToInt(transform.position);

        int desiredVal = getMarchingValue(start);
        checkedPoints.Add(start);
        digPointsLeft.Add(start);
        pointsToCheck.Enqueue(start);
        points.Add(start, new DigPoint.digPointData(desiredVal)); // used distance for a good while wondering why one of the center points was the wrong value when diggin the cave

        Vector3Int[] directions = { Vector3Int.up, Vector3Int.down, Vector3Int.left, Vector3Int.right, Vector3Int.forward, Vector3Int.back };

        while (pointsToCheck.Count > 0)
        {
            Vector3Int father = pointsToCheck.Dequeue();
            //Se mira cada punto adyacente
            foreach (Vector3Int direction in directions)
            {
                Vector3Int son = father + direction;
                if (!checkedPoints.Contains(son)) // si no se ha visto aun
                {
                    desiredVal = getMarchingValue(son);
                    //Si el punto se encuentra dentro de la forma que se excavará miraremos sus adyacentes
                    if (desiredVal < WorldGen.isolevel)
                    {
                        pointsToCheck.Enqueue(son);
                    }
                    //Si el punto realmente cambiará el terreno, lo guardamos para ser excavado
                    if (desiredVal < WorldGen.SampleTerrain(son) && !DigPoint.IsPointless(son))
                    {
                        points.Add(son, new DigPoint.digPointData(desiredVal));
                        digPointsLeft.Add(son);
                    }
                    //Registramos que ya hemos mirado este punto
                    checkedPoints.Add(son);
                }
            }
        }

        return points;
    }

    public int getMarchingValue(Vector3 pos)
    {
        if (mode == NestPartType.Tunnel)
        {
            float dist = DistancePointLine(pos, transform.position, endPos);
            return Mathf.RoundToInt(Mathf.Clamp(dist * 127.5f / radius, 0, 255));
        }
        else //if (mode == digType.Chamber)
        {
            return Mathf.RoundToInt(Mathf.Clamp(255 * EllipseDistance(pos), 0, 255));
        }
    }

    // Calculate distance between a point and a line.
    public static float DistancePointLine(Vector3 point, Vector3 lineStart, Vector3 lineEnd)
    {
        return Vector3.Magnitude(ProjectPointLine(point, lineStart, lineEnd) - point);
    }

    // Project /point/ onto a line.
    public static Vector3 ProjectPointLine(Vector3 point, Vector3 lineStart, Vector3 lineEnd)
    {
        Vector3 relativePoint = point - lineStart;
        Vector3 lineDirection = lineEnd - lineStart;
        float length = lineDirection.magnitude;
        Vector3 normalizedLineDirection = lineDirection;
        if (length > .000001f)
            normalizedLineDirection /= length;
        float dot = Vector3.Dot(normalizedLineDirection, relativePoint);
        dot = Mathf.Clamp(dot, 0.0F, length);

        return lineStart + normalizedLineDirection * dot;
    }

    //Función que devuelve 0.5 dado un punto en el eje del elipse, menor de 0.5 cuanto mas se aleja del centro y 1 en el centro
    float EllipseDistance(Vector3 point)
    {
        Vector3 distance = endPos - startPos;
        float a = Mathf.Abs(distance.x) / 2;
        float b = Mathf.Abs(distance.y) / 2;
        float c = Mathf.Abs(distance.z) / 2;
        Vector3 localPos = point - transform.position;
        //Debug.DrawLine(transform.position, point, Color.white, 100000);
        float x = localPos.x;
        float y = localPos.y;
        float z = localPos.z;
        //float wrongValue = (x*x/a*a + y*y/b*b + z*z/c*c); //I HAVE BEEN USING THIS FOR A WHOLE DAY AND A HALF WONDERING WHY IT DIDNT WORK AAAAAAAA
        float value = ((x * x) / (a * a) + (y * y) / (b * b) + (z * z) / (c * c));
        //float value2 = 
        //Debug.Log("a: " + a + ", b: " + b + ", c: " + c + ", x: " + x + ", y: " + y + ", z: " + z + " result: " + value);
        return value / 2;
    }



    private HashSet<GameObject> collidingClones = new HashSet<GameObject>();
    // Called when another collider starts touching this one
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject != this.gameObject && collision.gameObject.CompareTag("NestPart"))
        {
            //Only register other non-tunnels (should always be chambers)
            NestPart collisionNestPart = collision.gameObject.GetComponent<NestPart>();
            if (collisionNestPart != null)
                if (collisionNestPart.mode != NestPartType.Tunnel)
                    collidingClones.Add(collision.gameObject);
        }
    }

    // Called when another collider stops touching this one
    private void OnCollisionExit(Collision collision)
    {
        if (collision.gameObject.CompareTag("NestPart"))
        {
            collidingClones.Remove(collision.gameObject);
        }
    }


    public bool IsValidPosition()
    {
        if (mode != NestPartType.Tunnel)
        {
            if (!IsNotCollidingWithOtherChamber()) return false;

            //Obtener todos los puntos a mirar
            Vector3Int orig = Vector3Int.RoundToInt(this.startPos);
            Vector3 distance = endPos - startPos;

            Vector3Int[] check = {
                orig,
                Vector3Int.CeilToInt(orig + Vector3.up * (Mathf.Abs(distance.y / 2) + 1)),
                Vector3Int.FloorToInt(orig + Vector3.left * (Mathf.Abs(distance.x / 2) + 1)), //floor because right is negative and we want the most distant
                Vector3Int.CeilToInt(orig + Vector3.right * (Mathf.Abs(distance.x / 2) + 1)),
                Vector3Int.FloorToInt(orig + Vector3.back * (Mathf.Abs(distance.z / 2) + 1)), //floor because right is negative and we want the most distant
                Vector3Int.CeilToInt(orig + Vector3.forward * (Mathf.Abs(distance.z / 2) + 1)),
                Vector3Int.FloorToInt(orig + Vector3.down * (Mathf.Abs(distance.y / 2) + 1)),
                Vector3Int.FloorToInt(orig + Vector3.down * Mathf.Abs(distance.y / 2)),
                Vector3Int.FloorToInt(orig + Vector3.down * (Mathf.Abs(distance.y / 2) - 1)),
                Vector3Int.FloorToInt(orig + Vector3.down * (Mathf.Abs(distance.y / 2) - 2))
                
                };
            Vector3Int[] check2 = {
                Vector3Int.RoundToInt(orig + ResizeToEllipseSurface((check[1] + check[2]) / 2 - orig, 1)),
                Vector3Int.RoundToInt(orig + ResizeToEllipseSurface((check[1] + check[3]) / 2 - orig, 1)),
                Vector3Int.RoundToInt(orig + ResizeToEllipseSurface((check[1] + check[4]) / 2 - orig, 1)),
                Vector3Int.RoundToInt(orig + ResizeToEllipseSurface((check[1] + check[5]) / 2 - orig, 1)),
                Vector3Int.RoundToInt(orig + ResizeToEllipseSurface((check[2] + check[4]) / 2 - orig, 1)),
                Vector3Int.RoundToInt(orig + ResizeToEllipseSurface((check[2] + check[5]) / 2 - orig, 1)),
                Vector3Int.RoundToInt(orig + ResizeToEllipseSurface((check[3] + check[4]) / 2 - orig, 1)),
                Vector3Int.RoundToInt(orig + ResizeToEllipseSurface((check[3] + check[5]) / 2 - orig, 1)),
                Vector3Int.RoundToInt(orig + ResizeToEllipseSurface((check[6] + check[2]) / 2 - orig, 1)),
                Vector3Int.RoundToInt(orig + ResizeToEllipseSurface((check[6] + check[3]) / 2 - orig, 1)),
                Vector3Int.RoundToInt(orig + ResizeToEllipseSurface((check[6] + check[4]) / 2 - orig, 1)),
                Vector3Int.RoundToInt(orig + ResizeToEllipseSurface((check[6] + check[5]) / 2 - orig, 1)),
                check[7] + Vector3Int.left,
                check[7] + Vector3Int.right,
                check[7] + Vector3Int.forward,
                check[7] + Vector3Int.back
                };

            for (int i = 0; i < 6; i++)
            {
                //Debug.DrawLine(check[0], check[i], Color.yellow, 1);
                if (WorldGen.WasAboveSurface(check[i])) return false;
            }
            for (int i = 6; i < check.Count(); i++)
            {
                //Debug.DrawLine(check[0], check[i], Color.red, 1);
                if (WorldGen.IsAboveSurface(check[i])) return false;
            }

            for (int i = 0; i < 8; i++)
            {
                //Debug.DrawLine(check[0], check2[i], Color.yellow, 1);
                if (WorldGen.WasAboveSurface(check2[i])) return false;
            }
            for (int i = 8; i < check2.Count(); i++)
            {
                //Debug.DrawLine(check[0], check2[i], Color.red, 1);
                if (WorldGen.IsAboveSurface(check2[i])) return false;
            }

            return true;
        }
        else
        {
            if (WorldGen.IsAboveSurface(startPos) && WorldGen.IsAboveSurface(endPos))
                return false;

            foreach (var part in Nest.NestParts)
                {
                    if (part.mode != NestPartType.Tunnel)
                    {
                        Vector3 distance = part.endPos - part.startPos;
                        Vector3 chamberBottomPoint = part.startPos + Vector3.down * Mathf.Abs(distance.y / 2);
                        if (DistancePointLine(chamberBottomPoint, startPos, endPos) < radius + 1)
                            return false;
                    }
                }
            return true;
        }
    }

    // Your boolean function
    public bool IsNotCollidingWithOtherChamber()
    {
        if (mode == NestPartType.Tunnel) return true;
        //Debug.Log(collidingClones.Count);
        return collidingClones.Count == 0;
    }

    public void setKinematic(bool value)
    {
        rigidBody.isKinematic = value;
    }

    public Vector3 ResizeToEllipseSurface(Vector3 vector, float addition)
    {
        Vector3 dim = endPos - startPos;
        float a = Mathf.Abs(dim.x) / 2;
        float b = Mathf.Abs(dim.y) / 2;
        float c = Mathf.Abs(dim.z) / 2;

        vector = vector.normalized;
        float distance = 1 / Mathf.Sqrt((vector.x * vector.x) / (a * a) + (vector.y * vector.y) / (b * b) + (vector.z * vector.z) / (c * c));
        return vector * (distance + addition);
    }

}
