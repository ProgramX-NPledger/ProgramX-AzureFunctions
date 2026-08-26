using System.Text;
using System.Text.Json;
using ProgramX.Azure.FunctionApp.AzureStorage;
using ProgramX.Azure.FunctionApp.Contract;
using ProgramX.Azure.FunctionApp.Model;

namespace ProgramX.Azure.FunctionApp.Helpers;

public class FileUploader
{
    private readonly IStorageClient _storageClient;

    public FileUploader(IStorageClient storageClient)
    {
        _storageClient = storageClient;
    }

    public async Task<IEnumerable<SavedFile>> UploadFilesAsync(IEnumerable<UploadedFile> files, IEnumerable<string>? readRequiresRoles = null)
    {
        var savedFiles = new List<SavedFile>();
        foreach (var file in files)
        {
            var storageFolder = await _storageClient!.GetStorageFolderAsync(file.FilePurpose);

            var savedFile = new SavedFile();

            // files must be saved with unique names to avoid collisions
            var originalName = file.OriginalFileName;
            savedFile.FileName = originalName;
            var ext = Path.GetExtension(originalName); // eg. .jpg
            string blobFolder = $"{Guid.NewGuid():N}{ext}"; // eg abc123.jpg
            string safeFileName = $"{blobFolder}/original{ext}"; // eg. abc123.jpg/original.jpg

            using (var memoryStream = new MemoryStream(file.Data))
            {
                await storageFolder.SaveFileAsync($"{safeFileName}", memoryStream,
                    file.ContentType);
            }

            // create an index entry file
            var blobIndexEntry = new BlobIndexEntry()
            {
                ReadRequiresRoles = readRequiresRoles?.ToArray() ?? [],
                OriginalFileName = originalName,
                StoredFileName = safeFileName,
                ContainerName = file.FilePurpose,
                ContentType = file.ContentType
            };

            var json = JsonSerializer.Serialize(blobIndexEntry, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            var jsonBytes = Encoding.ASCII.GetBytes(json);
            using var blobIndexEntryMemoryStream = new MemoryStream(jsonBytes);
            var blobIndexFileName = Path.Join(blobFolder, "blobIndexEntry.json");
            await storageFolder.SaveFileAsync(blobIndexFileName, blobIndexEntryMemoryStream,
                "application/json");

            savedFile.FileName = $"{file.FilePurpose}/{safeFileName}";
            savedFile.FileSize = file.Data.Length;
            savedFiles.Add(savedFile);
        }
        return savedFiles;
    }
}