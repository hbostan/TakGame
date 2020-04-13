using UnityEngine;
using System.Collections;

public class LightFlickerEffect : MonoBehaviour {
  public float max_intensity;
  public float min_intensity;
  [Range(0, 10)]
  public float strength;
  public bool StopFlickering;

  private Light light_source;
  private float target, start;
  private float interpolator;

  public void Start() {
    light_source = GetComponent<Light>();
    if(light_source == null) {
      Debug.LogError("Flicker script must have a Light Component on the same GameObject.");
      return;
    }
    start = light_source.intensity;
    target = Random.Range(min_intensity, max_intensity);
    interpolator = 0;
  }

  void Update() {
    if(!StopFlickering) {
      light_source.intensity = Mathf.Lerp(start, target, interpolator);
    }
    interpolator += strength * Time.deltaTime;
    if(interpolator >= 1) {
      start = target;
      target = Random.Range(min_intensity, max_intensity);
      interpolator = 0;
    }
  }
}