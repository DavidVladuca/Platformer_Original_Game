using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    [SerializeField] string sceneName;
    [SerializeField] GameObject player;
    [SerializeField] GameObject startScreen;
    [SerializeField] GameObject playScreen;
    [SerializeField] GameObject pauseScreen;
    [SerializeField] AudioSource audioSource;
    [SerializeField] GameObject[] fullHearts;
    [SerializeField] GameObject[] emptyHearts;
    [SerializeField] Image dash;
    [SerializeField] TMP_Text ScoreText;
    [SerializeField] TMP_Text HeartNo;

    public bool gamePaused;
    public bool loseLife;
    public bool useDash;

    private int score;
    private int lives;
    private float startTime;

    void Start()
    {
        Application.targetFrameRate = 120;
        startScreen.SetActive(true);
        playScreen.SetActive(false);
        pauseScreen.SetActive(false);
        Time.timeScale = 0;
        audioSource.volume = 0.1f;
        lives = fullHearts.Length;
        HeartNo.text = "x " + lives;
        for (int i = 0; i < lives; i++)
        {
            fullHearts[i].SetActive(true);
            emptyHearts[i].SetActive(false);
        }
        startTime = Time.time;
        ResetScore();
    }

    void Update()
    {
        if (loseLife)
        {
            loseLife = false;
            Die();
        }

        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.P))
        {
            if (!gamePaused) StopGame();
            else StartGame();
        }

        int sec = Mathf.RoundToInt((Time.time - startTime) % 60);
        int min = Mathf.RoundToInt((Time.time - startTime) / 60);
        ScoreText.text = (min < 10 ? "0" : "") + min.ToString() + ":" + (sec < 10 ? "0" : "") + sec.ToString();
    }

    public void StartGame()
    {
        Time.timeScale = 1;
        gamePaused = false;
        startScreen.SetActive(false);
        playScreen.SetActive(true);
        pauseScreen.SetActive(false);
        audioSource.volume = 0.2f;
    }

    public void StopGame()
    {
        Time.timeScale = 0;
        gamePaused = true;
        startScreen.SetActive(false);
        playScreen.SetActive(false);
        pauseScreen.SetActive(true);
        audioSource.volume = 0.05f;
    }

    public void Die()
    {
        lives--;
        if (lives == 0)
        {
            // Instantly restart instead of waiting
            ResetGame();
        }
        else
        {
            HeartNo.text = "x " + lives;
            fullHearts[lives].SetActive(false);
            emptyHearts[lives].SetActive(true);
        }
    }

    public void UseDash()
    {
        SetDash(1);
    }

    public void AddScore(int sc)
    {
        score += sc;
        ScoreText.text = "Score: " + score;
    }

    public void ResetScore()
    {
        score = 0;
        ScoreText.text = "Score: " + score;
    }

    public void ResetGame()
    {
        SceneManager.LoadScene(sceneName);
    }

    public void QuitGame()
    {
        Application.Quit();
    }

    public void SetDash(float proc)
    {
        dash.fillAmount = 1 - Mathf.Clamp01(proc);
    }
}
