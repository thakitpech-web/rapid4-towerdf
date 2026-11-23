using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// Build phase bar: loop and orbs, a hint line, one shop button per tower type, Sell, and Start.
/// Build the layout in the editor and wire the buttons:
///   shop button i  -> OnShopClicked(i)   (i = index in Shop)
///   Sell button    -> OnSellClicked
///   Start button   -> OnStartClicked
public class BuildPanel : MonoBehaviour
{
    [Serializable]
    private class ShopSlot
    {
        public TowerData data;
        public Button button;
        [Tooltip("Shows name and cost")] public TMP_Text label;
        [Tooltip("Optional highlight (e.g. a frame image) shown while this tower is picked")]
        public GameObject pickedMark;
    }

    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text hintText;
    [SerializeField] private ShopSlot[] shop = new ShopSlot[3];
    [SerializeField] private Button sellButton;
    [SerializeField] private TMP_Text sellLabel;

    public void Show()
    {
        gameObject.SetActive(true);
        Refresh();
    }

    public void Hide() => gameObject.SetActive(false);

    public void OnStartClicked() => GameManager.Instance.StartLoop();

    public void OnShopClicked(int index)
    {
        if (index < 0 || index >= shop.Length || shop[index].data == null) return;
        GameManager.Instance.Builder.SelectShopItem(shop[index].data.Type);
    }

    public void OnSellClicked() => GameManager.Instance.Builder.SellSelected();

    private void Update() => Refresh();

    private void Refresh()
    {
        GameManager gm = GameManager.Instance;
        if (gm == null) return;
        BuildManager builder = gm.Builder;

        if (titleText != null)
            titleText.text = $"Next: loop {gm.CurrentLoop}/{gm.TotalLoops}   ·   {gm.Orbs} orb";

        foreach (ShopSlot s in shop)
        {
            if (s == null || s.data == null) continue;
            int cost = s.data.TotalCost(1);
            if (s.label != null) s.label.text = $"{s.data.DisplayName}  ({cost})";
            if (s.button != null) s.button.interactable = gm.Orbs >= cost;
            if (s.pickedMark != null)
            {
                bool picked = builder.SelectedShopItem == s.data.Type;
                if (s.pickedMark.activeSelf != picked) s.pickedMark.SetActive(picked);
            }
        }

        Tower selected = builder.SelectedTower;
        if (sellButton != null) sellButton.interactable = selected != null;
        if (sellLabel != null) sellLabel.text = selected != null ? $"Sell  (+{selected.SellValue()})" : "Sell";

        if (hintText != null) hintText.text = Hint(builder, selected);
    }

    private static string Hint(BuildManager builder, Tower selected)
    {
        if (builder.SelectedShopItem.HasValue)
            return "Tap an empty cell (not a lane) to build. Right-click or Esc to cancel.";
        if (selected != null)
            return $"{selected.Data.DisplayName} T{selected.Tier}: tap an empty cell to move, " +
                   "or a same-type same-tier tower to merge.";
        return "Pick a tower, or tap one to move, merge or sell. Drag/WASD to pan, wheel/+/- to zoom.";
    }
}
