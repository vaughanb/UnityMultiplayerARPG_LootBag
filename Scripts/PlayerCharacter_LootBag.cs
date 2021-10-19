using System.Collections;
using System.Collections.Generic;
using System.Linq;
using LiteNetLibManager;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace MultiplayerARPG
{
    public partial class PlayerCharacter : BaseCharacter
    {
        [Category("Loot Bag Settings")]
        public bool dropAllPlayerItems = false;
    }
}