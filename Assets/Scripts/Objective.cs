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
        public GameObject GetFood() {if (isFood()) return food; else return null;}
        public bool isDigPoint() {return Type.DigPoint == type;}
        public DigPoint GetDigPoint() {if (isDigPoint()) return digPoint; else return null;}
        public bool isPos() {return Type.Pos == type;}


        public bool stillValid()
        {
            switch (type)
            {
                case Type.DigPoint:
                    if (digPoint  == null)
                        return false;
                    break;
                case Type.Food:
                    if (Vector3.Distance(food.transform.position, pos) > 0.5f)
                        return false;
                    break;
                case Type.Pos:
                    return true;
            }
            return true;
        }

        public Vector3 getPos(){ return pos; }
    }
