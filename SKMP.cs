using BepInEx;
using Netcode.Transports.Facepunch;
using Steamworks;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

namespace SKMP
{
    [BepInPlugin("sk-multiplayer-ncfgo", "SKMP", "0.0.1")]
    public class Plugin : BaseUnityPlugin
    {
        private GameObject _prefabClone;
        internal static FacepunchTransport Transport;
        private static GameObject _netMan;
        private NetworkManager _netManComp;
        internal NetworkObject PlayerNetObj;
        internal NetworkTransform PlayerNetTrans;

        private void Awake()
        {
            // Plugin startup logic
            Logger.LogInfo($"Plugin Shady Knight Multiplayer is loaded!");
            _netMan = new GameObject("netMan");
            _netManComp = _netMan.AddComponent<NetworkManager>();
            Transport = _netMan.AddComponent<FacepunchTransport>();
            _netManComp.NetworkConfig = new NetworkConfig
            {
                NetworkTransport = Transport,
                ConnectionApproval = true
            };
            DontDestroyOnLoad(_netMan);
            SteamClient.Init(1155650);
            _netManComp.OnClientConnectedCallback += OnClientConnected;
            _netManComp.ConnectionApprovalCallback = static (_, response) =>
            {
                response.Approved = true;
                response.CreatePlayerObject = true;
                response.Pending = false;
            };
        }

        public void Start()
        {
            _prefabClone = Instantiate(GameObject.Find("Player(Clone)"));
            Destroy(_prefabClone.GetComponent<Camera>()); // We don't want the camera; it'll do some fucky shit if we keep it.
            _prefabClone.name = "PlayerPrefab";
            _prefabClone.SetActive(false);
            PlayerNetObj = _prefabClone.AddComponent<NetworkObject>();
            PlayerNetTrans = _prefabClone.AddComponent<NetworkTransform>();
            NetworkTransform nt = _prefabClone.GetComponent<NetworkTransform>();

            // I don't know why it's like this. Just... please.
            nt.SyncPositionX = true;
            nt.SyncPositionY = true;
            nt.SyncPositionZ = true;
            nt.SyncRotAngleX = true;
            nt.SyncRotAngleY = true;
            nt.SyncRotAngleZ = true;
            nt.SyncScaleX = false;
            nt.SyncScaleY = false;
            nt.SyncScaleZ = false;
            nt.Interpolate = true;

            _netManComp.NetworkConfig.PlayerPrefab = _prefabClone;

            DontDestroyOnLoad(_prefabClone);
        }

        private void OnClientConnected(ulong clientId)
        {
            Debug.Log($"Client connected: {clientId}");
            if (NetworkManager.Singleton.IsHost && clientId != 0)
            {
                GameObject prefab = NetworkManager.Singleton.NetworkConfig.PlayerPrefab;
                if (prefab != null)
                {
                    GameObject instance = Instantiate(prefab);
                    instance.SetActive(true);
                    instance.GetComponent<NetworkObject>().SpawnAsPlayerObject(clientId);
                    Debug.Log($"Spawned remote player for client {clientId}");
                }
            }
        }
    }

    public static class Commands
    {
        [BetterConsole.Command]
        public static void Host()
        {
            _ = NetworkManager.Singleton.StartHost();
            Game.message.Show("Hosting");
        }

        [BetterConsole.Command]
        public static void Join(ulong id)
        {
            Plugin.Transport.targetSteamId = id;
            _ = NetworkManager.Singleton.StartClient();
        }
    }
}
