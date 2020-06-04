using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
  public static GameManager Instance {
    set; get;
  }

  #region MenuFields
  public GameObject main_menu;
  public GameObject wait_menu;
  public GameObject multi_player_menu;
  public GameObject connection_settings_menu;
  public GameObject host_settings_menu;
  public InputField username;

  private bool game_on = false;
  #endregion

  #region serverClient
  public GameObject serverPrefab;
  public GameObject clientPrefab;
  #endregion

  private void Start() {
    Instance = this;
    main_menu.SetActive(true);
    multi_player_menu.SetActive(false);
    wait_menu.SetActive(false);
    host_settings_menu.SetActive(false);
    connection_settings_menu.SetActive(false);

    DontDestroyOnLoad(gameObject);
  }

  private void Update() {
    if (Input.GetKeyDown("escape")) {
      if (game_on) {
        game_on = false;
        SceneManager.LoadScene("menu");
      } else {
        BackToMenuButton();
      }
    }
  }

  public void PlayOnlineButton() {
    main_menu.SetActive(false);
    multi_player_menu.SetActive(true);
  }

  public void MenuConnectButton() {
    main_menu.SetActive(false);
    connection_settings_menu.SetActive(true);
  }

  public void MenuHostButton() {
    main_menu.SetActive(false);
    host_settings_menu.SetActive(true);
  }

  public void HostButton() {
    int host_port = 3131;
    bool suc = int.TryParse(GameObject.Find("HostPortInputField").GetComponent<InputField>().text, out host_port);
    if(!suc || host_port < 1024 || host_port > 6500) {
      host_port = 3131;
    }
    try {
      Server s = Instantiate(serverPrefab).GetComponent<Server>();
      s.Init();
      Client c = Instantiate(clientPrefab).GetComponent<Client>();
      c.client_name = username.text;
      c.isHost = true;
      if(c.client_name == "") {
        c.client_name = "User#" + (new System.Random().Next()).ToString();
      }
      c.Connect("127.0.0.1", 3131);
    }
    catch(Exception e) {
      Debug.LogError("Host: " + e.Message);
    }
    main_menu.SetActive(false);
    host_settings_menu.SetActive(false);
    wait_menu.SetActive(true);
  }

  public void ConnectButton() {
    string host_addr = "";
    host_addr = GameObject.Find("HostInputField").GetComponent<InputField>().text;
    if(host_addr == "") {
      host_addr = "127.0.0.1";
    }
    int host_port = 3131;
    bool suc = int.TryParse(GameObject.Find("PortInputField").GetComponent<InputField>().text, out host_port);
    if(!suc || host_port < 1024 || host_port > 6500) {
      host_port = 3131;
    }

    try {
      Debug.Log("Connecting to: " + host_addr + " " + host_port);
      Client c = Instantiate(clientPrefab).GetComponent<Client>();
      c.client_name = username.text;
      if(c.client_name == "") {
        c.client_name = "User#" + (new System.Random().Next()).ToString();
      }
      c.Connect(host_addr, host_port);
      connection_settings_menu.SetActive(false);
    } catch(Exception e) {
      Debug.LogError("Connection: " + e.Message);
    }

  }
  
  public void BackToMenuButton() {
    connection_settings_menu.SetActive(false);
    host_settings_menu.SetActive(false);
    wait_menu.SetActive(false);
    multi_player_menu.SetActive(false);
    main_menu.SetActive(true);

    Server s = FindObjectOfType<Server>();
    Client c = FindObjectOfType<Client>();
    if(s != null) {
      Destroy(s.gameObject);
    }
    if(c != null) {
      Destroy(c.gameObject);
    }
  }

  public void QuitButton() {
    Application.Quit();
  }

  public void StartGame() {
    game_on = true;
    SceneManager.LoadScene("game");
  }
}
