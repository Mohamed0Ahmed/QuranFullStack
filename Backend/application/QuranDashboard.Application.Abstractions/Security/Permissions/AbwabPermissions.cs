namespace QuranDashboard.Application.Abstractions.Security.Permissions;

public static class AbwabPermissions
{
    public static class Doors
    {
        public const string Create = "abwab.doors.create";
        public const string Edit = "abwab.doors.edit";
        public const string Move = "abwab.doors.move";
        public const string Reorder = "abwab.doors.reorder";
        public const string Archive = "abwab.doors.archive";
        public const string Restore = "abwab.doors.restore";
    }

    public static class Sections
    {
        public const string Create = "abwab.sections.create";
        public const string Edit = "abwab.sections.edit";
        public const string Reorder = "abwab.sections.reorder";
        public const string Delete = "abwab.sections.delete";
    }

    public static class Relations
    {
        public const string Create = "abwab.relations.create";
        public const string Delete = "abwab.relations.delete";
    }

    public static class Templates
    {
        public const string Create = "abwab.templates.create";
        public const string Delete = "abwab.templates.delete";
        public const string Apply = "abwab.templates.apply";
    }

    public static class TemplateNodes
    {
        public const string Create = "abwab.template_nodes.create";
        public const string Edit = "abwab.template_nodes.edit";
        public const string Reorder = "abwab.template_nodes.reorder";
        public const string Delete = "abwab.template_nodes.delete";
    }

    public static class Inclusions
    {
        public const string Create = "abwab.inclusions.create";
        public const string Delete = "abwab.inclusions.delete";
    }
}
