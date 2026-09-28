using System;
using UnityEngine;
using UnityEngine.UI;
using GigaGrub.Cosmetics;

namespace GigaGrub.UI.Customization
{
    public class CosmeticItemCardUI : MonoBehaviour
    {
        [Header("Card UI References")]
        [SerializeField] private Image iconImage;
        [SerializeField] private Text nameText;
        [SerializeField] private Text statusText;
        [SerializeField] private Image rarityBorder;
        [SerializeField] private Image selectionHighlight;
        [SerializeField] private Image equippedBadge;
        [SerializeField] private Button button;

        private CosmeticData itemData;
        private bool isSelected = false;
        private Action<CosmeticData> onClickCallback;

        public CosmeticData ItemData => itemData;
        public bool IsSelected => isSelected;

        public void Bind(CosmeticData data, bool isUnlocked, bool isEquipped, bool selected, Action<CosmeticData> onClick)
        {
            EnsureHierarchy();

            itemData = data;
            isSelected = selected;
            onClickCallback = onClick;

            if (button != null)
            {
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(OnClicked);
            }

            if (data == null)
            {
                // Unequip / None option card
                if (nameText != null) nameText.text = "None";
                if (statusText != null)
                {
                    statusText.text = isEquipped ? "EQUIPPED" : "UNEQUIP";
                    statusText.color = isEquipped ? new Color(0.29f, 0.87f, 0.50f) : new Color(0.70f, 0.75f, 0.85f);
                }
                if (iconImage != null) iconImage.gameObject.SetActive(false);
                if (rarityBorder != null) rarityBorder.color = new Color(0.35f, 0.40f, 0.50f, 0.8f);
                if (selectionHighlight != null) selectionHighlight.gameObject.SetActive(selected);
                if (equippedBadge != null) equippedBadge.gameObject.SetActive(isEquipped);
                return;
            }

            if (nameText != null)
            {
                nameText.text = data.DisplayName;
            }

            if (iconImage != null)
            {
                iconImage.gameObject.SetActive(true);
                Sprite previewSpr = data.PreviewSprite;
                if (previewSpr != null)
                {
                    iconImage.sprite = previewSpr;
                }

                if (data is SkinColorData colData)
                {
                    iconImage.color = colData.PrimaryColor;
                }
                else
                {
                    iconImage.color = Color.white;
                }
            }

            if (rarityBorder != null)
            {
                rarityBorder.color = data.GetRarityColor();
            }

            if (equippedBadge != null)
            {
                equippedBadge.gameObject.SetActive(isEquipped);
            }

            if (selectionHighlight != null)
            {
                selectionHighlight.gameObject.SetActive(selected);
            }

            if (statusText != null)
            {
                if (isEquipped)
                {
                    statusText.text = "EQUIPPED";
                    statusText.color = new Color(0.2f, 0.95f, 0.5f);
                }
                else if (isUnlocked)
                {
                    statusText.text = "UNLOCKED";
                    statusText.color = new Color(0.8f, 0.85f, 0.9f);
                }
                else
                {
                    if (data.UnlockType == UnlockType.Coins)
                    {
                        statusText.text = $"{data.Price:N0} Coins";
                        statusText.color = new Color(1f, 0.85f, 0.2f);
                    }
                    else
                    {
                        statusText.text = data.UnlockRequirement;
                        statusText.color = new Color(0.95f, 0.4f, 0.4f);
                    }
                }
            }
        }

        public void SetSelected(bool selected)
        {
            isSelected = selected;
            if (selectionHighlight != null)
            {
                selectionHighlight.gameObject.SetActive(selected);
            }
        }

        private void EnsureHierarchy()
        {
            if (button == null)
            {
                button = GetComponent<Button>() ?? gameObject.AddComponent<Button>();
            }

            Image baseImg = GetComponent<Image>();
            if (baseImg == null)
            {
                baseImg = gameObject.AddComponent<Image>();
            }
            baseImg.color = new Color(0.08f, 0.12f, 0.18f, 0.95f); // Deep Slate Card Bg

            // 1. Selection Highlight (Glow border)
            if (selectionHighlight == null)
            {
                Transform hlT = transform.Find("Highlight");
                if (hlT == null)
                {
                    GameObject hlGo = new GameObject("Highlight", typeof(RectTransform), typeof(Image));
                    hlGo.transform.SetParent(transform, false);
                    hlGo.transform.SetSiblingIndex(0);
                    hlT = hlGo.transform;
                }
                RectTransform hlRt = hlT.GetComponent<RectTransform>();
                hlRt.anchorMin = Vector2.zero;
                hlRt.anchorMax = Vector2.one;
                hlRt.offsetMin = new Vector2(-4f, -4f);
                hlRt.offsetMax = new Vector2(4f, 4f);

                selectionHighlight = hlT.GetComponent<Image>();
                selectionHighlight.color = new Color(0.22f, 0.74f, 0.97f, 0.95f); // Cyan Glow
                selectionHighlight.gameObject.SetActive(false);
            }

            // 2. Rarity Border
            if (rarityBorder == null)
            {
                Transform bT = transform.Find("RarityBorder");
                if (bT == null)
                {
                    GameObject bGo = new GameObject("RarityBorder", typeof(RectTransform), typeof(Image));
                    bGo.transform.SetParent(transform, false);
                    bT = bGo.transform;
                }
                RectTransform bRt = bT.GetComponent<RectTransform>();
                bRt.anchorMin = Vector2.zero;
                bRt.anchorMax = Vector2.one;
                bRt.offsetMin = Vector2.zero;
                bRt.offsetMax = Vector2.zero;

                rarityBorder = bT.GetComponent<Image>();
                rarityBorder.color = new Color(0.25f, 0.32f, 0.45f, 1f);

                // Inner card fill to leave 2px border
                Transform inT = bT.Find("InnerFill");
                if (inT == null)
                {
                    GameObject inGo = new GameObject("InnerFill", typeof(RectTransform), typeof(Image));
                    inGo.transform.SetParent(bT, false);
                    inT = inGo.transform;
                }
                RectTransform inRt = inT.GetComponent<RectTransform>();
                inRt.anchorMin = Vector2.zero;
                inRt.anchorMax = Vector2.one;
                inRt.offsetMin = new Vector2(2.5f, 2.5f);
                inRt.offsetMax = new Vector2(-2.5f, -2.5f);
                Image inImg = inT.GetComponent<Image>();
                inImg.color = new Color(0.08f, 0.11f, 0.17f, 1f);
            }

            // 3. Icon Image
            if (iconImage == null)
            {
                Transform icT = transform.Find("ItemIcon");
                if (icT == null)
                {
                    GameObject icGo = new GameObject("ItemIcon", typeof(RectTransform), typeof(Image));
                    icGo.transform.SetParent(transform, false);
                    icT = icGo.transform;
                }
                RectTransform icRt = icT.GetComponent<RectTransform>();
                icRt.anchorMin = new Vector2(0.5f, 0.62f);
                icRt.anchorMax = new Vector2(0.5f, 0.62f);
                icRt.pivot = new Vector2(0.5f, 0.5f);
                icRt.sizeDelta = new Vector2(58f, 58f);

                iconImage = icT.GetComponent<Image>();
                iconImage.preserveAspect = true;
                iconImage.raycastTarget = false;
            }

            // 4. Name Text
            if (nameText == null)
            {
                Transform nT = transform.Find("ItemName");
                if (nT == null)
                {
                    GameObject nGo = new GameObject("ItemName", typeof(RectTransform), typeof(Text));
                    nGo.transform.SetParent(transform, false);
                    nT = nGo.transform;
                }
                RectTransform nRt = nT.GetComponent<RectTransform>();
                nRt.anchorMin = new Vector2(0.04f, 0.20f);
                nRt.anchorMax = new Vector2(0.96f, 0.36f);
                nRt.offsetMin = Vector2.zero;
                nRt.offsetMax = Vector2.zero;

                nameText = nT.GetComponent<Text>();
                nameText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                nameText.fontSize = 12;
                nameText.fontStyle = FontStyle.Bold;
                nameText.alignment = TextAnchor.MiddleCenter;
                nameText.color = Color.white;
                nameText.raycastTarget = false;
            }

            // 5. Status / Price Text
            if (statusText == null)
            {
                Transform sT = transform.Find("ItemStatus");
                if (sT == null)
                {
                    GameObject sGo = new GameObject("ItemStatus", typeof(RectTransform), typeof(Text));
                    sGo.transform.SetParent(transform, false);
                    sT = sGo.transform;
                }
                RectTransform sRt = sT.GetComponent<RectTransform>();
                sRt.anchorMin = new Vector2(0.04f, 0.03f);
                sRt.anchorMax = new Vector2(0.96f, 0.19f);
                sRt.offsetMin = Vector2.zero;
                sRt.offsetMax = Vector2.zero;

                statusText = sT.GetComponent<Text>();
                statusText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                statusText.fontSize = 11;
                statusText.fontStyle = FontStyle.Bold;
                statusText.alignment = TextAnchor.MiddleCenter;
                statusText.color = new Color(0.80f, 0.84f, 0.90f);
                statusText.raycastTarget = false;
            }

            // 6. Equipped Badge
            if (equippedBadge == null)
            {
                Transform ebT = transform.Find("EquippedBadge");
                if (ebT == null)
                {
                    GameObject ebGo = new GameObject("EquippedBadge", typeof(RectTransform), typeof(Image));
                    ebGo.transform.SetParent(transform, false);
                    ebT = ebGo.transform;

                    GameObject ebTxtGo = new GameObject("Text", typeof(RectTransform), typeof(Text));
                    ebTxtGo.transform.SetParent(ebGo.transform, false);
                    RectTransform ebtRt = ebTxtGo.GetComponent<RectTransform>();
                    ebtRt.anchorMin = Vector2.zero;
                    ebtRt.anchorMax = Vector2.one;
                    ebtRt.offsetMin = Vector2.zero;
                    ebtRt.offsetMax = Vector2.zero;

                    Text ebTxt = ebTxtGo.GetComponent<Text>();
                    ebTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                    ebTxt.fontSize = 10;
                    ebTxt.fontStyle = FontStyle.Bold;
                    ebTxt.alignment = TextAnchor.MiddleCenter;
                    ebTxt.color = Color.white;
                    ebTxt.text = "EQ";
                    ebTxt.raycastTarget = false;
                }
                RectTransform ebRt = ebT.GetComponent<RectTransform>();
                ebRt.anchorMin = new Vector2(1f, 1f);
                ebRt.anchorMax = new Vector2(1f, 1f);
                ebRt.pivot = new Vector2(1f, 1f);
                ebRt.anchoredPosition = new Vector2(-3f, -3f);
                ebRt.sizeDelta = new Vector2(22f, 22f);

                equippedBadge = ebT.GetComponent<Image>();
                equippedBadge.color = new Color(0.06f, 0.73f, 0.51f, 1f); // Emerald
                equippedBadge.gameObject.SetActive(false);
            }
        }

        private void OnClicked()
        {
            onClickCallback?.Invoke(itemData);
        }
    }
}
