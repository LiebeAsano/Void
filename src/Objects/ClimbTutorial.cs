using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VoidTemplate.Objects;

class ClimbTutorial : TutorialTrigger
{
    public ClimbTutorial(Room room) : base(room, new RWCustom.IntRect(215, 0, room.Width, room.Height),
        new ("You have enough strength to climb the walls.",0,400),
        new ("Hold down the 'Direction' and 'Up' buttons to climb the wall.")
        )
    {
    }
}
