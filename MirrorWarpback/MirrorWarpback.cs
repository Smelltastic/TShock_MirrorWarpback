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
        public override Version Version
        {
            get
            {
                return new Version("1.0");
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
                return "Lets you use (not consume) a lens item to return to the spot where you last used a magic mirror, ice mirror, or cell phone. Requires mw.warpback permission.";
            }
        }

        public class WarpbackData
        {
            private int Uid;
            private bool Avail;
            public float X;
            public float Y;
            private DB db = new DB("MirrorWarpback", new String[] { "Avail", "X", "Y" });

            public bool Available
            {
                get
                {
                    return Avail;
                }
            }

            public WarpbackData(int uid)
            {
                Uid = uid;
                Avail = (db.GetUserData(Uid, "Avail") == "1");
                if( Avail )
                {
                    X = Convert.ToSingle(db.GetUserData(uid, "X"));
                    Y = Convert.ToSingle(db.GetUserData(uid, "Y"));
                }
                else
                {
                    X = -1;
                    Y = -1;
                }
            }

            public WarpbackData(int uid, float x, float y)
            {
                Uid = uid;
                Set(x, y);
            }

            public void Set(float x, float y)
            {
                Avail = true;
                X = x;
                Y = y;
                db.SetUserData(Uid, new List<string> { "1", Convert.ToString(X), Convert.ToString(Y) });
            }

            public void Clear()
            {
                Avail = false;
                db.DelUserData(Uid);
            }

            public void Teleport()
            {
                Teleport(TShock.Users.GetUserID(TShock.Users.GetUserByID(Uid).Name));
            }

            public void Teleport( int who )
            {
                if (!Avail)
                    return;

                TShock.Players[who].Teleport(X, Y, 1);
                Avail = false;
                db.DelUserData(Uid);
            }
        }

        public Dictionary<int, WarpbackData> wbplayers = new Dictionary<int, WarpbackData>();
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

        public void OnGreet(GreetPlayerEventArgs args)
        {
            int uid = TShock.Players[args.Who].User.ID;

            if( ! wbplayers.ContainsKey(uid) )
            {
                wbplayers.Add(uid, new WarpbackData(uid) );
            }
            
            if( wbplayers[uid].Available ) {
                bool haslens = false;
                foreach (NetItem thing in TShock.Players[args.Who].PlayerData.inventory)
                {
                    if (thing.NetId == 38)
                        haslens = true;
                }

                if (haslens)
                {
                    TShock.Players[args.Who].SendInfoMessage("Your lens appears to be glowing.");
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

                if( TShock.Players[args.PlayerId].HasPermission("mw.warpback") )
                {
                    int uid = TShock.Players[args.PlayerId].User.ID;

                    Item it = Main.player[args.PlayerId].inventory[args.Item];

                    if (new[] { 50, 3124, 3199 }.Contains(it.type)) // Magic Mirror, Cell Phone, Ice Mirror
                    {
                        bool haslens = false;

                        foreach (NetItem thing in TShock.Players[args.PlayerId].PlayerData.inventory)
                        {
                            if (thing.NetId == 38)
                                haslens = true;
                        }

                        if (haslens)
                        {
                            TShock.Players[args.PlayerId].SendInfoMessage("Your lens seems to lock on your current position as you step into the mirror.");
                        }
                        else
                        {
                            TShock.Players[args.PlayerId].SendInfoMessage("As you step into the mirror you notice a thin trail behind you, but it quickly dissipates.");
                        }

                        if (!wbplayers.ContainsKey(uid))
                        {
                            wbplayers.Add(uid, new WarpbackData(uid, TShock.Players[args.PlayerId].X, TShock.Players[args.PlayerId].Y));
                        }
                        else
                        {
                            wbplayers[uid].Set(TShock.Players[args.PlayerId].X, TShock.Players[args.PlayerId].Y);
                        }
                    }
                    else if (it.type == 38) // Lens
                    {
                        if ( wbplayers[uid].Available )
                        {
                            TShock.Players[args.PlayerId].SendInfoMessage("The lens' glow fades as you step into it.");
                            wbplayers[uid].Teleport(args.PlayerId);
                        }
                        else
                        {
                            TShock.Players[args.PlayerId].SendInfoMessage("You wave the lens about for a bit but nothing seems to happen.");
                        }
                    }
                }
            }
            else
            {
                Using[args.PlayerId] = false;
            }

        }
    }
}
