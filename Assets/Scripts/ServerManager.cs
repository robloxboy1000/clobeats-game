using UnityEngine;
using UnityEngine.SceneManagement;

public class ServerManager : MonoBehaviour
{
    public GameObject mainPanel;
    public bool isServer = true;

    // used by server
    public float playersConnected = 0f;
    public string ipToListen = "0.0.0.0";
    public float portToListen = 7777f;

    // used by client (if isServer = false)
    public string ipToConnect = "0.0.0.0";
    public float portToConnect = 7777f;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        DontDestroyOnLoad(gameObject);
        SceneManager.activeSceneChanged += ActiveSceneChanged;
    }

    void ActiveSceneChanged(Scene prevScene, Scene newScene)
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
