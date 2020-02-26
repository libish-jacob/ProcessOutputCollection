using System;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace WpfApp5
{
  /// <summary>
  /// Interaction logic for MainWindow.xaml
  /// </summary>
  public partial class MainWindow : Window
  {
    public MainWindow()
    {
      InitializeComponent();
    }

    private void Button_Click(object senderItem, RoutedEventArgs eventItem)
    {
      var arguments = "Test";
      
      var Program = @".\ConsoleApp1.exe";
      bool openCMD_Window = false;

      //lets run the same process 50 times in parallel.
      for (int i = 0; i < 50; i++)
      {
        Task.Factory.StartNew(() =>
        {
          // lets make use of this event to start waiting.
          AutoResetEvent ae = new AutoResetEvent(false);          
          string Output = string.Empty;
          /*The process has an internal sleep of 50 seconds.*/
          var task = Task.Factory.StartNew(() => Process(Program, arguments, openCMD_Window, ae, out Output));

          //lets wait until the thread starts.
          ae.WaitOne();
          if (task.Wait(TimeSpan.FromSeconds(60)))
          {
            Console.WriteLine(Output);
          }
          else
          {
            Console.WriteLine("Process timed out....");
          }
        });
      }
    }

    public void Process(string Program, string Arguments, bool OpenCommandWindow, AutoResetEvent ae, out string OutPut)
    {
      /*This is important since we are running on a task and the thread may not start immediately. We have to wait right after this thread has started.*/
      ae.Set();
      
      using (Process process = new Process())
      {
        if (Arguments != null)
        {
          process.StartInfo.FileName = "cmd";
          // For reading the files with special characters. 
          process.StartInfo.Arguments = "/C \"" + Program + " " + Arguments + "\"";
        }
        else
        {
          process.StartInfo.FileName = Program;
        }

        process.StartInfo.UseShellExecute = false;
        process.StartInfo.RedirectStandardError = !OpenCommandWindow;
        process.StartInfo.CreateNoWindow = !OpenCommandWindow;
        process.StartInfo.RedirectStandardOutput = !OpenCommandWindow;

        StringBuilder output = new StringBuilder();

        Action<object, DataReceivedEventArgs> dataCallback = (sender, e) =>
        {
          try
          {
            output.AppendLine(e.Data);
          }
          catch (ObjectDisposedException)
          {
              // It can happen and is ok. It can happen if there is still data and we timed out and the even fired before we unsubscribe.
            }
        };
        DataReceivedEventHandler handler = new DataReceivedEventHandler(dataCallback);
        process.OutputDataReceived += handler;

        process.Start();
        var processId = process.Id;

        if (!OpenCommandWindow)
        {
          process.BeginOutputReadLine();
        }

        process.WaitForExit();

        // unsubscribe it to avoid object disposed exception.
        process.OutputDataReceived -= handler;
        OutPut = output.ToString();
        if (string.IsNullOrWhiteSpace(OutPut))
        {
          /*This piece of code is here for demo purpose. If the output is empty, then pop up a notification.*/
          MessageBox.Show("Empty output");
        }
      }
    }
  }
}
