using TaskManagement.LocalizationManagement.Languages;

namespace TaskManagement.Provider
{
    public interface ILanguagesProvider
    {
        Task<List<LanguageQueryResponse>> GetLanguagessAsync(LanguageRequest input);
    }
}
