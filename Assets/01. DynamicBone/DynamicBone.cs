using UnityEngine;
using System.Collections.Generic;

public class DynamicBone : MonoBehaviour
{
    public Transform parent = null;
    [Range(0, 1)] public float inertia = 0.5f;
    [Range(0, 1)] public float damping = 0.2f;
    [Range(0, 1)] public float elasticity = 0.05f;
    [Range(0, 1)] public float stiffness = 0.7f;

    private Vector3 m_objectInertia = Vector3.zero;
    private Vector3 m_objectPrevPosition = Vector3.zero;

    private class Particle
    {
        public int parentIndex = -1;

        public Vector3 position = Vector3.zero;
        public Vector3 prevPosition = Vector3.zero;
        public Vector3 initLocalPosition = Vector3.zero;
        public Quaternion initLocalRotation = Quaternion.identity;

        public Particle(int parentIndex, Transform trans)
        {
            this.parentIndex = parentIndex;
            position = trans.position;
            prevPosition = position;
            initLocalPosition = trans.localPosition;
            initLocalRotation = trans.localRotation;
        }
    }

    private List<Transform> m_particleTransforms = new List<Transform>();
    private List<Particle> m_particles = new List<Particle>();

    private void Awake()
    {
        SetupParticles();
    }

    private void SetupParticles()
    {
        m_particleTransforms.Clear();
        m_particles.Clear();

        if (parent == null)
        {
            return;
        }

        damping = Mathf.Clamp01(damping);
        elasticity = Mathf.Clamp01(elasticity);
        stiffness = Mathf.Clamp01(stiffness);
        inertia = Mathf.Clamp01(inertia);

        m_objectPrevPosition = transform.position;
        AppendParticles(parent, -1);
    }

    private void AppendParticles(Transform trans, int parentIndex)
    {
        if (trans.GetComponent<Renderer>() != null)
        {
            return;
        }

        m_particleTransforms.Add(trans);
        m_particles.Add(new Particle(parentIndex, trans));

        int nextParentIndex = m_particles.Count - 1;
        for (int i = 0; i < trans.childCount; i++)
        {
            AppendParticles(trans.GetChild(i), nextParentIndex);
        }
    }

    private void OnEnable()
    {
        ResetParticles();
    }

    private void ResetParticles()
    {
        for (int i = 0, count = m_particles.Count; i < count; i++)
        {
            Particle particle = m_particles[i];
            Transform particleTrans = m_particleTransforms[i];

            if (particleTrans != null)
            {
                particle.position = particle.prevPosition = particleTrans.position;
            }
        }

        m_objectPrevPosition = transform.position;
    }

    private void OnDisable()
    {
        ResetTransforms();
    }

    private void Update()
    {
        ResetTransforms();
    }

    private void ResetTransforms()
    {
        for (int i = 0, count = m_particles.Count; i < count; i++)
        {
            Particle particle = m_particles[i];
            Transform particleTrans = m_particleTransforms[i];

            if (particleTrans != null)
            {
                particleTrans.localPosition = particle.initLocalPosition;
                particleTrans.localRotation = particle.initLocalRotation;
            }
        }
    }

    private void LateUpdate()
    {
        UpdateDynamicBones();
    }
    
    private void UpdateDynamicBones()
    {
        if (parent == null)
        {
            return;
        }
        
        m_objectInertia = transform.position - m_objectPrevPosition;
        m_objectPrevPosition = transform.position;

        UpdateInertiaDamping();
        UpdateElasticityStiffness();
        UpdateTransforms();
    }

    private void UpdateInertiaDamping()
    {
        for (int i = 0, count = m_particles.Count; i < count; i++)
        {
            Particle particle = m_particles[i];
            Transform particleTrans = m_particleTransforms[i];

            if (particle.parentIndex < 0)
            {
                particle.prevPosition = particle.position;
                particle.position = particleTrans.position;
            }
            else
            {
                //Inertia
                Vector3 particleInertia = m_objectInertia * inertia;
                particle.prevPosition += particleInertia;
                particle.position += particleInertia;

                //Verlet Integration & Damping
                Vector3 velocity = particle.position - particle.prevPosition;
                particle.prevPosition = particle.position;
                particle.position += velocity * (1 - damping);
            }
        }
    }

    private void UpdateElasticityStiffness()
    {
        for(int i = 0, count = m_particles.Count; i < count; i++)
        {
            Particle particle = m_particles[i];
            if(particle.parentIndex < 0)
            {
                continue;
            }

            Transform particleTrans = m_particleTransforms[i];
            Particle parentParticle = m_particles[particle.parentIndex];
            Transform parentParticleTrans = m_particleTransforms[particle.parentIndex];

            //Elasticity  
            Matrix4x4 m0 = parentParticleTrans.localToWorldMatrix;
            m0.SetColumn(3, parentParticle.position);
            Vector3 targetPosition = m0.MultiplyPoint3x4(particleTrans.localPosition);
            Vector3 delta = targetPosition - particle.position;
            particle.position += delta * elasticity;

            //Stiffness  
            float deltaLength = delta.magnitude;
            float length = (parentParticleTrans.position - particleTrans.position).magnitude;
            float lengthMax = length * 2 * (1 - stiffness);
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

    private void UpdateTransforms()
    {
        for (int i = 1, count = m_particles.Count; i < count; ++i)
        {
            Particle particle = m_particles[i];
            Transform particleTrans = m_particleTransforms[i];

            Particle parentParticle = m_particles[particle.parentIndex];
            Transform parentParticleTrans = m_particleTransforms[particle.parentIndex];

            Vector3 v = particleTrans.localPosition;
            Quaternion rot = Quaternion.FromToRotation(parentParticleTrans.TransformDirection(v), particle.position - parentParticle.position);
            parentParticleTrans.rotation = rot * parentParticleTrans.rotation;

            particleTrans.position = particle.position;
        }
    }
}
