using KafkaLoad.Core.Models.Reports;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace KafkaLoad.Infrastructure.Export
{
    public static class HtmlReportExporter
    {
        private static readonly (string Key, string Title, string YUnit, string Color)[] ChartDefinitions =
        {
            ("Producer_ThroughputMB", "Producer Throughput",   "MB/s",   "#4a9eff"),
            ("Producer_MsgRate",      "Producer Message Rate", "msg/s",  "#4a9eff"),
            ("Producer_Latency",      "Producer Latency",      "ms",     "#f59e0b"),
            ("Producer_Errors",       "Producer Errors",       "err/s",  "#ef4444"),
            ("Consumer_ThroughputMB", "Consumer Throughput",   "MB/s",   "#10b981"),
            ("Consumer_MsgRate",      "Consumer Message Rate", "msg/s",  "#10b981"),
            ("Consumer_Latency",      "Consumer Latency",      "ms",     "#f59e0b"),
            ("Consumer_Errors",       "Consumer Errors",       "err/s",  "#ef4444"),
        };

        public static string GenerateHtml(TestReport report)
        {
            _chartSeq = 0; // reset so ids are stable across calls
            var sb = new StringBuilder();

            sb.Append("""
                <!DOCTYPE html>
                <html lang="en">
                <head>
                <meta charset="UTF-8"/>
                <meta name="viewport" content="width=device-width, initial-scale=1.0"/>
                <title>Kafka Load Test Report</title>
                <style>
                  *, *::before, *::after { box-sizing: border-box; margin: 0; padding: 0; }
                  body { background: #1a1a2e; color: #e2e8f0; font-family: 'Segoe UI', system-ui, sans-serif; font-size: 14px; line-height: 1.5; }
                  .container { max-width: 1200px; margin: 0 auto; padding: 32px 24px; }
                  .card { background: #1e2030; border: 1px solid #2d3748; border-radius: 8px; padding: 24px; margin-bottom: 20px; }
                  h1 { font-size: 24px; font-weight: 700; color: #f7fafc; }
                  h2 { font-size: 15px; font-weight: 600; color: #4a9eff; margin-bottom: 14px; }
                  .badge { display: inline-block; background: #4a9eff; color: #fff; font-size: 11px; font-weight: 600; padding: 3px 10px; border-radius: 4px; }
                  .badge-outline { background: transparent; border: 1px solid #2d3748; color: #94a3b8; }
                  .text-secondary { color: #718096; }
                  .text-success { color: #16a34a; }
                  .text-warn { color: #d97706; }
                  .text-error { color: #ef4444; }
                  .header-row { display: flex; align-items: center; gap: 10px; flex-wrap: wrap; margin-bottom: 6px; }
                  .meta { color: #718096; font-size: 12px; margin-top: 4px; }
                  hr { border: none; border-top: 1px solid #2d3748; margin: 16px 0; }
                  table { width: 100%; border-collapse: collapse; }
                  td { padding: 7px 10px; }
                  td:first-child { color: #718096; width: 200px; }
                  td:last-child { font-weight: 600; text-align: right; }
                  tr { border-bottom: 1px solid #2d3748; }
                  tr:last-child { border-bottom: none; }
                  .metrics-grid { display: grid; grid-template-columns: repeat(auto-fit, minmax(280px, 1fr)); gap: 20px; }
                  .charts-columns { display: grid; grid-template-columns: 1fr 1fr; gap: 20px; }
                  @media (max-width: 800px) { .charts-columns { grid-template-columns: 1fr; } }
                  .chart-column { display: flex; flex-direction: column; gap: 12px; }
                  .chart-column-title { font-size: 13px; font-weight: 600; color: #4a9eff; margin-bottom: 4px; }
                  .chart-wrap { background: #161625; border: 1px solid #2d3748; border-radius: 6px; padding: 8px; }
                </style>
                </head>
                <body>
                <div class="container">
                """);

            // Header card
            sb.Append("<div class=\"card\">");
            sb.Append("<div class=\"header-row\">");
            sb.Append($"<h1>{Esc(report.ScenarioName)}</h1>");
            sb.Append($"<span class=\"badge\">{Esc(report.TestType.ToString())}</span>");
            sb.Append($"<span class=\"badge badge-outline\">{report.DurationSeconds}s</span>");
            sb.Append("</div>");
            sb.Append($"<div class=\"meta\">Run: {report.CreatedAt:yyyy-MM-dd HH:mm:ss} UTC</div>");
            sb.Append("</div>");

            // Config + metrics row
            sb.Append("<div class=\"metrics-grid\">");

            // Configuration
            sb.Append("<div class=\"card\">");
            sb.Append("<h2>Configuration</h2>");
            sb.Append("<table>");
            AppendRow(sb, "Topic", report.ConfigSnapshot.TopicName);
            AppendRow(sb, "Producers", report.ConfigSnapshot.ProducersCount.ToString());
            AppendRow(sb, "Consumers", report.ConfigSnapshot.ConsumersCount.ToString());
            AppendRow(sb, "Message Size", $"{report.ConfigSnapshot.MessageSize:N0} B");
            AppendRow(sb, "Target Throughput",
                report.ConfigSnapshot.TargetThroughput.HasValue
                    ? $"{report.ConfigSnapshot.TargetThroughput.Value:N0} msg/s"
                    : "Unlimited");
            sb.Append("</table>");
            sb.Append("</div>");

            // Producer metrics
            var p = report.ProducerMetrics;
            sb.Append("<div class=\"card\">");
            sb.Append("<h2>Producer Metrics</h2>");
            sb.Append("<table>");
            AppendRow(sb, "Total Attempted",   $"{p.TotalMessagesAttempted:N0}");
            AppendRow(sb, "Successful Sent",   $"{p.SuccessMessagesSent:N0}", "text-success");
            AppendRow(sb, "Errors",            $"{p.ErrorMessages:N0}",       "text-error");
            AppendRow(sb, "Error Rate",        $"{p.ErrorRatePercent:N2} %");
            AppendRow(sb, "Throughput",        $"{p.ThroughputMsgSec:N1} msg/s");
            AppendRow(sb, "Data Sent",         $"{p.TotalBytesSent / 1_048_576.0:N2} MB");
            AppendRow(sb, "Avg Latency",       $"{p.AvgLatencyMs:N2} ms",    "text-warn");
            AppendRow(sb, "Max Latency",       $"{p.MaxLatencyMs:N2} ms",    "text-warn");
            AppendRow(sb, "P95 Latency",       $"{p.P95Lat:N2} ms",          "text-warn");
            sb.Append("</table>");
            sb.Append("</div>");

            // Consumer metrics (if present)
            if (report.ConsumerMetrics != null)
            {
                var c = report.ConsumerMetrics;
                sb.Append("<div class=\"card\">");
                sb.Append("<h2>Consumer Metrics</h2>");
                sb.Append("<table>");
                AppendRow(sb, "Total Received",   $"{c.TotalMessagesConsumed:N0}");
                AppendRow(sb, "Successful",       $"{c.SuccessMessagesConsumed:N0}", "text-success");
                AppendRow(sb, "Errors",           $"{c.ErrorMessagesConsumed:N0}",   "text-error");
                AppendRow(sb, "Throughput",       $"{c.ThroughputMsgSec:N1} msg/s");
                AppendRow(sb, "Data Received",    $"{c.TotalBytesConsumed / 1_048_576.0:N2} MB");
                AppendRow(sb, "Avg E2E Latency",  $"{c.AvgEndToEndLatencyMs:N2} ms", "text-warn");
                AppendRow(sb, "Max E2E Latency",  $"{c.MaxEndToEndLatencyMs:N2} ms", "text-warn");
                AppendRow(sb, "Max Consumer Lag", $"{c.MaxConsumerLag:N0}",           "text-warn");
                AppendRow(sb, "Final Consumer Lag", $"{c.FinalConsumerLag:N0}");
                sb.Append("</table>");
                sb.Append("</div>");
            }

            sb.Append("</div>"); // metrics-grid

            // Charts — producer column (left) and consumer column (right)
            bool hasConsumer = report.ConsumerMetrics != null;
            var producerCharts = ChartDefinitions.Where(c => c.Key.StartsWith("Producer_")).ToArray();
            var consumerCharts = ChartDefinitions.Where(c => c.Key.StartsWith("Consumer_")).ToArray();

            sb.Append("<div class=\"card\">");
            sb.Append("<h2>Time Series</h2>");
            sb.Append("<div class=\"charts-columns\">");

            // Left column: producer charts
            sb.Append("<div class=\"chart-column\">");
            sb.Append("<div class=\"chart-column-title\">Producer</div>");
            foreach (var def in producerCharts)
            {
                report.TimeSeriesData.TryGetValue(def.Key, out var pts);
                sb.Append("<div class=\"chart-wrap\">");
                sb.Append(BuildSvgChart(def.Title, def.YUnit, pts, def.Color));
                sb.Append("</div>");
            }
            sb.Append("</div>"); // chart-column

            // Right column: consumer charts (only when consumer data exists)
            sb.Append("<div class=\"chart-column\">");
            if (hasConsumer)
            {
                sb.Append("<div class=\"chart-column-title\">Consumer</div>");
                foreach (var def in consumerCharts)
                {
                    report.TimeSeriesData.TryGetValue(def.Key, out var pts);
                    sb.Append("<div class=\"chart-wrap\">");
                    sb.Append(BuildSvgChart(def.Title, def.YUnit, pts, def.Color));
                    sb.Append("</div>");
                }
            }
            sb.Append("</div>"); // chart-column

            sb.Append("</div>"); // charts-columns
            sb.Append("</div>"); // card

            sb.Append("""
                </div>
                </body>
                </html>
                """);

            return sb.ToString();
        }

        private static void AppendRow(StringBuilder sb, string label, string value, string? valueCssClass = null)
        {
            string valueHtml = valueCssClass != null
                ? $"<span class=\"{valueCssClass}\">{Esc(value)}</span>"
                : Esc(value);
            sb.Append($"<tr><td>{Esc(label)}</td><td>{valueHtml}</td></tr>");
        }

        // Counter to generate unique clip-path IDs per chart within one HTML document
        private static int _chartSeq;

        private static string BuildSvgChart(string title, string yUnit, List<TimeSeriesPoint>? points, string lineColor)
        {
            const int W = 600, H = 230;
            // padL=68: enough for labels like "1.23k" at font-size 11 (monospace ~6.6px/ch → 5ch=33px + margin)
            const int padL = 68, padR = 14, padT = 30, padB = 30;
            int pw = W - padL - padR;
            int ph = H - padT - padB;

            // Unique id so clip-paths don't collide across charts in the same HTML file
            string clipId = $"cp{++_chartSeq}";

            var ic = CultureInfo.InvariantCulture; // ensure dots as decimal separators

            string F(double v, string fmt) => v.ToString(fmt, ic);

            var sb = new StringBuilder();
            sb.Append($"<svg width=\"100%\" viewBox=\"0 0 {W} {H}\" xmlns=\"http://www.w3.org/2000/svg\">");
            sb.Append($"<rect width=\"{W}\" height=\"{H}\" fill=\"#161625\" rx=\"4\"/>");

            // Title includes the Y unit so we don't need a separate rotated label
            sb.Append($"<text x=\"{padL + pw / 2}\" y=\"19\" text-anchor=\"middle\" fill=\"#94a3b8\" font-size=\"11\" font-family=\"'Segoe UI',sans-serif\">{Esc($"{title} ({yUnit})")}</text>");

            if (points == null || points.Count < 2)
            {
                sb.Append($"<text x=\"{W / 2}\" y=\"{H / 2 + 5}\" text-anchor=\"middle\" fill=\"#4a5568\" font-size=\"12\" font-family=\"'Segoe UI',sans-serif\">No data</text>");
                sb.Append("</svg>");
                return sb.ToString();
            }

            double xMin = points[0].TimeSeconds;
            double xMax = points[^1].TimeSeconds;
            double yActualMin = points.Min(p => p.Value);
            double yActualMax = points.Max(p => p.Value);
            double yActualRange = yActualMax - yActualMin;

            // Y-axis always starts from 0 for non-negative data so the baseline is visible
            double yMin, yMax;
            if (yActualRange < 0.0001)
            {
                yMin = yActualMin >= 0 ? 0 : yActualMin - 0.5;
                yMax = yActualMax + 0.5;
            }
            else
            {
                yMin = yActualMin >= 0 ? 0 : yActualMin - yActualRange * 0.05;
                yMax = yActualMax + yActualRange * 0.10;
            }

            double yRange = yMax - yMin;
            if (yRange < 0.0001) yRange = 1.0;
            double xRange = xMax - xMin;
            if (xRange < 0.0001) xRange = 1.0;

            double ToX(double xv) => padL + (xv - xMin) / xRange * pw;
            double ToY(double yv) => padT + ph - (yv - yMin) / yRange * ph;

            // Clip rect so the line/fill never bleed outside the plot area
            sb.Append($"<defs><clipPath id=\"{clipId}\"><rect x=\"{padL}\" y=\"{padT}\" width=\"{pw}\" height=\"{ph}\"/></clipPath></defs>");

            // Horizontal grid lines + Y labels
            const int gridLines = 4;
            for (int i = 0; i <= gridLines; i++)
            {
                double gY = padT + (double)ph * i / gridLines;
                sb.Append($"<line x1=\"{padL}\" y1=\"{F(gY, "F1")}\" x2=\"{padL + pw}\" y2=\"{F(gY, "F1")}\" stroke=\"#2d3748\" stroke-width=\"0.7\"/>");
                double val = yMax - yRange * i / gridLines;
                // Align labels to the right with a 6px gap from the axis line
                sb.Append($"<text x=\"{padL - 6}\" y=\"{F(gY + 4, "F1")}\" text-anchor=\"end\" dominant-baseline=\"middle\" fill=\"#718096\" font-size=\"11\" font-family=\"monospace\">{FormatAxisValue(val, ic)}</text>");
            }

            // X axis labels
            const int xTicks = 4;
            for (int i = 0; i <= xTicks; i++)
            {
                double xT = xMin + xRange * i / xTicks;
                double xPx = ToX(xT);
                sb.Append($"<text x=\"{F(xPx, "F1")}\" y=\"{H - 5}\" text-anchor=\"middle\" fill=\"#718096\" font-size=\"11\" font-family=\"monospace\">{F(xT, "F0")}s</text>");
            }

            // Axis lines
            sb.Append($"<line x1=\"{padL}\" y1=\"{padT}\" x2=\"{padL}\" y2=\"{padT + ph}\" stroke=\"#4a5568\" stroke-width=\"1\"/>");
            sb.Append($"<line x1=\"{padL}\" y1=\"{padT + ph}\" x2=\"{padL + pw}\" y2=\"{padT + ph}\" stroke=\"#4a5568\" stroke-width=\"1\"/>");

            // Clipped group: fill area + line
            sb.Append($"<g clip-path=\"url(#{clipId})\">");

            var linePoints = string.Join(" ", points.Select(p => $"{F(ToX(p.TimeSeconds), "F1")},{F(ToY(p.Value), "F1")}"));
            double baseY = padT + ph;
            var areaPoints = $"{F(ToX(points[0].TimeSeconds), "F1")},{F(baseY, "F1")} {linePoints} {F(ToX(points[^1].TimeSeconds), "F1")},{F(baseY, "F1")}";

            sb.Append($"<polygon points=\"{areaPoints}\" fill=\"{lineColor}\" opacity=\"0.15\"/>");
            sb.Append($"<polyline points=\"{linePoints}\" fill=\"none\" stroke=\"{lineColor}\" stroke-width=\"2\" stroke-linejoin=\"round\" stroke-linecap=\"round\"/>");

            sb.Append("</g>");
            sb.Append("</svg>");
            return sb.ToString();
        }

        private static string FormatAxisValue(double v, CultureInfo ic)
        {
            double a = Math.Abs(v);
            if (a >= 1_000_000) return (v / 1_000_000).ToString("F2", ic) + "M";
            if (a >= 10_000)    return (v / 1_000).ToString("F1", ic) + "k";
            if (a >= 1_000)     return (v / 1_000).ToString("F2", ic) + "k";
            if (a >= 100)       return v.ToString("F0", ic);
            if (a >= 10)        return v.ToString("F1", ic);
            if (a >= 1)         return v.ToString("F2", ic);
            if (a >= 0.01)      return v.ToString("F3", ic);
            return v.ToString("G2", ic);
        }

        private static string Esc(string? s) =>
            (s ?? string.Empty)
                .Replace("&", "&amp;")
                .Replace("<", "&lt;")
                .Replace(">", "&gt;")
                .Replace("\"", "&quot;");
    }
}
