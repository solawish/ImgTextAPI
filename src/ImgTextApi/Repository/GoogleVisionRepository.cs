using Google.Cloud.Vision.V1;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace ImgTextApi.Repository
{
    public class GoogleVisionRepository
    {
        private readonly ImageAnnotatorClient _client;

        public GoogleVisionRepository(ImageAnnotatorClient imageAnnotatorClient)
        {
            _client = imageAnnotatorClient;
        }

        public async Task<string> GetAllImgText(Stream stream)
        {
            Image image = Image.FromBytes(((MemoryStream)stream).ToArray());

            IReadOnlyList<EntityAnnotation> textAnnotations = await _client.DetectTextAsync(image);

            //foreach (EntityAnnotation text in textAnnotations)
            //{
            //    Console.WriteLine($"Description: {text.Description}");
            //}

            return textAnnotations.First()?.Description;
        }
    }
}