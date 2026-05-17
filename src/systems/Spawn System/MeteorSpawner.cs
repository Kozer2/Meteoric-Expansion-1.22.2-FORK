using System;
using Vintagestory.API.Server;
using MeteoricExpansion.Utility;
using MeteoricExpansion.Entities;
using MeteoricExpansion.EntityRenderers;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Common;

namespace MeteoricExpansion.Systems
{
    class MeteorSpawner: SpawnerBase
    {
        protected override Type EntityTypeToSpawn { get; set; }
        protected override int MinSpawnTime { get; set; }
        protected override int MaxSpawnTime { get; set; }
        protected override int MinSpawnDistance { get; set; }
        protected override int MaxSpawnDistance { get; set; }
        protected override double NextSpawn { get; set; }
        private bool ConfigDisableFallingMeteors { get; set; }

        //public override bool ShouldLoad(EnumAppSide side)
        //{
        //    return side == EnumAppSide.Server;
        //}

        public override void StartServerSide(ICoreServerAPI api)
        {
            base.StartServerSide(api);


            api.Logger.Warning("========== METEORIC EXPANSION DEBUG ==========");

            api.Logger.Warning(
                "[MeteoricExpansion DEBUG] Mod path root loaded."
            );

            var dllDir = System.IO.Path.GetDirectoryName(typeof(RegisterCommands).Assembly.Location);

            foreach (var path in System.IO.Directory.GetFileSystemEntries(dllDir))
            {
                api.Logger.Warning("[MeteoricExpansion DEBUG] unpack entry: " + path);
            }
            api.Logger.Warning("[MeteoricExpansion DEBUG] dllDir: " + dllDir);

            var physicalAsset = System.IO.Path.Combine(dllDir, "assets", "meteoricexpansion", "entities", "air", "meteor.json");
            api.Logger.Warning("[MeteoricExpansion DEBUG] physical meteor file exists: " + System.IO.File.Exists(physicalAsset));

            string[] assetChecks =
            {
                "meteoricexpansion:entities/air/meteor.json",
                "meteoricexpansion:entities/air/showermeteor.json",
                "meteoricexpansion:textures/block/rockash.png",
                "meteoricexpansion:shapes/entity/meteor.json"
            };

            foreach (string assetPath in assetChecks)
            {
                var asset = api.Assets.TryGet(assetPath);

                api.Logger.Warning(
                    "[MeteoricExpansion DEBUG] Asset check: "
                    + assetPath
                    + " => "
                    + (asset != null)
                );
            }

            api.Logger.Warning(
                "[MeteoricExpansion DEBUG] Total entity types loaded: "
                + api.World.EntityTypes.Count
            );

            foreach (var entity in api.World.EntityTypes)
            {
                if (
                    entity.Code?.Domain == "meteoricexpansion" ||
                    entity.Code?.Path?.Contains("meteor") == true
                )
                {
                    api.Logger.Warning(
                        "[MeteoricExpansion DEBUG] ENTITY REGISTERED => code="
                        + entity.Code
                        + " class="
                        + entity.Class
                    );
                }
            }

            api.Logger.Warning("========== END METEORIC DEBUG ==========");


            Initialize(api);
        }
        public override void Dispose()
        {
            if (ConfigDisableFallingMeteors == false)
            {
                base.Dispose();
            }
        }
        protected override void Initialize(ICoreServerAPI sApi)
        {
            ConfigDisableFallingMeteors = sApi.World.Config.GetBool("DisableFallingMeteors");

            if(ConfigDisableFallingMeteors == false)
            {
                base.Initialize(sApi);

                EntityTypeToSpawn = typeof(EntityFallingMeteor);

                SpawnTickListener = ServerAPI.Event.RegisterGameTickListener(OnSpawnerTick, SPAWNER_TICK_INTERVAL);

                MinSpawnTime = ServerAPI.World.Config.GetInt("MinimumMinutesBetweenMeteorSpawns");
                MaxSpawnTime = ServerAPI.World.Config.GetInt("MaximumMinutesBetweenMeteorSpawns");

                MinSpawnDistance = ServerAPI.World.Config.GetInt("MinimumSpawnDistanceInChunks");
                MaxSpawnDistance = ServerAPI.World.Config.GetInt("MaximumSpawnDistanceInChunks");

                NextSpawn = SpawnerRand.Next(MinSpawnTime, MaxSpawnTime) + SpawnerRand.NextDouble();
                NextSpawn = MeteoricExpansionHelpers.ConvertMinutesToMilliseconds(NextSpawn);

                MeteoricExpansionHelpers.InitializeHelpers(ServerAPI.World.Seed);
            }
        }
        //-- Spawn a meteor made with random rock and metals above the first online player every number of seconds as determined by tickIntervalInSeconds --//
        //-- Eventually spawns will happen between the minMeteorSpawnTime and maxMeteorSpawnTime --// 
        protected override void OnSpawnerTick(float deltaTime)
        {
            if(ServerAPI.World.ElapsedMilliseconds - TimeSinceSpawn > NextSpawn)
            {
                //-- This was moved out of the base class. For some reason the renderer wasn't being applied when inside it. --//
                if (ServerAPI.World.AllOnlinePlayers.Length > 0)
                {
                    int playerToSpawnOn = SpawnNearPlayer();

                    string entityCode = GetRandomEntityCode();

                    if (entityCode == null)
                    {
                        return;
                    }

                    EntityProperties entityType = ServerAPI.World.GetEntityType(
                        new AssetLocation("meteoricexpansion", entityCode)
                    );

                    if (entityType == null)
                    {
                        ServerAPI.Logger.Warning("[MeteoricExpansion] Entity type not found: " + entityCode);
                        return;
                    }
                    EntityFallingMeteor entity = (EntityFallingMeteor)ServerAPI.World.ClassRegistry.CreateEntity(entityType);
                    EntityPos entityPos = new EntityPos(ServerAPI.World.AllOnlinePlayers[playerToSpawnOn].Entity.Pos.X + GetSpawnOffset(), ServerAPI.WorldManager.MapSizeY - 10, ServerAPI.World.AllOnlinePlayers[playerToSpawnOn].Entity.Pos.Z + GetSpawnOffset());

                    entity.Pos.SetPos(entityPos);
                    entity.Pos.SetFrom(entity.Pos);

                    ServerAPI.World.SpawnEntity(entity);
                }

                NextSpawn = SpawnerRand.Next(MinSpawnTime, MaxSpawnTime) + SpawnerRand.NextDouble();
                NextSpawn = MeteoricExpansionHelpers.ConvertMinutesToMilliseconds(NextSpawn);

                TimeSinceSpawn = this.ServerAPI.World.ElapsedMilliseconds;
            }
        }
    }
}
