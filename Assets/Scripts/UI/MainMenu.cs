using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;


public class MainMenu : MonoBehaviour
{
    // Loading window
    public Slider loadingBar;
    public GameObject loadingImage;

    // Event system
    public EventSystem eventSystem;

    // Selected button
    //public GameObject selectedButton;
    //public bool buttonSelected;

    // Start is called before the first frame update
    //private void Start()
    //{

    //}

    //private void Update()
    //{
        //if (Mathf.Abs(Input.GetAxisRaw("Vertical")) > 0f && !buttonSelected)
        //{
        //    eventSystem.SetSelectedGameObject(selectedButton);
        //    buttonSelected = true;
        //}
    //}

    //private void OnDisable()
    //{
        //buttonSelected = false;
    //}

    public void OnStartButtonClicked()
    {
        Debug.Log("start button pressed");

        // aktivuj loading obraz
        loadingImage.SetActive(true);
        StartCoroutine(LoadingBarUpdate());
    }

    public void OnExitButtonClicked()
    {
        Debug.Log("exit button pressed");

        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }
        
    private IEnumerator LoadingBarUpdate()
    {
        // Load first scene
        var asyncOperation = SceneManager.LoadSceneAsync("Area1");

        // Kym nie je hra loadnuta
        while (!asyncOperation.isDone)
        {
            loadingBar.value = asyncOperation.progress;
            yield return null;
        }
    }
}
