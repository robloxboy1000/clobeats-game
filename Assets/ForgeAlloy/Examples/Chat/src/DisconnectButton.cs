using Forge.Networking.Unity;
using UnityEngine;

public class DisconnectButton : MonoBehaviour
{
	private IEngineFacade _engine = null;

	private void Awake()
	{
		_engine = GameObject.FindFirstObjectByType<ForgeEngineFacade>();
	}

	public void Disconnect()
	{
		_engine.ShutDown();
	}
}
