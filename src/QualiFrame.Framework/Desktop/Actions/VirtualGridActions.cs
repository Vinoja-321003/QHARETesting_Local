using System;
using System.Runtime.Versioning;
using FlaUI.Core.Definitions;
using QualiFrame.Framework.Desktop.Elements;
using QualiFrame.Framework.Desktop.Locators;
using QualiFrame.Framework.Desktop.Utilities;
using QualiFrame.Framework.Reports;
using QualiFrame.Framework.Utilities.Logging;

namespace QualiFrame.Framework.Desktop.Actions
{
    /// <summary>
    /// Actions for interacting with virtual / infinitely scrolling desktop grids.
    /// </summary>
    [SupportedOSPlatform("windows")]
    public static class VirtualGridActions
    {
        public static void VirtualSelectRow(DesktopElement gridRoot, int rowIndex, string? description = null)
        {
            Logger.Information("VirtualGridActions.VirtualSelectRow started. RowIndex={RowIndex}, Description={Description}", rowIndex, description ?? string.Empty);
            ExtentReportManager.LogInfo($"VirtualGridActions.VirtualSelectRow - RowIndex={rowIndex}, Description={description}");

            ScrollToward(gridRoot, rowIndex);
            GridActions.SelectRow(gridRoot, rowIndex, description);
        }

        public static void VirtualClickCell(DesktopElement gridRoot, int rowIndex, string columnName, string? description = null)
        {
            Logger.Information("VirtualGridActions.VirtualClickCell started. RowIndex={RowIndex}, Column={Column}, Description={Description}", rowIndex, columnName, description ?? string.Empty);
            ExtentReportManager.LogInfo($"VirtualGridActions.VirtualClickCell - RowIndex={rowIndex}, Column={columnName}, Description={description}");

            ScrollToward(gridRoot, rowIndex);
            GridActions.ClickCell(gridRoot, rowIndex, columnName, description);
        }

        public static string VirtualGetCellValue(DesktopElement gridRoot, int rowIndex, string columnName, string? description = null)
        {
            Logger.Information("VirtualGridActions.VirtualGetCellValue started. RowIndex={RowIndex}, Column={Column}, Description={Description}", rowIndex, columnName, description ?? string.Empty);
            ExtentReportManager.LogInfo($"VirtualGridActions.VirtualGetCellValue - RowIndex={rowIndex}, Column={columnName}, Description={description}");

            ScrollToward(gridRoot, rowIndex);
            return GridActions.GetCellValue(gridRoot, rowIndex, columnName, description);
        }

        public static void VirtualSetCellValue(DesktopElement gridRoot, int rowIndex, string columnName, string value, string? description = null)
        {
            Logger.Information("VirtualGridActions.VirtualSetCellValue started. RowIndex={RowIndex}, Column={Column}, Value={Value}, Description={Description}", rowIndex, columnName, value, description ?? string.Empty);
            ExtentReportManager.LogInfo($"VirtualGridActions.VirtualSetCellValue - RowIndex={rowIndex}, Column={columnName}, Value={value}, Description={description}");

            ScrollToward(gridRoot, rowIndex);
            GridActions.SetCellValue(gridRoot, rowIndex, columnName, value, description);
        }

        public static void VirtualScrollToStart(DesktopElement gridRoot, string? description = null)
        {
            Logger.Information("VirtualGridActions.VirtualScrollToStart started. Description={Description}", description ?? string.Empty);
            ExtentReportManager.LogInfo($"VirtualGridActions.VirtualScrollToStart - Description={description}");

            Scroll(gridRoot, toEnd: false);
        }

        public static void VirtualScrollToEnd(DesktopElement gridRoot, string? description = null)
        {
            Logger.Information("VirtualGridActions.VirtualScrollToEnd started. Description={Description}", description ?? string.Empty);
            ExtentReportManager.LogInfo($"VirtualGridActions.VirtualScrollToEnd - Description={description}");

            Scroll(gridRoot, toEnd: true);
        }

        private static void ScrollToward(DesktopElement gridRoot, int rowIndex)
        {
            GridActions.ScrollGrid(gridRoot, rowIndex > 0 ? Math.Min(rowIndex, 5) : 0, "Bring virtual row into view");
        }

        private static void Scroll(DesktopElement gridRoot, bool toEnd)
        {
            SmartDesktopActions.Retry(() =>
            {
                var raw = SelfHealingLocatorEngine.FindElement(gridRoot.Session, gridRoot.Locator);
                if (raw.Patterns.Scroll.TryGetPattern(out var scroll))
                {
                    var amount = toEnd
                        ? ScrollAmount.LargeIncrement
                        : ScrollAmount.LargeDecrement;
                    for (var i = 0; i < 25 && (toEnd ? scroll.VerticalScrollPercent < 99 : scroll.VerticalScrollPercent > 1); i++)
                    {
                        scroll.Scroll(ScrollAmount.NoAmount, amount);
                    }

                    return;
                }

                GridActions.ScrollGrid(gridRoot, toEnd ? 25 : -25, toEnd ? "Scroll to end" : "Scroll to start");
            });
        }
    }
}
