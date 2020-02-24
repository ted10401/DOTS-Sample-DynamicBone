using UnityEngine;
using System.Collections.Generic;

public class NewDynamicBone : MonoBehaviour
{
    public Transform root = null;
    [Range(0, 1)] public float inertia = 0.5f;
    [Range(0, 1)] public float damping = 0.2f;
    [Range(0, 1)] public float elasticity = 0.05f;
    [Range(0, 1)] public float stiffness = 0.7f;

    private Vector3 m_objectMove = Vector3.zero;
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

        if (root == null)
        {
            return;
        }

        damping = Mathf.Clamp01(damping);
        elasticity = Mathf.Clamp01(elasticity);
        stiffness = Mathf.Clamp01(stiffness);
        inertia = Mathf.Clamp01(inertia);

        m_objectPrevPosition = transform.position;
        AppendParticles(root, -1);
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
        ResetParticlesPosition();
    }

    private void ResetParticlesPosition()
    {
        for (int i = 0; i < m_particles.Count; ++i)
        {
            Particle p = m_particles[i];
            Transform pTrans = m_particleTransforms[i];

            if (pTrans != null)
            {
                p.position = p.prevPosition = pTrans.position;
            }
        }

        m_objectPrevPosition = transform.position;
    }

    private void OnDisable()
    {
        InitTransforms();
    }

    private void Update()
    {
        InitTransforms();
    }

    private void InitTransforms()
    {
        for (int i = 0; i < m_particles.Count; ++i)
        {
            Particle p = m_particles[i];
            Transform pTrans = m_particleTransforms[i];

            if (pTrans != null)
            {
                pTrans.localPosition = p.initLocalPosition;
                pTrans.localRotation = p.initLocalRotation;
            }
        }
    }

    private void LateUpdate()
    {
        UpdateDynamicBones();
    }
    
    private void UpdateDynamicBones()
    {
        if (root == null)
        {
            return;
        }
        
        m_objectMove = transform.position - m_objectPrevPosition;
        m_objectPrevPosition = transform.position;

        UpdateVerletIntegration();
        UpdateElasticityStiffness();

        ApplyParticlesToTransforms();
    }

    private void UpdateVerletIntegration()
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
                Vector3 inertiaVelocity = m_objectMove * inertia;
                Vector3 velocity = particle.position - particle.prevPosition;

                particle.prevPosition = particle.position + inertiaVelocity;
                particle.position = particle.prevPosition + velocity * (1 - damping);
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

            Matrix4x4 m0 = parentParticleTrans.localToWorldMatrix;
            m0.SetColumn(3, parentParticle.position);

            Vector3 practicalPosition = m0.MultiplyPoint3x4(particleTrans.localPosition);
            Vector3 deltaPosition = practicalPosition - particle.position;
            float deltaLength = deltaPosition.magnitude;

            if (elasticity > 0)
            {
                particle.position += deltaPosition * elasticity;
            }

            float practicalLength = (parentParticleTrans.position - particleTrans.position).magnitude;

            if (stiffness > 0)
            {
                float maxLength = practicalLength * 2 * (1 - stiffness);

                if (deltaLength > maxLength)
                {
                    float f = (deltaLength - maxLength) / deltaLength;
                    particle.position += deltaPosition * f;
                }
            }

            deltaPosition = parentParticle.position - particle.position;
            deltaLength = deltaPosition.magnitude;
            if (deltaLength > 0)
            {
                float f = (deltaLength - practicalLength) / deltaLength;
                particle.position += deltaPosition * f;
            }
        }
    }

    private void ApplyParticlesToTransforms()
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
