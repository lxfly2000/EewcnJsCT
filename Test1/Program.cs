// See https://aka.ms/new-console-template for more information

using System.Globalization;
using System.Net.WebSockets;
using System.Security;
using System.Speech.Synthesis;
using System.Text;
using Microsoft.ClearScript;
using Microsoft.ClearScript.V8;
using TestLib;

namespace Test1
{
    class Program
    {
        public static int Main(string[] args)
        {
            Console.WriteLine("Test1 started.");
            try
            {
                Console.WriteLine(Environment.OSVersion.Platform);
                TestJS test = new TestJS();
                Console.WriteLine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)+"/EewcnJsCT");
                TestSound ts=new TestSound();
                TestTTS tts = new TestTTS();
                Console.WriteLine(DateTimeOffset.Parse("2020/01/01 12:00:00 +9").ToString());
                Console.ReadLine();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return 1;
            }
            return 0;
        }
    }

    class TestJS
    {
        private ScriptEngine engine;

        public TestJS()
        {
            engine = new V8ScriptEngine();
            engine.Evaluate("function getobj(){return {name:'Zhang',age:1234567890000,yaju:[1,1,4,5,1,4]};}");
            //返回值类型参考：https://clearscript.clearfoundry.net/Reference/html/M_Microsoft_ClearScript_IScriptEngine_Evaluate_2.htm
            ScriptObject r = (ScriptObject)engine.Invoke("getobj");
            Console.WriteLine(r.ToString());
            foreach (var key in r.PropertyNames)
            {
                Console.WriteLine("Key: {0}, Value: {1}, Type: {2}", key, r[key], r[key].GetType().Name);
                if (r[key].GetType().Name == "V8Array")
                {
                    ScriptObject arr = (ScriptObject)r[key];
                    for (int i = 0; i < (int)arr.GetProperty("length"); i++)
                        Console.WriteLine(arr[i]);
                }
            }
            Console.WriteLine(r.GetProperty("sex")is Undefined);
        }
    }

    class TestWS
    {
        private ClientWebSocket wsEEW;
        public TestWS()
        {
            wsEEW = new ClientWebSocket();
            Console.WriteLine("Init state:" + wsEEW.State);
            Connect("ws://localhost:5404");
        }

        public async Task Connect(string url)
        {
            try
            {
                await wsEEW.ConnectAsync(new Uri(url), CancellationToken.None);
                Console.WriteLine("Connected State: {0}", wsEEW.State);
                string hello = "hello world";
                await wsEEW.SendAsync(Encoding.UTF8.GetBytes(hello), WebSocketMessageType.Text, true,
                    CancellationToken.None);
                byte[] buffer = new byte[4096];
                Console.WriteLine("Waiting for receiving...");
                WebSocketReceiveResult r =
                    await wsEEW.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                Console.WriteLine(r.MessageType + ":" + Encoding.UTF8.GetString(buffer));
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Console.WriteLine("State:" + wsEEW.State);
            }
            Console.WriteLine("WebSocket finished.");
        }

        public async Task End()
        {
            string s = "Close";
            await wsEEW.CloseAsync(WebSocketCloseStatus.NormalClosure, s, CancellationToken.None);
            Console.WriteLine("Close State: {0}", wsEEW.State);
        }
    }

    public class JSTest
    {
        public void Run()
        {
            Console.WriteLine("JS Test Started.");
        }
    }

    class TestSound
    {
        private V8ScriptEngine _engine=new V8ScriptEngine();
        public TestSound()
        {
            _engine.AddHostObject("jstest",new JSTest());
            _engine.Evaluate("jstest.Run()");
        }
    }
}