using UnityEngine;

public class PauseMenuController : MonoBehaviour
{
    public GameObject menuCanvas;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            ToggleMenu();
        }
    }

    private void ToggleMenu()
    {
        menuCanvas.SetActive(!menuCanvas.activeSelf);

        // Pause or resume the video when the menu is opened or closed
        VideoController videoController = FindObjectOfType<VideoController>();
        if (videoController)
        {
            if (menuCanvas.activeSelf)
            {
                videoController.videoPlayer.Pause();
            }
            else if (videoController.isPlaying)
            {
                videoController.videoPlayer.Play();
            }
        }
    }
}
