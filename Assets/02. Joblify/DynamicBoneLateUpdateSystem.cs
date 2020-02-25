using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

[AlwaysSynchronizeSystem]
public class DynamicBoneLateUpdateSystem : JobComponentSystem
{
    private NativeList<Entity> m_dynamicBoneEntities;
    private NativeList<DynamicBoneData> m_dynamicBones;

    protected override void OnCreate()
    {
        m_dynamicBoneEntities = new NativeList<Entity>(Allocator.Persistent);
        m_dynamicBones = new NativeList<DynamicBoneData>(Allocator.Persistent);
    }

    protected override void OnDestroy()
    {
        m_dynamicBoneEntities.Dispose();
        m_dynamicBones.Dispose();
    }

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        var dynamicBoneEntities = m_dynamicBoneEntities;
        var dynamicBones = m_dynamicBones;

        dynamicBoneEntities.Clear();
        dynamicBones.Clear();

        Entities
            .WithoutBurst()
            .ForEach((Entity entity, ref DynamicBoneData dynamicBoneData, in Translation translation) =>
            {
                dynamicBoneEntities.Add(entity);
                dynamicBones.Add(dynamicBoneData);
                dynamicBoneData.objectInertia = translation.Value - dynamicBoneData.objectPrevPosition;
                dynamicBoneData.objectPrevPosition = translation.Value;
            }).Run();

        Entities.WithoutBurst()
            .ForEach((ref ParticleData particleData, in LocalToWorld localToWorld) =>
            {
                if(particleData.parentIndex < 0)
                {
                    particleData.prevPosition = particleData.position;
                    particleData.position = localToWorld.Value.c3.xyz;
                }
            }).Run();

        Entities.WithoutBurst()
            .ForEach((ref ParticleData particleData) =>
            {
                if (particleData.parentIndex >= 0)
                {
                    DynamicBoneData dynamicBoneData = default;
                    for(int i = 0; i < dynamicBoneEntities.Length; i++)
                    {
                        if(dynamicBoneEntities[i] == particleData.dynamicBoneEntity)
                        {
                            dynamicBoneData = dynamicBones[i];
                            break;
                        }
                    }

                    //Inertia
                    float3 particleInertia = dynamicBoneData.objectInertia * dynamicBoneData.inertia;
                    particleData.prevPosition += particleInertia;
                    particleData.position += particleInertia;

                    //Verlet Integration & Damping
                    float3 velocity = particleData.position - particleData.prevPosition;
                    particleData.prevPosition = particleData.position;
                    particleData.position += velocity * (1 - particleData.damping);
                }
            }).Run();

        //Entities.WithoutBurst()
        //    .ForEach((ref ParticleData particleData, in Parent parent, in Translation translation, in LocalToWorld localToWorld) =>
        //    {
        //        if (particleData.parentIndex < 0)
        //        {
        //            return;
        //        }

        //        float3 particleLocalPosition = translation.Value;
        //        float3 particlePosition = localToWorld.Value.c3.xyz;

        //        ParticleData parentParticleData = EntityManager.GetComponentData<ParticleData>(parent.Value);
        //        float4x4 parentLocalToWorldMatrix = EntityManager.GetComponentData<LocalToWorld>(parent.Value).Value;
        //        float3 parentPosition = parentLocalToWorldMatrix.c3.xyz;

        //        //Elasticity
        //        float4x4 matrix = parentLocalToWorldMatrix;
        //        matrix.c3.xyz = parentParticleData.position;
        //        float3 targetPosition = matrix.MultiplyPoint3x4(particleLocalPosition);
        //        float3 delta = targetPosition - particlePosition;
        //        particleData.position += delta * particleData.elasticity;

        //        //Stiffness
        //        float deltaLength = delta.magnitude();
        //        float length = (parentPosition - particlePosition).magnitude();
        //        float lengthMax = length * 2 * (1 - particleData.stiffness);
        //        if (deltaLength > lengthMax)
        //        {
        //            particleData.position += delta * (deltaLength - lengthMax) / deltaLength;
        //        }

        //        //Length Constraint
        //        delta = parentParticleData.position - particleData.position;
        //        deltaLength = delta.magnitude();
        //        particleData.position += delta * (deltaLength - length) / deltaLength;
        //    }).Run();

        Entities.WithoutBurst()
            .ForEach((ref LocalToWorld localToWorld, ref Translation translation, in Parent parent, in ParticleData particleData) =>
        {
            float4x4 parentLocalToWorldMatrix = EntityManager.GetComponentData<LocalToWorld>(parent.Value).Value;
            float3 parentWorldPosition = parentLocalToWorldMatrix.c3.xyz;
            translation.Value = particleData.position - parentWorldPosition;

            //translation.Value = particleData.position;
            //localToWorld.Value.c3.xyz = particleData.osition;
        }).Run();

        return default;
    }
}
