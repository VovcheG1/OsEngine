using OsEngine.Entity;
using OsEngine.Language;
using OsEngine.Logging;
using OsEngine.Market.Servers.Entity;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading;
using WebSocket4Net;
using System.Security.Cryptography;
using System.Collections.Concurrent;
using OsEngine.Market.Servers.KiteConnect.Json;
using Newtonsoft.Json;
using RestSharp.Extensions.MonoHttp;
using System.Linq;


namespace OsEngine.Market.Servers.KiteConnect
{
    public class KiteConnectServer : AServer
    {
        public KiteConnectServer()
        {
            KiteServerRealization realization = new KiteServerRealization();
            ServerRealization = realization;

            CreateParameterString(OsLocalization.Market.ServerParamPublicKey, "");
            CreateParameterPassword(OsLocalization.Market.ServerParamSecretKey, "");
            CreateParameterString(OsLocalization.Market.ServerParamToken, "");
            CreateParameterString(OsLocalization.Market.ServerParamPassphrase, "");
            CreateParameterBoolean("Exchange NSE", false);
            CreateParameterBoolean("Exchange BSE", false);
            CreateParameterBoolean("Exchange NFO", false);
            CreateParameterBoolean("Exchange CDS", false);
            CreateParameterBoolean("Exchange BFO", false);
            CreateParameterBoolean("Exchange MCX", false);
            CreateParameterBoolean("Exchange BCD", false);
            // CreateParameterBoolean("Exchange MF", false);
        }
    }

    public class KiteServerRealization : IServerRealization
    {
        #region 1 Constructor, Status, Connection

        public KiteServerRealization()
        {
            //Thread worker = new Thread(ConnectionCheckThread);
            //worker.Name = "CheckAliveKite";
            //worker.Start();



        }

        public void Connect()
        {
            try
            {
                _apiKey = ((ServerParameterString)ServerParameters[0]).Value;
                _secretKey = ((ServerParameterPassword)ServerParameters[1]).Value;
                _requestToken = ((ServerParameterString)ServerParameters[2]).Value;
                _accessToken = ((ServerParameterString)ServerParameters[3]).Value;

                if (string.IsNullOrEmpty(_apiKey) ||
                    string.IsNullOrEmpty(_secretKey))
                {
                    SendLogMessage("Can`t run Kite connector. No keys", LogMessageType.Error);
                    return;
                }

                //if (GetCurSessionToken() == false)
                //{
                //    SendLogMessage("Authorization Error. Probably an invalid token is specified.",
                //    LogMessageType.Error);
                //    return;
                //}

                RestClient client = new RestClient(_baseUrl);
                RestRequest request = new RestRequest("/user/profile", Method.GET);
                request.AddHeader("Authorization", "token " + _apiKey + ":" + _accessToken);
                request.AddHeader("X-Kite-Version", "3");

                IRestResponse response = client.Execute(request);

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    //ResponseRestKite<UserProfile> responseUserProfile = JsonConvert.DeserializeAnonymousType(response.Content, new ResponseRestKite<UserProfile>());
                }
                else
                {
                    SendLogMessage($"User profile request error. Status: {response.StatusCode} - {response.Content}", LogMessageType.Error);
                }

                CreateWebSocketConnection();

            }
            catch (Exception exception)
            {
                SendLogMessage(exception.Message.ToString(), LogMessageType.Error);
            }
        }

        private void ConnectionCheckThread()
        {
            while (true)
            {
                Thread.Sleep(10000);

                //if (ServerStatus != ServerConnectStatus.Connect)
                //{
                //    continue;
                //}

                //if (_lastGetLiveTimeToketTime.AddMinutes(20) < DateTime.Now)
                //{
                if (GetCurSessionToken() == false)
                {
                    if (ServerStatus == ServerConnectStatus.Connect)
                    {
                        ServerStatus = ServerConnectStatus.Disconnect;
                        DisconnectEvent();
                    }
                }
                //}
            }
        }

        private bool GetCurSessionToken()
        {
            try
            {

                string url = $"{_login}?v=3&api_key={_apiKey}";

                //string s = $"api_key={_apiKey}&request_token={_requestToken}&checksum={checksum}";

                //Dictionary<string, string> param = new Dictionary<string, string>();
                //param.Add("api_key", _apiKey);
                //param.Add("request_token", _requestToken);
                //param.Add("checksum", checksum);


                RestClient client = new RestClient(url);
                RestRequest request = new RestRequest(Method.GET);

                // User-Agent: KiteConnect.Net / 4.3.0.0

                //request.AddHeader("User-Agent", "KiteConnect.Net / 4.3.0.0");
                //request.AddHeader("X-Kite-Version", "3");
                //request.AddParameter("api_key", _apiKey);
                //request.AddParameter("request_token", _requestToken);
                //request.AddParameter("checksum", checksum);

                IRestResponse response = client.Execute(request);

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    string[] responseUri = response.ResponseUri.ToString().Split('=');
                    string requestToken = responseUri[2];

                    _accessToken = GetAccessToken(requestToken);

                }


                //TokenResponse newLiveToken = JsonConvert.DeserializeAnonymousType(content, new TokenResponse());

                //_lastGetLiveTimeToketTime = DateTime.Now;
                //_apiTokenReal = newLiveToken.AccessToken;
                return true;

            }
            catch (Exception exception)
            {
                SendLogMessage("Token request error: " + exception.ToString(), LogMessageType.Error);
                return false;
            }
        }

        private string GetAccessToken(string requestToken)
        {
            string checksum = SHA256Hash(_apiKey + requestToken + _secretKey);

            var param = new Dictionary<string, dynamic>
            {
                {"api_key", _apiKey},
                {"request_token", requestToken},
                {"checksum", checksum}
            };


            RestClient client = new RestClient(_baseUrl);
            RestRequest request = new RestRequest("/session/token", Method.POST);

            // User-Agent: KiteConnect.Net / 4.3.0.0
            request.AddParameter("api_key", _apiKey);
            request.AddParameter("request_token", requestToken);
            request.AddParameter("checksum", checksum);
            request.AddHeader("User-Agent", "KiteConnect.Net / 4.3.0.0");
            request.AddHeader("X-Kite-Version", "3");
            request.AddHeader("Authorization", "token " + _apiKey + ":" + _accessToken);


            IRestResponse response = client.Execute(request);

            if (response.StatusCode == HttpStatusCode.OK)
            {

            }

            return null;
        }

        public static string SHA256Hash(string Data)
        {
            char[] inputData = Data.ToCharArray();

            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] inputBytes = Encoding.UTF8.GetBytes(inputData);
                byte[] hashBytes = sha256.ComputeHash(inputBytes);
                StringBuilder hexHash = new StringBuilder();

                for (int i = 0; i < hashBytes.Length; i++)
                {
                    hexHash.AppendFormat("{0:x2}", hashBytes[i]);
                }

                return hexHash.ToString();
            }
        }

        public void Dispose()
        {

            DeleteWebscoektConnection();

            SendLogMessage("Connection Closed by Kite. WebSocket Data Closed Event", LogMessageType.System);

            if (ServerStatus != ServerConnectStatus.Disconnect)
            {
                ServerStatus = ServerConnectStatus.Disconnect;
                DisconnectEvent();
            }
        }

        public DateTime ServerTime { get; set; }

        public ServerType ServerType
        {
            get { return ServerType.KiteConnect; }
        }

        public ServerConnectStatus ServerStatus { get; set; }

        public event Action ConnectEvent;

        public event Action DisconnectEvent;

        #endregion

        #region 2 Properties

        public List<IServerParameter> ServerParameters { get; set; }

        private static string _apiKey;

        private string _secretKey;

        private string _baseUrl = "https://api.kite.trade";

        private string _login = "https://kite.zerodha.com/connect/login";

        private static string _webSocketUrl = "wss://ws.kite.trade";

        private static string _accessToken;

        private string _requestToken;

        private bool _useExchangeNSE = false;

        private bool _useExchangeBSE = false;

        private bool _useExchangeNFO = false;

        private bool _useExchangeCDS = false;

        private bool _useExchangeBFO = false;

        private bool _useExchangeMCX = false;

        private bool _useExchangeBCD = false;

        private bool _useExchangeMF = false;

        private ConcurrentQueue<string> _fifoListWebSocketMessage = new ConcurrentQueue<string>();

        private RateGate _rateGateQuote = new RateGate(1, TimeSpan.FromMilliseconds(1000));

        private RateGate _rateGateHistoricalCandle = new RateGate(3, TimeSpan.FromMilliseconds(1000));

        private RateGate _rateGateOrder = new RateGate(10, TimeSpan.FromMilliseconds(1000));

        private RateGate _rateGateAllOthers = new RateGate(10, TimeSpan.FromMilliseconds(1000));

        #endregion

        #region 3 Securities

        public void GetSecurities()
        {
            _useExchangeNSE = ((ServerParameterBool)ServerParameters[4]).Value;
            _useExchangeBSE = ((ServerParameterBool)ServerParameters[5]).Value;
            _useExchangeNFO = ((ServerParameterBool)ServerParameters[6]).Value;
            _useExchangeCDS = ((ServerParameterBool)ServerParameters[7]).Value;
            _useExchangeBFO = ((ServerParameterBool)ServerParameters[8]).Value;
            _useExchangeMCX = ((ServerParameterBool)ServerParameters[9]).Value;
            _useExchangeBCD = ((ServerParameterBool)ServerParameters[10]).Value;
            //_useExchangeMF = ((ServerParameterBool)ServerParameters[11]).Value;

            if (_useExchangeNSE)
            {
                UpdateSecurity("NSE");
            }

            if (_useExchangeBSE)
            {
                UpdateSecurity("BSE");
            }

            if (_useExchangeNFO)
            {
                UpdateSecurity("NFO");
            }

            if (_useExchangeCDS)
            {
                UpdateSecurity("CDS");
            }

            if (_useExchangeBFO)
            {
                UpdateSecurity("BFO");
            }

            if (_useExchangeMCX)
            {
                UpdateSecurity("MCX");
            }

            if (_useExchangeBCD)
            {
                UpdateSecurity("BCD");
            }

            //if (_useExchangeMF)
            //{
            //    UpdateSecurity("MF");
            //}

            if (_securities.Count > 0)
            {
                SendLogMessage("Securities loaded. Count: " + _securities.Count, LogMessageType.System);

                if (SecurityEvent != null)
                {
                    SecurityEvent.Invoke(_securities);
                }
            }
        }

        private void UpdateSecurity(string exchange)
        {
            _rateGateAllOthers.WaitToProceed();

            try
            {
                RestClient client = new RestClient(_baseUrl);
                RestRequest request = new RestRequest($"/instruments/{exchange}", Method.GET);

                request.AddHeader("Authorization", "token " + _apiKey + ":" + _accessToken);
                request.AddHeader("X-Kite-Version", "3");

                IRestResponse response = client.Execute(request);

                if (response.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    List<List<string>> securitiesArea = ParseCSV(response.Content);

                    for (int i = 1; i < securitiesArea.Count - 1; i++)
                    {
                        List<string> security = securitiesArea[i];

                        if (security.Count < 12)
                        {
                            continue;
                        }

                        SecurityType instrumentType = GetSecurityType(security[9]);

                        if (instrumentType == SecurityType.None)
                        {
                            continue;
                        }

                        Security newSecurity = new Security();

                        newSecurity.Exchange = ServerType.KiteConnect.ToString();
                        newSecurity.Name = security[2];
                        newSecurity.NameFull = $"{security[11]}_{security[9]}_{security[2]}";
                        newSecurity.NameClass = $"{security[11]}_{security[9]}";
                        newSecurity.NameId = $"{security[0]}_{security[1]}";
                        newSecurity.SecurityType = instrumentType;

                        if (newSecurity.SecurityType == SecurityType.Option)
                        {
                            newSecurity.Strike = security[6].ToDecimal();
                        }

                        if (newSecurity.SecurityType == SecurityType.Option
                            || newSecurity.SecurityType == SecurityType.Futures)
                        {
                            //newSecurity.Expiration = security[5].ToDecimal();
                        }

                        newSecurity.DecimalsVolume = Convert.ToInt32(security[8]);
                        newSecurity.Lot = security[8].ToDecimal();
                        newSecurity.PriceStep = security[7].ToDecimal();
                        newSecurity.Decimals = security[7].DecimalsCount();
                        newSecurity.PriceStepCost = newSecurity.PriceStep;
                        newSecurity.State = SecurityStateType.Activ;
                        //newSecurity.MinTradeAmount = item.minov.ToDecimal();

                        _securities.Add(newSecurity);
                    }

                }
                else
                {
                    SendLogMessage($"Securities request error. Status: {response.StatusCode}, {response.Content}", LogMessageType.Error);
                }
            }
            catch (Exception exception)
            {
                SendLogMessage("Securities request error: " + exception.ToString(), LogMessageType.Error);
            }
        }

        private SecurityType GetSecurityType(string security)
        {
            if (security.StartsWith("FUT"))
            {
                return SecurityType.Futures;
            }
            else if (security.StartsWith("EQ"))
            {
                return SecurityType.Stock;
            }
            else if (security.StartsWith("CE") || security.StartsWith("PE"))
            {
                return SecurityType.Option;
            }

            return SecurityType.None;
        }


        private List<List<string>> ParseCSV(string content)
        {
            string[] lines = content.Split('\n');

            List<List<string>> instruments = new List<List<string>>();

            for (int i = 0; i < lines.Length; i++)
            {
                string[] fields = lines[i].Split(new[] { ',' }, StringSplitOptions.None);
                List<string> item = new List<string>(fields);
                instruments.Add(item);
            }

            return instruments;
        }

        private List<Security> _securities = new List<Security>();

        public event Action<List<Security>> SecurityEvent;

        #endregion

        #region 4 Portfolios

        public void GetPortfolios()
        {
            _rateGateAllOthers.WaitToProceed();

            try
            {
                RestClient client = new RestClient(_baseUrl);
                RestRequest request = new RestRequest("/user/margins", Method.GET);

                request.AddHeader("Authorization", "token " + _apiKey + ":" + _accessToken);
                request.AddHeader("X-Kite-Version", "3");

                IRestResponse response = client.Execute(request);

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    ResponseRestKite<Data> responseUserProfile = JsonConvert.DeserializeAnonymousType(response.Content, new ResponseRestKite<Data>());

                    if (responseUserProfile.status == "success")
                    {
                        if (Convert.ToBoolean(responseUserProfile.data.equity.enabled) == true)
                        {
                            Portfolio myPortfolio = new Portfolio();
                            myPortfolio.Number = "EquityPortfolio";
                            myPortfolio.ValueBegin = responseUserProfile.data.equity.available.opening_balance.ToDecimal();
                            myPortfolio.ValueCurrent = responseUserProfile.data.equity.available.live_balance.ToDecimal();
                            _myPortfolios.Add(myPortfolio);
                        }
                        if (Convert.ToBoolean(responseUserProfile.data.commodity.enabled) == true)
                        {
                            Portfolio myPortfolio = new Portfolio();
                            myPortfolio.Number = "CommodityPortfolio";
                            myPortfolio.ValueBegin = responseUserProfile.data.equity.available.opening_balance.ToDecimal();
                            myPortfolio.ValueCurrent = responseUserProfile.data.equity.available.live_balance.ToDecimal();
                            _myPortfolios.Add(myPortfolio);
                        }

                        if (PortfolioEvent != null)
                        {
                            PortfolioEvent(_myPortfolios);
                        }
                    }
                    else
                    {
                        SendLogMessage($"Portfolio error type: {responseUserProfile.error_type}, {responseUserProfile.message}", LogMessageType.Error);
                    }
                }
                else
                {
                    SendLogMessage($"Portfolio request error. Status: {response.StatusCode}, {response.Content}", LogMessageType.Error);
                }
            }
            catch (Exception exception)
            {
                SendLogMessage("Portfolio request error " + exception.ToString(), LogMessageType.Error);
            }

        }

        private List<Portfolio> _myPortfolios = new List<Portfolio>();

        public event Action<List<Portfolio>> PortfolioEvent;

        #endregion

        #region 5 Data

        public List<Candle> GetLastCandleHistory(Security security, TimeFrameBuilder timeFrameBuilder, int candleCount)
        {
            throw new NotImplementedException();
        }

        public List<Candle> GetCandleDataToSecurity(Security security, TimeFrameBuilder timeFrameBuilder, DateTime startTime, DateTime endTime, DateTime actualTime)
        {
            throw new NotImplementedException();
        }

        public List<Trade> GetTickDataToSecurity(Security security, DateTime startTime, DateTime endTime, DateTime actualTime)
        {
            throw new NotImplementedException();
        }

        #endregion

        #region 6 WebSocket creation

        private WebSocket _webSocket;



        private void CreateWebSocketConnection()
        {
            try
            {
                //Dictionary<string, string> param = new Dictionary<string, string>() { ["X-Kite-Version"] = "3" };

                string fullUrlWebSoket = $"{_webSocketUrl}/?api_key={_apiKey}&access_token={_accessToken}";

                if (_webSocket != null)
                {
                    return;
                }

                _webSocket = new WebSocket(fullUrlWebSoket);
                _webSocket.EnableAutoSendPing = true;
                _webSocket.AutoSendPingInterval = 10;

                _webSocket.Opened += _webSocket_Opened;
                _webSocket.Closed += _webSocket_Closed;
                _webSocket.MessageReceived += _webSocket_MessageReceived;
                _webSocket.Error += _webSocket_Error;

                _webSocket.Open();
            }
            catch (Exception ecxeption)
            {
                SendLogMessage(ecxeption.ToString(), LogMessageType.Error);
            }
        }

        private void DeleteWebscoektConnection()
        {
            if (_webSocket != null)
            {
                _webSocket.Opened -= _webSocket_Opened;
                _webSocket.Closed -= _webSocket_Closed;
                _webSocket.MessageReceived -= _webSocket_MessageReceived;
                _webSocket.Error -= _webSocket_Error;


                try
                {
                    _webSocket.Close();
                }
                catch
                {
                    // ignore
                }
                _webSocket = null;
            }
        }

        #endregion

        #region 7 WebSocket events

        private void _webSocket_Error(object sender, SuperSocket.ClientEngine.ErrorEventArgs e)
        {
            try
            {
                SuperSocket.ClientEngine.ErrorEventArgs error = (SuperSocket.ClientEngine.ErrorEventArgs)e;

                if (error.Exception != null)
                {
                    SendLogMessage(error.Exception.ToString(), LogMessageType.Error);
                }
            }
            catch (Exception exception)
            {
                SendLogMessage("Data socket error" + exception.ToString(), LogMessageType.Error);
            }
        }

        private void _webSocket_MessageReceived(object sender, MessageReceivedEventArgs e)
        {
            try
            {
                if (ServerStatus == ServerConnectStatus.Disconnect)
                {
                    return;
                }

                if (e == null)
                {
                    return;
                }

                if (e.Message.Length == 4)
                { // pong message
                    return;
                }

                if (string.IsNullOrEmpty(e.Message))
                {
                    return;
                }

                if (_fifoListWebSocketMessage == null)
                {
                    return;
                }

                _fifoListWebSocketMessage.Enqueue(e.Message);
            }
            catch (Exception exception)
            {
                SendLogMessage(exception.ToString(), LogMessageType.Error);
            }
        }

        private void _webSocket_Closed(object sender, EventArgs e)
        {
            try
            {
                if (DisconnectEvent != null && ServerStatus != ServerConnectStatus.Disconnect)
                {
                    SendLogMessage("Connection Closed by Kite. WebSocket Closed Event", LogMessageType.Connect);
                    ServerStatus = ServerConnectStatus.Disconnect;
                    DisconnectEvent();
                }
            }
            catch (Exception ex)
            {
                SendLogMessage(ex.ToString(), LogMessageType.Error);
            }
        }

        private void _webSocket_Opened(object sender, EventArgs e)
        {
            SendLogMessage("Socket Open", LogMessageType.Connect);

            if (ServerStatus != ServerConnectStatus.Connect
                && _webSocket != null
                && _webSocket.State == WebSocketState.Open)
            {
                ServerStatus = ServerConnectStatus.Connect;
                ConnectEvent();
            }
        }

        #endregion





        public event Action<MarketDepth> MarketDepthEvent;
        public event Action<Trade> NewTradesEvent;
        public event Action<Order> MyOrderEvent;
        public event Action<MyTrade> MyTradeEvent;


        public void CancelAllOrders()
        {
            throw new NotImplementedException();
        }

        public void CancelAllOrdersToSecurity(Security security)
        {
            throw new NotImplementedException();
        }

        public void CancelOrder(Order order)
        {
            throw new NotImplementedException();
        }

        public void ChangeOrderPrice(Order order, decimal newPrice)
        {
            throw new NotImplementedException();
        }

        public void GetAllActivOrders()
        {
            throw new NotImplementedException();
        }


        public void GetOrderStatus(Order order)
        {
            throw new NotImplementedException();
        }


        public void SendOrder(Order order)
        {
            throw new NotImplementedException();
        }

        public void Subscrible(Security security)
        {
            throw new NotImplementedException();
        }



        //public static DateTime UnixToDateTime(UInt64 unixTimeStamp)
        //{
        //    // Unix timestamp is seconds past epoch
        //    DateTime dateTime = new DateTime(1970, 1, 1, 5, 30, 0, 0, DateTimeKind.Unspecified);
        //    dateTime = dateTime.AddSeconds(unixTimeStamp);
        //    return dateTime;
        //}



        public static string BuildParam(string Key, dynamic Value)
        {
            if (Value is string)
            {
                return HttpUtility.UrlEncode(Key) + "=" + HttpUtility.UrlEncode((string)Value);
            }
            else
            {
                string[] values = (string[])Value;
                return String.Join("&", values.Select(x => HttpUtility.UrlEncode(Key) + "=" + HttpUtility.UrlEncode(x)));
            }
        }

        #region 13 Log

        private void SendLogMessage(string message, LogMessageType messageType)
        {
            LogMessageEvent?.Invoke(message, messageType);
        }

        public event Action<string, LogMessageType> LogMessageEvent;

        #endregion
    }

    //public enum Exchanges
    //{
    //    NSE,
    //    BSE,
    //    NFO,
    //    CDS,
    //    BFO,
    //    MCX
    //}
}
