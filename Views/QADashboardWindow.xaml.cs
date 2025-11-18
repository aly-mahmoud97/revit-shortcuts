using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using RevitShortcuts.Services;
using System.IO;
using System.Text;

namespace RevitShortcuts.Views
{
    public partial class QADashboardWindow : Window
    {
        private readonly Document _doc;
        private readonly UIDocument _uiDoc;
        private List<QACheckCategory> _categories;

        public QADashboardWindow(UIDocument uiDoc)
        {
            InitializeComponent();
            _uiDoc = uiDoc;
            _doc = uiDoc.Document;

            ProjectNameText.Text = $"Project: {_doc.Title}";
            RunQAChecks();
        }

        private void RunQAChecks()
        {
            try
            {
                StatusText.Text = "Running QA checks...";
                RefreshButton.IsEnabled = false;

                var qaService = new QACheckService(_doc);
                _categories = qaService.RunAllChecks();

                DisplayResults();

                StatusText.Text = $"QA checks completed at {DateTime.Now:HH:mm:ss}";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error running QA checks: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                StatusText.Text = "Error running checks";
            }
            finally
            {
                RefreshButton.IsEnabled = true;
            }
        }

        private void DisplayResults()
        {
            // Clear existing content
            SummaryPanel.Children.Clear();
            CategoriesPanel.Children.Clear();

            // Calculate totals
            int totalChecks = _categories.Sum(c => c.Results.Count);
            int totalPassed = _categories.Sum(c => c.PassedCount);
            int totalFailed = _categories.Sum(c => c.FailedCount);
            int totalIssues = _categories.Sum(c => c.TotalIssues);

            // Create summary cards
            CreateSummaryCard("Total Checks", totalChecks.ToString(), "#3498DB");
            CreateSummaryCard("Passed", totalPassed.ToString(), "#27AE60");
            CreateSummaryCard("Failed", totalFailed.ToString(), "#E74C3C");
            CreateSummaryCard("Total Issues", totalIssues.ToString(), "#F39C12");

            // Create category sections
            foreach (var category in _categories)
            {
                CreateCategorySection(category);
            }
        }

        private void CreateSummaryCard(string title, string value, string color)
        {
            var card = new Border
            {
                Background = new SolidColorBrush((System.Windows.Media.Color)ColorConverter.ConvertFromString(color)),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(20, 15, 20, 15),
                Margin = new Thickness(10, 0, 10, 0),
                MinWidth = 150
            };

            var stack = new StackPanel();

            var titleText = new TextBlock
            {
                Text = title,
                FontSize = 12,
                Foreground = new SolidColorBrush(Colors.White),
                Opacity = 0.9,
                HorizontalAlignment = HorizontalAlignment.Center
            };

            var valueText = new TextBlock
            {
                Text = value,
                FontSize = 28,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Colors.White),
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 5, 0, 0)
            };

            stack.Children.Add(titleText);
            stack.Children.Add(valueText);
            card.Child = stack;

            SummaryPanel.Children.Add(card);
        }

        private void CreateCategorySection(QACheckCategory category)
        {
            var categoryCard = new Border
            {
                Background = new SolidColorBrush(Colors.White),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(20),
                Margin = new Thickness(10, 10, 10, 10)
            };

            var shadowEffect = new System.Windows.Media.Effects.DropShadowEffect
            {
                BlurRadius = 10,
                ShadowDepth = 2,
                Opacity = 0.1
            };
            categoryCard.Effect = shadowEffect;

            var mainStack = new StackPanel();

            // Category header
            var headerPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(0, 0, 0, 15)
            };

            var categoryName = new TextBlock
            {
                Text = category.CategoryName,
                FontSize = 18,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush((System.Windows.Media.Color)ColorConverter.ConvertFromString("#2C3E50"))
            };

            var statsText = new TextBlock
            {
                Text = $"  ({category.PassedCount} passed, {category.FailedCount} failed)",
                FontSize = 13,
                Foreground = new SolidColorBrush((System.Windows.Media.Color)ColorConverter.ConvertFromString("#7F8C8D")),
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(10, 0, 0, 0)
            };

            headerPanel.Children.Add(categoryName);
            headerPanel.Children.Add(statsText);
            mainStack.Children.Add(headerPanel);

            // Add separator
            var separator = new Border
            {
                Height = 1,
                Background = new SolidColorBrush((System.Windows.Media.Color)ColorConverter.ConvertFromString("#ECF0F1")),
                Margin = new Thickness(0, 0, 0, 15)
            };
            mainStack.Children.Add(separator);

            // Add check results
            foreach (var result in category.Results)
            {
                CreateCheckResultItem(mainStack, result);
            }

            categoryCard.Child = mainStack;
            CategoriesPanel.Children.Add(categoryCard);
        }

        private void CreateCheckResultItem(StackPanel parent, QACheckResult result)
        {
            var itemPanel = new Border
            {
                Background = new SolidColorBrush((System.Windows.Media.Color)ColorConverter.ConvertFromString("#F8F9FA")),
                CornerRadius = new CornerRadius(5),
                Padding = new Thickness(15, 12, 15, 12),
                Margin = new Thickness(0, 0, 0, 10)
            };

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var contentStack = new StackPanel { Margin = new Thickness(0, 0, 15, 0) };

            // Check name with status icon
            var namePanel = new StackPanel { Orientation = Orientation.Horizontal };

            var statusIcon = new TextBlock
            {
                Text = result.Passed ? "✓" : "✗",
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(result.Passed ? Colors.Green : Colors.Red),
                Margin = new Thickness(0, 0, 10, 0),
                VerticalAlignment = VerticalAlignment.Center
            };

            var checkName = new TextBlock
            {
                Text = result.CheckName,
                FontSize = 14,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush((System.Windows.Media.Color)ColorConverter.ConvertFromString("#2C3E50")),
                VerticalAlignment = VerticalAlignment.Center
            };

            namePanel.Children.Add(statusIcon);
            namePanel.Children.Add(checkName);
            contentStack.Children.Add(namePanel);

            // Message
            var messageText = new TextBlock
            {
                Text = result.Message,
                FontSize = 12,
                Foreground = new SolidColorBrush((System.Windows.Media.Color)ColorConverter.ConvertFromString("#555555")),
                Margin = new Thickness(26, 5, 0, 0),
                TextWrapping = TextWrapping.Wrap
            };
            contentStack.Children.Add(messageText);

            // Details (if any)
            if (!string.IsNullOrEmpty(result.Details) && result.Details.Length > 0)
            {
                var detailsText = new TextBlock
                {
                    Text = result.Details.Length > 200 ? result.Details.Substring(0, 200) + "..." : result.Details,
                    FontSize = 11,
                    Foreground = new SolidColorBrush((System.Windows.Media.Color)ColorConverter.ConvertFromString("#7F8C8D")),
                    Margin = new Thickness(26, 5, 0, 0),
                    TextWrapping = TextWrapping.Wrap,
                    FontStyle = FontStyles.Italic
                };
                contentStack.Children.Add(detailsText);
            }

            Grid.SetColumn(contentStack, 0);
            grid.Children.Add(contentStack);

            // Severity badge
            var severityPanel = new StackPanel { VerticalAlignment = VerticalAlignment.Top };

            var severityBadge = new Border
            {
                CornerRadius = new CornerRadius(3),
                Padding = new Thickness(8, 4, 8, 4),
                Background = GetSeverityColor(result.Severity)
            };

            var severityText = new TextBlock
            {
                Text = result.Severity.ToString(),
                FontSize = 10,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(Colors.White)
            };

            severityBadge.Child = severityText;
            severityPanel.Children.Add(severityBadge);

            // Show affected elements button if there are any
            if (result.AffectedElements != null && result.AffectedElements.Count > 0)
            {
                var showButton = new Button
                {
                    Content = $"Show ({result.AffectedElements.Count})",
                    Margin = new Thickness(0, 5, 0, 0),
                    Padding = new Thickness(8, 3, 8, 3),
                    FontSize = 10,
                    Background = new SolidColorBrush((System.Windows.Media.Color)ColorConverter.ConvertFromString("#3498DB")),
                    Foreground = new SolidColorBrush(Colors.White),
                    BorderThickness = new Thickness(0),
                    Cursor = System.Windows.Input.Cursors.Hand,
                    Tag = result.AffectedElements
                };

                showButton.Click += ShowElementsButton_Click;
                severityPanel.Children.Add(showButton);
            }

            Grid.SetColumn(severityPanel, 1);
            grid.Children.Add(severityPanel);

            itemPanel.Child = grid;
            parent.Children.Add(itemPanel);
        }

        private SolidColorBrush GetSeverityColor(IssueSeverity severity)
        {
            switch (severity)
            {
                case IssueSeverity.Critical:
                    return new SolidColorBrush((System.Windows.Media.Color)ColorConverter.ConvertFromString("#C0392B"));
                case IssueSeverity.Error:
                    return new SolidColorBrush((System.Windows.Media.Color)ColorConverter.ConvertFromString("#E74C3C"));
                case IssueSeverity.Warning:
                    return new SolidColorBrush((System.Windows.Media.Color)ColorConverter.ConvertFromString("#F39C12"));
                case IssueSeverity.Info:
                    return new SolidColorBrush((System.Windows.Media.Color)ColorConverter.ConvertFromString("#3498DB"));
                default:
                    return new SolidColorBrush((System.Windows.Media.Color)ColorConverter.ConvertFromString("#95A5A6"));
            }
        }

        private void ShowElementsButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var button = sender as Button;
                if (button?.Tag is List<ElementId> elementIds && elementIds.Count > 0)
                {
                    _uiDoc.Selection.SetElementIds(elementIds);
                    _uiDoc.ShowElements(elementIds);

                    StatusText.Text = $"Selected {elementIds.Count} element(s) in the model";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error selecting elements: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            RunQAChecks();
        }

        private void ExportButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var saveDialog = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "Text File (*.txt)|*.txt|CSV File (*.csv)|*.csv",
                    FileName = $"QA_Report_{_doc.Title}_{DateTime.Now:yyyyMMdd_HHmmss}"
                };

                if (saveDialog.ShowDialog() == true)
                {
                    ExportReport(saveDialog.FileName, saveDialog.FilterIndex);
                    MessageBox.Show($"Report exported successfully to:\n{saveDialog.FileName}",
                        "Export Complete", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error exporting report: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExportReport(string filePath, int filterIndex)
        {
            var sb = new StringBuilder();

            // Header
            sb.AppendLine("QA & Quality Checks Report");
            sb.AppendLine("=" + new string('=', 50));
            sb.AppendLine($"Project: {_doc.Title}");
            sb.AppendLine($"Date: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"User: {_doc.Application.Username}");
            sb.AppendLine();

            // Summary
            int totalChecks = _categories.Sum(c => c.Results.Count);
            int totalPassed = _categories.Sum(c => c.PassedCount);
            int totalFailed = _categories.Sum(c => c.FailedCount);
            int totalIssues = _categories.Sum(c => c.TotalIssues);

            sb.AppendLine("SUMMARY");
            sb.AppendLine("-" + new string('-', 50));
            sb.AppendLine($"Total Checks: {totalChecks}");
            sb.AppendLine($"Passed: {totalPassed}");
            sb.AppendLine($"Failed: {totalFailed}");
            sb.AppendLine($"Total Issues: {totalIssues}");
            sb.AppendLine();

            // Detailed results
            foreach (var category in _categories)
            {
                sb.AppendLine();
                sb.AppendLine($"{category.CategoryName.ToUpper()}");
                sb.AppendLine("=" + new string('=', 50));
                sb.AppendLine($"Passed: {category.PassedCount} | Failed: {category.FailedCount} | Issues: {category.TotalIssues}");
                sb.AppendLine();

                foreach (var result in category.Results)
                {
                    sb.AppendLine($"  [{(result.Passed ? "PASS" : "FAIL")}] {result.CheckName}");
                    sb.AppendLine($"    Severity: {result.Severity}");
                    sb.AppendLine($"    {result.Message}");

                    if (!string.IsNullOrEmpty(result.Details))
                    {
                        sb.AppendLine($"    Details: {result.Details}");
                    }

                    if (result.AffectedElements != null && result.AffectedElements.Count > 0)
                    {
                        sb.AppendLine($"    Affected Elements: {result.AffectedElements.Count}");
                        sb.AppendLine($"    Element IDs: {string.Join(", ", result.AffectedElements.Take(20).Select(id => id.IntegerValue))}");
                    }

                    sb.AppendLine();
                }
            }

            File.WriteAllText(filePath, sb.ToString());
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
