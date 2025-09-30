// LocalizedText.cs
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Text))]
public class LocalizedText : MonoBehaviour
{
    public string key;
    Text _text;

    void Awake() { _text = GetComponent<Text>(); }
    void OnEnable()
    {
        Refresh();
        LocalizationManager.OnLanguageChanged += Refresh;
    }
    void OnDisable()
    {
        LocalizationManager.OnLanguageChanged -= Refresh;
    }

    public void Refresh()
    {
        if (_text) _text.text = LocalizationManager.Get(key);
    }
}
