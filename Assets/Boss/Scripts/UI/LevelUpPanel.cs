using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

/// The 1-of-3 card choice. Wire each card Button's OnClick to OnCardClicked with its index (0, 1, 2).
/// Keys 1, 2, 3 also pick a card.
public class LevelUpPanel : MonoBehaviour
{
    [Serializable]
    private class Card
    {
        public GameObject root;
        public TMP_Text tagText;
        public TMP_Text nameText;
        public TMP_Text descriptionText;
    }

    [SerializeField] private TMP_Text titleText;
    [SerializeField] private Card[] cards = new Card[3];

    private List<Upgrade> choices;

    public void Show(List<Upgrade> choices)
    {
        this.choices = choices;

        LevelSystem levels = GameManager.Instance.Levels;
        int levelReached = levels.Level - levels.PendingLevelUps + 1;
        int more = levels.PendingLevelUps - 1;
        titleText.text = more > 0 ? $"Level {levelReached}  ({more} more)" : $"Level {levelReached}";

        for (int i = 0; i < cards.Length; i++)
        {
            bool used = i < choices.Count;
            cards[i].root.SetActive(used);
            if (!used) continue;

            Upgrade u = choices[i];
            cards[i].tagText.text = $"{i + 1} · {u.Tag}";
            cards[i].nameText.text = u.Name;
            cards[i].descriptionText.text = u.Description;
        }

        gameObject.SetActive(true);
    }

    public void Hide() => gameObject.SetActive(false);

    public void OnCardClicked(int index)
    {
        if (choices == null || index < 0 || index >= choices.Count) return;
        GameManager.Instance.ChooseUpgrade(choices[index]);
    }

    private void Update()
    {
        Keyboard kb = Keyboard.current;
        if (kb == null) return;

        if (kb.digit1Key.wasPressedThisFrame) OnCardClicked(0);
        else if (kb.digit2Key.wasPressedThisFrame) OnCardClicked(1);
        else if (kb.digit3Key.wasPressedThisFrame) OnCardClicked(2);
    }
}
