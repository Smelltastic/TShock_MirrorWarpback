using System.IO;
using Newtonsoft.Json;
using TShockAPI;

namespace MirrorWarpback
{
    public class Config
    {
        public int returnItemType = 38;
        public bool returnItemConsume = false;
        public byte returnEffect = 1;
        public int graveReturnItemType = 2997;
        public bool graveReturnItemConsume = true;
        public byte graveReturnEffect = 1;
        public string msgOnGreet = "Your lens is holding an image of another place.";
        public bool greetRequiresItem = true;
        public string msgOnMirrorWithLens = "Your lens shimmers and holds an image of your location as you step into the mirror.";
        public string msgOnMirrorNoLens = "As you step into the mirror you notice an odd concave refraction behind you.";
        public string msgOnLensSuccess = "The lens' image fades as you step into it.";
        public string msgOnLensFailure = "You wave the lens about for a bit but nothing seems to happen.";
        public string msgOnWormholeSuccess = "";
        public string msgOnWormholeFailure = "";
        /*
        public string AdminChannel = "#admin";
        public string AdminChannelKey = "";
        public string Channel = "#terraria";
        public string ChannelKey = "";
        public string[] ConnectCommands = new string[] { "PRIVMSG NickServ :IDENTIFY password" };
        public string Nick = "TShock";
        public short Port = 6667;
        public string RealName = "TShock";
        public string Server = "localhost";
        public bool SSL = false;
        public string UserName = "TShock";

        public string BotPrefix = ".";
        public string[] IgnoredCommands = new string[] { };
        public string[] IgnoredIRCChatRegexes = new string[] { };
        public string[] IgnoredIRCNicks = new string[] { };
        public string[] IgnoredServerChatRegexes = new string[] { };

        public string IRCActionMessageFormat = "(IRC) * {0} {1}";
        public string IRCChatMessageFormat = "(IRC) {0}<{1}> {2}";
        public string IRCChatModesRequired = "";
        public string IRCJoinMessageFormat = "(IRC) {0} has joined.";
        public string IRCKickMessageFormat = "(IRC) {0} was kicked ({1}).";
        public string IRCLeaveMessageFormat = "(IRC) {0} has left ({1}).";
        public string IRCQuitMessageFormat = "(IRC) {0} has quit ({1}).";
        public string ServerActionMessageFormat = "\u000302\u0002* {0}\u000f {1}";
        public string ServerCommandMessageFormat = "\u000302{0}<{1}>\u000f executed /{2}";
        public string ServerChatMessageFormat = "\u000302{0}<{1}>\u000f {2}{3}";
        public string ServerJoinMessageFormat = "\u000303{0} has joined.";
        public string ServerJoinAdminMessageFormat = "\u000303{0} has joined. IP: {1}";
        public string ServerLeaveMessageFormat = "\u000305{0} has left.";
        public string ServerLeaveAdminMessageFormat = "\u000305{0} has left. IP: {1}";
        public string ServerLoginAdminMessageFormat = "\u000305{0} has logged in as {1}. IP: {2}";
        */

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
