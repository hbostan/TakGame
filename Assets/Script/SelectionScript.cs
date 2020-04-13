using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SelectionScript : MonoBehaviour
{
  MeshRenderer mesh_renderer;
  // Start is called before the first frame update
  void Awake()
  {
    mesh_renderer = GetComponent<MeshRenderer>();
    SetValid(false);
  }

  public void SetActive(bool active) {
    if(active) {
      gameObject.SetActive(true);
    } else {
      gameObject.SetActive(false);
    }
  }

  public bool GetActive() {
    return gameObject.activeSelf;
  }

  public void SetValid(bool valid) {
    if(valid) {
      mesh_renderer.material.color = Color.green;
    } else {
      mesh_renderer.material.color = Color.red;
    }
  }

  // Update is called once per frame
  void Update()
  {
    transform.Rotate(0, Time.deltaTime*15, 0);
  }
}
