using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.BellsAndWhistles;
using StardewValley.Buildings;
using StardewValley.Objects;

using StardewValley.TerrainFeatures;
using System;
using System.Collections.Generic;
using xTile.Dimensions;
using xTile.Tiles;
using System.Linq;
using SprinklerPlus;

namespace SprinklerPlus
{
	public class ModEntry : Mod
	{
		private ModConfig Config;

		public override void Entry(IModHelper helper)
		{
			Config = helper.ReadConfig<ModConfig>();

			helper.Events.GameLoop.GameLaunched += OnGameLaunched;
			helper.Events.GameLoop.DayStarted += OnDayStarted;
		}

		private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
		{
			var gmcm = Helper.ModRegistry.GetApi<IGenericModConfigMenuApi>(
				"spacechase0.GenericModConfigMenu");

			if (gmcm == null)
				return;

			gmcm.Register(
				mod: ModManifest,
				reset: () => Config = new ModConfig(),
				save: () => Helper.WriteConfig(Config)
			);

			gmcm.AddBoolOption(
				mod: ModManifest,
				getValue: () => Config.EnableMod,
				setValue: value => Config.EnableMod = value,
				name: () => "Enable Mod",
				tooltip: () => "Enable or disable sprinkler watering for garden pots."
			);

			gmcm.AddBoolOption(
				mod: ModManifest,
				getValue: () => Config.WaterIndoors,
				setValue: value => Config.WaterIndoors = value,
				name: () => "Water Indoors",
				tooltip: () => "Water garden pots in indoor locations."
			);

			gmcm.AddBoolOption(
				mod: ModManifest,
				getValue: () => Config.WaterPetBowl,
				setValue: value => Config.WaterPetBowl = value,
				name: () => "Water Pet Bowl",
				tooltip: () => "Water Pet bowls."
			);
		}

		private void OnDayStarted(object sender, DayStartedEventArgs e)
		{
			if (Config.EnableMod)
			{
				WaterBySprinkler();
			}
		}
		private void WaterBySprinkler()
		{
			foreach (GameLocation location in Game1.locations)
			{
				WaterPotsInLocation(location);

				if (Config.WaterIndoors && location.IsOutdoors)
				{
					// Include building interiors (like sheds)
					foreach (GameLocation indoor in location.buildings.Select(b => b.GetIndoors()).Where(l => l != null))
					{
						WaterPotsInLocation(indoor!);
					}
				}

				if (Config.WaterPetBowl)
				{
					WaterPetBowl();
				}

			}
		}

		private void WaterPotsInLocation(GameLocation location)
		{
			var sprinklers = location.Objects.Values
				.Where(o => o is StardewValley.Object obj && obj.IsSprinkler())
				.Cast<StardewValley.Object>()
				.ToList();


			foreach (var sprinkler in sprinklers)
			{

				//Monitor.Log($"Sprinkler location: {sprinkler.TileLocation}", LogLevel.Info);

				// Vanilla method — supports pressure nozzle automatically
				List<Vector2> coverage = sprinkler.GetSprinklerTiles();

				foreach (Vector2 tile in coverage)
				{
					if (!location.Objects.TryGetValue(tile, out StardewValley.Object obj))
						continue;

					if (obj is not IndoorPot pot)
						continue;

					// water the pot
					pot.Water();

					//apply fertilizer if the pot has none and the sprinkler has enricher attached and fertilizer in its inventory
					FertilizePot(pot, sprinkler);


					//if (!Config.ShowWaterAnimation)
					//	continue;

					//location.temporarySprites.Add(
					//	new TemporaryAnimatedSprite(
					//		13,
					//		tile * 64f,
					//		Color.White,
					//		4,
					//		false,
					//		100f
					//	)
					//);
				}
			}
		}
		private void FertilizePot(IndoorPot pot, StardewValley.Object sprinkler)
		{
			// Exit if the sprinkler has an enricher attachment
			if (sprinkler.heldObject.Value == null || sprinkler.heldObject.Value.Name != "Enricher")
				return;

			StardewValley.Object enricher = sprinkler.heldObject.Value;
			Chest inventory = (Chest)enricher.heldObject.Value;

			// Exit if the enricher has no fertilizer in its inventory
			if (inventory == null || inventory.Items.Count == 0)
				return;

			Item item = inventory.Items[0];
			var dirt = pot.hoeDirt.Value;

			// Exit if the pot has been fertilized
			if (dirt.fertilizer.Value != null)
				return;

			// Apply fertilizer to the pot
			dirt.fertilizer.Value = item.QualifiedItemId;

			// Reduce 1 fertilizer from the enricher's inventory
			item.Stack--;
		}
		private void WaterPetBowl()
		{		
			foreach (GameLocation location in Game1.locations)
			{
				var sprinklers = location.Objects.Values
					.Where(o => o is StardewValley.Object obj && obj.IsSprinkler())
					.Cast<StardewValley.Object>()
					.ToList();

				foreach (var sprinkler in sprinklers)
				{               
					// Vanilla method — supports pressure nozzle automatically
					List<Vector2> coverage = sprinkler.GetSprinklerTiles();

					foreach (Vector2 tile in coverage)
					{
						Building building = location.getBuildingAt(tile);
						if(building == null)
							continue;	

						if (building is not PetBowl)
							continue;

						PetBowl bowl = (PetBowl)building;

						if (tile.X == bowl.tileX.Value+1 && tile.Y == bowl.tileY.Value )
						{
							bowl.watered.Value = true;
						}
						

					}	
				}		 
			}

		}

	}

	/// <summary>The API which lets other mods add a config UI through Generic Mod Config Menu.</summary>
	public interface IGenericModConfigMenuApi
	{
	   void Register(
			IManifest mod, 
			Action reset, 
			Action save, 
			bool titleScreenOnly = false
		);

		void AddBoolOption(
			IManifest mod, 
			Func<bool> getValue, 
			Action<bool> setValue, 
			Func<string> name, 
			Func<string> tooltip = null, 
			string fieldId = null
		);

	}


}
