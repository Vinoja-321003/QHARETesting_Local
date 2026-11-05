using System;
using System.Linq;
using System.Runtime.Versioning;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using QualiFrame.Framework.Desktop.Elements;
using QualiFrame.Framework.Desktop.Locators;
using QualiFrame.Framework.Desktop.Utilities;
using QualiFrame.Framework.Reports;
using QualiFrame.Framework.Utilities.Logging;

namespace QualiFrame.Framework.Desktop.Actions
{
    /// <summary>
    /// Provides reusable high-level actions for interacting with desktop data grids / tables.
    /// </summary>
    [SupportedOSPlatform("windows")]
    public static class GridActions
    {
        public static void SelectRow(DesktopElement gridRoot, int rowIndex, string? description = null)
        {
            Logger.Information("GridActions.SelectRow started. RowIndex={RowIndex}, Description={Description}", rowIndex, description ?? string.Empty);
            ExtentReportManager.LogInfo($"GridActions.SelectRow - RowIndex={rowIndex}, Description={description}");

            SmartDesktopActions.Retry(() =>
            {
                var row = GetRow(Resolve(gridRoot), rowIndex);
                if (row.Patterns.SelectionItem.TryGetPattern(out var selection))
                {
                    selection.Select();
                }
                else
                {
                    row.Click();
                }
            });
        }

        public static void SelectRowByText(DesktopElement gridRoot, string columnName, string text, string? description = null)
        {
            Logger.Information("GridActions.SelectRowByText started. Column={Column}, Text={Text}, Description={Description}", columnName, text, description ?? string.Empty);
            ExtentReportManager.LogInfo($"GridActions.SelectRowByText - Column={columnName}, Text={text}, Description={description}");

            SmartDesktopActions.Retry(() =>
            {
                var raw = Resolve(gridRoot);
                var columnIndex = FindColumnIndex(raw, columnName);
                var rows = GetRows(raw);
                foreach (var row in rows)
                {
                    var cells = GetCells(row);
                    if (columnIndex >= 0 && columnIndex < cells.Length &&
                        string.Equals(DesktopUiQuery.ReadText(cells[columnIndex]).Trim(), text, StringComparison.OrdinalIgnoreCase))
                    {
                        if (row.Patterns.SelectionItem.TryGetPattern(out var selection))
                        {
                            selection.Select();
                        }
                        else
                        {
                            row.Click();
                        }

                        return;
                    }
                }

                throw new InvalidOperationException($"No grid row found where column '{columnName}' equals '{text}'.");
            });
        }

        public static void ClickCell(DesktopElement gridRoot, int rowIndex, string columnName, string? description = null)
        {
            Logger.Information("GridActions.ClickCell started. RowIndex={RowIndex}, Column={Column}, Description={Description}", rowIndex, columnName, description ?? string.Empty);
            ExtentReportManager.LogInfo($"GridActions.ClickCell - RowIndex={rowIndex}, Column={columnName}, Description={description}");

            SmartDesktopActions.Retry(() => GetCell(Resolve(gridRoot), rowIndex, columnName).Click());
        }

        public static string GetCellValue(DesktopElement gridRoot, int rowIndex, string columnName, string? description = null)
        {
            Logger.Information("GridActions.GetCellValue started. RowIndex={RowIndex}, Column={Column}, Description={Description}", rowIndex, columnName, description ?? string.Empty);
            ExtentReportManager.LogInfo($"GridActions.GetCellValue - RowIndex={rowIndex}, Column={columnName}, Description={description}");

            string value = string.Empty;
            SmartDesktopActions.Retry(() =>
            {
                value = DesktopUiQuery.ReadText(GetCell(Resolve(gridRoot), rowIndex, columnName));
            });
            return value;
        }

        public static void SetCellValue(DesktopElement gridRoot, int rowIndex, string columnName, string value, string? description = null)
        {
            Logger.Information("GridActions.SetCellValue started. RowIndex={RowIndex}, Column={Column}, Value={Value}, Description={Description}", rowIndex, columnName, value, description ?? string.Empty);
            ExtentReportManager.LogInfo($"GridActions.SetCellValue - RowIndex={rowIndex}, Column={columnName}, Value={value}, Description={description}");

            SmartDesktopActions.Retry(() =>
            {
                var cell = GetCell(Resolve(gridRoot), rowIndex, columnName);
                cell.Click();
                DesktopUiQuery.SetValue(cell, value, clearFirst: true);
            });
        }

        public static void SortColumn(DesktopElement gridRoot, string columnName, bool ascending = true, string? description = null)
        {
            Logger.Information("GridActions.SortColumn started. Column={Column}, Ascending={Ascending}, Description={Description}", columnName, ascending, description ?? string.Empty);
            ExtentReportManager.LogInfo($"GridActions.SortColumn - Column={columnName}, Ascending={ascending}, Description={description}");

            SmartDesktopActions.Retry(() =>
            {
                var header = FindHeader(Resolve(gridRoot), columnName)
                    ?? throw new InvalidOperationException($"Grid column '{columnName}' was not found.");
                header.Click();
                if (!ascending)
                {
                    header.Click();
                }
            });
        }

        public static void FilterColumn(DesktopElement gridRoot, string columnName, string filterText, string? description = null)
        {
            Logger.Information("GridActions.FilterColumn started. Column={Column}, FilterText={FilterText}, Description={Description}", columnName, filterText, description ?? string.Empty);
            ExtentReportManager.LogInfo($"GridActions.FilterColumn - Column={columnName}, FilterText={filterText}, Description={description}");

            SmartDesktopActions.Retry(() =>
            {
                var header = FindHeader(Resolve(gridRoot), columnName)
                    ?? throw new InvalidOperationException($"Grid column '{columnName}' was not found.");
                var filter = DesktopUiQuery.ByControlType(header, ControlType.Edit).FirstOrDefault()
                             ?? DesktopUiQuery.ByControlType(header, ControlType.ComboBox).FirstOrDefault();
                if (filter == null)
                {
                    header.RightClick();
                    filter = DesktopUiQuery.ByControlType(gridRoot.Session.CurrentWindow, ControlType.Edit).FirstOrDefault();
                }

                if (filter == null)
                {
                    throw new InvalidOperationException($"No filter input was found for column '{columnName}'.");
                }

                DesktopUiQuery.SetValue(filter, filterText, clearFirst: true);
            });
        }

        public static void ScrollGrid(DesktopElement gridRoot, int deltaRows, string? description = null)
        {
            Logger.Information("GridActions.ScrollGrid started. DeltaRows={DeltaRows}, Description={Description}", deltaRows, description ?? string.Empty);
            ExtentReportManager.LogInfo($"GridActions.ScrollGrid - DeltaRows={deltaRows}, Description={description}");

            SmartDesktopActions.Retry(() =>
            {
                var raw = Resolve(gridRoot);
                if (raw.Patterns.Scroll.TryGetPattern(out var scroll))
                {
                    var amount = deltaRows >= 0
                        ? FlaUI.Core.Definitions.ScrollAmount.SmallIncrement
                        : FlaUI.Core.Definitions.ScrollAmount.SmallDecrement;
                    var steps = Math.Max(1, Math.Abs(deltaRows));
                    for (var i = 0; i < steps; i++)
                    {
                        scroll.Scroll(FlaUI.Core.Definitions.ScrollAmount.NoAmount, amount);
                    }

                    return;
                }

                raw.Focus();
                var key = deltaRows >= 0
                    ? FlaUI.Core.WindowsAPI.VirtualKeyShort.DOWN
                    : FlaUI.Core.WindowsAPI.VirtualKeyShort.UP;
                for (var i = 0; i < Math.Max(1, Math.Abs(deltaRows)); i++)
                {
                    FlaUI.Core.Input.Keyboard.Type(key);
                }
            });
        }

        private static AutomationElement Resolve(DesktopElement gridRoot) =>
            SelfHealingLocatorEngine.FindElement(gridRoot.Session, gridRoot.Locator);

        private static AutomationElement GetRow(AutomationElement grid, int rowIndex)
        {
            var rows = GetRows(grid);
            if (rowIndex < 0 || rowIndex >= rows.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(rowIndex), $"Row index {rowIndex} is out of range. Total rows: {rows.Length}");
            }

            return rows[rowIndex];
        }

        private static AutomationElement[] GetRows(AutomationElement grid)
        {
            try
            {
                var typed = grid.AsGrid();
                if (typed.Rows is { Length: > 0 })
                {
                    return typed.Rows.Cast<AutomationElement>().ToArray();
                }
            }
            catch
            {
                // fall through
            }

            var dataItems = DesktopUiQuery.ByControlType(grid, ControlType.DataItem);
            if (dataItems.Length > 0)
            {
                return dataItems;
            }

            return DesktopUiQuery.ByControlType(grid, ControlType.ListItem);
        }

        private static AutomationElement[] GetCells(AutomationElement row)
        {
            try
            {
                if (row is GridRow gridRow && gridRow.Cells is { Length: > 0 })
                {
                    return gridRow.Cells.Cast<AutomationElement>().ToArray();
                }
            }
            catch
            {
                // fall through
            }

            var cells = DesktopUiQuery.ByControlType(row, ControlType.DataItem);
            if (cells.Length == 0)
            {
                cells = row.FindAllChildren() ?? Array.Empty<AutomationElement>();
            }

            return cells;
        }

        private static AutomationElement GetCell(AutomationElement grid, int rowIndex, string columnName)
        {
            var columnIndex = FindColumnIndex(grid, columnName);
            var cells = GetCells(GetRow(grid, rowIndex));
            if (columnIndex < 0 || columnIndex >= cells.Length)
            {
                throw new InvalidOperationException($"Grid cell at column '{columnName}' (index {columnIndex}) not found in row {rowIndex}.");
            }

            return cells[columnIndex];
        }

        private static int FindColumnIndex(AutomationElement grid, string columnName)
        {
            var headers = DesktopUiQuery.ByControlType(grid, ControlType.HeaderItem);
            if (headers.Length == 0)
            {
                headers = DesktopUiQuery.ByControlType(grid, ControlType.Header);
            }

            for (var i = 0; i < headers.Length; i++)
            {
                if (string.Equals(DesktopUiQuery.ReadText(headers[i]).Trim(), columnName, StringComparison.OrdinalIgnoreCase))
                {
                    return i;
                }
            }

            throw new InvalidOperationException($"Grid column '{columnName}' was not found.");
        }

        private static AutomationElement? FindHeader(AutomationElement grid, string columnName)
        {
            var headers = DesktopUiQuery.ByControlType(grid, ControlType.HeaderItem);
            if (headers.Length == 0)
            {
                headers = DesktopUiQuery.ByControlType(grid, ControlType.Header);
            }

            return headers.FirstOrDefault(h =>
                string.Equals(DesktopUiQuery.ReadText(h).Trim(), columnName, StringComparison.OrdinalIgnoreCase));
        }
    }
}
