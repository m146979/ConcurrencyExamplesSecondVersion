using System;
using System.Threading.Tasks.Dataflow;

class Program
{
    static async Task Main(string[] args)
    {
        int[] inputData = GetInputData();

        //await LinkingBlocksExample.Run(inputData);
        //await PropagatingErrorsExample.Run(inputData);
        //await UnlinkingBlocksExample.Run(inputData);
        //await ThrottlingBlocksExample.Run(inputData);
        //await ParallelProcessingExample.Run(inputData);
        await CustomBlockExample.Run(inputData);
        //await CompletionHandlingExample.Run(inputData);
    }

    static int[] GetInputData()
    {

        Console.WriteLine("Enter numbers separated by commas:");
        return Console.ReadLine().Split(',').Select(int.Parse).ToArray();
    }
}


public class LinkingBlocksExample
{
    public static async Task Run(int[] data)
    {
        var transformBlock = new TransformBlock<int, string>(i => $"Number: {i}");
        var actionBlock = new ActionBlock<string>(s => Console.WriteLine(s));

        transformBlock.LinkTo(actionBlock, new DataflowLinkOptions { PropagateCompletion = true });

        foreach (var item in data)
        {
            await transformBlock.SendAsync(item);
        }

        transformBlock.Complete();
        await actionBlock.Completion;
    }
}

public class PropagatingErrorsExample
{
    public static async Task Run(int[] data)
    {
        var transformBlock = new TransformBlock<int, int>(i =>
        {
            if (i == 3) throw new Exception("Error at 3");
            return i * 2;
        });

        var actionBlock = new ActionBlock<int>(i => Console.WriteLine(i));

        transformBlock.LinkTo(actionBlock, new DataflowLinkOptions { PropagateCompletion = true });

        foreach (var item in data)
        {
            transformBlock.Post(item);
        }

        transformBlock.Complete();

        try
        {
            await actionBlock.Completion;
        }
        catch (AggregateException ex)
        {
            Console.WriteLine($"Caught error: {ex.InnerExceptions[0].Message}");
        }
    }
}


public class ThrottlingBlocksExample
{
    public static async Task Run(int[] data)
    {
        var throttledBlock = new TransformBlock<int, int>(i => i * 2,
            new ExecutionDataflowBlockOptions { BoundedCapacity = 2 });

        var actionBlock = new ActionBlock<int>(i =>
        {
            Console.WriteLine($"Processed by action 1: {i}");
            Task.Delay(1000).Wait(); // Simulate work
        });
        var actionBlock2 = new ActionBlock<int>(i =>
        {
            Console.WriteLine($"Processed by action 2: {i}");
            Task.Delay(1000).Wait(); // Simulate work
        });

        throttledBlock.LinkTo(actionBlock);
        throttledBlock.LinkTo(actionBlock2);

        foreach (var item in data)
        {
            await throttledBlock.SendAsync(item);
        }

        throttledBlock.Complete();
        await actionBlock.Completion;
        await actionBlock2.Completion;
    }
}

public class ParallelProcessingExample
{
    public static async Task Run(int[] data)
    {
        var parallelBlock = new TransformBlock<int, int>(i =>
        {
            Console.WriteLine($"Processing {i} on thread {Thread.CurrentThread.ManagedThreadId}");
            return i * 2;
        }, new ExecutionDataflowBlockOptions { MaxDegreeOfParallelism = Environment.ProcessorCount });

        var actionBlock = new ActionBlock<int>(i => Console.WriteLine($"Result: {i} on thread {Thread.CurrentThread.ManagedThreadId}")
        , new ExecutionDataflowBlockOptions { MaxDegreeOfParallelism = Environment.ProcessorCount });

        parallelBlock.LinkTo(actionBlock);

        foreach (var item in data)
        {
            parallelBlock.Post(item);
        }

        parallelBlock.Complete();
        await actionBlock.Completion;
    }
}
public class CustomBlockExample
{
    public static async Task Run(int[] data)
    {
        // Custom block that filters for even numbers, doubles them, and sums them up
        var customBlock = MyCustomBlock();

        // Action block to output the final sum
        var actionBlock = new ActionBlock<int>(sum => Console.WriteLine($"Sum of even numbers: {sum}"));

        // Link our custom block to the action block
        customBlock.LinkTo(actionBlock, new DataflowLinkOptions { PropagateCompletion = true });

        // Send data to our custom block
        foreach (var item in data)
        {
            customBlock.Post(item);
        }

        // Signal that we're done sending data
        customBlock.Complete();

        // Wait for the pipeline to complete
        await actionBlock.Completion;
    }

    private static IPropagatorBlock<int, int> MyCustomBlock()
    {
        // Filter for even numbers
        var filterBlock = new TransformBlock<int, int>(item =>
        {
            if (item % 2 == 0)
            {
                Console.WriteLine($"Filtering: {item} is even.");
                return item;
            }
            return -1; // Odd numbers are discarded by returning -1
        });

        // Transform: Double the even numbers
        var transformBlock = new TransformBlock<int, int>(item =>
        {
            if (item != -1) // Only process even numbers
            {
                int doubled = item * 2;
                Console.WriteLine($"Transforming: {item} to {doubled}");
                return doubled;
            }
            return -1; // Pass through the discard signal
        });

        // Aggregate: Sum up all transformed numbers
        int sum = 0;
        var aggregateBlock = new ActionBlock<int>(item =>
        {
            if (item != -1) // Only sum non-discarded items
            {
                sum += item;
                Console.WriteLine($"Aggregating: Current sum = {sum}");
            }
        });

        // Link blocks together
        var linkOptions = new DataflowLinkOptions { PropagateCompletion = true };
        filterBlock.LinkTo(transformBlock, linkOptions);
        transformBlock.LinkTo(aggregateBlock, linkOptions);

        // Encapsulate our custom logic into one block
        return DataflowBlock.Encapsulate(filterBlock, transformBlock);
    }
}
public class CompletionHandlingExample
{
    public static async Task Run(int[] data)
    {
        var block1 = new TransformBlock<int, int>(i => i * 2);
        var block2 = new TransformBlock<int, int>(i => i + 1);
        var finalBlock = new ActionBlock<int>(i => Console.WriteLine(i));

        block1.LinkTo(block2, new DataflowLinkOptions { PropagateCompletion = true });
        block2.LinkTo(finalBlock, new DataflowLinkOptions { PropagateCompletion = true });

        foreach (var item in data)
        {
            block1.Post(item);
        }

        block1.Complete();

        // Wait for the final block to complete
        await finalBlock.Completion;
    }

}
public class UnlinkingBlocksExample
{
    public static async Task Run(int[] data)
    {
        // Use of IDisposable to manage the link
        IDisposable linkToActionBlock2 = null;

        var broadcastBlock = new BroadcastBlock<int>(i => i);
        var actionBlock1 = new ActionBlock<int>(i => Console.WriteLine($"Block 1: {i}"));
        var actionBlock2 = new ActionBlock<int>(i => Console.WriteLine($"Block 2: {i}"));

        // Link to actionBlock1
        broadcastBlock.LinkTo(actionBlock1);

        // Link to actionBlock2 and keep the disposable link
        linkToActionBlock2 = broadcastBlock.LinkTo(actionBlock2);

        for (int i = 0; i < data.Length; i++)
        {
            if (i == data.Length / 2) // Halfway through, unlink actionBlock2
            {
                // Unlink actionBlock2
                linkToActionBlock2.Dispose();
            }
            broadcastBlock.Post(data[i]);
        }

        broadcastBlock.Complete();
        await Task.WhenAll(actionBlock1.Completion, actionBlock2.Completion);
    }
}