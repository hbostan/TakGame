using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PauseMenu : MonoBehaviour
{
  public static bool gamePaused = false;
  #region MenuFields
  public GameObject pauseMenuUI;
  public GameObject disconnectMenu;
  public GameObject connectMenu;
  #endregion
  // Update is called once per frame
  void Update() {
    if (Input.GetKeyDown(KeyCode.Escape)) {
      if (gamePaused) {
        Resume();
      } else {
        Pause();
      }
    }
  }

  public void Exit() {
    Application.Quit();
  }

  public void Resume() {
    gamePaused = false;
    pauseMenuUI.SetActive(false);
  }

  public void Connect() {
    
  }

  public void Disconnect() {

  }

  private void Pause() {
    gamePaused = true;
    pauseMenuUI.SetActive(true);
    if (TakBoard.IsNetworkGame()) {
      disconnectMenu.SetActive(true);
    } else {
      connectMenu.SetActive(true);
    }
  }
}
