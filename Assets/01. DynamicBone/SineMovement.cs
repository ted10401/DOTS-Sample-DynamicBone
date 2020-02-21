using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SineMovement : MonoBehaviour
{
    [SerializeField] private float m_moveSpeed = 1f;
    private Transform m_transform;
    private Vector3 m_originalPosition;

    private void Awake()
    {
        m_transform = transform;
        m_originalPosition = m_transform.position;
    }

    private void Update()
    {
        m_transform.position = m_originalPosition + Vector3.forward * Mathf.Sin(Time.time * m_moveSpeed) * 0.5f;
    }
}
