using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Piece : MonoBehaviour {
  public string color;
  public bool isWall;
  public bool isCap;

  public void SetColor(string color) {
    this.color = color;
  }

  public void ToggleWall() {
    if(isWall) {
      transform.Rotate(90, 0, 0);
    } else {
      transform.Rotate(-90, 0, 0);
    }
    isWall = !isWall;
  }

  public bool IsCoverable(bool is_cap = false) {
    if(is_cap) {
      return true;
    }
    return !(isWall || isCap);
  }
}
