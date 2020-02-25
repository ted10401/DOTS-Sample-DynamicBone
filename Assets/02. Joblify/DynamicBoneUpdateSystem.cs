using Unity.Entities;
using Unity.Jobs;
using Unity.Transforms;

[AlwaysSynchronizeSystem]
[UpdateBefore(typeof(DynamicBoneLateUpdateSystem))]
public class DynamicBoneUpdateSystem : JobComponentSystem
{
    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        Entities.ForEach((ref Translation translation, in ParticleData particleData) =>
        {
            translation.Value = particleData.initLocalPosition;
        }).Run();

        Entities.ForEach((ref Rotation rotation, in ParticleData particleData) =>
        {
            rotation.Value = particleData.initLocalRotation;
        }).Run();

        return default;
    }
}
