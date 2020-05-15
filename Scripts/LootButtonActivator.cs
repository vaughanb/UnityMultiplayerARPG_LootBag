using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MultiplayerARPG
{
    public class LootButtonActivator : MonoBehaviour
    {
        public GameObject[] activateObjects;

        private bool canActivate;
        private PlayerCharacterController controller;
        private ShooterPlayerCharacterController shooterController;

        private void LateUpdate()
        {
            canActivate = false;

            controller = BasePlayerCharacterController.Singleton as PlayerCharacterController;
            shooterController = BasePlayerCharacterController.Singleton as ShooterPlayerCharacterController;

            if (controller != null)
            {
                foreach (BaseCharacterEntity character in controller.EnemyEntityDetector.characters)
                {
                    if (character.IsDead() && character.useLootBag && character.LootBag.Count > 0)
                    {
                        canActivate = true;
                        break;
                    }
                }
            }

            if (shooterController != null && shooterController.SelectedEntity != null)
            {
                if (shooterController.SelectedEntity is BaseCharacterEntity)
                {
                    BaseCharacterEntity character = shooterController.SelectedEntity as BaseCharacterEntity;
                    canActivate = character.IsDead() && character.useLootBag && character.LootBag.Count > 0;
                }
            }

            foreach (GameObject obj in activateObjects)
            {
                obj.SetActive(canActivate);
            }
        }
    }
}
