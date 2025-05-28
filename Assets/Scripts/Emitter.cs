using UnityEngine;

public class Emitter : MonoBehaviour
{
    public ParticleSystem ps;
    private ParticleSystemRenderer pr;
    int counter = 0;
    int mode = 0;

    //Hay 8 modos:
    //x par, y par, z par
    //x par, y par, z impar
    //x par, y impar, z par
    //x par, y impar, z impar
    //x impar, y par, z par
    //x impar, y par, z impar
    //x impar, y impar, z par
    //x impar, y impar, z impar

    void Start()
    {
        pr = ps.GetComponent<ParticleSystemRenderer>();   
    }

    public void EmitPheromone(Vector3 pos, int age)
    {
        ParticleSystem.EmitParams ep = new ParticleSystem.EmitParams
        {
            position = pos,
            applyShapeToPosition = true, //Makes it not bunch up a lot.
            startSize = 10 + (50f * age / 100f) //40 to 60 wasn't very visible
        };

        ps.Emit(ep, 1);
    }

    void FixedUpdate()
    {
        if (!FlyCamera.cameraUnderground)
        {
            if (pr.renderMode != ParticleSystemRenderMode.Mesh)
            {
                ps.Play();
                pr.renderMode = ParticleSystemRenderMode.Mesh;
            }
            counter++;
            if (counter > 10)
            {
                foreach (var (pos, val) in CubePaths.cubePheromones)
                {
                    switch (mode)
                    {
                        case 0:
                            if (pos.x % 2 == 0)
                                if (pos.y % 2 == 0)
                                    if (pos.z % 2 == 0)
                                        EmitPheromone(pos, val);
                            break;
                        case 1:
                            if (pos.x % 2 == 0)
                                if (pos.y % 2 == 0)
                                    if (pos.z % 2 == 1)
                                        EmitPheromone(pos, val);
                            break;
                        case 2:
                            if (pos.x % 2 == 0)
                                if (pos.y % 2 == 1)
                                    if (pos.z % 2 == 0)
                                        EmitPheromone(pos, val);
                            break;
                        case 3:
                            if (pos.x % 2 == 0)
                                if (pos.y % 2 == 1)
                                    if (pos.z % 2 == 1)
                                        EmitPheromone(pos, val);
                            break;
                        case 4:
                            if (pos.x % 2 == 1)
                                if (pos.y % 2 == 0)
                                    if (pos.z % 2 == 0)
                                        EmitPheromone(pos, val);
                            break;
                        case 5:
                            if (pos.x % 2 == 1)
                                if (pos.y % 2 == 0)
                                    if (pos.z % 2 == 1)
                                        EmitPheromone(pos, val);
                            break;
                        case 6:
                            if (pos.x % 2 == 1)
                                if (pos.y % 2 == 1)
                                    if (pos.z % 2 == 0)
                                        EmitPheromone(pos, val);
                            break;
                        case 7:
                            if (pos.x % 2 == 1)
                                if (pos.y % 2 == 1)
                                    if (pos.z % 2 == 1)
                                        EmitPheromone(pos, val);
                            break;
                    }
                }
                mode++;
                if (mode > 7) mode = 0;
                counter = 0;
            }
        }
        else //Si se está debajo del suelo.
        {
            if (pr.renderMode != ParticleSystemRenderMode.None)
            {
                ps.Pause();
                pr.renderMode = ParticleSystemRenderMode.None;
            }
        }
    }

}

