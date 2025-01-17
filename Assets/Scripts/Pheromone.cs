using UnityEngine;
namespace pheromoneClass
{

    public class Pheromone
    {
        public int pathId;
        public int age;
        public Vector3Int pos;
        private Pheromone previous;
        private Pheromone next;
        public bool aux = false;
        public Vector3 surfaceDir;
        private bool isLast = false;
        private bool isFirst = false;

        public Pheromone(int pathId, Pheromone previous, Vector3Int pos, Vector3 dir){
            this.pathId = pathId;
            this.previous = previous;
            previous.next = this;
            this.pos = pos;
            surfaceDir = dir;
            age = 100;
            isFirst = false;
            isLast = true;
        }

        public Pheromone(int pathId, Vector3Int pos, Vector3 dir){
            this.pathId = pathId;
            previous = null;
            this.pos = pos;
            surfaceDir = dir;
            age = 100;
            isFirst = true;
            isLast = true;
        }

        //Obtiene la siguiente pheromona del camino.
        //Si este no existe, devuelve falso.
        //Si existe, la retorna mediante out y devuelve falso
        public bool GetNext(bool forwards, out Pheromone next){
            if (forwards)
            {
                Debug.Log("getnext return true?");
                next = this.next;
                if (isLast) return false;
                Debug.Log("Yes");
                return true;
            }
            else
            {
                Debug.Log("getPREV return true?");
                next = this.previous;
                if (isFirst) return false;
                Debug.Log("YES");
                return true;
            }
        }
        //
        public void SetNext(Pheromone next)
        {
            if (next == this) return;
            if (next == null)
            {
                this.next = null;
                isLast = true;
                return;
            }
            this.next = next;
            isLast = false;
        }
        public void SetPrevious(Pheromone prev)
        {
            if (prev == this) return;
            if (prev == null)
            {
                this.previous = null;
                isFirst = true;
                return;
            }
            previous = prev;
            isFirst = false;
        }
        //El deconstructor se asegura que los nodos siguiente y previo, 
        //si le tienen a él mismo como previo y siguiente respectivamente,
        //se pongan a null
        ~Pheromone(){
            if (previous != null) if (previous.next == this) previous.SetNext(null);
            if (next != null) if (next.previous == this) next.SetPrevious(null);
        }


        public void ShowPath(){
            bool reachedStart = false;
            if (previous == null) reachedStart = true;
            if (reachedStart){
                if (next != null)
                {
                    Debug.DrawLine(pos, next.pos, Color.green, 100000);
                    next.ShowPath(true);
                }
                return;
            }
            else
            {
                previous.ShowPath(false);
            }
        }
        public void ShowPath(bool reachedStart){
            if (previous == null) reachedStart = true;
            if (reachedStart){
                if (next != null)
                {
                    Debug.DrawLine(pos, next.pos, Color.green, 100000);
                    next.ShowPath(true);
                }
                return;
            }
            else
            {
                previous.ShowPath(false);
            }
        }
        
        public bool IsEnd(bool movingForward)
        {
            if (movingForward && isLast) return true;
            if (!movingForward && isFirst) return true;
            return false;
        }

    }


}