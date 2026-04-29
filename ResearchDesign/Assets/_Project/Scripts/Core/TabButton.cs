using UnityEngine;
using UnityEngine.UI;

public class TabButton : MonoBehaviour
{
    [SerializeField] private int tabIndex;

    [Header("Visuals")]
    [SerializeField] private Image tabImage;
    [SerializeField] private Sprite activeSprite;
    [SerializeField] private Sprite inactiveSprite;

    private void Awake()
    {
        if (tabImage == null) tabImage = GetComponent<Image>();

        var btn = GetComponent<Button>();
        if (btn != null)
            btn.onClick.AddListener(OnClick);
    }

    private void OnClick()
    {
        TabManager.Instance?.SwitchToTab(tabIndex);
    }

    public void SetActive(bool isActive)
    {
        if (tabImage == null) return;
        tabImage.sprite = isActive ? activeSprite : inactiveSprite;
    }
}
