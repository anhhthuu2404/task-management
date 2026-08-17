using TaskManagement.Localization;
using Volo.Abp.Authorization.Permissions;
using Volo.Abp.Localization;
using Volo.Abp.MultiTenancy;

namespace TaskManagement.Permissions;

public class TaskManagementPermissionDefinitionProvider : PermissionDefinitionProvider
{
    public override void Define(IPermissionDefinitionContext context)
    {
        var myGroup = context.AddGroup(TaskManagementPermissions.GroupName);

        var booksPermission = myGroup.AddPermission(TaskManagementPermissions.Books.Default, L("Permission:Books"));
        booksPermission.AddChild(TaskManagementPermissions.Books.Create, L("Permission:Books.Create"));
        booksPermission.AddChild(TaskManagementPermissions.Books.Edit, L("Permission:Books.Edit"));
        booksPermission.AddChild(TaskManagementPermissions.Books.Delete, L("Permission:Books.Delete"));

        var languagesPermission = myGroup.AddPermission(TaskManagementPermissions.Languages.Default, L("Permission:Languages"));
        languagesPermission.AddChild(TaskManagementPermissions.Languages.Create, L("Permission:Languages.Create"));
        languagesPermission.AddChild(TaskManagementPermissions.Languages.Edit, L("Permission:Languages.Edit"));
        languagesPermission.AddChild(TaskManagementPermissions.Languages.Delete, L("Permission:Languages.Delete"));

        var languageTextsPermission = myGroup.AddPermission(TaskManagementPermissions.LanguageTexts.Default, L("Permission:LanguageTexts"));
        languageTextsPermission.AddChild(TaskManagementPermissions.LanguageTexts.Create, L("Permission:LanguageTexts.Create"));
        languageTextsPermission.AddChild(TaskManagementPermissions.LanguageTexts.Edit, L("Permission:LanguageTexts.Edit"));
        languageTextsPermission.AddChild(TaskManagementPermissions.LanguageTexts.Delete, L("Permission:LanguageTexts.Delete"));

        var sysMasterListsPermission = myGroup.AddPermission(TaskManagementPermissions.SysMasterLists.Default, L("Permission:SysMasterLists"));
        sysMasterListsPermission.AddChild(TaskManagementPermissions.SysMasterLists.Create, L("Permission:SysMasterLists.Create"));
        sysMasterListsPermission.AddChild(TaskManagementPermissions.SysMasterLists.Edit, L("Permission:SysMasterLists.Edit"));
        sysMasterListsPermission.AddChild(TaskManagementPermissions.SysMasterLists.Delete, L("Permission:SysMasterLists.Delete"));

        // 1. Phân quyền Categories
        var categoriesPermission = myGroup.AddPermission(TaskManagementPermissions.Categories.Default, L("Permission:Categories"));
        categoriesPermission.AddChild(TaskManagementPermissions.Categories.Create, L("Permission:Categories.Create"));
        categoriesPermission.AddChild(TaskManagementPermissions.Categories.Edit, L("Permission:Categories.Edit"));
        categoriesPermission.AddChild(TaskManagementPermissions.Categories.Delete, L("Permission:Categories.Delete"));

        // 2. Phân quyền Tags
        var tagsPermission = myGroup.AddPermission(TaskManagementPermissions.Tags.Default, L("Permission:Tags"));
        tagsPermission.AddChild(TaskManagementPermissions.Tags.Create, L("Permission:Tags.Create"));
        tagsPermission.AddChild(TaskManagementPermissions.Tags.Edit, L("Permission:Tags.Edit"));
        tagsPermission.AddChild(TaskManagementPermissions.Tags.Delete, L("Permission:Tags.Delete"));

        // 3. Phân quyền Departments
        var departmentsPermission = myGroup.AddPermission(TaskManagementPermissions.Departments.Default, L("Permission:Departments"));
        departmentsPermission.AddChild(TaskManagementPermissions.Departments.Create, L("Permission:Departments.Create"));
        departmentsPermission.AddChild(TaskManagementPermissions.Departments.Edit, L("Permission:Departments.Edit"));
        departmentsPermission.AddChild(TaskManagementPermissions.Departments.Delete, L("Permission:Departments.Delete"));
        departmentsPermission.AddChild(TaskManagementPermissions.Departments.ManageUsers, L("Permission:Departments.ManageUsers"));
        departmentsPermission.AddChild(TaskManagementPermissions.Departments.AssignUser, L("Permission:Departments.AssignUser"));
    }

    private static LocalizableString L(string name)
    {
        return LocalizableString.Create<TaskManagementResource>(name);
    }
}