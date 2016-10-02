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
    [ApiVersion(1, 24)]
    public class MirrorWarpback : TerrariaPlugin
    {
        Config config = Config.Read("mirrorwarpback.json");

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

        public class WarpbackData
        {
            private TSPlayer Plr;
            private bool Avail;
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

            public bool Available
            {
                get
                {
                    return Avail;
                }
            }

            public WarpbackData(TSPlayer plr)
            {
                Plr = plr;
                if (Plr.UUID != "")
                {
                    Avail = (db.GetUserData(plr, "Avail") == "1");
                    if (Avail)
                    {
                        X = Convert.ToSingle(db.GetUserData(plr, "X"));
                        Y = Convert.ToSingle(db.GetUserData(plr, "Y"));
                    }
                }
                else
                {
                    TShock.Log.ConsoleError("WARNING: WarbackData initialized before UUID available for " + plr.Name + "!");
                }
            }

            public WarpbackData(TSPlayer plr, float x, float y)
            {
                Plr = plr;
                Set(x, y);
            }

            public void Set(float x, float y)
            {
                Avail = true;
                X = x;
                Y = y;
                if( Plr.UUID != "" )
                    db.SetUserData(Plr, new List<string> { "1", Convert.ToString(X), Convert.ToString(Y) });
            }

            public void Clear()
            {
                Avail = false;
                if( Plr.UUID != "" )
                    db.DelUserData(Plr.UUID);
            }

            public void Teleport(byte effect = 1)
            {
                if (!Avail)
                    return;

                Plr.Teleport(X, Y, effect);
                Avail = false;
                if( Plr.UUID != "" )
                    db.DelUserData(Plr.UUID);
            }
        }

        //public Dictionary<int, WarpbackData> wbplayers = new Dictionary<int, WarpbackData>();
        public bool[] Using = new bool[255];

        public MirrorWarpback(Main game) : base(game)
        {

        }

        public override void Initialize()
        {
            GetDataHandlers.PlayerUpdate += OnPlayerUpdate;
            ServerApi.Hooks.NetGreetPlayer.Register(this, OnGreet);
        }

        protected override void Dispose(bool Disposing)
        {
            if (Disposing)
            {
                GetDataHandlers.PlayerUpdate -= OnPlayerUpdate;
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


            //int uid = TShock.Players[args.Who].User.ID;
            WarpbackData wb = WarpbackData.Get(TShock.Players[args.Who]);

            /*
            if( ! wbplayers.ContainsKey(uid) )
            {
                wbplayers.Add(uid, new WarpbackData(uid) );
            }
            */

            if( wb.Available ) {
                bool haslens = !config.greetRequiresItem;
                if (!haslens)
                {
                    foreach (NetItem thing in TShock.Players[args.Who].PlayerData.inventory)
                    {
                        if (thing.NetId == config.returnItemType)
                            haslens = true;
                    }
                }

                if (haslens)
                {
                    SendInfoMessageIfPresent(TShock.Players[args.Who], config.msgOnGreet );
                }
            }
        }

        private void OnPlayerUpdate(object sender, GetDataHandlers.PlayerUpdateEventArgs args)
        {
            if ((args.Control & 32) == 32)
            {
                if (Using[args.PlayerId])
                    return;

                Using[args.PlayerId] = true;

                //int uid = TShock.Players[args.PlayerId].User.ID;
                Item it = TShock.Players[args.PlayerId].TPlayer.inventory[args.Item];

                if ( (it.type == 50 || it.type == 3124 || it.type == 3199) && (config.returnItemType != 0) ) // Magic Mirror, Cell Phone, Ice Mirror
                {
                    TSPlayer p = TShock.Players[args.PlayerId];
                    WarpbackData wb = WarpbackData.Get(TShock.Players[args.PlayerId]);
                    if (p.HasPermission("mw.warpback"))
                    {
                        bool haslens = false;

                        foreach (NetItem thing in p.TPlayer.inventory)
                        {
                            if (thing.NetId == config.returnItemType)
                                haslens = true;
                        }

                        if (haslens)
                            SendInfoMessageIfPresent(p, config.msgOnMirrorWithLens);
                        else
                            SendInfoMessageIfPresent(p, config.msgOnMirrorNoLens);

                        wb.Set(p.X, p.Y);
                    }
                }
                else if (it.type == config.returnItemType && config.returnItemType != 0)
                {
                    TSPlayer p = TShock.Players[args.PlayerId];
                    WarpbackData wb = WarpbackData.Get(TShock.Players[args.PlayerId]);

                    if (p.HasPermission("mw.warpback"))
                    {
                        if (wb.Available)
                        {
                            SendInfoMessageIfPresent(p, config.msgOnLensSuccess);

                            if (config.returnItemConsume && Main.ServerSideCharacter)
                            {
                                if (p.TPlayer.inventory[args.Item].stack > 1)
                                    p.TPlayer.inventory[args.Item].stack -= 1;
                                else
                                    p.TPlayer.inventory[args.Item].type = 0;

                                NetMessage.SendData((int)PacketTypes.PlayerSlot, number:p.Index, number2:args.Item);
                            }
                            wb.Teleport(config.returnEffect);
                        }
                        else
                        {
                            SendInfoMessageIfPresent(p, config.msgOnLensFailure);
                        }
                    }
                }
                else if ( it.type == config.graveReturnItemType && config.graveReturnItemType != 0 )
                {
                    TSPlayer p = TShock.Players[args.PlayerId];
                    if( p.HasPermission("mw.gravewarp") )
                    {
                        if ( p.TPlayer.lastDeathTime.Year > 1980)
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

                            p.Teleport(p.TPlayer.lastDeathPostion.X, p.TPlayer.lastDeathPostion.Y, config.graveReturnEffect);
                        }
                        else
                        {
                            SendInfoMessageIfPresent(TShock.Players[args.PlayerId], config.msgOnWormholeFailure);
                        }
                    }
                }
            } // fi ((args.Control & 32) == 32)
            else
            {
                Using[args.PlayerId] = false;
            }

        }
    }
}
