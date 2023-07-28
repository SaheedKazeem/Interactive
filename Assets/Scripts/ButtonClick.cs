using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Threading.Tasks;
using UnityEngine.UI;
using UnityEngine.Video;

public class ButtonClick : MonoBehaviour
{
    [SerializeField] VideoPlayer RefToVideo;
    [SerializeField] GameObject RefToButton;
    [SerializeField] float buttonAppearTime; // The time in seconds when the button should appear
    // Start is called before the first frame update
    void Start()
    {
        RefToButton.SetActive(false);
    }

    // Update is called once per frame
     void Update()
      {
        // Check if the current video playback time matches the desired time
        if (RefToVideo.time >= buttonAppearTime && !RefToButton.gameObject.activeSelf)
        {
            // Show the button
            RefToButton.gameObject.SetActive(true);
        }
    }
    public void NextVideo()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
    }
     public void RandomDoors()
    {
        SceneManager.LoadScene(("Random Door"));
    }
     public void ReturnHome()
    {
        SceneManager.LoadScene(("Home"));
    }
    public void ReturnBackToPawel()
    {
        SceneManager.LoadScene(("Pawel's Door"));
    }
    public void PlayTheBFM()
    {
        SceneManager.LoadScene(("Play BFM"));
        
    }
    public void FaultyWiring()
    {
        SceneManager.LoadScene(("#Faulty Wiring"));
        
    }
    public void PostMortem()
    {
        SceneManager.LoadScene(("#FearSmells"));
        
    }
     public void BeamUp()
    {
        SceneManager.LoadScene(("Beam me Up!"));
        
    }
}
