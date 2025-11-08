using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;


public class MenuScript : MonoBehaviour
{
    [SerializeField]private GameObject refToOptionsMenu; 
    // Start is called before the first frame update
    void Start()
    {
        
    }

    public void LoadScene()
    {
        int next = SceneManager.GetActiveScene().buildIndex + 1;
        Debug.Log($"Start button clicked. Loading scene index {next}...");
        SceneFader.FadeAndLoad(next);
    }
     public void OptionsMenu()
     {
        refToOptionsMenu.SetActive(true);
     }
     public void CloseOptionsMenu()
     {
        refToOptionsMenu.SetActive(false);
     }
     
     
    // Update is called once per frame
    void Update()
    {
        
    }
     public void QuitGame ()
    {
       Application.Quit();
    }
}
