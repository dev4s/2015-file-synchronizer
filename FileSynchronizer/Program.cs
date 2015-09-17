using System;
using System.Threading.Tasks;
using FileSynchronizer.Logic;
using FileSynchronizer.Logic.Cryptography;
using NLog;

namespace FileSynchronizer
{
    class Program
    {
        private const string EncryptPassArg = "-encrypt";

        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        static int Main(params string[] args)
        {
            var encryptPassArgPosition = Array.IndexOf(args, EncryptPassArg);
            if (encryptPassArgPosition > -1)
            {
                return ValidateEncryptPassArg(encryptPassArgPosition, args);
            }
            
            MainAsync().Wait();

            return 0;
        }

        private static async Task MainAsync()
        {
            try
            {
                await new Synchronizer(new InternalProgress()).DownloadFilesAsync();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                Logger.Error(e);
            }
        }

        private static int ValidateEncryptPassArg(int encryptPassArgPosition, params string[] args)
        {
            var encryptPassValuePosition = encryptPassArgPosition + 1;

            if (args.Length > encryptPassValuePosition)
            {
                var encryptPassValue = args[encryptPassValuePosition];

                var encryptedValue = new SimpleAES().EncryptToString(encryptPassValue);

                Console.WriteLine("Passed value: {0}", args[encryptPassValuePosition]);
                Console.WriteLine("Encrypted value: {0}", encryptedValue);

                return 0;
            }

            Console.WriteLine("You've forgot value to encrypt.");
            return -1;
        }
    }
}
