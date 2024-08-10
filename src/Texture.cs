using UnityEngine;

namespace VoidTemplate
{
    internal class Texture
    {
        public class PlayerGraphics(PhysicalObject owner, bool internalContainers) : GraphicsModule(owner, internalContainers)
        {
            public GenericBodyPart legs;

            public override void Update()
            {
                base.Update();
                UpdateLegsVisibility();
            }

            private void UpdateLegsVisibility()
            {
                if (owner is Player player)
                {
                    if (player.bodyMode == BodyModeIndexExtension.CeilCrawl)
                    {
                        legs.pos = new Vector2(1000, 1000);
                    }
                    else
                    {
                        legs.pos = player.mainBodyChunk.pos;
                    }
                }
            }
        }
    }
}
