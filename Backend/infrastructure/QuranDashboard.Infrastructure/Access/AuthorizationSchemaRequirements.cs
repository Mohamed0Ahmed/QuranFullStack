namespace QuranDashboard.Infrastructure.Access;

internal static class AuthorizationSchemaRequirements
{
    internal static readonly IReadOnlyList<string> Tables =
    [
        "users",
        "permissions",
        "user_permissions",
        "access_audit_events",
    ];

    private const string NoIdentity = "-";
    private const string IdentityByDefault = "d";

    internal static readonly IReadOnlyList<AuthorizationSchemaColumnRequirement> Columns =
    [
        new("users", "normalized_email", false, "text", NoIdentity),
        new("users", "status", false, "integer", NoIdentity),
        new("permissions", "id", false, "integer", IdentityByDefault),
        new("permissions", "code", false, "character varying(128)", NoIdentity),
        new("permissions", "arabic_label", false, "character varying(256)", NoIdentity),
        new("permissions", "english_description", false, "character varying(512)", NoIdentity),
        new("permissions", "display_order", false, "integer", NoIdentity),
        new("permissions", "retired_at", true, "timestamp with time zone", NoIdentity),
        new("user_permissions", "user_id", false, "integer", NoIdentity),
        new("user_permissions", "permission_id", false, "integer", NoIdentity),
        new("user_permissions", "granted_by_user_id", false, "integer", NoIdentity),
        new("user_permissions", "granted_at", false, "timestamp with time zone", NoIdentity),
        new("access_audit_events", "id", false, "bigint", IdentityByDefault),
        new("access_audit_events", "occurred_at", false, "timestamp with time zone", NoIdentity),
        new("access_audit_events", "action_type", false, "character varying(64)", NoIdentity),
        new("access_audit_events", "actor_type", false, "character varying(32)", NoIdentity),
        new("access_audit_events", "actor_user_id", true, "integer", NoIdentity),
        new("access_audit_events", "target_user_id", false, "integer", NoIdentity),
        new("access_audit_events", "actor_snapshot", false, "jsonb", NoIdentity),
        new("access_audit_events", "target_snapshot", false, "jsonb", NoIdentity),
        new("access_audit_events", "permission_code", true, "character varying(128)", NoIdentity),
        new("access_audit_events", "before_state", true, "jsonb", NoIdentity),
        new("access_audit_events", "after_state", true, "jsonb", NoIdentity),
        new("access_audit_events", "reason", true, "character varying(1024)", NoIdentity),
        new("access_audit_events", "metadata", false, "jsonb", NoIdentity),
    ];

    internal static readonly IReadOnlyList<AuthorizationSchemaConstraintRequirement> Constraints =
    [
        new("users", "PK_users", """
            PRIMARY KEY (id)
            """),
        new("permissions", "PK_permissions", """
            PRIMARY KEY (id)
            """),
        new("permissions", "ck_permissions_code_format", """
            CHECK (((code)::text ~ '^[a-z0-9]+(\.[a-z0-9_]+)+$'::text))
            """),
        new("user_permissions", "PK_user_permissions", """
            PRIMARY KEY (user_id, permission_id)
            """),
        new("user_permissions", "FK_user_permissions_permissions_permission_id", """
            FOREIGN KEY (permission_id) REFERENCES permissions(id) ON DELETE RESTRICT
            """),
        new("user_permissions", "FK_user_permissions_users_granted_by_user_id", """
            FOREIGN KEY (granted_by_user_id) REFERENCES users(id) ON DELETE RESTRICT
            """),
        new("user_permissions", "FK_user_permissions_users_user_id", """
            FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE RESTRICT
            """),
        new("access_audit_events", "PK_access_audit_events", """
            PRIMARY KEY (id)
            """),
        new("access_audit_events", "FK_access_audit_events_users_actor_user_id", """
            FOREIGN KEY (actor_user_id) REFERENCES users(id) ON DELETE RESTRICT
            """),
        new("access_audit_events", "FK_access_audit_events_users_target_user_id", """
            FOREIGN KEY (target_user_id) REFERENCES users(id) ON DELETE RESTRICT
            """),
        new("access_audit_events", "ck_access_audit_events_documents_are_objects", """
            CHECK (((jsonb_typeof(actor_snapshot) = 'object'::text)
            AND (jsonb_typeof(target_snapshot) = 'object'::text)
            AND ((before_state IS NULL) OR (jsonb_typeof(before_state) = 'object'::text))
            AND ((after_state IS NULL) OR (jsonb_typeof(after_state) = 'object'::text))))
            """),
        new("access_audit_events", "ck_access_audit_events_metadata_schema_version", """
            CHECK (((jsonb_typeof(metadata) = 'object'::text)
            AND jsonb_exists(metadata, 'schemaVersion'::text)
            AND (jsonb_typeof((metadata -> 'schemaVersion'::text)) = 'number'::text)
            AND ((metadata ->> 'schemaVersion'::text) ~ '^[1-9][0-9]*$'::text)
            AND (((metadata ->> 'schemaVersion'::text))::numeric <= (2147483647)::numeric)))
            """),
    ];

    internal static readonly IReadOnlyList<AuthorizationSchemaIndexRequirement> Indexes =
    [
        new("users", "PK_users", """
            CREATE UNIQUE INDEX "PK_users" ON users USING btree (id)
            """),
        new("users", "IX_users_normalized_email", """
            CREATE UNIQUE INDEX "IX_users_normalized_email" ON users USING btree (normalized_email)
            """),
        new("users", "IX_users_status_id", """
            CREATE INDEX "IX_users_status_id" ON users USING btree (status, id)
            """),
        new("permissions", "PK_permissions", """
            CREATE UNIQUE INDEX "PK_permissions" ON permissions USING btree (id)
            """),
        new("permissions", "IX_permissions_code", """
            CREATE UNIQUE INDEX "IX_permissions_code" ON permissions USING btree (code)
            """),
        new("permissions", "IX_permissions_display_order", """
            CREATE INDEX "IX_permissions_display_order" ON permissions USING btree (display_order)
            """),
        new("user_permissions", "PK_user_permissions", """
            CREATE UNIQUE INDEX "PK_user_permissions" ON user_permissions
            USING btree (user_id, permission_id)
            """),
        new("user_permissions", "IX_user_permissions_user_id", """
            CREATE INDEX "IX_user_permissions_user_id" ON user_permissions USING btree (user_id)
            """),
        new("user_permissions", "IX_user_permissions_permission_id", """
            CREATE INDEX "IX_user_permissions_permission_id" ON user_permissions
            USING btree (permission_id)
            """),
        new("user_permissions", "IX_user_permissions_granted_by_user_id", """
            CREATE INDEX "IX_user_permissions_granted_by_user_id" ON user_permissions
            USING btree (granted_by_user_id)
            """),
        new("access_audit_events", "PK_access_audit_events", """
            CREATE UNIQUE INDEX "PK_access_audit_events" ON access_audit_events USING btree (id)
            """),
        new("access_audit_events", "IX_access_audit_events_action_type_occurred_at", """
            CREATE INDEX "IX_access_audit_events_action_type_occurred_at" ON access_audit_events
            USING btree (action_type, occurred_at DESC)
            """),
        new("access_audit_events", "IX_access_audit_events_actor_user_id", """
            CREATE INDEX "IX_access_audit_events_actor_user_id" ON access_audit_events
            USING btree (actor_user_id)
            """),
        new("access_audit_events", "IX_access_audit_events_occurred_at_id", """
            CREATE INDEX "IX_access_audit_events_occurred_at_id" ON access_audit_events
            USING btree (occurred_at DESC, id DESC)
            """),
        new("access_audit_events", "IX_access_audit_events_permission_code", """
            CREATE INDEX "IX_access_audit_events_permission_code" ON access_audit_events
            USING btree (permission_code) WHERE (permission_code IS NOT NULL)
            """),
        new("access_audit_events", "IX_access_audit_events_target_user_id_occurred_at_id", """
            CREATE INDEX "IX_access_audit_events_target_user_id_occurred_at_id" ON access_audit_events
            USING btree (target_user_id, occurred_at DESC, id DESC)
            """),
    ];
}

internal sealed record AuthorizationSchemaColumnRequirement(
    string TableName,
    string ColumnName,
    bool IsNullable,
    string DataType,
    string Identity);

internal sealed record AuthorizationSchemaConstraintRequirement(
    string TableName,
    string ConstraintName,
    string Definition);

internal sealed record AuthorizationSchemaIndexRequirement(
    string TableName,
    string IndexName,
    string Definition);
