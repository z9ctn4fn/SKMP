using BepInEx;
using Netcode.Transports.Facepunch;
using Steamworks;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

namespace SKMP
{
    [BepInDependency("tipe.shadyknight.betterconsole", BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency("tipe.shadyknight.betterconsolelive", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInPlugin("sk-multiplayer-ncfgo", "SKMP", "0.0.1")]
    public class Plugin : BaseUnityPlugin
    {
        private GameObject _prefabClone;
        internal static FacepunchTransport Transport;
        private static GameObject _networkManagerObject;
        private NetworkManager _networkManager;
        internal NetworkObject PlayerNetworkObject;
        internal NetworkTransform PlayerNetworkTransform;

        private void Awake()
        {
            // Plugin startup logic
            Logger.LogInfo($"Plugin Shady Knight Multiplayer is loaded!");

            _networkManagerObject = new GameObject("SKMP Network Mananager Object");
            DontDestroyOnLoad(_networkManagerObject);

            _networkManager = _networkManagerObject.AddComponent<NetworkManager>();
            _networkManager.OnClientConnectedCallback += OnClientConnected;
            _networkManager.ConnectionApprovalCallback = static (_, response) =>
            {
                response.Approved = true;
                response.CreatePlayerObject = true;
                response.Pending = false;
            };

            Transport = _networkManagerObject.AddComponent<FacepunchTransport>();

            _networkManager.NetworkConfig = new NetworkConfig
            {
                NetworkTransport = Transport,
                ConnectionApproval = true
            };

            SteamClient.Init(1155650);
        }

        public void Start()
        {
            GameObject _playerPrefab = Game.player.gameObject;
            _prefabClone = Instantiate(_playerPrefab);

            // destorying all cameras so that we can use the original prefab as a network object
            foreach (Camera camera in _prefabClone.GetComponentsInChildren<Camera>(true))
            {
                Destroy(camera);
            }

            _prefabClone.name = "PlayerPrefab";
            _prefabClone.SetActive(false);

            PlayerNetworkObject = _prefabClone.AddComponent<NetworkObject>();
            PlayerNetworkTransform = _prefabClone.AddComponent<NetworkTransform>();
            NetworkTransform nt = _prefabClone.GetComponent<NetworkTransform>();

            // configuring NetworkTransform so it works
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

            // setting the prefab to use for othe players as otu modified prefab
            _networkManager.NetworkConfig.PlayerPrefab = _prefabClone;

            // making sure it persist loads so it doesnt get lost
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
