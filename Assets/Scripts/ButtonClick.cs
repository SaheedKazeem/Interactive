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
        SceneFader.FadeAndLoad(SceneManager.GetActiveScene().buildIndex + 1);
    }
     public void GoBackVideo()
    {
        SceneFader.FadeAndLoad(SceneManager.GetActiveScene().buildIndex - 1);
    }
     public void RandomDoors()
    {
        SceneFader.FadeAndLoad(("Random Door"));
    }
     public void ReturnHome()
    {
        SceneFader.FadeAndLoad(("Home"));
    }
    public void ReturnBackToPawel()
    {
        SceneFader.FadeAndLoad(("Pawel's Door"));
    }
    public void PlayTheBFM()
    {
        SceneFader.FadeAndLoad(("Play BFM"));
        
    }
      public void PlayXTC()
    {
        SceneFader.FadeAndLoad(("Play XTC"));
        
    }
      public void LookXTC()
    {
        SceneFader.FadeAndLoad(("Look at X-TC Disc"));
        
    }
    public void TapOut()
    {
        SceneFader.FadeAndLoad(("Tap Out"));
        
    }
    public void PostMortem()
    {
        SceneFader.FadeAndLoad(("#Fear Smells"));
        
    }
    public void BeamUp()
    {
        SceneFader.FadeAndLoad(("Beam me Up!"));
        
    }
    public void SmokeUp()
    {
        SceneFader.FadeAndLoad(("Smoke"));
        
    }
     public void BusSpot()
    {
        SceneFader.FadeAndLoad(("Bus"));
        
    }
    public void StandUp()
    {
        SceneFader.FadeAndLoad(("Stand Up"));
        
    }
     public void FGod()
    {
        SceneFader.FadeAndLoad(("Fucking God"));
        
    }
     public void Eyes()
    {
        SceneFader.FadeAndLoad(("Eyes"));
        
    }
    public void Scent()
    {
        SceneFader.FadeAndLoad(("Scent"));
        
    }
    public void NewConvo()
    {
        SceneFader.FadeAndLoad(("New Convo"));
        
    }
    public void Leave()
    {
        SceneFader.FadeAndLoad(("Leave"));

    }
    public void TodayIsDay()
    {
        SceneFader.FadeAndLoad(("Today is the Day"));

    }
    public void WDYM()
    {
        SceneFader.FadeAndLoad(("WDYM"));

    }
     public void TakeOffVeil()
    {
        SceneFader.FadeAndLoad(("Take Off Veil"));

    }
       public void Name()
    {
        SceneFader.FadeAndLoad(("Name"));

    }
    public void ReturnToTrainStation()
    {
        SceneFader.FadeAndLoad(("Train Station"));
    }


}
