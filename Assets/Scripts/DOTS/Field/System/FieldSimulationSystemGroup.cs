using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

[UpdateInGroup(typeof(SimulationSystemGroup))]
public class FieldSimulationSystemGroup : ComponentSystemGroup
{    protected override void OnUpdate()
    {
        // Scene Active 되어졌을 때만 SystemBase 가 작동한다.
        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().isLoaded == false)
            return;

        base.OnUpdate();
    }
}
