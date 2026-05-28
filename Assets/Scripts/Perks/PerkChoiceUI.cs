using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class PerkChoiceUI : MonoBehaviour
{
    [Header("Data")]
    public RunData runData;

    [Header("Optional Perk Pool (ScriptableObjects)")]
    public List<PerkDefinition> perkPool = new List<PerkDefinition>();

    public bool IsOpen => canvas != null && canvas.gameObject.activeSelf;

    private Canvas canvas;
    private Button[] buttons;
    private Text titleText;
    private Text[] buttonTexts;
    private Action<PerkDefinition> onChosen;
    private PerkDefinition[] currentChoices;

    private void Awake()
    {
        if (runData == null)
            runData = Resources.FindObjectsOfTypeAll<RunData>() != null ? runData : runData;
    }

    public void ShowChoices(PerkDefinition[] choices, Action<PerkDefinition> onChosen)
    {
        if (choices == null || choices.Length == 0) return;
        EnsureBuilt();
        EnsureEventSystem();

        this.onChosen = onChosen;
        currentChoices = choices;

        for (int i = 0; i < buttons.Length; i++)
        {
            int idx = i;
            if (idx >= choices.Length)
            {
                buttons[i].gameObject.SetActive(false);
                continue;
            }

            buttons[i].gameObject.SetActive(true);
            var perk = choices[idx];
            buttonTexts[i].text = FormatPerk(perk);
            buttons[i].onClick.RemoveAllListeners();
            buttons[i].onClick.AddListener(() => Choose(idx));
        }

        canvas.gameObject.SetActive(true);
    }

    public void Close()
    {
        if (canvas != null)
            canvas.gameObject.SetActive(false);
    }

    private void Choose(int index)
    {
        if (currentChoices == null || index < 0 || index >= currentChoices.Length) return;
        var chosen = currentChoices[index];
        Close();
        onChosen?.Invoke(chosen);
    }

    private void Update()
    {
        if (!IsOpen) return;

        if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1)) Choose(0);
        if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2)) Choose(1);
        if (Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Keypad3)) Choose(2);
    }

    private void EnsureBuilt()
    {
        if (canvas != null) return;

        GameObject root = new GameObject("PerkChoiceUI");
        root.transform.SetParent(transform, false);

        canvas = root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        root.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        root.AddComponent<GraphicRaycaster>();

        GameObject panelGO = new GameObject("Panel");
        panelGO.transform.SetParent(root.transform, false);
        Image panel = panelGO.AddComponent<Image>();
        panel.color = new Color(0f, 0f, 0f, 0.75f);

        RectTransform panelRt = panel.GetComponent<RectTransform>();
        panelRt.anchorMin = new Vector2(0.1f, 0.15f);
        panelRt.anchorMax = new Vector2(0.9f, 0.85f);
        panelRt.offsetMin = Vector2.zero;
        panelRt.offsetMax = Vector2.zero;

        GameObject titleGO = new GameObject("Title");
        titleGO.transform.SetParent(panelGO.transform, false);
        titleText = titleGO.AddComponent<Text>();
        titleText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        titleText.text = "Choose an upgrade";
        titleText.alignment = TextAnchor.MiddleCenter;
        titleText.color = Color.white;
        titleText.fontSize = 36;

        RectTransform titleRt = titleGO.GetComponent<RectTransform>();
        titleRt.anchorMin = new Vector2(0f, 0.82f);
        titleRt.anchorMax = new Vector2(1f, 0.98f);
        titleRt.offsetMin = Vector2.zero;
        titleRt.offsetMax = Vector2.zero;

        buttons = new Button[3];
        buttonTexts = new Text[3];

        for (int i = 0; i < 3; i++)
        {
            GameObject btnGO = new GameObject($"Choice_{i + 1}");
            btnGO.transform.SetParent(panelGO.transform, false);
            Image img = btnGO.AddComponent<Image>();
            img.color = new Color(0.15f, 0.15f, 0.15f, 0.95f);

            Button btn = btnGO.AddComponent<Button>();
            buttons[i] = btn;

            GameObject txtGO = new GameObject("Text");
            txtGO.transform.SetParent(btnGO.transform, false);
            Text txt = txtGO.AddComponent<Text>();
            txt.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = Color.white;
            txt.fontSize = 24;
            txt.horizontalOverflow = HorizontalWrapMode.Wrap;
            txt.verticalOverflow = VerticalWrapMode.Overflow;
            buttonTexts[i] = txt;

            RectTransform btnRt = btnGO.GetComponent<RectTransform>();
            float yMin = 0.58f - (i * 0.22f);
            float yMax = yMin + 0.18f;
            btnRt.anchorMin = new Vector2(0.15f, yMin);
            btnRt.anchorMax = new Vector2(0.85f, yMax);
            btnRt.offsetMin = Vector2.zero;
            btnRt.offsetMax = Vector2.zero;

            RectTransform txtRt = txtGO.GetComponent<RectTransform>();
            txtRt.anchorMin = new Vector2(0.05f, 0.05f);
            txtRt.anchorMax = new Vector2(0.95f, 0.95f);
            txtRt.offsetMin = Vector2.zero;
            txtRt.offsetMax = Vector2.zero;
        }

        canvas.gameObject.SetActive(false);
    }

    private static void EnsureEventSystem()
    {
        if (FindObjectOfType<EventSystem>() != null) return;

        GameObject es = new GameObject("EventSystem");
        es.AddComponent<EventSystem>();
        es.AddComponent<StandaloneInputModule>();
    }

    private static string FormatPerk(PerkDefinition perk)
    {
        if (perk == null) return "Unknown";
        if (!string.IsNullOrEmpty(perk.displayName) && !string.IsNullOrEmpty(perk.description))
            return $"{perk.displayName}\n{perk.description}";
        return !string.IsNullOrEmpty(perk.displayName) ? perk.displayName : perk.name;
    }
}

