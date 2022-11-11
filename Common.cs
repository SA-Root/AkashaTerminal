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
        public sealed class WSResponse : WSMessage
        {
            public required uint Code { get; set; }
            public string? Msg { get; set; }
        }
        //1
        //Response.Code = New UID
        public sealed class WSRegister : WSMessage
        {
            public required string UserName { get; set; }
            public required byte[] SecPassword { get; set; }
        }
        //2
        //Success: Response.Code == UID
        public sealed class WSLogin : WSMessage
        {
            public required uint UID { get; set; }
            public required byte[] SecPassword { get; set; }
        }
        //3
        public sealed class WSChatRequest : WSMessage
        {
            public required uint FromUID { get; set; }
            public required uint ToUID { get; set; }
        }
        //4
        public sealed class WSChatMessage : WSMessage
        {
            public required string FromUserName { get; set; }
            public required uint FromUID { get; set; }
            public required uint ToUID { get; set; }
            public required string Content { get; set; }
            public required string Timestamp { get; set; }
        }
        public static class Extensions
        {
            public static byte[] GetMD5(this string data) => MD5.HashData(Encoding.UTF8.GetBytes(data));
        }
    }
}