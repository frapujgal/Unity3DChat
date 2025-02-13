using UnityEngine;
using WebSocketSharp;
using WebSocketSharp.Server;
using TMPro;
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;

// Clase que se adjunta a un GameObject en Unity para iniciar el servidor WebSocket.
public class BasicWebSocketServer : MonoBehaviour
{
    // Instancia del servidor WebSocket.
    private static WebSocketServer wss;
    public TMP_Text chatDisplay;

    // Se ejecuta al iniciar la escena.
    void Start()
    {
        try
        {
            // Crear un servidor WebSocket que escucha en el puerto 7777.
            wss = new WebSocketServer(7777);

            // Añadir un servicio en la ruta "/" que utiliza el comportamiento EchoBehavior.
            wss.AddWebSocketService<ChatBehavior>("/");

            // Iniciar el servidor.
            wss.Start();
            Debug.Log("Servidor WebSocket iniciado en ws://127.0.0.1:7777/");
        }
        catch (Exception ex) 
        {
            gameObject.SetActive(false);
            Debug.LogWarning("El servidor ya está en funcionamiento.");
        }
    }

    // Se ejecuta cuando el objeto se destruye (por ejemplo, al cerrar la aplicación o cambiar de escena).
    void OnDestroy()
    {
        // Si el servidor está activo, se detiene de forma limpia.
        if (wss != null)
        {
            string chatHistory = ChatBehavior.chatHistory;
            saveChatHistory(chatHistory);

            wss.Stop();
            wss = null;
            Debug.Log("Servidor WebSocket detenido.");
        }
    }

    private void saveChatHistory(string history)
    {
        DateTime now = DateTime.Now;
        string dateString = now.ToString("yyyy-MM-dd");
        string timeString = now.ToString("HH-mm-ss");

        string fileName = $"Conversación {dateString}_{timeString}.txt";
        string filePath = Path.Combine(Application.dataPath, "../Historial", fileName);

        try
        {
            File.WriteAllText(filePath, history);
            Debug.Log($"Conversación guardada en: {filePath}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error al guardar la conversación: {ex.Message}");
        }
    }
}


// Sacar a otra clase llamada ChatBehavior ?
// Comportamiento básico del servicio WebSocket: simplemente devuelve el mensaje recibido.
public class ChatBehavior : WebSocketBehavior
{
    private static Dictionary<string, string> clients = new Dictionary<string, string>();
    private static List<string> colors = new List<string> { "#FF5733", "#33FF57", "#3357FF", "#F5B041", "#9B59B6" };
    private static int clientCounter = 1;

    private string clientID;
    private string clientColor;

    public static string chatHistory = "";

    protected override void OnOpen()
    {
        clientID = "Cliente" + clientCounter++;
        clientColor = colors[clients.Count % colors.Count];
        clients[ID] = clientColor;

        chatHistory += $">>> {clientID} se ha conectado al chat.\n";
        Sessions.Broadcast($"<color={clientColor}>{clientID} se ha conectado al chat.</color>");
    }

    // Se invoca cuando se recibe un mensaje desde un cliente.
    protected override void OnMessage(MessageEventArgs e)
    {
        // Envía de vuelta el mismo mensaje recibido.
        chatHistory += $"<{clientID}> {e.Data}\n";
        Sessions.Broadcast($"<color={clientColor}><{clientID}></color> {e.Data}");
    }

    protected override void OnClose(CloseEventArgs e)
    {
        clients.Remove(ID);
        chatHistory += $"<<< {clientID} se ha desconectado del chat.\n";
        Sessions.Broadcast($"<color={clientColor}>{clientID} se ha desconectado del chat.</color>");
    }
}
