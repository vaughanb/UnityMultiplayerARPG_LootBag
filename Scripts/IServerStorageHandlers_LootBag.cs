using Cysharp.Threading.Tasks;
using System.Collections.Generic;

namespace MultiplayerARPG
{
    /// <summary>
    /// These properties and functions will be called at server only
    /// </summary>
    public partial interface IServerStorageHandlers
    {
        /// <summary>
        /// Adds items to loot bag storage
        /// </summary>
        /// <param name="storageId">ID of loot bag storage</param>
        /// <param name="lootItems">items to add to loot bag</param>
        UniTask<bool> AddLootBagItems(StorageId storageId, List<CharacterItem> lootItems);
    }
}