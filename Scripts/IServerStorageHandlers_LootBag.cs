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
        /// <param name="storageId"></param>
        /// <param name="addingItem"></param>
        UniTask<bool> AddLootBagItems(StorageId storageId, CharacterItem addingItem);
    }
}