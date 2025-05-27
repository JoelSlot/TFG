using UnityEngine;

public class Emitter : MonoBehaviour
{
    public ParticleSystem ps;

    public void EmitPheromone(Vector3 pos)
    {
        ParticleSystem.EmitParams ep = new ParticleSystem.EmitParams
        {
            position = pos,
            applyShapeToPosition = true //Makes it not bunch up a lot.
        };

        ps.Emit(ep, 1);
    }

    void Update()
    {
        foreach (var pos in CubePaths.cubePheromones.Keys)
        {
            ps.transform.position = pos;
            ps.Emit(1);
        }
    }

}

