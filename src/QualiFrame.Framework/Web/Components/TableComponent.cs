using OpenQA.Selenium;
using QualiFrame.Framework.Utilities.Logging;
using System;
using System.Collections.Generic;
using System.Linq;

namespace QualiFrame.Framework.Web.Components
{
    /// <summary>
    /// Reusable table component for web pages.
    /// </summary>
    public class TableComponent : BaseComponent
    {
        private readonly By _tableLocator;
        private readonly By _headerLocator;
        private readonly By _rowLocator;
        private readonly By _cellLocator;

        public TableComponent(
            IWebDriver driver,
            By tableLocator = null!,
            By headerLocator = null!,
            By rowLocator = null!,
            By cellLocator = null!)
            : base(driver)
        {
            _tableLocator = tableLocator ?? By.TagName("table");
            _headerLocator = headerLocator ?? By.TagName("th");
            _rowLocator = rowLocator ?? By.TagName("tr");
            _cellLocator = cellLocator ?? By.TagName("td");
        }

        public override bool IsVisible()
        {
            try
            {
                return FindElement(_tableLocator).IsDisplayed();
            }
            catch
            {
                return false;
            }
        }

        public override void WaitForVisible(int timeoutSeconds = 10)
        {
            try
            {
                FindElement(_tableLocator).WaitForVisible(timeoutSeconds);
                Logger.Debug("Table component is now visible");
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to wait for table component visibility");
                throw;
            }
        }

        public List<string> GetHeaders()
        {
            var headers = new List<string>();
            try
            {
                var headerElements = FindElements(_headerLocator);
                foreach (var header in headerElements)
                {
                    headers.Add(header.GetText());
                }
            }
            catch (Exception ex)
            {
                Logger.Warning("Error getting table headers: {Error}", ex.Message);
            }
            return headers;
        }

        public int GetRowCount()
        {
            try
            {
                var rows = FindElements(_rowLocator);
                return rows.Count;
            }
            catch
            {
                return 0;
            }
        }

        public List<Dictionary<string, string>> GetTableData()
        {
            var tableData = new List<Dictionary<string, string>>();
            try
            {
                var headers = GetHeaders();
                var rows = FindElements(_rowLocator);

                foreach (var row in rows.Skip(1))
                {
                    var rowData = new Dictionary<string, string>();
                    var cells = row.Element.FindElements(_cellLocator);

                    for (int i = 0; i < Math.Min(headers.Count, cells.Count); i++)
                    {
                        rowData[headers[i]] = cells[i].Text;
                    }

                    tableData.Add(rowData);
                }
            }
            catch (Exception ex)
            {
                Logger.Warning("Error getting table data: {Error}", ex.Message);
            }
            return tableData;
        }

        public int FindRowByCellValue(string columnName, string value)
        {
            try
            {
                var tableData = GetTableData();
                for (int i = 0; i < tableData.Count; i++)
                {
                    if (tableData[i].TryGetValue(columnName, out var cellValue) && cellValue == value)
                    {
                        return i;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Warning("Error finding row: {Error}", ex.Message);
            }
            return -1;
        }

        public string GetCellValue(int rowIndex, string columnName)
        {
            try
            {
                var tableData = GetTableData();
                if (rowIndex >= 0 && rowIndex < tableData.Count)
                {
                    if (tableData[rowIndex].TryGetValue(columnName, out var value))
                    {
                        return value;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Warning("Error getting cell value: {Error}", ex.Message);
            }
            return string.Empty;
        }
    }
}
