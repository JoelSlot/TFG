using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering;

public enum TaskType
{
    GoToPos,
    DigPoint,
    GetFood, 
    GoOutside,
    GoInside,
    None
}

public class Task
    {
        public Vector3Int digPointId {get; set;}
        public int foodId {get; set;}
        public Vector3 pos {get; set;}
        public TaskType type;
        
        //Queue de todos los tasks que quedan por hacer
        public static Queue<Task> ToDo = new();

        public List<CubePaths.CubeSurface> path = new(); //Path the ant will follow if in followingPath mode

        public Task(int newFoodId)
        {
            foodId = newFoodId;
            pos = Corn.cornDictionary[foodId].transform.position;
            type = TaskType.GetFood;
        }

        public Task(Vector3Int newDigPointId)
        {
            digPointId = newDigPointId;
            pos = digPointId;
            type = TaskType.DigPoint;
        }

        public Task(Vector3 newPos)
        {
            pos = newPos;
            type = TaskType.GoToPos;
        }

        public Task(GameObject newGameObject, TaskType newType, List<CubePaths.CubeSurface> newPath)
        {
            type = newType;
            if (type == TaskType.DigPoint) digPointId = Vector3Int.RoundToInt(newGameObject.transform.position);
            else foodId = newGameObject.GetComponent<Corn>().id;
            pos = newGameObject.transform.position;
            path = newPath;
        }


        private Task()
        {

        }

        public static Task GoOutsideTask()
        {
            Task GOtask = new()
            {
                type = TaskType.GoOutside,
                path = new()
            };
            return GOtask;
        }

        public static Task GoInsideTask()
        {
            Task GOtask = new()
            {
                type = TaskType.GoInside,
                path = new()
            };
            return GOtask;
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

        public bool isTaskType(TaskType checkType) {return checkType == type;}
        public GameObject GetFood() {if (isTaskType(TaskType.GetFood)) return Corn.cornDictionary[foodId].gameObject; else return null;}
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
                case TaskType.GetFood:
                    //if the food item no longer exists
                    if (GetFood() == null)
                    {
                        ant.objective = Task.NoTask();
                        return false;
                    }
                    // if it has moved somehow or has been picked up
                    if (Vector3.Distance(GetFood().transform.position, pos) > 0.5f || GetFood().transform.parent != null)
                        {
                        //If the objective is not valid, the ant loses it.
                        ant.objective = Task.NoTask();
                        return false;
                        }
                    break;
            }
            return true;
        }

        public Vector3 getPos(){ return pos; }

        public string TaskToString()
        {
            switch (type)
            {
            case TaskType.GoToPos: return "Go to pos";
            case TaskType.DigPoint: return "Dig point";
            case TaskType.GetFood: return "Get food";
            case TaskType.GoOutside: return "Go outside";
            case TaskType.GoInside: return "Go inside";
            case TaskType.None: return "None";
            }
            return "error";
        }

        public static int TypeToIndex(TaskType type)
        {
            switch (type)
            {
                case TaskType.GoToPos: return 0;
                case TaskType.DigPoint: return 1;
                case TaskType.GetFood: return 2;
                case TaskType.GoOutside: return 3;
                case TaskType.GoInside: return 4;
                case TaskType.None: return 5;
            }
            return -1;
        }

        public static TaskType IndexToType(int index)
        {
            switch (index)
            {
                case 0: return TaskType.GoToPos;
                case 1: return TaskType.DigPoint;
                case 2: return TaskType.GetFood;
                case 3: return TaskType.GoOutside;
                case 4: return TaskType.GoInside;
                case 5: return TaskType.None;
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
