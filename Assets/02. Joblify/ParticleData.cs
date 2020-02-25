using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public struct ParticleData : IComponentData
{
    public Entity dynamicBoneEntity;
    public int parentIndex;

    public float inertia;
    public float damping;
    public float elasticity;
    public float stiffness;

    public float3 position;
    public float3 prevPosition;
    public float3 initLocalPosition;
    public quaternion initLocalRotation;

    public ParticleData(Entity dynamicBoneEntity, int parentIndex, float inertia, float damping, float elasticity, float stiffness, Transform trans)
    {
        this.dynamicBoneEntity = dynamicBoneEntity;
        this.parentIndex = parentIndex;
        this.inertia = inertia;
        this.damping = damping;
        this.elasticity = elasticity;
        this.stiffness = stiffness;
        position = trans.position;
        prevPosition = position;
        initLocalPosition = trans.localPosition;
        initLocalRotation = trans.localRotation;
    }
}