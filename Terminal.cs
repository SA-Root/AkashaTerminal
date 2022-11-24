namespace Akasha
{
    sealed class Terminal
    {
        ClientWebSocket ws { get; set; }
        string ServerIPPort = "[::1]:8888";
        string AkashaPrompt = "Akasha>";
        uint UID = 0;
        uint TargetUID = 0;
        string TargetUserName = "";
        string UserName = "";
        string Prompt
        {
            get
            {
                if (UID > 0)
                {
                    if (TargetUID > 0) return $"{UserName}({UID}):{TargetUserName}({TargetUID})>";
                    else return $"{UserName}({UID})>";
                }
                else return AkashaPrompt;
            }
        }
        byte[] buf = new byte[4096];
        CancellationToken CTRecvService;
        CancellationTokenSource? cts;
        public Terminal()
        {
            ws = new ClientWebSocket();
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
                        // UserName = userName;
                        // UID = msgResp.Code;
                        Console.WriteLine($"[INFO]Your New Account is Ready, UID: {msgResp.Code}");
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
            var pwd = new string(cpwd);
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
            if (new string(cpwd) == pwd)
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
                    }
                    else
                    {
                        Console.WriteLine("[ERROR]Wrong UID or Password!");
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
                        Console.WriteLine();
                        Console.WriteLine($"{ChatMsg.FromUserName}({ChatMsg.FromUID})>{ChatMsg.Content}");
                        Console.WriteLine();
                        Console.Write(Prompt);
                    }
                }
            }
        }
        private async Task<int> Connect()
        {
            Console.Write($"{AkashaPrompt}Server Address: ");
            var sip = Console.ReadLine() ?? "";
            try
            {
                await ws.ConnectAsync(new Uri($"ws://{sip}/ws"), CancellationToken.None);
            }
            catch (Exception)
            {
                Console.WriteLine($"[ERROR]Cannot connect to server {sip}");
                return 1;
            }
            Console.WriteLine($"[INFO]Connected to server {sip}");
            return 0;
        }
        public async Task Activate()
        {
            Console.WriteLine("[INFO]Terminal Activated");
            while (await Connect() > 0) ;
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
                    else if (cmd == "exit" || cmd == "quit")
                    {
                        await ws.CloseAsync(WebSocketCloseStatus.NormalClosure,
                            null, CancellationToken.None);
                        cts?.Cancel();
                        break;
                    }
                    else if (cmd.StartsWith("chat "))
                    {
                        var sp = cmd.Split(' ', 2);
                        TargetUID = uint.Parse(sp[1]);

                    }
                    else
                    {
                        Console.WriteLine($"[ERROR]Unrecognized command: {line}");
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
                    await ws.CloseAsync(WebSocketCloseStatus.NormalClosure,
                        null, CancellationToken.None);
                    break;
                }
                else
                {
                    Console.WriteLine($"[ERROR]Unrecognized command: {line}");
                }
            }
        }
    }
    sealed class Launcher
    {
        public static async Task Main(string[] args)
        {
            var a = "123456".GetMD5();
            var b = "Gszroot402".GetMD5();
            var t = new Terminal();
            await t.Activate();
        }
    }
}