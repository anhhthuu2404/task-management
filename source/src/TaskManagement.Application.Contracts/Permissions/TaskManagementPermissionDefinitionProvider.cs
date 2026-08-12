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

        //Define your own permissions here. Example:
        //myGroup.AddPermission(TaskManagementPermissions.MyPermission1, L("Permission:MyPermission1"));
    }

    private static LocalizableString L(string name)
    {
        return LocalizableString.Create<TaskManagementResource>(name);
    }
}
