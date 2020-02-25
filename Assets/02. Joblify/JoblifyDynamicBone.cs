using UnityEngine;
using System.Collections.Generic;

public class JoblifyDynamicBone : MonoBehaviour
{
    public Transform parent = null;
    [Range(0, 1)] public float inertia = 0.5f;
    [Range(0, 1)] public float damping = 0.2f;
    [Range(0, 1)] public float elasticity = 0.05f;
    [Range(0, 1)] public float stiffness = 0.7f;

    public Vector3 m_objectInertia = Vector3.zero;
    public Vector3 m_objectPrevPosition = Vector3.zero;

    public class Particle
    {
        public Transform trans;
        public int parentIndex = -1;

        public Vector3 position = Vector3.zero;
        public Vector3 prevPosition = Vector3.zero;
        public Vector3 initLocalPosition = Vector3.zero;
        public Quaternion initLocalRotation = Quaternion.identity;

        public Particle(int parentIndex, Transform trans)
        {
            this.trans = trans;
            this.parentIndex = parentIndex;
            position = trans.position;
            prevPosition = position;
            initLocalPosition = trans.localPosition;
            initLocalRotation = trans.localRotation;
        }
    }
    
    public List<Particle> m_particles = new List<Particle>();

    private void Awake()
    {
        SetupParticles();
    }

    private void SetupParticles()
    {
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
        
        m_particles.Add(new Particle(parentIndex, trans));

        int nextParentIndex = m_particles.Count - 1;
        for (int i = 0; i < trans.childCount; i++)
        {
            AppendParticles(trans.GetChild(i), nextParentIndex);
        }
    }

    private void OnEnable()
    {
        if (parent == null)
        {
            return;
        }

        JoblifyDynamicBoneManager.Instance.Register(this);
    }

    private void OnDisable()
    {
        JoblifyDynamicBoneManager.Instance?.Unregister(this);
    }
}
