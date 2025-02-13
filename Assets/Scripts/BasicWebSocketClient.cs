using UnityEngine;
using WebSocketSharp;
using TMPro;
using System;
using System.Collections.Generic;
using UnityEngine.UI;
using System.Threading;

public class BasicWebSocketClient : MonoBehaviour
{
    // Instancia del cliente WebSocket
    private WebSocket ws;

    public TMP_Text chatDisplay;
    public TMP_InputField inputField;
    private Queue<Action> _actionsToRun;

    private AudioSource audioSource;
    public AudioClip msgNotification;
    public AudioClip popNotification;

    public TMP_Text muteTextButton;

    public ScrollRect scrollRect;

    // Se ejecuta al iniciar la escena
    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        _actionsToRun = new Queue<Action>();

        // Crear una instancia del WebSocket apuntando a la URI del servidor
        ws = new WebSocket("ws://127.0.0.1:7777/");

        // Evento OnOpen: se invoca cuando se establece la conexión con el servidor
        ws.OnOpen += (sender, e) =>
        {
            Debug.Log("WebSocket conectado correctamente.");
        };

        // Evento OnMessage: se invoca cuando se recibe un mensaje del servidor
        ws.OnMessage += (sender, e) =>
        {
            EnqueueUIAction(() =>
            {
                chatDisplay.text += e.Data + "\n";
            });

            Debug.Log("Mensaje recibido: " + e.Data);
        };

        // Evento OnError: se invoca cuando ocurre un error en la conexión
        ws.OnError += (sender, e) =>
        {
            Debug.LogError("Error en el WebSocket: " + e.Message);
        };

        // Evento OnClose: se invoca cuando se cierra la conexión con el servidor
        ws.OnClose += (sender, e) =>
        {
            Debug.Log("WebSocket cerrado. Código: " + e.Code + ", Razón: " + e.Reason);
        };

        // Conectar de forma asíncrona al servidor WebSocket
        ws.ConnectAsync();
    }

    void Update()
    {
        if (ws == null || ws.ReadyState != WebSocketState.Open)
        {
            // Si el WebSocket no está conectado, no hacer nada
            return;
        }

        if (Input.GetKeyDown(KeyCode.Return))
        {
            SendMessageToServer(inputField.text);
            audioSource.PlayOneShot(popNotification);

            // Limpiamos el input field y le devolvemos el foco, para no tener que seleccionarlo cada vez
            inputField.text = "";
            inputField.ActivateInputField();
        }

        if (_actionsToRun.Count > 0)
        {
            Action action;

            lock (_actionsToRun)
            {
                action = _actionsToRun.Dequeue();
            }

            action?.Invoke();
            audioSource.PlayOneShot(msgNotification);

            // Forzar actualización del Layout para el Scroll
            LayoutRebuilder.ForceRebuildLayoutImmediate(chatDisplay.rectTransform);

            // Hacer que el Scroll se desplace hasta el final
            ScrollToBottom();
        }
    }

    public void OnSendButtonClick()
    {
        SendMessageToServer(inputField.text);

        // Limpiamos el input field y le devolvemos el foco, para no tener que seleccionarlo cada vez
        inputField.text = "";
        inputField.ActivateInputField();
    }

    public void OnMuteButtonClick()
    {
        if (audioSource.mute)
        {
            audioSource.mute = false;
            muteTextButton.text = "Mute";
        }
        else
        {
            audioSource.mute = true;
            muteTextButton.text = "Unmute";
        }
    }

    // Método para enviar un mensaje al servidor (puedes llamarlo, por ejemplo, desde un botón en la UI)
    public void SendMessageToServer(string message)
    {
        print("entro");
        if (ws != null && ws.ReadyState == WebSocketState.Open)
        {
            if (string.IsNullOrEmpty(message))
            {
                Debug.LogError("No se puede enviar mensajes vacíos.");
                return;
            }

            ws.Send(message);
        }
        else
        {
            Debug.LogError("No se puede enviar el mensaje. La conexión no está abierta.");
        }
    }

    private void EnqueueUIAction(Action action)
    {
        lock (_actionsToRun)
        {
            _actionsToRun.Enqueue(action);
        }
    }

    void ScrollToBottom()
    {
        Canvas.ForceUpdateCanvases();
        scrollRect.verticalNormalizedPosition = 0f;
    }


    // Se ejecuta cuando el objeto se destruye (por ejemplo, al cambiar de escena o cerrar la aplicación)
    void OnDestroy()
    {
        if (ws != null)
        {
            ws.Close();
            ws = null;
        }
    }

}
