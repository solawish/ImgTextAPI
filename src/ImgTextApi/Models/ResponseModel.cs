using System.Collections.Generic;

namespace ImgTextApi.Models
{
    public class ResponseModel
    {
        public List<TextProcessModel> Data { get; set; }

        public string OCRResult { get; set; }
    }
}