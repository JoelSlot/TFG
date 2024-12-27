using UnityEngine;
namespace pheromoneClass
{

    public class Pheromone
    {
        public int pathId;
        public int age;
        public Vector3Int pos;
        public Pheromone previous;
        public Pheromone next;

        public Pheromone(int pathId, Pheromone previous, Vector3Int pos){
            this.pathId = pathId;
            this.previous = previous;
            previous.next = this;
            this.pos = pos;
            age = 100;
        }

        public Pheromone(int pathId, Vector3Int pos){
            this.pathId = pathId;
            previous = null;
            this.pos = pos;
            age = 100;
        }

        //Obtiene la siguiente pheromona del camino.
        //Si este no existe, devuelve falso.
        //Si existe, la retorna mediante out y devuelve falso
        public bool getNext(out Pheromone next){
            next = this.next;
            if (this.next == null) return false;
            return true;
        }
        //Obtiene la previa pheromona del camino.
        //Si este no existe, devuelve falso.
        //Si existe, la retorna mediante out y devuelve falso
        public bool getPrevious(out Pheromone prev){
            prev = this.previous;
            if (this.previous == null) return false;
            return true;
        }
        //El deconstructor se asegura que los nodos siguiente y previo, 
        //si le tienen a él mismo como previo y siguiente respectivamente,
        //se pongan a null
        ~Pheromone(){
            if (previous != null) if (previous.next == this) previous.next = null;
            if (next != null) if (next.previous == this) next.previous = null;
        }

        public void showPath(bool reachedStart){
            if (previous == null) reachedStart = true;
            if (reachedStart){
                if (next != null)
                {
                    Debug.DrawLine(pos, next.pos, Color.green, 100000);
                    next.showPath(true);
                }
                return;
            }
            else
            {
                previous.showPath(false);
            }
        }

    }
}