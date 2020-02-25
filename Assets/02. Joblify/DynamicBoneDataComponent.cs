using UnityEngine;
using Unity.Entities;
using System.Collections.Generic;
using Unity.Collections;

public class DynamicBoneDataComponent : MonoBehaviour, IConvertGameObjectToEntity
{
    public Transform parent = null;
    [Range(0, 1)] public float inertia = 0.5f;
    [Range(0, 1)] public float damping = 0.2f;
    [Range(0, 1)] public float elasticity = 0.05f;
    [Range(0, 1)] public float stiffness = 0.7f;

    public List<Transform> m_particleTransforms = new List<Transform>();

    [ContextMenu("Generate")]
    private void Generate()
    {
        ParticleDataComponent[] components = GetComponentsInChildren<ParticleDataComponent>(true);
        for(int i = 0; i < components.Length; i++)
        {
            DestroyImmediate(components[i]);
        }

        SetupParticles();
        
        for (int i = 0; i < m_particleTransforms.Count; i++)
        {
            ParticleDataComponent particleDataComponent = m_particleTransforms[i].gameObject.AddComponent<ParticleDataComponent>();
            particleDataComponent.parentIndex = i - 1;
            particleDataComponent.inertia = inertia;
            particleDataComponent.damping = damping;
            particleDataComponent.elasticity = elasticity;
            particleDataComponent.stiffness = stiffness;
        }
    }

    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        for (int i = 0; i < m_particleTransforms.Count; i++)
        {
            ParticleDataComponent particleDataComponent = m_particleTransforms[i].gameObject.GetComponent<ParticleDataComponent>();
            particleDataComponent.dynamicBoneEntity = entity;
            particleDataComponent.parentIndex = i - 1;
            particleDataComponent.inertia = inertia;
            particleDataComponent.damping = damping;
            particleDataComponent.elasticity = elasticity;
            particleDataComponent.stiffness = stiffness;
        }

        dstManager.AddComponentData(entity, new DynamicBoneData()
        {
            objectPrevPosition = transform.position,
            inertia = inertia,
            damping = damping,
            elasticity = elasticity,
            stiffness = stiffness,
        });
    }

    private void SetupParticles()
    {
        m_particleTransforms.Clear();

        if (parent == null)
        {
            return;
        }

        damping = Mathf.Clamp01(damping);
        elasticity = Mathf.Clamp01(elasticity);
        stiffness = Mathf.Clamp01(stiffness);
        inertia = Mathf.Clamp01(inertia);

        AppendParticles(parent, -1);
    }

    private void AppendParticles(Transform trans, int parentIndex)
    {
        if (trans.GetComponent<Renderer>() != null)
        {
            return;
        }

        m_particleTransforms.Add(trans);

        int nextParentIndex = m_particleTransforms.Count - 1;
        for (int i = 0; i < trans.childCount; i++)
        {
            AppendParticles(trans.GetChild(i), nextParentIndex);
        }
    }
}
