using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;
#if MIRAGE
using Mirage;
#else
using Mirror;
using System;
#endif

namespace Object_Pooler
{
    public class ObjectPoolingManager : MonoBehaviour
    {
        #region Fields

#if MIRAGE
        private ServerObjectManager _serverObjectManager;
        private readonly Dictionary<int?, NetworkIdentity> _objectsAssetIds = new Dictionary<int?, NetworkIdentity>();
#else
        private readonly Dictionary<Guid, NetworkIdentity> _objectsAssetIds = new Dictionary<Guid, NetworkIdentity>();
#endif
        private readonly Dictionary<int, object> _recycledObjects = new Dictionary<int, object>();
        private GameObject _parent;


        #endregion

        #region Unity Methods

        private void Start()
        {
            _parent = new GameObject { name = "Pooled Objects" };
            DontDestroyOnLoad(_parent);
#if MIRAGE
            _serverObjectManager = FindObjectOfType<ServerObjectManager>();

            ClientObjectManager clientObject = FindObjectOfType<ClientObjectManager>();

            if (clientObject == null) return;

            foreach (NetworkIdentity g in clientObject.spawnPrefabs)
            {
                clientObject.RegisterSpawnHandler(g.PrefabHash, SpawnObject, UnSpawnObject);

                _objectsAssetIds.Add(g.PrefabHash, g);
            }
#else
            foreach (KeyValuePair<Guid, GameObject> g in NetworkClient.prefabs)
            {
                NetworkClient.RegisterSpawnHandler(g.Key, SpawnObject, UnSpawnObject);

                _objectsAssetIds.Add(g.Key, g.Value.GetComponent<NetworkIdentity>());
            }
#endif
        }

        #endregion

        #region Custom Spawn Handlers

#if MIRAGE
        /// <summary>
        ///     Spawn handler for all network spawning of things.
        /// </summary>
        /// <param name="msg">The message incoming from spawn handler.</param>
        /// <returns>Return back the network identity of the object needing soawning.</returns>
        internal NetworkIdentity SpawnObject(SpawnMessage msg)
#else
        /// <summary>
        ///     Spawn handler for all network spawning of things.
        /// </summary>
        /// <param name="position">The position which to spawn the object.</param>
        /// <param name="assetId">the guid id of the asset prefab we want to spawn from</param>
        /// <returns></returns>
        internal GameObject SpawnObject(Vector3 position, Guid assetId)
#endif
        {
#if MIRAGE
            NetworkIdentity spawnedObject = NetworkSpawnPool(msg.position, Quaternion.identity, msg.prefabHash, 1);
#else
            NetworkIdentity spawnedObject = NetworkSpawnPool(position, Quaternion.identity, assetId, 1);
#endif

            // Active the pooled object.
            spawnedObject.gameObject.SetActive(true);

            // return the game object back to mirage to have it finish spawning.
#if MIRAGE
            return spawnedObject;
#else
            return spawnedObject.gameObject;
#endif
        }

        /// <summary>
        ///     Spawn handler for all network unspawning of things.
        /// </summary>
        /// <param name="spawned">What network identity we are going to unspawn and return back to network pool.</param>
#if MIRAGE
        internal void UnSpawnObject(NetworkIdentity spawned)
#else
        internal void UnSpawnObject(GameObject spawned)
#endif
        {
            ObjectPooling<NetworkIdentity> p = default;

            foreach (object pool in _recycledObjects.Values)
            {
                if (!(pool is ObjectPooling<NetworkIdentity> obj) || !obj.PooledObjectIDs.Contains(spawned.gameObject.GetInstanceID())) continue;

                p = obj;

                break;
            }
#if MIRAGE

            p.Despawn(spawned);
#else
            p.Despawn(spawned.GetComponent<NetworkIdentity>());
#endif
        }

        /// <summary>
        ///     Unspawn of local gameobjects
        /// </summary>
        /// <param name="spawned">What object we are going to unspawn and return back to local pool.</param>
        private void UnSpawnObject<T>(T spawned) where T : Object
        {
            ObjectPooling<T> p = default;

            foreach (object pool in _recycledObjects.Values)
            {
                if (!(pool is ObjectPooling<T> obj) || !obj.PooledObjectIDs.Contains(spawned.GetInstanceID())) continue;

                p = obj;

                break;
            }

            p.Despawn(spawned);
        }

#endregion

#region Network Spawning

        /// <summary>
        ///     Spawn an object using our pooled objects for the specific object
        ///     or create a new pool if no pool has been created yet.
        /// </summary>
        /// <param name="position">The position we want to spawn the object at.</param>
        /// <param name="rotation"></param>
        /// <param name="AssetId">The asset id of the object from <see cref="NetworkIdentity" /> to use to spawn object of.</param>
        /// <param name="quantity">How many pooled objects we want to create of this object. If we don't have a pool started.</param>
        /// <returns></returns>
#if MIRAGE
        public NetworkIdentity NetworkSpawnPool(Vector3 position, Quaternion rotation, int? AssetId, int quantity = 3)
#else
        public NetworkIdentity NetworkSpawnPool(Vector3 position, Quaternion rotation, Guid AssetId, int quantity = 3)
#endif
        {
            int prefabId = _objectsAssetIds[AssetId].gameObject.GetInstanceID();

            if (_recycledObjects.ContainsKey(prefabId))
                return (_recycledObjects[prefabId] is ObjectPooling<NetworkIdentity>
                    ? (ObjectPooling<NetworkIdentity>)_recycledObjects[prefabId]
                    : default).NetworkSpawn(position, rotation);

            var createParent = new GameObject(_objectsAssetIds[AssetId].name) { transform = { parent = _parent.transform } };

            _recycledObjects[prefabId] = new ObjectPooling<NetworkIdentity>(_objectsAssetIds[AssetId], quantity, createParent);

            return (_recycledObjects[prefabId] is ObjectPooling<NetworkIdentity>
                ? (ObjectPooling<NetworkIdentity>)_recycledObjects[prefabId]
                : default).NetworkSpawn(position, rotation);
        }

        /// <summary>
        ///     Network object pooler unspawning of object and resetting parent back
        ///     to the object pooler.
        /// </summary>
        /// <param name="objectSpawned">The object we want to set in active and despawn.</param>
        /// <param name="isServer">Whether or not this is server telling us to unspawn object.</param>
        public void NetworkUnSpawnObject(NetworkIdentity objectSpawned, bool isServer)
        {
            UnSpawnObject(objectSpawned);

#if MIRAGE
            // If this is network server then tell rest of clients to unspawn object.
            if (_serverObjectManager == null || !isServer) return;

            // Unspawn object.
            _serverObjectManager.Destroy(objectSpawned.gameObject, false);
#else
            // Unspawn object.
            NetworkServer.Destroy(objectSpawned.gameObject);

#endif
        }

        #endregion

        #region Local Spawning.

        /// <summary>
        ///     Spawn an object using our pooled objects for the specific object
        ///     or create a new pool if no pool has been created not networked objects.
        /// </summary>
        /// <param name=""></param>
        /// <param name="gObject">The object we want to spawn.</param>
        /// <param name="position">The position we want to spawn the object at.</param>
        /// <param name="rotation"></param>
        /// <param name="worldStayPosition">Unity specific <see cref="Instantiate"/></param>
        /// <param name="quantity">How many pooled objects we want to create of this object. If we don't have a pool started.</param>
        /// <returns></returns>
        public T LocalSpawnPool<T>(T gObject, Vector3 position, Quaternion rotation, bool worldStayPosition = false, int quantity = 3) where T : Object
        {
            int prefabId = gObject.GetInstanceID();

            if (_recycledObjects.ContainsKey(prefabId))
                return (T)(_recycledObjects[prefabId] is ObjectPooling<T>
                    ? (ObjectPooling<T>)_recycledObjects[prefabId]
                    : default).Spawn(position, rotation);

            var createParent = new GameObject(gObject.name) { transform = { parent = _parent.transform } };

            _recycledObjects[prefabId] = new ObjectPooling<T>(gObject, quantity, createParent);

            return (T)(_recycledObjects[prefabId] is ObjectPooling<T>
                ? (ObjectPooling<T>)_recycledObjects[prefabId]
                : default).Spawn(position, rotation);
        }

        /// <summary>
        ///     Spawn an object using our pooled objects for the specific object
        ///     or create a new pool if no pool has been created not networked objects.
        /// </summary>
        /// <param name="gObject">The object we want to spawn.</param>
        /// <param name="parentTransform">The transform we want to parent the object to.</param>
        /// <param name="worldStayPosition">Unity specific <see cref="UnityEngine.Object.Instantiate{T}(T,UnityEngine.Transform,bool)"/></param>
        /// <param name="quantity">How many pooled objects we want to create of this object. If we don't have a pool started.</param>
        /// <returns></returns>
        internal T LocalSpawnPool<T>(T gObject, Transform parentTransform, bool worldStayPosition = false, int quantity = 3) where T : Object
        {
            int prefabId = gObject.GetInstanceID();

            if (_recycledObjects.ContainsKey(prefabId))
                return (_recycledObjects[prefabId] is ObjectPooling<T>
                    ? (ObjectPooling<T>)_recycledObjects[prefabId]
                    : default).Spawn(parentTransform, worldStayPosition);

            var createParent = new GameObject(gObject.name) { transform = { parent = _parent.transform } };

            _recycledObjects[prefabId] = new ObjectPooling<T>(gObject, quantity, createParent);

            return (_recycledObjects[prefabId] is ObjectPooling<T>
                ? (ObjectPooling<T>)_recycledObjects[prefabId]
                : default).Spawn(parentTransform, worldStayPosition);
        }

        /// <summary>
        ///     Local object pooler un-spawning of object.
        /// </summary>
        /// <param name="objectSpawned">The object we want to set in active and despawn.</param>
        internal void LocalUnSpawnObject<T>(T objectSpawned) where T : Object
        {
            UnSpawnObject(objectSpawned);
        }

#endregion
    }
}
