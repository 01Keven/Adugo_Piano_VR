using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.UI;

namespace Pianinho.UI
{
    /// <summary>
    /// Monta painéis de interface em world space, no estilo do Pianinho: formas simples, cores de
    /// baixa saturação e botões grandes o bastante para tocar com o dedo.
    /// </summary>
    public static class UIFactory
    {
        public const float PixelsPerMeter = 1000f;
        public const float RowHeight = 52f;

        public static readonly Color PanelColor = new Color(0.20f, 0.23f, 0.29f, 0.94f);
        public static readonly Color ButtonColor = new Color(0.40f, 0.52f, 0.68f);
        public static readonly Color SelectedColor = new Color(0.42f, 0.62f, 0.86f);
        public static readonly Color UnselectedColor = new Color(0.31f, 0.35f, 0.42f);
        public static readonly Color PrimaryColor = new Color(0.42f, 0.66f, 0.50f);
        public static readonly Color DangerColor = new Color(0.74f, 0.47f, 0.45f);
        public static readonly Color DisabledColor = new Color(0.33f, 0.35f, 0.38f);
        public static readonly Color TextColor = new Color(0.95f, 0.95f, 0.93f);
        public static readonly Color MutedTextColor = new Color(0.75f, 0.78f, 0.82f);

        /// <summary>Canvas world space que cresce na vertical conforme o conteúdo. Aceita laser e toque.</summary>
        public static RectTransform CreatePanel(string name, Transform parent, float width)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler),
                typeof(TrackedDeviceGraphicRaycaster), typeof(ContentSizeFitter), typeof(Image), typeof(VerticalLayoutGroup));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            go.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;
            rect.sizeDelta = new Vector2(width, 400f);
            rect.localScale = Vector3.one / PixelsPerMeter;
            go.GetComponent<CanvasScaler>().dynamicPixelsPerUnit = 3f;
            go.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            go.GetComponent<Image>().color = PanelColor;
            var layout = go.GetComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(22, 22, 20, 20);
            layout.spacing = 10f;
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            return rect;
        }

        /// <summary>Grupo vertical sem fundo, para trocar páginas inteiras ligando e desligando.</summary>
        public static RectTransform CreateGroup(Transform parent, string name = "Group")
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(VerticalLayoutGroup));
            go.transform.SetParent(parent, false);
            var layout = go.GetComponent<VerticalLayoutGroup>();
            layout.spacing = 10f;
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            return (RectTransform)go.transform;
        }

        public static RectTransform CreateRow(Transform parent, float height = RowHeight)
        {
            var go = new GameObject("Row", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            go.transform.SetParent(parent, false);
            var layout = go.GetComponent<HorizontalLayoutGroup>();
            layout.spacing = 8f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            go.GetComponent<LayoutElement>().preferredHeight = height;
            return (RectTransform)go.transform;
        }

        public static TextMeshProUGUI CreateText(Transform parent, string text, float size = 22f,
            FontStyles style = FontStyles.Normal, float height = 34f, TextAlignmentOptions align = TextAlignmentOptions.Center, Color? color = null)
        {
            var go = new GameObject("Text", typeof(RectTransform), typeof(LayoutElement));
            go.transform.SetParent(parent, false);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = size;
            tmp.fontStyle = style;
            tmp.color = color ?? TextColor;
            tmp.alignment = align;
            tmp.raycastTarget = false;
            tmp.textWrappingMode = TextWrappingModes.Normal;
            var element = go.GetComponent<LayoutElement>();
            element.preferredHeight = height;
            element.flexibleWidth = 1f;
            return tmp;
        }

        /// <summary>Rótulo à esquerda de uma linha, com largura fixa para alinhar as opções.</summary>
        public static TextMeshProUGUI CreateLabel(Transform row, string text, float width = 130f)
        {
            var label = CreateText(row, text, 22f, FontStyles.Normal, RowHeight, TextAlignmentOptions.Left);
            var element = label.GetComponent<LayoutElement>();
            element.preferredWidth = width;
            element.flexibleWidth = 0f;
            return label;
        }

        public static Button CreateButton(Transform parent, string text, Action onClick, Color? color = null,
            float width = -1f, float flexible = 1f, float height = RowHeight)
        {
            var go = new GameObject($"Button {text}", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            go.transform.SetParent(parent, false);
            var image = go.GetComponent<Image>();
            image.color = color ?? ButtonColor;
            var button = go.GetComponent<Button>();
            button.targetGraphic = image;
            var colors = button.colors;
            colors.highlightedColor = new Color(1.08f, 1.08f, 1.08f);
            colors.pressedColor = new Color(0.8f, 0.8f, 0.8f);
            colors.disabledColor = new Color(0.7f, 0.7f, 0.7f, 0.6f);
            button.colors = colors;
            if (onClick != null)
                button.onClick.AddListener(() => onClick());

            var element = go.GetComponent<LayoutElement>();
            element.preferredHeight = height;
            if (width > 0f)
            {
                element.preferredWidth = width;
                element.flexibleWidth = 0f;
            }
            else
            {
                element.flexibleWidth = flexible;
            }

            var label = CreateText(go.transform, text, 22f, FontStyles.Bold, height);
            // Encolhe o texto em vez de quebrar a linha quando o botão é estreito.
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.enableAutoSizing = true;
            label.fontSizeMin = 14f;
            label.fontSizeMax = 22f;
            label.margin = new Vector4(6f, 0f, 6f, 0f);
            var rt = label.rectTransform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            return button;
        }

        public static TextMeshProUGUI GetLabel(this Button button) => button.GetComponentInChildren<TextMeshProUGUI>(true);

        public static void SetColor(this Button button, Color color) => button.GetComponent<Image>().color = color;

        /// <summary>Linha de botões em que um fica selecionado (por exemplo Guiado | Reflexo).</summary>
        public static Segmented CreateSegmented(Transform row, string[] options, Action<int> onSelect)
        {
            var segmented = new Segmented();
            for (var i = 0; i < options.Length; i++)
            {
                var index = i;
                segmented.buttons.Add(CreateButton(row, options[i], () =>
                {
                    segmented.Select(index);
                    onSelect(index);
                }, UnselectedColor));
            }
            return segmented;
        }

        public class Segmented
        {
            public readonly List<Button> buttons = new List<Button>();
            public int selected { get; private set; } = -1;

            public void Select(int index)
            {
                selected = index;
                for (var i = 0; i < buttons.Count; i++)
                    buttons[i].SetColor(i == index ? SelectedColor : UnselectedColor);
            }
        }
    }
}
