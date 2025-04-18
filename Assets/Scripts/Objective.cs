using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public enum TaskType
    {
        GoToPos,
        DigPoint,
        GetFood, 
        GoOutside,
        GoInside,
    }

public class Task
    {
        DigPoint digPoint;
        GameObject food;
        Vector3 pos;
        TaskType type;
        
        //Queue de todos los tasks que quedan por hacer
        public static Queue<Task> ToDo = new();

        public List<CubePaths.CubeSurface> path = new(); //Path the ant will follow if in followingPath mode

        public Task(GameObject newFood)
        {
            food = newFood;
            pos = newFood.transform.position;
            type = TaskType.GetFood;
        }

        public Task(DigPoint newDigPoint)
        {
            digPoint = newDigPoint;
            pos = digPoint.transform.position;
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
            if (type == TaskType.DigPoint) digPoint = newGameObject.GetComponent<DigPoint>();
            else food = newGameObject;
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

        public bool isTaskType(TaskType checkType) {return checkType == type;}
        public GameObject GetFood() {if (isTaskType(TaskType.GetFood)) return food; else return null;}
        public DigPoint GetDigPoint() {if (isTaskType(TaskType.DigPoint)) return digPoint; else return null;}


        public bool isValid(Ant ant)
        {
            switch (type)
            {
                case TaskType.DigPoint:
                    if (digPoint  == null)
                    {
                        //If the objective is not valid, the ant loses it.
                        ant.objective = null;
                        return false;
                    }
                    break;
                case TaskType.GetFood:
                    // if it has moved somehow or has been picked up
                    if (Vector3.Distance(food.transform.position, pos) > 0.5f || food.transform.parent != null)
                        {
                        //If the objective is not valid, the ant loses it.
                        ant.objective = null;
                        return false;
                        }
                    break;
            }
            return true;
        }

        public Vector3 getPos(){ return pos; }

    }
