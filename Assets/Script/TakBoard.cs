using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TakBoard : MonoBehaviour {
  public const int BOARD_SIZE = 5;
  public Stack[,] board = new Stack[BOARD_SIZE, BOARD_SIZE];
  public GameObject white_piece_prefab;
  public GameObject black_piece_prefab;
  public GameObject white_capstone_prefab;
  public GameObject black_capstone_prefab;
  public GameObject stack_prefab;
  public GameObject selection_highlight_prefab;
  public SelectionScript selection_highlight;
  public bool honor_turn = false;
  private string player_color = "white";
  private string player_turn = "white";
  private Vector3 board_offset { get { return new Vector3(-2.5f, 0f, -2.5f); } }
  private Vector3 piece_offset = new Vector3(0.5f, 0.065f, 0.5f);
  private Vector2Int whitepiece_spawn_coord = new Vector2Int(1, -1);
  private Vector2Int blackpiece_spawn_coord = new Vector2Int(4, 5);
  private Vector2Int whitecapstone_spawn_coord = new Vector2Int(2, -1);
  private Vector2Int blackcapstone_spawn_coord = new Vector2Int(3, 5);
  private Stack selected_stack;
  private Vector2Int selected_coords;
  private Vector2 endDrag;
  private Vector2Int mouse_coord;
  private Vector2Int slide_dir;

  private List<Vector2Int> possible_moves;
  private GameState gstate;
  private Client client;
  private bool is_network_game;
  private enum GameState {
    None = 0,
    PlaceFlat,
    PlaceWall,
    PlaceCaps,
    SlideStack,
    Finished
  }

  private Vector3 GetWorldCoord(Vector2Int coord) {
    return (Vector3.right * coord.x) + (Vector3.forward * coord.y) + board_offset + piece_offset;
  }
  private void Start() {
    ChangeGameState(GameState.None);
    client = FindObjectOfType<Client>();
    if(client == null) {
      is_network_game = false;
      player_color = "white";
    } else {
      is_network_game = true;
      client.tak_board = this;
      player_color = client.isHost ? "white" : "black";
    }
    
    
    GameObject obj = Instantiate(selection_highlight_prefab);
    selection_highlight = obj.GetComponent<SelectionScript>();
    selection_highlight.SetActive(false);
    possible_moves = new List<Vector2Int>(5);
    GenerateBoard();
  }

  private void ChangeGameState(GameState newState) {
    Debug.Log("GameState: " + gstate + " -> " + newState);
    gstate = newState;
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
        stack.transform.position = GetWorldCoord(new Vector2Int(j, i));
      }
    }
  }

  private Piece InstantiatePiece(string color, bool is_cap=false) {
    // Default is a white piece. Change prefab if it is black
    GameObject prefab = white_piece_prefab;
    if(color.Equals("black")) {
      prefab = black_piece_prefab;
    }
    if(is_cap) {
      prefab = white_capstone_prefab;
      if(color.Equals("black")) {
        prefab = black_capstone_prefab;
      }
    }
    GameObject obj = Instantiate(prefab);
    Piece created_piece = obj.GetComponent<Piece>();
    if(created_piece == null) {
      Debug.Log("Can't get Piece component of GameObject.");
      return null;
    }
    if(is_cap) {
      created_piece.isCap = true;
    }
    return created_piece;
  }

  private void MovePiece(Stack s, Vector2Int coord) {
    s.transform.position = GetWorldCoord(coord);
  }

  private void ChangeTurn() {
    string new_turn = player_turn == "white" ? "black" : "white";
    Debug.Log("Turn: " + player_turn + " -> " + new_turn);
    if(!is_network_game) {
      player_color = new_turn;
    }
    player_turn = new_turn;
  }

  public void SpawnPiece(string color, Vector2Int coords, bool is_cap=false) {
    Piece p = InstantiatePiece(color, is_cap);
    if(selected_stack == null) {
      GameObject stack_obj = Instantiate(stack_prefab);
      selected_stack = stack_obj.GetComponent<Stack>();
      selected_stack.transform.SetParent(this.transform);
    }
    selected_stack.transform.position = GetWorldCoord(coords) + Vector3.up*0.5f;
    selected_stack.AddTop(p);
    selected_coords = coords;
    if(is_cap) {
      ChangeGameState(GameState.PlaceCaps);
    } else {
      ChangeGameState(GameState.PlaceFlat);
    }
    
    Debug.Log("Selected Stack: " + selected_stack + "Coords: "+selected_coords );
  }

  public void PlaceMove(Vector2Int place_coords) {
    board[place_coords.x, place_coords.y].AddTop(selected_stack.PopBottom());
    ChangeTurn();
  }

  public void FlipPiece() {
    if(selected_stack != null) {
      Piece p = selected_stack.PopBottom();
      p.ToggleWall();
      selected_stack.AddTop(p);
    }
  }


  // Check if a coordinate is out of bounds (i.e. not on the board)
  private bool OutOfBounds(Vector2Int coord) {
    if(coord.x < 0 || coord.x >= BOARD_SIZE ||
       coord.y < 0 || coord.y >= BOARD_SIZE) {
      return true;
    }
    return false;
  }

  private bool OnSpawn(Vector2Int coord) {
    if(player_turn == "white" && 
        (coord == whitecapstone_spawn_coord || coord == whitepiece_spawn_coord)) {
      return true;
    }
    if(player_turn == "black" && 
        (coord == blackcapstone_spawn_coord || coord == blackpiece_spawn_coord)) {
      return true;
    }
    return false;
  }

  public void DropStack(Stack s, Vector2Int coord) {
    if(s != null && !OutOfBounds(coord)) {
      int num_pieces = s.PieceCount();
      while(num_pieces > 0) {
        board[coord.x, coord.y].AddTop(s.PopBottom());
        num_pieces--;
      }
    }
  }

  public bool SelectStack(Vector2Int target) {
    if(OutOfBounds(target)) {
      // Out of bounds - Don't select any stacks;
      return false;
    }
    // This should never be null, since we populate the
    // board at the beginning.
    Stack s = board[target.x, target.y];
    if(s != null && s.PieceCount() > 0 && s.controlling_piece.color.Equals(player_turn)) {
      // TODO: Improve selection logic. Currently when board[x,y] is selected
      // we set the selected_stack to board[x,y] move it along the Y axis and
      // create a new empty Stack to fill board[x,y]. We also destroy the
      // selected_stack after we are done with it.
      selected_stack = s;
      selected_coords = target;
      // Create a new empty Stack GameObject for board[x,y]
      GameObject stack_obj = Instantiate(stack_prefab);
      s = stack_obj.GetComponent<Stack>();
      board[target.x, target.y] = s;
      stack_obj.transform.SetParent(this.transform);
      s.transform.position = GetWorldCoord(target);
      while(selected_stack.PieceCount() > BOARD_SIZE) {
        s.AddTop(selected_stack.PopBottom());
      }

      // Move selected stack up along Y axis to show it is selected
      selected_stack.transform.position = s.transform.position +                      // Original position
                                          Vector3.up * 0.1f * (s.PieceCount() - 1) +  // Height of the pieces in stack
                                          Vector3.up * 0.5f;                          // Selection offset
      Debug.Log("Selected stack at: " + target + " Controlling player: " + selected_stack.controlling_player);
      // Update GameState
      ChangeGameState(GameState.SlideStack);
      return true;
    }
    return false;
  }

  // Move the selected stack at (startX, startY) to (endX, endY) if possible
  public void SlideMove(Vector2Int start_coord, Vector2Int end_coord) {
    
    // Selected stack should never be null (Maybe changed
    // when placing new stones?)
    if(selected_stack != null) {
      // TODO: Check valid direction also need to check if continuation
      // of a move it should continue in same direction
      // Currently: If target destination is on board move all pieces
      // from selected piece to board.
      int num_pieces = selected_stack.PieceCount();
      Vector2Int cur_slide_dir = end_coord - start_coord;
      if(OutOfBounds(end_coord)) {
        DropStack(selected_stack, start_coord);
      }
      if(possible_moves.Contains(end_coord)) {
        if(slide_dir == Vector2Int.zero) {
          slide_dir = cur_slide_dir;
        }
        Stack s = board[end_coord.x, end_coord.y];
        s.AddTop(selected_stack.PopBottom());
        num_pieces--;
        selected_coords = end_coord;
        selected_stack.transform.position = s.transform.position +                      // Original position
                                            Vector3.up * 0.1f * (s.PieceCount() - 1) +  // Height of the pieces in stack
                                            Vector3.up * 0.5f;                          // Selection offset
      } else {
        DropStack(selected_stack, start_coord);
      }
      // If we placed all the pieces in selected_stack
      // we are done. Destory and deselect it.
      // (and we need to change turn)
      if(selected_stack.PieceCount() == 0) {
        if(slide_dir != Vector2Int.zero) {
          slide_dir = Vector2Int.zero;
          ChangeTurn();
        }
      }
    }
  }

  private System.Tuple<List<Vector2Int>, string> BFSPath(Vector2Int start, bool horz) {
    Queue<Vector2Int> que = new Queue<Vector2Int>();
    Dictionary<Vector2Int, Vector2Int> parents = new Dictionary<Vector2Int, Vector2Int>();
    Vector2Int cur = new Vector2Int(start.x, start.y);    
    string cur_player = board[start.x, start.y].controlling_player;
    que.Enqueue(cur);
    parents[cur] = new Vector2Int(-1, -1);
    while(que.Count > 0) {
      cur = que.Dequeue();
      Vector2Int[] neighbors = new[] { cur + new Vector2Int(0, -1), cur + new Vector2Int(-1, 0), cur + new Vector2Int(1, 0), cur + new Vector2Int(0, 1) };
      if(horz) {
        neighbors = new[] { cur + new Vector2Int(1, 0), cur + new Vector2Int(0, -1), cur + new Vector2Int(0, 1), cur + new Vector2Int(-1, 0)};
      }
      for(int j = 0; j < neighbors.Length; j++) {
        Vector2Int n = neighbors[j];
        if(OutOfBounds(n)) {
          continue;
        }
        Stack s = board[n.x, n.y];
        if(s.PieceCount() < 1 || s.controlling_player != cur_player || !s.IsRoad() || parents.ContainsKey(n)) {
          continue;
        }
        parents[n] = cur;
        que.Enqueue(n);
        if(horz && n.x == 4) {
          Vector2Int noparent = new Vector2Int(-1, -1);
          List<Vector2Int> win_road = new List<Vector2Int>();
          string win_player = s.controlling_player;
          while(n != noparent) {
            win_road.Add(n);
            n = parents[n];
          }
          que.Clear();
          return new System.Tuple<List<Vector2Int>, string>(win_road, win_player);
        } 
        if(!horz && n.y == 0){
          Vector2Int noparent = new Vector2Int(-1, -1);
          List<Vector2Int> win_road = new List<Vector2Int>();
          string win_player = s.controlling_player;
          while(n != noparent) {
            win_road.Add(n);
            n = parents[n];
          }
          que.Clear();
          return new System.Tuple<List<Vector2Int>, string>(win_road, win_player);
        }
      }
    }
    return new System.Tuple<List<Vector2Int>, string>(null, "none");
  }

  public string CheckGameOver() {
    // Top Down White Victory
    if(gstate == GameState.None) {
      List<Vector2Int> min_win_road = null;
      string min_winner = "";
      for(int i = 0; i < BOARD_SIZE; i++) {
        System.Tuple<List<Vector2Int>, string> res;
        res = BFSPath(new Vector2Int(i, 4), false);
        if(min_win_road == null || (res.Item1 != null && res.Item1.Count < min_win_road.Count)) {
          min_win_road = res.Item1;
          min_winner = res.Item2;
        }
      }
      // Left Right White Victory
      for(int i = 0; i < BOARD_SIZE; i++) {
        System.Tuple<List<Vector2Int>, string> res;
        res = BFSPath(new Vector2Int(0, i), true);
        if(min_win_road == null || (res.Item1 != null && res.Item1.Count < min_win_road.Count)) {
          min_win_road = res.Item1;
          min_winner = res.Item2;
        }
      }
      Debug.Log("Winner: " + min_winner);
      if(min_winner != "none") {
        ChangeGameState(GameState.Finished);
      }
      return min_winner;
    }
    return "";
  }

  private void UpdateSelectionHighlight() {
    if(selection_highlight == null){
      return;
    }
    if(!OutOfBounds(mouse_coord)) {
      selection_highlight.SetValid(false);
      if(possible_moves.Contains(mouse_coord)) {
        selection_highlight.SetValid(true);
      }
      selection_highlight.transform.position = GetWorldCoord(mouse_coord);
    } else {
        if(OnSpawn(mouse_coord)) {
          selection_highlight.HighlightSpawn();
          selection_highlight.transform.position = GetWorldCoord(mouse_coord);
        }
      }
  }

  public void UpdatePossibleMoves() {
    possible_moves.Clear();
    if(selected_stack != null) {
      if(gstate == GameState.SlideStack) {
        possible_moves.Add(selected_coords);
        if(slide_dir == Vector2Int.zero) {
          Vector2Int right = selected_coords + new Vector2Int(1, 0);
          Vector2Int left = selected_coords + new Vector2Int(-1, 0);
          Vector2Int up = selected_coords + new Vector2Int(0, 1);
          Vector2Int down = selected_coords + new Vector2Int(0, -1);
          Piece bottom_piece = selected_stack.PeekBottom();
          if(!OutOfBounds(left) && board[left.x, left.y].CanMoveOver(bottom_piece.isCap))
            possible_moves.Add(left);
          if(!OutOfBounds(right) && board[right.x, right.y].CanMoveOver(bottom_piece.isCap))
            possible_moves.Add(right);
          if(!OutOfBounds(up) && board[up.x, up.y].CanMoveOver(bottom_piece.isCap))
            possible_moves.Add(up);
          if(!OutOfBounds(down) && board[down.x, down.y].CanMoveOver(bottom_piece.isCap))
            possible_moves.Add(down);
        } else {
          Vector2Int sliding_coord = selected_coords + slide_dir;
          if(!OutOfBounds(sliding_coord) && board[sliding_coord.x, sliding_coord.y].CanMoveOver()) {
            possible_moves.Add(sliding_coord);
          }
        }
      }
      if(gstate == GameState.PlaceWall || gstate == GameState.PlaceFlat || gstate == GameState.PlaceCaps) {
        // Find empty places on board
        for(int j = 0; j < BOARD_SIZE; j++) {
          for(int i = 0; i < BOARD_SIZE; i++) {
            if(board[i, j].PieceCount() == 0) {
              possible_moves.Add(new Vector2Int(i, j));
            }
          }
        }
      }
    }
  }

  private void UpdateMouseCoord() {
    RaycastHit hit;
    if(Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out hit, 25.0f, LayerMask.GetMask("Board"))) {
      Vector3 hit_point = hit.point - board_offset;
      mouse_coord.x = (int)hit_point.x;
      mouse_coord.y = (int)hit_point.z;
    } else if(Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out hit, 25.0f, LayerMask.GetMask("PieceSpawn"))) {
      if(hit.point.z > 0) {
        mouse_coord = blackpiece_spawn_coord;
      } else {
        mouse_coord = whitepiece_spawn_coord;
      }
    } else if(Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out hit, 25.0f, LayerMask.GetMask("CapstoneSpawn"))) {
      if(hit.point.z > 0) {
        mouse_coord = blackcapstone_spawn_coord;
      } else {
        mouse_coord = whitecapstone_spawn_coord;
      }
    } else {
      mouse_coord.x = -99;
      mouse_coord.y = -99;
    }

    //Debug.Log(mouse_coord);
  }

  public void CheckSelectedStack() {
    if(selected_stack != null) {
      if(selected_stack.PieceCount() == 0) {
        Destroy(selected_stack.gameObject);
        selected_stack = null;
        ChangeGameState(GameState.None);
      }
    }
  }
  public void DestroySelectedStack() {
    while(selected_stack.PieceCount() > 0) {
      Destroy(selected_stack.PopBottom().gameObject);
    }
    Destroy(selected_stack.gameObject);
    selected_stack = null;
    ChangeGameState(GameState.None);
  }

  private void Update() {
    UpdateMouseCoord();
    UpdateSelectionHighlight();
    if(player_turn.Equals(player_color)) {
      if(Input.GetMouseButtonDown(0)) {
        // First Move
        if(gstate == GameState.None) {
          if(selected_stack != null) {
            Debug.LogError("GameState: " + gstate + ", Selected stack is not null during GameState.None");
            DestroySelectedStack();
            return;
          }
          if(!OutOfBounds(mouse_coord)) {
            // Clicked on Board, so select a stack 
            if(SelectStack(mouse_coord)) {
              if(is_network_game)
                client.Send("CSTAK|" + mouse_coord.x + "," + mouse_coord.y);
            }
            // TODO: PROTOCOL MESSAGES
          } else if(mouse_coord == whitepiece_spawn_coord && player_color.Equals("white")) { // WHITE PIECE
            // Clicked on white flat piece
            SpawnPiece("white", mouse_coord);
            if(is_network_game)
              client.Send("CSPWN|" + "white" + "|" + mouse_coord.x + "," + mouse_coord.y + "|0");
          } else if(mouse_coord == blackpiece_spawn_coord && player_color.Equals("black")) { // BLACK PIECE
            // Clicked on black flat piece
            SpawnPiece("black", mouse_coord);
            if(is_network_game)
              client.Send("CSPWN|" + "black" + "|" + mouse_coord.x + "," + mouse_coord.y + "|0");
          } else if(mouse_coord == whitecapstone_spawn_coord && player_color.Equals("white")) { // WHITE CAPSTONE
            // Clicked on black flat piece
            SpawnPiece("white", mouse_coord, true);
            if(is_network_game)
              client.Send("CSPWN|" + "white" + "|" + mouse_coord.x + "," + mouse_coord.y + "|1");
          } else if(mouse_coord == blackcapstone_spawn_coord && player_color.Equals("black")) { // BLACK CAPSTONE
            // Clicked on black flat piece
            SpawnPiece("black", mouse_coord, true);
            if(is_network_game)
              client.Send("CSPWN|" + "black" + "|" + mouse_coord.x + "," + mouse_coord.y + "|1");
          }
        } else if(gstate == GameState.SlideStack) {
          if(selected_stack == null) {
            Debug.LogError("GameState: " + gstate + ", Selected stack is null during GameState.SlideStack");
            return;
          }
          Vector2Int network_coords = selected_coords;
          SlideMove(selected_coords, mouse_coord);
          if(is_network_game)
            client.Send("CSLDE|" + network_coords.x+","+ network_coords.y+"|"+ mouse_coord.x + "," + mouse_coord.y); 
        } else if(gstate == GameState.PlaceFlat || gstate == GameState.PlaceWall || gstate == GameState.PlaceCaps) {
          if(selected_stack == null) {
            Debug.LogError("GameState: " + gstate + ", Selected stack is null during GameState.Place");
            return;
          }
          if(!OutOfBounds(mouse_coord)) {
            if(possible_moves.Contains(mouse_coord)) {
              PlaceMove(mouse_coord);
              if(is_network_game)
                client.Send("CPLCE|" + mouse_coord.x + "," + mouse_coord.y);
            }
          } else {
            DestroySelectedStack();
            if(is_network_game)
              client.Send("CDCRD|"); 
          }
        }
        CheckSelectedStack();
        UpdatePossibleMoves();
        Debug.Log("Turn " + player_turn);
        Debug.Log("GameState: " + gstate);
        CheckGameOver();
      }
      if(Input.GetMouseButtonDown(1)) {
        if(gstate == GameState.PlaceFlat || gstate == GameState.PlaceWall) {
          FlipPiece();
          if(is_network_game)
            client.Send("CFLIP|");
        }
      }
      if(gstate != GameState.None && gstate != GameState.Finished) {
        selection_highlight.SetActive(true);
      }
    }
    else {
      selection_highlight.SetActive(false);
    }
  }
}
