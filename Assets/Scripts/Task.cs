using System.Collections.Generic;
using System.Linq;
using FluentBehaviourTree;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering;

public enum TaskType
{
    Explore,
    Dig,
    GetCorn,
    GetEgg,
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
    public int itemId { get; set; }
    public Vector3 pos { get; set; }
    public TaskType type;

    //Queue de todos los tasks que quedan por hacer
    public static Queue<Task> ToDo = new();

    public List<CubePaths.CubeSurface> path = new(); //Path the ant will follow if in followingPath mode


    public Task(GameObject newGameObject, TaskType newType, List<CubePaths.CubeSurface> newPath)
    {
        type = newType;
        if (type == TaskType.Dig) digPointId = Vector3Int.RoundToInt(newGameObject.transform.position);
        else if (type == TaskType.GetCorn) itemId = newGameObject.GetComponent<Corn>().id;
        else if (type == TaskType.GetEgg) itemId = newGameObject.GetComponent<Ant>().id;
        else if (type == TaskType.CollectFromCob) itemId = newGameObject.GetComponent<CornCob>().id;
        else Debug.Log("WRONG TASKTYPE");
        pos = newGameObject.transform.position;
        path = newPath;
    }

    private Task()
    {

    }

    public static Task DigTask(Vector3Int digPointId, int antId, List<CubePaths.CubeSurface> newPath)
    {
        if (!DigPoint.digPointDict.ContainsKey(digPointId)) return Task.NoTask();
        if (DigPoint.digPointDict[digPointId].digPoint == null) return Task.NoTask();

        Task newTask = new();
        newTask.type = TaskType.Dig;
        newTask.digPointId = digPointId;
        newTask.pos = digPointId;
        newTask.path = newPath;

        DigPoint.digPointDict[digPointId].antId = antId;

        return newTask;
    }

    public static Task GetCornTask(int cornId, int antId, List<CubePaths.CubeSurface> newPath)
    {
        if (!Corn.cornDictionary.ContainsKey(cornId)) return Task.NoTask();

        Corn.cornDictionary[cornId].antId = antId;

        Task newTask = new();
        newTask.type = TaskType.GetCorn;
        newTask.itemId = cornId;
        newTask.pos = Corn.cornDictionary[cornId].transform.position;
        newTask.path = newPath;

        return newTask;
    }

    public static Task GetEggTask(int eggAntId, int antId, List<CubePaths.CubeSurface> newPath)
    {
        if (!Ant.antDictionary.ContainsKey(eggAntId)) return Task.NoTask();

        Ant.antDictionary[eggAntId].antId = antId;

        Task newTask = new();
        newTask.type = TaskType.GetCorn;
        newTask.itemId = eggAntId;
        newTask.pos = Ant.antDictionary[eggAntId].transform.position;
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

    public static Task GoToAnyChamber(CubePaths.CubeSurface antSurface)
    {
        Task GOtask = new();
        
        if (Nest.GetPointInAnyChamber(out Vector3 point))
        {
            if (CubePaths.GetKnownPathToPoint(antSurface, point, 1, out GOtask.path))
            {
                //Debug.Log("Going to specific point boss");
                return GOtask;
            }
        }

        Debug.Log("No chambers");

        return LostTask(antSurface, Vector3.up);
    }


    public static Task GoToNestPartTask(CubePaths.CubeSurface antSurface, NestPart.NestPartType type)
    {
        Task GOtask = new();
        switch (type)
        {
            case NestPart.NestPartType.Outside:
                GOtask.type = TaskType.GoOutside;
                break;
            case NestPart.NestPartType.Tunnel:
                GOtask.type = TaskType.GoToTunnel;
                break;
            default:
                GOtask.type = TaskType.GoToChamber;
                break;
        }

        if (GOtask.type == TaskType.GoToChamber)
        {
            if (Nest.GetPointInChamber(type, out Vector3 point))
            {
                if (CubePaths.GetKnownPathToPoint(antSurface, point, 1, out GOtask.path))
                {
                    //Debug.Log("Going to specific point boss");
                    return GOtask;
                }
            }

        }
        else
            if (CubePaths.GetKnownPathToMapPart(antSurface, type, out GOtask.path))
            return GOtask;

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
        Debug.Log("JUst got the lost task bro....");

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

    public static Task WaitTask(AntQueen antQueen, int time)
    {

        Debug.Log("Got a waiting task zzzz");
        Task waitTask = new()
        {
            type = TaskType.Wait,
            path = new()
        };

        antQueen.Counter = time;
        return waitTask;
    }

    public bool isTaskType(TaskType checkType) { return checkType == type; }
    public GameObject GetItem()
    {
        if (isTaskType(TaskType.GetCorn))
        {
            if (Corn.cornDictionary.TryGetValue(itemId, out Corn corn))
                return corn.gameObject;
        }
        else if (isTaskType(TaskType.CollectFromCob))
        {
            if (CornCob.cornCobDictionary.TryGetValue(itemId, out CornCob cob))
                return cob.gameObject;
        }
        else if (isTaskType(TaskType.GetEgg))
        {
            if (Ant.antDictionary.TryGetValue(itemId, out Ant ant))
                if (ant.age < 100)
                    return ant.gameObject;
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
                    if (cornAnt.objective.itemId == cornScript.id)
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

    public static bool IsEggBeingPickedUp(Ant antScript)
    {
        //Mirar si ya lo va a recoger otra hormiga
        if (antScript.antId != -1)
        {
            if (Ant.antDictionary.TryGetValue(antScript.antId, out Ant otherAnt))
            {
                if (otherAnt.objective.isTaskType(TaskType.GetEgg))
                    if (otherAnt.objective.itemId == antScript.id)
                        return true;
            }
        }
        return false;
    }
    public static bool IsEggBeingPickedUp(int eggId)
    {
        if (!Ant.antDictionary.TryGetValue(eggId, out Ant antScript)) return true;

        return IsEggBeingPickedUp(antScript);
    }


    public static bool IsDigPointBeingDug(Vector3Int digPointPos)
    {
        //i mean if it doens't exist it mught as well be dug right???
        if (!DigPoint.digPointDict.TryGetValue(digPointPos, out DigPoint.digPointData digPoint)) return true;

        int digPointsAntId = digPoint.antId;
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


    public bool isValid(ref Task objective)
    {
        switch (type)
        {
            case TaskType.Dig:
                if (!DigPoint.digPointDict.ContainsKey(digPointId))
                {
                    //If the objective is not valid, the ant loses it.
                    objective = Task.NoTask();
                    return false;
                }
                break;
            case TaskType.GetCorn:
                GameObject foodObj = GetItem();
                //if the food item no longer exists
                if (foodObj == null)
                {
                    objective = Task.NoTask();
                    return false;
                }
                //To check if held by ant, just see if held, it is not by corncob.
                bool heldByAnt = false;
                if (foodObj.transform.parent != null)
                    if (foodObj.transform.parent.GetComponent<CornCob>() == null)
                        heldByAnt = true;
                // if it has moved somehow or has been picked up
                if (Vector3.Distance(GetItem().transform.position, pos) > 0.5f || heldByAnt)
                {
                    //If the objective is not valid, the ant loses it.
                    objective = Task.NoTask();
                    return false;
                }
                break;
            case TaskType.CollectFromCob:
                //if the cornCob no longer exists:
                if (GetItem() == null)
                {
                    objective = Task.NoTask();
                    return false;
                }
                // if it has moved somehow or has no more corn.
                if (Vector3.Distance(GetItem().transform.position, pos) > 0.5f || !GetItem().GetComponent<CornCob>().hasCorn())
                {
                    //If the objective is not valid, the ant loses it.
                    objective = Task.NoTask();
                    return false;
                }
                break;
            case TaskType.GetEgg:
                GameObject antObj = GetItem(); //returns null if the ant age isn't lower than 100
                if (antObj == null)
                {
                    objective = Task.NoTask();
                    return false;
                }

                Ant script = antObj.GetComponent<Ant>();//Check if actually an ant

                if (script == null)
                {
                    objective = Task.NoTask();
                    return false;
                }
                
                if (script.IsBeingHeld())  //Check if being held
                if (script == null)
                {
                    objective = Task.NoTask();
                    return false;
                }
                
                
                if (Vector3.Distance(GetItem().transform.position, pos) > 0.5f)// if it has moved somehow
                {
                    //If the objective is not valid, the ant loses it.
                    objective = Task.NoTask();
                    return false;
                }

                break;
        }
        return true;
    }

    //Calculates path again. To be used when a path has changed. If it can't find a path, returns failure and sets given task to none
    public static bool RecalculateTaskPath(CubePaths.CubeSurface antSurface, ref Task objective)
    {
        switch (objective.type)
        {
            case TaskType.Dig:
                if (CubePaths.GetKnownPathToPoint(antSurface, objective.pos, Ant.digPointDistance, out objective.path))
                {
                    return true;
                }
                else
                {
                    objective = Task.NoTask();
                    return false;
                }
            case TaskType.CollectFromCob:
                if (CubePaths.GetKnownPathToPoint(antSurface, objective.pos, Ant.cornCobDistance, out objective.path))
                {
                    return true;
                }
                else
                {
                    objective = Task.NoTask();
                    return false;
                }
            default:
                objective = Task.NoTask();
                return false;
        }
    }




    public Vector3 getPos() { return pos; }

    public string TaskToString()
    {
        switch (type)
        {
            case TaskType.Explore: return "Go to pos";
            case TaskType.Dig: return "Dig point";
            case TaskType.GetCorn: return "Get food";
            case TaskType.GetEgg: return "Get egg";
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
            case TaskType.GetEgg: return 3;
            case TaskType.GoOutside: return 4;
            case TaskType.GoInside: return 5;
            case TaskType.GoToChamber: return 6;
            case TaskType.GoToTunnel: return 7;
            case TaskType.Lost: return 8;
            case TaskType.Wait: return 9;
            case TaskType.None: return 10;
            case TaskType.CollectFromCob: return 11;
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
            case 3: return TaskType.GetEgg;
            case 4: return TaskType.GoOutside;
            case 5: return TaskType.GoInside;
            case 6: return TaskType.GoToChamber;
            case 7: return TaskType.GoToTunnel;
            case 8: return TaskType.Lost;
            case 9: return TaskType.Wait;
            case 10: return TaskType.None;
            case 11: return TaskType.CollectFromCob;
        }
        return TaskType.None;
    }

    public Task(GameData.TaskInfo info)
    {
        digPointId = info.digPointId;
        itemId = info.foodId;
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
