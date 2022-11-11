namespace Akasha
{
    sealed class Terminal
    {
        ClientWebSocket ws { get; set; }
        string ServerIPPort = "[::1]:8888";
        string AkashaPrompt = "Akasha>";
        uint UID = 0;
        uint TargetUID = 0;
        string UserName = "";
        string UserPrompt = "";
        string Prompt
        {
            get
            {
                if (UID > 0) return UserPrompt;
                else return AkashaPrompt;
            }
        }
        byte[] buf = new byte[4096];
        CancellationToken CTRecvService;
        CancellationTokenSource cts;
        public Terminal()
        {
            ws = new ClientWebSocket();
            UserPrompt = $"{UserName}({UID})>";
        }
        async Task SendRegisterMsgAsync(string userName, string pwd)
        {
            var msgReg = new WSRegister
            {
                UserName = userName,
                SecPassword = pwd.GetMD5()
            };
            var msgRegJsonBytes = JsonSerializer.SerializeToUtf8Bytes<WSMessage>(msgReg);
            await ws.SendAsync(msgRegJsonBytes, WebSocketMessageType.Binary,
                true, CancellationToken.None);
            var result = await ws.ReceiveAsync(buf, CancellationToken.None);
            if (result.MessageType == WebSocketMessageType.Binary)
            {
                var msg = JsonSerializer.Deserialize<WSMessage>(
                            new ReadOnlySpan<byte>(buf, 0, result.Count));
                if (msg is WSResponse msgResp)
                {
                    if (msgResp.Code > 100_000)
                    {
                        UserName = userName;
                        UID = msgResp.Code;
                        UserPrompt = $"{UserName}({UID})>";
                        Console.WriteLine("[INFO]Your New Account is Ready");
                    }
                }
            }
        }
        async Task RegisterAsync()
        {
            Console.Write($"{AkashaPrompt}New User Name: ");
            var uname = Console.ReadLine() ?? "";
            Console.Write($"{AkashaPrompt}Password: ");
            char[] cpwd = new char[64];
            uint pos = 0;
            while (true)
            {
                var k = Console.ReadKey(true);
                if (k.Key == ConsoleKey.Enter)
                {
                    Console.Write('\n');
                    break;
                }
                cpwd[pos++] = k.KeyChar;
            }
            var pwd = cpwd.ToString() ?? "";
            Console.Write($"{AkashaPrompt}Confirm Your Password: ");
            pos = 0;
            while (true)
            {
                var k = Console.ReadKey(true);
                if (k.Key == ConsoleKey.Enter)
                {
                    Console.Write('\n');
                    break;
                }
                cpwd[pos++] = k.KeyChar;
            }
            if (cpwd.ToString() == pwd)
            {
                await SendRegisterMsgAsync(uname, pwd);
            }
            else
            {
                Console.WriteLine("[ERROR]Inconsistent password");
            }
        }
        async Task LoginAsync()
        {
            Console.Write($"{AkashaPrompt}UID: ");
            var uid = uint.Parse(Console.ReadLine() ?? "0");
            Console.Write($"{AkashaPrompt}Password: ");
            char[] cpwd = new char[64];
            uint pos = 0;
            while (true)
            {
                var k = Console.ReadKey(true);
                if (k.Key == ConsoleKey.Enter)
                {
                    Console.Write('\n');
                    break;
                }
                cpwd[pos++] = k.KeyChar;
            }
            var pwd = cpwd.ToString() ?? "";

            var msgLogin = new WSLogin
            {
                UID = uid,
                SecPassword = pwd.GetMD5()
            };
            var msgLoginJsonBytes = JsonSerializer.SerializeToUtf8Bytes<WSMessage>(msgLogin);
            await ws.SendAsync(msgLoginJsonBytes, WebSocketMessageType.Binary,
                true, CancellationToken.None);
            var result = await ws.ReceiveAsync(buf, CancellationToken.None);
            if (result.MessageType == WebSocketMessageType.Binary)
            {
                var msg = JsonSerializer.Deserialize<WSMessage>(
                            new ReadOnlySpan<byte>(buf, 0, result.Count));
                if (msg is WSResponse msgResp)
                {
                    if (msgResp.Code == uid)
                    {
                        UserName = msgResp.Msg ?? "NULL";
                        UID = msgResp.Code;
                        UserPrompt = $"{UserName}({UID})>";
                    }
                }
            }
        }
        async Task SendMsgAsync(string str)
        {
            var msg = JsonSerializer.SerializeToUtf8Bytes<WSMessage>(new WSChatMessage
            {
                FromUserName = UserName,
                FromUID = UID,
                ToUID = TargetUID,
                Content = str,
                Timestamp = DateTime.Now.ToString("G")
            });
            await ws.SendAsync(msg, WebSocketMessageType.Binary, true, CancellationToken.None);
        }
        async Task ReceiveAndDisplay(CancellationToken ct)
        {
            while (true)
            {
                var result = await ws.ReceiveAsync(buf, ct);
                if (ct.IsCancellationRequested) break;
                if (result.MessageType == WebSocketMessageType.Binary)
                {
                    var msg = JsonSerializer.Deserialize<WSMessage>(
                                new ReadOnlySpan<byte>(buf, 0, result.Count));
                    if (msg is WSChatMessage ChatMsg)
                    {
                        Console.WriteLine();
                        Console.WriteLine($"{ChatMsg.FromUserName}({ChatMsg.FromUID})>{ChatMsg.Content}");
                        Console.WriteLine();
                        Console.WriteLine(Prompt);
                    }
                }
            }
        }
        public async Task Activate()
        {
            Console.WriteLine("[INFO]Terminal Activated");
            await ws.ConnectAsync(new Uri($"ws://{ServerIPPort}/ws"), CancellationToken.None);
            Console.WriteLine($"[INFO]Connected to server {ServerIPPort}");
            while (true)
            {
                Console.Write(Prompt);
                var line = Console.ReadLine() ?? "";
                var cmd = line.ToLower();
                if (UID > 0)
                {
                    if (TargetUID > 0)
                    {
                        if (cmd == "$exit" || cmd == "$quit")
                        {
                            TargetUID = 0;
                        }
                        else
                        {
                            await SendMsgAsync(line);
                        }
                    }
                    if (cmd == "exit" || cmd == "quit")
                    {
                        cts.Cancel();
                        await ws.CloseAsync(WebSocketCloseStatus.NormalClosure,
                            null, CancellationToken.None);
                        break;
                    }
                    else if (cmd.StartsWith("chat "))
                    {
                        var sp = cmd.Split(' ', 2);
                        TargetUID = uint.Parse(sp[1]);
                    }
                    else
                    {
                        Console.WriteLine("[ERROR]Unrecognized command");
                    }
                }
                else if (cmd == "register")
                {
                    await RegisterAsync();
                }
                else if (cmd == "login")
                {
                    await LoginAsync();
                    cts = new();
                    CTRecvService = cts.Token;
                    Task.Run(() => ReceiveAndDisplay(CTRecvService));
                }
                else if (cmd == "exit" || cmd == "quit")
                {
                    cts.Cancel();
                    await ws.CloseAsync(WebSocketCloseStatus.NormalClosure,
                        null, CancellationToken.None);
                    break;
                }
                else
                {
                    Console.WriteLine("[ERROR]Unrecognized command");
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