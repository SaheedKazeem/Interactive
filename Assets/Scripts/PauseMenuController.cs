using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseMenuController : MonoBehaviour
{
    public GameObject menuCanvas;
    private VideoController videoController;
    private bool wasPlayingBeforeMenu;

    private static PauseMenuController activeController;
    private static int lastToggleFrame = -1;
    void Awake()
    {
    }

    private void OnEnable()
    {
        activeController = this;
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        if (activeController == this)
        {
            activeController = null;
        }
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void Start()
    {
        if (menuCanvas)
        {
            menuCanvas.SetActive(false); // Ensure the menu is initially not active
        }
        
    }
    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (activeController == null)
        {
            activeController = this;
        }
        videoController = Interactive.Util.SceneObjectFinder.FindFirst<VideoController>(true);
       
    }
      private void OnDestroy()
    {
        // Unsubscribe from the sceneLoaded event to prevent memory leaks
        SceneManager.sceneLoaded -= OnSceneLoaded;
        if (activeController == this)
        {
            activeController = null;
        }
    }

    private void Update()
    {
        if (activeController != this) return;
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (lastToggleFrame == Time.frameCount) return;
            lastToggleFrame = Time.frameCount;
            ToggleMenu();
        }
    }

    private void ToggleMenu()
    {
        if (!menuCanvas) return;

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
