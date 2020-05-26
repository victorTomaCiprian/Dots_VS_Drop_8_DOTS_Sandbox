using System;
using Unity.GraphElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Editor.UsingDataModel.NoPresenters
{
    class SimpleGroup : Group
    {
        public SimpleGroup()
        {
            this.AddManipulator(new ContextualMenuManipulator(BuildContextualMenu));
        }

        void AddNoteMenuItems(DropdownMenu menu, string menuText, string category, Action<VisualElement, SpriteAlignment> createMethod)
        {
            Array spriteAlignments = Enum.GetValues(typeof(SpriteAlignment));
            Array.Reverse(spriteAlignments);
            foreach (SpriteAlignment align in spriteAlignments)
            {
                SpriteAlignment alignment = align;
                menu.AppendAction(menuText + "/" + category + "/" + alignment, (a) => createMethod(this, alignment), DropdownMenuAction.AlwaysEnabled);
            }
        }

        public virtual void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            if (evt.target is SimpleGroup)
            {
                AddNoteMenuItems(evt.menu, "Attach Badge", "Comment Badge", NoteManager.CreateCommentNote);
                AddNoteMenuItems(evt.menu, "Attach Badge", "Error Badge", NoteManager.CreateErrorNote);
            }
        }
    }
}
