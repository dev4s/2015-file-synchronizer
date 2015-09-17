using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FileSynchronizer.Configuration;
using FileSynchronizer.Logic.Cryptography;
using Renci.SshNet;
using Renci.SshNet.Sftp;

namespace FileSynchronizer.Logic
{
    public class Synchronizer
    {
        private readonly string[] excludedListOfFiles = { ".", ".." };

        private readonly IInternalProgress progress;

        public Synchronizer(IInternalProgress progress)
        {
            if (progress == null)
            {
                throw new ArgumentNullException();
            }

            this.progress = progress;
        }

        public async Task DownloadFilesAsync()
        {
            var settings = ServiceSettings.GetSettings();

            foreach (var endpoint in settings.Endpoints)
            {
                var decodedPass = new SimpleAES().DecryptString(endpoint.EncryptedPass);
                
                if (endpoint.Protocol == Protocol.Sftp)
                {
                    await this.DownloadFilesWithSftpAsync(endpoint, decodedPass);
                }
            }
        }

        private async Task DownloadFilesWithSftpAsync(Endpoint endpoint, string decodedPass)
        {
            var client = new SftpClient(endpoint.Url, endpoint.Username, decodedPass);
            client.Connect();

            SftpFile[] filesOnServer =
                (await this.ListRemoteDirectoriesAsync(client, endpoint.RemoteDirectory)).Where(
                    x => !this.excludedListOfFiles.Any(y => x.Name.Equals(y))).ToArray();

            var primaryLocalDirectory = endpoint.Destinations.FirstOrDefault(x => x.Type == DestinationType.Primary);
            var archiveLocalDirectory = endpoint.Destinations.FirstOrDefault(x => x.Type == DestinationType.Archive);

            var primaryAndArchiveAreNotExisting = primaryLocalDirectory == null || archiveLocalDirectory == null;
            if (primaryAndArchiveAreNotExisting)
            {
                progress.ReportLine("You haven't provided the primary or archive folder");
                return;
            }

            var filteredLocalFiles = FilterLocalFiles(filesOnServer, GetFilesListInFolder(primaryLocalDirectory.Name));

            await this.GetFilesAsync(filteredLocalFiles, endpoint.RemoteDirectory, primaryLocalDirectory.Name, client).ContinueWith(x => client.Disconnect());

            this.ArchiveFiles(GetFilesListInFolder(primaryLocalDirectory.Name), primaryLocalDirectory, archiveLocalDirectory);
        }

        private IEnumerable<string> GetFilesListInFolder(string folderName)
        {
            return Directory.EnumerateFiles(folderName, "*.zip").Select(Path.GetFileName);
        }

        private void ArchiveFiles(IEnumerable<string> filesInPrimaryLocalDirectory, Destination primaryLocalDirectory, Destination archiveLocalDirectory)
        {
            var groupedFileTypes = filesInPrimaryLocalDirectory.GroupBy(x => x.Contains("Y2K") ? "Y2K" : "FED");

            foreach (var groupedFiles in groupedFileTypes)
            {
                var firstGroupedFiles = groupedFiles.First();

                var sourceFile = string.Format("{0}\\{1}", primaryLocalDirectory.Name, firstGroupedFiles);
                var destinationFile = string.Format("{0}\\{1}", archiveLocalDirectory.Name, firstGroupedFiles);

                if (!File.Exists(destinationFile))
                {
                    File.Move(sourceFile, destinationFile);
                    this.progress.ReportLine(string.Format("Moved file {0} to archive folder", firstGroupedFiles));
                }
                else
                {
                    this.progress.ReportLine(string.Format("This file exists at archive folder: {0}", firstGroupedFiles));
                }
            }
        }

        private static IEnumerable<string> FilterLocalFiles(IEnumerable<SftpFile> filesOnServer, IEnumerable<string> filesInPrimaryLocalDirectory)
        {
            var filesOnServerOnlyNames = filesOnServer.Select(x => x.Name);
            var filteredLocalFiles = filesOnServerOnlyNames.Except(filesInPrimaryLocalDirectory);
            return filteredLocalFiles;
        }

        private async Task GetFilesAsync(IEnumerable<string> filesOnServer, string remoteDirectory, string primaryDirectory, SftpClient client)
        {
            foreach (var file in filesOnServer)
            {
                var localFullName = string.Format("{0}\\{1}", primaryDirectory, file);

                await this.DownloadFileAsync(client, remoteDirectory + file, localFullName);
            }
        }

        private async Task<IEnumerable<SftpFile>> ListRemoteDirectoriesAsync(SftpClient client, string remoteFullName)
        {
            return await Task.Factory.FromAsync<IEnumerable<SftpFile>>((callback, obj) => client.BeginListDirectory(remoteFullName, callback, obj), client.EndListDirectory, null);
        }

        private Task DownloadFileAsync(SftpClient client, string remoteFullName, string localFullName)
        {
            var fileStr = new FileStream(localFullName, FileMode.Create);

            return Task.Factory.FromAsync(
                (callback, obj) => 
                    client.BeginDownloadFile(remoteFullName, fileStr, callback, obj, filePosition => 
                        progress.Report(filePosition.ToString(CultureInfo.InvariantCulture))),
                result =>
                {
                    client.EndDownloadFile(result);
                    if (result.IsCompleted)
                    {
                        fileStr.Close();
                    }
                    progress.ReportLine(string.Format("File downloaded: {0}", remoteFullName));
                },
                null);
        }
    }
}