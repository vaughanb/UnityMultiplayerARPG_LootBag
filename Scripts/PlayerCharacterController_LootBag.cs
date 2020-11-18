using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MultiplayerARPG
{
    public partial class PlayerCharacterController : BasePlayerCharacterController
    {
        protected BaseCharacterEntity targetCharacterEntity;

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
