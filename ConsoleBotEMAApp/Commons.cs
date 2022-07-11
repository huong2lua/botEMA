using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleBotEMAApp
{
    internal class Commons
    {
        public static string CreateTimeStamp()
        {
            var dateTimeOffset = new DateTimeOffset(DateTime.Now);
            var unixDateTime = dateTimeOffset.ToUnixTimeMilliseconds();
            return unixDateTime.ToString();
        }

        public static string CheckUpOrDowntrendEma550()
        {
            decimal Ema5 = Api.BinanceKlineCandlestickData("BTCUSDT", "1h", "5");
            decimal Ema50 = Api.BinanceKlineCandlestickData("BTCUSDT", "1h", "50");
            string result = Ema5 > Ema50 ? "UP" : "DOWN";
            return result;
        }

        public static void WriteLog(string Content) {
            Console.WriteLine($"{DateTime.Now} => {Content}");
        }

        public static string AddQueueItemFutureBinance(string symbol, string adjustLeverage, string side, string quantity)
        {
            /*Điều chỉnh đòn bẩy*/
            BaseRes BinanceChangeLeverage = Api.BinanceChangeLeverage(symbol, adjustLeverage);
            if (BinanceChangeLeverage.code != "00")
            {
                return $"{DateTime.Now.ToString()} => Da xay ra loi trong qua trinh dieu chinh don bay {symbol}...";
            }
            WriteLog($"Da dieu chinh don bay {symbol}...");

            /*Tạo hợp đồng*/
            BinanceNewOrderReq newOrderReq = new BinanceNewOrderReq();
            newOrderReq.symbol = symbol;
            newOrderReq.side = side;
            newOrderReq.type = "MARKET";
            newOrderReq.quantity = quantity;
            BinanceNewOrderRes newOrder = Api.BinanceNewOrder(newOrderReq);
            if (newOrder.code != "00")
            {
                return $"{DateTime.Now.ToString()} => Da xay ra loi trong qua trinh Market hop dong {symbol}...";
            }
            return $"{DateTime.Now.ToString()} => Da Market hop dong {symbol} thanh cong...";
        }
    }
}
