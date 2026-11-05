using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
    [SerializeField] private string sceneToLoad;
    [SerializeField] private Image creditsImage;
    [SerializeField] private Image backButton;

    public void PlayGame()
    {
        SceneManager.LoadScene(sceneToLoad);
    }

    public void ShowCredits()
    {
        creditsImage.gameObject.SetActive(true);
        backButton.gameObject.SetActive(true);
    }

    public void HideCredits()
    {
        creditsImage.gameObject.SetActive(false);
        backButton.gameObject.SetActive(false);
    }

    public void QuitGame()
    {
        Application.Quit();
    }
}