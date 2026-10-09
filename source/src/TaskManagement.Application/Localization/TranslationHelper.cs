using GTranslate.Translators;
using System.Threading.Tasks;

namespace TaskManagement.Localization
{
    public static class TranslationHelper
    {
        private static readonly GoogleTranslator _translator = new GoogleTranslator();

        public static async Task<string> AutoTranslateAsync(string text, string toLanguage = "en")
        {
            if (string.IsNullOrWhiteSpace(text))
                return string.Empty;

            try
            {
                var result = await _translator.TranslateAsync(text, toLanguage);
                return result.Translation;
            }
            catch
            {
                // Fallback nếu có lỗi mạng hoặc lỗi dịch, trả về text gốc để không bị chết luồng
                return text;
            }
        }
    }
}