using System;
using UnityEngine.EventSystems;

namespace MultiplayerARPG
{
    public partial class UILootItemDragHandler : UICharacterItemDragHandler
    {
        public uint sourceObjectId { get; private set; }
        
        [NonSerialized]
        public SourceLocation LootItems = (SourceLocation)69;

        /// <summary>
        /// Returns true if item can be dragged.
        /// True if loot item. Base CanDrag method called otherwise.
        /// </summary>
        public override bool CanDrag
        {
            get
            {
                if (sourceLocation == LootItems)
                    return true;

                return base.CanDrag;
            }
        }

        /// <summary>
        /// Configures the specified item as a loot item.
        /// </summary>
        /// <param name="uiCharacterItem">item to configure</param>
        /// <param name="sourceObjectId">ID of source object</param>
        public void SetupForLootItems(UICharacterItem uiCharacterItem, uint sourceObjectId)
        {
            sourceLocation = LootItems;
            this.sourceObjectId = sourceObjectId;
            this.uiCharacterItem = uiCharacterItem;
        }

        /// <summary>
        /// Event called on end of drag to allow for loot by drag & drop.
        /// </summary>
        /// <param name="eventData"></param>
        public override void OnEndDrag(PointerEventData eventData)
        {
            base.OnEndDrag(eventData);

            if (CanDrag && sourceLocation == LootItems)
                uiCharacterItem.OnClickLootItem();
        }
    }
}
