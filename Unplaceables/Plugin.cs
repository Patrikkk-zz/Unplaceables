using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Terraria;
using TerrariaApi.Server;
using TShockAPI;
using TShockAPI.Hooks;
using System.IO;

namespace Unplaceables
{

	[ApiVersion(1, 19)]
	public class Plugin : TerrariaPlugin
	{
		public override string Name { get { return "Unplaceables"; } }
		public override string Author { get { return "Patrikk"; } }
		public override string Description { get { return "Place unplaceable tiles!"; } }
		public override Version Version { get { return new Version(1, 0); } }

		private static List<int> grasses = new List<int>() { 3, 24, 61, 71, 73, 74, 110, 113, 201 };
		private static Dictionary<int, int> tiles = new Dictionary<int, int>() { { 3, 10 }, { 12, 0 }, { 20, 2 }, { 24, 8 }, { 26, 1 }, { 27, 3 }, { 28, 30 }, { 31, 1 }, { 61, 9 }, { 71, 4 }, { 73, 20 }, { 74, 16 }, { 81, 5 }, { 84, 6 }, { 110, 9 }, { 113, 7 }, { 185, 94 }, { 186, 34 }, { 187, 28 }, { 201, 14 }, { 227, 4 }, { 231, 0 }, { 233, 20 }, { 236, 2 }, { 238, 0 }, { 324, 5 } };
		private static Dictionary<int, DataStorage> storage = new Dictionary<int, DataStorage>();
	   
		//231 is queenbee
		//238 is plantera

		public int WireX;
		public int WireY;


		public Plugin(Main game)
			: base(game)
		{
			base.Order = 1;
		}
		public override void Initialize()
		{
			ServerApi.Hooks.GameInitialize.Register(this, OnInitialize);
			GetDataHandlers.TileEdit += OnTileEdit;
			ServerApi.Hooks.ServerLeave.Register(this, OnLeave);
		}
		protected override void Dispose(bool Disposing)
		{
			if (Disposing)
			{
				ServerApi.Hooks.GameInitialize.Deregister(this, OnInitialize);
				GetDataHandlers.TileEdit -= OnTileEdit;
				ServerApi.Hooks.ServerLeave.Deregister(this, OnLeave);
			}
			base.Dispose(Disposing);
		}
		private void OnLeave(LeaveEventArgs args)
		{
			 if (storage.ContainsKey(args.Who))
				{
					storage.Remove(args.Who);
				}
		}
		private void OnInitialize(EventArgs args)
		{
			Commands.ChatCommands.Add(new Command("unplaceables.place", Place, "place") { AllowServer = false, HelpText="Allows you to place unplaceable tiles!"});
		}

		private void OnTileEdit(object sender, GetDataHandlers.TileEditEventArgs args)
		{
			if (args.Action == GetDataHandlers.EditAction.PlaceWire)
			{
				if (storage.ContainsKey(args.Player.Index))
				{
					WireX = args.X;
					WireY = args.Y;
					WirePlace(args.Player.Index, WireX, WireY);
					args.Handled = true;
				}
			}
		}

		private static void Place(CommandArgs args)
		{
			int index = args.Player.Index;
			if (args.Parameters.Count == 1 && args.Parameters[0] == "help")
			 {
				 args.Player.SendInfoMessage("To place unplaceable tiles, type /place ID style and place the tile with a wrench!");
				 args.Player.SendInfoMessage("Type /place list for a list of unplaceable IDs.");
				 args.Player.SendInfoMessage("To turn off this feature, type /place off.");
				 return;
			 }
			 if (args.Parameters.Count == 1 && args.Parameters[0] == "off") 
			 {
				 if (storage.ContainsKey(index))
				 {
					 storage.Remove(args.Player.Index);
				 }
				 args.Player.SendInfoMessage("No longer placing tile!");
				 return;
			 }
			 if (args.Parameters.Count == 1 && args.Parameters[0] == "list")
			 {
				 List<string> all = new List<string>();
				 foreach (object o in tiles.Keys)
				 {
					 all.Add("" + o); 
				 }
				 args.Player.SendInfoMessage("Tiles: {0}", string.Join(",",all ));
				 return;
			 }
			if (args.Player.Group.HasPermission("unplaceables.auto"))
			{
				 if (args.Parameters.Count > 3 || args.Parameters.Count < 2)
				 {
					 args.Player.SendErrorMessage("Proper syntax: /place <ID> <style> -auto ! Type /place help for more info!");
				   return;
				 }
				if (args.Parameters.Count == 3 && args.Parameters[2] != "-auto")
				{
					args.Player.SendErrorMessage("Proper syntax: /place <ID> <style> -auto ! Type /place help for more info!");
				   return;
				}
			}
			else
			{
				if (args.Parameters.Count != 2)
				{
					args.Player.SendErrorMessage("Proper syntax: /place <ID> <style>! Type /place help for more info!");
					return;
				}
			}

			int x = args.Player.TileX;
			int y = args.Player.TileY + 2;
			int ID = -1;
			int style = -1;
			int maxstyle = -1;
			bool isAuto;

			if (!int.TryParse(args.Parameters[0], out ID))
			{
				args.Player.SendErrorMessage("Invalid unplaceable ID!");
				args.Player.SendErrorMessage("Type /place list, or check provided document for a list of unplaceables!");

				return;
			}
			if (!int.TryParse(args.Parameters[1], out style))
			{
				args.Player.SendErrorMessage("Invalid style!");
				return;
			}
			if (!tiles.ContainsKey(ID))
			{
				args.Player.SendErrorMessage("Tile ID {0} is not an unplaceable tile!", ID);
				args.Player.SendErrorMessage("Type /place list, or check provided document for a list of unplaceables!");
				return;
			}
			style = style - 1;
			if (tiles.TryGetValue(ID, out maxstyle))
			{
				if (style > maxstyle || style < 0)
				{
					args.Player.SendErrorMessage("Style not found! The maximum style value for ID: {0} is  style: {1}!", ID, maxstyle + 1);
					args.Player.SendErrorMessage("Check provided document for a list of unplaceables!");

					return;
				}
			}
			
			if (args.Parameters.Count == 3)
			{
				isAuto = true;
			}
			else
			{	
				isAuto = false;
			}

			if (!storage.ContainsKey(index))
			{
				storage.Add(index, new DataStorage(ID, style, args.Player , isAuto));
			}
			else
			{
				storage[index].isAuto = isAuto;
				storage[index].ID = ID;
				storage[index].style = style;
			}
			args.Player.SendInfoMessage("Placing unplaceable ID: {0}, Style: {1} with wrench", ID, style+1);
		}

		private static void WirePlace(int playerindex, int WireX, int WireY)
		{
			var player = storage[playerindex];
			int ID = storage[playerindex].ID;
			int style = storage[playerindex].style;
			int x = WireX;
			int y = WireY;

			if (TShock.Players[playerindex].CurrentRegion != null && !TShock.Players[playerindex].CurrentRegion.HasPermissionToBuildInRegion(TShock.Players[playerindex]))
			{
				TSPlayer.All.SendTileSquare(x, y);
				player.tsplayer.SendErrorMessage("This region is protected from changes!");
				return;
			}

			if (ID == 233 || ID == 236) //JunglePlants
			{
				int X = 0;
				int Y = 0;
				if (style < 10)
				{
					X = style;
					Y = 0;
				}
				else
				{
					X = style - 9;
					Y = 1;
				}
				WorldGen.PlaceJunglePlant(x, y, (ushort)ID, X, Y);
			}
			else if (ID == 324) //Seachells
			{
				int X = 0;
				int Y = 0;
				if (style < 3)
				{
					X = style;
					Y = 0;
				}
				else
				{
					X = style - 3;
					Y = 1;
				}
				WorldGen.Place1x1(x, y, (ushort)ID, style);
				Main.tile[x, y].frameX = (short)(22 * X);
				Main.tile[x, y].frameY = (short)(22 * Y);
			}
			else if (ID == 31 || ID == 12) //Orb/heart tile ,heart crystal
			{
				WorldGen.Place2x2Style(x + 1, y, (ushort)ID, style);
			}
			else if (ID == 185) //Smallpiles
			{
				int X = 0;
				int Y = 0;
				if (style < 54)
				{
					X = style;
					Y = 0;
				}
				else
				{
					X = style - 54;
					Y = 1;
				}
				WorldGen.PlaceSmallPile(x, y, X, Y);
			}
			else if (ID == 20) //Acorn
			{
				WorldGen.PlaceTile(x, y, ID, true, true);
				Main.tile[x, y].frameX = (short)(style * 18);
				Main.tile[x, y - 1].frameX = (short)(style * 18);
				var tile = Main.tile[x, y];
				
			}
			else if (grasses.Contains(ID)) //Grasseses
			{
				WorldGen.PlaceTile(x, y, ID, true, true);
				Main.tile[WireX, WireY].frameX = (short)(style * 18);
			}
			else if (ID == 84) //Herbs
			{
				WorldGen.Place1x1(x, y, ID, style);
				Main.tile[x, y].frameX = (short)(style * 18);
			}

			else if (ID == 28) //Pots
			{
				/*Random r = new Random();
				List<int> styles = new List<int>() {0, 36, 72};
				int rand = r.Next(styles.Count());
			  for (int i = 0; i < 2; i++) // logic to change between 3 sizes of each style. acting weird
				{
					Main.tile[x + i, y - 1].frameX = (short)((i * 18) + styles[r.Next(0,2)]);
				  //Main.tile[x + i, y - 1].frameY = (short)(0);
					Main.tile[x + i, y].frameX = (short)((i * 18) + styles[r.Next(0, 2)]);
				  //Main.tile[x + i, y].frameY = (short)(18);

				}*/
				WorldGen.PlacePot(x, y, (ushort)ID, style);
				
			}
			else if (ID == 81) //Corals
			{
				WorldGen.PlaceTile(x, y, (ushort)ID);
				Main.tile[x, y].frameX = (short)(style * 26);
			}
			else
			{
				WorldGen.PlaceTile(x, y, ID, true, true, -1, style);
			}

			TSPlayer.All.SendTileSquare(x, y);
			if (Main.tile[x, y].type == ID)
			{
				storage[playerindex].tsplayer.SendSuccessMessage("Successfully placed tile ID: {0} with Style: {1}!", ID , style+1);
				if (!storage[playerindex].isAuto)
				{
				 storage.Remove(playerindex);
				}
				return;
			}
			else
			{
				storage[playerindex].tsplayer.SendErrorMessage("Failed to place tile ID: {0} with Style: {1}!", ID, style+1);
				return;
			}
		}

	}
	public class DataStorage
	{
		public TSPlayer tsplayer;
		public int ID;
		public int style;
		public bool isAuto;

		public DataStorage(int _ID, int _style, TSPlayer _tsplayer, bool _isAuto)
		{
			ID = _ID;
			style = _style;
			tsplayer = _tsplayer;
			isAuto = _isAuto;
		}
	}


}