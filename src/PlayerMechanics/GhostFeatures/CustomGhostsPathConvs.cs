using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using MonoMod.RuntimeDetour;
using RegionKit.Modules.EchoExtender;
using VoidTemplate.Useful;

namespace VoidTemplate.PlayerMechanics.GhostFeatures
{
    public class CustomGhostsPathConvs
    {
        public static void Hook()
        {
            EEGhostConvHook();
        }

        public static void EEGhostConvHook()
        {
            new Hook(typeof(EEGhostConversation).GetMethod("ResolveEchoConversation", BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic),
                new Func<Func<EEGhostConversation, InGameTranslator.LanguageID, string, string>, EEGhostConversation, InGameTranslator.LanguageID, string, string>((orig, self, lang, region) =>
                {
                    if (self.currentSaveFile == VoidEnums.SlugcatID.Void && self.ghost.room.game.session is StoryGameSession story)
                    {
                        string mark = story.saveState.deathPersistentSaveData.theMark ? "mark" : "nomark";
                        string langPath = self.ghost.room.game.rainWorld.inGameTranslator.SpecificTextFolderDirectory(lang);
                        string path = AssetManager.ResolveFilePath($"{langPath}/echoConvVoid_{region}_{mark}.txt");

                        if (File.Exists(path))
                        {
                            if (lang == InGameTranslator.LanguageID.English)
                            {
                                return EchoParser.ManageXOREncryption(path);
                            }
                            return File.ReadAllText(path);
                        }
                    }
                    return orig(self, lang, region);
                }));

        }


    }
}
