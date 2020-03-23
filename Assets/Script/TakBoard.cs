using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TakBoard : MonoBehaviour
{
  public const int BOARD_SIZE = 5;
  public Stack[,] board = new Stack[BOARD_SIZE, BOARD_SIZE];
  public GameObject white_piece_prefab;
  public GameObject black_piece_prefab;
  public GameObject stack_prefab;
  private Vector3 board_offset = new Vector3(-2.5f, 0f, -2.5f);
  private Vector3 piece_offset = new Vector3(0.5f, 0.15f, 0.5f);
  private Stack selected_stack;
  private Vector2Int selected_coords;
  private Vector2 endDrag;
  private Vector2Int mouse_coord;

  private Vector3 GetWorldCoord(int x, int y) {
    return (Vector3.right * x) + (Vector3.forward * y) + board_offset + piece_offset;
  }

  private void GenerateBoard() {
    Debug.Assert(stack_prefab != null, "stack_prefab is null");
    // Instantiate empty game objects for stacks
    for(int i = 0; i < BOARD_SIZE; i++) {
      for(int j = 0; j < BOARD_SIZE; j++) {
        GameObject stack_obj = Instantiate(stack_prefab);
        Stack stack = stack_obj.GetComponent<Stack>();
        board[j, i] = stack;
        stack_obj.transform.SetParent(this.transform);
        stack.transform.position = GetWorldCoord(j, i);
      }
    }
    // THIS PART IS ONLY FOR TEST
    // GAME START WITH AN EMPTY BOARD
    // Put 5 white pieces to bottom row
    for(int i=0; i<5; i++) {
      InstantiatePiece(i, 0, "white");
    }
    // Put 5 black pieces to top row
    for(int i = 0; i < 5; i++) {
      InstantiatePiece(i, 4, "black");
    }
  }

  private void InstantiatePiece(int x, int y, string color) {
    // Default is a white piece. Change prefab if it is black
    GameObject prefab = white_piece_prefab;
    if(color.Equals("black")) {
      prefab = black_piece_prefab;
    }
    GameObject obj = Instantiate(prefab);
    Piece created_piece = obj.GetComponent<Piece>();
    if(created_piece == null) {
      Debug.Log("Can't get Piece component of GameObject.");
      return;
    }
    // AddTop() should handle parent setting and height of the piece;
    created_piece.SetColor(color);
    board[x, y].AddTop(created_piece);
    // obj.transform.SetParent(this.transform);
    //pieces[x,y] = obj.GetComponent<Piece>();
    //MovePiece(pieces[x, y], x, y);
  }

  private void MovePiece(Stack p, int x, int y) {
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

  private void SelectStack(int x, int y) {
    if(OutOfBounds(x)||OutOfBounds(y)) {
      // Out of bounds - Don't select any stacks;
      return;
    }
    // This should never be null, since we populate the
    // board at the beginning.
    Stack s = board[x, y];
    if(s != null && s.PieceCount()>0) {
      // TODO: Improve selection logic. Currently when board[x,y] is selected
      // we set the selected_stack to board[x,y] move it along the Y axis and
      // create a new empty Stack to fill board[x,y]. We also destroy the
      // selected_stack after we are done with it.
      selected_stack = s;
      // Move selected stack up along Y axis to show it is selected
      selected_stack.transform.position = s.transform.position +                      // Original position
                                          Vector3.up * 0.1f * (s.PieceCount() - 1) +  // Height of the pieces in stack
                                          Vector3.up * 0.5f;                          // Selection offset
      selected_coords = mouse_coord;
      // Create a new empty Stack GameObject for board[x,y]
      GameObject stack_obj = Instantiate(stack_prefab);
      s = stack_obj.GetComponent<Stack>();
      board[x, y] = s;
      stack_obj.transform.SetParent(this.transform);
      s.transform.position = GetWorldCoord(x, y);
      Debug.Log("Selected stack at: ("+x+","+y+") Controlling player: "+selected_stack.controlling_player);
    }
  }

  // OLD CODE
  // Currently no support for drag and drop
  private void UpdatePieceDrag(Piece p) {
    if(p == null) {
      return;
    }
    RaycastHit hit;
    Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
    if(Physics.Raycast(ray, out hit, 25.0f, LayerMask.GetMask("Board"))) {
      p.transform.position = hit.point + Vector3.up * 2f;
    }
  }

  // Check if a coordinate is out of bounds (i.e. not on the board)
  private bool OutOfBounds(int coord) {
    if(coord < 0 || coord > BOARD_SIZE) {
      return true;
    }
    return false;
  }

  // Move the selected stack at (startX, startY) to (endX, endY) if possible
  private void TryMove(int startX, int startY, int endX, int endY) {
    // Selected stack should never be null (Maybe changed
    // when placing new stones?)
    if(selected_stack != null) {
      // TODO: Check valid direction also need to check if continuation
      // of a move it should continue in same direction
      // Currently: If target destination is on board move all pieces
      // from selected piece to board.
      if(!OutOfBounds(endX) && !OutOfBounds(endY)) {
        Debug.Log("TryMove: (" + startX + "," + startY + ") -> (" + endX + "," + endY + ")");
        // TODO: Set slide direction and only allow moves in that direction
        // If selected stack is dropped into same place it is not counted as
        // a move, don't forget to check it.
        int num_pieces = selected_stack.PieceCount();
        while(num_pieces > 0) {
          board[endX, endY].AddTop(selected_stack.PopBottom());
          num_pieces--;
        }
      } else {
        // If out of bounds, drop the selected stack in its place for now
        int num_pieces = selected_stack.PieceCount();
        while(num_pieces > 0) {
          board[startX, startY].AddTop(selected_stack.PopBottom());
          num_pieces--;
        }
      }
      // If we placed all the pieces in selected_stack
      // we are done. Destory and deselect it.
      // (and we need to change turn)
      if(selected_stack.PieceCount() == 0) {
        Destroy(selected_stack);
        selected_stack = null;
      }
    }
  }

  private void Start() {
    GenerateBoard();
  }

  private void Update() {
    UpdateMouseCoord();

    if(Input.GetMouseButtonDown(0)) {
      if(selected_stack != null) {
        // This should mean distributing a tower
        // we should enter this codepath until
        // selected_stack is empty, i.e. no more
        // pieces left for player to place
        TryMove(selected_coords.x, selected_coords.y, mouse_coord.x, mouse_coord.y);
      } else {
        SelectStack(mouse_coord.x, mouse_coord.y);
      }
      
    }
    //if(Input.GetMouseButtonUp(0)) {
    //  TryMove(startDrag.x, startDrag.y, mouse_coord.x, mouse_coord.y);
    //}
  }

}
