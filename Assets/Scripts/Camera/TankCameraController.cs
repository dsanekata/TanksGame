using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TankCameraController : MonoBehaviour
{
    [HideInInspector] public GameObject m_CameraRig;
    [HideInInspector] public GameObject m_Turret;

    // Start is called before the first frame update
    void Start()
    {
        m_CameraRig.transform.position = m_Turret.transform.position;
        m_CameraRig.transform.rotation = m_Turret.transform.rotation;
    }

    // Update is called once per frame
    void Update()
    {

    }

    void FixedUpdate()
    {
        m_CameraRig.transform.position = Vector3.Lerp(m_CameraRig.transform.position, m_Turret.transform.position, 0.3f);
        m_CameraRig.transform.rotation = Quaternion.Lerp(m_CameraRig.transform.rotation, m_Turret.transform.rotation, 0.3f);
    }
}
