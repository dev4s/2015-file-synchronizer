using System;
using NLog;

namespace FileSynchronizer.Logic
{
    public class InternalProgress : IInternalProgress
    {
        private readonly Progress<string> progressLine = new Progress<string>();

        private readonly Progress<string> progress = new Progress<string>();

        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        public InternalProgress()
        {
            progressLine.ProgressChanged += (sender, s) => Console.WriteLine("{0}", s);
            progress.ProgressChanged += (sender, s) => Console.Write("{0}, ", s);
        }

        public void ReportLine(string text)
        {
            this.Report(progressLine, text);
            Logger.Info(text);
        }

        public void Report(string text)
        {
            this.Report(progress, text);
            Logger.Info(text);
        }

        private void Report<T>(Progress<T> prog, T text)
        {
            ((IProgress<T>)prog).Report(text);
        }
    }
}