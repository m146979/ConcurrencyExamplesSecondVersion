using System;
using System.Diagnostics;
using System.Net;
using System.Reactive;
using System.Reactive.Linq;
using System.Windows.Forms;

namespace RxWindowsFormApp
{
    public class MainForm : Form
    {
        public static Button startButton;
        private TextBox outputTextBox;
        public static SynchronizationContext uiContext;
        public MainForm()
        {
            uiContext = SynchronizationContext.Current;
            InitializeComponents();
            if (uiContext is not null)
                SetupEvents();
        }

        private void InitializeComponents()
        {
            startButton = new Button { Text = "Start", Location = new System.Drawing.Point(10, 10) };
            outputTextBox = new TextBox { Multiline = true, ScrollBars = ScrollBars.Vertical, Location = new System.Drawing.Point(10, 40), Size = new System.Drawing.Size(200, 200) };

            this.Controls.Add(startButton);
            this.Controls.Add(outputTextBox);

            this.Text = "Rx Examples";
            this.Size = new System.Drawing.Size(230, 300);
        }

        private void SetupEvents()
        {
            startButton.Click += (sender, e) =>
            {
                //ConvertEventsExample.Run(outputTextBox);
                //ContextExample.Run(outputTextBox);
                //GroupingExample.Run(outputTextBox);
                //ThrottlingSamplingExample.Run(outputTextBox);
                TimeoutsExample.Run(outputTextBox);
                // DeferredEvaluationExample.Run(outputTextBox);
            };
        }

        public static void AppendText(TextBox textBox, string message)
        {
            if (textBox.InvokeRequired)
            {
                textBox.Invoke(new Action<TextBox, string>(AppendText), textBox, message);
            }
            else
            {
                textBox.AppendText(message + Environment.NewLine);
            }
        }

        [STAThread]
        public static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }

    public static class ConvertEventsExample
    {
        public static void Run(TextBox outputTextBox)
        {

            Observable.FromEventPattern<EventHandler, EventArgs>(h => MainForm.startButton.Click += h, h => MainForm.startButton.Click -= h)
                .Subscribe(args => MainForm.AppendText(outputTextBox, "Button was clicked!"));

            var client = new WebClient();
            IObservable<EventPattern<object>> downloadedStrings =
             Observable.
             FromEventPattern(client, nameof(WebClient.DownloadStringCompleted));
            downloadedStrings.Subscribe(
             data =>
             {
                 var eventArgs = (DownloadStringCompletedEventArgs)data.EventArgs;
                 if (eventArgs.Error != null)
                     Trace.WriteLine("OnNext: (Error) " + eventArgs.Error);
                 else
                     Trace.WriteLine("OnNext: " + eventArgs.Result);
             },
             ex => Trace.WriteLine("OnError: " + ex.ToString()),
             () => Trace.WriteLine("OnCompleted"));
            client.DownloadStringAsync(new Uri("http://invalid.example.com/"));

        }
    }

    public static class ContextExample
    {
        public static void Run(TextBox outputTextBox)
        {
            Observable.Interval(TimeSpan.FromSeconds(1))
                      .Take(5) // Just for demonstration, stop after 5 emissions
                      .ObserveOn(MainForm.uiContext)
                      .Subscribe(x => MainForm.AppendText(outputTextBox, $"Time elapsed: {x}s"));



        }
    }

    public static class GroupingExample
    {
        public static void Run(TextBox outputTextBox)
        {
            Observable.Interval(TimeSpan.FromSeconds(1))
                      .Buffer(3)
                      .Take(2) // Just for demonstration, stop after 2 buffers
                      .ObserveOn(MainForm.uiContext)
                      .Subscribe(buffer => MainForm.AppendText(outputTextBox, $"Buffered: {string.Join(", ", buffer)}"));

            Observable.Interval(TimeSpan.FromSeconds(1))
                      .Window(TimeSpan.FromSeconds(5))
                      .Take(1) // Just for demonstration, stop after 1 window
                      .Subscribe(window =>
                      {
                          window.ObserveOn(MainForm.uiContext)
                                .Subscribe(x => MainForm.AppendText(outputTextBox, $"In Window: {x}"));
                      });
        }
    }

    public static class ThrottlingSamplingExample
    {
        public static void Run(TextBox outputTextBox)
        {
            // Simulate mouse movement events
            var mouseMoves = Observable.FromEventPattern<MouseEventHandler, MouseEventArgs>(
                h => MainForm.ActiveForm.MouseMove += h,
                h => MainForm.ActiveForm.MouseMove -= h
            );

            // Throttling: Only update if no new movement for 500ms
            mouseMoves.Throttle(TimeSpan.FromMilliseconds(500))
                      .ObserveOn(MainForm.uiContext)
                      .Subscribe(e =>
                      {
                          var args = e.EventArgs;
                          MainForm.AppendText(outputTextBox, $"Throttled Update: Mouse at {args.X}, {args.Y}");
                      });

            // Sampling: Update every 500ms with the latest position
            mouseMoves.Sample(TimeSpan.FromMilliseconds(500))
                      .ObserveOn(MainForm.uiContext)
                      .Subscribe(e =>
                      {
                          var args = e.EventArgs;
                          MainForm.AppendText(outputTextBox, $"Sampled Update: Mouse at {args.X}, {args.Y}");
                      });

            MainForm.AppendText(outputTextBox, "Move the mouse over the form to see throttled and sampled updates.");
        }
    }

    public static class TimeoutsExample
    {
        public static void Run(TextBox outputTextBox)
        {
            Observable.Interval(TimeSpan.FromSeconds(3))
                      .Timeout(TimeSpan.FromSeconds(2))
                      .ObserveOn(MainForm.uiContext)
                      .Subscribe(
                          x => MainForm.AppendText(outputTextBox, $"Received: {x}"),
                          ex => MainForm.AppendText(outputTextBox, "Operation timed out")
                      );
        }
    }

    public static class DeferredEvaluationExample
    {
        public static void Run(TextBox outputTextBox)
        {
            var deferredObservable = Observable.Defer(() =>
                Observable.Return(DateTime.Now)
            );
            deferredObservable.ObserveOn(MainForm.uiContext).Subscribe(t => MainForm.AppendText(outputTextBox, $"First Subscription: {t}"));
            System.Threading.Tasks.Task.Delay(1000).ContinueWith(_ => 
            {
                deferredObservable.ObserveOn(MainForm.uiContext).Subscribe(t => MainForm.AppendText(outputTextBox, $"Second Subscription: {t}"));
            }, TaskScheduler.FromCurrentSynchronizationContext());
        }
    }
}