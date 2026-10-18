
using System;
using UnityEngine;
using TMPro;

public class ContactMatchingGame : MonoBehaviour
{
    [Header("Четыре подвижных контакта")]
    [SerializeField] private ContactDrag[] contacts;

    [Header("Таймер")]
    [SerializeField] private float gameDuration = 20f;
    [SerializeField] private TMP_Text timerText;

    [Header("Результат")]
    [SerializeField] private GameObject resultPanel;
    [SerializeField] private TMP_Text resultText;

    private Vector2[] initialSlots;
    private int connectedCount;
    private float remainingTime;
    private bool gameEnded;

    public bool IsGameEnded => gameEnded;

    // Позже можно подключить к основной игре
    public event Action<bool> OnGameFinished;

    private void Awake()
    {
        if (resultPanel != null)
            resultPanel.SetActive(false);
    }

    private void Start()
    {
        if (contacts == null || contacts.Length != 4)
        {
            Debug.LogError("Нужно назначить 4 контакта!");
            enabled = false;
            return;
        }

        initialSlots = new Vector2[4];

        for (int i = 0; i < contacts.Length; i++)
        {
            if (contacts[i] == null)
            {
                Debug.LogError("Один из контактов не назначен!");
                enabled = false;
                return;
            }

            initialSlots[i] = contacts[i].GetPosition();
        }

        StartGame();
    }

    private void StartGame()
    {
        connectedCount = 0;
        remainingTime = Mathf.Max(0, gameDuration);
        gameEnded = false;

        if (resultPanel != null)
            resultPanel.SetActive(false);

        if (resultText != null)
            resultText.text = "";

        ShuffleContacts();
        UpdateTimer();

        if (remainingTime <= 0f)
            LoseGame();
    }

    private void ShuffleContacts()
    {
        int count = contacts.Length;
        int[] order = new int[count];

        for (int i = 0; i < count; i++)
            order[i] = i;

        // Алгоритм Фишера — Йетса
        for (int i = count - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);

            (order[i], order[j]) =
                (order[j], order[i]);
        }

        // Исключаем исходный порядок
        bool unchanged = true;

        for (int i = 0; i < count; i++)
        {
            if (order[i] != i)
            {
                unchanged = false;
                break;
            }
        }

        if (unchanged)
        {
            (order[0], order[1]) =
                (order[1], order[0]);
        }

        // Переставляем штекеры по позициям
        for (int i = 0; i < count; i++)
        {
            contacts[i].Initialize(this);
            contacts[i].ResetContact(
                initialSlots[order[i]]
            );
        }
    }

    private void Update()
    {
        if (gameEnded)
            return;

        remainingTime -= Time.deltaTime;

        if (remainingTime <= 0f)
        {
            remainingTime = 0f;
            UpdateTimer();
            LoseGame();
            return;
        }

        UpdateTimer();
    }

    private void UpdateTimer()
    {
        if (timerText == null)
            return;

        int seconds = Mathf.CeilToInt(
            Mathf.Max(0f, remainingTime)
        );

        timerText.text =
            $"{seconds / 60:00}:{seconds % 60:00}";

        timerText.color =
            remainingTime <= 5f
                ? Color.red
                : Color.white;
    }

    public void ContactConnected()
    {
        if (gameEnded)
            return;

        connectedCount++;

        Debug.Log($"Соединено: {connectedCount}/4");

        if (connectedCount >= contacts.Length)
            WinGame();
    }

    private void WinGame()
    {
        FinishGame(true, "ПОБЕДА!");
    }

    private void LoseGame()
    {
        FinishGame(false, "ВРЕМЯ ВЫШЛО!");
    }

    private void FinishGame(bool victory, string message)
    {
        if (gameEnded)
            return;

        gameEnded = true;

        if (resultText != null)
            resultText.text = message;

        if (resultPanel != null)
            resultPanel.SetActive(true);

        Debug.Log(victory
            ? "Мини-игра пройдена!"
            : "Мини-игра проиграна!");

        OnGameFinished?.Invoke(victory);
    }
}
