using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering;

public enum TaskType
{
    Explore,
    DigPoint,
    GetCorn,
    CollectFromCob,
    GoOutside,
    GoInside,
    GoToChamber,
    GoToTunnel,
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
        if (type == TaskType.DigPoint) digPointId = Vector3Int.RoundToInt(newGameObject.transform.position);
        else if (type == TaskType.GetCorn) foodId = newGameObject.GetComponent<Corn>().id;
        else if (type == TaskType.CollectFromCob) foodId = newGameObject.GetComponent<CornCob>().id;
        else Debug.Log("WRONG TASKTYPE");
        pos = newGameObject.transform.position;
        path = newPath;
    }

    private Task()
    {

    }

    public static Task GoOutsideTask(CubePaths.CubeSurface antSurface)
    {
        Task GOtask = new()
        {
            type = TaskType.GoOutside,
            path = new()
        };

        if (CubePaths.GetPathToMapPart(antSurface, NestPart.NestPartType.Outside, out GOtask.path)) return GOtask;

        Debug.Log("No valid path, didnt create GoOutSideTask");

        return NoTask();
    }

    public static Task GoInsideTask(CubePaths.CubeSurface antSurface)
    {
        Task GOtask = new()
        {
            type = TaskType.GoInside,
        };

        //Used foodchamber for now as default
        if (CubePaths.GetPathToMapPart(antSurface, NestPart.NestPartType.FoodChamber, out GOtask.path)) return GOtask;

        Debug.Log("No valid path");

        return NoTask();
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

        if (CubePaths.GetPathToMapPart(antSurface, type, out GOtask.path)) return GOtask;

        Debug.Log("No valid path");

        return NoTask();
    }

    public static Task ExploreTask(CubePaths.CubeSurface antSurface, Vector3 forward)
    {
        Task exploreTask = new()
        {
            type = TaskType.Explore,
        };
        
        //Used foodchamber for now as default
        if (CubePaths.GetExplorePath(antSurface, forward, out exploreTask.path)) return exploreTask;

        Debug.Log("No valid path");

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

    public bool isTaskType(TaskType checkType) { return checkType == type; }
    public GameObject GetFood()
    {
        if (isTaskType(TaskType.GetCorn))
            return Corn.cornDictionary[foodId].gameObject;
        else if (isTaskType(TaskType.CollectFromCob))
            return CornCob.cornCobDictionary[foodId].gameObject;
        else return null;
    }

    public DigPoint GetDigPoint()
    {
        if (isTaskType(TaskType.DigPoint))
            if (DigPoint.digPointDict.ContainsKey(digPointId))
                return DigPoint.digPointDict[digPointId].digPoint;

        return null;
    }


    public bool isValid(Ant ant)
    {
        switch (type)
        {
            case TaskType.DigPoint:
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
            case TaskType.DigPoint: return "Dig point";
            case TaskType.GetCorn: return "Get food";
            case TaskType.GoOutside: return "Go outside";
            case TaskType.GoInside: return "Go inside";
            case TaskType.GoToChamber: return "Go to chamber";
            case TaskType.GoToTunnel: return "Go to tunnel";
            case TaskType.None: return "None";
        }
        return "error";
    }

    public static int TypeToIndex(TaskType type)
    {
        switch (type)
        {
            case TaskType.Explore: return 0;
            case TaskType.DigPoint: return 1;
            case TaskType.GetCorn: return 2;
            case TaskType.GoOutside: return 3;
            case TaskType.GoInside: return 4;
            case TaskType.GoToChamber: return 5;
            case TaskType.GoToTunnel: return 6;
            case TaskType.None: return 7;
        }
        return -1;
    }

    public static TaskType IndexToType(int index)
    {
        switch (index)
        {
            case 0: return TaskType.Explore;
            case 1: return TaskType.DigPoint;
            case 2: return TaskType.GetCorn;
            case 3: return TaskType.GoOutside;
            case 4: return TaskType.GoInside;
            case 5: return TaskType.GoToChamber;
            case 6: return TaskType.GoToTunnel;
            case 7: return TaskType.None;
        }
        return TaskType.None;
    }

    public Task(GameData.TaskInfo info)
    {
        digPointId = info.digPointId;
        foodId = info.foodId;
        pos = info.pos.ToVector3();
        type = IndexToType(info.typeIndex);
    }

}
