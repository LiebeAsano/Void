using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace VoidTemplate.Objects.MarkItem
{
    public class MarkHooks
    {

        public static void Hook()
        {
            On.SLOracleBehaviorHasMark.TypeOfMiscItem += SLOracleBehaviorHasMark_TypeOfMiscItem;
            On.SLOracleBehaviorHasMark.MoonConversation.AddEvents += MoonConversation_AddEvents;
        }

        private static void MoonConversation_AddEvents(On.SLOracleBehaviorHasMark.MoonConversation.orig_AddEvents orig, SLOracleBehaviorHasMark.MoonConversation self)
        {
            if (self.describeItem == VoidEnums.MiscTalkItem.VoidMark)
            {
                self.events.Add(new Conversation.TextEvent(self, 10, "Нормальная метка", 0));
            }
            else if (self.describeItem == VoidEnums.MiscTalkItem.VoidMarkV2)
            {
                self.events.Add(new Conversation.TextEvent(self, 10, "Метка версии 2", 0));
            }
            else if (self.describeItem == VoidEnums.MiscTalkItem.VoidMarkV3)
            {
                self.events.Add(new Conversation.TextEvent(self, 10, "Метка версии 3", 0));
            }
            else
                orig(self);
        }

        private static SLOracleBehaviorHasMark.MiscItemType SLOracleBehaviorHasMark_TypeOfMiscItem(On.SLOracleBehaviorHasMark.orig_TypeOfMiscItem orig, SLOracleBehaviorHasMark self, PhysicalObject testItem)
        {
            if (testItem is Mark mark)
            {
                if (mark.MType == Mark.MarkType.V3)
                    return VoidEnums.MiscTalkItem.VoidMarkV3;

                else if (mark.MType == Mark.MarkType.V2)
                    return VoidEnums.MiscTalkItem.VoidMarkV2;

                return VoidEnums.MiscTalkItem.VoidMark;
            }
            return orig(self, testItem);
        }
    }
}
