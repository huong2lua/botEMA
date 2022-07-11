using Newtonsoft.Json.Linq;
using System;
using System.Configuration;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Web.Script.Serialization;

namespace ConsoleBotApp
{
    public class Api
    {
        public static string BinanceApiUrl = ConfigurationManager.AppSettings["BinanceApiUrl"];
        public static string BinanceApiKey = ConfigurationManager.AppSettings["BinanceApiKey"];
        public static string BinanceApiSecret = ConfigurationManager.AppSettings["BinanceApiSecret"];
        public static bool TrustAllCertificateCallback(object sender, X509Certificate cert, X509Chain chain, SslPolicyErrors errors)
        {
            return true;
        }
        public static xypherApiRes CallApiCoin(string CoinName)
        {
            xypherApiRes obj = new xypherApiRes();
            string URL = "https://xypher.io/Remote/API/MVP/Screener/LIQ";
            System.Net.WebRequest webRequest = System.Net.WebRequest.Create(URL);
            webRequest.Method = "POST";
            webRequest.ContentType = "application/x-www-form-urlencoded";
            Stream reqStream = webRequest.GetRequestStream();
            string postData = $"Exchange=Binance&PageNumber=0&Ticker={CoinName}&Side=All&Amount=1";
            byte[] postArray = Encoding.ASCII.GetBytes(postData);
            reqStream.Write(postArray, 0, postArray.Length);
            reqStream.Close();
            StreamReader sr = new StreamReader(webRequest.GetResponse().GetResponseStream());
            string response = sr.ReadToEnd().Replace("[", "").Replace("]", "");
            JObject objJson = JObject.Parse(response);

            if (objJson != null)
            {
                //result
                obj.MarketName = objJson["MarketName"] != null ? objJson["MarketName"].ToString() : null;
                obj.Exchange = objJson["Exchange"] != null ? objJson["Exchange"].ToString() : null;
                obj.Amount = objJson["Amount"] != null ? objJson["Amount"].ToString() : null;
                obj.Rate = objJson["Rate"] != null ? objJson["Rate"].ToString() : null;
                obj.Side = objJson["Side"] != null ? objJson["Side"].ToString() : null;
                obj.unixtime = objJson["unixtime"] != null ? objJson["unixtime"].ToString() : null;
            }

            return obj;
        }
        public static BinancePositionInformationRes BinancePositionInformation(string symbol)
        {
            BinancePositionInformationRes obj = new BinancePositionInformationRes();

            string timeStamp = Commons.CreateTimeStamp();

            ASCIIEncoding encoding = new ASCIIEncoding();
            byte[] keyBytes = encoding.GetBytes(BinanceApiSecret);
            string contents = $"symbol={symbol}&timestamp={timeStamp}";
            byte[] messageBytes = encoding.GetBytes(contents);
            HMACSHA256 cryptographer = new HMACSHA256(keyBytes);
            byte[] bytes = cryptographer.ComputeHash(messageBytes);
            string signature = BitConverter.ToString(bytes).Replace("-", "").ToLower();

            string postData = $"{BinanceApiUrl}/fapi/v2/positionRisk?{contents}&signature={signature}";

            var request = WebRequest.Create(postData);
            request.Method = "GET";
            request.Headers.Add("X-MBX-APIKEY", BinanceApiKey);

            var webResponse = request.GetResponse();
            var webStream = webResponse.GetResponseStream();

            var reader = new StreamReader(webStream);
            string res = reader.ReadToEnd().Replace("[", "").Replace("]", "");

            JObject objJson = JObject.Parse(res);

            obj.code = "00";
            obj.msg = "Success";

            if (objJson != null)
            {
                //result
                obj.entryPrice = objJson["entryPrice"] != null ? objJson["entryPrice"].ToString() : "0";
            }
            else
            {
                obj.code = "99";
                obj.msg = "System Error";
            }
            if (Convert.ToDouble(obj.entryPrice) != 0)
            {
                obj.code = "01";
                obj.msg = "Lenh da duoc khoi tao";
            }
            return obj;
        }
        public static BinanceNewOrderRes BinanceNewOrder(BinanceNewOrderReq req)
        {
            BinanceNewOrderRes obj = new BinanceNewOrderRes();
            try
            {
                req.timestamp = Commons.CreateTimeStamp();

                ASCIIEncoding encoding = new ASCIIEncoding();
                byte[] keyBytes = encoding.GetBytes(BinanceApiSecret);
                string contents = string.Empty;
                switch (req.type)
                {
                    case "LIMIT":
                        req.timeInForce = "GTC";
                        contents = $"symbol={req.symbol}&side={req.side}&type={req.type}&timeInForce={req.timeInForce}&quantity={req.quantity}&price={req.price}&timestamp={req.timestamp}";
                        break;
                    case "MARKET":
                        contents = $"symbol={req.symbol}&side={req.side}&type={req.type}&quantity={req.quantity}&timestamp={req.timestamp}";
                        break;
                    case "TAKE_PROFIT_MARKET":
                        req.timeInForce = "GTE_GTC";
                        contents = $"symbol={req.symbol}&side={req.side}&type={req.type}&timeInForce={req.timeInForce}&stopPrice={req.stopPrice}&closePosition={req.closePosition}&timestamp={req.timestamp}";
                        break;
                    case "STOP_MARKET":
                        req.timeInForce = "GTE_GTC";
                        contents = $"symbol={req.symbol}&side={req.side}&type={req.type}&timeInForce={req.timeInForce}&stopPrice={req.stopPrice}&closePosition={req.closePosition}&timestamp={req.timestamp}";
                        break;
                }

                byte[] messageBytes = encoding.GetBytes(contents);
                HMACSHA256 cryptographer = new HMACSHA256(keyBytes);
                byte[] bytes = cryptographer.ComputeHash(messageBytes);
                string signature = BitConverter.ToString(bytes).Replace("-", "").ToLower();
                string url = $"{BinanceApiUrl}/fapi/v1/order";
                WebRequest webRequest = WebRequest.Create(url);
                webRequest.Headers.Add("X-MBX-APIKEY", BinanceApiKey);
                webRequest.Method = "POST";
                webRequest.ContentType = "application/x-www-form-urlencoded";

                Stream reqStream = webRequest.GetRequestStream();
                string postData = $"{contents}&signature={signature}";
                byte[] postArray = Encoding.ASCII.GetBytes(postData);
                reqStream.Write(postArray, 0, postArray.Length);
                reqStream.Close();
                StreamReader sr = new StreamReader(webRequest.GetResponse().GetResponseStream());
                string response = sr.ReadToEnd().Replace("[", "").Replace("]", "");
                JObject objJson = JObject.Parse(response);

                if (objJson != null)
                {
                    //result
                    obj.code = objJson["code"] != null ? objJson["code"].ToString() : "00";
                    obj.msg = objJson["msg"] != null ? objJson["msg"].ToString() : "Success";
                    obj.orderId = objJson["orderId"] != null ? objJson["orderId"].ToString() : null;
                }
                else
                {
                    obj.code = "99";
                    obj.msg = "System Error";
                }

                return obj;
            }
            catch (Exception ex)
            {
                obj.code = "99";
                obj.msg = ex.Message;
                return obj;
            }
        }
        public static BinanceChangeLeverageRes BinanceChangeLeverage(string symbol, string leverage)
        {
            BinanceChangeLeverageRes obj = new BinanceChangeLeverageRes();
            string timestamp = Commons.CreateTimeStamp();

            ASCIIEncoding encoding = new ASCIIEncoding();
            byte[] keyBytes = encoding.GetBytes(BinanceApiSecret);
            string contents = $"symbol={symbol}&leverage={leverage}&timestamp={timestamp}";

            byte[] messageBytes = encoding.GetBytes(contents);
            HMACSHA256 cryptographer = new HMACSHA256(keyBytes);
            byte[] bytes = cryptographer.ComputeHash(messageBytes);
            string signature = BitConverter.ToString(bytes).Replace("-", "").ToLower();
            string url = $"{BinanceApiUrl}/fapi/v1/leverage";
            WebRequest webRequest = WebRequest.Create(url);
            webRequest.Headers.Add("X-MBX-APIKEY", BinanceApiKey);
            webRequest.Method = "POST";
            webRequest.ContentType = "application/x-www-form-urlencoded";

            Stream reqStream = webRequest.GetRequestStream();
            string postData = $"{contents}&signature={signature}";
            byte[] postArray = Encoding.ASCII.GetBytes(postData);
            reqStream.Write(postArray, 0, postArray.Length);
            reqStream.Close();
            var getResponse = webRequest.GetResponse();
            StreamReader sr = new StreamReader(getResponse.GetResponseStream());
            string response = sr.ReadToEnd().Replace("[", "").Replace("]", "");
            JObject objJson = JObject.Parse(response);

            if (objJson != null)
            {
                //result
                obj.code = objJson["code"] != null ? objJson["code"].ToString() : "00";
                obj.msg = objJson["msg"] != null ? objJson["msg"].ToString() : null;
                obj.symbol = objJson["symbol"] != null ? objJson["symbol"].ToString() : null;
                obj.leverage = objJson["leverage"] != null ? objJson["leverage"].ToString() : null;
                obj.maxNotionalValue = objJson["maxNotionalValue"] != null ? objJson["maxNotionalValue"].ToString() : null;
            }
            else
            {
                obj.code = "99";
                obj.msg = "System Error";
            }

            return obj;
        }
    }
    public class BaseRes
    {
        public string code { get; set; }
        public string msg { get; set; }
    }
    public class BinanceChangeLeverageRes : BaseRes
    {
        public string symbol { get; set; }
        public string leverage { get; set; }
        public string maxNotionalValue { get; set; }
    }
    public class BinancePositionInformationRes : BaseRes
    {
        public string entryPrice { get; set; }
    }
    public class xypherApiRes
    {
        public string MarketName { get; set; }
        public string Exchange { get; set; }
        public string Amount { get; set; }
        public string Rate { get; set; }
        public string Side { get; set; }
        public string unixtime { get; set; }
    }
    public class BinanceNewOrderReq
    {
        public string symbol { get; set; }
        public string side { get; set; }
        public string type { get; set; }
        public string timeInForce { get; set; }
        public string quantity { get; set; }
        public string price { get; set; }
        public string stopPrice { get; set; }
        public string closePosition { get; set; }
        public string timestamp { get; set; }
        public string signature { get; set; }
    }
    public class BinanceNewOrderRes : BaseRes
    {
        public string orderId { get; set; }
    }
}
