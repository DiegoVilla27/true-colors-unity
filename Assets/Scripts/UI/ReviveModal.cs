using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Responsabilidad Única (SRP): Administra la interacción del modal de Revivir (RevivePanel).
/// Muestra las revividas disponibles en la run actual (máximo 2), y canaliza la solicitud
/// a través de AdsManager para otorgar la recompensa únicamente si se ve el video completo.
/// </summary>
public class ReviveModal : MonoBehaviour
{
    public static ReviveModal Instance { get; private set; }

    [Header("Referencias UI")]
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI questionText;
    [SerializeField] private TextMeshProUGUI explanationText;
    [SerializeField] private Button confirmButton;
    [SerializeField] private Button cancelButton;

    private void Awake()
    {
        Instance = this;
        AutoBindComponents();
    }

    private void OnEnable()
    {
        UpdateDisplay();
    }

    private void Start()
    {
        WireButtons();
    }

    /// <summary>
    /// Vincula automáticamente las referencias si no fueron asignadas en el Inspector.
    /// </summary>
    public void AutoBindComponents()
    {
        if (titleText == null)
        {
            var t = transform.Find("Container/Header/Title");
            if (t != null) titleText = t.GetComponent<TextMeshProUGUI>();
        }

        if (questionText == null)
        {
            var q = transform.Find("Container/Content/QuestionText");
            if (q != null) questionText = q.GetComponent<TextMeshProUGUI>();
        }

        if (explanationText == null)
        {
            var e = transform.Find("Container/Content/ExplanationText");
            if (e != null) explanationText = e.GetComponent<TextMeshProUGUI>();
        }

        if (confirmButton == null)
        {
            var btn = transform.Find("Container/Content/Buttons/BtnConfirm");
            if (btn != null) confirmButton = btn.GetComponent<Button>();
        }

        if (cancelButton == null)
        {
            var btn = transform.Find("Container/Content/Buttons/BtnCancel");
            if (btn != null) cancelButton = btn.GetComponent<Button>();
        }
    }

    private void WireButtons()
    {
        if (confirmButton != null)
        {
            confirmButton.onClick.RemoveListener(HandleConfirmClicked);
            confirmButton.onClick.AddListener(HandleConfirmClicked);
        }

        if (cancelButton != null)
        {
            cancelButton.onClick.RemoveListener(HandleCancelClicked);
            cancelButton.onClick.AddListener(HandleCancelClicked);
        }
    }

    /// <summary>
    /// Actualiza los textos y disponibilidad del botón según las revividas restantes en la partida.
    /// </summary>
    public void UpdateDisplay()
    {
        AutoBindComponents();

        int remaining = AdsManager.Instance != null ? AdsManager.Instance.RemainingRevives : 2;
        int max = AdsManager.Instance != null ? AdsManager.Instance.MaxRevivesPerGame : 2;

        if (titleText != null)
        {
            titleText.text = "REVIVE";
        }

        if (remaining > 0)
        {
            if (questionText != null)
            {
                questionText.text = "DO YOU WANT\nTO REVIVE?";
            }

            if (explanationText != null)
            {
                explanationText.text = $"YOU WILL KEEP THE SAME POINTS.\n<color=#FFD700>({remaining} OF {max} REVIVES LEFT)</color>";
            }

            if (confirmButton != null)
            {
                confirmButton.interactable = true;
            }
        }
        else
        {
            if (questionText != null)
            {
                questionText.text = "NO REVIVES\nLEFT THIS RUN";
            }

            if (explanationText != null)
            {
                explanationText.text = "YOU HAVE REACHED THE MAXIMUM OF 2 REVIVES.";
            }

            if (confirmButton != null)
            {
                confirmButton.interactable = false;
            }
        }
    }

    /// <summary>
    /// El jugador pulsa CONFIRMAR: se solicita ver el anuncio largo.
    /// </summary>
    public void HandleConfirmClicked()
    {
        if (AdsManager.Instance != null && AdsManager.Instance.IsAdShowing) return;

        if (AdsManager.Instance == null)
        {
            gameObject.SetActive(false);
            GameManager.Instance?.ReviveGame();
            return;
        }

        if (!AdsManager.Instance.CanRevive)
        {
            HandleCancelClicked();
            return;
        }

        if (confirmButton != null) confirmButton.interactable = false;

        AdsManager.Instance.RequestRevive(
            onSuccess: () =>
            {
                if (confirmButton != null) confirmButton.interactable = true;
                gameObject.SetActive(false);
                GameManager.Instance?.ReviveGame();
            },
            onFailed: () =>
            {
                if (confirmButton != null) confirmButton.interactable = true;
                HandleCancelClicked();
            }
        );
    }

    /// <summary>
    /// El jugador pulsa CANCELAR: simplemente cierra este modal y regresa a GameOver.
    /// </summary>
    public void HandleCancelClicked()
    {
        if (AdsManager.Instance != null && AdsManager.Instance.IsAdShowing) return;

        gameObject.SetActive(false);
        if (confirmButton != null) confirmButton.interactable = true;
    }
}
