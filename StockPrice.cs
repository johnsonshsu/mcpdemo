using Newtonsoft.Json.Linq;
using System.Text;
using System.Text.RegularExpressions;

/// <summary>
/// 股票價格相關操作
/// 參考文章： https://ithelp.ithome.com.tw/m/articles/10258478
/// </summary>
public static class StockPrice
{
    /// <summary>
    /// 取得即時股價資料 API URL
    /// </summary>
    public static string CurrentStockApiUrl { get; } = "https://mis.twse.com.tw/stock/api/getStockInfo.jsp?json=1&delay=0&ex_ch=";
    /// <summary>
    /// 取得當月股票價格資料 API URL
    /// </summary>
    public static string MonthlyStockApiUrl { get; } = "http://www.twse.com.tw/exchangeReport/STOCK_DAY?response=csv";
    /// <summary>
    /// 錯誤訊息
    /// </summary>
    public static string ErrorMessage { get; set; } = "";
    /// <summary>
    /// 是否成功
    /// </summary>
    public static bool SuccessCode { get; set; } = true;
    /// <summary>
    /// 取得即時股價資料
    /// </summary>
    /// <param name="stockCode">股票代號</param>
    /// <returns></returns>
    public static List<StockCurrentPriceModel> GetCurrentStockInfo(string stockCode)
    {
        SuccessCode = true;
        ErrorMessage = "";
        var result = new List<StockCurrentPriceModel>();
        try
        {
            if (string.IsNullOrEmpty(stockCode)) { SuccessCode = false; ErrorMessage = "股票代號不可為空"; return result; }

            // API URL for stock data
            string apiUrl = CurrentStockApiUrl;
            List<string> stockCodeList = stockCode.Split(',').ToList();
            if (stockCodeList.Count == 0) { SuccessCode = false; ErrorMessage = "無效的股票代號"; return result; }
            if (stockCodeList.Count == 1)
                apiUrl += $"tse_{stockCode.Trim()}.tw"; // 單一股票代號
            else
            {
                foreach (var code in stockCodeList)
                {
                    var codeNo = code.Trim();
                    apiUrl += $"tse_{codeNo}.tw|";
                }
            }

            using (HttpClient client = new HttpClient())
            {
                HttpResponseMessage response = client.GetAsync(apiUrl).Result;

                if (!response.IsSuccessStatusCode) { SuccessCode = false; ErrorMessage = "API請求失敗"; return result; }

                byte[] responseBytes = response.Content.ReadAsByteArrayAsync().Result;
                string responseData = Encoding.UTF8.GetString(responseBytes);
                JObject json = JObject.Parse(responseData);

                var stockData = json["msgArray"]?.ToList();
                if (stockData == null || stockData.Count == 0)
                {
                    SuccessCode = false;
                    ErrorMessage = "無法取得股票資料";
                    return result;
                }
                foreach (var stock in stockData)
                {
                    var stockModel = new StockCurrentPriceModel();
                    stockModel.StockCode = stock["c"]?.ToString() ?? "未知股票代號";
                    stockModel.StockName = stock["n"]?.ToString() ?? "未知股票名稱";
                    stockModel.CurrentPrice = stock["z"]?.ToString() ?? "未知成交價";
                    stockModel.HighestPrice = stock["h"]?.ToString() ?? "未知最高價";
                    stockModel.LowestPrice = stock["l"]?.ToString() ?? "未知最低價";
                    result.Add(stockModel);
                }
            }
        }
        catch (Exception ex)
        {
            SuccessCode = false;
            ErrorMessage = $"發生錯誤: {ex.Message}";
        }
        return result;
    }

    /// <summary>
    /// 取得指定股票及日期成交資訊
    /// </summary>
    /// <param name="stockCode">股票代號</param>
    /// <param name="stockDate">股票日期</param>
    /// <returns></returns>
    public static StockPriceDetailModel? GetDailyStockInfo(string stockCode, string stockDate)
    {
        SuccessCode = true;
        var result = new StockPriceDetailModel();
        var data = GetMonthlyStockInfo(stockCode, stockDate);
        if (data.Count == 0)
        {
            SuccessCode = false;
        }
        else
        {
            SuccessCode = true;
            string str_date = stockDate;
            if (stockDate.Length == 6) // 格式化日期 202501 => 2025/01/01
                str_date = stockDate.Insert(4, "/") + "/01";
            else if (stockDate.Length == 7) // 格式化日期 2025-01 => 2025/01/01
                str_date = stockDate.Replace("-", "/") + "/01";
            else if (stockDate.Length == 8) // 格式化日期 20250101 => 2025/01/01
                str_date = stockDate.Insert(4, "/").Insert(7, "/");
            else if (stockDate.Length == 10) // 格式化日期 2025-01-01 => 2025/01/01
                str_date = stockDate.Replace("-", "/");
            else
            {
                SuccessCode = false;
                ErrorMessage = "股票日期格式錯誤，應為YYYYMMDD或YYYY/MM/DD";
                return new StockPriceDetailModel();
            }

            // 將西元年轉換為民國年
            int int_year = int.Parse(str_date.Substring(0, 4)) - 1911;
            string str_chinese_date = int_year.ToString() + "/" + str_date.Substring(5, 2) + "/" + str_date.Substring(8, 2);
            result = data.Where(m => m.Date == str_chinese_date).FirstOrDefault();
            if (result == null) SuccessCode = false;
        }
        if (!SuccessCode)
        {
            ErrorMessage = "無法取得指定日期的股票資料";
            result = new StockPriceDetailModel();
        }
        return result;
    }

    /// <summary>
    /// 取得指定股票當月各日成交資訊
    /// </summary>
    /// <param name="stockCode">股票代號</param>
    /// <param name="stockMonth">股票月份</param>
    /// <returns></returns>
    public static List<StockPriceDetailModel> GetMonthlyStockInfo(string stockCode, string stockMonth)
    {
        SuccessCode = true;
        ErrorMessage = "";
        var result = new List<StockPriceDetailModel>();
        if (string.IsNullOrEmpty(stockCode)) { SuccessCode = false; ErrorMessage = "股票代號不可為空"; return result; }
        if (string.IsNullOrEmpty(stockMonth)) { SuccessCode = false; ErrorMessage = "股票月份不可為空"; return result; }
        if (stockMonth.Length == 6) // 格式化日期 202501 => 2025/01/01
            stockMonth = stockMonth.Insert(4, "/") + "/01";
        else if (stockMonth.Length == 7) // 格式化日期 2025-01 => 2025/01/01
            stockMonth = stockMonth.Replace("-", "/") + "/01";
        else if (stockMonth.Length == 8) // 格式化日期 20250101 => 2025/01/01
            stockMonth = stockMonth.Insert(4, "/").Insert(7, "/");
        else if (stockMonth.Length == 10) // 格式化日期 2025-01-01 => 2025/01/01
            stockMonth = stockMonth.Replace("-", "/");
        else
        {
            SuccessCode = false;
            ErrorMessage = "股票日期格式錯誤，應為YYYYMMDD或YYYY/MM/DD";
            return new List<StockPriceDetailModel>();
        }

        try
        {
            string stockDate = stockMonth; // 股票月份格式為YYYYMM
            stockDate = stockDate.Replace("/", "").Replace("-", ""); // 格式化日期
            if (stockDate.Length == 6) stockDate += "01"; // 將月份轉換為YYYYMMDD格式，默認日為01
            if (stockDate.Length != 8) { SuccessCode = false; ErrorMessage = "股票日期格式錯誤，應為YYYYMMDD"; return result; } // 股票日期格式錯誤，應為YYYYMMDD

            // API URL for stock data
            string apiUrl = MonthlyStockApiUrl;
            apiUrl += $"&date={stockDate}";
            apiUrl += $"&stockNo={stockCode}";

            using (HttpClient client = new HttpClient())
            {
                HttpResponseMessage response = client.GetAsync(apiUrl).Result;

                if (!response.IsSuccessStatusCode) { SuccessCode = false; ErrorMessage = "API請求失敗"; return result; } // API請求失敗

                Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
                byte[] responseBytes = response.Content.ReadAsByteArrayAsync().Result;
                string responseData = Encoding.GetEncoding("big5").GetString(responseBytes);

                string[] lines = responseData.Split('\n');
                string pattern = "(?<=^|,)(?:\"([^\"]*)\"|([^,]*))";
                Regex regex = new Regex(pattern);
                int lineCount = 0;
                // 跳過前兩行標題
                string stockData = "";
                string stockName = "";
                foreach (string line in lines)
                {
                    lineCount++;
                    var matches = regex.Matches(line);
                    if (lineCount == 1)
                    {
                        stockData = matches[0].Groups[1].Value.Trim('"'); // 第一行為股票標題行
                        stockName = stockData.Split(' ')[2].Trim('"'); // 股票名稱
                    }

                    if (lineCount <= 2) continue; // 跳過前兩行標題
                    if (matches.Count < 9) continue; // 跳過非股價資料

                    var stockModel = new StockPriceDetailModel
                    {
                        StockCode = stockCode,
                        StockName = stockName,
                        Date = matches[0].Groups[1].Value.Trim('"'),
                        Volume = matches[1].Groups[1].Value.Trim('"'),
                        Turnover = matches[2].Groups[1].Value.Trim('"'),
                        OpeningPrice = matches[3].Groups[1].Value.Trim('"'),
                        HighestPrice = matches[4].Groups[1].Value.Trim('"'),
                        LowestPrice = matches[5].Groups[1].Value.Trim('"'),
                        ClosingPrice = matches[6].Groups[1].Value.Trim('"'),
                        Change = matches[7].Groups[1].Value.Trim('"'),
                        Transaction = matches[8].Groups[1].Value.Trim('"')
                    };

                    result.Add(stockModel);
                }
            }
            return result;
        }
        catch (Exception ex)
        {
            SuccessCode = false;
            ErrorMessage = $"發生錯誤: {ex.Message}";
            return result;
        }
    }
    /// <summary>
    /// 顯示即時股價資訊
    /// </summary>
    /// <param name="stockCode">股票代號</param>
    /// <returns></returns>
    public static string DisplayCurrentStockInfo(string stockCode)
    {
        var stockList = GetCurrentStockInfo(stockCode);
        if (stockList.Count == 0)
        {
            return $"無法取得 {stockCode} 的即時股票資料";
        }
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("即時股價資訊：");
        sb.AppendLine(new string('-', 50));
        foreach (var stock in stockList)
        {
            sb.AppendLine($"股票代號: {stock.StockCode}");
            sb.AppendLine($"股票名稱: {stock.StockName}");
            sb.AppendLine($"當前價格: {stock.CurrentPrice}");
            sb.AppendLine($"最高價格: {stock.HighestPrice}");
            sb.AppendLine($"最低價格: {stock.LowestPrice}");
            sb.AppendLine(new string('-', 50));
        }
        return sb.ToString();
    }
    /// <summary>
    /// 顯示當月股價資訊
    /// </summary>
    /// <param name="stockCode">股票代號</param>
    /// <param name="stockMonth">股票月份</param>
    /// <returns></returns>
    public static string DisplayMonthlyStockInfo(string stockCode, string stockMonth)
    {
        var stockList = GetMonthlyStockInfo(stockCode, stockMonth);
        if (stockList.Count == 0)
        {
            return $"無法取得 {stockCode} {stockMonth} 的當月股票資料";
        }
        StringBuilder sb = new StringBuilder();
        sb.AppendLine($"股票代號: {stockCode}");
        sb.AppendLine($"股票月份: {stockMonth}");
        sb.AppendLine(new string('-', 50));
        foreach (var stock in stockList)
        {
            sb.AppendLine($"交易日期: {stock.Date}");
            sb.AppendLine($"成交股數: {stock.Volume}");
            sb.AppendLine($"成交金額: {stock.Turnover}");
            sb.AppendLine($"開盤價格: {stock.OpeningPrice}");
            sb.AppendLine($"最高價格: {stock.HighestPrice}");
            sb.AppendLine($"最低價格: {stock.LowestPrice}");
            sb.AppendLine($"收盤價格: {stock.ClosingPrice}");
            sb.AppendLine($"成交漲跌: {stock.Change}");
            sb.AppendLine($"成交筆數: {stock.Transaction}");
            sb.AppendLine(new string('-', 50));
        }
        return sb.ToString();
    }
    /// <summary>
    /// 顯示股票指定日期的股價資訊
    /// </summary>
    /// <param name="stockCode">股票代號</param>
    /// <param name="stockDate">股票日期</param>
    /// <returns></returns>
    public static string DisplayDailyStockInfo(string stockCode, string stockDate)
    {
        var stockDetail = GetDailyStockInfo(stockCode, stockDate);
        if (stockDetail == null)
        {
            return $"無法取得 {stockCode} {stockDate} 的當日股票資料";
        }
        StringBuilder sb = new StringBuilder();
        sb.AppendLine($"股票代號: {stockDetail.StockCode}");
        sb.AppendLine($"股票名稱: {stockDetail.StockName}");
        sb.AppendLine($"交易日期: {stockDetail.Date}");
        sb.AppendLine($"成交股數: {stockDetail.Volume}");
        sb.AppendLine($"成交金額: {stockDetail.Turnover}");
        sb.AppendLine($"開盤價格: {stockDetail.OpeningPrice}");
        sb.AppendLine($"最高價格: {stockDetail.HighestPrice}");
        sb.AppendLine($"最低價格: {stockDetail.LowestPrice}");
        sb.AppendLine($"收盤價格: {stockDetail.ClosingPrice}");
        sb.AppendLine($"成交漲跌: {stockDetail.Change}");
        sb.AppendLine($"成交筆數: {stockDetail.Transaction}");
        return sb.ToString();
    }
}

/// <summary>
/// 股票即時價格模型
/// </summary>
public class StockCurrentPriceModel
{
    /// <summary>
    /// 股票代號
    /// </summary>
    public string? StockCode { get; set; } = "";
    /// <summary>
    /// 股票名稱
    /// </summary>
    public string? StockName { get; set; } = "";
    /// <summary>
    /// 當前價格
    /// </summary>
    public string? CurrentPrice { get; set; } = "";
    /// <summary>
    /// 最高價格
    /// </summary>
    public string? HighestPrice { get; set; } = "";
    /// <summary>
    /// 最低價格
    /// </summary>
    public string? LowestPrice { get; set; } = "";
}

/// <summary>
/// 股票價格明細模型
/// </summary>
public class StockPriceDetailModel
{
    /// <summary>
    /// 股票代號
    /// </summary>
    public string? StockCode { get; set; } = "";
    /// <summary>
    /// 股票名稱
    /// </summary>
    public string? StockName { get; set; } = "";
    /// <summary>
    /// 日期
    /// </summary>
    public string? Date { get; set; } = null;
    /// <summary>
    /// 成交量
    /// </summary>
    public string? Volume { get; set; } = "";
    /// <summary>
    /// 總成交額
    /// </summary>
    public string? Turnover { get; set; } = "";
    /// <summary>
    /// 開盤價
    /// </summary>
    public string? OpeningPrice { get; set; } = "";
    /// <summary>
    /// 最高價
    /// </summary>
    public string? HighestPrice { get; set; } = "";
    /// <summary>
    /// 最低價
    /// </summary>
    public string? LowestPrice { get; set; } = "";
    /// <summary>
    /// 收盤價
    /// </summary>
    public string? ClosingPrice { get; set; } = "";
    /// <summary>
    /// 漲跌幅
    /// </summary>
    public string? Change { get; set; } = "";
    /// <summary>
    /// 成交筆數
    /// </summary>
    public string? Transaction { get; set; } = "";
}
