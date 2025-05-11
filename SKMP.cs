using Steamworks;
using Unity.Netcode.Components;

namespace SKMP;
using BepInEx;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Netcode;
using Netcode.Transports.Facepunch;

// This code looks like it was a fungus.

public class SKMP
{
    [BepInPlugin("sk-multiplayer-ncfgo", "SKMP", "Alpha 1")]
    public class Plugin : BaseUnityPlugin
    {
        public GameObject prefabClone;
        public static FacepunchTransport transport;
        public static GameObject netMan;
        public NetworkManager netManComp;
        public NetworkObject playerNetObj;
        public NetworkTransform playerNetTrans;
        private void Awake()
        {
            // Plugin startup logic
            Logger.LogInfo($"Plugin Shady Knight Multiplayer is loaded!");
            SceneManager.sceneLoaded += OnSceneLoaded;
            netMan = new GameObject("netMan");
            netManComp = netMan.AddComponent<NetworkManager>();
            transport = netMan.AddComponent<FacepunchTransport>();
            netManComp.NetworkConfig = new NetworkConfig();
            netManComp.NetworkConfig.NetworkTransport = transport;
            netManComp.NetworkConfig.ConnectionApproval = true;
            DontDestroyOnLoad(netMan);
            SteamClient.Init(1155650, true);
            netManComp.OnClientConnectedCallback += OnClientConnected;
            netManComp.ConnectionApprovalCallback = (request, response) =>
            {
                response.Approved = true;
                response.CreatePlayerObject = true;
                response.Pending = false;
            };
        }
        //create commands
        [BetterConsole.Command]
        public static void host()
        {
            NetworkManager.Singleton.StartHost();
            Game.message.Show("Hosting");
        }

        [BetterConsole.Command]
        public static void join(int intID)
        {
            var ulongID = ulong.Parse(intID.ToString());
            transport.targetSteamId = ulongID;
            NetworkManager.Singleton.StartClient();
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (prefabClone == null)
            {
                prefabClone = GameObject.Instantiate(GameObject.Find("Player(Clone)"));
                Camera.Destroy(prefabClone.GetComponent<Camera>()); // We don't want the camera; it'll do some fucky shit if we keep it.
                prefabClone.name = "PlayerPrefab";
                prefabClone.SetActive(false);
                playerNetObj = prefabClone.AddComponent<NetworkObject>();
                playerNetTrans = prefabClone.AddComponent<NetworkTransform>();
                var nt = prefabClone.GetComponent<NetworkTransform>();
                nt.SyncPositionX = true; // I don't know why it's like this. Just... please.
                nt.SyncPositionY = true;
                nt.SyncPositionZ = true;
                nt.SyncRotAngleX = true;
                nt.SyncRotAngleY = true;
                nt.SyncRotAngleZ = true;
                nt.SyncScaleX = false;
                nt.SyncScaleY = false;
                nt.SyncScaleZ = false;
                nt.Interpolate = true;
                netManComp.NetworkConfig.PlayerPrefab = prefabClone;
            }
        }

        private void OnClientConnected(ulong clientId)
        {
            Debug.Log($"Client connected: {clientId}");
            if (NetworkManager.Singleton.IsHost && clientId != 0)
            {
                var prefab = NetworkManager.Singleton.NetworkConfig.PlayerPrefab;
                if (prefab != null)
                {
                    var instance = Instantiate(prefab);
                    instance.SetActive(true);
                    instance.GetComponent<NetworkObject>().SpawnAsPlayerObject(clientId);
                    Debug.Log($"Spawned remote player for client {clientId}");
                }
            }    
        }
    }
}