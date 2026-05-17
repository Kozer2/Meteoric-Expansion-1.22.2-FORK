using MeteoricExpansion.Entities;
using System;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;

namespace MeteoricExpansion
{
    class RegisterEntities : ModSystem
    {
        public override void Start(ICoreAPI api)
        {
            base.Start(api);

            api.RegisterEntity("EntityFallingMeteor", typeof(EntityFallingMeteor));
            api.RegisterEntity("EntityShowerMeteor", typeof(EntityShowerMeteor));

            api.Event.EnqueueMainThreadTask(() =>
            {
                foreach (var entity in api.World.EntityTypes)
                {
                    if (entity.Code?.Domain == "meteoricexpansion" ||
                        entity.Code?.Path?.Contains("meteor") == true)
                    {
                        api.Logger.Warning(
                            "[MeteoricExpansion DEBUG] entity code="
                            + entity.Code
                            + " class="
                            + entity.Class
                        );
                    }
                }
            }, "meteoricexpansion-debug-entitydump");
        }
    }
}
