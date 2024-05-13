using UnityEngine;
using System.Collections;

public class Mover : MonoBehaviour 
{
    public float Speed = 400.0f;
    public bool Pause = false;
    
    void Awake(){
        Speed = PlayerPrefs.GetFloat(UIManager.DISPLAY_SPEED) * Speed;
        //Debug.LogFormat("Speed: {0}", Speed);
        
    }
    // Update is called once per frame
    // Default 30 FPS
    void Update () 
    {
        //Debug.LogFormat("Trying to move {0}", gameObject.name);
        if (!Pause){
            transform.Translate( new Vector2(-1 * Speed * Time.deltaTime, 0f));
        }
    }
}