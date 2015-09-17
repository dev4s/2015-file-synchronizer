namespace FileSynchronizer.Logic
{
    public interface IInternalProgress
    {
        void ReportLine(string text);

        void Report(string text);
    }
}