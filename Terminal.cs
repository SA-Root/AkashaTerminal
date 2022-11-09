namespace Akasha
{
    sealed class Terminal
    {
        public Terminal()
        {

        }
        public async Task Activate()
        {
            Console.WriteLine("Terminal Activated.");
            using var ws = new ClientWebSocket();
            await ws.ConnectAsync(new Uri("ws://[::1]:8888/ws"), CancellationToken.None);
            byte[] buf = new byte[1056];

            while (ws.State == WebSocketState.Open)
            {
                var result = await ws.ReceiveAsync(buf, CancellationToken.None);

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, null, CancellationToken.None);
                    Console.WriteLine(result.CloseStatusDescription);
                }
                else
                {
                    Console.WriteLine(Encoding.ASCII.GetString(buf, 0, result.Count));
                }
            }
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