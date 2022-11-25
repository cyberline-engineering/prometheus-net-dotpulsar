using System.Diagnostics;
using System.Diagnostics.Metrics;
using Prometheus;

AppDomain.CurrentDomain.FirstChanceException += (sender, eventArgs) =>
{
    Debug.WriteLine(eventArgs.Exception.ToString());
};

using var server = new KestrelMetricServer(port: 1234);
server.Start();

// Generate some sample data from fake business logic.
var recordsProcessed = Metrics.CreateCounter("sample_records_processed_total", "Total number of records processed.");

_ = Task.Run(async delegate
{
    while (true)
    {
        // Pretend to process a record approximately every second, just for changing sample data.
        recordsProcessed.Inc();
        await Task.Delay(TimeSpan.FromSeconds(1));
    }
});

// Metrics published in this sample:
// * built-in process metrics giving basic information about the .NET runtime (enabled by default)
// * metrics from .NET Event Counters (enabled by default)
// * metrics from .NET Meters (enabled by default)
// * the custom sample counter defined above
Console.WriteLine("Open http://localhost:1234/metrics in a web browser.");
Console.WriteLine("Press enter to exit.");

Counters.Inc();

Console.ReadLine();

internal static class Counters
{
    static int counter;
    private static readonly Meter m;

    public static int Inc()
    {
        return Interlocked.Increment(ref counter);
    }

    static Counters()
    {
        m = new Meter("prometheus-dotpulsar", "1.0.0");
    
        _ = m.CreateObservableGauge("dotpulsar.client.count", Inc, "{clients}", "Number of clients");
        _ = m.CreateObservableGauge("dotpulsar.connection.count", Inc, "{connections}", "Number of connections");
        _ = m.CreateObservableGauge("dotpulsar.reader.count", Inc, "{readers}", "Number of readers");
        _ = m.CreateObservableGauge("dotpulsar.consumer.count", Inc, "{consumers}", "Number of consumers");
        _ = m.CreateObservableGauge("dotpulsar.producer.count", Inc, "{producers}", "Number of producers");
        _ = m.CreateHistogram<double>("dotpulsar.producer.send.duration", "ms", "Measures the duration for sending a message");
        _ = m.CreateHistogram<double>("dotpulsar.consumer.process.duration", "ms", "Measures the duration for processing a message");
    }
}