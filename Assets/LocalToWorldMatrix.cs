using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LocalToWorldMatrix : MonoBehaviour
{
    private void Update()
    {
        if(Input.GetKeyDown(KeyCode.Space))
        {
            Debug.LogFormat("name = {0}\nlocalToWorldMatrix = \n{1}\nlocalPosition = {2}\nlocalRotation = {3}\nposition = {4}\nrotation = {5}", name, transform.localToWorldMatrix, transform.localPosition, transform.localRotation, transform.position, transform.rotation);
        }
    }
}
