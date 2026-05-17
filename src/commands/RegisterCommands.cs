using MeteoricExpansion.Entities;
using MeteoricExpansion.Entities.Behaviors;
using MeteoricExpansion.Utility;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.GameContent;
using System;
using System.Collections.Generic;

namespace MeteoricExpansion
{
    class RegisterCommands : ModSystem
    {
        public override bool ShouldLoad(EnumAppSide side)
        {
            return side == EnumAppSide.Server;
        }
        public override void StartServerSide(ICoreServerAPI api)
        {

            base.StartServerSide(api);
            //-- Registers a command that will spawn a random meteor 10 blocks above the player --//
            api.RegisterCommand("fallingmeteor", "Spawns a meteor for testing purposes.", "",
            (IServerPlayer player, int groupId, CmdArgs args) =>
            {
                AssetLocation code = new AssetLocation("meteoricexpansion", "meteor-bismuthinite-andesite");

                EntityProperties entityType = api.World.GetEntityType(code);

                if (entityType == null)
                {
                    api.Logger.Error("[MeteoricExpansion] Could not find entity type: " + code);

                    foreach (var entityProp in api.World.EntityTypes)
                    {
                        if (entityProp.Code?.Domain == "meteoricexpansion")
                        {
                            api.Logger.Warning("[MeteoricExpansion] Registered entity: " + entityProp.Code + " class=" + entityProp.Class);
                        }
                    }

                    return;
                }

                EntityFallingMeteor entity = (EntityFallingMeteor)api.World.ClassRegistry.CreateEntity(entityType);

                EntityPos entityPos = new EntityPos(
                    player.Entity.Pos.X,
                    api.WorldManager.MapSizeY - 10,
                    player.Entity.Pos.Z
                );

                entity.Pos.SetPos(entityPos);
                entity.Pos.SetFrom(entity.Pos);

                api.World.SpawnEntity(entity);

                api.Logger.Warning("[MeteoricExpansion] Spawned test meteor at: " + entity.Pos);

            }, Privilege.controlserver);
            //-- Registers a command that will spawn a random meteor 10 blocks above the player --//
            api.RegisterCommand("showermeteor", "Spawns a meteor for testing purposes.", "",
            (IServerPlayer player, int groupId, CmdArgs args) =>
            {
                EntityProperties entityType = api.World.GetEntityType(new AssetLocation("meteoricexpansion", "showermeteor-" + args[0]));
                EntityShowerMeteor entity = (EntityShowerMeteor)api.World.ClassRegistry.CreateEntity(entityType);
                EntityPos entityPos = new EntityPos(player.Entity.Pos.X, api.WorldManager.MapSizeY - 10, player.Entity.Pos.Z);

                entity.Pos.SetPos(entityPos);
                entity.Pos.SetFrom(entity.Pos);

                api.World.SpawnEntity(entity);

                entity.GetBehavior<EntityBehaviorShowerMeteorMotion>().SetMeteorTranslation(new Vec2f(20, 20));

                System.Diagnostics.Debug.WriteLine("Spawned at: " + entity.Pos);
                System.Diagnostics.Debug.WriteLine("Player at: " + player.Entity.Pos);

            }, Privilege.controlserver);

            api.RegisterCommand("testcrater", "Makes a crater below the player.", "",
                (IServerPlayer player, int groupId, CmdArgs args) =>
                {
                    IWorldAccessor world = player.Entity.World;
                    IBlockAccessor blockAccessor = world.GetBlockAccessorBulkUpdate(true, true);

                    Vec3i centerPos = new Vec3i((int)player.Entity.Pos.X, (int)player.Entity.Pos.Y - 1, (int)player.Entity.Pos.Z);
                    //BlockPos blockPos = new BlockPos();
                    int craterRadius = 3;

                    blockAccessor.WalkBlocks(new BlockPos(centerPos.X - craterRadius, centerPos.Y - craterRadius, centerPos.Z - craterRadius),
                        new BlockPos(centerPos.X + craterRadius, centerPos.Y + craterRadius, centerPos.Z + craterRadius), (block, xPos, yPos, zPos) =>
                        {
                            BlockPos blockPos = new BlockPos(xPos, yPos, zPos);

                            if (blockPos.DistanceTo(centerPos.ToBlockPos()) < craterRadius)
                            {
                                blockAccessor.SetBlock(0, blockPos);
                                blockAccessor.TriggerNeighbourBlockUpdate(blockPos);
                            }
                        }, false);

                    blockAccessor.Commit();
                }, Privilege.controlserver);


            api.RegisterCommand("fallingskies", "Weaponizes the skies.", "[count]",
            (IServerPlayer player, int groupId, CmdArgs args) =>
            {
                if (player == null)
                {
                    api.Logger.Error("[MeteoricExpansion] /fallingskies must be run by an in-game player.");
                    return;
                }

                Random rand = new Random();

                int count = 50;

                if (args.Length > 0 && int.TryParse(args[0], out int parsedCount))
                {
                    count = Math.Clamp(parsedCount, 25, 100);
                }

                bool destructive = api.World.Config.GetBool("Destructive");

                api.BroadcastMessageToAllGroups(
                    "The skies begin to fall...",
                    EnumChatType.Notification
                );

                int delay = 0;

                for (int i = 0; i < count; i++)
                {
                    delay += rand.Next(250, 1001);

                    api.Event.RegisterCallback((dt) =>
                    {
                        SpawnFallingSkiesMeteor(api, player, rand, destructive);
                    }, delay);
                }

                api.Logger.Warning("[MeteoricExpansion] FallingSkies triggered by " + player.PlayerName + " count=" + count);

            }, Privilege.controlserver);

        }
        private void SpawnFallingSkiesMeteor(ICoreServerAPI api, IServerPlayer player, Random rand, bool destructive)
        {
            if (player?.Entity == null)
            {
                return;
            }

            List<EntityProperties> meteorTypes = api.World.EntityTypes.FindAll(entity =>
                entity.Code?.Domain == "meteoricexpansion" &&
                entity.Class?.Contains("FallingMeteor") == true
            );

            if (meteorTypes == null || meteorTypes.Count == 0)
            {
                api.Logger.Warning("[MeteoricExpansion] FallingSkies found no meteor entity types.");
                return;
            }

            EntityProperties entityType = meteorTypes[rand.Next(meteorTypes.Count)];

            EntityFallingMeteor entity =
                api.World.ClassRegistry.CreateEntity(entityType) as EntityFallingMeteor;

            if (entity == null)
            {
                api.Logger.Warning("[MeteoricExpansion] FallingSkies failed to create entity: " + entityType.Code);
                return;
            }

            double offsetX = rand.Next(1, 4) * (rand.Next(0, 2) == 0 ? -1 : 1);
            double offsetZ = rand.Next(1, 4) * (rand.Next(0, 2) == 0 ? -1 : 1);

            EntityPos entityPos = new EntityPos(
                player.Entity.Pos.X + offsetX,
                api.WorldManager.MapSizeY - 20,
                player.Entity.Pos.Z + offsetZ
            );

            entity.Pos.SetPos(entityPos);
            entity.Pos.SetFrom(entity.Pos);

            float meteorSize = rand.Next(1, 7);

            entity.WatchedAttributes.SetFloat("fallingSkiesSize", meteorSize);
            entity.WatchedAttributes.SetFloat("size", meteorSize);
            entity.WatchedAttributes.SetBool("fallingSkies", true);

            if (destructive)
            {
                entity.WatchedAttributes.SetBool("fallingSkiesIgnoreClaims", true);
                entity.WatchedAttributes.SetBool("fallingSkiesForceDestructive", true);
            }

            api.World.SpawnEntity(entity);

            api.Logger.Warning(
                "[MeteoricExpansion] FallingSkies spawned "
                + entityType.Code
                + " size="
                + meteorSize
                + " at "
                + entity.Pos
            );
        }
    }
}
