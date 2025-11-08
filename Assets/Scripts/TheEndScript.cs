using UnityEngine;
using UnityEngine.Video;
 
public class TheEndScript : MonoBehaviour
{
 
     VideoPlayer video;
 
    void Awake()
    {
        video = GetComponent<VideoPlayer>();
        video.Play();
        video.loopPointReached += CheckOver;
 
         
    }
 
 
    void CheckOver(UnityEngine.Video.VideoPlayer vp)
    {
        SceneFader.FadeAndLoad(1);//the scene that you want to load after the video has ended.
    }
}
