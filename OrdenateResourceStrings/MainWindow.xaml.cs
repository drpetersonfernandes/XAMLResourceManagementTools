using System.IO;
using System.Windows;
using System.Windows.Forms;

namespace OrdenateResourceStrings;

public partial class MainWindow
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private void SelectInputFolderButton_Click(object sender, RoutedEventArgs e)
    {
        using var dialog = new FolderBrowserDialog();
        if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
        {
            InputFolderTextBox.Text = dialog.SelectedPath;
        }
    }

    private void SelectOutputFolderButton_Click(object sender, RoutedEventArgs e)
    {
        using var dialog = new FolderBrowserDialog();
        if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
        {
            OutputFolderTextBox.Text = dialog.SelectedPath;
        }
    }

    private void ProcessFilesButton_Click(object sender, RoutedEventArgs e)
    {
        var inputFolder = InputFolderTextBox.Text;
        var outputFolder = OutputFolderTextBox.Text;

        if (string.IsNullOrWhiteSpace(inputFolder) || !Directory.Exists(inputFolder))
        {
            System.Windows.MessageBox.Show("Please select a valid input folder.", "Error", MessageBoxButton.OK,
                MessageBoxImage.Error);
            return;
        }

        if (string.IsNullOrWhiteSpace(outputFolder) || !Directory.Exists(outputFolder))
        {
            System.Windows.MessageBox.Show("Please select a valid output folder.", "Error", MessageBoxButton.OK,
                MessageBoxImage.Error);
            return;
        }

        try
        {
            var files = Directory.GetFiles(inputFolder, "*.xaml");

            foreach (var file in files)
            {
                // Read all lines of the file
                var lines = File.ReadAllLines(file).ToList();

                // Separate header, resource strings, and footer
                var headerLines = lines.TakeWhile(line => !line.Contains("<system:String x:Key=")).ToList();
                var resourceLines = lines.SkipWhile(line => !line.Contains("<system:String x:Key="))
                    .TakeWhile(line => !line.Trim().StartsWith("</ResourceDictionary>", StringComparison.Ordinal))
                    .ToList();
                var footerLines = lines.Skip(headerLines.Count + resourceLines.Count).ToList();


                // Sort resource lines
                var sortedResourceLines = resourceLines.OrderBy(ExtractKey).ToList();

                // Combine all lines
                var outputLines = headerLines.Concat(sortedResourceLines).Concat(footerLines).ToList();


                // Write the output to the new file
                var outputFile = Path.Combine(outputFolder, Path.GetFileName(file));
                File.WriteAllLines(outputFile, outputLines);
            }

            System.Windows.MessageBox.Show("Files processed successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"An error occurred: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private static string ExtractKey(string line)
    {
        var start = line.IndexOf("x:Key=\"", StringComparison.Ordinal);

        if (start == -1)
        {
            // Handle cases where x:Key is not in the expected format
            // You might want to log this or throw a more specific exception
            return ""; // Or some other default value
        }

        start += 7; // Move past "x:Key=\""

        var end = line.IndexOf("\"", start, StringComparison.Ordinal);

        if (end == -1)
        {
            // Handle the missing closing quote
            return ""; // Or handle differently
        }

        return line.Substring(start, end - start);
    }
}