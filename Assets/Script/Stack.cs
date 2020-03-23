using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Stack : MonoBehaviour
{
  public Queue<Piece> pieces;
  public string controlling_player;

  public void Awake() {
    pieces = new Queue<Piece>();
    controlling_player = "";
  }
  public void AddTop(Piece p) {
    // Set parent of the piece and set piece transform according to tower height
    controlling_player = p.color;
    pieces.Enqueue(p);
    p.transform.SetParent(this.transform);
    p.transform.position = this.transform.position + Vector3.up * (pieces.Count-1) * 0.1f;
  }

  public Piece PopBottom() {
    if(pieces.Count != 0) {
      return pieces.Dequeue();
    }
    return null;
  }

  public int PieceCount() {
    return pieces.Count;
  }

  public void CopyFrom(Stack s) {
    int num_pieces = s.PieceCount();
    while(num_pieces > 0) {
      this.AddTop(s.PopBottom());
    }
  }

}
