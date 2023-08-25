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
        
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
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
