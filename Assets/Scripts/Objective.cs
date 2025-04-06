using Unity.VisualScripting;
using UnityEngine;

public enum Type
    {
        Pos,
        DigPoint,
        Food
    }

public class Objective
    {
        DigPoint digPoint;
        GameObject food;
        Vector3 pos;
        Type type;


        public Objective(GameObject newFood)
        {
            food = newFood;
            pos = newFood.transform.position;
            type = Type.Food;
        }

        public Objective(DigPoint newDigPoint)
        {
            digPoint = newDigPoint;
            pos = digPoint.transform.position;
            type = Type.DigPoint;
        }

        public Objective(Vector3 newPos)
        {
            pos = newPos;
            type = Type.Pos;
        }

        public bool isFood() {return Type.Food == type;}
        public bool isDigPoint() {return Type.DigPoint == type;}
        public bool isPos() {return Type.Pos == type;}

        //Interacts with the objective.
        //- digpoint: it is dug
        //- food: it is picked up.
        //If the object is not present anymore, return false. Otherwise return true
        public bool interact(out GameObject toDestroy)
        {
            toDestroy = null;
            switch (type)
            {
                case Type.DigPoint:
                    if (digPoint.gameObject == null) return false;
                    digPoint.Dig();
                    toDestroy = digPoint.transform.gameObject;
                    return true;
                case Type.Food:
                    if (Vector3.Distance(food.transform.position, pos) > 0.5f)
                        return false;
                    toDestroy = food;
                    return true;
                case Type.Pos:
                    return true;
            }
            return false;
        }

        public Vector3 getPos(){ return pos; }
    }
