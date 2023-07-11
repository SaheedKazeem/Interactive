using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

[Serializable]
public class OrderedVideo
{
    public VideoClip videoClip;
    public float orderIndex;
}

public class VideoPlayerController : MonoBehaviour
{
    public Button button; 
    public VideoPlayer videoPlayer; 
    

    

    void Start()
    {
        // Sort the videos based on the order index
       

       
    }

   public void PlayNextVideo()
    {
       
        videoPlayer.Play();

        
    }
   
    
}
