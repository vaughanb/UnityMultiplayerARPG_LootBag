using System.Collections.Generic;
using UnityEngine;
using LiteNetLibManager;

namespace MultiplayerARPG
{
    public abstract partial class BaseGameNetworkManager : LiteNetLibGameManager
    {
        [DevExtMethods("RegisterMessages")]
        public void RegisterLootBagMessages()
        {
            RegisterRequestToServer<RequestMoveAllItemsFromStorageMessage, ResponseMoveAllItemsFromStorageMessage>(GameNetworkingConsts.MoveAllItemsFromStorage, ServerStorageMessageHandlers.HandleRequestMoveAllItemsFromStorage);
        }

        /// <summary>
        /// Registers all lootbag prefabs in resources so that they can be spawned.
        /// </summary>
        [DevExtMethods("InitPrefabs")]
        public void InitLootBag()
        {
            Object[] lootBagObjects = Resources.LoadAll("LootBags");
            GameInstance.Singleton.LootBagEntities = new Dictionary<string, LootBagEntity>();

            HashSet<LiteNetLibIdentity> spawnablePrefabs = new HashSet<LiteNetLibIdentity>(Assets.spawnablePrefabs);
            foreach (Object lootBagObject in lootBagObjects)
            {
                GameObject lootBagGameObject = lootBagObject as GameObject;
                LootBagEntity lootBagEntity = lootBagGameObject.GetComponent<LootBagEntity>();
                if (lootBagEntity == null)
                    continue;

                spawnablePrefabs.Add(lootBagEntity.Identity);
                GameInstance.AddBuildingEntities(lootBagEntity);

                GameInstance.Singleton.LootBagEntities.Add(lootBagObject.name, lootBagEntity);
            }

            Assets.spawnablePrefabs = new LiteNetLibIdentity[spawnablePrefabs.Count];
            spawnablePrefabs.CopyTo(Assets.spawnablePrefabs);
        }
    }
}