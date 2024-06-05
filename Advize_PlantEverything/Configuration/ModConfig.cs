namespace Advize_PlantEverything;

using BepInEx.Configuration;
using ServerSync;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using static ConfigEventHandlers;

sealed class ModConfig
{
    private readonly ConfigFile ConfigFile;
    private readonly ConfigSync ConfigSync;

    //General 5
    private readonly ConfigEntry<bool> serverConfigLocked;
    internal readonly ConfigEntry<int> nexusID; //local
    private readonly ConfigEntry<bool> enableDebugMessages; //local
    private readonly ConfigEntry<bool> showPickableSpawners; //local
    private readonly ConfigEntry<bool> enableMiscFlora;
    private readonly ConfigEntry<bool> enableDebris;
    private readonly ConfigEntry<bool> enableExtraResources;
    private readonly ConfigEntry<bool> snappableVines; //local
    private readonly ConfigEntry<bool> enableLocalization; //local
    private readonly ConfigEntry<string> language; //local
    private readonly ConfigEntry<string> disabledResourceNames;

    //Difficulty 9
    private readonly ConfigEntry<bool> requireCultivation;
    private readonly ConfigEntry<bool> placeAnywhere;
    private readonly ConfigEntry<bool> enforceBiomes;
    private readonly ConfigEntry<bool> enforceBiomesVanilla;
    private readonly ConfigEntry<bool> plantsRequireShielding;
    private readonly ConfigEntry<bool> canRemoveFlora;
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

    //Crops 31
    private readonly ConfigEntry<bool> enableCropOverrides;
    private readonly ConfigEntry<bool> overrideModdedCrops;
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

    //Debris 9
    private readonly ConfigEntry<int> pickableBranchCost;
    private readonly ConfigEntry<int> pickableBranchReturn;
    private readonly ConfigEntry<int> pickableBranchRespawnTime;
    private readonly ConfigEntry<int> pickableStoneCost;
    private readonly ConfigEntry<int> pickableStoneReturn;
    private readonly ConfigEntry<int> pickableStoneRespawnTime;
    private readonly ConfigEntry<int> pickableFlintCost;
    private readonly ConfigEntry<int> pickableFlintReturn;
    private readonly ConfigEntry<int> pickableFlintRespawnTime;

    //Mushrooms 12
    private readonly ConfigEntry<int> mushroomCost;
    private readonly ConfigEntry<int> yellowMushroomCost;
    private readonly ConfigEntry<int> blueMushroomCost;
    private readonly ConfigEntry<int> smokePuffCost;
    private readonly ConfigEntry<int> mushroomRespawnTime;
    private readonly ConfigEntry<int> yellowMushroomRespawnTime;
    private readonly ConfigEntry<int> blueMushroomRespawnTime;
    private readonly ConfigEntry<int> smokePuffRespawnTime;
    private readonly ConfigEntry<int> mushroomReturn;
    private readonly ConfigEntry<int> yellowMushroomReturn;
    private readonly ConfigEntry<int> blueMushroomReturn;
    private readonly ConfigEntry<int> smokePuffReturn;

    //Flowers 9
    private readonly ConfigEntry<int> thistleCost;
    private readonly ConfigEntry<int> dandelionCost;
    private readonly ConfigEntry<int> fiddleheadCost;
    private readonly ConfigEntry<int> thistleRespawnTime;
    private readonly ConfigEntry<int> dandelionRespawnTime;
    private readonly ConfigEntry<int> fiddleheadRespawnTime;
    private readonly ConfigEntry<int> thistleReturn;
    private readonly ConfigEntry<int> dandelionReturn;
    private readonly ConfigEntry<int> fiddleheadReturn;

    //Saplings 36
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
    private readonly ConfigEntry<float> ashwoodMinScale;
    private readonly ConfigEntry<float> ashwoodMaxScale;
    private readonly ConfigEntry<float> ashwoodGrowthTime;
    private readonly ConfigEntry<float> ashwoodGrowRadius;

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

    //Vines 7
    private readonly ConfigEntry<bool> enableVineOverrides;
    private readonly ConfigEntry<bool> enableCustomVinePiece;
    private readonly ConfigEntry<AshVineStyle> ashVineStyle; //local
    private readonly ConfigEntry<VineBerryStyle> vineBerryStyle; //local
    private readonly ConfigEntry<float> vineAttachDistance;
    private readonly ConfigEntry<float> vineGrowRadius;
    private readonly ConfigEntry<float> vineGrowthTime;
    private readonly ConfigEntry<int> vineBerryRespawnTime;
    private readonly ConfigEntry<int> vineBerryReturn;

    private readonly ConfigEntry<Color> ashVineCustomColor; //local
    private readonly ConfigEntry<Color> leftBerryColor; //local
    private readonly ConfigEntry<Color> centerBerryColor; //local
    private readonly ConfigEntry<Color> rightBerryColor; //local

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
    private readonly List<ConfigurationManagerAttributes> cropSettingAttributes = [new(), new() { Order = 27 }];

    internal ModConfig(ConfigFile configFile, ConfigSync configSync)
    {
        ConfigFile = configFile; ConfigSync = configSync;
        configFile.SaveOnConfigSet = false;

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
            new ConfigDescription("Nexus mod ID for updates.", null, new ConfigurationManagerAttributes { Category = "Internal", ReadOnly = true }),
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
        enableDebris = Config(
            "General",
            "EnableDebris",
            true,
            "Allows for the placement of debris such as branches, flint, and stone.");
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
            "Restrict modded plantables (pickables and saplings) to being placed in their respective biome.");
        enforceBiomesVanilla = Config(
            "Difficulty",
            "EnforceBiomesVanilla",
            true,
            "Restrict vanilla plantables (crops and saplings) to being placed in their respective biome.");
        plantsRequireShielding = Config(
            "Difficulty",
            "PlantsRequireShielding",
            true,
            "Controls whether plants need shielding to survive in hostile environments.");
        canRemoveFlora = Config(
            "Difficulty",
            "CanRemoveFlora",
            false,
            "Allows the cultivator to deconstruct decorative flora placed by a player. Useful for creative game types.");
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
            new ConfigDescription("Enables the [Crops] section of this config.", null, new ConfigurationManagerAttributes { Order = 28 }));
        overrideModdedCrops = Config(
            "Crops",
            "OverrideModdedCrops",
            true,
            new ConfigDescription("Applies all [Crops] settings to 3rd party modded crops.", null, cropSettingAttributes[1]));
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
            new ConfigDescription("Crops can only grow under an open sky.", null, cropSettingAttributes[1]));
        cropRequireGrowthSpace = Config(
            "Crops",
            "CropsRequireGrowthSpace",
            true,
            new ConfigDescription("Crops require space to grow. This setting overrides the CropGrowRadius setting but without altering it, allowing grid spacing mods to continue functioning.", null, cropSettingAttributes[1]));
        enemiesTargetCrops = Config(
            "Crops",
            "EnemiesTargetCrops",
            true,
            new ConfigDescription("Determines whether enemies will target and attack crops. If this setting is changed, pre-existing placed crops will not be affected until the world/server is reloaded.", null, cropSettingAttributes[1]));
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
        pickableBranchRespawnTime = Config(
            "Debris",
            "PickableBranchRespawnTime",
            240,
            "Number of minutes it takes for a pickable branch to respawn.");
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
        pickableStoneRespawnTime = Config(
            "Debris",
            "PickableStoneRespawnTime",
            0,
            "Number of minutes it takes for a pickable Stone to respawn.");
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
        pickableFlintRespawnTime = Config(
            "Debris",
            "PickableFlintRespawnTime",
            240,
            "Number of minutes it takes for pickable flint to respawn.");

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
        smokePuffCost = Config(
            "Mushrooms",
            "SmokepuffCost",
            5,
            "Number of smoke puffs required to place a pickable smoke puff spawner. Set to 0 to disable the ability to plant this resource.");
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
        smokePuffRespawnTime = Config(
            "Mushrooms",
            "SmokepuffRespawnTime",
            240,
            "Number of minutes it takes for smoke puffs to respawn.");
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
        smokePuffReturn = Config(
            "Mushrooms",
            "SmokepuffReturn",
            1,
            "Number of smoke puffs a pickable smoke puff spawner will spawn.");

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
        fiddleheadCost = Config(
            "Flowers",
            "FiddleheadCost",
            15,
            "Number of fiddlehead required to place a pickable fiddlehead spawner. Set to 0 to disable the ability to plant this resource.");
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
        fiddleheadRespawnTime = Config(
            "Flowers",
            "FiddleheadRespawnTime",
            300,
            "Number of minutes it takes for fiddlehead to respawn.");
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
        fiddleheadReturn = Config(
            "Flowers",
            "FiddleheadReturn",
            3,
            "Number of fiddlehead a pickable fiddlehead spawner will spawn.");

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
        ashwoodMinScale = Config(
            "Saplings",
            "AshwoodMinScale",
            0.5f,
            "The minimum scaling factor used to scale an ashwood tree upon growth.");
        ashwoodMaxScale = Config(
            "Saplings",
            "AshwoodMaxScale",
            2f,
            "The minimum scaling factor used to scale an ashwood tree upon growth.");
        ashwoodGrowthTime = Config(
            "Saplings",
            "AshwoodGrowthTime",
            3000f,
            "Number of seconds it takes for an ashwood tree to grow from an ashwood sapling (will take at least 10 seconds after planting to grow).");
        ashwoodGrowRadius = Config(
            "Saplings",
            "AshwoodGrowRadius",
            2f,
            "Radius of free space required for an ashwood sapling to grow.");

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

        //Vines
        enableVineOverrides = Config(
            "Vines",
            "EnableVineOverrides",
            false,
            new ConfigDescription("Enables/Disables the [Vines] section of this config with the exception of color related settings.", null, new ConfigurationManagerAttributes { Order = 12 }));
        enableCustomVinePiece = Config(
            "Vines",
            "EnableCustomVinePiece",
            true,
            new ConfigDescription("Adds/Removes the color customizable vine piece from the cultivator.", null, new ConfigurationManagerAttributes { Order = 11 }));
        ashVineStyle = Config(
            "Vines",
            "AshVineStyle",
            AshVineStyle.MeadowsGreen,
            new ConfigDescription("Defines how the color customizable vines will appear for you, and also what colors are saved on the vines when a sapling is placed. Select custom to display the colors selected at the time of placement.", null, new ConfigurationManagerAttributes { Order = 5 }),
            false);
        vineBerryStyle = Config(
            "Vines",
            "VineBerryStyle",
            VineBerryStyle.VanillaGreen,
            new ConfigDescription("Defines how the color customizable vine berries will appear for you, and also what colors are saved on the vines when a sapling is placed. Select custom to display the colors selected at the time of placement.", null, new ConfigurationManagerAttributes { Order = 3 }),
            false);
        vineAttachDistance = Config(
            "Vines",
            "VineAttachDistance",
            1.8f,
            "Distance at which vine saplings can attach to walls.");
        vineGrowRadius = Config(
            "Vines",
            "VineGrowRadius",
            1.8f,
            "Distance from existing wall-attached vines required for an ashvine sapling to mature and attach to a wall.");
        vineGrowthTime = Config(
            "Vines",
            "VineGrowthTime",
            200f,
            "Length of time (in seconds) that it takes for a vine ash sapling to mature.");
        vineBerryRespawnTime = Config(
            "Vines",
            "VineBerryRespawnTime",
            200,
            "Length of time (in minutes) that it takes for vine berries to regrow");
        vineBerryReturn = Config(
            "Vines",
            "VineBerryReturn",
            3,
            "Resources gained upon harvesting vine berries.");
        ashVineCustomColor = Config(
            "Vines",
            "AshVineCustomColor",
            new Color(0.867f, 0, 0.278f, 1),
            new ConfigDescription("The customizable color for the leaf portion of color customizable vines.", null, new ConfigurationManagerAttributes { Order = 4 }),
            false);
        leftBerryColor = Config(
            "Vines",
            "LeftBerryColor",
            new Color(1, 1, 1, 1),
            new ConfigDescription("The customizable color for the left-most vine berry cluster on color customizable vines.", null, new ConfigurationManagerAttributes { Order = 2 }),
            false);
        centerBerryColor = Config(
            "Vines",
            "CenterBerryColor",
            new Color(1, 1, 1, 1),
            new ConfigDescription("The customizable color for the center vine berry cluster on color customizable vines.", null, new ConfigurationManagerAttributes { Order = 1 }),
            false);
        rightBerryColor = Config(
            "Vines",
            "RightBerryColor",
            new Color(1, 1, 1, 1),
            new ConfigDescription("The customizable color for the right-most vine berry cluster on color customizable vines.", null, new ConfigurationManagerAttributes { Order = 0 }),
            false);

        configFile.Save();
        configFile.SaveOnConfigSet = true;

        //General
        showPickableSpawners.SettingChanged += CoreSettingChanged;
        enableMiscFlora.SettingChanged += PieceSettingChanged;
        enableDebris.SettingChanged += PieceSettingChanged;
        snappableVines.SettingChanged += CoreSettingChanged;
        enableExtraResources.SettingChanged += ExtraResourcesFileOrSettingChanged;
        disabledResourceNames.SettingChanged += CoreSettingChanged;

        //Difficulty
        requireCultivation.SettingChanged += CoreSettingChanged;
        placeAnywhere.SettingChanged += CoreSettingChanged;
        enforceBiomes.SettingChanged += CoreSettingChanged;
        enforceBiomesVanilla.SettingChanged += CoreSettingChanged;
        plantsRequireShielding.SettingChanged += CoreSettingChanged;
        recoverResources.SettingChanged += CoreSettingChanged;
        //resourcesSpawnEmpty.SettingChanged += PlantEverything.ConfigurationSettingChanged;
        enemiesTargetPieces.SettingChanged += PieceSettingChanged;

        //Berries
        raspberryCost.SettingChanged += PieceSettingChanged;
        blueberryCost.SettingChanged += PieceSettingChanged;
        cloudberryCost.SettingChanged += PieceSettingChanged;
        raspberryRespawnTime.SettingChanged += PieceSettingChanged;
        blueberryRespawnTime.SettingChanged += PieceSettingChanged;
        cloudberryRespawnTime.SettingChanged += PieceSettingChanged;
        raspberryReturn.SettingChanged += PieceSettingChanged;
        blueberryReturn.SettingChanged += PieceSettingChanged;
        cloudberryReturn.SettingChanged += PieceSettingChanged;

        //Crops
        enableCropOverrides.SettingChanged += CropSettingChanged;
        overrideModdedCrops.SettingChanged += CropSettingChanged;
        cropMinScale.SettingChanged += CropSettingChanged;
        cropMaxScale.SettingChanged += CropSettingChanged;
        cropGrowTimeMin.SettingChanged += CropSettingChanged;
        cropGrowTimeMax.SettingChanged += CropSettingChanged;
        cropGrowRadius.SettingChanged += CropSettingChanged;
        cropRequireCultivation.SettingChanged += CropSettingChanged;
        enemiesTargetCrops.SettingChanged += CropSettingChanged;
        barleyCost.SettingChanged += CropSettingChanged;
        barleyReturn.SettingChanged += CropSettingChanged;
        carrotCost.SettingChanged += CropSettingChanged;
        carrotReturn.SettingChanged += CropSettingChanged;
        flaxCost.SettingChanged += CropSettingChanged;
        flaxReturn.SettingChanged += CropSettingChanged;
        onionCost.SettingChanged += CropSettingChanged;
        onionReturn.SettingChanged += CropSettingChanged;
        seedCarrotCost.SettingChanged += CropSettingChanged;
        seedCarrotReturn.SettingChanged += CropSettingChanged;
        seedOnionCost.SettingChanged += CropSettingChanged;
        seedOnionReturn.SettingChanged += CropSettingChanged;
        seedTurnipCost.SettingChanged += CropSettingChanged;
        seedTurnipReturn.SettingChanged += CropSettingChanged;
        turnipCost.SettingChanged += CropSettingChanged;
        turnipReturn.SettingChanged += CropSettingChanged;

        //Debris
        pickableBranchCost.SettingChanged += PieceSettingChanged;
        pickableBranchReturn.SettingChanged += PieceSettingChanged;
        pickableBranchRespawnTime.SettingChanged += PieceSettingChanged;
        pickableStoneCost.SettingChanged += PieceSettingChanged;
        pickableStoneReturn.SettingChanged += PieceSettingChanged;
        pickableStoneRespawnTime.SettingChanged += PieceSettingChanged;
        pickableFlintCost.SettingChanged += PieceSettingChanged;
        pickableFlintReturn.SettingChanged += PieceSettingChanged;
        pickableFlintRespawnTime.SettingChanged += PieceSettingChanged;

        //Mushrooms
        mushroomCost.SettingChanged += PieceSettingChanged;
        yellowMushroomCost.SettingChanged += PieceSettingChanged;
        blueMushroomCost.SettingChanged += PieceSettingChanged;
        smokePuffCost.SettingChanged += PieceSettingChanged;
        mushroomRespawnTime.SettingChanged += PieceSettingChanged;
        yellowMushroomRespawnTime.SettingChanged += PieceSettingChanged;
        blueMushroomRespawnTime.SettingChanged += PieceSettingChanged;
        smokePuffRespawnTime.SettingChanged += PieceSettingChanged;
        mushroomReturn.SettingChanged += PieceSettingChanged;
        yellowMushroomReturn.SettingChanged += PieceSettingChanged;
        blueMushroomReturn.SettingChanged += PieceSettingChanged;
        smokePuffReturn.SettingChanged += PieceSettingChanged;

        //Flowers
        thistleCost.SettingChanged += PieceSettingChanged;
        dandelionCost.SettingChanged += PieceSettingChanged;
        fiddleheadCost.SettingChanged += PieceSettingChanged;
        thistleRespawnTime.SettingChanged += PieceSettingChanged;
        dandelionRespawnTime.SettingChanged += PieceSettingChanged;
        fiddleheadRespawnTime.SettingChanged += PieceSettingChanged;
        thistleReturn.SettingChanged += PieceSettingChanged;
        dandelionReturn.SettingChanged += PieceSettingChanged;
        fiddleheadReturn.SettingChanged += PieceSettingChanged;

        //Saplings
        ancientGrowRadius.SettingChanged += SaplingSettingChanged;
        ancientGrowthTime.SettingChanged += SaplingSettingChanged;
        ancientMinScale.SettingChanged += SaplingSettingChanged;
        ancientMaxScale.SettingChanged += SaplingSettingChanged;
        beechGrowRadius.SettingChanged += SaplingSettingChanged;
        beechGrowthTime.SettingChanged += SaplingSettingChanged;
        beechMinScale.SettingChanged += SaplingSettingChanged;
        beechMaxScale.SettingChanged += SaplingSettingChanged;
        pineGrowRadius.SettingChanged += SaplingSettingChanged;
        pineGrowthTime.SettingChanged += SaplingSettingChanged;
        pineMinScale.SettingChanged += SaplingSettingChanged;
        pineMaxScale.SettingChanged += SaplingSettingChanged;
        firGrowRadius.SettingChanged += SaplingSettingChanged;
        firGrowthTime.SettingChanged += SaplingSettingChanged;
        firMinScale.SettingChanged += SaplingSettingChanged;
        firMaxScale.SettingChanged += SaplingSettingChanged;
        birchGrowRadius.SettingChanged += SaplingSettingChanged;
        birchGrowthTime.SettingChanged += SaplingSettingChanged;
        birchMinScale.SettingChanged += SaplingSettingChanged;
        birchMaxScale.SettingChanged += SaplingSettingChanged;
        oakGrowRadius.SettingChanged += SaplingSettingChanged;
        oakGrowthTime.SettingChanged += SaplingSettingChanged;
        oakMinScale.SettingChanged += SaplingSettingChanged;
        oakMaxScale.SettingChanged += SaplingSettingChanged;
        yggaGrowRadius.SettingChanged += SaplingSettingChanged;
        yggaGrowthTime.SettingChanged += SaplingSettingChanged;
        yggaMinScale.SettingChanged += SaplingSettingChanged;
        yggaMaxScale.SettingChanged += SaplingSettingChanged;
        autumnBirchGrowRadius.SettingChanged += SaplingSettingChanged;
        autumnBirchGrowthTime.SettingChanged += SaplingSettingChanged;
        autumnBirchMinScale.SettingChanged += SaplingSettingChanged;
        autumnBirchMaxScale.SettingChanged += SaplingSettingChanged;
        ashwoodGrowRadius.SettingChanged += SaplingSettingChanged;
        ashwoodGrowthTime.SettingChanged += SaplingSettingChanged;
        ashwoodMinScale.SettingChanged += SaplingSettingChanged;
        ashwoodMaxScale.SettingChanged += SaplingSettingChanged;

        //Seeds
        enableSeedOverrides.SettingChanged += SeedSettingChanged;
        seedDropMin.SettingChanged += SeedSettingChanged;
        seedDropMax.SettingChanged += SeedSettingChanged;
        treeDropMin.SettingChanged += SeedSettingChanged;
        treeDropMax.SettingChanged += SeedSettingChanged;
        dropChance.SettingChanged += SeedSettingChanged;
        oneOfEach.SettingChanged += SeedSettingChanged;

        //Vines
        enableVineOverrides.SettingChanged += VineSettingChanged;
        enableCustomVinePiece.SettingChanged += VineSettingChanged;
        ashVineStyle.SettingChanged += VineSettingChanged;
        vineBerryStyle.SettingChanged += VineSettingChanged;

        vineAttachDistance.SettingChanged += VineSettingChanged;
        vineGrowRadius.SettingChanged += VineSettingChanged;
        vineGrowthTime.SettingChanged += VineSettingChanged;
        vineBerryRespawnTime.SettingChanged += VineSettingChanged;
        vineBerryReturn.SettingChanged += VineSettingChanged;

        ashVineCustomColor.SettingChanged += VineSettingChanged;
        leftBerryColor.SettingChanged += VineSettingChanged;
        centerBerryColor.SettingChanged += VineSettingChanged;
        rightBerryColor.SettingChanged += VineSettingChanged;

        //CustomSyncedValue
        extraResources = new(configSync, "PE_ExtraResources", []);
        extraResources.ValueChanged += ExtraResourcesChanged;

        configSync.AddLockingConfigEntry(serverConfigLocked);

        cropSettingAttributes.ForEach(attributes => attributes.Browsable = enableCropOverrides.Value);
        seedSettingAtrributes.Browsable = enableSeedOverrides.Value;

        enableCropOverrides.SettingChanged += (_, _) =>
        {
            cropSettingAttributes.ForEach(attributes => attributes.Browsable = enableCropOverrides.Value);
            ConfigManagerHelper.ReloadConfigDisplay();
        };
        enableSeedOverrides.SettingChanged += (_, _) =>
        {
            seedSettingAtrributes.Browsable = enableSeedOverrides.Value;
            ConfigManagerHelper.ReloadConfigDisplay();
        };
    }

    internal bool EnableDebugMessages => enableDebugMessages.Value;
    internal bool ShowPickableSpawners => showPickableSpawners.Value;
    internal bool EnableMiscFlora => enableMiscFlora.Value;
    internal bool EnableDebris => enableDebris.Value;
    internal bool EnableExtraResources => enableExtraResources.Value;
    internal bool SnappableVines => snappableVines.Value;
    internal bool EnableLocalization => enableLocalization.Value;
    internal string Language => language.Value;
    internal string[] DisabledResourceNames => disabledResourceNames.Value.Split(',');
    internal bool RequireCultivation => requireCultivation.Value;
    internal bool PlaceAnywhere => placeAnywhere.Value;
    internal bool EnforceBiomes => enforceBiomes.Value;
    internal bool EnforceBiomesVanilla => enforceBiomesVanilla.Value;
    internal bool PlantsRequireShielding => plantsRequireShielding.Value;
    internal bool CanRemoveFlora => canRemoveFlora.Value;
    internal bool RecoverResources => recoverResources.Value;
    internal bool ResourcesSpawnEmpty => resourcesSpawnEmpty.Value;
    internal bool EnemiesTargetPieces => enemiesTargetPieces.Value;
    internal int RaspberryCost => raspberryCost.Value;
    internal int BlueberryCost => blueberryCost.Value;
    internal int CloudberryCost => cloudberryCost.Value;
    internal int RaspberryRespawnTime => raspberryRespawnTime.Value;
    internal int BlueberryRespawnTime => blueberryRespawnTime.Value;
    internal int CloudberryRespawnTime => cloudberryRespawnTime.Value;
    internal int RaspberryReturn => raspberryReturn.Value;
    internal int BlueberryReturn => blueberryReturn.Value;
    internal int CloudberryReturn => cloudberryReturn.Value;
    internal bool EnableCropOverrides => enableCropOverrides.Value;
    internal bool OverrideModdedCrops => overrideModdedCrops.Value;
    internal float CropMinScale => cropMinScale.Value;
    internal float CropMaxScale => cropMaxScale.Value;
    internal float CropGrowTimeMin => Mathf.Max(cropGrowTimeMin.Value, 10);
    internal float CropGrowTimeMax => Mathf.Max(cropGrowTimeMax.Value, 10);
    internal float CropGrowRadius => cropGrowRadius.Value;
    internal bool CropRequireCultivation => cropRequireCultivation.Value;
    internal bool CropRequireSunlight => cropRequireSunlight.Value;
    internal bool CropRequireGrowthSpace => cropRequireGrowthSpace.Value;
    internal bool EnemiesTargetCrops => enemiesTargetCrops.Value;
    internal int BarleyCost => barleyCost.Value;
    internal int BarleyReturn => barleyReturn.Value;
    internal int CarrotCost => carrotCost.Value;
    internal int CarrotReturn => carrotReturn.Value;
    internal int FlaxCost => flaxCost.Value;
    internal int FlaxReturn => flaxReturn.Value;
    internal int OnionCost => onionCost.Value;
    internal int OnionReturn => onionReturn.Value;
    internal int SeedCarrotCost => seedCarrotCost.Value;
    internal int SeedCarrotReturn => seedCarrotReturn.Value;
    internal int SeedOnionCost => seedOnionCost.Value;
    internal int SeedOnionReturn => seedOnionReturn.Value;
    internal int SeedTurnipCost => seedTurnipCost.Value;
    internal int SeedTurnipReturn => seedTurnipReturn.Value;
    internal int TurnipCost => turnipCost.Value;
    internal int TurnipReturn => turnipReturn.Value;
    internal int MagecapCost => magecapCost.Value;
    internal int MagecapReturn => magecapReturn.Value;
    internal int JotunPuffsCost => jotunPuffsCost.Value;
    internal int JotunPuffsReturn => jotunPuffsReturn.Value;
    internal int PickableBranchCost => pickableBranchCost.Value;
    internal int PickableBranchReturn => pickableBranchReturn.Value;
    internal int PickableBranchRespawnTime => pickableBranchRespawnTime.Value;
    internal int PickableStoneCost => pickableStoneCost.Value;
    internal int PickableStoneReturn => pickableStoneReturn.Value;
    internal int PickableStoneRespawnTime => pickableStoneRespawnTime.Value;
    internal int PickableFlintCost => pickableFlintCost.Value;
    internal int PickableFlintReturn => pickableFlintReturn.Value;
    internal int PickableFlintRespawnTime => pickableFlintRespawnTime.Value;
    internal int MushroomCost => mushroomCost.Value;
    internal int YellowMushroomCost => yellowMushroomCost.Value;
    internal int BlueMushroomCost => blueMushroomCost.Value;
    internal int SmokePuffCost => smokePuffCost.Value;
    internal int MushroomRespawnTime => mushroomRespawnTime.Value;
    internal int YellowMushroomRespawnTime => yellowMushroomRespawnTime.Value;
    internal int BlueMushroomRespawnTime => blueMushroomRespawnTime.Value;
    internal int SmokePuffRespawnTime => smokePuffRespawnTime.Value;
    internal int MushroomReturn => mushroomReturn.Value;
    internal int YellowMushroomReturn => yellowMushroomReturn.Value;
    internal int BlueMushroomReturn => blueMushroomReturn.Value;
    internal int SmokePuffReturn => smokePuffReturn.Value;
    internal int ThistleCost => thistleCost.Value;
    internal int DandelionCost => dandelionCost.Value;
    internal int FiddleheadCost => fiddleheadCost.Value;
    internal int ThistleRespawnTime => thistleRespawnTime.Value;
    internal int DandelionRespawnTime => dandelionRespawnTime.Value;
    internal int FiddleheadRespawnTime => fiddleheadRespawnTime.Value;
    internal int ThistleReturn => thistleReturn.Value;
    internal int DandelionReturn => dandelionReturn.Value;
    internal int FiddleheadReturn => fiddleheadReturn.Value;
    internal float BirchMinScale => birchMinScale.Value;
    internal float BirchMaxScale => birchMaxScale.Value;
    internal float OakMinScale => oakMinScale.Value;
    internal float OakMaxScale => oakMaxScale.Value;
    internal float AncientMinScale => ancientMinScale.Value;
    internal float AncientMaxScale => ancientMaxScale.Value;
    internal float BirchGrowthTime => Mathf.Max(birchGrowthTime.Value, 10);
    internal float OakGrowthTime => Mathf.Max(oakGrowthTime.Value, 10);
    internal float AncientGrowthTime => Mathf.Max(ancientGrowthTime.Value, 10);
    internal float BirchGrowRadius => birchGrowRadius.Value;
    internal float OakGrowRadius => oakGrowRadius.Value;
    internal float AncientGrowRadius => ancientGrowRadius.Value;
    internal float BeechGrowthTime => Mathf.Max(beechGrowthTime.Value, 10);
    internal float PineGrowthTime => Mathf.Max(pineGrowthTime.Value, 10);
    internal float FirGrowthTime => Mathf.Max(firGrowthTime.Value, 10);
    internal float BeechMinScale => beechMinScale.Value;
    internal float BeechMaxScale => beechMaxScale.Value;
    internal float PineMinScale => pineMinScale.Value;
    internal float PineMaxScale => pineMaxScale.Value;
    internal float FirMinScale => firMinScale.Value;
    internal float FirMaxScale => firMaxScale.Value;
    internal float BeechGrowRadius => beechGrowRadius.Value;
    internal float PineGrowRadius => pineGrowRadius.Value;
    internal float FirGrowRadius => firGrowRadius.Value;
    internal float YggaMinScale => yggaMinScale.Value;
    internal float YggaMaxScale => yggaMaxScale.Value;
    internal float YggaGrowthTime => yggaGrowthTime.Value;
    internal float YggaGrowRadius => yggaGrowRadius.Value;
    internal float AutumnBirchMinScale => autumnBirchMinScale.Value;
    internal float AutumnBirchMaxScale => autumnBirchMaxScale.Value;
    internal float AutumnBirchGrowthTime => autumnBirchGrowthTime.Value;
    internal float AutumnBirchGrowRadius => autumnBirchGrowRadius.Value;
    internal float AshwoodMinScale => ashwoodMinScale.Value;
    internal float AshwoodMaxScale => ashwoodMaxScale.Value;
    internal float AshwoodGrowthTime => ashwoodGrowthTime.Value;
    internal float AshwoodGrowRadius => ashwoodGrowRadius.Value;
    internal bool EnableSeedOverrides => enableSeedOverrides.Value;
    internal int SeedDropMin => seedDropMin.Value;
    internal int SeedDropMax => seedDropMax.Value;
    internal int TreeDropMin => treeDropMin.Value;
    internal int TreeDropMax => treeDropMax.Value;
    internal float DropChance => dropChance.Value;
    internal bool OneOfEach => oneOfEach.Value;
    internal bool EnablePickableTimers => enablePickableTimers.Value;
    internal bool EnablePlantTimers => enablePlantTimers.Value;
    internal bool GrowthAsPercentage => growthAsPercentage.Value;
    internal bool EnableVineOverrides => enableVineOverrides.Value;
    internal bool EnableCustomVinePiece => enableCustomVinePiece.Value;
    internal float VinesAttachDistance => vineAttachDistance.Value;
    internal float VineGrowRadius => vineGrowRadius.Value;
    internal float VinesGrowthTime => vineGrowthTime.Value;
    internal int VineBerryRespawnTime => vineBerryRespawnTime.Value;
    internal int VineBerryReturn => vineBerryReturn.Value;
    internal Color VinesColor => ashVineCustomColor.Value;

    private List<ConfigEntry<Color>> _BerryColors;
    internal List<ConfigEntry<Color>> BerryColors => _BerryColors ??= [rightBerryColor, centerBerryColor, leftBerryColor];
    internal AshVineStyle AshVineStyle => ashVineStyle.Value;
    internal VineBerryStyle VineBerryStyle => vineBerryStyle.Value;
    internal CustomSyncedValue<List<string>> SyncedExtraResources => extraResources;

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
        internal static void ReloadConfigDisplay() => ConfigManager?.GetType().GetMethod("BuildSettingList")!.Invoke(ConfigManager, []);
    }
}

internal enum AshVineStyle { AshlandsRed, MeadowsGreen, Custom }
internal enum VineBerryStyle { VanillaGreen, RedGrapes, Custom }
