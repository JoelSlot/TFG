using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering;

public enum TaskType
{
    Explore,
    Dig,
    GetCorn,
    CollectFromCob,
    GoOutside,
    GoInside,
    GoToChamber,
    GoToTunnel,
    Lost,
    Wait,
    None
}

public class Task
{
    public Vector3Int digPointId { get; set; }
    public int foodId { get; set; }
    public Vector3 pos { get; set; }
    public TaskType type;

    //Queue de todos los tasks que quedan por hacer
    public static Queue<Task> ToDo = new();

    public List<CubePaths.CubeSurface> path = new(); //Path the ant will follow if in followingPath mode


    public Task(GameObject newGameObject, TaskType newType, List<CubePaths.CubeSurface> newPath)
    {
        type = newType;
        if (type == TaskType.Dig) digPointId = Vector3Int.RoundToInt(newGameObject.transform.position);
        else if (type == TaskType.GetCorn) foodId = newGameObject.GetComponent<Corn>().id;
        else if (type == TaskType.CollectFromCob) foodId = newGameObject.GetComponent<CornCob>().id;
        else Debug.Log("WRONG TASKTYPE");
        pos = newGameObject.transform.position;
        path = newPath;
    }

    private Task()
    {

    }

    public static Task DigTask(Vector3Int digPointId, List<CubePaths.CubeSurface> newPath)
    {
        if (!DigPoint.digPointDict.ContainsKey(digPointId)) return Task.NoTask();
        if (DigPoint.digPointDict[digPointId].digPoint == null) return Task.NoTask();

        Task newTask = new();
        newTask.type = TaskType.Dig;
        newTask.digPointId = digPointId;
        newTask.pos = digPointId;
        newTask.path = newPath;

        return newTask;
    }

    public static Task GetCornTask(int cornId, int antId, List<CubePaths.CubeSurface> newPath)
    {
        if (!Corn.cornDictionary.ContainsKey(cornId)) return Task.NoTask();

        Corn.cornDictionary[cornId].antId = antId;

        Task newTask = new();
        newTask.type = TaskType.GetCorn;
        newTask.foodId = cornId;
        newTask.pos = Corn.cornDictionary[cornId].transform.position;
        newTask.path = newPath;

        return newTask;
    }

    public static Task GoOutsideTask(CubePaths.CubeSurface antSurface)
    {
        Task GOtask = new()
        {
            type = TaskType.GoOutside,
            path = new()
        };

        if (CubePaths.GetKnownPathToMapPart(antSurface, NestPart.NestPartType.Outside, out GOtask.path)) return GOtask;

        Debug.Log("No valid path, didnt create GoOutSideTask");

        return LostTask(antSurface, Vector3.up);
    }

    public static Task GoInsideTask(CubePaths.CubeSurface antSurface)
    {
        Task GOtask = new()
        {
            type = TaskType.GoInside,
        };

        //Used foodchamber for now as default
        if (CubePaths.GetKnownPathToMapPart(antSurface, NestPart.NestPartType.FoodChamber, out GOtask.path)) return GOtask;

        Debug.Log("No valid path");

        return LostTask(antSurface, Vector3.forward);
    }

    public static Task GoToNestPartTask(CubePaths.CubeSurface antSurface, NestPart.NestPartType type)
    {
        Task GOtask = new();
        switch (type)
        {
            case NestPart.NestPartType.Outside:
                GOtask.type = TaskType.GoOutside;
                break;
            case NestPart.NestPartType.FoodChamber:
                GOtask.type = TaskType.GoToChamber;
                break;
            case NestPart.NestPartType.Tunnel:
                GOtask.type = TaskType.GoToTunnel;
                break;
        }

        if (CubePaths.GetKnownPathToMapPart(antSurface, type, out GOtask.path)) return GOtask;

        Debug.Log("No valid path");

        return LostTask(antSurface, Vector3.up);
    }

    public static Task ExploreTask(CubePaths.CubeSurface antSurface, Vector3 forward, out int timesToRepeat)
    {
        Task exploreTask = new()
        {
            type = TaskType.Explore,
        };

        if (CubePaths.GetExplorePath(antSurface, forward, out exploreTask.path))
        {
            timesToRepeat = Random.Range(30, 80); //La "distancia" de exploración
            return exploreTask;
        }

        Debug.Log("BIG ERROR; ANT IS IN STRANGE PLACE");

        timesToRepeat = 0; //Ya que puede ser el counter, hay que ponerlo en 0
        return NoTask();
    }

    public static Task NoTask()
    {
        Task notask = new()
        {
            type = TaskType.None,
            path = new()
        };
        return notask;
    }

    public static Task LostTask(CubePaths.CubeSurface antSurface, Vector3 forward)
    {
        Task lostTask = new()
        {
            type = TaskType.Lost,
            path = new()
        };

        if (CubePaths.GetLostPath(antSurface, forward, out lostTask.path)) return lostTask;

        return lostTask;
    }

    public static Task WaitTask(Ant ant, int time)
    {
        Task waitTask = new()
        {
            type = TaskType.Wait,
            path = new()
        };

        ant.Counter = time;
        return waitTask;
    }

    public bool isTaskType(TaskType checkType) { return checkType == type; }
    public GameObject GetFood()
    {
        if (isTaskType(TaskType.GetCorn))
        {
            if (Corn.cornDictionary.TryGetValue(foodId, out Corn corn))
                return corn.gameObject;
        }
        else if (isTaskType(TaskType.CollectFromCob))
        {
            if (CornCob.cornCobDictionary.TryGetValue(foodId, out CornCob cob))
                return cob.gameObject;
        }
        return null;
    }

    public DigPoint GetDigPoint()
    {
        if (isTaskType(TaskType.Dig))
            if (DigPoint.digPointDict.ContainsKey(digPointId))
                return DigPoint.digPointDict[digPointId].digPoint;

        return null;
    }

    public static bool IsCornBeingPickedUp(Corn cornScript)
    {
        //Mirar si ya lo va a recoger otra hormiga
        if (cornScript.antId != -1)
        {
            if (Ant.antDictionary.TryGetValue(cornScript.antId, out Ant cornAnt))
            {
                if (cornAnt.objective.isTaskType(TaskType.GetCorn))
                    if (cornAnt.objective.foodId == cornScript.id)
                        return true;
            }
        }
        return false;
    }
    public static bool IsCornBeingPickedUp(int cornId)
    {
        if (!Corn.cornDictionary.TryGetValue(cornId, out Corn cornScript)) return true;

        return IsCornBeingPickedUp(cornScript);
    }

    public static bool IsDigPointBeingDug(Vector3Int digPointPos)
    {
        int digPointsAntId = DigPoint.digPointDict[digPointPos].antId;
        //Mirar si ya lo va a excavar otra hormiga
        if (digPointsAntId != -1)
        {
            if (Ant.antDictionary.TryGetValue(digPointsAntId, out Ant digPointsAnt))
            {
                if (digPointsAnt.objective.isTaskType(TaskType.Dig))
                    if (digPointsAnt.objective.digPointId == digPointPos)
                        return true;
            }
        }
        return false;
    }


    public bool isValid(Ant ant)
    {
        switch (type)
        {
            case TaskType.Dig:
                if (!DigPoint.digPointDict.ContainsKey(digPointId))
                {
                    //If the objective is not valid, the ant loses it.
                    ant.objective = Task.NoTask();
                    return false;
                }
                break;
            case TaskType.GetCorn:
                GameObject foodObj = GetFood();
                //if the food item no longer exists
                if (foodObj == null)
                {
                    ant.objective = Task.NoTask();
                    return false;
                }
                //To check if held by ant, just see if held, it is not by corncob.
                bool heldByAnt = false;
                if (foodObj.transform.parent != null)
                    if (foodObj.transform.parent.GetComponent<CornCob>() == null)
                        heldByAnt = true;
                // if it has moved somehow or has been picked up
                if (Vector3.Distance(GetFood().transform.position, pos) > 0.5f || heldByAnt)
                {
                    //If the objective is not valid, the ant loses it.
                    ant.objective = Task.NoTask();
                    return false;
                }
                break;
            case TaskType.CollectFromCob:
                //if the cornCob no longer exists:
                if (GetFood() == null)
                {
                    ant.objective = Task.NoTask();
                    return false;
                }
                // if it has moved somehow or has no more corn.
                if (Vector3.Distance(GetFood().transform.position, pos) > 0.5f || !GetFood().GetComponent<CornCob>().hasCorn())
                {
                    //If the objective is not valid, the ant loses it.
                    ant.objective = Task.NoTask();
                    return false;
                }
                break;
        }
        return true;
    }

    
    public bool isValid(AntQueen ant)
    {
        switch (type)
        {
            case TaskType.Dig:
                if (!DigPoint.digPointDict.ContainsKey(digPointId))
                {
                    //If the objective is not valid, the ant loses it.
                    ant.objective = Task.NoTask();
                    return false;
                }
                break;
            case TaskType.GetCorn:
                GameObject foodObj = GetFood();
                //if the food item no longer exists
                if (foodObj == null)
                {
                    ant.objective = Task.NoTask();
                    return false;
                }
                //To check if held by ant, just see if held, it is not by corncob.
                bool heldByAnt = false;
                if (foodObj.transform.parent != null)
                    if (foodObj.transform.parent.GetComponent<CornCob>() == null)
                        heldByAnt = true;
                // if it has moved somehow or has been picked up
                if (Vector3.Distance(GetFood().transform.position, pos) > 0.5f || heldByAnt)
                {
                    //If the objective is not valid, the ant loses it.
                    ant.objective = Task.NoTask();
                    return false;
                }
                break;
            case TaskType.CollectFromCob:
                //if the cornCob no longer exists:
                if (GetFood() == null)
                {
                    ant.objective = Task.NoTask();
                    return false;
                }
                // if it has moved somehow or has no more corn.
                if (Vector3.Distance(GetFood().transform.position, pos) > 0.5f || !GetFood().GetComponent<CornCob>().hasCorn())
                {
                    //If the objective is not valid, the ant loses it.
                    ant.objective = Task.NoTask();
                    return false;
                }
                break;
        }
        return true;
    }


    public Vector3 getPos() { return pos; }

    public string TaskToString()
    {
        switch (type)
        {
            case TaskType.Explore: return "Go to pos";
            case TaskType.Dig: return "Dig point";
            case TaskType.GetCorn: return "Get food";
            case TaskType.GoOutside: return "Go outside";
            case TaskType.GoInside: return "Go inside";
            case TaskType.GoToChamber: return "Go to chamber";
            case TaskType.GoToTunnel: return "Go to tunnel";
            case TaskType.Lost: return "Lost";
            case TaskType.Wait: return "Waiting";
            case TaskType.None: return "None";
            case TaskType.CollectFromCob: return "Going to cob";
        }
        return "error";
    }

    public static int TypeToIndex(TaskType type)
    {
        switch (type)
        {
            case TaskType.Explore: return 0;
            case TaskType.Dig: return 1;
            case TaskType.GetCorn: return 2;
            case TaskType.GoOutside: return 3;
            case TaskType.GoInside: return 4;
            case TaskType.GoToChamber: return 5;
            case TaskType.GoToTunnel: return 6;
            case TaskType.Lost: return 7;
            case TaskType.Wait: return 8;
            case TaskType.None: return 9;
            case TaskType.CollectFromCob: return 10;
        }
        return -1;
    }

    public static TaskType IndexToType(int index)
    {
        switch (index)
        {
            case 0: return TaskType.Explore;
            case 1: return TaskType.Dig;
            case 2: return TaskType.GetCorn;
            case 3: return TaskType.GoOutside;
            case 4: return TaskType.GoInside;
            case 5: return TaskType.GoToChamber;
            case 6: return TaskType.GoToTunnel;
            case 7: return TaskType.Lost;
            case 8: return TaskType.Wait;
            case 9: return TaskType.None;
            case 10: return TaskType.CollectFromCob;
        }
        return TaskType.None;
    }

    public Task(GameData.TaskInfo info)
    {
        digPointId = info.digPointId;
        foodId = info.foodId;
        pos = info.pos.ToVector3();
        type = IndexToType(info.typeIndex);
        
        path = new();
        foreach (var surfaceInfo in info.path)
        {
            path.Add(new(surfaceInfo));
            CubePaths.DrawSurface(path.Last(), Color.yellow, 6);
        }
    }

}
