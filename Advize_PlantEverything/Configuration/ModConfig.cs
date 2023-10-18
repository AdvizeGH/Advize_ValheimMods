using BepInEx.Configuration;
using ServerSync;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace Advize_PlantEverything.Configuration
{
    class ModConfig
    {
        private readonly ConfigFile ConfigFile;
        private readonly ConfigSync ConfigSync;

        //General 4
        private readonly ConfigEntry<bool> serverConfigLocked;
        internal readonly ConfigEntry<int> nexusID; //local
        private readonly ConfigEntry<bool> enableDebugMessages; //local
        private readonly ConfigEntry<bool> showPickableSpawners; //local
        private readonly ConfigEntry<bool> enableMiscFlora;
        private readonly ConfigEntry<bool> enableExtraResources;
        private readonly ConfigEntry<bool> snappableVines; //local
        private readonly ConfigEntry<bool> enableLocalization; //local
        private readonly ConfigEntry<string> language; //local
        private readonly ConfigEntry<string> disabledResourceNames;

        //Difficulty 7
        private readonly ConfigEntry<bool> requireCultivation;
        private readonly ConfigEntry<bool> placeAnywhere;
        private readonly ConfigEntry<bool> enforceBiomes;
        private readonly ConfigEntry<bool> enforceBiomesVanilla;
        private readonly ConfigEntry<bool> recoverResources;
        private readonly ConfigEntry<bool> resourcesSpawnEmpty;
        private readonly ConfigEntry<bool> enemiesTargetPieces;

        //Berries 9
        private readonly ConfigEntry<int> raspberryCost;
        private readonly ConfigEntry<int> blueberryCost;
        private readonly ConfigEntry<int> cloudberryCost;
        private readonly ConfigEntry<int> raspberryRespawnTime;
        private readonly ConfigEntry<int> blueberryRespawnTime;
        private readonly ConfigEntry<int> cloudberryRespawnTime;
        private readonly ConfigEntry<int> raspberryReturn;
        private readonly ConfigEntry<int> blueberryReturn;
        private readonly ConfigEntry<int> cloudberryReturn;

        //Crops 30
        private readonly ConfigEntry<bool> enableCropOverrides;
        private readonly ConfigEntry<bool> cropRequireCultivation;
        private readonly ConfigEntry<bool> cropRequireSunlight;
        private readonly ConfigEntry<bool> cropRequireGrowthSpace;
        private readonly ConfigEntry<bool> enemiesTargetCrops;
        private readonly ConfigEntry<float> cropMinScale;
        private readonly ConfigEntry<float> cropMaxScale;
        private readonly ConfigEntry<float> cropGrowTimeMin;
        private readonly ConfigEntry<float> cropGrowTimeMax;
        private readonly ConfigEntry<float> cropGrowRadius;
        private readonly ConfigEntry<int> barleyCost;
        private readonly ConfigEntry<int> barleyReturn;
        private readonly ConfigEntry<int> carrotCost;
        private readonly ConfigEntry<int> carrotReturn;
        private readonly ConfigEntry<int> flaxCost;
        private readonly ConfigEntry<int> flaxReturn;
        private readonly ConfigEntry<int> onionCost;
        private readonly ConfigEntry<int> onionReturn;
        private readonly ConfigEntry<int> seedCarrotCost;
        private readonly ConfigEntry<int> seedCarrotReturn;
        private readonly ConfigEntry<int> seedOnionCost;
        private readonly ConfigEntry<int> seedOnionReturn;
        private readonly ConfigEntry<int> seedTurnipCost;
        private readonly ConfigEntry<int> seedTurnipReturn;
        private readonly ConfigEntry<int> turnipCost;
        private readonly ConfigEntry<int> turnipReturn;
        private readonly ConfigEntry<int> magecapCost;
        private readonly ConfigEntry<int> magecapReturn;
        private readonly ConfigEntry<int> jotunPuffsCost;
        private readonly ConfigEntry<int> jotunPuffsReturn;

        //Debris 6
        private readonly ConfigEntry<int> pickableBranchCost;
        private readonly ConfigEntry<int> pickableBranchReturn;
        private readonly ConfigEntry<int> pickableStoneCost;
        private readonly ConfigEntry<int> pickableStoneReturn;
        private readonly ConfigEntry<int> pickableFlintCost;
        private readonly ConfigEntry<int> pickableFlintReturn;

        //Mushrooms 9
        private readonly ConfigEntry<int> mushroomCost;
        private readonly ConfigEntry<int> yellowMushroomCost;
        private readonly ConfigEntry<int> blueMushroomCost;
        private readonly ConfigEntry<int> mushroomRespawnTime;
        private readonly ConfigEntry<int> yellowMushroomRespawnTime;
        private readonly ConfigEntry<int> blueMushroomRespawnTime;
        private readonly ConfigEntry<int> mushroomReturn;
        private readonly ConfigEntry<int> yellowMushroomReturn;
        private readonly ConfigEntry<int> blueMushroomReturn;

        //Flowers 6
        private readonly ConfigEntry<int> thistleCost;
        private readonly ConfigEntry<int> dandelionCost;
        private readonly ConfigEntry<int> thistleRespawnTime;
        private readonly ConfigEntry<int> dandelionRespawnTime;
        private readonly ConfigEntry<int> thistleReturn;
        private readonly ConfigEntry<int> dandelionReturn;

        //Saplings 32
        private readonly ConfigEntry<float> birchMinScale;
        private readonly ConfigEntry<float> birchMaxScale;
        private readonly ConfigEntry<float> oakMinScale;
        private readonly ConfigEntry<float> oakMaxScale;
        private readonly ConfigEntry<float> ancientMinScale;
        private readonly ConfigEntry<float> ancientMaxScale;
        private readonly ConfigEntry<float> birchGrowthTime;
        private readonly ConfigEntry<float> oakGrowthTime;
        private readonly ConfigEntry<float> ancientGrowthTime;
        private readonly ConfigEntry<float> birchGrowRadius;
        private readonly ConfigEntry<float> oakGrowRadius;
        private readonly ConfigEntry<float> ancientGrowRadius;
        private readonly ConfigEntry<float> beechGrowthTime;
        private readonly ConfigEntry<float> pineGrowthTime;
        private readonly ConfigEntry<float> firGrowthTime;
        private readonly ConfigEntry<float> beechMinScale;
        private readonly ConfigEntry<float> beechMaxScale;
        private readonly ConfigEntry<float> pineMinScale;
        private readonly ConfigEntry<float> pineMaxScale;
        private readonly ConfigEntry<float> firMinScale;
        private readonly ConfigEntry<float> firMaxScale;
        private readonly ConfigEntry<float> beechGrowRadius;
        private readonly ConfigEntry<float> pineGrowRadius;
        private readonly ConfigEntry<float> firGrowRadius;
        private readonly ConfigEntry<float> yggaMinScale;
        private readonly ConfigEntry<float> yggaMaxScale;
        private readonly ConfigEntry<float> yggaGrowthTime;
        private readonly ConfigEntry<float> yggaGrowRadius;
        private readonly ConfigEntry<float> autumnBirchMinScale;
        private readonly ConfigEntry<float> autumnBirchMaxScale;
        private readonly ConfigEntry<float> autumnBirchGrowthTime;
        private readonly ConfigEntry<float> autumnBirchGrowRadius;

        //Seeds 7
        private readonly ConfigEntry<bool> enableSeedOverrides;
        private readonly ConfigEntry<int> seedDropMin;
        private readonly ConfigEntry<int> seedDropMax;
        private readonly ConfigEntry<int> treeDropMin;
        private readonly ConfigEntry<int> treeDropMax;
        private readonly ConfigEntry<float> dropChance;
        private readonly ConfigEntry<bool> oneOfEach;

        //UI 0
        private readonly ConfigEntry<bool> enablePickableTimers; //local
        private readonly ConfigEntry<bool> enablePlantTimers; //local
        private readonly ConfigEntry<bool> growthAsPercentage; //local

        //CustomSyncedValue
        private readonly CustomSyncedValue<List<string>> extraResources;

        private ConfigEntry<T> Config<T>(string group, string name, T value, ConfigDescription description, bool synchronizedSetting = true)
        {
            ConfigEntry<T> configEntry = ConfigFile.Bind(group, name, value, description);

            SyncedConfigEntry<T> syncedConfigEntry = ConfigSync.AddConfigEntry(configEntry);
            syncedConfigEntry.SynchronizedConfig = synchronizedSetting;

            return configEntry;
        }

        private ConfigEntry<T> Config<T>(string group, string name, T value, string description, bool synchronizedSetting = true) => Config(group, name, value, new ConfigDescription(description), synchronizedSetting);

        private readonly ConfigurationManagerAttributes seedSettingAtrributes = new();
        private readonly List<ConfigurationManagerAttributes> cropSettingAttributes = new()
        {
            { new() },
            { new ConfigurationManagerAttributes { Order = 26 } },
            { new ConfigurationManagerAttributes { Order = 25 } },
            { new ConfigurationManagerAttributes { Order = 24 } }
        };

        internal ModConfig(ConfigFile configFile, ConfigSync configSync)
        {
            ConfigFile = configFile; ConfigSync = configSync;

            //General
            serverConfigLocked = Config(
                "General",
                "LockConfiguration",
                true,
                "If on, the configuration is locked and can be changed by server admins only.");
            nexusID = Config(
                "General",
                "NexusID",
                1042,
                new ConfigDescription("Nexus mod ID for updates.", null, new ConfigurationManagerAttributes{ Category = "Internal", ReadOnly = true }),
                false);
            enableDebugMessages = Config(
                "General",
                "EnableDebugMessages",
                false,
                "Enable mod debug messages in console.",
                false);
            showPickableSpawners = Config(
                "General",
                "ShowPickableSpawners",
                true,
                "Continue to show mushroom, thistle, and dandelion spawners after being picked. (Requires world reload on client to take effect.)",
                false);
            enableMiscFlora = Config(
                "General",
                "EnableMiscFlora",
                true,
                "Enables small trees, bushes, shrubs, vines, and large mushrooms.");
            enableExtraResources = Config(
                "General",
                "EnableExtraResources",
                false,
                "When set to true, the mod will attempt to make user-defined prefabs buildable with the cultivator. Prefabs are defined in PlantEverything_ExtraResources.cfg. If file is not present, an example one will be generated for you.");
            snappableVines = Config(
                "General",
                "SnappableVines",
                true,
                "Enables snap points when placing vines adjacently.",
                false);
            enableLocalization = Config(
                "General",
                "EnableLocalization",
                false,
                "Enable this to attempt to load localized text data for the language set in the following setting.",
                false);
            language = Config(
                "General",
                "Language",
                "english",
                "Language to be used. If EnableLocalization is enabled, game will attempt to load localized text from a file named {language}_PlantEverything.json.",
                false);
            disabledResourceNames = Config(
                "General",
                "DisabledResourceNames",
                "",
                new ConfigDescription("To disable specific resources added by this mod (and not add them to the cultivator), list their prefab names here separated by a comma. Names are case-sensitive.",
                null, new ConfigurationManagerAttributes { Order = -1 }));

            //Difficulty
            requireCultivation = Config(
                "Difficulty",
                "RequireCultivation",
                false,
                "Pickable resources can only be planted on cultivated ground.");
            placeAnywhere = Config(
                "Difficulty",
                "PlaceAnywhere",
                false,
                "Allow resources to be placed anywhere (not just on the ground). Does not apply to mushrooms or flowers.");
            enforceBiomes = Config(
                "Difficulty",
                "EnforceBiomes",
                false,
                "Restrict modded plantables to being placed in their respective biome.");
            enforceBiomesVanilla = Config(
                "Difficulty",
                "EnforceBiomesVanilla",
                true,
                "Restrict vanilla plantables to being placed in their respective biome.");
            recoverResources = Config(
                "Difficulty",
                "RecoverResources",
                false,
                "Recover resources when pickables are removed with the cultivator. Applies to berries, mushrooms, and flowers.");
            resourcesSpawnEmpty = Config(
                "Difficulty",
                "ResourcesSpawnEmpty",
                false,
                "Specifies whether resources should spawn empty or full. Applies to berry bushes, mushrooms, flowers, and debris.");
            enemiesTargetPieces = Config(
                "Difficulty",
                "EnemiesTargetPieces",
                true,
                "When enabled, enemies may target and attack player placed resources added by the mod. If this setting is changed, pre-existing placed pieces will not be affected until the world/server is reloaded.");

            //Berries
            raspberryCost = Config(
                "Berries",
                "RaspberryCost",
                5,
                "Number of raspberries required to place a raspberry bush. Set to 0 to disable the ability to plant this resource.");
            blueberryCost = Config(
                "Berries",
                "BlueberryCost",
                5,
                "Number of blueberries required to place a blueberry bush. Set to 0 to disable the ability to plant this resource.");
            cloudberryCost = Config(
                "Berries",
                "CloudberryCost",
                5,
                "Number of cloudberries required to place a cloudberry bush. Set to 0 to disable the ability to plant this resource.");
            raspberryRespawnTime = Config(
                "Berries",
                "RaspberryRespawnTime",
                300,
                "Number of minutes it takes for a raspberry bush to respawn berries.");
            blueberryRespawnTime = Config(
                "Berries",
                "BlueberryRespawnTime",
                300,
                "Number of minutes it takes for a blueberry bush to respawn berries.");
            cloudberryRespawnTime = Config(
                "Berries",
                "CloudberryRespawnTime",
                300,
                "Number of minutes it takes for a cloudberry bush to respawn berries.");
            raspberryReturn = Config(
                "Berries",
                "RaspberryReturn",
                1,
                "Number of berries a raspberry bush will spawn.");
            blueberryReturn = Config(
                "Berries",
                "BlueberryReturn",
                1,
                "Number of berries a blueberry bush will spawn.");
            cloudberryReturn = Config(
                "Berries",
                "CloudberryReturn",
                1,
                "Number of berries a cloudberry bush will spawn.");

            //Crops
            enableCropOverrides = Config(
                "Crops",
                "EnableCropOverrides",
                false,
                new ConfigDescription("Enables the [Crops] section of this config.", null, new ConfigurationManagerAttributes { Order = 27 }));
            cropMinScale = Config(
                "Crops",
                "CropMinScale",
                0.9f,
                new ConfigDescription("The minimum scaling factor used to scale crops upon growth.", null, cropSettingAttributes[0]));
            cropMaxScale = Config(
                "Crops",
                "CropMaxScale",
                1.1f,
                new ConfigDescription("The maximum scaling factor used to scale crops upon growth.", null, cropSettingAttributes[0]));
            cropGrowTimeMin = Config(
                "Crops",
                "CropGrowTimeMin",
                4000f,
                new ConfigDescription("Minimum number of seconds it takes for crops to grow (will take at least 10 seconds after planting to grow).", null, cropSettingAttributes[0]));
            cropGrowTimeMax = Config(
                "Crops",
                "CropGrowTimeMax",
                5000f,
                new ConfigDescription("Maximum number of seconds it takes for crops to grow (will take at least 10 seconds after planting to grow).", null, cropSettingAttributes[0]));
            cropGrowRadius = Config(
                "Crops",
                "CropGrowRadius",
                0.5f,
                new ConfigDescription("Radius of free space required for crops to grow.", null, cropSettingAttributes[0]));
            cropRequireCultivation = Config(
                "Crops",
                "CropsRequireCultivation",
                true,
                new ConfigDescription("Crops can only be planted on cultivated ground.", null, cropSettingAttributes[1]));
            cropRequireSunlight = Config(
                "Crops",
                "CropsRequireSunlight",
                true,
                new ConfigDescription("Crops can only grow under an open sky.", null, cropSettingAttributes[2]));
            cropRequireGrowthSpace = Config(
                "Crops",
                "CropsRequireGrowthSpace",
                true,
                new ConfigDescription("Crops require space to grow. This setting overrides the CropGrowRadius setting but without altering it, allowing grid spacing mods to continue functioning.", null, cropSettingAttributes[3]));
            enemiesTargetCrops = Config(
                "Crops",
                "EnemiesTargetCrops",
                true,
                new ConfigDescription("Determines whether enemies will target and attack crops. If this setting is changed, pre-existing placed crops will not be affected until the world/server is reloaded.", null, cropSettingAttributes[0]));
            barleyCost = Config(
                "Crops",
                "BarleyCost",
                1,
                new ConfigDescription("Resource cost of planting barley.", null, cropSettingAttributes[0]));
            barleyReturn = Config(
                "Crops",
                "BarleyReturn",
                2,
                new ConfigDescription("Resources gained upon harvesting barley (does not apply to wild barley).", null, cropSettingAttributes[0]));
            carrotCost = Config(
                "Crops",
                "CarrotCost",
                1,
                new ConfigDescription("Resource cost of planting carrots.", null, cropSettingAttributes[0]));
            carrotReturn = Config(
                "Crops",
                "CarrotReturn",
                1,
                new ConfigDescription("Resources gained upon harvesting carrots.", null, cropSettingAttributes[0]));
            flaxCost = Config(
                "Crops",
                "FlaxCost",
                1,
                new ConfigDescription("Resource cost of planting flax.", null, cropSettingAttributes[0]));
            flaxReturn = Config(
                "Crops",
                "FlaxReturn",
                2,
                new ConfigDescription("Resources gained upon harvesting flax (does not apply to wild flax).", null, cropSettingAttributes[0]));
            onionCost = Config(
                "Crops",
                "OnionCost",
                1,
                new ConfigDescription("Resource cost of planting onions.", null, cropSettingAttributes[0]));
            onionReturn = Config(
                "Crops",
                "OnionReturn",
                1,
                new ConfigDescription("Resources gained upon harvesting onions.", null, cropSettingAttributes[0]));
            seedCarrotCost = Config(
                "Crops",
                "SeedCarrotCost",
                1,
                new ConfigDescription("Resource cost of planting seed carrots.", null, cropSettingAttributes[0]));
            seedCarrotReturn = Config(
                "Crops",
                "SeedCarrotReturn",
                3,
                new ConfigDescription("Resources gained upon harvesting seed carrots.", null, cropSettingAttributes[0]));
            seedOnionCost = Config(
                "Crops",
                "SeedOnionCost",
                1,
                new ConfigDescription("Resource cost of planting seed onions.", null, cropSettingAttributes[0]));
            seedOnionReturn = Config(
                "Crops",
                "SeedOnionReturn",
                3,
                new ConfigDescription("Resources gained upon harvesting seed onions.", null, cropSettingAttributes[0]));
            seedTurnipCost = Config(
                "Crops",
                "SeedTurnipCost",
                1,
                new ConfigDescription("Resource cost of planting seed turnips.", null, cropSettingAttributes[0]));
            seedTurnipReturn = Config(
                "Crops",
                "SeedTurnipReturn",
                3,
                new ConfigDescription("Resources gained upon harvesting seed turnips.", null, cropSettingAttributes[0]));
            turnipCost = Config(
                "Crops",
                "TurnipCost",
                1,
                new ConfigDescription("Resource cost of planting turnips.", null, cropSettingAttributes[0]));
            turnipReturn = Config(
                "Crops",
                "TurnipReturn",
                1,
                new ConfigDescription("Resources gained upon harvesting turnips.", null, cropSettingAttributes[0]));
            magecapCost = Config(
                "Crops",
                "MagecapCost",
                1,
                new ConfigDescription("Resource cost of planting magecap.", null, cropSettingAttributes[0]));
            magecapReturn = Config(
                "Crops",
                "MagecapReturn",
                3,
                new ConfigDescription("Resources gained upon harvesting magecap.", null, cropSettingAttributes[0]));
            jotunPuffsCost = Config(
                "Crops",
                "JotunPuffsCost",
                1,
                new ConfigDescription("Resource cost of planting Jotun puffs.", null, cropSettingAttributes[0]));
            jotunPuffsReturn = Config(
                "Crops",
                "JotunPuffsReturn",
                3,
                new ConfigDescription("Resources gained upon harvesting Jotun puffs.", null, cropSettingAttributes[0]));

            //Debris
            pickableBranchCost = Config(
                "Debris",
                "PickableBranchCost",
                5,
                "Amount of wood required to place branch debris. Set to 0 to disable the ability to plant this resource.");
            pickableBranchReturn = Config(
                "Debris",
                "PickableBranchReturn",
                1,
                "Amount of wood that branch debris drops when picked.");
            pickableStoneCost = Config(
                "Debris",
                "PickableStoneCost",
                1,
                "Amount of stone required to place stone debris. Set to 0 to disable the ability to plant this resource.");
            pickableStoneReturn = Config(
                "Debris",
                "PickableStoneReturn",
                1,
                "Amount of stones that stone debris drops when picked.");
            pickableFlintCost = Config(
                "Debris",
                "PickableFlintCost",
                5,
                "Amount of flint required to place flint debris. Set to 0 to disable the ability to plant this resource.");
            pickableFlintReturn = Config(
                "Debris",
                "PickableFlintReturn",
                1,
                "Amount of flint that flint debris drops when picked.");

            //Mushrooms
            mushroomCost = Config(
                "Mushrooms",
                "MushroomCost",
                5,
                "Number of mushrooms required to place a pickable mushroom spawner. Set to 0 to disable the ability to plant this resource.");
            yellowMushroomCost = Config(
                "Mushrooms",
                "YellowMushroomCost",
                5,
                "Number of yellow mushrooms required to place a pickable yellow mushroom spawner. Set to 0 to disable the ability to plant this resource.");
            blueMushroomCost = Config(
                "Mushrooms",
                "BlueMushroomCost",
                5,
                "Number of blue mushrooms required to place a pickable blue mushroom spawner. Set to 0 to disable the ability to plant this resource.");
            mushroomRespawnTime = Config(
                "Mushrooms",
                "MushroomRespawnTime",
                240,
                "Number of minutes it takes for mushrooms to respawn.");
            yellowMushroomRespawnTime = Config(
                "Mushrooms",
                "YellowMushroomRespawnTime",
                240,
                "Number of minutes it takes for yellow mushrooms to respawn.");
            blueMushroomRespawnTime = Config(
                "Mushrooms",
                "BlueMushroomRespawnTime",
                240,
                "Number of minutes it takes for blue mushrooms to respawn.");
            mushroomReturn = Config(
                "Mushrooms",
                "MushroomReturn",
                1,
                "Number of mushrooms a pickable mushroom spawner will spawn.");
            yellowMushroomReturn = Config(
                "Mushrooms",
                "YellowMushroomReturn",
                1,
                "Number of yellow mushrooms a pickable yellow mushroom spawner will spawn.");
            blueMushroomReturn = Config(
                "Mushrooms",
                "BlueMushroomReturn",
                1,
                "Number of blue mushrooms a pickable blue mushroom spawner will spawn.");

            //Flowers
            thistleCost = Config(
                "Flowers",
                "ThistleCost",
                5,
                "Number of thistle required to place a pickable thistle spawner. Set to 0 to disable the ability to plant this resource.");
            dandelionCost = Config(
                "Flowers",
                "DandelionCost",
                5,
                "Number of dandelion required to place a pickable dandelion spawner. Set to 0 to disable the ability to plant this resource.");
            thistleRespawnTime = Config(
                "Flowers",
                "ThistleRespawnTime",
                240,
                "Number of minutes it takes for thistle to respawn.");
            dandelionRespawnTime = Config(
                "Flowers",
                "DandelionRespawnTime",
                240,
                "Number of minutes it takes for dandelion to respawn.");
            thistleReturn = Config(
                "Flowers",
                "ThistleReturn",
                1,
                "Number of thistle a pickable thistle spawner will spawn.");
            dandelionReturn = Config(
                "Flowers",
                "DandelionReturn",
                1,
                "Number of dandelion a pickable dandelion spawner will spawn.");

            //Saplings
            birchMinScale = Config(
                "Saplings",
                "BirchMinScale",
                0.5f,
                "The minimum scaling factor used to scale a birch tree upon growth.");
            birchMaxScale = Config(
                "Saplings",
                "BirchMaxScale",
                1f,
                "The maximum scaling factor used to scale a birch tree upon growth.");
            oakMinScale = Config(
                "Saplings",
                "OakMinScale",
                0.7f,
                "The minimum scaling factor used to scale an oak tree upon growth.");
            oakMaxScale = Config(
                "Saplings",
                "OakMaxScale",
                0.9f,
                "The maximum scaling factor used to scale an oak tree upon growth.");
            ancientMinScale = Config(
                "Saplings",
                "AncientMinScale",
                0.5f,
                "The minimum scaling factor used to scale an ancient tree upon growth.");
            ancientMaxScale = Config(
                "Saplings",
                "AncientMaxScale",
                2f,
                "The maximum scaling factor used to scale an ancient tree upon growth.");
            birchGrowthTime = Config(
                "Saplings",
                "BirchGrowthTime",
                3000f,
                "Number of seconds it takes for a birch tree to grow from a birch sapling (will take at least 10 seconds after planting to grow).");
            oakGrowthTime = Config(
                "Saplings",
                "OakGrowthTime",
                3000f,
                "Number of seconds it takes for an oak tree to grow from an oak sapling (will take at least 10 seconds after planting to grow).");
            ancientGrowthTime = Config(
                "Saplings",
                "AncientGrowthTime",
                3000f,
                "Number of seconds it takes for an ancient tree to grow from an ancient sapling (will take at least 10 seconds after planting to grow).");
            birchGrowRadius = Config(
                "Saplings",
                "BirchGrowRadius",
                2f,
                "Radius of free space required for a birch sapling to grow.");
            oakGrowRadius = Config(
                "Saplings",
                "OakGrowRadius",
                3f,
                "Radius of free space required for an oak sapling to grow.");
            ancientGrowRadius = Config(
                "Saplings",
                "AncientGrowRadius",
                2f,
                "Radius of free space required for an ancient sapling to grow.");
            beechGrowthTime = Config(
                "Saplings",
                "BeechGrowthTime",
                3000f,
                "Number of seconds it takes for a beech tree to grow from a beech sapling (will take at least 10 seconds after planting to grow).");
            pineGrowthTime = Config(
                "Saplings",
                "PineGrowthTime",
                3000f,
                "Number of seconds it takes for a pine tree to grow from a pine sapling (will take at least 10 seconds after planting to grow).");
            firGrowthTime = Config(
                "Saplings",
                "FirGrowthTime",
                3000f,
                "Number of seconds it takes for a fir tree to grow from a fir sapling (will take at least 10 seconds after planting to grow).");
            beechMinScale = Config(
                "Saplings",
                "BeechMinScale",
                0.8f,
                "The minimum scaling factor used to scale a beech tree upon growth.");
            beechMaxScale = Config(
                "Saplings",
                "BeechMaxScale",
                1.5f,
                "The maximum scaling factor used to scale a beech tree upon growth.");
            pineMinScale = Config(
                "Saplings",
                "PineMinScale",
                1.5f,
                "The minimum scaling factor used to scale a pine tree upon growth.");
            pineMaxScale = Config(
                "Saplings",
                "PineMaxScale",
                2.5f,
                "The maximum scaling factor used to scale a pine tree upon growth.");
            firMinScale = Config(
                "Saplings",
                "FirMinScale",
                1f,
                "The minimum scaling factor used to scale a fir tree upon growth.");
            firMaxScale = Config(
                "Saplings",
                "FirMaxScale",
                2.5f,
                "The maximum scaling factor used to scale a fir tree upon growth.");
            beechGrowRadius = Config(
                "Saplings",
                "BeechGrowRadius",
                2f,
                "Radius of free space required for a beech sapling to grow.");
            pineGrowRadius = Config(
                "Saplings",
                "PineGrowRadius",
                2f,
                "Radius of free space required for a pine sapling to grow.");
            firGrowRadius = Config(
                "Saplings",
                "FirGrowRadius",
                2f,
                "Radius of free space required for a fir sapling to grow.");
            yggaMinScale = Config(
                "Saplings",
                "YggaMinScale",
                0.5f,
                "The minimum scaling factor used to scale a ygga tree upon growth.");
            yggaMaxScale = Config(
                "Saplings",
                "YggaMaxScale",
                2f,
                "The minimum scaling factor used to scale a ygga tree upon growth.");
            yggaGrowthTime = Config(
                "Saplings",
                "YggaGrowthTime",
                3000f,
                "Number of seconds it takes for a ygga tree to grow from a ygga sapling (will take at least 10 seconds after planting to grow).");
            yggaGrowRadius = Config(
                "Saplings",
                "YggaGrowRadius",
                2f,
                "Radius of free space required for a ygga sapling to grow.");
            autumnBirchMinScale = Config(
                "Saplings",
                "AutumnBirchMinScale",
                0.5f,
                "The minimum scaling factor used to scale an autumn birch tree upon growth.");
            autumnBirchMaxScale = Config(
                "Saplings",
                "AutumnBirchMaxScale",
                1f,
                "The minimum scaling factor used to scale an autumn birch tree upon growth.");
            autumnBirchGrowthTime = Config(
                "Saplings",
                "AutumnBirchGrowthTime",
                3000f,
                "Number of seconds it takes for an autumn birch tree to grow from an autumn birch sapling (will take at least 10 seconds after planting to grow).");
            autumnBirchGrowRadius = Config(
                "Saplings",
                "AutumnBirchGrowRadius",
                2f,
                "Radius of free space required for an autumn birch sapling to grow.");

            //Seeds
            enableSeedOverrides = Config(
                "Seeds",
                "EnableSeedOverrides",
                false,
                new ConfigDescription("Enables the [Seeds] section of this config.", null, new ConfigurationManagerAttributes { Order = 10 }));
            seedDropMin = Config(
                "Seeds",
                "seedDropMin",
                1,
                new ConfigDescription("Determines minimum amount of seeds that can drop when trees drop seeds.", null, seedSettingAtrributes));
            seedDropMax = Config(
                "Seeds",
                "seedDropMax",
                2,
                new ConfigDescription("Determines maximum amount of seeds that can drop when trees drop seeds.", null, seedSettingAtrributes));
            treeDropMin = Config(
                "Seeds",
                "treeDropMin",
                1,
                new ConfigDescription("Determines minimum amount of times a destroyed tree will attempt to select a drop from its drop table.", null, seedSettingAtrributes));
            treeDropMax = Config(
                "Seeds",
                "treeDropMax",
                3,
                new ConfigDescription("Determines (maximum amount of times - 1) a destroyed tree will attempt to select a drop from its drop table.", null, seedSettingAtrributes));
            dropChance = Config(
                "Seeds",
                "dropChance",
                0.5f,
                new ConfigDescription("Chance that items will drop from trees when destroyed. Default value 0.5f (50%). Set between 0 and 1f.", null, seedSettingAtrributes));
            oneOfEach = Config(
                "Seeds",
                "oneOfEach",
                false,
                new ConfigDescription("When enabled, destroyed trees will not drop the same item from its drop table more than once.", null, seedSettingAtrributes));

            //UI
            enablePickableTimers = Config(
                "UI",
                "EnablePickableTimers",
                true,
                "Enables display of growth time remaining on pickable resources.",
                false);
            enablePlantTimers = Config(
                "UI",
                "EnablePlantTimers",
                true,
                "Enables display of growth time remaining on planted resources, such as crops and saplings.",
                false);
            growthAsPercentage = Config(
                "UI",
                "GrowthAsPercentage",
                false,
                "Enables display of growth time as a percentage instead of time remaining.",
                false);

            //General
            //resourcesSpawnEmpty.SettingChanged += PlantEverything.ConfigurationSettingChanged;
            showPickableSpawners.SettingChanged += PlantEverything.CoreSettingChanged;
            enableMiscFlora.SettingChanged += PlantEverything.CoreSettingChanged;
            snappableVines.SettingChanged += PlantEverything.CoreSettingChanged;
            enableExtraResources.SettingChanged += PlantEverything.ExtraResourcesFileOrSettingChanged;
            disabledResourceNames.SettingChanged += PlantEverything.PieceSettingChanged;

            //Difficulty
            requireCultivation.SettingChanged += PlantEverything.CoreSettingChanged;
            placeAnywhere.SettingChanged += PlantEverything.CoreSettingChanged;
            enforceBiomes.SettingChanged += PlantEverything.CoreSettingChanged;
            //enforceBiomesVanilla.SettingChanged += PlantEverything.ConfigurationSettingChanged;
            recoverResources.SettingChanged += PlantEverything.CoreSettingChanged;
            //resourcesSpawnEmpty.SettingChanged += PlantEverything.ConfigurationSettingChanged;
            enemiesTargetPieces.SettingChanged += PlantEverything.PieceSettingChanged;

            //Berries
            raspberryCost.SettingChanged += PlantEverything.PieceSettingChanged;
            blueberryCost.SettingChanged += PlantEverything.PieceSettingChanged;
            cloudberryCost.SettingChanged += PlantEverything.PieceSettingChanged;
            raspberryRespawnTime.SettingChanged += PlantEverything.PieceSettingChanged;
            blueberryRespawnTime.SettingChanged += PlantEverything.PieceSettingChanged;
            cloudberryRespawnTime.SettingChanged += PlantEverything.PieceSettingChanged;
            raspberryReturn.SettingChanged += PlantEverything.PieceSettingChanged;
            blueberryReturn.SettingChanged += PlantEverything.PieceSettingChanged;
            cloudberryReturn.SettingChanged += PlantEverything.PieceSettingChanged;

            //Crops
            enableCropOverrides.SettingChanged += PlantEverything.CropSettingChanged;
            cropMinScale.SettingChanged += PlantEverything.CropSettingChanged;
            cropMaxScale.SettingChanged += PlantEverything.CropSettingChanged;
            cropGrowTimeMin.SettingChanged += PlantEverything.CropSettingChanged;
            cropGrowTimeMax.SettingChanged += PlantEverything.CropSettingChanged;
            cropGrowRadius.SettingChanged += PlantEverything.CropSettingChanged;
            cropRequireCultivation.SettingChanged += PlantEverything.CropSettingChanged;
            enemiesTargetCrops.SettingChanged += PlantEverything.CropSettingChanged;
            barleyCost.SettingChanged += PlantEverything.CropSettingChanged;
            barleyReturn.SettingChanged += PlantEverything.CropSettingChanged;
            carrotCost.SettingChanged += PlantEverything.CropSettingChanged;
            carrotReturn.SettingChanged += PlantEverything.CropSettingChanged;
            flaxCost.SettingChanged += PlantEverything.CropSettingChanged;
            flaxReturn.SettingChanged += PlantEverything.CropSettingChanged;
            onionCost.SettingChanged += PlantEverything.CropSettingChanged;
            onionReturn.SettingChanged += PlantEverything.CropSettingChanged;
            seedCarrotCost.SettingChanged += PlantEverything.CropSettingChanged;
            seedCarrotReturn.SettingChanged += PlantEverything.CropSettingChanged;
            seedOnionCost.SettingChanged += PlantEverything.CropSettingChanged;
            seedOnionReturn.SettingChanged += PlantEverything.CropSettingChanged;
            seedTurnipCost.SettingChanged += PlantEverything.CropSettingChanged;
            seedTurnipReturn.SettingChanged += PlantEverything.CropSettingChanged;
            turnipCost.SettingChanged += PlantEverything.CropSettingChanged;
            turnipReturn.SettingChanged += PlantEverything.CropSettingChanged;

            //Debris
            pickableBranchCost.SettingChanged += PlantEverything.PieceSettingChanged;
            pickableBranchReturn.SettingChanged += PlantEverything.PieceSettingChanged;
            pickableStoneCost.SettingChanged += PlantEverything.PieceSettingChanged;
            pickableStoneReturn.SettingChanged += PlantEverything.PieceSettingChanged;
            pickableFlintCost.SettingChanged += PlantEverything.PieceSettingChanged;
            pickableFlintReturn.SettingChanged += PlantEverything.PieceSettingChanged;

            //Mushrooms
            mushroomCost.SettingChanged += PlantEverything.PieceSettingChanged;
            yellowMushroomCost.SettingChanged += PlantEverything.PieceSettingChanged;
            blueMushroomCost.SettingChanged += PlantEverything.PieceSettingChanged;
            mushroomRespawnTime.SettingChanged += PlantEverything.PieceSettingChanged;
            yellowMushroomRespawnTime.SettingChanged += PlantEverything.PieceSettingChanged;
            blueMushroomRespawnTime.SettingChanged += PlantEverything.PieceSettingChanged;
            mushroomReturn.SettingChanged += PlantEverything.PieceSettingChanged;
            yellowMushroomReturn.SettingChanged += PlantEverything.PieceSettingChanged;
            blueMushroomReturn.SettingChanged += PlantEverything.PieceSettingChanged;

            //Flowers
            thistleCost.SettingChanged += PlantEverything.PieceSettingChanged;
            dandelionCost.SettingChanged += PlantEverything.PieceSettingChanged;
            thistleRespawnTime.SettingChanged += PlantEverything.PieceSettingChanged;
            dandelionRespawnTime.SettingChanged += PlantEverything.PieceSettingChanged;
            thistleReturn.SettingChanged += PlantEverything.PieceSettingChanged;
            dandelionReturn.SettingChanged += PlantEverything.PieceSettingChanged;

            //Saplings
            ancientGrowRadius.SettingChanged += PlantEverything.SaplingSettingChanged;
            ancientGrowthTime.SettingChanged += PlantEverything.SaplingSettingChanged;
            ancientMinScale.SettingChanged += PlantEverything.SaplingSettingChanged;
            ancientMaxScale.SettingChanged += PlantEverything.SaplingSettingChanged;
            beechGrowRadius.SettingChanged += PlantEverything.SaplingSettingChanged;
            beechGrowthTime.SettingChanged += PlantEverything.SaplingSettingChanged;
            beechMinScale.SettingChanged += PlantEverything.SaplingSettingChanged;
            beechMaxScale.SettingChanged += PlantEverything.SaplingSettingChanged;
            pineGrowRadius.SettingChanged += PlantEverything.SaplingSettingChanged;
            pineGrowthTime.SettingChanged += PlantEverything.SaplingSettingChanged;
            pineMinScale.SettingChanged += PlantEverything.SaplingSettingChanged;
            pineMaxScale.SettingChanged += PlantEverything.SaplingSettingChanged;
            firGrowRadius.SettingChanged += PlantEverything.SaplingSettingChanged;
            firGrowthTime.SettingChanged += PlantEverything.SaplingSettingChanged;
            firMinScale.SettingChanged += PlantEverything.SaplingSettingChanged;
            firMaxScale.SettingChanged += PlantEverything.SaplingSettingChanged;
            birchGrowRadius.SettingChanged += PlantEverything.SaplingSettingChanged;
            birchGrowthTime.SettingChanged += PlantEverything.SaplingSettingChanged;
            birchMinScale.SettingChanged += PlantEverything.SaplingSettingChanged;
            birchMaxScale.SettingChanged += PlantEverything.SaplingSettingChanged;
            oakGrowRadius.SettingChanged += PlantEverything.SaplingSettingChanged;
            oakGrowthTime.SettingChanged += PlantEverything.SaplingSettingChanged;
            oakMinScale.SettingChanged += PlantEverything.SaplingSettingChanged;
            oakMaxScale.SettingChanged += PlantEverything.SaplingSettingChanged;
            yggaGrowRadius.SettingChanged += PlantEverything.SaplingSettingChanged;
            yggaGrowthTime.SettingChanged += PlantEverything.SaplingSettingChanged;
            yggaMinScale.SettingChanged += PlantEverything.SaplingSettingChanged;
            yggaMaxScale.SettingChanged += PlantEverything.SaplingSettingChanged;

            //Seeds
            enableSeedOverrides.SettingChanged += PlantEverything.SeedSettingChanged;
            seedDropMin.SettingChanged += PlantEverything.SeedSettingChanged;
            seedDropMax.SettingChanged += PlantEverything.SeedSettingChanged;
            treeDropMin.SettingChanged += PlantEverything.SeedSettingChanged;
            treeDropMax.SettingChanged += PlantEverything.SeedSettingChanged;
            dropChance.SettingChanged += PlantEverything.SeedSettingChanged;
            oneOfEach.SettingChanged += PlantEverything.SeedSettingChanged;

            //CustomSyncedValue
            extraResources = new(configSync, "PE_ExtraResources", new());
            extraResources.ValueChanged += PlantEverything.ExtraResourcesChanged;

            configSync.AddLockingConfigEntry(serverConfigLocked);

            foreach (ConfigurationManagerAttributes attributes in cropSettingAttributes)
                attributes.Browsable = enableCropOverrides.Value;
            seedSettingAtrributes.Browsable = enableSeedOverrides.Value;

            enableCropOverrides.SettingChanged += (_, _) =>
            {
                foreach (ConfigurationManagerAttributes attributes in cropSettingAttributes)
                    attributes.Browsable = enableCropOverrides.Value;
                ConfigManagerHelper.ReloadConfigDisplay();
            };
            enableSeedOverrides.SettingChanged += (_, _) =>
            {
                seedSettingAtrributes.Browsable = enableSeedOverrides.Value;
                ConfigManagerHelper.ReloadConfigDisplay();
            };
        }

        internal bool EnableDebugMessages
        {
            get { return enableDebugMessages.Value; }
        }
        internal bool ShowPickableSpawners
        {
            get { return showPickableSpawners.Value; }
        }
        internal bool EnableMiscFlora
        {
            get { return enableMiscFlora.Value; }
        }
        internal bool EnableExtraResources
        {
            get { return enableExtraResources.Value; } // Fully integrate this more. It's only checked when adding pieces atm.
        }
        internal bool SnappableVines
        {
            get { return snappableVines.Value; }
        }
        internal bool EnableLocalization
        {
            get { return enableLocalization.Value; }
        }
        internal string Language
        {
            get { return language.Value; }
        }
        internal string[] DisabledResourceNames
        {
            get { return disabledResourceNames.Value.Split(','); }
        }
        internal bool RequireCultivation
        {
            get { return requireCultivation.Value; }
        }
        internal bool PlaceAnywhere
        {
            get { return placeAnywhere.Value; }
        }
        internal bool EnforceBiomes
        {
            get { return enforceBiomes.Value; }
        }
        internal bool EnforceBiomesVanilla
        {
            get { return enforceBiomesVanilla.Value; }
        }
        internal bool RecoverResources
        {
            get { return recoverResources.Value; }
        }
        internal bool ResourcesSpawnEmpty
        {
            get { return resourcesSpawnEmpty.Value; }
        }
        internal bool EnemiesTargetPieces
        {
            get { return enemiesTargetPieces.Value; }
        }
        internal int RaspberryCost
        {
            get { return raspberryCost.Value; }
        }
        internal int BlueberryCost
        {
            get { return blueberryCost.Value; }
        }
        internal int CloudberryCost
        {
            get { return cloudberryCost.Value; }
        }
        internal int RaspberryRespawnTime
        {
            get { return raspberryRespawnTime.Value; }
        }
        internal int BlueberryRespawnTime
        {
            get { return blueberryRespawnTime.Value; }
        }
        internal int CloudberryRespawnTime
        {
            get { return cloudberryRespawnTime.Value; }
        }
        internal int RaspberryReturn
        {
            get { return raspberryReturn.Value; }
        }
        internal int BlueberryReturn
        {
            get { return blueberryReturn.Value; }
        }
        internal int CloudberryReturn
        {
            get { return cloudberryReturn.Value; }
        }
        internal bool EnableCropOverrides
        {
            get { return enableCropOverrides.Value; }
        }
        internal float CropMinScale
        {
            get { return cropMinScale.Value; }
        }
        internal float CropMaxScale
        {
            get { return cropMaxScale.Value; }
        }
        internal float CropGrowTimeMin
        {
            get { return Mathf.Max(cropGrowTimeMin.Value, 10); }
        }
        internal float CropGrowTimeMax
        {
            get { return Mathf.Max(cropGrowTimeMax.Value, 10); }
        }
        internal float CropGrowRadius
        {
            get { return cropGrowRadius.Value; }
        }
        internal bool CropRequireCultivation
        {
            get { return cropRequireCultivation.Value; }
        }
        internal bool CropRequireSunlight
        {
            get { return cropRequireSunlight.Value; }
        }
        internal bool CropRequireGrowthSpace
        {
            get { return cropRequireGrowthSpace.Value; }
        }
        internal bool EnemiesTargetCrops
        {
            get { return enemiesTargetCrops.Value; }
        }
        internal int BarleyCost
        {
            get { return barleyCost.Value; }
        }
        internal int BarleyReturn
        {
            get { return barleyReturn.Value; }
        }
        internal int CarrotCost
        {
            get { return carrotCost.Value; }
        }
        internal int CarrotReturn
        {
            get { return carrotReturn.Value; }
        }
        internal int FlaxCost
        {
            get { return flaxCost.Value; }
        }
        internal int FlaxReturn
        {
            get { return flaxReturn.Value; }
        }
        internal int OnionCost
        {
            get { return onionCost.Value; }
        }
        internal int OnionReturn
        {
            get { return onionReturn.Value; }
        }
        internal int SeedCarrotCost
        {
            get { return seedCarrotCost.Value; }
        }
        internal int SeedCarrotReturn
        {
            get { return seedCarrotReturn.Value; }
        }
        internal int SeedOnionCost
        {
            get { return seedOnionCost.Value; }
        }
        internal int SeedOnionReturn
        {
            get { return seedOnionReturn.Value; }
        }
        internal int SeedTurnipCost
        {
            get { return seedTurnipCost.Value; }
        }
        internal int SeedTurnipReturn
        {
            get { return seedTurnipReturn.Value; }
        }
        internal int TurnipCost
        {
            get { return turnipCost.Value; }
        }
        internal int TurnipReturn
        {
            get { return turnipReturn.Value; }
        }
        internal int MagecapCost
        {
            get { return magecapCost.Value; }
        }
        internal int MagecapReturn
        {
            get { return magecapReturn.Value; }
        }
        internal int JotunPuffsCost
        {
            get { return jotunPuffsCost.Value; }
        }
        internal int JotunPuffsReturn
        {
            get { return jotunPuffsReturn.Value; }
        }
        internal int PickableBranchCost
        {
            get { return pickableBranchCost.Value; }
        }
        internal int PickableBranchReturn
        {
            get { return pickableBranchReturn.Value; }
        }
        internal int PickableStoneCost
        {
            get { return pickableStoneCost.Value; }
        }
        internal int PickableStoneReturn
        {
            get { return pickableStoneReturn.Value; }
        }
        internal int PickableFlintCost
        {
            get { return pickableFlintCost.Value; }
        }
        internal int PickableFlintReturn
        {
            get { return pickableFlintReturn.Value; }
        }
        internal int MushroomCost
        {
            get { return mushroomCost.Value; }
        }
        internal int YellowMushroomCost
        {
            get { return yellowMushroomCost.Value; }
        }
        internal int BlueMushroomCost
        {
            get { return blueMushroomCost.Value; }
        }
        internal int MushroomRespawnTime
        {
            get { return mushroomRespawnTime.Value; }
        }
        internal int YellowMushroomRespawnTime
        {
            get { return yellowMushroomRespawnTime.Value; }
        }
        internal int BlueMushroomRespawnTime
        {
            get { return blueMushroomRespawnTime.Value; }
        }
        internal int MushroomReturn
        {
            get { return mushroomReturn.Value; }
        }
        internal int YellowMushroomReturn
        {
            get { return yellowMushroomReturn.Value; }
        }
        internal int BlueMushroomReturn
        {
            get { return blueMushroomReturn.Value; }
        }
        internal int ThistleCost
        {
            get { return thistleCost.Value; }
        }
        internal int DandelionCost
        {
            get { return dandelionCost.Value; }
        }
        internal int ThistleRespawnTime
        {
            get { return thistleRespawnTime.Value; }
        }
        internal int DandelionRespawnTime
        {
            get { return dandelionRespawnTime.Value; }
        }
        internal int ThistleReturn
        {
            get { return thistleReturn.Value; }
        }
        internal int DandelionReturn
        {
            get { return dandelionReturn.Value; }
        }
        internal float BirchMinScale
        {
            get { return birchMinScale.Value; }
        }
        internal float BirchMaxScale
        {
            get { return birchMaxScale.Value; }
        }
        internal float OakMinScale
        {
            get { return oakMinScale.Value; }
        }
        internal float OakMaxScale
        {
            get { return oakMaxScale.Value; }
        }
        internal float AncientMinScale
        {
            get { return ancientMinScale.Value; }
        }
        internal float AncientMaxScale
        {
            get { return ancientMaxScale.Value; }
        }
        internal float BirchGrowthTime
        {
            get { return Mathf.Max(birchGrowthTime.Value, 10); }
        }
        internal float OakGrowthTime
        {
            get { return Mathf.Max(oakGrowthTime.Value, 10); }
        }
        internal float AncientGrowthTime
        {
            get { return Mathf.Max(ancientGrowthTime.Value, 10); }
        }
        internal float BirchGrowRadius
        {
            get { return birchGrowRadius.Value; }
        }
        internal float OakGrowRadius
        {
            get { return oakGrowRadius.Value; }
        }
        internal float AncientGrowRadius
        {
            get { return ancientGrowRadius.Value; }
        }
        internal float BeechGrowthTime
        {
            get { return Mathf.Max(beechGrowthTime.Value, 10); }
        }
        internal float PineGrowthTime
        {
            get { return Mathf.Max(pineGrowthTime.Value, 10); }
        }
        internal float FirGrowthTime
        {
            get { return Mathf.Max(firGrowthTime.Value, 10); }
        }
        internal float BeechMinScale
        {
            get { return beechMinScale.Value; }
        }
        internal float BeechMaxScale
        {
            get { return beechMaxScale.Value; }
        }
        internal float PineMinScale
        {
            get { return pineMinScale.Value; }
        }
        internal float PineMaxScale
        {
            get { return pineMaxScale.Value; }
        }
        internal float FirMinScale
        {
            get { return firMinScale.Value; }
        }
        internal float FirMaxScale
        {
            get { return firMaxScale.Value; }
        }
        internal float BeechGrowRadius
        {
            get { return beechGrowRadius.Value; }
        }
        internal float PineGrowRadius
        {
            get { return pineGrowRadius.Value; }
        }
        internal float FirGrowRadius
        {
            get { return firGrowRadius.Value; }
        }
        internal float YggaMinScale
        {
            get { return yggaMinScale.Value; }
        }
        internal float YggaMaxScale
        {
            get { return yggaMaxScale.Value; }
        }
        internal float YggaGrowthTime
        {
            get { return yggaGrowthTime.Value; }
        }
        internal float YggaGrowRadius
        {
            get { return yggaGrowRadius.Value; }
        }
        internal float AutumnBirchMinScale
        {
            get { return autumnBirchMinScale.Value; }
        }
        internal float AutumnBirchMaxScale
        {
            get { return autumnBirchMaxScale.Value; }
        }
        internal float AutumnBirchGrowthTime
        {
            get { return autumnBirchGrowthTime.Value; }
        }
        internal float AutumnBirchGrowRadius
        {
            get { return autumnBirchGrowRadius.Value; }
        }
        internal bool EnableSeedOverrides
        {
            get { return enableSeedOverrides.Value; }
        }
        internal int SeedDropMin
        {
            get { return seedDropMin.Value; }
        }
        internal int SeedDropMax
        {
            get { return seedDropMax.Value; }
        }
        internal int TreeDropMin
        {
            get { return treeDropMin.Value; }
        }
        internal int TreeDropMax
        {
            get { return treeDropMax.Value; }
        }
        internal float DropChance
        {
            get { return dropChance.Value; }
        }
        internal bool OneOfEach
        {
            get { return oneOfEach.Value; }
        }
        internal bool EnablePickableTimers
        {
            get { return enablePickableTimers.Value; }
        }
        internal bool EnablePlantTimers
        {
            get { return enablePlantTimers.Value; }
        }
        internal bool GrowthAsPercentage
        {
            get { return growthAsPercentage.Value; }
        }
        internal CustomSyncedValue<List<string>> SyncedExtraResources
        {
            get { return extraResources; }
        }

        internal bool IsSourceOfTruth => ConfigSync.IsSourceOfTruth;
#nullable enable
        internal class ConfigurationManagerAttributes
        {
            public bool? Browsable;
            public string? Category;
            public int? Order;
            public bool? ReadOnly;
        }

        internal class ConfigManagerHelper
        {
            private static Assembly? BepinexConfigManager => AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.GetName().Name == "ConfigurationManager");
            private static Type? ConfigManagerType => BepinexConfigManager?.GetType("ConfigurationManager.ConfigurationManager");
            private static object? ConfigManager => ConfigManagerType == null ? null : BepInEx.Bootstrap.Chainloader.ManagerObject.GetComponent(ConfigManagerType);
            internal static void ReloadConfigDisplay() => ConfigManager?.GetType().GetMethod("BuildSettingList")!.Invoke(ConfigManager, Array.Empty<object>());
        }
#nullable disable
    }
}
