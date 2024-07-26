using HUD;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VoidTemplate.Oracles;

internal class VoidConversation : Conversation
{
    public VoidConversation(IOwnAConversation interfaceOwner, ID id, DialogBox dialogBox) : base(interfaceOwner, id, dialogBox)
    {
    }
}
