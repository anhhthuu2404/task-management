using Riok.Mapperly.Abstractions;
using TaskManagement.Books;
using TaskManagement.Categories;
using TaskManagement.Departments;
using TaskManagement.LocalizationManagement.Languages;
using TaskManagement.LocalizationManagement.LanguageTexts;
using TaskManagement.Provider;
using TaskManagement.SysMasterLists;
using TaskManagement.Tags;
using TaskManagement.Tasks;
using Volo.Abp.Mapperly;

namespace TaskManagement;

#region Books
[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
public partial class TaskManagementBookToBookDtoMapper : MapperBase<Book, BookDto>
{
    public override partial BookDto Map(Book source);
    public override partial void Map(Book source, BookDto destination);
}

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.None)]
public partial class TaskManagementCreateUpdateBookDtoToBookMapper : MapperBase<CreateUpdateBookDto, Book>
{
    public override partial Book Map(CreateUpdateBookDto source);
    public override partial void Map(CreateUpdateBookDto source, Book destination);
}
#endregion

#region Languages
[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
public partial class TaskManagementLanguageToLanguageDtoMapper : MapperBase<Language, LanguageDto>
{
    public override partial LanguageDto Map(Language source);
    public override partial void Map(Language source, LanguageDto destination);
}

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.None)]
public partial class TaskManagementCreateUpdateLanguageDtoToLanguageMapper : MapperBase<CreateUpdateLanguageDto, Language>
{
    public override partial Language Map(CreateUpdateLanguageDto source);
    public override partial void Map(CreateUpdateLanguageDto source, Language destination);
}

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
public partial class TaskManagementInputLanguageDtoToLanguageRequestMapper : MapperBase<InputLanguageDto, LanguageRequest>
{
    public override partial LanguageRequest Map(InputLanguageDto source);
    public override partial void Map(InputLanguageDto source, LanguageRequest destination);
}

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
public partial class TaskManagementLanguageQueryResponseToLanguageDtoMapper : MapperBase<LanguageQueryResponse, LanguageDto>
{
    public override partial LanguageDto Map(LanguageQueryResponse source);
    public override partial void Map(LanguageQueryResponse source, LanguageDto destination);
}
#endregion

#region LanguageTexts
[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
public partial class TaskManagementLanguageTextToLanguageTextDtoMapper : MapperBase<LanguageText, LanguageTextDto>
{
    public override partial LanguageTextDto Map(LanguageText source);
    public override partial void Map(LanguageText source, LanguageTextDto destination);
}

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.None)]
public partial class TaskManagementCreateUpdateLanguageTextDtoToLanguageTextMapper : MapperBase<CreateUpdateLanguageTextDto, LanguageText>
{
    public override partial LanguageText Map(CreateUpdateLanguageTextDto source);
    public override partial void Map(CreateUpdateLanguageTextDto source, LanguageText destination);
}
#endregion

#region SysMasterLists
[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.None)]
public partial class TaskManagement_GetSysMasterListInput_To_SysMasterListRequest_Mapper : MapperBase<GetSysMasterListInput, SysMasterListRequest>
{
    public override partial SysMasterListRequest Map(GetSysMasterListInput source);
    public override partial void Map(GetSysMasterListInput source, SysMasterListRequest destination);
}

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
public partial class TaskManagement_PageSysMasterListQueryResponse_To_SysMasterListDto_Mapper : MapperBase<PageSysMasterListQueryResponse, SysMasterListDto>
{
    public override partial SysMasterListDto Map(PageSysMasterListQueryResponse source);
    public override partial void Map(PageSysMasterListQueryResponse source, SysMasterListDto destination);
}

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
public partial class TaskManagement_InfoSysMasterListQueryResponse_To_SysMasterListDto_Mapper : MapperBase<InfoSysMasterListQueryResponse, SysMasterListDto>
{
    public override partial SysMasterListDto Map(InfoSysMasterListQueryResponse source);
    public override partial void Map(InfoSysMasterListQueryResponse source, SysMasterListDto destination);
}

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.None)]
public partial class TaskManagement_SysMasterListQueryResponse_To_SysMasterListDto_Mapper : MapperBase<SysMasterListQueryResponse, SysMasterListDto>
{
    public override partial SysMasterListDto Map(SysMasterListQueryResponse source);
    public override partial void Map(SysMasterListQueryResponse source, SysMasterListDto destination);
}

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.None)]
public partial class TaskManagement_CreateUpdateSysMasterListDto_To_SysMasterListInsertOrUpdateRequest_Mapper : MapperBase<CreateUpdateSysMasterListDto, SysMasterListInsertOrUpdateRequest>
{
    public override partial SysMasterListInsertOrUpdateRequest Map(CreateUpdateSysMasterListDto source);
    public override partial void Map(CreateUpdateSysMasterListDto source, SysMasterListInsertOrUpdateRequest destination);
}

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.None)]
public partial class TaskManagement_DeleteSysMasterListDto_To_SysMasterListDeleteRequest_Mapper : MapperBase<DeleteSysMasterListDto, SysMasterListDeleteRequest>
{
    public override partial SysMasterListDeleteRequest Map(DeleteSysMasterListDto source);
    public override partial void Map(DeleteSysMasterListDto source, SysMasterListDeleteRequest destination);
}
#endregion

#region Categories
[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
public partial class TaskManagementCategoryToCategoryDtoMapper : MapperBase<Category, CategoryDto>
{
    public override partial CategoryDto Map(Category source);
    public override partial void Map(Category source, CategoryDto destination);
}

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.None)]
public partial class TaskManagementCreateUpdateCategoryDtoToCategoryMapper : MapperBase<CreateUpdateCategoryDto, Category>
{
    public override partial Category Map(CreateUpdateCategoryDto source);
    public override partial void Map(CreateUpdateCategoryDto source, Category destination);
}
#endregion

#region Tags
[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
public partial class TaskManagementTagToTagDtoMapper : MapperBase<Tag, TagDto>
{
    [MapperIgnoreTarget(nameof(TagDto.CategoryName))]
    public override partial TagDto Map(Tag source);

    [MapperIgnoreTarget(nameof(TagDto.CategoryName))]
    public override partial void Map(Tag source, TagDto destination);
}

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.None)]
public partial class TaskManagementCreateUpdateTagDtoToTagMapper : MapperBase<CreateUpdateTagDto, Tag>
{
    public override partial Tag Map(CreateUpdateTagDto source);
    public override partial void Map(CreateUpdateTagDto source, Tag destination);
}
#endregion

#region Departments
[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.None)]
public partial class TaskManagementDepartmentToDepartmentDtoMapper : MapperBase<Department, DepartmentDto>
{
    public override partial DepartmentDto Map(Department source);
    public override partial void Map(Department source, DepartmentDto destination);
}

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.None)]
public partial class TaskManagementDepartmentToDepartmentTreeDtoMapper : MapperBase<Department, DepartmentTreeDto>
{
    [MapperIgnoreTarget(nameof(DepartmentTreeDto.Children))]
    public override partial DepartmentTreeDto Map(Department source);

    [MapperIgnoreTarget(nameof(DepartmentTreeDto.Children))]
    public override partial void Map(Department source, DepartmentTreeDto destination);
}

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.None)]
public partial class TaskManagementCreateUpdateDepartmentDtoToDepartmentMapper : MapperBase<CreateUpdateDepartmentDto, Department>
{
    public override partial Department Map(CreateUpdateDepartmentDto source);
    public override partial void Map(CreateUpdateDepartmentDto source, Department destination);
}

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.None)]
public partial class TaskManagementDepartmentToCreateUpdateDepartmentDtoMapper : MapperBase<Department, CreateUpdateDepartmentDto>
{
    public override partial CreateUpdateDepartmentDto Map(Department source);
    public override partial void Map(Department source, CreateUpdateDepartmentDto destination);
}
#endregion

#region Tasks
[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.None)]
public partial class TaskManagementTaskItemToTaskDtoMapper : MapperBase<TaskItem, TaskDto>
{
    [MapperIgnoreTarget(nameof(TaskDto.AssigneeName))]
    [MapperIgnoreTarget(nameof(TaskDto.AssigneeUserName))]
    public override partial TaskDto Map(TaskItem source);

    [MapperIgnoreTarget(nameof(TaskDto.AssigneeName))]
    [MapperIgnoreTarget(nameof(TaskDto.AssigneeUserName))]
    public override partial void Map(TaskItem source, TaskDto destination);
}

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.None)]
public partial class TaskManagementCreateTaskInputDtoToTaskItemMapper : MapperBase<CreateTaskInputDto, TaskItem>
{
    [MapperIgnoreTarget(nameof(TaskItem.Id))]
    [MapperIgnoreSource(nameof(CreateTaskInputDto.Attachments))]
    public override partial TaskItem Map(CreateTaskInputDto source);

    [MapperIgnoreTarget(nameof(TaskItem.Id))]
    [MapperIgnoreSource(nameof(CreateTaskInputDto.Attachments))]
    public override partial void Map(CreateTaskInputDto source, TaskItem destination);
}

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.None)]
public partial class TaskManagementUpdateTaskInputDtoToTaskItemMapper : MapperBase<UpdateTaskInputDto, TaskItem>
{
    [MapperIgnoreTarget(nameof(TaskItem.Id))]
    [MapperIgnoreSource(nameof(UpdateTaskInputDto.Attachments))]
    public override partial TaskItem Map(UpdateTaskInputDto source);

    [MapperIgnoreTarget(nameof(TaskItem.Id))]
    [MapperIgnoreSource(nameof(UpdateTaskInputDto.Attachments))]
    public override partial void Map(UpdateTaskInputDto source, TaskItem destination);
}

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.None)]
public partial class TaskManagementTaskAttachmentToTaskAttachmentDtoMapper : MapperBase<TaskAttachment, TaskAttachmentDto>
{
    public override partial TaskAttachmentDto Map(TaskAttachment source);
    public override partial void Map(TaskAttachment source, TaskAttachmentDto destination);
}

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.None)]
public partial class TaskManagementSubTaskToSubTaskDtoMapper : MapperBase<SubTask, SubTaskDto>
{
    [MapperIgnoreTarget(nameof(SubTaskDto.AssigneeName))]
    public override partial SubTaskDto Map(SubTask source);

    [MapperIgnoreTarget(nameof(SubTaskDto.AssigneeName))]
    public override partial void Map(SubTask source, SubTaskDto destination);
}

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.None)]
public partial class TaskManagementTaskChecklistItemToChecklistItemDtoMapper : MapperBase<TaskChecklistItem, ChecklistItemDto>
{
    public override partial ChecklistItemDto Map(TaskChecklistItem source);
    public override partial void Map(TaskChecklistItem source, ChecklistItemDto destination);
}

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.None)]
public partial class TaskManagementTaskActivityLogToTaskActivityLogDtoMapper : MapperBase<TaskActivityLog, TaskActivityLogDto>
{
    public override partial TaskActivityLogDto Map(TaskActivityLog source);
    public override partial void Map(TaskActivityLog source, TaskActivityLogDto destination);
}

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.None)]
public partial class TaskManagementTaskCommentToTaskCommentDtoMapper : MapperBase<TaskComment, TaskCommentDto>
{
    [MapperIgnoreTarget(nameof(TaskCommentDto.CreatorName))]
    public override partial TaskCommentDto Map(TaskComment source);

    [MapperIgnoreTarget(nameof(TaskCommentDto.CreatorName))]
    public override partial void Map(TaskComment source, TaskCommentDto destination);
}

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.None)]
public partial class TaskManagementTaskItemToTaskDetailDtoMapper : MapperBase<TaskItem, TaskDetailDto>
{
    [MapperIgnoreTarget(nameof(TaskDetailDto.AssigneeName))]
    [MapperIgnoreTarget(nameof(TaskDetailDto.AssigneeUserName))]
    [MapperIgnoreTarget(nameof(TaskDetailDto.SubTasks))]
    [MapperIgnoreTarget(nameof(TaskDetailDto.ChecklistItems))]
    [MapperIgnoreTarget(nameof(TaskDetailDto.ActivityLogs))]
    public override partial TaskDetailDto Map(TaskItem source);

    [MapperIgnoreTarget(nameof(TaskDetailDto.AssigneeName))]
    [MapperIgnoreTarget(nameof(TaskDetailDto.AssigneeUserName))]
    [MapperIgnoreTarget(nameof(TaskDetailDto.SubTasks))]
    [MapperIgnoreTarget(nameof(TaskDetailDto.ChecklistItems))]
    [MapperIgnoreTarget(nameof(TaskDetailDto.ActivityLogs))]
    public override partial void Map(TaskItem source, TaskDetailDto destination);
}
#endregion