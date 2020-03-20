using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TakBoard : MonoBehaviour
{
  public Piece[,] pieces = new Piece[5, 5];
  public GameObject white_piece_prefab;
  public GameObject black_piece_prefab;
  private Vector3 board_offset = new Vector3(-2.5f, 0f, -2.5f);
  private Vector3 piece_offset = new Vector3(0.5f, 0.15f, 0.5f);
  private bool alternate = true;
  private Piece selected_piece;
  private Vector2Int startDrag;
  private Vector2 endDrag;
  private Vector2Int mouse_coord;

  private Vector3 GetWorldCoord(int x, int y) {
    return (Vector3.right * x) + (Vector3.forward * y) + board_offset + piece_offset;
  }

  private void GenerateBoard() {
    for(int j=0; j<5; j+=4) {
      for(int i=0; i<5; i++) {
        InstantiatePiece(i, j);
      }
    }
  }

  private void InstantiatePiece(int x, int y) {
    GameObject obj = Instantiate(alternate? white_piece_prefab:black_piece_prefab) as GameObject;
    alternate = !alternate;
    obj.transform.SetParent(this.transform);
    pieces[x,y] = obj.GetComponent<Piece>();
    MovePiece(pieces[x, y], x, y);
  }

  private void MovePiece(Piece p, int x, int y) {
    p.transform.position = GetWorldCoord(x, y);
  }

  private void UpdateMouseCoord() {
    RaycastHit hit;
    if (Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out hit, 25.0f, LayerMask.GetMask("Board"))) {
      Vector3 hit_point = hit.point - board_offset;
      mouse_coord.x = (int)hit_point.x;
      mouse_coord.y = (int)hit_point.z;
    } else {
      mouse_coord.x = -1;
      mouse_coord.y = -1;
    }
  }

  private void SelectPiece(int x, int y) {
    if(x < 0 || x >= pieces.Length || y < 0 || y >= pieces.Length) {
      // Don't select any piece;
      return;
    }
    Piece p = pieces[x, y];
    if(p != null) {
      selected_piece = p;
      startDrag = mouse_coord;
      p.transform.position += Vector3.up * 1f;
      Debug.Log(selected_piece.name);
    }
  }

  private void UpdatePieceDrag(Piece p) {
    if(p == null) {
      return;
    }
    RaycastHit hit;
    if(Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out hit, 25.0f, LayerMask.GetMask("Board"))) {
      p.transform.position = hit.point + Vector3.up * 2f;
    }
  }

  private bool OutOfBounds(int coord) {
    if(coord < 0 || coord > pieces.Length) {
      return true;
    }
    return false;
  }

  private void TryMove(int startX, int startY, int endX, int endY) {
    
    startDrag = new Vector2Int(startX, startY);
    endDrag = new Vector2Int(endX, endY);
    selected_piece = pieces[startX, startY];
    // Check empty;
    if(selected_piece != null) {
      if(!OutOfBounds(endX) &&
         !OutOfBounds(endY) &&
         (startX != endX || startY != endY) &&
         pieces[endX, endY] == null) {
        Debug.Log("Here");
        pieces[startX, startY] = null;
        MovePiece(selected_piece, endX, endY);
        pieces[endX, endY] = selected_piece;
        selected_piece = null;
        return;
      }
      MovePiece(selected_piece, startX, startY);
      startDrag = Vector2Int.zero;
      endDrag = Vector2Int.zero;
      selected_piece = null;
    }
  }

  private void Start() {
    GenerateBoard();
  }

  private void Update() {
    UpdateMouseCoord();
    //if(selected_piece != null) {
    //  UpdatePieceDrag(selected_piece);
    //}

    if(Input.GetMouseButtonDown(0)) {
      if(selected_piece != null) {
        TryMove(startDrag.x, startDrag.y, mouse_coord.x, mouse_coord.y);
        selected_piece = null;
      } else {
        SelectPiece(mouse_coord.x, mouse_coord.y);
      }
      
    }
    //if(Input.GetMouseButtonUp(0)) {
    //  TryMove(startDrag.x, startDrag.y, mouse_coord.x, mouse_coord.y);
    //}
  }

}
