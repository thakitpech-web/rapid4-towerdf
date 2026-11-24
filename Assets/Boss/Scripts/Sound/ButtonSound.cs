using UnityEngine;
using UnityEngine.UI;

/// Plays a click sound when the button on this object is pressed. Put it on buttons that have no sound of
/// their own (the shop, Sell and level-up card buttons already play theirs).
[RequireComponent(typeof(Button))]
public class ButtonSound : MonoBehaviour
{
    [Tooltip("Sound name in the SoundLibrary")]
    [SerializeField] private string soundName = "Button";

    private void Awake()
    {
        GetComponent<Button>().onClick.AddListener(() => SoundManager.Play(soundName));
    }
}
