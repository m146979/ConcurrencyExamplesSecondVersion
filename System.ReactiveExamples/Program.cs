using System.Reactive.Concurrency;
using System.Reactive.Linq;

class Program
{
    static void Main(string[] args)
    {
        // Convert .NET Events to Observable (simulated with Console.ReadKey)
        ConvertEventsToObservable();

        // Sending Notifications to a Context (simulated with current thread context)
        SendNotificationsToContext();

        // Grouping Event Data with Windows and Buffers
        GroupEvents();

        // Taming Event Streams with Throttling and Sampling
        ControlEventRate();

        // Timeouts
        HandleTimeouts();

        // Deferred Evaluation
        DeferredEvaluation();

        Console.WriteLine("Press any key to exit...");
        Console.ReadKey();
    }

    static void ConvertEventsToObservable()
    {
        Console.WriteLine("Convert .NET Events to Observable:");
        var observable = Observable.FromEventPattern<ConsoleCancelEventHandler, ConsoleCancelEventArgs>(
            h => Console.CancelKeyPress += h,
            h => Console.CancelKeyPress -= h
        );
        observable.Subscribe(args => Console.WriteLine("Ctrl+C pressed!"));

        Console.WriteLine("Press Ctrl+C to simulate an event...");
        // Keep the method running until Ctrl+C is pressed
        while (true)
        {
            if (Console.KeyAvailable)
            {
                var key = Console.ReadKey(true);
                if (key.Key == ConsoleKey.C && (key.Modifiers & ConsoleModifiers.Control) != 0)
                    break;
            }
            Thread.Sleep(100); // Small delay to not block CPU
        }
    }

    static void SendNotificationsToContext()
    {
        Console.WriteLine("Sending Notifications to Context:");
        var observable = Observable.Interval(TimeSpan.FromSeconds(1));
        observable.ObserveOn(Scheduler.CurrentThread)
                  .Subscribe(x => Console.WriteLine($"Time elapsed: {x} seconds"));


        Thread.Sleep(5000); // Let it run for 5 seconds
    }

    static void GroupEvents()
    {
        Console.WriteLine("Grouping Event Data:");
        var source = Observable.Interval(TimeSpan.FromSeconds(1));

        // Buffer every 3 events
        source.Buffer(3).Subscribe(buffer => Console.WriteLine($"Buffered: {string.Join(", ", buffer)}"));

        // Window for 5 seconds
        source.Window(TimeSpan.FromSeconds(5)).Subscribe(window =>
        {
            window.Subscribe(x => Console.WriteLine($"In Window: {x}"));
        });

        Thread.Sleep(15000); // Let it run for 15 seconds to see some windows and buffers
    }

    static void ControlEventRate()
    {
        Console.WriteLine("Taming Event Streams:");
        var source = Observable.Interval(TimeSpan.FromMilliseconds(100));

        // Throttle: Only process events if no new event arrives within 500ms
        source.Throttle(TimeSpan.FromMilliseconds(500)).Subscribe(x => Console.WriteLine($"Throttled: {x}"));

        // Sample: Emit the last value in each 500ms window
        source.Sample(TimeSpan.FromMilliseconds(500)).Subscribe(x => Console.WriteLine($"Sampled: {x}"));

        Thread.Sleep(5000); // Let it run for 5 seconds
    }

    static void HandleTimeouts()
    {
        Console.WriteLine("Handling Timeouts:");
        var source = Observable.Interval(TimeSpan.FromSeconds(3));
        source.Timeout(TimeSpan.FromSeconds(2))
              .Subscribe(
                  x => Console.WriteLine($"Received: {x}"),
                  ex => Console.WriteLine("Operation timed out")
              );

        Thread.Sleep(5000); // Wait for a timeout to occur
    }

    static void DeferredEvaluation()
    {
        Console.WriteLine("Deferred Evaluation:");
        var deferredObservable = Observable.Defer(() =>
            Observable.Return(DateTime.Now)
        );

        // This will be different each time it's subscribed
        deferredObservable.Subscribe(t => Console.WriteLine($"First Subscription: {t}"));
        Thread.Sleep(1000); // Simulate some time passing
        deferredObservable.Subscribe(t => Console.WriteLine($"Second Subscription: {t}"));
    }
}