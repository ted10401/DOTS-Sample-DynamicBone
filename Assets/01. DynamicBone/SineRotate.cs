using UnityEngine;

public class SineRotate : MonoBehaviour
{
    [SerializeField] private float m_rotateSpeed = 45f;
    private Transform m_transform;
    private Vector3 m_originalLocalEulerAngle;
    private Vector3 m_curLocalEulerAngle;

    private void Awake()
    {
        m_transform = transform;
        m_originalLocalEulerAngle = m_transform.localEulerAngles;
    }

    private void Update()
    {
        m_curLocalEulerAngle = m_originalLocalEulerAngle;
        m_curLocalEulerAngle.x = Mathf.Sin(Time.time * m_rotateSpeed) * 45f;
        m_transform.localEulerAngles = m_curLocalEulerAngle;
    }
}
