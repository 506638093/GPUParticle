using UnityEngine;
using System.Collections;

public class CameraMove : MonoBehaviour
{
    public float sensitivityMouse = 2f;
    public float sensitivetyKeyBoard = 0.1f;
    public float sensitivetyMouseWheel = 10f;

    void Update()
    {
        if (Input.GetAxis("Mouse ScrollWheel") != 0)
        {
            this.GetComponent<Camera>().fieldOfView = this.GetComponent<Camera>().fieldOfView - Input.GetAxis("Mouse ScrollWheel") * sensitivetyMouseWheel;
        }
        if (Input.GetMouseButton(1))
        {
            transform.Rotate(-Input.GetAxis("Mouse Y") * sensitivityMouse, Input.GetAxis("Mouse X") * sensitivityMouse, 0);
        }

        if (Input.GetAxis("Horizontal") != 0)
        {
            transform.Translate(transform.right * Input.GetAxis("Horizontal") * sensitivetyKeyBoard);
        }
        if (Input.GetAxis("Vertical") != 0)
        {
            transform.Translate(transform.up * Input.GetAxis("Vertical") * sensitivetyKeyBoard);
        }
    }

}