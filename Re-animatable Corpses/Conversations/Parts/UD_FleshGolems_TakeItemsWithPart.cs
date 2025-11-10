using System;
using System.Collections.Generic;
using System.Text;

using XRL;
using XRL.World;
using XRL.World.Parts;

using UD_FleshGolems;
using XRL.UI;
using XRL.Collections;

namespace XRL.World.Conversations.Parts
{
    public class UD_FleshGolems_TakeItemsWithPart : IConversationPart
    {
        public string Parts;

        public bool MatchAll;

        public string Amount;

        public string Message;

        public bool Unsellable;

        public bool ClearQuest;

        public bool Remove;

        public bool AllowTemporary;

        public bool Require;

        public bool FromSpeaker;

        public bool Destroy
        {
            get
            {
                return Remove;
            }
            set
            {
                Remove = value;
            }
        }

        public UD_FleshGolems_TakeItemsWithPart()
        {
            Priority = -1000;
            Parts = null;
            MatchAll = false;
            Amount = "1";
            Message = null;
            Unsellable = true;
            ClearQuest = true;
            Remove = false;
            AllowTemporary = false;
            Require = true;
            FromSpeaker = false;
        }

        public UD_FleshGolems_TakeItemsWithPart(string Parts)
            : this()
        {
            this.Parts = Parts;
        }

        public override bool HandleEvent(EnterElementEvent E)
        {
            if (!Execute())
            {
                try
                {
                    GameObject subject = (FromSpeaker ? The.Speaker : The.Player);
                    List<GameObject> gameObjectList = Event.NewGameObjectList();
                    if (!Parts.IsNullOrEmpty())
                    {
                        gameObjectList.AddRange(subject.GetInventory(GO => GO.GetPartNames().OverlapsWith(Parts.CachedCommaExpansion())));
                    }
                    string message = Message.Coalesce("=subject.T= =verb:do= not have any of the correct type of items.");
                    if (gameObjectList != null || !message.Contains("=object"))
                    {
                        Popup.ShowFail(GameText.VariableReplace(message, subject));
                    }
                }
                catch (Exception x)
                {
                    MetricsManager.LogException("Require conversation item", x);
                }
                return false;
            }
            return base.HandleEvent(E);
        }

        public override bool HandleEvent(EnteredElementEvent E)
        {
            Execute();
            return base.HandleEvent(E);
        }

        public static bool ObjectHasAnyPart(GameObject GO, string Parts)
        {
            if (GO == null || Parts.IsNullOrEmpty())
            {
                return false;
            }
            foreach (string partName in Parts.CachedCommaExpansion())
            {
                if (GO.HasPart(partName))
                {
                    return true;
                }
            }
            return false;
        }

        public static bool ObjectHasAllParts(GameObject GO, string Parts)
        {
            if (GO == null || Parts.IsNullOrEmpty())
            {
                return false;
            }
            foreach (string partName in Parts.CachedCommaExpansion())
            {
                if (!GO.HasPart(partName))
                {
                    return false;
                }
            }
            return true;
        }

        public bool Execute()
        {
            if (Parts.IsNullOrEmpty())
            {
                return false;
            }

            GameObject itemGiver = (FromSpeaker ? The.Speaker : The.Player);
            GameObject itemTaker = (FromSpeaker ? The.Player : The.Speaker);

            using ScopeDisposedList<GameObject> takeableItems = ScopeDisposedList<GameObject>.GetFromPool();
            takeableItems.AddRange(itemGiver.GetInventory(GO => ObjectHasAnyPart(GO, Parts)));
            bool allItems = Amount == "*" || Amount.EqualsNoCase("all");
            int amountToTake = (allItems ? int.MaxValue : Amount.RollCached());
            foreach (GameObject item in takeableItems)
            {
                List<string> partNames = new(item.GetPartNames());
                if ((!ObjectHasAnyPart(item, Parts) && MatchAll && !ObjectHasAllParts(item, Parts))
                    || (!AllowTemporary && item.IsTemporary)
                    || amountToTake <= 0)
                {
                    continue;
                }
                int stackCount = 1;
                Stacker stacker = item.Stacker;
                if (item.Stacker is Stacker itemStacker)
                {
                    stackCount = stacker.Number;
                    if (stackCount > amountToTake)
                    {
                        stacker.SplitStack(amountToTake, The.Player);
                        stackCount = amountToTake;
                    }
                    if (Remove)
                    {
                        if (!item.TryRemoveFromContext())
                        {
                            Popup.ShowFail("You cannot give " + item.t() + "!");
                            continue;
                        }
                    }
                }
                else
                {
                    if (!itemTaker.ReceiveObject(item))
                    {
                        Popup.ShowFail("You cannot give " + item.t() + "!");
                        itemGiver.ReceiveObject(item);
                        continue;
                    }
                    Popup.Show(itemTaker.Does("take", Stripped: true) + " " + item.t(AsPossessed: false) + ".");
                    if (Unsellable)
                    {
                        item.SetIntProperty("WontSell", 1);
                    }
                    if (ClearQuest)
                    {
                        item.Physics.Category = item.GetStringProperty("OriginalCategory") 
                            ?? item.GetBlueprint().GetPartParameter("Physics", "Category", "Miscellaneous");
                        item.RemoveProperty("QuestItem");
                        item.RemoveProperty("NoAIEquip");
                    }
                }
                amountToTake -= stackCount;
            }
            return allItems || amountToTake <= 0;
        }
    }
}
