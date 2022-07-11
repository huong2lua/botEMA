using System;
using System.Configuration;
using System.IO;
using System.Threading;
using System.Web.Script.Serialization;

namespace ConsoleBotApp
{
    class Commons
    {
        public static string GetDirectory = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
        public static double RateBinance = Convert.ToDouble(ConfigurationManager.AppSettings["RateBinance"]);
        public static string CreateTimeStamp()
        {
            var dateTimeOffset = new DateTimeOffset(DateTime.Now);
            var unixDateTime = dateTimeOffset.ToUnixTimeMilliseconds();
            return unixDateTime.ToString();
        }
        public static double RoundMarketPrice(double MarketPrice)
        {
            string stringMarketPrice = MarketPrice.ToString();

            if (stringMarketPrice.Contains("."))
            {
                if (MarketPrice > 1)
                {
                    if (stringMarketPrice.Split('.')[1].Length > 1)
                    {
                        stringMarketPrice = stringMarketPrice.Split('.')[0] + "." + stringMarketPrice.Split('.')[1].Substring(0, 1);
                    }
                }
                else
                {
                    if (stringMarketPrice.Split('.')[1].Length > 2)
                    {
                        stringMarketPrice = stringMarketPrice.Split('.')[0] + "." + stringMarketPrice.Split('.')[1].Substring(0, 2);
                    }
                }
            }

            return Convert.ToDouble(stringMarketPrice);
        }
        public static double ChangeMarketPrice(double MarketPrice, string UpDown)
        {
            if (UpDown.ToString() == "UP")
            {
                if (MarketPrice > 1)
                {
                    MarketPrice = MarketPrice + 0.1;
                }
                else
                {
                    MarketPrice = MarketPrice + 0.01;
                }
            }
            else
            {
                if (MarketPrice > 1)
                {
                    MarketPrice = MarketPrice - 0.1;
                }
                else
                {
                    MarketPrice = MarketPrice - 0.01;
                }
            }
            return MarketPrice;
        }
        public static string ReadTempDataCoin(string CoinName, string tempData)
        {
            if (string.IsNullOrEmpty(tempData))
            {
                return "";
            }
            foreach (var item in tempData.Split(';'))
            {
                if (!string.IsNullOrEmpty(item))
                {
                    string key = item.Split('~')[0];
                    string value = item.Split('~')[1];
                    if (key == CoinName)
                    {
                        return value;
                    }
                }
            }
            return "";
        }
        public static string WriteTempDataCoin(string CoinName, string Content, string tempData)
        {
            if (string.IsNullOrEmpty(tempData))
            {
                return $"{CoinName}~{Content};";
            }
            string temp = string.Empty;
            bool checkCoin = false;
            foreach (var item in tempData.Split(';'))
            {
                if (!string.IsNullOrEmpty(item))
                {
                    string key = item.Split('~')[0];
                    string value = item.Split('~')[1];
                    if (key == CoinName)
                    {
                        checkCoin = true;
                        temp += $"{key}~{Content};";
                    }
                    else
                    {
                        temp += $"{key}~{value};";
                    }
                }
            }
            if (!checkCoin)
            {
                temp += $"{CoinName}~{Content};";
            }
            return temp;
        }
        public static bool CheckCoinFutureBinance(string CoinName)
        {
            BaseRes obj = Api.BinancePositionInformation($"{CoinName}USDT");
            if (obj.code == "00")
            {
                return true;
            }
            return false;
        }
        public static void CreateQueueFutureBinance(string MarketName, string Rate, string _Side, string CoinName, string AdjustLeverage, string Amount)
        {
            double Target = 0;
            double Stoploss = 0;
            decimal Target_ = 0;
            decimal Stoploss_ = 0;
            double Side = double.Parse(Rate);

            /*Check lựa chọn giá thanh lý LiquidationPrice*/
            string LiquidationPrice = ConfigurationManager.AppSettings["LiquidationPrice"].ToUpper();

            /*Check xem có phải dữ liệu future không?*/
            if (LiquidationPrice == "FUTURE" && !MarketName.Contains("PERP"))
            {
                Console.WriteLine($"{DateTime.Now.ToString()} => Only Future. MarketName: {MarketName}, Side: {_Side}, Entry: {Rate}.");
                return;
            }

            /*Check xem có phải dữ liệu future không?*/
            if (LiquidationPrice == "SPOT" && MarketName.Contains("PERP"))
            {
                Console.WriteLine($"{DateTime.Now.ToString()} => Only Spot. MarketName: {MarketName}, Side: {_Side}, Entry: {Rate}.");
                return;
            }

            string SameSide = ConfigurationManager.AppSettings["SameSide"].ToUpper();
            string _SideSameSide = string.Empty;

            /*Chọn cùng hướng hoặc ngược hướng.*/
            if (SameSide == "TRUE")
            {
                _SideSameSide = _Side;
            }
            else
            {
                if (_Side.ToUpper() == "LONG")
                {
                    _SideSameSide = "Short";
                }
                else
                {
                    _SideSameSide = "Long";
                }
            }

            if (_SideSameSide.ToUpper() == "LONG")
            {
                Target = Side * (1 + (RateBinance / 100));
                Stoploss = Side * (1 - (RateBinance / 100));
            }
            else
            {
                Stoploss = Side * (1 + (RateBinance / 100));
                Target = Side * (1 - (RateBinance / 100));
            }

            Target_ = decimal.Round(Convert.ToDecimal(Target), 1);
            Stoploss_ = decimal.Round(Convert.ToDecimal(Stoploss), 1);

            CoinName = CoinName + "USDT";

            string symbol = CoinName;
            string side = _SideSameSide.ToUpper() == "LONG" ? "BUY" : "SELL";
            string type = ConfigurationManager.AppSettings["TypeBinance"].ToUpper();
            string timeInForce = "GTC";
            string quantity = Amount;
            string price = Rate;
            string takeProfitPrice = Convert.ToString(Target_);
            string stopLossPrice = Convert.ToString(Stoploss_);
            Console.WriteLine(AddQueueItemFutureBinance(symbol, AdjustLeverage, side, type, timeInForce, quantity, price, takeProfitPrice, stopLossPrice));
        }
        public static string AddQueueItemFutureBinance(string symbol, string AdjustLeverage, string side, string type, string timeInForce, string quantity, string price, string takeProfitPrice, string stopLossPrice)
        {
            /*Điều chỉnh đòn bẩy*/
            BaseRes BinanceChangeLeverage = Api.BinanceChangeLeverage(symbol, AdjustLeverage);
            if (BinanceChangeLeverage.code != "00")
            {
                return $"{DateTime.Now.ToString()} => Binance: Da xay ra loi trong qua trinh dieu chinh don bay {symbol}.";
            }
            Console.WriteLine($"{DateTime.Now.ToString()} => Binance: Da dieu chinh don bay {symbol}.");

            /*Tạo hợp đồng*/
            BinanceNewOrderReq newOrderReq = new BinanceNewOrderReq();
            newOrderReq.symbol = symbol;
            newOrderReq.side = side;
            newOrderReq.type = type;
            newOrderReq.timeInForce = timeInForce;
            newOrderReq.quantity = quantity;
            newOrderReq.price = price;
            BinanceNewOrderRes newOrder = Api.BinanceNewOrder(newOrderReq);
            if (newOrder.code != "00")
            {
                return $"{DateTime.Now.ToString()} => Binance: Da xay ra loi trong qua trinh tao hop dong {symbol}.";
            }
            Console.WriteLine($"{DateTime.Now.ToString()} => Binance: Dang tao hop dong {symbol}...");

            /*Kiểm tra Entry, tạo TP/SL nếu là Market*/
            double EntryMarket = 0;
            double TargetMarket = 0;
            double StoplossMarket = 0;

            if (type == "MARKET")
            {
                BinancePositionInformationRes checkEntry = Api.BinancePositionInformation(symbol);
                EntryMarket = checkEntry.entryPrice == null ? 0 : Convert.ToDouble(checkEntry.entryPrice);

                if (side == "BUY")
                {
                    TargetMarket = EntryMarket * (1 + (RateBinance / 100));
                    StoplossMarket = EntryMarket * (1 - (RateBinance / 100));
                }
                else
                {
                    StoplossMarket = EntryMarket * (1 + (RateBinance / 100));
                    TargetMarket = EntryMarket * (1 - (RateBinance / 100));
                }

                TargetMarket = RoundMarketPrice(TargetMarket);
                StoplossMarket = RoundMarketPrice(StoplossMarket);

                if (TargetMarket == StoplossMarket)
                {
                    if (side == "BUY")
                    {
                        if (TargetMarket > 1)
                        {
                            TargetMarket = TargetMarket + 0.1;
                            StoplossMarket = StoplossMarket - 0.1;
                        }
                        else
                        {
                            TargetMarket = TargetMarket + 0.01;
                            StoplossMarket = StoplossMarket - 0.01;
                        }
                    }
                    else
                    {
                        if (TargetMarket > 1)
                        {
                            TargetMarket = TargetMarket - 0.1;
                            StoplossMarket = StoplossMarket + 0.1;
                        }
                        else
                        {
                            TargetMarket = TargetMarket - 0.01;
                            StoplossMarket = StoplossMarket + 0.01;
                        }
                    }
                }
            }

            string entry = type == "MARKET" ? EntryMarket.ToString() : price;
            takeProfitPrice = type == "MARKET" ? TargetMarket.ToString() : takeProfitPrice;
            stopLossPrice = type == "MARKET" ? StoplossMarket.ToString() : stopLossPrice;

            Console.WriteLine($"{DateTime.Now.ToString()} => Binance: Dang tao hop dong {symbol} voi {quantity} {symbol}. Entry/TP/SL: {entry}/{takeProfitPrice}/{stopLossPrice}.");

            /*Tạo TP*/
            newOrderReq.side = side == "BUY" ? "SELL" : "BUY";
            newOrderReq.type = "TAKE_PROFIT_MARKET";
            newOrderReq.stopPrice = takeProfitPrice;
            newOrderReq.closePosition = "true";
            int CountTP = 1;
            bool ErrorTP = false;
            do
            {
                BinanceNewOrderRes newTakeProfit = Api.BinanceNewOrder(newOrderReq);
                if (newTakeProfit.code == "00")
                {
                    Console.WriteLine($"{DateTime.Now.ToString()} => Binance: Da tao Take Profit {symbol}.");
                    ErrorTP = false;
                }
                else
                {
                    /*Check xem đã có lệnh future nào được đặt chưa?*/
                    if (CheckCoinFutureBinance(symbol.Replace("USDT", "")))
                    {
                        return $"{DateTime.Now.ToString()} => Binance: Khong ton tai hop dong {symbol} de tao Take Profit.";
                    }
                    TargetMarket = (side == "BUY") ? ChangeMarketPrice(TargetMarket, "UP") : ChangeMarketPrice(TargetMarket, "DOWN");
                    newOrderReq.stopPrice = TargetMarket.ToString();
                    ErrorTP = true;
                    Console.WriteLine($"{DateTime.Now.ToString()} => Binance: Da co loi trong qua trinh tao Take Profit {symbol}. Thu lai lan {CountTP.ToString()} voi takeProfitPrice = {TargetMarket.ToString()}");
                }

                CountTP++;
            } while (ErrorTP);

            /*Tạo SL*/
            newOrderReq.type = "STOP_MARKET";
            newOrderReq.stopPrice = stopLossPrice;
            int CountSL = 1;
            bool ErrorSL = false;
            do
            {
                BinanceNewOrderRes newStopLoss = Api.BinanceNewOrder(newOrderReq);
                if (newStopLoss.code == "00")
                {
                    Console.WriteLine($"{DateTime.Now.ToString()} => Binance: Da tao Stop Loss {symbol}.");
                    ErrorSL = false;
                }
                else
                {
                    /*Check xem đã có lệnh future nào được đặt chưa?*/
                    if (CheckCoinFutureBinance(symbol.Replace("USDT", "")))
                    {
                        return $"{DateTime.Now.ToString()} => Binance: Khong ton tai hop dong {symbol} de tao Stop Loss.";
                    }
                    StoplossMarket = (side == "BUY") ? ChangeMarketPrice(StoplossMarket, "DOWN") : ChangeMarketPrice(StoplossMarket, "UP");
                    newOrderReq.stopPrice = StoplossMarket.ToString();
                    ErrorSL = true;
                    Console.WriteLine($"{DateTime.Now.ToString()} => Binance: Da co loi trong qua trinh tao Stop Loss {symbol}. Thu lai lan {CountSL.ToString()} voi stopLossPrice = {StoplossMarket.ToString()}");
                }

                CountSL++;
            } while (ErrorSL);

            takeProfitPrice = type == "MARKET" ? TargetMarket.ToString() : takeProfitPrice;
            stopLossPrice = type == "MARKET" ? StoplossMarket.ToString() : stopLossPrice;

            return $"{DateTime.Now.ToString()} => Binance: Tao hop dong {symbol} thanh cong voi {quantity} {symbol}. Entry/TP/SL: {entry}/{takeProfitPrice}/{stopLossPrice}.";
        }
    }
}
