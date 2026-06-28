using System;
using System.IO;
using System.Net.Http;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ClearScript;
using Microsoft.ClearScript.V8;

namespace EewcnJsCT
{
    public class EewcnJs
    {
        private ScriptEngine _engine = new V8ScriptEngine();
        private JsonDocument settingsJson = null;
        private int eewQueryInterval = 1;//秒
        private int eewQueryCount = 5;
        private int historyQueryInterval = 1;//秒
        private int historyQueryCount = 5;
        private string[] supportedLangTag = { "en", "zh_CN", "zh_TW", "ja" };
        private string userExtra = "";
        private int language = 1;
        public object LastEvalObject { get; private set; } = null;
        public string JavaScriptPath { get; private set; } = "";
        private ClientWebSocket wsEEW = null;//注意C#里WebSocket是一次性的，一旦连接失败、被关闭或主动断开连接就无法再复用，必须创建新的
        private ClientWebSocket wsHistory = null;
        private CancellationTokenSource cancelEEWSource = null;
        private CancellationTokenSource cancelHistorySource = null;
        private HttpClient httpClient = new HttpClient();
        private string wsPingEEW, wsPingHistory;
        private string eewURL="";
        private string historyURL = "";

        public EewcnJs(string wsPingEew, string wsPingHistory)
        {
            this.wsPingEEW = wsPingEew;
            this.wsPingHistory = wsPingHistory;
        }

        public int EvaluateJavaScript(string code, string filePathForRef)
        {
            try
            {
                LastEvalObject = _engine.Evaluate(code);
                return 0;
            }
            catch (ScriptInterruptedException e)
            {
                Logger.GetInstance().error(filePathForRef + " : " + e.ErrorDetails);
            }
            catch (ScriptEngineException e)
            {
                Logger.GetInstance().error(filePathForRef + " : " + e.ErrorDetails);
            }
            catch (Exception e)
            {
                Logger.GetInstance().error(filePathForRef + " : " + e.Message);
            }

            return 1;
        }

        private int GetSettingsInt(string key, int defValue)
        {
            try
            {
                return settingsJson.RootElement.GetProperty(key).GetInt32();
            }
            catch (JsonException e)
            {
                return defValue;
            }
        }

        private string GetSettingsString(string key, string defValue)
        {
            try
            {
                return settingsJson.RootElement.GetProperty(key).GetString() ?? defValue;
            }
            catch (JsonException e)
            {
                return defValue;
            }
        }

        public int Init()
        {
            //Linux和MacOS上的路径获取：
            //https://learn.microsoft.com/zh-cn/dotnet/core/compatibility/core-libraries/8.0/getfolderpath-unix
            string[] jsPaths =
            {
                "./custom.js",
                "~\\AppData\\Roaming\\lxfly2000\\eewcn\\custom.js",
                "~/.local/share/lxfly2000/eewcn/custom.js",
                "~/Library/Application Support/lxfly2000/eewcn/custom.js",
                "/sdcard/Android/data/com.lxfly2000.eewcn/files/custom.js",
            };
            string homePath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            int indexJsPath = -1;
            //查找JS文件位置
            for (int i = 0; i < jsPaths.Length; i++)
            {
                if (jsPaths[i].Substring(0, 1) == "~")
                    jsPaths[i] = homePath + jsPaths[i].Substring(1);
                string code = Util.ReadAllFile(jsPaths[i]);
                if (!string.IsNullOrEmpty(code) && EvaluateJavaScript(code, jsPaths[i]) == 0)
                {
                    indexJsPath = i;
                    JavaScriptPath = jsPaths[i];
                    break;
                }
            }

            if (indexJsPath == -1)
                return 1;
            //成功执行脚本，获取设置项
            string settingsPath = jsPaths[indexJsPath].Replace("custom.js", "settings.json");
            string settingsContent = Util.ReadAllFile(settingsPath);
            if (!string.IsNullOrEmpty(settingsContent))
            {
                try
                {
                    settingsJson = JsonDocument.Parse(settingsContent);
                }
                catch (JsonException e)
                {
                    Logger.GetInstance().warn(GetString("FailedToLoadSettings"));
                }
            }

            if (settingsJson != null)
            {
                eewQueryInterval = GetSettingsInt("eewQueryInterval", eewQueryInterval);
                eewQueryCount = GetSettingsInt("eewQueryCount", eewQueryCount);
                historyQueryInterval = GetSettingsInt("historyQueryInterval", historyQueryInterval);
                historyQueryCount = GetSettingsInt("historyQueryCount", historyQueryCount);
                userExtra = GetSettingsString("userExtra", userExtra);
                language = GetSettingsInt("language", language);
            }
            //添加JS调用的功能
            _engine.AddHostObject("logger", Logger.GetInstance());
            _engine.AddHostObject("tts",JSTTS.GetInstance());
            _engine.AddHostObject("sound",JSSound.GetInstance());
            //向JS发送数据
            try{_engine.Invoke("setLangTag", supportedLangTag[language]);}catch{}
            try{_engine.Invoke("setUserData", userExtra);}catch{}
            //获取URL用来判断是否开启相应功能
            try{eewURL=GetEEWURL();}catch{}
            try{historyURL=GetHistoryURL();}catch{}
            return 0;
        }

        public void End()
        {
            if (wsEEW!=null&&String.Compare(GetEEWMethod(), "websocket", StringComparison.OrdinalIgnoreCase) == 0)
                wsEEW.CloseAsync(WebSocketCloseStatus.NormalClosure, "", cancelEEWSource.Token);
            if(wsHistory!=null&&String.Compare(GetHistoryMethod(),"websocket", StringComparison.OrdinalIgnoreCase) == 0)
                wsHistory.CloseAsync(WebSocketCloseStatus.NormalClosure, "", cancelHistorySource.Token);
        }

        public string GetString(string key)
        {
            return Util.GetString(key, supportedLangTag[language]);
        }

        private long lastEEWQueryMsts = 0;
        private long lastHistoryQueryMsts = 0;

        //dataType:0=EEW 1=History
        public async Task LoopWSReceive(int dataType,string wsUrl)
        {
            try
            {
                while (true)
                {
                    //接收数据
                    byte[] buffer = new byte[16384];
                    WebSocketReceiveResult r;
                    switch (dataType)
                    {
                        case 0:
                        default:
                            r = await wsEEW.ReceiveAsync(buffer, cancelEEWSource.Token);
                            break;
                        case 1:
                            r = await wsHistory.ReceiveAsync(buffer, cancelHistorySource.Token);
                            break;
                    }

                    if (r.MessageType == WebSocketMessageType.Close)
                    {
                        //State会变成Aborted
                        switch (dataType)
                        {
                            case 0:
                            default:
                                await wsEEW.CloseAsync(WebSocketCloseStatus.NormalClosure, "", cancelEEWSource.Token);
                                wsEEW.Dispose();
                                wsEEW = null;
                                Logger.GetInstance().info(GetString("EEWWebSocketReceiveCloseColon") + wsUrl);
                                return;
                            case 1:
                                await wsHistory.CloseAsync(WebSocketCloseStatus.NormalClosure, "",
                                    cancelHistorySource.Token);
                                wsHistory.Dispose();
                                wsHistory = null;
                                Logger.GetInstance().info(GetString("HistoryWebSocketReceiveCloseColon") + wsUrl);
                                return;
                        }
                    }
                    else if (r.MessageType == WebSocketMessageType.Text)
                    {
                        //处理文本数据
                        string response = Encoding.UTF8.GetString(buffer, 0, r.Count);
                        if(!response.StartsWith("{"))
                            continue;
                        //发送这个数据给主程序
                        switch (dataType)
                        {
                            case 0:
                            default:
                                Worker.lastInstance.InvokeReceived(GetEEWOnSuccess(ref response));
                                break;
                            case 1:
                                Worker.lastInstance.InvokeReceived(GetHistoryOnSuccess(ref response));
                                break;
                        }
                    }
                }
            }
            catch (OperationCanceledException e)
            {
                Logger.GetInstance().info(e.Message);
            }
            catch (Exception e)
            {
                Logger.GetInstance().error(e.ToString());
            }
        }

        public async Task RunPeriodic()
        {
            long nowMsts = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            if (eewURL.Length>0&&nowMsts - lastEEWQueryMsts > eewQueryInterval*1000)
            {
                try
                {
                    //需要执行EEW查询
                    string eewMethod = GetEEWMethod();
                    if (String.Compare(eewMethod, "websocket", StringComparison.OrdinalIgnoreCase) == 0)
                    {
                        //使用WebSocket连接
                        //断线重连的实现：https://jocoboy.github.io/Hexo-Blog/2024/08/18/websocket-usage/
                        //先看是不是断线了或者还没连接
                        if (wsEEW == null||wsEEW.State>WebSocketState.Open)
                            wsEEW = new ClientWebSocket();
                        if (wsEEW.State==WebSocketState.None)
                        {
                            Logger.GetInstance().info(GetString("EEWWebSocketConnectingColon") + eewURL);
                            cancelEEWSource = new CancellationTokenSource();
                            await wsEEW.ConnectAsync(new Uri(eewURL), cancelEEWSource.Token);
                            Logger.GetInstance().info(GetString("EEWWebSocketConnectedColon") + eewURL);
                            if (wsEEW.State == WebSocketState.Open)
                            {
                                //正常连接后发送post数据
                                await wsEEW.SendAsync(Encoding.UTF8.GetBytes(GetEEWPostData()),
                                    WebSocketMessageType.Text, true, cancelEEWSource.Token);
                                //开始接收数据
                                LoopWSReceive(0, eewURL);
                            }
                        }

                        //正常连接之后发送、接收数据
                        if (wsEEW.State == WebSocketState.Open)
                        {
                            //发送ping
                            await wsEEW.SendAsync(Encoding.UTF8.GetBytes(wsPingEEW),WebSocketMessageType.Text,true, cancelEEWSource.Token);
                        }
                    }
                    else
                    {
                        //使用HTTP连接
                        ScriptObject headerObj = GetEEWHeader();
                        bool isPost=String.Compare(eewMethod, "post", StringComparison.OrdinalIgnoreCase)==0;
                        HttpRequestMessage req=new HttpRequestMessage(isPost?HttpMethod.Post:HttpMethod.Get,eewURL);
                        foreach (var key in headerObj.PropertyNames)
                            req.Headers.Add(key, headerObj[key].ToString());
                        if (isPost)
                            req.Content = new StringContent(GetEEWPostData());
                        HttpResponseMessage response = httpClient.Send(req);
                        String responseString = await response.Content.ReadAsStringAsync();
                        ScriptObject eewData = GetEEWOnSuccess(ref responseString);
                        //发送这个数据给主程序
                        Worker.lastInstance.InvokeReceived(eewData);
                    }
                }
                catch (Exception e)
                {
                    Logger.GetInstance().error(GetString("EEWExceptionColon")+e.Message);
                    if (e is WebSocketException)
                    {
                        await wsEEW.CloseAsync(WebSocketCloseStatus.NormalClosure,"",cancelEEWSource.Token);
                        wsEEW.Dispose();
                        wsEEW = null;
                    }
                }

                lastEEWQueryMsts = nowMsts;
            }

            if (historyURL.Length>0&&nowMsts - lastHistoryQueryMsts > historyQueryInterval*1000)
            {
                try
                {
                    //需要执行History查询
                    string historyMethod = GetHistoryMethod();
                    if (String.Compare(historyMethod, "websocket", StringComparison.OrdinalIgnoreCase) == 0)
                    {
                        //使用WebSocket连接
                        //断线重连的实现：https://jocoboy.github.io/Hexo-Blog/2024/08/18/websocket-usage/
                        //先看是不是断线了或者还没连接
                        if (wsHistory == null||wsHistory.State>WebSocketState.Open)
                            wsHistory = new ClientWebSocket();
                        if (wsHistory.State==WebSocketState.None)
                        {
                            Logger.GetInstance().info(GetString("HistoryWebSocketConnectingColon") + historyURL);
                            cancelHistorySource = new CancellationTokenSource();
                            await wsHistory.ConnectAsync(new Uri(historyURL), cancelHistorySource.Token);
                            Logger.GetInstance().info(GetString("HistoryWebSocketConnectedColon") + historyURL);
                            if (wsHistory.State == WebSocketState.Open)
                            {
                                //正常连接后发送post数据
                                await wsHistory.SendAsync(Encoding.UTF8.GetBytes(GetHistoryPostData()),
                                    WebSocketMessageType.Text, true, cancelHistorySource.Token);
                                //开始接收数据
                                LoopWSReceive(1, historyURL);
                            }
                        }

                        //正常连接之后发送、接收数据
                        if (wsHistory.State == WebSocketState.Open)
                        {
                            //发送ping
                            await wsHistory.SendAsync(Encoding.UTF8.GetBytes(wsPingHistory),WebSocketMessageType.Text,true, cancelHistorySource.Token);
                        }
                    }
                    else
                    {
                        //使用HTTP连接
                        ScriptObject headerObj = GetHistoryHeader();
                        bool isPost=String.Compare(historyMethod, "post", StringComparison.OrdinalIgnoreCase)==0;
                        HttpRequestMessage req=new HttpRequestMessage(isPost?HttpMethod.Post:HttpMethod.Get,historyURL);
                        foreach (var key in headerObj.PropertyNames)
                            req.Headers.Add(key, headerObj[key].ToString());
                        if (isPost)
                            req.Content = new StringContent(GetHistoryPostData());
                        HttpResponseMessage response = httpClient.Send(req);
                        String responseString = await response.Content.ReadAsStringAsync();
                        ScriptObject historyData = GetHistoryOnSuccess(ref responseString);
                        //发送这个数据给主程序
                        Worker.lastInstance.InvokeReceived(historyData);
                    }
                }
                catch (Exception e)
                {
                    Logger.GetInstance().error(GetString("HistoryExceptionColon")+e.Message);
                    if (e is WebSocketException)
                    {
                        await wsHistory.CloseAsync(WebSocketCloseStatus.NormalClosure,"",cancelHistorySource.Token);
                        wsHistory.Dispose();
                        wsHistory = null;
                    }
                }
                lastHistoryQueryMsts = nowMsts;
            }
        }

        public string GetEEWURL()
        {
            int r = EvaluateJavaScript("eew_url()",JavaScriptPath);
            if(r == 0&&LastEvalObject.GetType()==typeof(string))
                return LastEvalObject.ToString();
            return "";
        }

        public string GetEEWMethod()
        {
            int r = EvaluateJavaScript("eew_method()",JavaScriptPath);
            if (r == 0 && LastEvalObject.GetType() == typeof(string))
                return LastEvalObject.ToString();
            return "";
        }

        public ScriptObject GetEEWHeader()
        {
            return (ScriptObject)_engine.Invoke("eew_header");
        }

        public string GetEEWPostData()
        {
            int r=EvaluateJavaScript("eew_postdata()",JavaScriptPath);
            if(r == 0&&LastEvalObject.GetType()==typeof(string))
                return LastEvalObject.ToString();
            return "";
        }

        /// <summary>
        /// 获取脚本处理结果
        /// </summary>
        /// <param name="response">传入字符串</param>
        /// <returns>当JS返回null时，返回值为null，当JS返回undefined时，返回Undefined类，或者是一个由JSON产生的数据集</returns>
        public ScriptObject GetEEWOnSuccess(ref string response)
        {
            return (ScriptObject)_engine.Invoke("eew_onsuccess", response);
        }

        public void GetEEWOnFail(int errorCode)
        {
            _engine.Invoke("eew_onfail", errorCode);
        }

        public bool IsEEWData(ref string url)
        {
            object r = _engine.Invoke("is_eew_data", url);
            if (!(r is Undefined))
            {
                return (bool)r;
            }

            return false;
        }

        public void GetEEWOnReport(ref string data)
        {
            _engine.Invoke("eew_onreport", data);
        }
        
        public string GetHistoryURL()
        {
            int r = EvaluateJavaScript("history_url()",JavaScriptPath);
            if(r == 0&&LastEvalObject.GetType()==typeof(string))
                return LastEvalObject.ToString();
            return "";
        }

        public string GetHistoryMethod()
        {
            int r = EvaluateJavaScript("history_method()",JavaScriptPath);
            if (r == 0 && LastEvalObject.GetType() == typeof(string))
                return LastEvalObject.ToString();
            return "";
        }

        public ScriptObject GetHistoryHeader()
        {
            return (ScriptObject)_engine.Invoke("history_header");
        }

        public string GetHistoryPostData()
        {
            int r=EvaluateJavaScript("history_postdata()",JavaScriptPath);
            if(r == 0&&LastEvalObject.GetType()==typeof(string))
                return LastEvalObject.ToString();
            return "";
        }

        public ScriptObject GetHistoryOnSuccess(ref string response)
        {
            return (ScriptObject)_engine.Invoke("history_onsuccess", response);
        }

        public void GetHistoryOnFail(int errorCode)
        {
            _engine.Invoke("history_onfail", errorCode);
        }

        public bool IsHistoryData(ref string url)
        {
            object r = _engine.Invoke("is_history_data", url);
            if (!(r is Undefined))
            {
                return (bool)r;
            }

            return false;
        }

        public void GetHistoryOnReport(ref string data)
        {
            _engine.Invoke("history_onreport", data);
        }
    }
}