using Riok.Mapperly.Abstractions;
using Volo.Abp.Mapperly;
using TaskManagement.Books;
using TaskManagement.Categories;
using TaskManagement.Departments;
using TaskManagement.LocalizationManagement.Languages;
using TaskManagement.LocalizationManagement.LanguageTexts;
using TaskManagement.Provider;
using TaskManagement.SysMasterLists;
using TaskManagement.Tags;

namespace TaskManagement;

#region Books
[Riok.Mapperly.Abstractions.Mapper(RequiredMappingStrategy = Riok.Mapperly.Abstractions.RequiredMappingStrategy.Target)]
public partial class TaskManagementBookToBookDtoMapper : MapperBase<Book, BookDto>
{
    public override partial BookDto Map(Book source);
    public override partial void Map(Book source, BookDto destination);
}

[Riok.Mapperly.Abstractions.Mapper(RequiredMappingStrategy = Riok.Mapperly.Abstractions.RequiredMappingStrategy.None)]
public partial class TaskManagementCreateUpdateBookDtoToBookMapper : MapperBase<CreateUpdateBookDto, Book>
{
    public override partial Book Map(CreateUpdateBookDto source);
    public override partial void Map(CreateUpdateBookDto source, Book destination);
}
#endregion

#region Languages
[Riok.Mapperly.Abstractions.Mapper(RequiredMappingStrategy = Riok.Mapperly.Abstractions.RequiredMappingStrategy.Target)]
public partial class TaskManagementLanguageToLanguageDtoMapper : MapperBase<Language, LanguageDto>
{
    public override partial LanguageDto Map(Language source);
    public override partial void Map(Language source, LanguageDto destination);
}

[Riok.Mapperly.Abstractions.Mapper(RequiredMappingStrategy = Riok.Mapperly.Abstractions.RequiredMappingStrategy.None)]
public partial class TaskManagementCreateUpdateLanguageDtoToLanguageMapper : MapperBase<CreateUpdateLanguageDto, Language>
{
    public override partial Language Map(CreateUpdateLanguageDto source);
    public override partial void Map(CreateUpdateLanguageDto source, Language destination);
}

[Riok.Mapperly.Abstractions.Mapper(RequiredMappingStrategy = Riok.Mapperly.Abstractions.RequiredMappingStrategy.Target)]
public partial class TaskManagementInputLanguageDtoToLanguageRequestMapper : MapperBase<InputLanguageDto, LanguageRequest>
{
    public override partial LanguageRequest Map(InputLanguageDto source);
    public override partial void Map(InputLanguageDto source, LanguageRequest destination);
}

[Riok.Mapperly.Abstractions.Mapper(RequiredMappingStrategy = Riok.Mapperly.Abstractions.RequiredMappingStrategy.Target)]
public partial class TaskManagementLanguageQueryResponseToLanguageDtoMapper : MapperBase<LanguageQueryResponse, LanguageDto>
{
    public override partial LanguageDto Map(LanguageQueryResponse source);
    public override partial void Map(LanguageQueryResponse source, LanguageDto destination);
}
#endregion

#region LanguageTexts
[Riok.Mapperly.Abstractions.Mapper(RequiredMappingStrategy = Riok.Mapperly.Abstractions.RequiredMappingStrategy.Target)]
public partial class TaskManagementLanguageTextToLanguageTextDtoMapper : MapperBase<LanguageText, LanguageTextDto>
{
    public override partial LanguageTextDto Map(LanguageText source);
    public override partial void Map(LanguageText source, LanguageTextDto destination);
}

[Riok.Mapperly.Abstractions.Mapper(RequiredMappingStrategy = Riok.Mapperly.Abstractions.RequiredMappingStrategy.None)]
public partial class TaskManagementCreateUpdateLanguageTextDtoToLanguageTextMapper : MapperBase<CreateUpdateLanguageTextDto, LanguageText>
{
    public override partial LanguageText Map(CreateUpdateLanguageTextDto source);
    public override partial void Map(CreateUpdateLanguageTextDto source, LanguageText destination);
}
#endregion

#region SysMasterLists
[Riok.Mapperly.Abstractions.Mapper(RequiredMappingStrategy = Riok.Mapperly.Abstractions.RequiredMappingStrategy.None)]
public partial class TaskManagement_GetSysMasterListInput_To_SysMasterListRequest_Mapper : MapperBase<GetSysMasterListInput, SysMasterListRequest>
{
    public override partial SysMasterListRequest Map(GetSysMasterListInput source);
    public override partial void Map(GetSysMasterListInput source, SysMasterListRequest destination);
}

[Riok.Mapperly.Abstractions.Mapper(RequiredMappingStrategy = Riok.Mapperly.Abstractions.RequiredMappingStrategy.Target)]
public partial class TaskManagement_PageSysMasterListQueryResponse_To_SysMasterListDto_Mapper : MapperBase<PageSysMasterListQueryResponse, SysMasterListDto>
{
    public override partial SysMasterListDto Map(PageSysMasterListQueryResponse source);
    public override partial void Map(PageSysMasterListQueryResponse source, SysMasterListDto destination);
}

[Riok.Mapperly.Abstractions.Mapper(RequiredMappingStrategy = Riok.Mapperly.Abstractions.RequiredMappingStrategy.Target)]
public partial class TaskManagement_InfoSysMasterListQueryResponse_To_SysMasterListDto_Mapper : MapperBase<InfoSysMasterListQueryResponse, SysMasterListDto>
{
    public override partial SysMasterListDto Map(InfoSysMasterListQueryResponse source);
    public override partial void Map(InfoSysMasterListQueryResponse source, SysMasterListDto destination);
}

[Riok.Mapperly.Abstractions.Mapper(RequiredMappingStrategy = Riok.Mapperly.Abstractions.RequiredMappingStrategy.None)]
public partial class TaskManagement_SysMasterListQueryResponse_To_SysMasterListDto_Mapper : MapperBase<SysMasterListQueryResponse, SysMasterListDto>
{
    public override partial SysMasterListDto Map(SysMasterListQueryResponse source);
    public override partial void Map(SysMasterListQueryResponse source, SysMasterListDto destination);
}

[Riok.Mapperly.Abstractions.Mapper(RequiredMappingStrategy = Riok.Mapperly.Abstractions.RequiredMappingStrategy.None)]
public partial class TaskManagement_CreateUpdateSysMasterListDto_To_SysMasterListInsertOrUpdateRequest_Mapper : MapperBase<CreateUpdateSysMasterListDto, SysMasterListInsertOrUpdateRequest>
{
    public override partial SysMasterListInsertOrUpdateRequest Map(CreateUpdateSysMasterListDto source);
    public override partial void Map(CreateUpdateSysMasterListDto source, SysMasterListInsertOrUpdateRequest destination);
}

[Riok.Mapperly.Abstractions.Mapper(RequiredMappingStrategy = Riok.Mapperly.Abstractions.RequiredMappingStrategy.None)]
public partial class TaskManagement_DeleteSysMasterListDto_To_SysMasterListDeleteRequest_Mapper : MapperBase<DeleteSysMasterListDto, SysMasterListDeleteRequest>
{
    public override partial SysMasterListDeleteRequest Map(DeleteSysMasterListDto source);
    public override partial void Map(DeleteSysMasterListDto source, SysMasterListDeleteRequest destination);
}
#endregion

#region Categories
[Riok.Mapperly.Abstractions.Mapper(RequiredMappingStrategy = Riok.Mapperly.Abstractions.RequiredMappingStrategy.Target)]
public partial class TaskManagementCategoryToCategoryDtoMapper : MapperBase<Category, CategoryDto>
{
    public override partial CategoryDto Map(Category source);
    public override partial void Map(Category source, CategoryDto destination);
}

[Riok.Mapperly.Abstractions.Mapper(RequiredMappingStrategy = Riok.Mapperly.Abstractions.RequiredMappingStrategy.None)]
public partial class TaskManagementCreateUpdateCategoryDtoToCategoryMapper : MapperBase<CreateUpdateCategoryDto, Category>
{
    public override partial Category Map(CreateUpdateCategoryDto source);
    public override partial void Map(CreateUpdateCategoryDto source, Category destination);
}
#endregion

#region Tags
[Riok.Mapperly.Abstractions.Mapper(RequiredMappingStrategy = Riok.Mapperly.Abstractions.RequiredMappingStrategy.Target)]
public partial class TaskManagementTagToTagDtoMapper : MapperBase<Tag, TagDto>
{
    
    [MapperIgnoreTarget(nameof(TagDto.CategoryName))]
    public override partial TagDto Map(Tag source);

    [MapperIgnoreTarget(nameof(TagDto.CategoryName))]
    public override partial void Map(Tag source, TagDto destination);
}

[Riok.Mapperly.Abstractions.Mapper(RequiredMappingStrategy = Riok.Mapperly.Abstractions.RequiredMappingStrategy.None)]
public partial class TaskManagementCreateUpdateTagDtoToTagMapper : MapperBase<CreateUpdateTagDto, Tag>
{
    public override partial Tag Map(CreateUpdateTagDto source);
    public override partial void Map(CreateUpdateTagDto source, Tag destination);
}
#endregion

#region Departments
[Riok.Mapperly.Abstractions.Mapper(RequiredMappingStrategy = Riok.Mapperly.Abstractions.RequiredMappingStrategy.None)]
public partial class TaskManagementDepartmentToDepartmentDtoMapper : MapperBase<Department, DepartmentDto>
{
    public override partial DepartmentDto Map(Department source);
    public override partial void Map(Department source, DepartmentDto destination);
}

[Riok.Mapperly.Abstractions.Mapper(RequiredMappingStrategy = Riok.Mapperly.Abstractions.RequiredMappingStrategy.None)]
public partial class TaskManagementDepartmentToDepartmentTreeDtoMapper : MapperBase<Department, DepartmentTreeDto>
{
    [MapperIgnoreTarget(nameof(DepartmentTreeDto.Children))]
    public override partial DepartmentTreeDto Map(Department source);

    [MapperIgnoreTarget(nameof(DepartmentTreeDto.Children))]
    public override partial void Map(Department source, DepartmentTreeDto destination);
}

[Riok.Mapperly.Abstractions.Mapper(RequiredMappingStrategy = Riok.Mapperly.Abstractions.RequiredMappingStrategy.None)]
public partial class TaskManagementCreateUpdateDepartmentDtoToDepartmentMapper : MapperBase<CreateUpdateDepartmentDto, Department>
{
    public override partial Department Map(CreateUpdateDepartmentDto source);
    public override partial void Map(CreateUpdateDepartmentDto source, Department destination);
}

[Riok.Mapperly.Abstractions.Mapper(RequiredMappingStrategy = Riok.Mapperly.Abstractions.RequiredMappingStrategy.None)]
public partial class TaskManagementDepartmentToCreateUpdateDepartmentDtoMapper : MapperBase<Department, CreateUpdateDepartmentDto>
{
    public override partial CreateUpdateDepartmentDto Map(Department source);
    public override partial void Map(Department source, CreateUpdateDepartmentDto destination);
}
#endregion