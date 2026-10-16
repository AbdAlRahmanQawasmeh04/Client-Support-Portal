using ClientSupportTicketingSystem.Data.Models;

namespace ClientSupportTicketingSystem.Services
{
    public class TicketAttachmentService : ITicketAttachmentService
    {
        private const long MaxFileSize = 5 * 1024 * 1024;
        private readonly string _directory;
        private readonly ILogger<TicketAttachmentService> _logger;

        public TicketAttachmentService(IWebHostEnvironment environment, ILogger<TicketAttachmentService> logger)
        {
            _directory = Path.Combine(environment.ContentRootPath, "App_Data", "TicketAttachments");
            _logger = logger;
        }

        public async Task<List<TicketAttachment>> SaveAsync(IReadOnlyList<IFormFile> files)
        {
            if (files.Count > 5)
            {
                throw new InvalidDataException("A maximum of 5 attachments is allowed.");
            }

            var saved = new List<TicketAttachment>();

            try
            {
                foreach (var file in files)
                {
                    if (file.Length <= 0 || file.Length > MaxFileSize)
                    {
                        throw new InvalidDataException("Each attachment must be between 1 byte and 5 MB.");
                    }

                    string name = Path.GetFileName(file.FileName.Replace('\\', '/'));

                    if (string.IsNullOrWhiteSpace(name) || name.Length > 255 || name.Any(char.IsControl))
                    {
                        throw new InvalidDataException("Invalid attachment file name.");
                    }

                    string extension = Path.GetExtension(name).ToLowerInvariant();

                    if (extension != ".pdf" && extension != ".png" && extension != ".jpg" && extension != ".jpeg")
                    {
                        throw new InvalidDataException("Only PDF, PNG and JPG attachments are allowed.");
                    }

                    using var input = file.OpenReadStream();
                    using var buffer = new MemoryStream();
                    byte[] block = new byte[81920];
                    int count;

                    while ((count = await input.ReadAsync(block, 0, block.Length)) > 0)
                    {
                        if (buffer.Length + count > MaxFileSize)
                        {
                            throw new InvalidDataException("Attachment exceeds 5 MB.");
                        }

                        await buffer.WriteAsync(block, 0, count);
                    }

                    byte[] bytes = buffer.ToArray();
                    bool valid = extension switch
                    {
                        ".pdf" => bytes.Take(5).SequenceEqual(new byte[] { 0x25, 0x50, 0x44, 0x46, 0x2D }),
                        ".png" => bytes.Take(8).SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }),
                        _ => bytes.Length >= 3 && bytes[0] == 0xFF && bytes[1] == 0xD8 && bytes[2] == 0xFF
                    };

                    if (!valid)
                    {
                        throw new InvalidDataException("Attachment content does not match its file type.");
                    }

                    Directory.CreateDirectory(_directory);

                    var attachment = new TicketAttachment
                    {
                        FileName = name,
                        FilePath = Guid.NewGuid().ToString("N") + extension
                    };

                    saved.Add(attachment);

                    await File.WriteAllBytesAsync(Path.Combine(_directory, attachment.FilePath), bytes);
                }

                return saved;
            }
            catch
            {
                DeleteFiles(saved);
                throw;
            }
        }

        public Stream? OpenRead(string storedName)
        {
            if (string.IsNullOrWhiteSpace(storedName) || Path.GetFileName(storedName) != storedName)
            {
                return null;
            }

            try
            {
                return new FileStream(Path.Combine(_directory, storedName), FileMode.Open, FileAccess.Read, FileShare.Read);
            }
            catch (FileNotFoundException)
            {
                return null;
            }
            catch (DirectoryNotFoundException)
            {
                return null;
            }
        }

        public void DeleteFiles(IEnumerable<TicketAttachment> attachments)
        {
            foreach (var attachment in attachments)
            {
                try
                {
                    string fileName = Path.GetFileName(attachment.FilePath);
                    File.Delete(Path.Combine(_directory, fileName));
                }
                catch (Exception exception)
                {
                    _logger.LogWarning(exception, "Failed to clean up attachment {FilePath}.", attachment.FilePath);
                }
            }
        }
    }
}