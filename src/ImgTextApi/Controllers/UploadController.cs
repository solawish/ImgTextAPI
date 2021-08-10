using ImgTextApi.Models;
using ImgTextApi.Service;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IO;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace ImgTextApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UploadController : ControllerBase
    {
        private readonly ImgService _imgService;

        private readonly RecyclableMemoryStreamManager _recyclableMemoryStreamManager;

        private readonly FileService _fileService;

        private readonly IHttpClientFactory _httpClientFactory;

        public UploadController(ImgService imgService, RecyclableMemoryStreamManager recyclableMemoryStreamManager,
            FileService fileService, IHttpClientFactory httpClientFactory)
        {
            _imgService = imgService;
            _recyclableMemoryStreamManager = recyclableMemoryStreamManager;
            _fileService = fileService;
            _httpClientFactory = httpClientFactory;
        }

        /// <summary>
        /// Posts the specified files.
        /// </summary>
        /// <param name="files">The files.</param>
        /// <returns></returns>
        [HttpPost]
        [Route("file")]
        [RequestSizeLimit(1024000)]
        public async Task<ResponseModel> Post(List<IFormFile> files)
        {
            using (var stream = _recyclableMemoryStreamManager.GetStream())
            {
                await files.FirstOrDefault().CopyToAsync(stream);

                var result = await _imgService.ProcessImg(stream);

                await _fileService.SaveToDisk(stream);

                return result;
            }
        }

        [HttpPost]
        [Route("url")]
        [RequestSizeLimit(1024000)]
        public async Task<ResponseModel> Post([FromForm] string url)
        {
            using (var stream = _recyclableMemoryStreamManager.GetStream())
            {
                url = url.Trim();

                await _fileService.LogUrl(url);

                var urlResult = await _httpClientFactory.CreateClient().GetAsync(url);

                if (urlResult.Content.Headers.ContentLength > 1024000)
                {
                    return new ResponseModel();
                }

                await urlResult.Content.CopyToAsync(stream);

                var result = await _imgService.ProcessImg(stream);

                await _fileService.SaveToDisk(stream);

                return result;
            }
        }
    }
}