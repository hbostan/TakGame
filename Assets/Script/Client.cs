using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using UnityEngine;

public class Client : MonoBehaviour
{
  private bool socket_ready;
  public bool isHost = false;
  public string client_name;
  private TcpClient socket;
  private NetworkStream stream;
  private StreamWriter writer;
  private StreamReader reader;
  public TakBoard tak_board;

  private List<GameClient> players;
  public void Start() {
    DontDestroyOnLoad(gameObject);
    players = new List<GameClient>();
  }
  private void OnApplicationQuit() {
    CloseAll();
  }
  private void OnDisable() {
    CloseAll();
  }
  public bool Connect(string host, int port) {
    if(socket_ready) {
      return false;
    }
    try {
      socket = new TcpClient(host, port);
      stream = socket.GetStream();
      writer = new StreamWriter(stream);
      reader = new StreamReader(stream);
      socket_ready = true;
    } catch (Exception e) {
      Debug.LogError("Connect: " + e.Message);
      
    }
    return socket_ready;
  }

  public void Update() {
    if(socket_ready) {
      if(stream.DataAvailable) {
        string msg = reader.ReadLine();
        if(msg != null) {
          OnMessageReceived(msg);
        }
      }
    }
  }

  public void Send(string msg) {
    if(!socket_ready) {
      return;
    }
    writer.WriteLine(msg);
    writer.Flush();
    Debug.Log("Client Send: \"" + msg + "\"(" + client_name + ")");
  }

  private void UserConnected(string name, bool isHost) {
    GameClient c = new GameClient();
    c.name = name;
    c.isHost = isHost;
    players.Add(c);
    if(players.Count == 2) {
      GameManager.Instance.StartGame();
    }
  }

  private void OnMessageReceived(string msg) {
    Debug.Log("Client Receive: \"" + msg + "\"");
    string[] rcvData = msg.Split('|');
    string[] rcvCoord;
    switch(rcvData[0]) {
      case "SWHO":
        for(int i = 1; i < rcvData.Length-1; i++) {
          UserConnected(rcvData[i], false);
        }
        Send("CWHO|" + client_name +"|"+ ((isHost)?1:0));
        return;
      case "SCONN":
        UserConnected(rcvData[1], false);
        return;
      case "SSTAK":
        rcvCoord = rcvData[1].Split(',');
        int x = int.Parse(rcvCoord[0]);
        int y = int.Parse(rcvCoord[1]);
        tak_board.SelectStack(new Vector2Int(x, y));
        break;
      case "SSPWN":
        rcvCoord = rcvData[2].Split(',');
        int spwnx = int.Parse(rcvCoord[0]);
        int spwny = int.Parse(rcvCoord[1]);
        bool is_cap = rcvData[3] == "1" ? true : false;
        tak_board.SpawnPiece(rcvData[1], new Vector2Int(spwnx, spwny), is_cap);
        break;
      case "SSLDE":
        // scs = Selected Coordinate String
        // mcs = Mouse coordinate string
        rcvCoord = rcvData[1].Split(',');
        int scx = int.Parse(rcvCoord[0]);
        int scy = int.Parse(rcvCoord[1]);
        rcvCoord = rcvData[2].Split(',');
        int mcx = int.Parse(rcvCoord[0]);
        int mcy = int.Parse(rcvCoord[1]);
        tak_board.SlideMove(new Vector2Int(scx, scy), new Vector2Int(mcx, mcy));
        break;
      case "SPLCE":
        rcvCoord = rcvData[1].Split(',');
        int plcx = int.Parse(rcvCoord[0]);
        int plcy = int.Parse(rcvCoord[1]);
        tak_board.PlaceMove(new Vector2Int(plcx, plcy));
        break;
      case "SDCRD":
        tak_board.DestroySelectedStack();
        break;
      case "SFLIP":
        tak_board.FlipPiece();
        break;
    }
    tak_board.CheckSelectedStack();
    tak_board.UpdatePossibleMoves();
    tak_board.CheckGameOver();
  }

  private void CloseAll() {
    if(!socket_ready) {
      return;
    }
    writer.Close();
    reader.Close();
    stream.Close();
    socket.Close();
    socket_ready = false;
  }
}

public class GameClient {
  public string name;
  public bool isHost;
}