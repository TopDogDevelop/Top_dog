using UnityEngine;
using UnityEngine.UIElements;

namespace TopDog.Client;

/// <summary>Boot scene entry; loads main menu UIDocument.</summary>
public sealed class BootLoader : MonoBehaviour
{
    [SerializeField] private UIDocument? uiDocument;
    [SerializeField] private VisualTreeAsset? mainMenuUxml;

    private void Awake()
    {
        if (uiDocument == null)
        {
            uiDocument = GetComponent<UIDocument>();
        }
        if (uiDocument != null && mainMenuUxml != null)
        {
            uiDocument.visualTreeAsset = mainMenuUxml;
        }
    }
}
