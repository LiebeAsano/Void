using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace VoidTemplate
{
    internal class Texture
    {
        public class PlayerGraphics : GraphicsModule
        {
            public GenericBodyPart legs;

            public PlayerGraphics(PhysicalObject owner, bool internalContainers) : base(owner, internalContainers)
            {
                // Инициализация
            }

            // Ваши методы и логика класса...
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
                        // Скрываем ноги, изменяя позицию
                        legs.pos = new Vector2(-1000, -1000); // Переместим ноги вне экрана
                                                              // legs.scale = 0; // Или можно попробовать изменять масштаб до нуля, если это доступно
                    }
                    else
                    {
                        // Возвращаем ноги в нормальное состояние
                        legs.pos = player.mainBodyChunk.pos; // Установим исходное положение ног
                                                             // legs.scale = 1; // Если мы изменяли масштаб, возвращаем обратно
                    }
                }
            }
        }
    }
    }
