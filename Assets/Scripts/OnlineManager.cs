//using Unity.Netcode;
using UnityEngine;

namespace CloBeats
{
    public class OnlineManager : MonoBehaviour
    {
        //private NetworkManager m_NetworkManager;
        void Awake()
        {
            //m_NetworkManager = GetComponent<NetworkManager>();
        }

        void OnGUI()
        {
            
        }
        public void GenerateGUI()
        {
            /*GUILayout.BeginArea(new Rect(10, 10, 300, 300));
            if (!m_NetworkManager.IsClient && !m_NetworkManager.IsServer)
            {
                StartButtons();
            }
            else
            {
                StatusLabels();

                SubmitNewUsername();
            }

            GUILayout.EndArea();*/
        }

        void StartButtons()
        {
            /*if (GUILayout.Button("Host")) m_NetworkManager.StartHost();
            if (GUILayout.Button("Client")) m_NetworkManager.StartClient();
            if (GUILayout.Button("Server")) m_NetworkManager.StartServer();*/
        }

        void StatusLabels()
        {
            /*var mode = m_NetworkManager.IsHost ?
                "Host" : m_NetworkManager.IsServer ? "Server" : "Client";

            GUILayout.Label("Transport: " +
                m_NetworkManager.NetworkConfig.NetworkTransport.GetType().Name);
            GUILayout.Label("Mode: " + mode);*/
        }

        void SubmitNewUsername()
        {
            /*if (GUILayout.Button(m_NetworkManager.IsServer ? "Move" : "Request Username Change"))
            {
                if (m_NetworkManager.IsServer && !m_NetworkManager.IsClient )
                {
                    foreach (ulong uid in m_NetworkManager.ConnectedClientsIds)
                        m_NetworkManager.SpawnManager.GetPlayerNetworkObject(uid).GetComponent<GuitarPlayer>().SetUsername(PlayerPrefs.GetString("Username", "Guest"));
                }
                else
                {
                    var playerObject = m_NetworkManager.SpawnManager.GetLocalPlayerObject();
                    var player = playerObject.GetComponent<GuitarPlayer>();
                    player.SetUsername(PlayerPrefs.GetString("Username", "Guest"));
                }
            }*/
        }
    }

}
