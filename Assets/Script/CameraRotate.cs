using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraRotate : MonoBehaviour
{
  public float move_speed = 300f;
  public float zoom_speed = 300f;
  public float min_distance = 2f;
  public float max_distance = 15f;

  void Update() {
    CameraControl();
    ZoomControl();
  }

  // TODO: Camera goes under board when moved with high speed, skipping the angle check
  void CameraControl() {
    if(Input.GetMouseButton(2)) {
      float angleX = (Input.GetAxisRaw("Mouse X") * Time.deltaTime) * move_speed;
      float angleY = -((Input.GetAxisRaw("Mouse Y") * Time.deltaTime) * move_speed);
      transform.RotateAround(Vector3.zero, Vector3.up, angleX);
      if(transform.eulerAngles.x >= 88 && angleY > 0) {
        transform.rotation.SetEulerAngles(88, transform.eulerAngles.y, transform.eulerAngles.z);
        return;
      }
      if(transform.eulerAngles.x <= 8 && angleY < 0) {
        transform.rotation.SetEulerAngles(8, transform.eulerAngles.y, transform.eulerAngles.z);
        return;
      }
      transform.RotateAround(Vector3.zero, transform.right, angleY);
    }
  }

  void ZoomControl() {
    float move_amount = (Input.GetAxis("Mouse ScrollWheel") * Time.deltaTime) * zoom_speed;
    if(Vector3.Distance(transform.position, Vector3.zero) <= min_distance && move_amount > 0f) {
      return;
    }
    if(Vector3.Distance(transform.position, Vector3.zero) >= max_distance && move_amount < 0f) {
      return;
    }
    transform.Translate(0f, 0f, move_amount, Space.Self);
  }
}
