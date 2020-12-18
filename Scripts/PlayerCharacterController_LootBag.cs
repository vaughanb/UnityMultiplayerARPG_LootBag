using UnityEngine;

namespace MultiplayerARPG
{
    public partial class PlayerCharacterController : BasePlayerCharacterController
    {
        protected BaseCharacterEntity targetCharacterEntity;

        /// <summary>
        /// Returns true if target entity is a dead lootable character.
        /// </summary>
        /// <param name="character">Entity to check</param>
        /// <returns>true if lootable, false otherwise</returns>
        public bool TryGetLootableCharacter(out BaseCharacterEntity character)
        {
            character = null;

            if (TargetEntity != null && TargetEntity is BaseCharacterEntity)
            {
                character = TargetEntity as BaseCharacterEntity;
                if (character.IsDead() && character.useLootBag && character.LootBag.Count > 0)
                    return true;
                else
                    character = null;
            }
            return false;
        }
    }
}
