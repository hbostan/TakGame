using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HighlightManager : MonoBehaviour {
  public static HighlightManager Instance {
    set;
    get;
  }
  public GameObject highlight_prefab;
  private List<GameObject> highlights;

  private void Start() {
    Instance = this;
    highlights = new List<GameObject>();
  }

  private GameObject GetHighlightObject() {
    GameObject obj = highlights.Find(o => !o.activeSelf);
    if(obj == null) {
      obj = Instantiate(highlight_prefab);
      highlights.Add(obj);
    }
    return obj;
  }

  public void HighlightMoves(List<Vector3> positions) {
    return;
    foreach(Vector3 pos in positions) {
      GameObject obj = GetHighlightObject();
      obj.SetActive(true);
      obj.transform.position = pos;
    }
  }

  public void RemoveHighlights() {
    return;
    foreach(GameObject obj in highlights) {
      obj.SetActive(false);
    }
  }
}
