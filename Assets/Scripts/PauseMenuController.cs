using UnityEngine;

public class PauseMenuController : MonoBehaviour
{
    public GameObject menuCanvas;
    private VideoController videoController;
    private bool wasPlayingBeforeMenu;

    private void Start()
    {
        
        menuCanvas.SetActive(false); // Ensure the menu is initially not active
    }

    private void Update()
    {
        videoController = FindObjectOfType<VideoController>();
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            ToggleMenu();
        }
    }

    private void ToggleMenu()
    {
        bool isMenuActive = menuCanvas.activeSelf;
        menuCanvas.SetActive(!isMenuActive);

        if (videoController)
        {
            if (isMenuActive)
            {
                // Store whether the video was playing before opening the menu
                wasPlayingBeforeMenu = videoController.IsVideoPlaying();
                videoController.PauseVideo();
            }
            else
            {
                // Resume video playback if it was playing before opening the menu
                if (wasPlayingBeforeMenu)
                {
                    videoController.PlayVideo();
                }
            }
        }
    }
}