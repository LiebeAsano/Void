using MoreSlugcats;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VoidTemplate.Oracles
{
    /// <summary>
    /// A function library for SSOracleBehavior.ConversationBehavior implementations. Consider moving these to a better place
    /// (a unified ConversationBehavior impl or a common intermediate <see langword="abstract"/>) when refactoring these.
    /// 
    /// Библиотека функций для реализаций SSOracleBehaviour.Convers SSOracleBehavior.ConversationBehavior. Нужно подумать о 
    /// более подходящем месте для них (либо единый ConversationBehavior, либо общий промежуточный <see langword="abstract"/>) при рефакторе этих классов.
    /// </summary>
    public static class VoidOracleUtils
    {
        public static void SSOracleVoidCommonConvoEnd(this SSOracleBehavior.ConversationBehavior callingConvoBehavior)
        {
            SSOracleBehavior.Action oracleAction = callingConvoBehavior.oracle.room.game.GetStorySession.saveState.GetVoidMeetMoon() ?
                MoreSlugcatsEnums.SSOracleBehaviorAction.Pebbles_SlumberParty :
                SSOracleBehavior.Action.ThrowOut_ThrowOut;

            callingConvoBehavior.owner.UnlockShortcuts();
            callingConvoBehavior.owner.NewAction(oracleAction);
            callingConvoBehavior.owner.getToWorking = 1f;
        }
    }
}
