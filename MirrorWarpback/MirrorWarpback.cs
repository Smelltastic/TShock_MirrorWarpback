using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using TerrariaApi.Server;
using TShockAPI;

namespace MirrorWarpback
{
    [ApiVersion(1, 25)]
    public class MirrorWarpback : TerrariaPlugin
    {
        public static Config config = Config.Read("mirrorwarpback.json");

        public override Version Version
        {
            get
            {
                return new Version("1.1");
            }
        }

        public override string Name
        {
            get
            {
                return "MirrorWarpback";
            }
        }

        public override string Author
        {
            get
            {
                return "Brian Emmons";
            }
        }

        public override string Description
        {
            get
            {
                return "Lets you use a lens item (configurable) to return to the spot where you last used a magic mirror, ice mirror, or cell phone. Requires mw.warpback permission.";
            }
        }

        public enum WarpbackState
        {
            None,
            WaitingForSpawn,
            Available
        }

        public class WarpbackData
        {
            private TSPlayer Plr;
            //private bool Avail;
            private WarpbackState WarpbackState;
            private float X;
            private float Y;
            private PlayerDB.DB db = new PlayerDB.DB("MirrorWarpback", new String[] { "Avail", "X", "Y" });

            public static WarpbackData Get( TSPlayer plr )
            {
                WarpbackData ret = plr.GetData<WarpbackData>("warpback");
                if( ret == null )
                {
                    ret = new WarpbackData(plr);
                    plr.SetData<WarpbackData>("warpback", ret);
                }
                return ret;
            }

            public WarpbackState State
            {
                get
                {
                    return WarpbackState;
                }
            }

            public WarpbackData(TSPlayer plr)
            {
                Plr = plr;
                if (Plr.UUID != "")
                {
                    plr.SendInfoMessage("(Warpbackdata) Avail: " + db.GetUserData(plr, "Avail") + ", X/Y: " + db.GetUserData(plr, "X") + "," + db.GetUserData(plr, "Y") );
                    if (!Enum.TryParse<WarpbackState>(db.GetUserData(plr, "Avail"), out WarpbackState))
                    {
                        plr.SendInfoMessage("(Warpbackdata) Avail parsing failed!");
                        WarpbackState = WarpbackState.None;
                    }

                    if( WarpbackState != WarpbackState.None )
                    {
                        plr.SendInfoMessage("(Warpbackdata) Loading warpback data...");
                        X = Convert.ToSingle(db.GetUserData(plr, "X"));
                        Y = Convert.ToSingle(db.GetUserData(plr, "Y"));
                    }
                }
                else
                {
                    TShock.Log.ConsoleError("WARNING: WarpbackData initialized before UUID available for " + plr.Name + "!");
                }
            }

            public WarpbackData(TSPlayer plr, float x, float y)
            {
                Plr = plr;
                Set(x, y);
            }

            public void Set(float x, float y)
            {
                WarpbackState = WarpbackState.Available;
                X = x;
                Y = y;
                if( Plr.UUID != "" )
                    db.SetUserData(Plr, new List<string> { WarpbackState.Available.ToString(), Convert.ToString(X), Convert.ToString(Y) });
            }

            public void Clear()
            {
                WarpbackState = WarpbackState.None;
                if( Plr.UUID != "" )
                    db.DelUserData(Plr.UUID);
            }

            public void Teleport(byte effect = 0)
            {
                if ( WarpbackState == WarpbackState.None )
                    return;

                if (effect == 0)
                    effect = config.returnEffect;

                Plr.Teleport(X, Y, effect);
                WarpbackState = WarpbackState.None;
                if( Plr.UUID != "" )
                    db.DelUserData(Plr.UUID);
            }

            public void TeleportOnSpawn()
            {
                if (WarpbackState == WarpbackState.None)
                    return;

                WarpbackState = WarpbackState.WaitingForSpawn;
            }

            public void Spawned()
            {
                if (WarpbackState != WarpbackState.WaitingForSpawn)
                    return;

                Teleport();
            }
        }

        public bool[] Using = new bool[255];

        public MirrorWarpback(Main game) : base(game)
        {

        }

        public override void Initialize()
        {
            config.Write("mirrorwarpback.json");
            GetDataHandlers.PlayerUpdate += OnPlayerUpdate;
            GetDataHandlers.PlayerSpawn += OnPlayerSpawn;
            ServerApi.Hooks.NetGreetPlayer.Register(this, OnGreet);
        }

        protected override void Dispose(bool Disposing)
        {
            if (Disposing)
            {
                GetDataHandlers.PlayerUpdate -= OnPlayerUpdate;
                GetDataHandlers.PlayerSpawn -= OnPlayerSpawn;
                ServerApi.Hooks.NetGreetPlayer.Deregister(this, OnGreet);
            }
            base.Dispose(Disposing);
        }

        private void SendInfoMessageIfPresent( TSPlayer p, string msg )
        {
            if( !(p == null) && !(String.IsNullOrEmpty(msg)) )
            {
                p.SendInfoMessage(msg);
            }
        }

        public void OnGreet(GreetPlayerEventArgs args)
        {
            if (TShock.Players[args.Who].User == null)
            {
                // Player hasn't logged in or has no account.
                return;
            }


            WarpbackData wb = WarpbackData.Get(TShock.Players[args.Who]);

            if( wb.State == WarpbackState.Available ) {
                bool haslens = !config.greetRequiresItem;
                if (!haslens)
                {
                    foreach (NetItem thing in TShock.Players[args.Who].PlayerData.inventory)
                    {
                        if (config.returnItemTypes.Contains(thing.NetId))
                            haslens = true;
                    }
                }

                if (haslens)
                {
                    SendInfoMessageIfPresent(TShock.Players[args.Who], config.msgOnGreet );
                }
            }
        }

        // Credit goes to TeamFluff for the warpback-on-spawn idea, thanks!
        private void OnPlayerSpawn(object sender, GetDataHandlers.SpawnEventArgs args)
        {
            WarpbackData wb = WarpbackData.Get(TShock.Players[args.Player]);

            if( wb.State == WarpbackState.WaitingForSpawn )
            {
                wb.Spawned();
            }
        }

        private void OnPlayerUpdate(object sender, GetDataHandlers.PlayerUpdateEventArgs args)
        {
            if ((args.Control & 32) != 32)
            {
                Using[args.PlayerId] = false;
                return;
            }

            if (Using[args.PlayerId])
                return;

            Using[args.PlayerId] = true;

            TSPlayer p = TShock.Players[args.PlayerId];
            Item it = TShock.Players[args.PlayerId].TPlayer.inventory[args.Item];

            if (p.HasPermission("mw.warpback") && config.returnItemTypes.Count() > 0 )
            {
                WarpbackData wb = WarpbackData.Get(TShock.Players[args.PlayerId]);

                // If you use the return item while warpback is available...
                if (wb.State == WarpbackState.Available && config.returnItemTypes.Contains(it.type) )
                {
                    SendInfoMessageIfPresent(p, config.msgOnLensSuccess);

                    if (config.returnItemConsume && Main.ServerSideCharacter)
                    {
                        if (p.TPlayer.inventory[args.Item].stack > 1)
                            p.TPlayer.inventory[args.Item].stack -= 1;
                        else
                            p.TPlayer.inventory[args.Item].type = 0;

                        NetMessage.SendData((int)PacketTypes.PlayerSlot, number: p.Index, number2: args.Item);
                    }

                    // If an item that warps you to spawn is your return item, set the state to return once you get there instead of immediately.
                    if (new int[] { Terraria.ID.ItemID.MagicMirror, Terraria.ID.ItemID.IceMirror, Terraria.ID.ItemID.CellPhone, Terraria.ID.ItemID.RecallPotion }.Contains(it.type))
                        wb.TeleportOnSpawn();
                    else
                        wb.Teleport();
                }
                // If you use a reset item while warpback is available...
                else if (config.resetItemTypes.Contains(it.type))
                {
                    wb.Clear();
                    SendInfoMessageIfPresent(p, config.msgOnReset);
                }
                // If you use a mirror-type, so long as that type isn't your workback item while it is available...
                else if (it.type == Terraria.ID.ItemID.MagicMirror || it.type == Terraria.ID.ItemID.CellPhone || it.type == Terraria.ID.ItemID.IceMirror || (config.returnFromRecallPotion && it.type == Terraria.ID.ItemID.RecallPotion))
                {
                    bool haslens = false;

                    foreach (NetItem thing in p.TPlayer.inventory)
                    {
                        if (config.returnItemTypes.Contains(thing.NetId))
                            haslens = true;
                    }

                    if (haslens)
                        SendInfoMessageIfPresent(p, config.msgOnMirrorWithLens);
                    else
                        SendInfoMessageIfPresent(p, config.msgOnMirrorNoLens);

                    wb.Set(p.X, p.Y);
                }
                // If you use a return item, but the above conditions were not met...
                else if( config.returnItemTypes.Contains(it.type) )
                {
                    SendInfoMessageIfPresent(p, config.msgOnLensFailure);
                }

            }

            if (config.graveReturnItemTypes.Count() > 0 && p.HasPermission("mw.gravewarp") && config.graveReturnItemTypes.Contains(it.type))
            {
                if (p.TPlayer.lastDeathTime.Year > 1980)
                {
                    SendInfoMessageIfPresent(p, config.msgOnWormholeSuccess);

                    if (config.graveReturnItemConsume && Main.ServerSideCharacter)
                    {
                        if (p.TPlayer.inventory[args.Item].stack > 1)
                            p.TPlayer.inventory[args.Item].stack -= 1;
                        else
                            p.TPlayer.inventory[args.Item].type = 0;

                        NetMessage.SendData((int)PacketTypes.PlayerSlot, number: p.Index, number2: args.Item);
                    }

                    // If an item that warps you to spawn is your grave return item, set the state to return once you get there instead of immediately.
                    // NOTE: If this is done, gravewarping will cost the player any original return spot they might've had!
                    if (new int[] { Terraria.ID.ItemID.MagicMirror, Terraria.ID.ItemID.IceMirror, Terraria.ID.ItemID.CellPhone, Terraria.ID.ItemID.RecallPotion }.Contains(it.type))
                    {
                        WarpbackData wb = WarpbackData.Get(TShock.Players[args.PlayerId]);
                        wb.Set(p.TPlayer.lastDeathPostion.X, p.TPlayer.lastDeathPostion.Y);
                        wb.TeleportOnSpawn();
                    }
                    else
                        p.Teleport(p.TPlayer.lastDeathPostion.X, p.TPlayer.lastDeathPostion.Y, config.graveReturnEffect);
                }
                else
                {
                    SendInfoMessageIfPresent(TShock.Players[args.PlayerId], config.msgOnWormholeFailure);
                }
            }
        }
    }
}
