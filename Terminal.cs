namespace Akasha
{
    sealed class Terminal
    {
        ClientWebSocket ws { get; set; }
        public Terminal()
        {
            ws = new ClientWebSocket();
        }
        public async Task Activate()
        {
            Console.WriteLine("Terminal Activated.");
            await ws.ConnectAsync(new Uri("ws://[::1]:8888/ws"), CancellationToken.None);
            byte[] buf = new byte[4096];
            var msgReg = new WSRegister
            {
                UserName = "TestUser",
                SecPassword = "abcde".GetMD5()
            };
            var msgRegJsonBytes = JsonSerializer.SerializeToUtf8Bytes<WSMessage>(msgReg);
            await ws.SendAsync(msgRegJsonBytes, WebSocketMessageType.Binary,
                true, CancellationToken.None);
            await ws.CloseAsync(WebSocketCloseStatus.NormalClosure,
                null, CancellationToken.None);
        }
    }
    sealed class Launcher
    {
        public static async Task Main(string[] args)
        {
            var t = new Terminal();
            await t.Activate();
        }
    }
}