using System;
using System.Collections.Generic;
using FarmMergerBattle.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FarmMergerBattle.UI
{
    public class BattleUIController : MonoBehaviour
    {
        public event Action PullPressed;
        public event Action<SlotSymbol> CardSelected;
        public event Action RestartPressed;

        private TextMeshProUGUI _waveLabel;
        private Image _waveFill;
        private TextMeshProUGUI _coinsLabel;
        private TextMeshProUGUI _pullCostLabel;
        private Button _pullButton;

        private GameObject _cardOverlay;
        private GameObject _resultOverlay;
        private TextMeshProUGUI _resultLabel;

        private readonly List<Image> _slotImages = new List<Image>();

        public void BuildUI()
        {
            var canvasGo = new GameObject("BattleCanvas");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGo.AddComponent<CanvasScaler>();
            canvasGo.AddComponent<GraphicRaycaster>();

            if (FindObjectOfType<TMPro.TMP_Settings>() == null)
            {
                // no-op; TMP defaults still work in most Unity projects once TMP package is installed
            }

            BuildTopUI(canvas.transform);
            BuildBottomUI(canvas.transform);
            BuildCardOverlay(canvas.transform);
            BuildResultOverlay(canvas.transform);
        }

        public void SetWave(int current, int total, float progress01)
        {
            _waveLabel.text = $"Wave {current}/{total}";
            _waveFill.fillAmount = Mathf.Clamp01(progress01);
        }

        public void SetCoins(int coins)
        {
            _coinsLabel.text = $"Coins: {coins}";
        }

        public void SetPullCost(int cost, bool canAfford)
        {
            _pullCostLabel.text = $"Pull ({cost})";
            _pullButton.interactable = canAfford;
        }

        public void SetSlotSymbols(IReadOnlyList<SlotSymbol> symbols, Sprite fallback)
        {
            for (var i = 0; i < _slotImages.Count; i++)
            {
                var symbol = i < symbols.Count ? symbols[i] : null;
                _slotImages[i].sprite = symbol?.slotSymbolSprite != null ? symbol.slotSymbolSprite : fallback;
            }
        }

        public void ShowCardSelection(IReadOnlyList<SlotSymbol> options, Sprite fallback)
        {
            foreach (Transform child in _cardOverlay.transform)
            {
                if (child.name == "Cards")
                {
                    Destroy(child.gameObject);
                }
            }

            var cards = new GameObject("Cards", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            cards.transform.SetParent(_cardOverlay.transform, false);
            var rect = cards.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(900f, 320f);

            var layout = cards.GetComponent<HorizontalLayoutGroup>();
            layout.spacing = 25f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = false;
            layout.childControlHeight = false;

            foreach (var option in options)
            {
                var card = new GameObject($"Card_{option.id}", typeof(Image), typeof(Button));
                card.transform.SetParent(cards.transform, false);
                var cardRect = card.GetComponent<RectTransform>();
                cardRect.sizeDelta = new Vector2(260f, 320f);
                card.GetComponent<Image>().sprite = option.cardBackground != null ? option.cardBackground : fallback;

                var icon = new GameObject("Icon", typeof(Image));
                icon.transform.SetParent(card.transform, false);
                var iconRect = icon.GetComponent<RectTransform>();
                iconRect.anchorMin = new Vector2(0.5f, 0.7f);
                iconRect.anchorMax = new Vector2(0.5f, 0.7f);
                iconRect.sizeDelta = new Vector2(90f, 90f);
                icon.GetComponent<Image>().sprite = option.cardIcon != null ? option.cardIcon : fallback;

                var label = CreateText(card.transform, option.displayName, 28, TextAlignmentOptions.Center);
                var labelRect = label.GetComponent<RectTransform>();
                labelRect.anchorMin = new Vector2(0.5f, 0.35f);
                labelRect.anchorMax = new Vector2(0.5f, 0.35f);
                labelRect.sizeDelta = new Vector2(220f, 80f);

                card.GetComponent<Button>().onClick.AddListener(() => CardSelected?.Invoke(option));
            }

            _cardOverlay.SetActive(true);
        }

        public void HideCardSelection()
        {
            _cardOverlay.SetActive(false);
        }

        public void ShowResult(bool isVictory)
        {
            _resultLabel.text = isVictory ? "Victory" : "Defeat";
            _resultOverlay.SetActive(true);
        }

        private void BuildTopUI(Transform parent)
        {
            var top = new GameObject("TopUI", typeof(RectTransform));
            top.transform.SetParent(parent, false);
            var rect = top.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.sizeDelta = new Vector2(0f, 90f);

            _waveLabel = CreateText(top.transform, "Wave 1/1", 30, TextAlignmentOptions.Center);
            var labelRect = _waveLabel.GetComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0.5f, 0.75f);
            labelRect.anchorMax = new Vector2(0.5f, 0.75f);
            labelRect.sizeDelta = new Vector2(400f, 40f);

            var barBg = new GameObject("ProgressBG", typeof(Image));
            barBg.transform.SetParent(top.transform, false);
            var barRect = barBg.GetComponent<RectTransform>();
            barRect.anchorMin = new Vector2(0.5f, 0.25f);
            barRect.anchorMax = new Vector2(0.5f, 0.25f);
            barRect.sizeDelta = new Vector2(500f, 25f);
            barBg.GetComponent<Image>().color = Color.gray;

            var barFill = new GameObject("ProgressFill", typeof(Image));
            barFill.transform.SetParent(barBg.transform, false);
            _waveFill = barFill.GetComponent<Image>();
            _waveFill.type = Image.Type.Filled;
            _waveFill.fillMethod = Image.FillMethod.Horizontal;
            _waveFill.fillOrigin = 0;
            _waveFill.color = Color.green;
            var fillRect = barFill.GetComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;
        }

        private void BuildBottomUI(Transform parent)
        {
            var bottom = new GameObject("BottomUI", typeof(RectTransform));
            bottom.transform.SetParent(parent, false);
            var rect = bottom.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.sizeDelta = new Vector2(0f, 170f);

            _coinsLabel = CreateText(bottom.transform, "Coins: 0", 30, TextAlignmentOptions.Left);
            var coinRect = _coinsLabel.GetComponent<RectTransform>();
            coinRect.anchorMin = new Vector2(0.1f, 0.6f);
            coinRect.anchorMax = new Vector2(0.1f, 0.6f);
            coinRect.sizeDelta = new Vector2(240f, 40f);

            var slotRoot = new GameObject("SlotMachine", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            slotRoot.transform.SetParent(bottom.transform, false);
            var slotRect = slotRoot.GetComponent<RectTransform>();
            slotRect.anchorMin = new Vector2(0.5f, 0.55f);
            slotRect.anchorMax = new Vector2(0.5f, 0.55f);
            slotRect.sizeDelta = new Vector2(390f, 110f);
            var layout = slotRoot.GetComponent<HorizontalLayoutGroup>();
            layout.spacing = 15;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlHeight = false;
            layout.childControlWidth = false;

            for (var i = 0; i < 3; i++)
            {
                var s = new GameObject($"Slot{i}", typeof(Image));
                s.transform.SetParent(slotRoot.transform, false);
                var sRect = s.GetComponent<RectTransform>();
                sRect.sizeDelta = new Vector2(120f, 100f);
                _slotImages.Add(s.GetComponent<Image>());
            }

            var pullButtonGo = new GameObject("PullButton", typeof(Image), typeof(Button));
            pullButtonGo.transform.SetParent(bottom.transform, false);
            var pullRect = pullButtonGo.GetComponent<RectTransform>();
            pullRect.anchorMin = new Vector2(0.88f, 0.55f);
            pullRect.anchorMax = new Vector2(0.88f, 0.55f);
            pullRect.sizeDelta = new Vector2(180f, 80f);
            pullButtonGo.GetComponent<Image>().color = new Color(0.2f, 0.6f, 0.2f, 1f);
            _pullButton = pullButtonGo.GetComponent<Button>();
            _pullButton.onClick.AddListener(() => PullPressed?.Invoke());
            _pullCostLabel = CreateText(pullButtonGo.transform, "Pull (0)", 24, TextAlignmentOptions.Center);
            var pullLabelRect = _pullCostLabel.GetComponent<RectTransform>();
            pullLabelRect.anchorMin = new Vector2(0.5f, 0.5f);
            pullLabelRect.anchorMax = new Vector2(0.5f, 0.5f);
            pullLabelRect.sizeDelta = new Vector2(170f, 50f);
        }

        private void BuildCardOverlay(Transform parent)
        {
            _cardOverlay = new GameObject("CardOverlay", typeof(Image), typeof(RectTransform));
            _cardOverlay.transform.SetParent(parent, false);
            var image = _cardOverlay.GetComponent<Image>();
            image.color = new Color(0f, 0f, 0f, 0.7f);
            var rect = _cardOverlay.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var text = CreateText(_cardOverlay.transform, "Pick 1", 48, TextAlignmentOptions.Center);
            var txtRect = text.GetComponent<RectTransform>();
            txtRect.anchorMin = new Vector2(0.5f, 0.85f);
            txtRect.anchorMax = new Vector2(0.5f, 0.85f);
            txtRect.sizeDelta = new Vector2(400f, 80f);

            _cardOverlay.SetActive(false);
        }

        private void BuildResultOverlay(Transform parent)
        {
            _resultOverlay = new GameObject("ResultOverlay", typeof(Image), typeof(RectTransform));
            _resultOverlay.transform.SetParent(parent, false);
            _resultOverlay.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.75f);
            var rect = _resultOverlay.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            _resultLabel = CreateText(_resultOverlay.transform, "Victory", 64, TextAlignmentOptions.Center);
            var resultRect = _resultLabel.GetComponent<RectTransform>();
            resultRect.anchorMin = new Vector2(0.5f, 0.62f);
            resultRect.anchorMax = new Vector2(0.5f, 0.62f);
            resultRect.sizeDelta = new Vector2(420f, 100f);

            var restartButton = new GameObject("RestartButton", typeof(Image), typeof(Button));
            restartButton.transform.SetParent(_resultOverlay.transform, false);
            restartButton.GetComponent<Image>().color = new Color(0.2f, 0.4f, 0.8f, 1f);
            var btnRect = restartButton.GetComponent<RectTransform>();
            btnRect.anchorMin = new Vector2(0.5f, 0.42f);
            btnRect.anchorMax = new Vector2(0.5f, 0.42f);
            btnRect.sizeDelta = new Vector2(220f, 90f);
            restartButton.GetComponent<Button>().onClick.AddListener(() => RestartPressed?.Invoke());

            var txt = CreateText(restartButton.transform, "Restart", 32, TextAlignmentOptions.Center);
            var txtRect = txt.GetComponent<RectTransform>();
            txtRect.anchorMin = new Vector2(0.5f, 0.5f);
            txtRect.anchorMax = new Vector2(0.5f, 0.5f);
            txtRect.sizeDelta = new Vector2(180f, 60f);

            _resultOverlay.SetActive(false);
        }

        private static TextMeshProUGUI CreateText(Transform parent, string value, int size, TextAlignmentOptions align)
        {
            var text = new GameObject("Text", typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();
            text.transform.SetParent(parent, false);
            text.fontSize = size;
            text.alignment = align;
            text.color = Color.white;
            text.text = value;
            return text;
        }
    }
}
