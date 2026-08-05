namespace QuranDashboard.Application.Abstractions.Security.Permissions;

public static class AbwabPermissionCatalogue
{
    public static IReadOnlyList<PermissionDefinition> All { get; } =
    [
        new(AbwabPermissions.Doors.Create, "إنشاء الأبواب", "Create a root or child door.", "Doors", 1, 1),
        new(AbwabPermissions.Doors.Edit, "تعديل الأبواب", "Edit authored door fields and aliases.", "Doors", 1, 2),
        new(AbwabPermissions.Doors.Move, "نقل الأبواب", "Move one or several doors to another parent or section.", "Doors", 1, 3),
        new(AbwabPermissions.Doors.Reorder, "إعادة ترتيب الأبواب", "Reorder a door in Section or Global scope.", "Doors", 1, 4),
        new(AbwabPermissions.Doors.Archive, "أرشفة الأبواب", "Archive one or several door subtrees.", "Doors", 1, 5),
        new(AbwabPermissions.Doors.Restore, "استعادة الأبواب", "Restore an archived door subtree.", "Doors", 1, 6),
        new(AbwabPermissions.Sections.Create, "إنشاء الأقسام", "Create an Abwab section.", "Sections", 2, 7),
        new(AbwabPermissions.Sections.Edit, "إعادة تسمية الأقسام", "Change a section name.", "Sections", 2, 8),
        new(AbwabPermissions.Sections.Reorder, "إعادة ترتيب الأقسام", "Reorder the live section list.", "Sections", 2, 9),
        new(AbwabPermissions.Sections.Delete, "حذف الأقسام", "Retire an empty section.", "Sections", 2, 10),
        new(AbwabPermissions.Relations.Create, "إنشاء العلاقات", "Add one relation type from an anchor to one or more doors.", "Relations", 3, 11),
        new(AbwabPermissions.Relations.Delete, "حذف العلاقات", "Remove a door relation.", "Relations", 3, 12),
        new(AbwabPermissions.Templates.Create, "إنشاء القوالب", "Create a template and its root node.", "Templates", 4, 13),
        new(AbwabPermissions.Templates.Delete, "حذف القوالب", "Retire a template.", "Templates", 4, 14),
        new(AbwabPermissions.Templates.Apply, "تطبيق القوالب على الأبواب", "Copy template child subtrees into selected doors.", "Templates", 4, 15),
        new(AbwabPermissions.TemplateNodes.Create, "إضافة عناصر القوالب", "Add a child node to a template.", "Template nodes", 5, 16),
        new(AbwabPermissions.TemplateNodes.Edit, "تعديل عناصر القوالب", "Edit a template node; root edit also renames the template.", "Template nodes", 5, 17),
        new(AbwabPermissions.TemplateNodes.Reorder, "إعادة ترتيب عناصر القوالب", "Reorder a non-root template node.", "Template nodes", 5, 18),
        new(AbwabPermissions.TemplateNodes.Delete, "حذف عناصر القوالب", "Retire a non-root node and its subtree.", "Template nodes", 5, 19)
    ];

    static AbwabPermissionCatalogue()
    {
        var codes = All.Select(permission => permission.Code).ToArray();
        if (codes.Any(code => !System.Text.RegularExpressions.Regex.IsMatch(code, "^[a-z0-9]+(\\.[a-z0-9_]+)+$"))
            || codes.Distinct(StringComparer.Ordinal).Count() != codes.Length
            || !All.Select(permission => permission.DisplayOrder).SequenceEqual(Enumerable.Range(1, All.Count))
            || !All.Select(permission => permission.GroupDisplayOrder).Distinct().OrderBy(order => order)
                .SequenceEqual(Enumerable.Range(1, 5)))
        {
            throw new InvalidOperationException("The Abwab permission catalogue is invalid.");
        }
    }
}
