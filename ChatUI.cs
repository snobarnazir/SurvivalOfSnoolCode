using UnityEngine;
using TMPro;
using Unity.Netcode;
using System.Collections.Generic;

public class ChatUI : MonoBehaviour
{
    public TMP_InputField inputField;
    public TextMeshProUGUI chatHistoryText;

    [Tooltip("Maximum number of chat lines to keep visible.")]
    public int maxMessages = 8;

    public static bool IsChatOpen = false;
    public static bool MultiplayerMode = false;

    private CanvasGroup canvasGroup;
    private bool isOpen = false;
    private PlayerChat localPlayerChat;
    private List<string> chatMessages = new List<string>();

    void Start()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        HideChat();

        if (!MultiplayerMode)
        {
            enabled = false;
            return;
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.C))
        {
            if (!isOpen)
            {
                OpenChat();
            }
            else
            {
                if (string.IsNullOrEmpty(inputField.text))
                {
                    CloseChat();
                }
            }
        }

        if (Input.GetKeyDown(KeyCode.Return) && isOpen)
        {
            SendMessage();
        }
    }

    void OpenChat()
    {
        isOpen = true;
        IsChatOpen = true;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        canvasGroup.alpha = 1f;
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;

        inputField.text = "";
        inputField.Select();
        inputField.ActivateInputField();
        StartCoroutine(FocusInputNextFrame());
    }

    private System.Collections.IEnumerator FocusInputNextFrame()
    {
        yield return null;
        inputField.Select();
        inputField.ActivateInputField();
    }

    void CloseChat()
    {
        isOpen = false;
        IsChatOpen = false;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        HideChat();
        inputField.DeactivateInputField();
    }

    void HideChat()
    {
        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
    }

    void SendMessage()
    {
        string message = inputField.text.Trim();
        if (string.IsNullOrEmpty(message)) return;

        if (localPlayerChat == null)
        {
            localPlayerChat = GetComponentInParent<PlayerChat>();
        }

        if (localPlayerChat != null)
        {
            localPlayerChat.SendChatMessage(message);
        }

        inputField.text = "";
        inputField.ActivateInputField();
    }

    // Called by PlayerChat when a message arrives from the network
    public void AddMessage(ulong senderId, string message)
    {
        string name = PlayerChat.GetName(senderId);
        Color color = PlayerChat.GetColor(senderId);
        string hex = ColorUtility.ToHtmlStringRGB(color);
        string line = $"<color=#{hex}>{name}</color>: {message}";

        chatMessages.Add(line);
        RebuildAndTrim();
    }

    private void RebuildAndTrim()
    {
        // Safety cap so the list never grows forever in memory
        while (chatMessages.Count > 50)
        {
            chatMessages.RemoveAt(0);
        }

        chatHistoryText.text = string.Join("\n", chatMessages);
        chatHistoryText.ForceMeshUpdate();

        float boxHeight = chatHistoryText.rectTransform.rect.height;

        // Remove oldest messages until the text fits inside the box height
        while (chatHistoryText.preferredHeight > boxHeight &&
               chatMessages.Count > 0)
        {
            chatMessages.RemoveAt(0);
            chatHistoryText.text = string.Join("\n", chatMessages);
            chatHistoryText.ForceMeshUpdate();
        }
    }
}