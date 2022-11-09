namespace Akasha
{
    namespace Common
    {
        [JsonDerivedType(typeof(WSMessage), typeDiscriminator: "base")]
        [JsonDerivedType(typeof(WSRegister), typeDiscriminator: "MsgRegister")]
        [JsonDerivedType(typeof(WSLogin), typeDiscriminator: "MsgLogin")]
        [JsonDerivedType(typeof(WSResponse), typeDiscriminator: "MsgResponse")]
        [JsonDerivedType(typeof(WSChatRequest), typeDiscriminator: "MsgChatRequest")]
        [JsonDerivedType(typeof(WSChatMessage), typeDiscriminator: "MsgChatMessage")]
        public class WSMessage
        {

        }
        public class WSRegister : WSMessage
        {
            public string? UserName { get; set; }
            public uint UID { get; set; }
            public byte[]? SecPassword { get; set; }
        }
        public class WSLogin : WSMessage
        {
            public uint UID { get; set; }
            public byte[]? SecPassword { get; set; }
        }
        public class WSResponse : WSMessage
        {
            public uint Code { get; set; }
        }
        public class WSChatRequest : WSMessage
        {
            public uint FromUID { get; set; }
            public uint ToUID { get; set; }
        }
        public class WSChatMessage : WSMessage
        {
            public string? FromUserName { get; set; }
            public uint FromUID { get; set; }
            public uint ToUID { get; set; }
            public string? Content { get; set; }
            public string? Timestamp { get; set; }
        }
        public static class Extensions
        {
            public static byte[] GetMD5(this string data) => MD5.HashData(Encoding.UTF8.GetBytes(data));
        }
    }
}