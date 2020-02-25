using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

public class SineMovementSystem : JobComponentSystem
{
    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        float time = UnityEngine.Time.time;

        return Entities.ForEach((ref Translation translation, in SineMovementData sineMovementData) =>
        {
            float3 position = sineMovementData.originalPosition;
            position.z += math.sin(time * sineMovementData.moveSpeed) * 0.5f;
            translation.Value = position;
        }).Schedule(inputDeps);
    }
}
