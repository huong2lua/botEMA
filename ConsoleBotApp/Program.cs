using System;
using System.Configuration;
using System.Threading;
using System.Web.Script.Serialization;

namespace ConsoleBotApp
{
    class Program
    {
        static void Main(string[] args)
        {
            string tempData = string.Empty;
            while (true)
            {
                string[] TradeCoinMultiBinance = ConfigurationManager.AppSettings["TradeCoinMultiBinance"].Split(';');
                string CoinsName = string.Empty;
                foreach (var itemCoin in TradeCoinMultiBinance)
                {
                    if (!string.IsNullOrEmpty(itemCoin))
                    {
                        /*Tên Coin*/
                        string CoinName = itemCoin.Split(':')[0];
                        CoinsName += CoinName;

                        /*Điều chỉnh đòn bẩy*/
                        string AdjustLeverage = itemCoin.Split(':')[1];
                        /*Để trống thì mặc định hiểu vào 10x*/
                        AdjustLeverage = string.IsNullOrEmpty(AdjustLeverage) ? "10" : AdjustLeverage;

                        /*Điều chỉnh lượng coin*/
                        string Amount = itemCoin.Split(':')[2];

                        /*Kiểm tra giá thanh lý*/
                        xypherApiRes resCallApiCoin = Api.CallApiCoin(CoinName);
                        string response = new JavaScriptSerializer().Serialize(resCallApiCoin);

                        /*Kiểm tra dữ liệu cũ*/
                        string checkDataPre = Commons.ReadTempDataCoin(CoinName, tempData);

                        /*Check xem có phải data cũ hay không?*/
                        if (response == checkDataPre)
                        {
                            continue;
                        }
                        tempData = Commons.WriteTempDataCoin(CoinName, response, tempData);

                        Console.WriteLine($"{DateTime.Now.ToString()} => Liquidation: {response}");

                        /*Check xem đã có lệnh future nào được đặt chưa?*/
                        if (!Commons.CheckCoinFutureBinance(CoinName))
                        {
                            Console.WriteLine($"{DateTime.Now.ToString()} => Binance: {CoinName}USDT da duoc dat lenh tren Future.");
                            continue;
                        }

                        try
                        {
                            Commons.CreateQueueFutureBinance(resCallApiCoin.MarketName, resCallApiCoin.Rate, resCallApiCoin.Side, CoinName, AdjustLeverage, Amount);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"{DateTime.Now.ToString()} => System: {ex.Message}");
                        }
                    }
                }

                Thread.Sleep(100);
            }
        }
    }
}
