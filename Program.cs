using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using System.ComponentModel;

var builder = Host.CreateApplicationBuilder(args);

builder.Logging.AddConsole(consoleLogOptions =>
{
    consoleLogOptions.LogToStandardErrorThreshold = LogLevel.Trace;
});

builder.Services
    .AddMcpServer()
    .WithStdioServerTransport()
    .WithToolsFromAssembly();

await builder.Build().RunAsync();

[McpServerToolType]
public static class MCPSample
{
    [McpServerTool, Description("取得查詢股票代號的即時股價資訊")]
    public static string GetCurrentStockInfo(
        [Description("要查詢的股票代號")] string stockCode)
    {
        return StockPrice.DisplayCurrentStockInfo(stockCode);
    }

    [McpServerTool, Description("取得查詢股票代號指定日期的股價資訊")]
    public static string GetDailyStockInfo(
        [Description("要查詢的股票代號")] string stockCode,
        [Description("要查詢的日期")] string stockDate)
    {
        return StockPrice.DisplayDailyStockInfo(stockCode, stockDate);
    }
    
    [McpServerTool, Description("取得查詢股票代號指定月份的股價資訊")]
    public static string GetMonthlyStockInfo(
        [Description("要查詢的股票代號")] string stockCode,
        [Description("要查詢的月份")] string stockMonth)
    {
        return StockPrice.DisplayMonthlyStockInfo(stockCode, stockMonth);
    }
}