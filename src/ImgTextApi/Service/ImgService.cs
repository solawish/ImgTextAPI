using ImgTextApi.Models;
using ImgTextApi.Repository;
using Microsoft.Extensions.Options;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ImgTextApi.Service
{
    public class ImgService
    {
        private readonly GoogleVisionRepository _googleVisionRepository;

        private readonly LevenshteinDistanceService _levenshteinDistanceService;

        private readonly IdListModel _idListModel;

        public ImgService(GoogleVisionRepository googleVisionRepository, IOptions<IdListModel> idListModel,
            LevenshteinDistanceService levenshteinDistanceService)
        {
            _googleVisionRepository = googleVisionRepository;
            _idListModel = idListModel.Value;
            _levenshteinDistanceService = levenshteinDistanceService;
        }

        /// <summary>
        /// 主流程
        /// </summary>
        /// <param name="stream"></param>
        /// <returns></returns>
        public async Task<ResponseModel> ProcessImg(Stream stream)
        {
            var responseModel = new ResponseModel();

            responseModel.OCRResult = await _googleVisionRepository.GetAllImgText(stream);

            responseModel.Data = this.AnalyzeText(responseModel.OCRResult);

            return responseModel;
        }

        /// <summary>
        /// 處理vision的結果
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        private List<TextProcessModel> AnalyzeText(string text)
        {
            //去除奇怪的字元
            text = text.Replace("|", "").Replace("。", "").Replace(".", "").Replace(",", "").Replace("「", "");

            //去除標示(可能會有問題) (山:團長符號, X:X符號, 會:五角星, 心:副團長符號, S:拾取符號, 凹:團長符號)
            text = text.Replace("山", "").Replace("X", "").Replace("會", "").Replace("心", "").Replace("S", "").Replace("凹", "");

            //去除下線
            text = text.Replace("下線", "");

            //去除數字
            text = Regex.Replace(text, "[0-9]", "", RegexOptions.IgnoreCase);

            // 空白取代為 \n
            text = text.Replace(" ", "\n");

            // 基本 \n 分隔
            var result = text.Split("\n".ToCharArray().First()).ToList();

            //去除只有數字的項目
            //result = result.Select(x => int.TryParse(x, out var _) ? "" : x).Where(x => !string.IsNullOrEmpty(x)).ToList();

            //去除只有一個英文字的項目
            result = result.Where(x => Encoding.Default.GetBytes(x).Count() > 1).ToList();

            // trim 空白項目去除
            result = result.Select(x => x.Trim()).Where(x => !string.IsNullOrEmpty(x)).ToList();

            var modelResult = result.Select(x => new TextProcessModel { OriginResult = x }).ToList();

            // ..(ID太長) 的處理 (開頭部分ID比對)
            Parallel.ForEach(modelResult, (element) =>
            {
                var dictResult = _idListModel.Data.Where(x => x.IndexOf(element.OriginResult) == 0);
                if (dictResult.Distinct().Count() == 1)
                {
                    element.RecommandResult = dictResult.First();
                }
            });

            // ID連在一起的處理
            var AddList = new Dictionary<string, List<string>>();

            Parallel.ForEach(modelResult, (element) =>
            {
                var indexResult = GetConcatIdFuzzy(element.OriginResult);
                if (indexResult.Distinct().Count() > 1)
                {
                    AddList.Add(element.OriginResult, indexResult.Distinct().ToList());
                }
            });
            foreach (var item in AddList)
            {
                var removeIndex = modelResult.IndexOf(modelResult.Where(x => x.OriginResult == item.Key).First());

                modelResult.RemoveAt(removeIndex);
                modelResult.InsertRange(removeIndex, item.Value.Select(x => new TextProcessModel { OriginResult = x }));
            }

            // 相似字處理
            Parallel.ForEach(modelResult, (element) =>
            {
                var compareResult = _idListModel.Data.Where(x => _levenshteinDistanceService.LevenshteinDistancePercent(x, element.OriginResult) >= this.ChooseRateByLanguage(x));
                if (compareResult.Distinct().Count() == 1)
                {
                    element.RecommandResult = compareResult.First();
                }
            });

            return modelResult;
        }

        /// <summary>
        /// 分隔ID
        /// </summary>
        /// <param name="concatId"></param>
        /// <returns></returns>
        private IEnumerable<string> GetConcatId(string concatId)
        {
            var resultId = new List<string>();

            foreach (var id in _idListModel.Data)
            {
                if (concatId.IndexOf(id) > -1)
                {
                    resultId.Add(id);
                    concatId = concatId.Replace(id, string.Empty);
                }
            }

            if (resultId.Any() && !string.IsNullOrEmpty(concatId))
            {
                resultId.Add(concatId);
            }

            return resultId;
        }

        /// <summary>
        /// 分隔ID(模糊比對)
        /// </summary>
        /// <param name="concatId"></param>
        /// <returns></returns>
        private IEnumerable<string> GetConcatIdFuzzy(string concatId)
        {
            //相連ID至少要有5+2的長度
            if (concatId.Length < 6)
            {
                return new List<string>();
            }

            //如果可以確定是現存的就不找
            if (_idListModel.Data.Where(x => x == concatId).Any())
            {
                return new List<string>();
            }

            var resultId = new List<string>();

            // 因為目前看起來最多只會有兩個ID相連，所以偷懶 =D
            foreach (var id in _idListModel.Data)
            {
                //只比對傳入ID超過字典ID的
                if (id.Length >= concatId.Length)
                {
                    continue;
                }

                if (_levenshteinDistanceService.LevenshteinDistancePercent(concatId.Substring(0, id.Length), id) >= ChooseRateByLanguage(id))
                {
                    resultId.Add(id);
                    concatId = concatId.Replace(concatId.Substring(0, id.Length), string.Empty);
                }
            }

            if (resultId.Any() && !string.IsNullOrEmpty(concatId))
            {
                resultId.Add(concatId);
            }

            return resultId;
        }

        /// <summary>
        /// 由語言決定文字相似的比率 (中文:0.5, 英文0.6)
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        private decimal ChooseRateByLanguage(string text)
        {
            Regex regEnglish = new Regex(@"^[A-Za-z0-9]+$");
            return regEnglish.IsMatch(text) ? 0.6m : 0.5m;
        }
    }
}