using System;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace TaskManagement.LocalizationManagement.LanguageTexts
{
    public interface ILanguageTextAppService :
    ICrudAppService< //Defines CRUD methods
        LanguageTextDto, //Used to show LanguageTexts
        Guid, //Primary key of the LanguageText entity
        PagedAndSortedResultRequestDto, //Used for paging/sorting
        CreateUpdateLanguageTextDto> //Used to create/update a LanguageText
    {

    }
}
