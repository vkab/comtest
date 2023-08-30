global using Microsoft.Extensions.Configuration;
global using TestCom.Resources;
using TestCom;

try
{
    using var task = await ReadingTask.CreateInstanceAsync();
    await task.StartWorkAsync();
    await Task.Delay(60_000);
    task.StopWork();
}
catch(Exception ex)
{
    Console.WriteLine(ex.Message);
}