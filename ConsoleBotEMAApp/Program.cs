using System;
using System.Threading;

namespace ConsoleBotEMAApp
{
    internal class Program
    {
        static void Main(string[] args)
        {
            string symbol = string.Empty;
            string leverage = string.Empty;
            string quantity = string.Empty;
            string updownTrend = string.Empty;
            string sideNew = string.Empty;

            Console.Write("Hop dong ma ban muon trade: ");
            symbol = Console.ReadLine().ToUpper();

            Console.Write("Don bay cua hop dong: ");
            leverage = Console.ReadLine();

            Console.Write($"Ban dang co 1 lenh {symbol} dang trade dung khong? YES/NO: ");
            string checkIstrade = Console.ReadLine().ToUpper();

            if (checkIstrade == "YES")
            {
                Console.Write($"Lenh dang trade la? LONG/SHORT: ");
                string checkLongShort = Console.ReadLine().ToUpper();
                sideNew = checkLongShort == "LONG" ? "SELL" : "BUY";
            }

            Console.Write("Gia cua hop dong: ");
            quantity = Console.ReadLine();

            Console.Write("Xu huong hien tai Ema5_50 du doan. UP/DOWN: ");
            updownTrend = Console.ReadLine().ToUpper();

            while (true)
            {
                /*Kiểm tra xem đã sang giờ chưa?*/
                /*Check có nằm trong 1 phút không Nếu không thì return luôn?*/
                if (Convert.ToInt32(DateTime.Now.Minute.ToString()) != 1)
                {
                    Commons.WriteLog($"Chua den luc trade {symbol}...");
                    Thread.Sleep(1000);
                    continue;
                }

                /*Chỉ cho 10s để vào lệnh*/
                if (Convert.ToInt32(DateTime.Now.Second.ToString()) > 10)
                {
                    Commons.WriteLog("Da qua thoi gian vao lenh...");
                    Thread.Sleep(1000);
                    continue;
                }

                /*Kiểm tra xu hướng hiện tại*/
                string CheckUpDownTrend = Commons.CheckUpOrDowntrendEma550();
                string CheckUpDownTrendAlert = CheckUpDownTrend == "UP" ? "Uptrend" : "Downtrend";
                if (CheckUpDownTrend == updownTrend)
                {
                    Commons.WriteLog($"Xu huong hien tai cua {symbol} dang la {CheckUpDownTrendAlert}. Khong co gi thay doi...");
                    Thread.Sleep(1000);
                    continue;
                }
                /*Update xu hướng mới*/
                updownTrend = CheckUpDownTrend;

                /*Nếu xu hướng thay đổi thì chốt lời cắt lỗ luôn*/
                /*Kiểm tra user có đang vào lệnh không?*/
                BinancePositionInformationRes checkIsNoTrade = Api.BinanceIsNoTrade(symbol);
                if (checkIsNoTrade.code == "00")
                {
                    string PreparingTrade = CheckUpDownTrend == "UP" ? "BUY" : "SELL";
                    Commons.WriteLog(Commons.AddQueueItemFutureBinance(symbol, leverage, PreparingTrade, quantity));
                    sideNew = PreparingTrade == "BUY" ? "SELL" : "BUY";
                }
                else
                {
                    Commons.WriteLog(Commons.AddQueueItemFutureBinance(symbol, leverage, sideNew, quantity));
                    sideNew = sideNew == "BUY" ? "SELL" : "BUY";
                    Commons.WriteLog(Commons.AddQueueItemFutureBinance(symbol, leverage, sideNew, quantity));
                    sideNew = sideNew == "BUY" ? "SELL" : "BUY";
                }

                Commons.WriteLog("Da dat lenh thanh cong...");
                Thread.Sleep(10000);
            }
        }
    }
}
