using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;

namespace ClientSupportTicketingSystem.Services
{
    public class UserImageService : IUserImageService
    {
        private readonly IWebHostEnvironment _environment;

        public UserImageService(IWebHostEnvironment environment)
        {
            _environment = environment;
        }

        public async Task<string> SaveAsync(IFormFile image)
        {
            if (image.Length == 0 || image.Length > 2 * 1024 * 1024)
            {
                throw new InvalidDataException(
                    "Image size must be between 1 byte and 2 MB.");
            }

            using var buffer = new MemoryStream();
            await image.CopyToAsync(buffer);
            byte[] bytes = buffer.ToArray();

            bool isJpeg = bytes.Length >= 3
                && bytes[0] == 0xFF
                && bytes[1] == 0xD8
                && bytes[2] == 0xFF;

            bool isPng = bytes.Length >= 8
                && bytes[0] == 0x89
                && bytes[1] == 0x50
                && bytes[2] == 0x4E
                && bytes[3] == 0x47
                && bytes[4] == 0x0D
                && bytes[5] == 0x0A
                && bytes[6] == 0x1A
                && bytes[7] == 0x0A;

            if (!isJpeg && !isPng)
            {
                throw new InvalidDataException(
                    "Only JPEG and PNG images are allowed.");
            }

            string extension = isJpeg ? ".jpg" : ".png";
            string fileName = Guid.NewGuid().ToString("N") + extension;

            string directory = Path.Combine(
                _environment.ContentRootPath,
                "App_Data",
                "UserImages");

            Directory.CreateDirectory(directory);

            string fullPath = Path.Combine(directory, fileName);
            await File.WriteAllBytesAsync(fullPath, bytes);

            return fileName;
        }

        public void Delete(string fileName)
        {
            string fullPath = Path.Combine(
                _environment.ContentRootPath,
                "App_Data",
                "UserImages",
                Path.GetFileName(fileName));

            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
            }
        }
    }
}