using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseMenuController : MonoBehaviour
{
    public GameObject menuCanvas;
    private VideoController videoController;
    private bool wasPlayingBeforeMenu;
    void Awake()
    {
       SceneManager.sceneLoaded += OnSceneLoaded; 
    }

    private void Start()
    {
        
        menuCanvas.SetActive(false); // Ensure the menu is initially not active
        
    }
    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        videoController = FindObjectOfType<VideoController>();
       
    }
      private void OnDestroy()
    {
        // Unsubscribe from the sceneLoaded event to prevent memory leaks
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void Update()
    {
       
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            ToggleMenu();
        }
    }

    private void ToggleMenu()
    {
        bool wasActive = menuCanvas.activeSelf;
        bool opening = !wasActive; // if currently inactive, we are opening
        menuCanvas.SetActive(!wasActive);

        if (!videoController) return;

        if (opening)
        {
            // Opening menu: pause and snapshot state
            wasPlayingBeforeMenu = videoController.IsVideoPlaying();
            videoController.PauseVideo();
        }
        else
        {
            // Closing menu: resume only if it was playing before
            if (wasPlayingBeforeMenu)
            {
                videoController.PlayVideo();
            }
        }
    }
}
