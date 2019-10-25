using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;


public class MainMenu : MonoBehaviour
{
    // Tlacidlo novej hry
    public Button startButton;

    // Tlacidlo ukoncenia hry
    public Button exitButton;

    // Loading window
    public Slider loadingBar;
    public GameObject loadingImage;

    // Referencia na asynchronnu operaciu
    private AsyncOperation asyncOperation;

    // Start is called before the first frame update
    void Start()
    {
        startButton.onClick.AddListener(OnStartButtonClicked);
        exitButton.onClick.AddListener(OnExitButtonClicked);
    }

    // Update is called once per frame
    void Update()
    {
        // Kym nie je hra loadnuta vypisuj progres na bar
        if (asyncOperation != null)
        {
            loadingBar.value = asyncOperation.progress;
        }
    }

    private void OnStartButtonClicked()
    {
        Debug.Log("start button pressed");

        // aktivuj loading obraz
        loadingImage.SetActive(true);
        //StartCoroutine(LoadingBarUpdate());

        // Load first scene
        asyncOperation = SceneManager.LoadSceneAsync("Area1");
    }

    private void OnExitButtonClicked()
    {
        Debug.Log("exit button pressed");

        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }

    /*private IEnumerator LoadingBarUpdate()
    {
        // Load first scene
        asyncOperation = SceneManager.LoadSceneAsync("Area1");

        // Kym nie je hra loadnuta
        while (!asyncOperation.isDone)
        {
            loadingBar.value = asyncOperation.progress;
            yield return null;
        }
    }*/
}
