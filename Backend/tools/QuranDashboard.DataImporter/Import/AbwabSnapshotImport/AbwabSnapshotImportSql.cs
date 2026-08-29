namespace QuranDashboard.DataImporter.Import.AbwabSnapshotImport;

internal static class AbwabSnapshotImportSql
{
    internal const string LockTables = """
        LOCK TABLE
            public.abwab_sections,
            public.abwab_templates,
            public.abwab_doors,
            public.abwab_template_nodes,
            public.abwab_door_aliases,
            public.abwab_door_relations,
            public.abwab_door_inclusions,
            public.abwab_door_inclusion_unit_syncs
        IN ACCESS EXCLUSIVE MODE
        """;

    internal const string ReadMigrationHead =
        "SELECT \"MigrationId\" FROM \"__EFMigrationsHistory\" ORDER BY \"MigrationId\" DESC LIMIT 1";

    internal const string ReadTableNames = """
        SELECT table_name
        FROM information_schema.tables
        WHERE table_schema = 'public'
          AND table_type = 'BASE TABLE'
          AND table_name LIKE 'abwab\_%' ESCAPE '\'
        ORDER BY table_name
        """;

    internal const string ReadSchema = """
        SELECT table_name, column_name, data_type, is_nullable, ordinal_position
        FROM information_schema.columns
        WHERE table_schema = 'public'
          AND table_name = ANY (@tables)
          AND column_name <> 'xmin'
        ORDER BY array_position(@tables, table_name), ordinal_position
        """;

    internal const string ReadCounts = """
        SELECT 'abwab_sections', count(*)::int,
               count(*) FILTER (WHERE deleted_at IS NULL)::int,
               count(*) FILTER (WHERE deleted_at IS NOT NULL)::int FROM public.abwab_sections
        UNION ALL
        SELECT 'abwab_doors', count(*)::int,
               count(*) FILTER (WHERE deleted_at IS NULL)::int,
               count(*) FILTER (WHERE deleted_at IS NOT NULL)::int FROM public.abwab_doors
        UNION ALL
        SELECT 'abwab_door_aliases', count(*)::int,
               count(*) FILTER (WHERE deleted_at IS NULL)::int,
               count(*) FILTER (WHERE deleted_at IS NOT NULL)::int FROM public.abwab_door_aliases
        UNION ALL
        SELECT 'abwab_door_relations', count(*)::int,
               count(*) FILTER (WHERE deleted_at IS NULL)::int,
               count(*) FILTER (WHERE deleted_at IS NOT NULL)::int FROM public.abwab_door_relations
        UNION ALL
        SELECT 'abwab_templates', count(*)::int,
               count(*) FILTER (WHERE deleted_at IS NULL)::int,
               count(*) FILTER (WHERE deleted_at IS NOT NULL)::int FROM public.abwab_templates
        UNION ALL
        SELECT 'abwab_template_nodes', count(*)::int,
               count(*) FILTER (WHERE deleted_at IS NULL)::int,
               count(*) FILTER (WHERE deleted_at IS NOT NULL)::int FROM public.abwab_template_nodes
        UNION ALL
        SELECT 'abwab_door_inclusions', count(*)::int,
               count(*) FILTER (WHERE deleted_at IS NULL)::int,
               count(*) FILTER (WHERE deleted_at IS NOT NULL)::int FROM public.abwab_door_inclusions
        UNION ALL
        SELECT 'abwab_door_inclusion_unit_syncs', count(*)::int, NULL::int, NULL::int
        FROM public.abwab_door_inclusion_unit_syncs
        """;

    internal const string ReadIds = """
        SELECT 'abwab_sections', id::bigint FROM public.abwab_sections
        UNION ALL SELECT 'abwab_doors', id::bigint FROM public.abwab_doors
        UNION ALL SELECT 'abwab_door_aliases', id::bigint FROM public.abwab_door_aliases
        UNION ALL SELECT 'abwab_door_relations', id::bigint FROM public.abwab_door_relations
        UNION ALL SELECT 'abwab_templates', id::bigint FROM public.abwab_templates
        UNION ALL SELECT 'abwab_template_nodes', id::bigint FROM public.abwab_template_nodes
        UNION ALL SELECT 'abwab_door_inclusions', id::bigint FROM public.abwab_door_inclusions
        UNION ALL SELECT 'abwab_door_inclusion_unit_syncs', id::bigint
            FROM public.abwab_door_inclusion_unit_syncs
        ORDER BY 1, 2
        """;

    internal const string ReadMissingReferences = """
        SELECT (
            (SELECT count(*) FROM public.abwab_doors child
             LEFT JOIN public.abwab_sections parent ON parent.id = child.section_id
             WHERE parent.id IS NULL)
          + (SELECT count(*) FROM public.abwab_doors child
             LEFT JOIN public.abwab_doors parent ON parent.id = child.parent_id
             WHERE child.parent_id IS NOT NULL AND parent.id IS NULL)
          + (SELECT count(*) FROM public.abwab_door_aliases child
             LEFT JOIN public.abwab_doors parent ON parent.id = child.door_id
             WHERE parent.id IS NULL)
          + (SELECT count(*) FROM public.abwab_door_relations child
             LEFT JOIN public.abwab_doors parent ON parent.id = child.door_a_id
             WHERE parent.id IS NULL)
          + (SELECT count(*) FROM public.abwab_door_relations child
             LEFT JOIN public.abwab_doors parent ON parent.id = child.door_b_id
             WHERE parent.id IS NULL)
          + (SELECT count(*) FROM public.abwab_door_relations child
             LEFT JOIN public.abwab_doors parent ON parent.id = child.broader_door_id
             WHERE child.broader_door_id IS NOT NULL AND parent.id IS NULL)
          + (SELECT count(*) FROM public.abwab_template_nodes child
             LEFT JOIN public.abwab_templates parent ON parent.id = child.template_id
             WHERE parent.id IS NULL)
          + (SELECT count(*) FROM public.abwab_template_nodes child
             LEFT JOIN public.abwab_template_nodes parent ON parent.id = child.parent_node_id
             WHERE child.parent_node_id IS NOT NULL AND parent.id IS NULL)
          + (SELECT count(*) FROM public.abwab_door_inclusions child
             LEFT JOIN public.abwab_doors parent ON parent.id = child.target_door_id
             WHERE parent.id IS NULL)
          + (SELECT count(*) FROM public.abwab_door_inclusions child
             LEFT JOIN public.abwab_doors parent ON parent.id = child.source_door_id
             WHERE parent.id IS NULL)
        )::bigint
        """;

    internal const string InsertSections = """
        INSERT INTO public.abwab_sections
            (id, name, order_value, created_at, created_by, updated_at, updated_by,
             approved_at, approved_by, deleted_at, deleted_by)
        OVERRIDING SYSTEM VALUE
        SELECT id, name, order_value, created_at, created_by, updated_at, updated_by,
               approved_at, approved_by, deleted_at, deleted_by
        FROM jsonb_populate_recordset(NULL::public.abwab_sections, @rows)
        """;

    internal const string InsertTemplates = """
        INSERT INTO public.abwab_templates
            (id, created_at, created_by, updated_at, updated_by,
             approved_at, approved_by, deleted_at, deleted_by)
        OVERRIDING SYSTEM VALUE
        SELECT id, created_at, created_by, updated_at, updated_by,
               approved_at, approved_by, deleted_at, deleted_by
        FROM jsonb_populate_recordset(NULL::public.abwab_templates, @rows)
        """;

    internal const string InsertDoors = """
        INSERT INTO public.abwab_doors
            (id, section_id, parent_id, name, description, representative_ayah_text,
             order_value, global_order_value, created_at, created_by, updated_at, updated_by,
             approved_at, approved_by, deleted_at, deleted_by)
        OVERRIDING SYSTEM VALUE
        SELECT id, section_id, parent_id, name, description, representative_ayah_text,
               order_value, global_order_value, created_at, created_by, updated_at, updated_by,
               approved_at, approved_by, deleted_at, deleted_by
        FROM jsonb_populate_recordset(NULL::public.abwab_doors, @rows)
        """;

    internal const string InsertTemplateNodes = """
        INSERT INTO public.abwab_template_nodes
            (id, template_id, parent_node_id, name, description, representative_ayah_text,
             aliases, order_value, created_at, created_by, updated_at, updated_by,
             approved_at, approved_by, deleted_at, deleted_by)
        OVERRIDING SYSTEM VALUE
        SELECT id, template_id, parent_node_id, name, description, representative_ayah_text,
               aliases, order_value, created_at, created_by, updated_at, updated_by,
               approved_at, approved_by, deleted_at, deleted_by
        FROM jsonb_populate_recordset(NULL::public.abwab_template_nodes, @rows)
        """;

    internal const string InsertAliases = """
        INSERT INTO public.abwab_door_aliases
            (id, door_id, value, created_at, created_by, updated_at, updated_by,
             approved_at, approved_by, deleted_at, deleted_by)
        OVERRIDING SYSTEM VALUE
        SELECT id, door_id, value, created_at, created_by, updated_at, updated_by,
               approved_at, approved_by, deleted_at, deleted_by
        FROM jsonb_populate_recordset(NULL::public.abwab_door_aliases, @rows)
        """;

    internal const string InsertRelations = """
        INSERT INTO public.abwab_door_relations
            (id, door_a_id, door_b_id, relation_type, broader_door_id,
             created_at, created_by, updated_at, updated_by,
             approved_at, approved_by, deleted_at, deleted_by)
        OVERRIDING SYSTEM VALUE
        SELECT id, door_a_id, door_b_id, relation_type, broader_door_id,
               created_at, created_by, updated_at, updated_by,
               approved_at, approved_by, deleted_at, deleted_by
        FROM jsonb_populate_recordset(NULL::public.abwab_door_relations, @rows)
        """;

    internal const string InsertInclusions = """
        INSERT INTO public.abwab_door_inclusions
            (id, target_door_id, source_door_id, created_at, created_by,
             updated_at, updated_by, deleted_at, deleted_by)
        OVERRIDING SYSTEM VALUE
        SELECT id, target_door_id, source_door_id, created_at, created_by,
               updated_at, updated_by, deleted_at, deleted_by
        FROM jsonb_populate_recordset(NULL::public.abwab_door_inclusions, @rows)
        """;

    internal const string ResetIdentitySequences = """
        DO $reset$
        DECLARE next_value bigint;
        BEGIN
            SELECT COALESCE(max(id)::bigint + 1, 1) INTO next_value FROM public.abwab_sections;
            EXECUTE format('ALTER SEQUENCE public.abwab_sections_id_seq RESTART WITH %s', next_value);
            SELECT COALESCE(max(id)::bigint + 1, 1) INTO next_value FROM public.abwab_doors;
            EXECUTE format('ALTER SEQUENCE public.abwab_doors_id_seq RESTART WITH %s', next_value);
            SELECT COALESCE(max(id)::bigint + 1, 1) INTO next_value FROM public.abwab_door_aliases;
            EXECUTE format('ALTER SEQUENCE public.abwab_door_aliases_id_seq RESTART WITH %s', next_value);
            SELECT COALESCE(max(id)::bigint + 1, 1) INTO next_value FROM public.abwab_door_relations;
            EXECUTE format('ALTER SEQUENCE public.abwab_door_relations_id_seq RESTART WITH %s', next_value);
            SELECT COALESCE(max(id)::bigint + 1, 1) INTO next_value FROM public.abwab_templates;
            EXECUTE format('ALTER SEQUENCE public.abwab_templates_id_seq RESTART WITH %s', next_value);
            SELECT COALESCE(max(id)::bigint + 1, 1) INTO next_value FROM public.abwab_template_nodes;
            EXECUTE format('ALTER SEQUENCE public.abwab_template_nodes_id_seq RESTART WITH %s', next_value);
            SELECT COALESCE(max(id)::bigint + 1, 1) INTO next_value FROM public.abwab_door_inclusions;
            EXECUTE format('ALTER SEQUENCE public.abwab_door_inclusions_id_seq RESTART WITH %s', next_value);
            SELECT COALESCE(max(id)::bigint + 1, 1) INTO next_value
            FROM public.abwab_door_inclusion_unit_syncs;
            EXECUTE format(
                'ALTER SEQUENCE public.abwab_door_inclusion_unit_syncs_id_seq RESTART WITH %s',
                next_value);
        END
        $reset$;
        """;

    internal const string ReadSequenceStates = """
        SELECT 'abwab_sections', last_value::bigint, is_called
        FROM public.abwab_sections_id_seq
        UNION ALL SELECT 'abwab_doors', last_value::bigint, is_called
        FROM public.abwab_doors_id_seq
        UNION ALL SELECT 'abwab_door_aliases', last_value::bigint, is_called
        FROM public.abwab_door_aliases_id_seq
        UNION ALL SELECT 'abwab_door_relations', last_value::bigint, is_called
        FROM public.abwab_door_relations_id_seq
        UNION ALL SELECT 'abwab_templates', last_value::bigint, is_called
        FROM public.abwab_templates_id_seq
        UNION ALL SELECT 'abwab_template_nodes', last_value::bigint, is_called
        FROM public.abwab_template_nodes_id_seq
        UNION ALL SELECT 'abwab_door_inclusions', last_value::bigint, is_called
        FROM public.abwab_door_inclusions_id_seq
        UNION ALL SELECT 'abwab_door_inclusion_unit_syncs', last_value::bigint, is_called
        FROM public.abwab_door_inclusion_unit_syncs_id_seq
        """;
}
