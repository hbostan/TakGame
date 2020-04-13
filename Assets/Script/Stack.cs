using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Stack : MonoBehaviour
{
  public Queue<Piece> pieces;
  public string controlling_player;
  public Piece controlling_piece;

  public void Awake() {
    pieces = new Queue<Piece>();
    controlling_player = "";
    controlling_piece = null;
  }
  public void AddTop(Piece p) {
    // Set parent of the piece and set piece transform according to tower height
    if(pieces.Count>0 && controlling_piece.isWall) {
      Queue<Piece> flattened = new Queue<Piece>();
      for(int i = 0; i < pieces.Count; i++) {
        Piece op = pieces.Dequeue();
        if(op.isWall) {
          op.ToggleWall();
          op.transform.position -= Vector3.up * 0.25f;
        }
        flattened.Enqueue(op);
      }
      pieces = flattened;
    }
    controlling_piece = p;
    controlling_player = p.color;
    pieces.Enqueue(p);
    p.transform.SetParent(this.transform);
    p.transform.position = this.transform.position + Vector3.up * (pieces.Count-1) * 0.1f;
    if(p.isWall) {
      p.transform.position += Vector3.up * 0.25f;
    }
  }

  public Piece PeekBottom() {
    if(pieces.Count != 0) {
      return pieces.Peek();
    }
    return null;
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

  public bool CanMoveOver(bool is_cap=false) {
    if(controlling_piece == null || controlling_piece.IsCoverable(is_cap)) {
      return true;
    }
    return false;
  }

  public bool IsRoad() {
    if(controlling_piece != null && !controlling_piece.isWall) {
      return true;
    }
    return false;
  }
}
