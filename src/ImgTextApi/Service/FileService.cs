using System;
using System.IO;
using System.Threading.Tasks;

namespace ImgTextApi.Service
{
    public class FileService
    {
        private readonly string savePath = "/data/upload";

        private readonly string urlFileName = "uploadUrl.txt";

        public FileService()
        {
        }

        /// <summary>
        /// Saves to disk.
        /// </summary>
        /// <param name="stream">The stream.</param>
        public async Task SaveToDisk(Stream stream)
        {
            var id = Guid.NewGuid().ToString("N");

            if (!Directory.Exists(savePath))
            {
                Directory.CreateDirectory(savePath);
            }

            using (var fileStream = new FileStream($"{savePath}/{id}.jpg", FileMode.Create))
            {
                stream.Seek(0, SeekOrigin.Begin);
                await stream.CopyToAsync(fileStream);
            }
        }

        /// <summary>
        /// Logs the URL.
        /// </summary>
        /// <param name="url">The URL.</param>
        public async Task LogUrl(string url)
        {
            if (!Directory.Exists(savePath))
            {
                Directory.CreateDirectory(savePath);
            }

            StreamWriter sw;
            if (!File.Exists($"{savePath}/{urlFileName}"))
            {
                sw = File.CreateText($"{savePath}/{urlFileName}");
            }
            else
            {
                sw = File.AppendText($"{savePath}/{urlFileName}");
            }

            await sw.WriteLineAsync(url);

            sw.Close();
            await sw.DisposeAsync();
        }
    }
}