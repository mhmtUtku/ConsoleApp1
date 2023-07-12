using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.ComponentModel;
using System.Net;
using System.Net.Http.Headers;

var builder = new ConfigurationBuilder();
builder.SetBasePath(Directory.GetCurrentDirectory()).AddJsonFile("Input.json", optional: false, reloadOnChange: true);
IConfiguration config = builder.Build();

var totalCount = Convert.ToInt32(config.GetSection("Count").Value);
var parallelism = Convert.ToInt32(config.GetSection("Parallelism").Value);
var path = config.GetSection("SavePath").Value;

var keepRunning = true;

var imageHandlers = GetImageHandlers(totalCount);



WebClient webClient = new WebClient();

using HttpClient httpClient = new()
{
    BaseAddress = new Uri("https://picsum.photos"),
};
httpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("AdCreative", "1"));

ParallelOptions options = new()
{
    MaxDegreeOfParallelism = parallelism
};

IEnumerable<int> numbers = Enumerable.Range(1, totalCount);
IEnumerable<int[]> chunks = numbers.Chunk(parallelism);

Console.WriteLine($"Downloading {totalCount} images ({parallelism} parallel downloads at most)\n");

Console.CancelKeyPress += delegate (object? sender, ConsoleCancelEventArgs e) {
    e.Cancel = true;
    keepRunning = false;
};

bool exists = Directory.Exists(path);
if (!exists)
    Directory.CreateDirectory(path);

var count = 0;
//var fileNameList = new List<string>();
foreach (int[] chunk in chunks)
{
    if (keepRunning)
    {
        var chunkImageHandlers = imageHandlers.Skip(chunk.First() - 1).Take(parallelism).ToList();

        await Parallel.ForEachAsync(chunkImageHandlers, options, async (uri, token) =>
        {
            var adr = new Uri("https://picsum.photos/" + uri);

            using (var webClient = new WebClient())
            {
                var currentCount = Interlocked.Increment(ref count);
                var name = $"{currentCount}.png";

                //fileNameList.Add(name);
                //webClient.DownloadFileCompleted += new AsyncCompletedEventHandler(DoSomethingOnFinish);

                webClient.DownloadFileAsync(adr, path + name);
                while (webClient.IsBusy) { }
            }
        });

        Console.Write("\r{0}   ", $"Progress: {chunk.Last()}/{totalCount}");
    }
    else
    {
        DirectoryInfo di = new DirectoryInfo(path);
        foreach (FileInfo file in di.GetFiles())
            file.Delete();
    }
}

List<string> GetImageHandlers(int totalCount)
{
    var imageList = new List<string>();
    for (int i = 0; i < totalCount; i++)
    {
        imageList.Add("200");
    }

    return imageList;
}


//void DoSomethingOnFinish(object sender, AsyncCompletedEventArgs args)
//{
//    var progressCountVal = fileNameList.Count;
//    Console.Write("\r{0}   ", $"Progress: {progressCountVal}/{totalCount}");
//}