using System.Collections.Generic;
using UnityEngine;

namespace MultiplayerARPG
{
    public partial class GameInstance : MonoBehaviour
    {
        public Dictionary<string, LootBagEntity> LootBagEntities;
        public LootBagEntity targetLootBagEntity;

        [DevExtMethods("LoadedGameData")]
        public void GameDataLoaded_DevExt()
        {
            foreach (KeyValuePair<int, PlayerCharacter> pc in PlayerCharacters)
            {
                pc.Value.ResetCaches();
            }
        }
    }
}