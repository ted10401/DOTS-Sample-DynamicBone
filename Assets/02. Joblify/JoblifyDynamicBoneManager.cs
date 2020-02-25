using System.Collections.Generic;
using UnityEngine;

public class JoblifyDynamicBoneManager : Singleton<JoblifyDynamicBoneManager>
{
    private List<JoblifyDynamicBone> m_joblifyDynamicBones = new List<JoblifyDynamicBone>();

    public void Register(JoblifyDynamicBone joblifyDynamicBone)
    {
        if(m_joblifyDynamicBones.Contains(joblifyDynamicBone))
        {
            return;
        }
        
        m_joblifyDynamicBones.Add(joblifyDynamicBone);
    }

    public void Unregister(JoblifyDynamicBone joblifyDynamicBone)
    {
        m_joblifyDynamicBones.Remove(joblifyDynamicBone);
    }

    private void Update()
    {
        ResetTransforms();
    }

    private void ResetTransforms()
    {
        for (int i = 0; i < m_joblifyDynamicBones.Count; i++)
        {
            JoblifyDynamicBone joblifyDynamicBone = m_joblifyDynamicBones[i];

            for (int j = 0, count = joblifyDynamicBone.m_particles.Count; j < count; j++)
            {

                ResetTransform(joblifyDynamicBone.m_particles[j]);
            }
        }
    }

    private void ResetTransform(JoblifyDynamicBone.Particle particle)
    {
        if(particle.trans == null)
        {
            return;
        }

        particle.trans.localPosition = particle.initLocalPosition;
        particle.trans.localRotation = particle.initLocalRotation;
    }

    private void LateUpdate()
    {
        UpdateDynamicBones();
    }

    private void UpdateDynamicBones()
    {
        for (int i = 0; i < m_joblifyDynamicBones.Count; i++)
        {
            JoblifyDynamicBone joblifyDynamicBone = m_joblifyDynamicBones[i];

            UpdateObjectInertia(joblifyDynamicBone);
            UpdateInertiaDamping(joblifyDynamicBone);
            UpdateElasticityStiffness(joblifyDynamicBone);
            UpdateTransforms(joblifyDynamicBone);
        }
    }

    private void UpdateObjectInertia(JoblifyDynamicBone joblifyDynamicBone)
    {
        joblifyDynamicBone.m_objectInertia = joblifyDynamicBone.transform.position - joblifyDynamicBone.m_objectPrevPosition;
        joblifyDynamicBone.m_objectPrevPosition = joblifyDynamicBone.transform.position;
    }

    private void UpdateInertiaDamping(JoblifyDynamicBone joblifyDynamicBone)
    {
        for (int i = 0, count = joblifyDynamicBone.m_particles.Count; i < count; i++)
        {
            JoblifyDynamicBone.Particle particle = joblifyDynamicBone.m_particles[i];

            if (particle.parentIndex < 0)
            {
                particle.prevPosition = particle.position;
                particle.position = particle.trans.position;
            }
            else
            {
                //Inertia
                Vector3 particleInertia = joblifyDynamicBone.m_objectInertia * joblifyDynamicBone.inertia;
                particle.prevPosition += particleInertia;
                particle.position += particleInertia;

                //Verlet Integration & Damping
                Vector3 velocity = particle.position - particle.prevPosition;
                particle.prevPosition = particle.position;
                particle.position += velocity * (1 - joblifyDynamicBone.damping);
            }
        }
    }

    private void UpdateElasticityStiffness(JoblifyDynamicBone joblifyDynamicBone)
    {
        for (int i = 0, count = joblifyDynamicBone.m_particles.Count; i < count; i++)
        {
            JoblifyDynamicBone.Particle particle = joblifyDynamicBone.m_particles[i];
            if (particle.parentIndex < 0)
            {
                continue;
            }

            Transform particleTrans = particle.trans;
            JoblifyDynamicBone.Particle parentParticle = joblifyDynamicBone.m_particles[particle.parentIndex];
            Transform parentParticleTrans = parentParticle.trans;

            //Elasticity  
            Matrix4x4 m0 = parentParticleTrans.localToWorldMatrix;
            m0.SetColumn(3, parentParticle.position);
            Vector3 targetPosition = m0.MultiplyPoint3x4(particleTrans.localPosition);
            Vector3 delta = targetPosition - particle.position;
            particle.position += delta * joblifyDynamicBone.elasticity;

            //Stiffness  
            float deltaLength = delta.magnitude;
            float length = (parentParticleTrans.position - particleTrans.position).magnitude;
            float lengthMax = length * 2 * (1 - joblifyDynamicBone.stiffness);
            if (deltaLength > lengthMax)
            {
                particle.position += delta * (deltaLength - lengthMax) / deltaLength;
            }

            //Length Constraint  
            delta = parentParticle.position - particle.position;
            deltaLength = delta.magnitude;
            particle.position += delta * (deltaLength - length) / deltaLength;
        }
    }

    private void UpdateTransforms(JoblifyDynamicBone joblifyDynamicBone)
    {
        for (int i = 1, count = joblifyDynamicBone.m_particles.Count; i < count; ++i)
        {
            JoblifyDynamicBone.Particle particle = joblifyDynamicBone.m_particles[i];
            Transform particleTrans = particle.trans;

            JoblifyDynamicBone.Particle parentParticle = joblifyDynamicBone.m_particles[particle.parentIndex];
            Transform parentParticleTrans = parentParticle.trans;

            Vector3 v = particleTrans.localPosition;
            Quaternion rot = Quaternion.FromToRotation(parentParticleTrans.TransformDirection(v), particle.position - parentParticle.position);
            parentParticleTrans.rotation = rot * parentParticleTrans.rotation;

            particleTrans.position = particle.position;
        }
    }
}
