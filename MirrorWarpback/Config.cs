using System.IO;
using Newtonsoft.Json;
using TShockAPI;

namespace MirrorWarpback
{
    public class Config
    {
        public int[] returnItemTypes = { Terraria.ID.ItemID.MagicMirror, Terraria.ID.ItemID.IceMirror, Terraria.ID.ItemID.CellPhone, Terraria.ID.ItemID.RecallPotion };
        public bool returnItemConsume = false;
        public bool returnFromRecallPotion = true;
        public byte returnEffect = 1;
        public bool restrictToSpawnArea = true;
        public string spawnAreaRegion = "";
        public int spawnMaxWarpbackDistanceX = 100;
        public int spawnMaxWarpbackDistanceY = 50;
        public int[] resetItemTypes = { Terraria.ID.ItemID.CopperCoin, Terraria.ID.ItemID.SilverCoin, Terraria.ID.ItemID.GoldCoin, Terraria.ID.ItemID.PlatinumCoin };
        public int[] graveReturnItemTypes = { Terraria.ID.ItemID.WormholePotion };
        public bool graveReturnItemConsume = true;
        public byte graveReturnEffect = 1;
        public string msgOnGreet = "You feel a tugging sensation to somewhere out in the world.";
        public bool greetRequiresItem = true;
        public string msgOnMirrorWithLens = "Return point set!";
        public string msgOnMirrorNoLens = "";
        public string msgOnLensSuccess = "Return point cleared!";
        public string msgOnLensFailure = "";
        public string msgOnReset = "Return point cleared!";
        public string msgOnWormholeSuccess = "";
        public string msgOnWormholeFailure = "";

        public void Write(string filename)
        {
            File.WriteAllText( Path.Combine(TShock.SavePath, filename), JsonConvert.SerializeObject(this, Formatting.Indented));
        }

        public static Config Read(string filename)
        {
            if (!File.Exists( Path.Combine(TShock.SavePath, filename)))
            {
                Config c = new Config();
                c.Write(filename);
                return c;
            }
            return JsonConvert.DeserializeObject<Config>(File.ReadAllText(Path.Combine(TShock.SavePath, filename)));
        }
    }
}
