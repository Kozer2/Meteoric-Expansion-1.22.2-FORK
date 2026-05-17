using MeteoricExpansion.Entities;
using MeteoricExpansion.Entities.Behaviors;
using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace MeteoricExpansion.Systems
{
    abstract class SpawnerBase: ModSystem
    {
        protected const int SPAWNER_TICK_INTERVAL = 5000;

        protected ICoreServerAPI ServerAPI { get; set; }
        protected Random SpawnerRand { get; set; }
        protected long TimeSinceSpawn { get; set; }
        protected long SpawnTickListener { get; set; }

        protected abstract Type EntityTypeToSpawn { get; set; }
        protected abstract int MinSpawnTime { get; set; }
        protected abstract int MaxSpawnTime { get; set; }
        protected abstract int MinSpawnDistance { get; set; }
        protected abstract int MaxSpawnDistance { get; set; }
        protected abstract double NextSpawn { get; set; }

        public override void StartServerSide(ICoreServerAPI api)
        {
            base.StartServerSide(api);

            foreach (var entity in api.World.EntityTypes)
            {
                if (entity.Code?.Domain == "meteoricexpansion")
                {
                    api.Logger.Warning(
                        "[MeteoricExpansion] Registered entity: "
                        + entity.Code
                        + " class="
                        + entity.Class
                    );
                }
            }

            Initialize(api);
        }
        protected virtual void Initialize(ICoreServerAPI sApi)
        {
            ServerAPI = sApi;

            TimeSinceSpawn = sApi.World.ElapsedMilliseconds;
            SpawnerRand = new Random((sApi.World.Seed * (int)TimeSinceSpawn));
        }
        public override void Dispose()
        {
            base.Dispose();

            if (SpawnTickListener != 0)
            {
                ServerAPI.Event.UnregisterCallback(SpawnTickListener);
            }
        }
        protected abstract void OnSpawnerTick(float deltaTime);

        protected string[] GetEntityCodes()
        {
            return GetEntityCodes(EntityTypeToSpawn.Name);
        }
        protected string[] GetEntityCodes(string ofType)
        {
            Queue<string> entityCodes = new Queue<string>();

            foreach (EntityProperties property in FindAllEntitiesOfType(ofType))
            {
                entityCodes.Enqueue(property.Code.Path);
            }

            return entityCodes.ToArray();
        }
        /// <summary>
        /// Fetches the entity type, assigns the entity to an object, sets its position on server and client, then returns the entity object
        /// </summary>
        /// <param name="entityCode"></param>
        /// <returns></returns>
        protected MeteorBase GenerateEntity()
        {
            string entityCode = GetRandomEntityCode();

            if (entityCode == null)
            {
                return null;
            }

            return GenerateEntity(entityCode);
        }
        protected MeteorBase GenerateEntity(string entityCode)
        {
            int playerToSpawnOn = SpawnNearPlayer();

            EntityProperties entityType = ServerAPI.World.GetEntityType(new AssetLocation("meteoricexpansion", entityCode));
            
            MeteorBase entity = ServerAPI.World.ClassRegistry.CreateEntity(entityType) as MeteorBase;

            EntityPos entityPos = new EntityPos(ServerAPI.World.AllOnlinePlayers[playerToSpawnOn].Entity.Pos.X + GetSpawnOffset(), ServerAPI.WorldManager.MapSizeY, ServerAPI.World.AllOnlinePlayers[playerToSpawnOn].Entity.Pos.Z + GetSpawnOffset());

            entity.Pos.SetPos(entityPos);
            entity.Pos.SetFrom(entity.Pos);

            return entity;
        }
        protected string GetRandomEntityCode()
        {
            string[] possibleCodes = GetEntityCodes();

            if (possibleCodes == null || possibleCodes.Length == 0)
            {
                ServerAPI.Logger.Warning("[MeteoricExpansion] No entity codes returned by GetEntityCodes(). Skipping spawn.");
                return null;
            }

            int index = SpawnerRand.Next(possibleCodes.Length);
            return possibleCodes[index];
        }
        protected int SpawnNearPlayer()
        {
            int totalPlayersOnline = ServerAPI.World.AllOnlinePlayers.Length;

            return SpawnerRand.Next(0, totalPlayersOnline);
        }
        /// <summary>
        /// Offsets meteor spawn position so that it does not spawn directly above the player. Be sure to set the MinSpawnDistance and MaxSpawnDistance before calling the base method.
        /// </summary>
        /// <returns></returns>
        protected virtual double GetSpawnOffset()
        {
            int negativeRand = SpawnerRand.Next(0, 2);
            double spawnOffset = SpawnerRand.Next(ServerAPI.WorldManager.ChunkSize * MinSpawnDistance, ServerAPI.WorldManager.ChunkSize * MaxSpawnDistance) + SpawnerRand.NextDouble();

            if (negativeRand == 0)
                return -spawnOffset;

            return spawnOffset;
        }
        protected Vec3d DetermineTranslation()
        {
            double horiztontalTranslation, verticalTranslation;

            horiztontalTranslation = SpawnerRand.NextDouble();
            verticalTranslation = SpawnerRand.NextDouble();

            bool isMovingEast = Convert.ToBoolean(SpawnerRand.Next(0, 2));
            bool isMovingSouth = Convert.ToBoolean(SpawnerRand.Next(0, 2));

            Vec3d randomTranslation = new Vec3d
            {
                X = horiztontalTranslation,
                Y = verticalTranslation,
                Z = 1 - horiztontalTranslation
            };

            if (isMovingEast != false)
                randomTranslation.X *= -1;

            if (isMovingSouth != false)
                randomTranslation.Y *= -1;

            return randomTranslation;
        }
        private List<EntityProperties> FindAllEntitiesOfType(string entityClass)
        {
            var found = ServerAPI.World.EntityTypes.FindAll(entity =>
                entity.Class == entityClass
            );

            ServerAPI.Logger.Warning(
                "[MeteoricExpansion] Searching entity class '" + entityClass + "' found " + found.Count + " matches."
            );

            return found;
        }
    }
}
