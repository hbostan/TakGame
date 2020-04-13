using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using UnityEngine;

public class Server : MonoBehaviour
{
  public int port = 3131;
  private List<ServerClient> clients;
  private List<ServerClient> disconnect_clients;
  private TcpListener server;
  private bool running;

  public void Init() {
    DontDestroyOnLoad(gameObject);
    clients = new List<ServerClient>();
    disconnect_clients = new List<ServerClient>();
    try {
      server = new TcpListener(IPAddress.Any, port);
      server.Start();
      StartListen();
      running = true;
    } catch (Exception e) {
      Debug.LogError("Server Error: " + e.Message);
    }
  }

  public void Update() {
    if(!running) {
      return;
    }
    foreach(ServerClient c in clients) {
      if(c != null) {
        if(!c.isConnected()) {
          c.tcp_client.Close();
          disconnect_clients.Add(c);
          Debug.Log("Disconnect: " + c.client_name);
          continue;
        }
        // Connected
        NetworkStream stream = c.tcp_client.GetStream();
        if(stream.DataAvailable) {
          StreamReader reader = new StreamReader(stream, true);
          string msg = reader.ReadLine();
          if(msg != null) {
            OnMessageReceived(c, msg);
          }
        }
      }
    }
    for(int i = 0; i < disconnect_clients.Count; i++) {
      clients.Remove(disconnect_clients[i]);
      disconnect_clients.RemoveAt(i);
      // Send disconnect message
    }
  }

  private void StartListen() {
    if(server != null) {
      server.BeginAcceptTcpClient(AcceptTcpClient, server);
    }
  }
  private void AcceptTcpClient(IAsyncResult ar) {
    string cur_users = "";
    foreach(ServerClient i in clients) {
      cur_users += i.client_name + '|';
    }
    TcpListener listener = (TcpListener)ar.AsyncState;
    ServerClient sc = new ServerClient(listener.EndAcceptTcpClient(ar));
    clients.Add(sc);
    StartListen();
    Debug.Log(sc.tcp_client.Client.RemoteEndPoint.ToString() + " connected.");
    SendMsg("SWHO|"+cur_users, sc);
  }

  private void SendMsg(string msg, ServerClient cl) {
    try {
      StreamWriter writer = new StreamWriter(cl.tcp_client.GetStream());
      writer.WriteLine(msg);
      writer.Flush();
      Debug.Log("Server Send: \"" + msg + "\"");
    }
    catch(Exception e) {
      Debug.LogError("SendMsg: " + e.Message);
    }
  }
  private void Broadcast(string msg, List<ServerClient> cl) {
    foreach(ServerClient c in cl) {
      SendMsg(msg, c);
    }
  }

  private void Broadcast(string msg, List<ServerClient> cl, ServerClient excpt) {
    foreach(ServerClient c in cl) {
      if(c == excpt) {
        continue;
      }
      SendMsg(msg, c);
    }
  }
  private void OnMessageReceived(ServerClient c, string msg) {
    Debug.Log("Server Receive: \"" + msg + "\"");
    string[] rcvData = msg.Split('|');
    switch(rcvData[0]) {
      case "CWHO":
        c.client_name = rcvData[1];
        c.is_host = rcvData[2] == "0" ? false : true;
        Broadcast("SCONN|" + c.client_name, clients);
        break;
      default:
        char[] m = msg.ToCharArray();
        m[0] = 'S';
        msg = new String(m);
        Broadcast(msg, clients, c);
        break;
    }
  }

}

public class ServerClient {
  public string client_name;
  public bool is_host;
  public TcpClient tcp_client;
  public ServerClient(TcpClient cl) {
    this.tcp_client = cl;
  }
  public bool isConnected() {
    try {
      if(tcp_client.Client != null && tcp_client.Client.Connected) {
        if(tcp_client.Client.Poll(0, SelectMode.SelectRead)) {
          return !(tcp_client.Client.Receive(new byte[1], SocketFlags.Peek) == 0);
        }
        return true;
      }
      return false;
    } catch (Exception e) {
      Debug.LogError("isConnected: " + e.Message);
      return false;
    }
  }
}