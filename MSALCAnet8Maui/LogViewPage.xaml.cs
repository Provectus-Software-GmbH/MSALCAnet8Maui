using CsvHelper;
using System.Globalization;

namespace MSALCAnet8Maui;

public partial class LogViewPage : ContentPage
{
    private string LogsPath;
    private string LogfileCSV;

    public LogViewPage()
	{
		InitializeComponent();

        LogsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), "../Library");
        LogfileCSV = Path.Combine(LogsPath, "nlog.csv");

        ListViewLogs.ItemsSource = ReadLogfileCSV();
    }

    public IEnumerable<CSVLine> ReadLogfileCSV()
    {       
        if (!File.Exists(LogfileCSV)) return new List<CSVLine>();

        using var reader = new StreamReader(LogfileCSV);
        using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
        return csv.GetRecords<CSVLine>().ToList().OrderByDescending(o => o.time).Take(1000);        
    }

    private async void ListViewLogs_ItemTapped(object sender, ItemTappedEventArgs e)
    {     
        if (e.Item == null) return;        
        if (sender is ListView lv) lv.SelectedItem = null;
        
        CSVLine thisItem = e.Item as CSVLine;

        await DisplayAlert(thisItem.level, $"{thisItem.logger}\n{thisItem.time.ToString("dd.MM.yyyy HH: mm:ss")}\n\n{thisItem.message}", "Dismiss");
    }

    private async void ToolbarItem_Clicked(object sender, EventArgs e)
    {
        await ShareLogfiles();
    }

    private async Task ShareLogfiles()
    {   
        if (!File.Exists(LogfileCSV)) return;

        // add current log file
        List<ShareFile> LogfileToShare = new()
        {
            new ShareFile(LogfileCSV)
        };

        // get the latest archive zip file
        DirectoryInfo info = new DirectoryInfo(LogsPath);
        FileInfo[] files = info.GetFiles().OrderByDescending(p => p.CreationTime).ToArray();
        foreach (FileInfo f in files)
            if (f.Name.StartsWith("nlog-") && f.Name.EndsWith("zip"))
            {
                // add latest archive zip file
                LogfileToShare.Add(new ShareFile(Path.Combine(LogsPath, f.Name)));
                break;
            }

        // share logs
        await Share.RequestAsync(new ShareMultipleFilesRequest
        {
            Title = "App Logs",
            Files = LogfileToShare
        });
    }
}

public class CSVLine
{    
    public DateTime time { get; set; }
    public string logger { get; set; }
    public string level { get; set; }
    public string machinename { get; set; }
    public string appdomain { get; set; }
    public string processid { get; set; }
    public string processname { get; set; }
    public string threadid { get; set; }
    public string message { get; set; }
    public string stacktrace { get; set; }
}