# Authorization and Granular Permissions: Product and Architecture Decisions

- **Decision date:** 2026-08-05
- **Status:** Accepted for implementation planning
- **Inspection baseline:** [`authorization-permissions-current-state-report.md`](authorization-permissions-current-state-report.md), repository `dev` at `5dea4ec0121cb0f91409ac9fb1a5709fa9e04e7c`

This record freezes the product and architecture decisions that follow from the current-state inspection. It is not current implementation truth and is not an implementation plan. Live behavior remains defined by code and the nearest README until this design is implemented.

## 1. Purpose and scope

This record defines the v1 authorization boundary for:

- Logto identity and local application-account responsibilities;
- public content reads versus administrative/security access;
- multiple configured Owners;
- active non-owner users with direct granular write permissions;
- the 19 permissions required by the 21 currently implemented Abwab writes;
- Owner-only account, permission, relink, and audit administration;
- activation, disable, reactivation, and append-only audit semantics;
- Backend and frontend authorization contracts;
- data migration, fail-closed enforcement, and mandatory test obligations.

The endpoint and permission baseline is the verified controller/catalogue inventory in the [current-state report §5](authorization-permissions-current-state-report.md#5-complete-abwab-endpoint-permission-matrix). Target decisions in this record supersede that report where it proposed authenticated active-administrator access for content reads. The report’s inspection facts, write-route inventory, permission catalogue, composite-action analysis, and testing-debt evidence remain the baseline.

## 2. Locked public-read model

Normal project content is public. Authentication, local account status, Owner status, and direct permissions do not control normal reads.

### 2.1 Public content

The following remain anonymous/public:

- Quran data reads;
- Mushaf reads;
- Words reads;
- Dashboard/content information reads;
- the four current Abwab reads;
- future normal content or research reads;
- `GET /api/health`.

The current route catalogue contains 73 routes across Words, Mushaf, Dashboard, Health, Access, and Abwab, and all routes except `/api/access/me` are currently open. This existing read posture is compatible with the target; the security defect is the currently open unsafe surface, not the public content GETs (`Backend/tests/QuranDashboard.Tests/Smoke/SmokeRouteCatalog.cs:117-359`, `Backend/api/QuranDashboard.Api/Controllers/Access/AccessController.cs:7-15`).

No read permission will be created. No global authenticated-user or active-user fallback policy will be applied to public content. Pending, Disabled, active read-only, unprovisioned, and anonymous people may all browse public content.

### 2.2 Non-public read exceptions

There are two explicit non-public read classes:

| Read class | Rule |
|---|---|
| `GET /api/access/me` | Authentication required. It provisions or returns the current local account and may return `Pending` or `Disabled` as `200`. |
| Future user-management, permission-management, access-audit, Owner reconciliation, and other security-administration reads | Active Owner only. These are administrative security data, not public project content. |

The current `/api/access/me` class-level `[Authorize]` is the only live endpoint authorization attribute (`Backend/api/QuranDashboard.Api/Controllers/Access/AccessController.cs:7-15`). Future security-administration GETs must carry explicit Owner-only metadata; they must never inherit public-read behavior merely because they use `GET`.

### 2.3 Resolution of the current-state recommendation

The “active read” future rules in the [current-state report §5](authorization-permissions-current-state-report.md#5-complete-abwab-endpoint-permission-matrix), the active-administrator fallback in [§9](authorization-permissions-current-state-report.md#9-proposed-backend-authorization-direction), and active-user route guards in [§10](authorization-permissions-current-state-report.md#10-frontend-integration-direction) are superseded.

The adopted split is:

- **public content GET** → anonymous;
- **`/api/access/me`** → authenticated;
- **security-administration GET** → active Owner;
- **administrative write** → authenticated, active local account, and exact permission or active Owner.

## 3. Logto/application responsibility boundary

| Concern | Authority | Contract |
|---|---|---|
| Login, logout, sessions, token issuance, JWT signature/issuer/audience, and `sub` | Logto | The API authenticates the Logto token. `sub` is preserved without inbound claim remapping (`Backend/api/QuranDashboard.Api/Authentication/AuthenticationRegistration.cs:29-48`, `Backend/api/QuranDashboard.Api/Authentication/HttpContextCurrentUser.cs:5-20`). |
| Primary email identity and verification | Logto | The Backend obtains these through the server-side Logto integration. It never trusts a client-supplied email or verification flag (`Backend/infrastructure/QuranDashboard.Infrastructure/Access/LogtoManagementApiUserProfileSource.cs:22-47`, `Backend/application/QuranDashboard.Application.Abstractions/Security/IExternalUserProfileSource.cs:3-8`). |
| Local user, `Pending`/`Active`/`Disabled`, Owner role reference, and direct permission grants | Application database | These are the only application-authorization inputs. |
| Owner configuration | Environment configuration plus verified Logto identity | Configuration defines desired Owner membership; reconciliation writes the authoritative local Owner state. |
| Authorization decision | Application database | Token-borne role or permission claims are ignored. A token proves identity, not application authority. |
| Public content access | Public endpoint contract | No local-user lookup is required to authorize a normal content GET. |

`ICurrentUser` remains the narrow authenticated-identity boundary that exposes `sub` (`Backend/application/QuranDashboard.Application.Abstractions/Security/ICurrentUser.cs:3-6`). `IUserRoleResolver` and `RoleClaimsTransformation` must not become permission-authority paths. The current transformation already prevents a token role claim from bypassing its database role load, but the target authorization decision is made from one local access snapshot rather than transformed role claims (`Backend/api/QuranDashboard.Api/Authentication/RoleClaimsTransformation.cs:7-42`, `Backend/tests/QuranDashboard.Tests/Api/Access/RoleClaimsTransformationTests.cs:20-46`).

Same-email/different-`sub` remains a conflict. Email is not a substitute identity key and never authorizes an automatic relink (`Backend/infrastructure/QuranDashboard.Infrastructure/Access/UserProvisioningService.cs:99-115`, `Backend/tests/QuranDashboard.Tests/Api/Access/AccessMeEndpointTests.cs:116-145`).

## 4. Multiple-Owner configuration and reconciliation contract

### 4.1 Cardinality and configuration

Several Owners may coexist. The database has one seeded `Owner` role row, referenced by zero or more users during migration and by one or more valid users before enforcement is activated.

The canonical configuration section is:

```text
OwnerBootstrap:Emails
```

Environment array binding uses:

```text
OwnerBootstrap__Emails__0
OwnerBootstrap__Emails__1
OwnerBootstrap__Emails__2
```

Each configured value must be trimmed, parsed as a valid email address, normalized using one invariant case-insensitive comparison, and unique after normalization. Invalid or duplicate normalized entries fail configuration validation; they are not silently ignored.

The current implementation accepts one optional `Auth:BootstrapOwnerEmail` and performs an ordinal case-insensitive comparison (`Backend/infrastructure/QuranDashboard.Infrastructure/Access/OwnerBootstrapOptions.cs:6-26`, `Backend/infrastructure/QuranDashboard.Infrastructure/Access/UserProvisioningService.cs:119-127`). Replacing that singular option with the validated list is a required implementation gap, not a remaining product decision.

### 4.2 Source of Owner membership

Owner membership requires all of:

1. an email in the normalized configured Owner set;
2. a Logto identity whose primary email matches that value;
3. server-side confirmation that the Logto email identity is verified;
4. a successfully reconciled local user whose `RoleId` references the one `Owner` role.

The current Logto profile adapter derives `EmailVerified` from linked social/SSO identities rather than a direct verification property (`Backend/infrastructure/QuranDashboard.Infrastructure/Access/LogtoManagementApiUserProfileSource.cs:40-47`). Before Owner enforcement is activated, implementation planning must identify and verify the tenant-authoritative server-side verification signal. An unvalidated inference cannot satisfy the Owner-verification requirement.

Owner status must never be granted by:

- a dashboard checkbox or normal user-management operation;
- a direct permission grant;
- a token role or permission claim;
- email matching without server-side verification;
- a generic role-management endpoint.

Owners carry no `UserPermissions` rows. Their unrestricted authority comes from the central active-Owner bypass.

### 4.3 Reconciliation

Owner configuration is desired state. A dedicated, idempotent reconciliation operation applies it to application-database state. It is a trusted deployment/recovery operation, not a normal dashboard endpoint and not per-request authorization logic.

The reconciliation contract is:

1. validate and normalize the complete configured list;
2. resolve each candidate through trusted Logto data and require verified matching identity;
3. identify additions, removals, unresolved candidates, and unchanged Owners;
4. reject any change that would leave enforcement without at least one verified, local, `Active` configured Owner;
5. apply the accepted membership changes transactionally;
6. append one audit event per effective Owner membership change, with system actor, target, before/after state, timestamp, and deployment/configuration metadata;
7. publish a success/failure result suitable for deployment preflight.

A configured identity that has not yet produced a local user is unresolved and does not count toward the production Owner preflight. Provisioning that identity through `/api/access/me` may invoke the same guarded reconciliation rules after server verification; it must append the same audit evidence.

Adding a verified configured Owner may move a `Pending` user to `Active` and attach the Owner role. It must not reactivate a `Disabled` user. Removing an email from the configured set removes that user’s Owner role while preserving the user’s current status; because Owners have no direct grants, an active demoted user becomes write-disabled until an Owner explicitly grants permissions.

There is no last-Owner deletion path. Production authorization enforcement must refuse activation when no verified local active configured Owner exists or when desired configuration and reconciled database membership are inconsistent.

## 5. Owner and non-owner authorization model

| User state | Public content reads | Administrative writes | Security-administration reads/writes |
|---|---|---|---|
| Anonymous | Allowed | `401` | `401` |
| Authenticated, no local user yet | Allowed | `403`; write paths do not provision implicitly | `403` |
| `Pending` non-owner | Allowed | `403` | `403` |
| `Disabled` non-owner | Allowed | `403` | `403` |
| `Active` non-owner, no grant | Allowed | `403` | `403` |
| `Active` non-owner, exact direct grant | Allowed | Allowed only for the mapped action | `403` |
| `Disabled` Owner | Allowed | `403` | `403` |
| `Active` Owner | Allowed | Allowed without direct grants | Allowed |

`Owner` is the only named role. Multiple users may reference it. Every non-owner has `RoleId = null`; no Admin, Editor, Supervisor, or replacement role is permitted.

The Owner bypass occurs only after authentication and `UserStatus.Active` are established. It bypasses granular permission requirements, not model validation, domain rules, optimistic concurrency, conflicts, resource existence checks, or other business refusals.

The current `UserStatus` values already provide the required account states: `Pending = 1`, `Active = 2`, and `Disabled = 3` (`Backend/domain/QuranDashboard.Domain/Access/UserStatus.cs:5-10`). The nullable role FK already permits role-less active users (`Backend/domain/QuranDashboard.Domain/Access/User.cs:12-18`, `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Configurations/Access/UserConfiguration.cs:33-39`).

## 6. Canonical 19-permission catalogue with Arabic labels

Permission codes use the canonical form:

```text
<bounded_context>.<plural_resource>.<verb>
```

They are lowercase, dot-separated, business-action identities. Multiword resource segments use snake case. Codes are immutable after release; labels and descriptions may be corrected without changing authorization identity. Numeric permission IDs are database details and never appear in attributes, API authorization checks, or frontend permission checks.

| Code | Arabic label | Group | Technical action | Current actions covered |
|---|---|---|---|---|
| `abwab.doors.create` | إنشاء الأبواب | Doors | Create a root or child door. | `AbwabDoorsController.Create` |
| `abwab.doors.edit` | تعديل الأبواب | Doors | Edit authored door fields and aliases. | `AbwabDoorsController.Edit` |
| `abwab.doors.move` | نقل الأبواب | Doors | Move one or several doors to another parent/section. | `AbwabDoorsController.Move`, `BulkMove` |
| `abwab.doors.reorder` | إعادة ترتيب الأبواب | Doors | Reorder a door in Section or Global scope. | `AbwabDoorsController.Reorder` |
| `abwab.doors.archive` | أرشفة الأبواب | Doors | Archive one or several door subtrees. | `AbwabDoorsController.Delete`, `BulkArchive` |
| `abwab.doors.restore` | استعادة الأبواب | Doors | Restore an archived door subtree. | `AbwabDoorsController.Restore` |
| `abwab.sections.create` | إنشاء الأقسام | Sections | Create an Abwab section. | `AbwabSectionsController.Create` |
| `abwab.sections.edit` | إعادة تسمية الأقسام | Sections | Change a section name. | `AbwabSectionsController.Rename` |
| `abwab.sections.reorder` | إعادة ترتيب الأقسام | Sections | Reorder the live section list. | `AbwabSectionsController.Reorder` |
| `abwab.sections.delete` | حذف الأقسام | Sections | Retire an empty section. | `AbwabSectionsController.Delete` |
| `abwab.relations.create` | إنشاء العلاقات | Relations | Add one relation type from an anchor to one or more doors. | `AbwabDoorRelationsController.AddForDoor` |
| `abwab.relations.delete` | حذف العلاقات | Relations | Remove a door relation. | `AbwabDoorRelationsController.Delete` |
| `abwab.templates.create` | إنشاء القوالب | Templates | Create a template and its root node. | `AbwabTemplatesController.Create` |
| `abwab.templates.delete` | حذف القوالب | Templates | Retire a template. | `AbwabTemplatesController.Delete` |
| `abwab.templates.apply` | تطبيق القوالب على الأبواب | Templates | Copy template child subtrees into selected doors. | `AbwabTemplatesController.Apply` |
| `abwab.template_nodes.create` | إضافة عناصر القوالب | Template nodes | Add a child node to a template. | `AbwabTemplateNodesController.Add` |
| `abwab.template_nodes.edit` | تعديل عناصر القوالب | Template nodes | Edit a template node; root edit also renames the template. | `AbwabTemplateNodesController.Edit` |
| `abwab.template_nodes.reorder` | إعادة ترتيب عناصر القوالب | Template nodes | Reorder a non-root template node. | `AbwabTemplateNodesController.Reorder` |
| `abwab.template_nodes.delete` | حذف عناصر القوالب | Template nodes | Retire a non-root node and its subtree. | `AbwabTemplateNodesController.Delete` |

This catalogue adopts the names and Arabic labels from the [current-state report §6](authorization-permissions-current-state-report.md#6-proposed-minimal-permission-catalogue). `edit` remains the stable business verb for the currently rename-only section action; door `DELETE` uses `archive` because the implementation is reversible soft archive; and single/bulk transport variants do not create new codes.

There is no door-protection permission. No protection action exists in the current controllers or route catalogue (`Backend/tests/QuranDashboard.Tests/Smoke/SmokeRouteCatalog.cs:224-359`, `Frontend/quran-dashboard-ui/src/app/features/abwab/components/abwab-side-panel/abwab-side-panel.component.spec.ts:53-73`).

## 7. Permission-group/select-all behavior

V1 groups are code-defined presentation metadata:

| Group | Arabic group label | Select-all behavior |
|---|---|---|
| Doors | الأبواب | Grants the six current `abwab.doors.*` codes individually. |
| Sections | الأقسام | Grants the four current `abwab.sections.*` codes individually. |
| Relations | العلاقات | Grants the two current `abwab.relations.*` codes individually. |
| Templates | القوالب | Grants the three current `abwab.templates.*` codes individually. |
| Template nodes | عناصر القوالب | Grants the four current `abwab.template_nodes.*` codes individually. |

“Manage all” is a UI editing convenience:

- selecting it selects every current code in the group;
- clearing it clears the current group selection;
- the Owner may uncheck any individual permission before saving;
- saving creates or removes individual `UserPermissions` rows;
- it is not a role, permission code, grant row, policy, or Backend authorization shortcut;
- adding a future permission to a group does not silently grant it to users who previously used select-all.

The canonical Backend catalogue owns code, label, group, and display order. The frontend consumes or contract-checks that metadata. There is no `PermissionGroups`, `UserPermissionGroups`, or group-grant table in v1.

## 8. Complete current Abwab endpoint-to-permission matrix

The 25 routes below match the live controller actions and the bidirectional `SmokeRouteCatalog` inventory (`Backend/tests/QuranDashboard.Tests/Smoke/SmokeRouteCatalog.cs:224-359`, `Backend/tests/QuranDashboard.Tests/Smoke/SmokeCoverageParityTests.cs:10-35`). All are currently open. Target read rules in this table correct the earlier “active read” cells in the [current-state report §5](authorization-permissions-current-state-report.md#5-complete-abwab-endpoint-permission-matrix).

| Method | Route | Controller/action | Kind | Current | Target rule | Permission | Notes |
|---|---|---|---|---|---|---|---|
| GET | `/api/abwab/tree` | `AbwabTreeController.Get` (`Backend/api/QuranDashboard.Api/Controllers/Abwab/AbwabTreeController.cs:7-31`) | Read | Open | Public anonymous | — | Doors/sections snapshot; conditional GET |
| GET | `/api/abwab/doors/{doorId}/relations` | `AbwabDoorRelationsController.GetForDoor` (`Backend/api/QuranDashboard.Api/Controllers/Abwab/AbwabDoorRelationsController.cs:8-29`) | Read | Open | Public anonymous | — | Relations read |
| GET | `/api/abwab/templates` | `AbwabTemplatesController.GetAll` (`Backend/api/QuranDashboard.Api/Controllers/Abwab/AbwabTemplatesController.cs:11-41`) | Read | Open | Public anonymous | — | Template list; conditional GET |
| GET | `/api/abwab/templates/{templateId}` | `AbwabTemplatesController.Get` (`Backend/api/QuranDashboard.Api/Controllers/Abwab/AbwabTemplatesController.cs:43-60`) | Read | Open | Public anonymous | — | Template detail; conditional GET |
| POST | `/api/abwab/sections` | `AbwabSectionsController.Create` (`Backend/api/QuranDashboard.Api/Controllers/Abwab/AbwabSectionsController.cs:9-35`) | Write | Open | Active Owner or exact grant | `abwab.sections.create` | One section |
| PUT | `/api/abwab/sections/{id}` | `AbwabSectionsController.Rename` (`Backend/api/QuranDashboard.Api/Controllers/Abwab/AbwabSectionsController.cs:37-60`) | Write | Open | Active Owner or exact grant | `abwab.sections.edit` | Currently rename-only |
| DELETE | `/api/abwab/sections/{id}` | `AbwabSectionsController.Delete` (`Backend/api/QuranDashboard.Api/Controllers/Abwab/AbwabSectionsController.cs:62-79`) | Write | Open | Active Owner or exact grant | `abwab.sections.delete` | Refuses a section with live doors |
| POST | `/api/abwab/sections/{id}/order` | `AbwabSectionsController.Reorder` (`Backend/api/QuranDashboard.Api/Controllers/Abwab/AbwabSectionsController.cs:81-100`) | Write | Open | Active Owner or exact grant | `abwab.sections.reorder` | Resequences live sections |
| POST | `/api/abwab/doors` | `AbwabDoorsController.Create` (`Backend/api/QuranDashboard.Api/Controllers/Abwab/AbwabDoorsController.cs:13-51`) | Write | Open | Active Owner or exact grant | `abwab.doors.create` | Root or child |
| PUT | `/api/abwab/doors/{id}` | `AbwabDoorsController.Edit` (`Backend/api/QuranDashboard.Api/Controllers/Abwab/AbwabDoorsController.cs:53-75`) | Write | Open | Active Owner or exact grant | `abwab.doors.edit` | Authored fields and aliases |
| POST | `/api/abwab/doors/{id}/move` | `AbwabDoorsController.Move` (`Backend/api/QuranDashboard.Api/Controllers/Abwab/AbwabDoorsController.cs:77-104`) | Write | Open | Active Owner or exact grant | `abwab.doors.move` | May reparent/change section |
| POST | `/api/abwab/doors/{id}/order` | `AbwabDoorsController.Reorder` (`Backend/api/QuranDashboard.Api/Controllers/Abwab/AbwabDoorsController.cs:106-129`) | Write | Open | Active Owner or exact grant | `abwab.doors.reorder` | Section or Global scope |
| POST | `/api/abwab/doors/bulk-move` | `AbwabDoorsController.BulkMove` (`Backend/api/QuranDashboard.Api/Controllers/Abwab/AbwabDoorsController.cs:131-159`) | Write | Open | Active Owner or exact grant | `abwab.doors.move` | Same capability as single move |
| POST | `/api/abwab/doors/bulk-archive` | `AbwabDoorsController.BulkArchive` (`Backend/api/QuranDashboard.Api/Controllers/Abwab/AbwabDoorsController.cs:161-179`) | Write | Open | Active Owner or exact grant | `abwab.doors.archive` | Archives selected subtrees |
| DELETE | `/api/abwab/doors/{id}` | `AbwabDoorsController.Delete` (`Backend/api/QuranDashboard.Api/Controllers/Abwab/AbwabDoorsController.cs:181-197`) | Write | Open | Active Owner or exact grant | `abwab.doors.archive` | Soft archive, not hard delete |
| POST | `/api/abwab/doors/{id}/restore` | `AbwabDoorsController.Restore` (`Backend/api/QuranDashboard.Api/Controllers/Abwab/AbwabDoorsController.cs:199-225`) | Write | Open | Active Owner or exact grant | `abwab.doors.restore` | Restores the swept subtree |
| POST | `/api/abwab/doors/{doorId}/relations` | `AbwabDoorRelationsController.AddForDoor` (`Backend/api/QuranDashboard.Api/Controllers/Abwab/AbwabDoorRelationsController.cs:31-60`) | Write | Open | Active Owner or exact grant | `abwab.relations.create` | Multi-target add is one action |
| DELETE | `/api/abwab/relations/{relationId}` | `AbwabDoorRelationsController.Delete` (`Backend/api/QuranDashboard.Api/Controllers/Abwab/AbwabDoorRelationsController.cs:62-77`) | Write | Open | Active Owner or exact grant | `abwab.relations.delete` | One relation |
| POST | `/api/abwab/templates` | `AbwabTemplatesController.Create` (`Backend/api/QuranDashboard.Api/Controllers/Abwab/AbwabTemplatesController.cs:62-79`) | Write | Open | Active Owner or exact grant | `abwab.templates.create` | Creates mandatory root node |
| DELETE | `/api/abwab/templates/{templateId}` | `AbwabTemplatesController.Delete` (`Backend/api/QuranDashboard.Api/Controllers/Abwab/AbwabTemplatesController.cs:81-96`) | Write | Open | Active Owner or exact grant | `abwab.templates.delete` | Retires template |
| POST | `/api/abwab/templates/{templateId}/apply` | `AbwabTemplatesController.Apply` (`Backend/api/QuranDashboard.Api/Controllers/Abwab/AbwabTemplatesController.cs:98-125`) | Write | Open | Active Owner or exact grant | `abwab.templates.apply` | Deep-copies child door subtrees |
| POST | `/api/abwab/templates/{templateId}/nodes` | `AbwabTemplateNodesController.Add` (`Backend/api/QuranDashboard.Api/Controllers/Abwab/AbwabTemplateNodesController.cs:9-43`) | Write | Open | Active Owner or exact grant | `abwab.template_nodes.create` | Adds child node |
| PUT | `/api/abwab/template-nodes/{nodeId}` | `AbwabTemplateNodesController.Edit` (`Backend/api/QuranDashboard.Api/Controllers/Abwab/AbwabTemplateNodesController.cs:45-65`) | Write | Open | Active Owner or exact grant | `abwab.template_nodes.edit` | Root edit renames template |
| POST | `/api/abwab/template-nodes/{nodeId}/order` | `AbwabTemplateNodesController.Reorder` (`Backend/api/QuranDashboard.Api/Controllers/Abwab/AbwabTemplateNodesController.cs:67-86`) | Write | Open | Active Owner or exact grant | `abwab.template_nodes.reorder` | Root is refused |
| DELETE | `/api/abwab/template-nodes/{nodeId}` | `AbwabTemplateNodesController.Delete` (`Backend/api/QuranDashboard.Api/Controllers/Abwab/AbwabTemplateNodesController.cs:88-105`) | Write | Open | Active Owner or exact grant | `abwab.template_nodes.delete` | Deletes node subtree; root refused |

Every Abwab write has exactly one permission. “Active Owner or exact grant” always includes authentication and `UserStatus.Active`.

## 9. Composite-action decisions

Authorization follows the user-visible action. Required internal invariant work does not create hidden permission dependencies.

| User-visible operation | Permission decision | Contract |
|---|---|---|
| Move one door | `abwab.doors.move` | Reparenting, descendant section changes, and sibling/global resequencing are inherent effects. |
| Bulk move | `abwab.doors.move` | Cardinality does not create a stronger privilege; do not also require create or reorder. |
| Reorder one door | `abwab.doors.reorder` | Peer resequencing is the declared action. |
| Archive one subtree | `abwab.doors.archive` | The `DELETE` route is a soft archive and may resequence the old scope. |
| Bulk archive | `abwab.doors.archive` | The selected subtrees share the single archive capability. |
| Restore one archived subtree | `abwab.doors.restore` | Descendant restoration, destination repair, and resequencing are part of restore; do not require move/reorder. |
| Delete section | `abwab.sections.delete` | It refuses live doors and does not grant a hidden door-delete capability. |
| Reorder section | `abwab.sections.reorder` | Resequencing the full live list is inherent. |
| Add relations to several targets | `abwab.relations.create` | Target count does not add authority; the operation remains transactional. |
| Create template | `abwab.templates.create` | Mandatory root-node creation does not require template-node create. |
| Apply template | `abwab.templates.apply` | Controlled door-subtree creation is the advertised operation; do not require door create or template-node permissions. |
| Delete template node subtree | `abwab.template_nodes.delete` | Subtree retirement and sibling resequencing are inherent. |

These decisions adopt the implementation evidence and rationale in the [current-state report §7](authorization-permissions-current-state-report.md#7-composite-action-decisions), including the door writers (`Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Writes/Abwab/EfAbwabDoorsWriter.cs:88-435`) and template-apply writer (`Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Writes/Abwab/EfAbwabTemplateApplyWriter.cs:11-145`).

No current composite action requires multiple permissions or a stronger grouped permission.

## 10. User activation, disable, and reactivation semantics

Only an active Owner may change a non-owner account’s administrative status.

| Transition | Authority | Permission effect | Required audit |
|---|---|---|---|
| New ordinary `/api/access/me` provisioning → `Pending` | Authenticated Logto subject, server profile | No grants | Provisioning evidence may be retained separately; no write authority is created. |
| `Pending` → `Active` through acceptance | Active Owner | Starts with no grants unless the Owner explicitly grants in the same controlled transaction | `UserAccepted` and `UserActivated` events with actor, target, before/after, and time |
| `Active` → `Disabled` | Active Owner; target must be non-owner | Remove every current `UserPermissions` row atomically; no grant survives as active state | `PermissionRevoked` per removed grant with disable reason, plus `UserDisabled` |
| `Disabled` → `Active` | Active Owner; target must be non-owner | Begins with zero grants; previous grants are not restored | `UserReactivated` with before/after; later grants create their own events |

Acceptance and initial activation are one controlled state transition but preserve both audit facts. Permission grant is a distinct authority decision even when performed in the same user-management workflow.

Disabling and removal of current grants must commit in one database transaction. If any revocation or audit insert fails, the entire disable operation fails closed. Public reading remains available before, during, and after every status transition.

Reactivation never resurrects old grants. Historical permissions remain visible only through append-only audit. An Owner must explicitly grant each desired current permission again.

Normal user-management operations cannot target an Owner. To remove Owner authority, an operator changes `OwnerBootstrap:Emails` and runs the guarded reconciliation contract in section 4; ordinary disable/reactivate actions never transfer or mutate Owner membership.

## 11. Owner-only user and permission administration

Only an active Owner may:

- list or inspect application users and their account status;
- accept a `Pending` non-owner;
- activate, disable, or reactivate a non-owner;
- view the permission catalogue and a user’s direct grants;
- grant or revoke direct permissions;
- view access-audit records;
- initiate and confirm a Logto `sub` relink;
- inspect Owner reconciliation results.

These future security-administration endpoints use explicit Owner-only authorization. They do not use the 19 Abwab codes, and v1 does not define a delegatable `permissions.manage` permission.

The user/permission administration model has no role selector. It cannot create roles, assign Owner, remove Owner, disable Owner, transfer Owner, or store “manage all” as authority. Owner membership remains exclusively configuration-reconciled.

## 12. Logto `sub` relinking and Owner recovery

### 12.1 Fail-closed collision

If an authenticated Logto subject presents a verified email already owned by another local user, `/api/access/me` must continue to fail closed rather than attaching the new `sub`. The current unique-email conflict path already returns this condition without relinking (`Backend/infrastructure/QuranDashboard.Infrastructure/Access/UserProvisioningService.cs:99-115`, `Backend/tests/QuranDashboard.Tests/Api/Access/AccessMeEndpointTests.cs:116-145`).

No permissions are evaluated through the old account for the new subject while the conflict is unresolved.

### 12.2 Manual relink contract

A future relink operation is Owner-only security administration and requires:

1. an active Owner actor;
2. explicit selection of the target local user;
3. explicit entry/selection of the proposed new `sub`;
4. server-side retrieval of that Logto subject;
5. verified primary email matching the target’s normalized email;
6. confirmation that the new `sub` is not linked to another local user;
7. an explicit confirmation step that displays the target and old/new identity binding;
8. one transaction that updates `Users.LogtoSub` and appends the audit event.

The target user’s grants remain tied to its local `UserId`; they become available to the new identity only after the relink transaction commits successfully. Failure leaves the old `sub`, grants, status, and Owner state unchanged.

Relinking never changes `RoleId`, status, or permissions. If the target is an Owner, the verified email must still be present in reconciled Owner configuration; otherwise relink is refused and configuration reconciliation is required. This preserves Owner membership through a relink but does not grant or transfer it.

The required audit event contains actor, target, old `sub`, new `sub`, verified email evidence metadata, timestamp, reason, and before/after state.

### 12.3 Owner recovery

Owner recovery does not depend on relinking an inaccessible Owner. An operator adds another verified Owner email to environment configuration, provisions/verifies that identity, and runs Owner reconciliation. Once the new local user is an `Active` Owner, it may perform Owner-only account recovery actions.

Removing an unavailable Owner from configuration is a separate reconciled and audited change and cannot reduce the active configured Owner count below one.

## 13. Access audit contract

`AccessAuditEvents` is append-only from its first migration. Ordinary application behavior may insert and read authorized audit events; it has no update or delete operation.

### 13.1 Required action types

At minimum:

- `UserAccepted`;
- `UserActivated`;
- `UserDisabled`;
- `UserReactivated`;
- `PermissionGranted`;
- `PermissionRevoked`;
- `LogtoSubjectRelinked`;
- `OwnerGrantedByReconciliation`;
- `OwnerRemovedByReconciliation`.

### 13.2 Required event content

Every event preserves:

- immutable event identifier;
- UTC occurrence timestamp;
- actor type (`User` or `System`);
- actor local user ID when a user initiated it;
- actor identity snapshot sufficient to understand the historical actor;
- target local user ID and target identity snapshot;
- stable action type;
- permission code when applicable;
- relevant before-state snapshot;
- relevant after-state snapshot;
- reason and structured metadata when applicable.

Relink events include old/new `sub`. Reconciliation events include normalized configured email and deployment/configuration fingerprint or equivalent provenance. Disable-triggered revocations identify disable as their reason.

### 13.3 Immutability and retrieval

- There is no ordinary hard delete for administrative users; audit foreign-key behavior must not erase history.
- Actor/target snapshots remain understandable if a user’s current email, display name, status, or `sub` later changes.
- Audit writes occur in the same transaction as the state change they describe.
- A failed audit insert fails the state change; security state is never changed without its required history.
- Owner-only paginated/filterable retrieval is part of the Backend contract.
- V1 has no ordinary retention purge; events are retained indefinitely unless a later separately accepted legal/operational retention decision introduces controlled archival outside normal application behavior.
- A complete audit-history UI is not required in the first implementation, but the data and retrieval contract cannot be deferred or discarded.

## 14. V1 database model and invariants

V1 uses:

```text
Users
Roles
Permissions
UserPermissions
AccessAuditEvents
```

It does not add a generic RBAC role-permission model.

| Model | V1 direction | Required invariants |
|---|---|---|
| `Users` | Retain current local account, `LogtoSub`, email/profile, nullable `RoleId`, and `Pending`/`Active`/`Disabled`. | `LogtoSub` and email remain unique; non-owner `RoleId` is null; public reads do not depend on a row; no ordinary hard delete. |
| `Roles` | Retain one seeded `Owner` row. Remove Admin and Editor after transition. | No role-management endpoint; multiple users may reference Owner; no other live role rows. |
| `Permissions` | Seed the 19 immutable codes and their catalogue metadata. | `Code` is required, normalized lowercase, unique, and never repurposed; numeric `Id` is internal. |
| `UserPermissions` | Store current active direct grants only: `UserId`, `PermissionId`, `GrantedByUserId`, `GrantedAtUtc`. | Unique `(UserId, PermissionId)`; target is active non-owner; grantor is active Owner; Owners have no rows; revoke/disable removes current row transactionally after audit data is prepared. |
| `AccessAuditEvents` | Store append-only security history. | No ordinary update/delete; required event fields from section 13; transactionally coupled to state changes. |

The current schema already has nullable `users.role_id`, unique `logto_sub`, unique email, and restrictive role deletion (`Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Configurations/Access/UserConfiguration.cs:9-54`). It currently seeds Owner, Admin, and Editor and has no permission/grant/audit sets (`Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Configurations/Access/RoleConfiguration.cs:9-31`, `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/QuranDashboardDbContext.cs:53-54`).

Cross-row invariants that cannot be represented safely by a simple relational constraint must be enforced by one transactional application service and covered by persistence/integration tests. Authorization still ignores grants for a non-active user even if corrupt or transitional data is encountered.

Permission-group metadata remains in the canonical catalogue, not in authorization tables. Labels/descriptions may be seeded into `Permissions` for API display, but group selection never creates authority beyond individual codes.

## 15. Backend authorization contracts

### 15.1 Canonical components

The Backend authorization design has these cohesive components:

1. **Canonical permission catalogue:** constants plus code/Arabic label/description/group metadata for the 19 known codes.
2. **Request-scoped authorization-state resolver:** loads the authenticated `sub`’s local `UserId`, status, Owner state, and direct permission-code set once per request.
3. **Reusable permission requirement/handler:** receives a known code, requires an authenticated active local user, centrally succeeds for active Owner, otherwise requires the exact direct code.
4. **Explicit endpoint metadata:** every unsafe endpoint declares one known granular permission or explicit Owner-only security-administration access.
5. **Owner-only security policy:** requires the same resolved local state to be authenticated, `Active`, and Owner.
6. **Shared authorization result handling:** emits controlled `ApiResponse` envelopes for challenge and forbid.
7. **Unsafe-endpoint validation/parity:** blocks missing or unknown authorization metadata.

Controllers do not query access tables, compare role strings, or embed permission string literals. Application handlers continue to own domain behavior; endpoint authorization rejects the request before a write handler runs.

### 15.2 Public and non-public endpoint posture

- No fallback authorization policy is applied to normal content.
- Public content GETs and `GET /api/health` remain anonymous.
- `GET /api/access/me` remains explicit authenticated-only provisioning/status access.
- Future security-administration controllers/actions carry explicit Owner-only metadata, including their GETs.
- Every `POST`, `PUT`, `PATCH`, and `DELETE` carries explicit known protection metadata.
- Every current Abwab write carries exactly the permission in section 8.

The current pipeline correctly orders authentication before authorization (`Backend/api/QuranDashboard.Api/Extensions/WebApplicationExtensions.cs:19-29`). The target must preserve that order without turning public GETs into authenticated routes.

### 15.3 Owner bypass and active status

The permission handler resolves application-database state and evaluates in this order:

1. authenticated Logto principal with a valid `sub`;
2. existing local user;
3. `UserStatus.Active`;
4. Owner bypass, if the sole local role is Owner;
5. exact direct permission code.

The Owner-only policy stops after step 4. A disabled Owner fails step 3. Token role/permission claims are not consulted.

### 15.4 Loading, cache, and failure

V1 uses request-scoped memoization only and no cross-request authorization cache. Grant, revoke, disable, reactivate, relink, and reconciliation changes therefore apply on the next request without an invalidation protocol.

The current role resolver’s 30-second process-local cache can retain negative state until explicit eviction, so it is not an acceptable permission-authority cache (`Backend/infrastructure/QuranDashboard.Infrastructure/Access/CachedUserRoleResolver.cs:8-52`, `Backend/tests/QuranDashboard.Tests/Api/Access/CachedUserRoleResolverTests.cs:44-73`).

If authorization state cannot be resolved because of database/infrastructure failure, authorization fails closed. It may return a controlled `500`/`503` operational response rather than mislabeling an outage as a permission denial, but it never allows the action.

If a cross-request cache is proposed later, its versioning, multi-instance invalidation, revocation latency, and tests require a separate accepted design change.

### 15.5 Current-user contract

The target `/api/access/me` response supplies:

```text
sub
email
displayName
status
isOwner
permissions
```

`permissions` is the active direct code set and is empty for Owner, Pending, Disabled, and active read-only users. `isOwner` is explicit. The existing nullable `roleName` may remain only as a transitional field (`"Owner"` or null); internal numeric `roleId` is not part of the target frontend authorization contract (`Backend/api/QuranDashboard.Api/Controllers/Access/AccessController.cs:37-43`, `Frontend/quran-dashboard-ui/src/app/core/auth/current-user.model.ts:1-12`).

## 16. Frontend behavior contracts

### 16.1 Public route behavior

Normal content routes remain unguarded and readable without a Logto session. No active-user route guard is added to Dashboard, Quran, Mushaf, Words, Abwab, relations, templates, or other normal read pages.

An anonymous deep link or refreshed public URL loads the same content directly. The frontend does not require `/api/access/me` before rendering public content.

Security-administration routes are separate and active-Owner guarded. `/api/access/me` is loaded when a session exists and access-aware UI is needed, not as a prerequisite for public navigation.

### 16.2 Access state

The frontend current-user state exposes:

```text
isAuthenticated
status
isOwner
permissionSet
can(permissionCode)
```

The reusable check is:

```text
can(code) = status == Active && (isOwner || permissionSet contains code)
```

One typed frontend catalogue is generated from or contract-checked against the Backend catalogue. Feature components do not scatter raw code strings.

The current store only loads `/api/access/me`, and the current `roleGuard` is limited to Owner/Admin/Editor but attached to no route (`Frontend/quran-dashboard-ui/src/app/core/auth/current-user.store.ts:8-79`, `Frontend/quran-dashboard-ui/src/app/core/auth/role.guard.ts:9-27`, `Frontend/quran-dashboard-ui/src/app/app.routes.spec.ts:57-66`). Named-role guarding is replaced for write UX; public routes remain unguarded.

### 16.3 Write affordances

- Anonymous users see public content but no enabled administrative write actions. A login affordance may appear where appropriate.
- Pending, Disabled, and active read-only users see public content but no enabled write actions.
- Active Owners see every administrative action.
- Active non-owner users see or enable only actions authorized by the exact code.
- Stable controls may be disabled with an accessible Arabic explanation; write-only contextual actions may be hidden.
- A user with one permission does not receive a neighboring control merely because it shares a screen or group.

Every event path is gated, including:

- toolbar and context-menu commands;
- keyboard move/reorder/delete paths;
- bulk selection and bulk action dispatch;
- modal opening and submission;
- URL-restored write modals;
- quick-add controls;
- inline editing and inline reorder;
- relation add versus relation delete;
- archive versus restore;
- template actions versus template-node actions.

The current Abwab pages expose these paths across the main page, templates workshop, relations modal, side panel, and archive view (`Frontend/quran-dashboard-ui/src/app/features/abwab/pages/abwab-page/abwab-page.component.html:27-290`, `Frontend/quran-dashboard-ui/src/app/features/abwab/pages/abwab-templates-page/abwab-templates-page.component.html:42-251`, `Frontend/quran-dashboard-ui/src/app/features/abwab/components/abwab-relations-modal/abwab-relations-modal.component.html:54-190`, `Frontend/quran-dashboard-ui/src/app/features/abwab/components/abwab-side-panel/abwab-side-panel.component.html:21-118`, `Frontend/quran-dashboard-ui/src/app/features/abwab/components/abwab-archive-view/abwab-archive-view.component.html:30-45`).

If a deep link restores a write surface without current authority, the public base page remains available and the write surface does not open or submit. Frontend checks remain UX only; the Backend independently rejects a handcrafted request.

### 16.4 Stale access state

If status or permission state changes while a write UI is open, a Backend `403` is authoritative. The frontend:

1. displays the controlled Arabic message;
2. refreshes `/api/access/me`;
3. closes or disables the stale write surface;
4. never automatically retries the mutation.

## 17. `401`/`403` response contracts

The Backend owns both outcomes through one shared authorization-result path.

| Scenario | Status | Contract |
|---|---:|---|
| Anonymous or invalid-token request to an administrative write or non-public security read | `401` | Shared JSON `ApiResponse<object>.Fail(...)` envelope using the existing Arabic unauthorized message: `يجب تسجيل الدخول للوصول إلى هذا المورد`. |
| Authenticated but unprovisioned caller on a protected route | `403` | Shared Arabic forbidden envelope; provisioning remains `/api/access/me`’s responsibility. |
| Authenticated `Pending` or `Disabled` caller on a protected route | `403` | Controlled inactive-account Arabic message. |
| Active non-owner missing the exact permission | `403` | Controlled Arabic no-permission message. |
| Active non-owner requesting Owner-only security administration | `403` | Controlled Arabic Owner-only denial. |
| Public content GET by any caller | Not an auth rejection | Authorization does not challenge or forbid the read. Normal domain/transport failures remain possible. |

The existing challenge writer already emits the shared `401` envelope (`Backend/api/QuranDashboard.Api/Authentication/UnauthorizedRejectionWriter.cs:3-18`, `Backend/api/QuranDashboard.Api/Common/ApiMessages.cs:14`). The target adds centralized Arabic `403` messages rather than accepting a bare framework response. Exact messages are centralized beside `ApiMessages` and contract-tested; controllers do not hardcode them.

On `401`, the frontend invokes Logto authentication or session renewal and preserves the intended location where appropriate. On `403`, it shows the Backend message, refreshes access state, and does not retry the write.

Authentication/authorization denial does not replace domain responses. Once authorized, current `400`, `404`, `409`, concurrency, and success envelopes remain authoritative.

## 18. Endpoint fail-closed/parity requirements

Forgotten write protection is both a startup/runtime failure and a test failure.

### 18.1 Unsafe endpoint metadata

Every controller endpoint using `POST`, `PUT`, `PATCH`, or `DELETE` must carry exactly one recognized protection classification:

- a known granular permission code; or
- explicit active-Owner-only metadata for security administration.

Every current Abwab unsafe endpoint must use the granular code in section 8. No unsafe endpoint may rely on authentication alone, a fallback policy, frontend hiding, an unknown permission string, or “manage all.”

A startup endpoint validator or equivalent runtime convention enumerates routed unsafe endpoints after endpoint construction and refuses to expose the application when metadata is absent, ambiguous, or references an unknown code. Authorization itself also fails closed if resolved endpoint metadata is missing or invalid.

### 18.2 Route/metadata parity

The existing parity test already compares live method/path pairs bidirectionally with `SmokeRouteCatalog`, but the catalogue currently distinguishes only `Open` and `RequiresAuthentication` (`Backend/tests/QuranDashboard.Tests/Smoke/SmokeCoverageParityTests.cs:10-68`, `Backend/tests/QuranDashboard.Tests/Smoke/SmokeRouteCatalog.cs:5-9`).

The target catalogue records an access classification sufficient to assert:

- each normal content GET is public;
- `/api/access/me` is authenticated-only;
- every security-administration route is active-Owner-only;
- every unsafe content route has its exact known permission code;
- no unsafe route is anonymous;
- every catalogue permission exists in the canonical 19-code set;
- no route has more than one granular permission.

Adding or changing an unsafe route without updating the catalogue and authorization metadata fails the route-smoke gate. Adding a public content GET must not accidentally attach authentication. Adding a security-administration GET must place it under explicit Owner-only controller/action metadata.

No global fallback policy is used to achieve fail-closed writes because that would incorrectly authenticate public content GETs.

## 19. Migration and rollout invariants

These are release invariants, not phase/task decomposition:

1. Inventory all existing Owner/Admin/Editor users before role conversion.
2. Preserve every intended Owner through the normalized environment list and verified reconciliation.
3. Do not infer any direct permission from current Admin or Editor role names. Former Admin/Editor users become role-less with zero write grants until an Owner explicitly grants permissions.
4. Add and seed the 19 immutable permission codes, current-grant storage, and append-only audit storage before enabling write enforcement.
5. Convert every non-owner `RoleId` to null. Retain the single Owner role row for all reconciled Owners.
6. Verify at least one configured, verified, local `Active` Owner and successful reconciliation before production enforcement starts.
7. Keep `/api/access/me` backward-compatible only long enough for Backend/frontend rollout; `isOwner` and `permissions` are additive before legacy role fields are removed.
8. Deploy Backend enforcement before, or atomically with, permission-aware frontend controls. A temporarily denied control is safer than a temporarily writable unauthenticated API.
9. Preserve public content GET anonymity throughout rollout. Do not introduce the active-read fallback or public-route guard proposed in the earlier report.
10. Remove Admin/Editor seeds, constants, policies, frontend union members, role guard assumptions, and transitional contract fields only after database conversion and the new authorization path are verified.
11. Apply Owner configuration removals only through reconciliation, with last-active-Owner protection and append-only audit.
12. Do not authorize from legacy transformed role claims or token claims during transition; the application database remains authoritative at every stage.

The current roles are fixed Owner/Admin/Editor seeds and the named policies are registered but unused (`Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Configurations/Access/RoleConfiguration.cs:26-31`, `Backend/api/QuranDashboard.Api/Authentication/AuthenticationRegistration.cs:50-58`). That makes explicit data conversion mandatory and role-name-to-permission inference unjustified.

## 20. Mandatory testing-debt acceptance obligations

The testing-debt ledger explicitly makes five Abwab smoke rows acceptance criteria for the authorization feature (`docs/TESTING_DEBT.md:20-25`). All five are mandatory:

| Debt row | Acceptance obligation |
|---|---|
| `abwab-relations` row 3 | Dispatch all three relation routes across documented `200`/`201`/`204`/`400`/`404`/`409` behavior, including archived-anchor `200 []`, plus authorization personas (`docs/TESTING_DEBT.md:33-37`). |
| `abwab-templates` row 8 | Dispatch all nine template/template-node routes across documented success/failure envelopes, plus authorization personas (`docs/TESTING_DEBT.md:58-63`). |
| `ux-slice-f` F3 | Add section-reorder smoke for `200`/`400`/`404`/`409`, plus authorization personas (`docs/TESTING_DEBT.md:83-87`). |
| `ux-slice-g` G3 | Cover template apply’s narrowed `400` and reshaped `409`; this narrows row 8 rather than replacing it (`docs/TESTING_DEBT.md:98-102`). |
| `ux-slice-i` I2 | Pay all remaining conditional-GET cases for tree/template reads, including matching/mismatching/malformed validators, required headers, bodiless `304`, and zero-query list-read `304` paths (`docs/TESTING_DEBT.md:138-142`). |

I2 must be executed through anonymous public requests as well as any authenticated regression persona. The earlier report’s “unauthorized callers do not receive validators” expectation is superseded because these content reads are public.

### 20.1 Mandatory authorization acceptance matrix

| Scenario | Required result/evidence |
|---|---|
| Anonymous public content read | Every normal content GET, including all four Abwab reads and health, remains reachable without authentication. |
| Authenticated Pending/Disabled public read | Content still succeeds; account status does not gate public data. |
| Anonymous administrative write | Every current Abwab write returns shared-envelope `401`; no handler/database mutation occurs. |
| Authenticated unknown local user write | `403`; no implicit provisioning or mutation. `/api/access/me` remains the provisioning exception. |
| Pending/Disabled write | `403`; disabled Owner also fails. |
| Active read-only non-owner | All public reads succeed; all 21 Abwab writes return `403`. |
| Exact permission | Each mapped write succeeds subject to existing domain behavior. |
| Missing/neighboring permission | `403`; create does not edit, edit does not move, archive does not restore, relation create does not delete, template create does not apply, and node permissions remain isolated. |
| Active Owner with no grant rows | All 21 Abwab writes and every Owner-only security operation are authorized; existing validation/concurrency/domain failures still apply. |
| Token claim smuggling | Role- or permission-looking JWT claims never grant Owner or a direct permission. |
| Single/bulk equivalence | Door move grants single and bulk move only; door archive grants single and bulk archive only. |
| Composite action | Each section 9 action requires only its one visible permission and preserves current all-or-nothing/domain behavior. |
| Disable | Status change, current-grant removal, per-grant audit, and disable audit are atomic; the next write is denied. |
| Reactivate | User is Active with zero grants; old permissions do not return. |
| Grant/revoke | Change takes effect on the next request under request-scoped resolution. If any cross-request cache is introduced, multi-instance invalidation becomes mandatory. |
| Multiple Owners | Several configured verified Owners share unrestricted active authority; removing one does not affect another; last-active-Owner removal fails. |
| Owner reconciliation | Invalid/duplicate/unverified config fails; effective additions/removals are audited; a Disabled Owner remains denied. |
| Relink | Email-only collision fails; only active Owner can explicitly confirm; old/new `sub` is audited; permissions become usable only after successful commit. |
| Audit immutability | Every required event is written with actor/target/before/after; ordinary update/delete paths do not exist. |
| `401`/`403` envelope | Both statuses use the shared response shape and centralized Arabic messages; no bare framework body. |
| Route completeness | Every live route matches the smoke catalogue; every unsafe endpoint has one known access metadata value; normal public GETs remain anonymous. |
| Frontend persona/control coverage | Anonymous, Pending, Disabled, read-only, single-permission, and Owner personas cover buttons, context menus, keyboards, bulk actions, modals/forms, deep links, quick-add, and inline edit/reorder. |
| Stale frontend permission | Backend `403` refreshes access, closes/disables stale write UI, and is never automatically retried. |
| Handcrafted HTTP request | Direct calls that bypass frontend visibility receive the same Backend `401`/`403`; Backend enforcement is proven independently of browser UI. |

Authorization changes require focused access/middleware tests and the Backend Smoke route gate; the evidence must say whether the smoke data tier ran or skipped (`TESTING_STRATEGY.md:176-207`, `TESTING_STRATEGY.md:279-294`, `TESTING_STRATEGY.md:534-540`). Frontend core/routing changes require focused tests followed by the full frontend suite and production build at the applicable tier; Playwright remains supplementary and cannot replace Backend route smoke (`TESTING_STRATEGY.md:140-170`, `TESTING_STRATEGY.md:266-271`, `TESTING_STRATEGY.md:371-409`).

`ux-slice-h` H1 remains conditional and becomes mandatory only if the implementation changes the navbar/nav model to add auth-gated entries (`docs/TESTING_DEBT.md:113-118`). Other debt rows retain their own triggers; authorization metadata alone does not make unrelated writer/UI debt due.

## 21. Explicitly out-of-scope items

- implementation code, tests, migrations, database changes, and deployment execution;
- a Spec Kit or final implementation plan;
- public-content read permissions or authenticated public-content routes;
- named non-owner roles;
- generic RBAC or role-permission tables;
- delegatable permission administration in v1;
- runtime-editable permission groups;
- persisted/selectable “manage all” authority;
- a door-protection permission before a real action exists;
- permissions for any other unimplemented feature;
- granting or removing Owner through dashboard user management;
- automatic email-based `sub` relinking;
- automatic restoration of permissions after reactivation;
- a complete audit-history UI in the first implementation;
- cross-request authorization caching in v1;
- changing current domain validation, concurrency, or response semantics beyond controlled authorization responses.

## 22. Traceability to the current-state report and live code/routes

| Decision area | Current-state baseline | Live repository evidence | Resolution |
|---|---|---|---|
| Public content reads | [Report §§4–5](authorization-permissions-current-state-report.md#4-current-endpoint-protection-inventory) found all non-`/me` routes open but recommended future active reads. | `Backend/tests/QuranDashboard.Tests/Smoke/SmokeRouteCatalog.cs:117-359`; `Backend/api/QuranDashboard.Api/Controllers/Access/AccessController.cs:7-15`; `Backend/tests/QuranDashboard.Tests/Api/Access/AuthorizationPolicyRegistrationTests.cs:33-39` | Inspection fact retained; active-read recommendation superseded. Public content GETs remain anonymous. |
| Logto/local boundary | [Report §2](authorization-permissions-current-state-report.md#2-current-authentication-architecture) | `Backend/api/QuranDashboard.Api/Authentication/AuthenticationRegistration.cs:10-48`; `Backend/api/QuranDashboard.Api/Authentication/HttpContextCurrentUser.cs:5-20`; `Backend/infrastructure/QuranDashboard.Infrastructure/Access/UserProvisioningService.cs:13-32` | Adopted, with token role/permission claims explicitly non-authoritative. |
| Multiple Owners | [Report §§2.3, 8.2, 13](authorization-permissions-current-state-report.md#23-provisioning-and-identity-profile-behavior) identified singular bootstrap and left cardinality open. | `Backend/infrastructure/QuranDashboard.Infrastructure/Access/OwnerBootstrapOptions.cs:6-26`; `Backend/infrastructure/QuranDashboard.Infrastructure/Access/UserProvisioningService.cs:35-127`; `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Configurations/Access/RoleConfiguration.cs:26-31` | Resolved: several Owners, one role row, normalized env list, verified identity, explicit reconciliation, last-active-Owner preflight. |
| Non-owner direct permissions | [Report §§6, 8](authorization-permissions-current-state-report.md#6-proposed-minimal-permission-catalogue) | `Backend/domain/QuranDashboard.Domain/Access/User.cs:12-18`; `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Configurations/Access/UserConfiguration.cs:33-39`; `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/QuranDashboardDbContext.cs:53-54` | Adopted: nullable role only for Owner; 19 direct write grants; no non-owner roles. |
| Current Abwab route map | [Report §5](authorization-permissions-current-state-report.md#5-complete-abwab-endpoint-permission-matrix) | Each controller/action is cited in section 8; route completeness is locked by `Backend/tests/QuranDashboard.Tests/Smoke/SmokeRouteCatalog.cs:224-359` and `Backend/tests/QuranDashboard.Tests/Smoke/SmokeCoverageParityTests.cs:10-35`. | All 25 routes retained; four reads changed to public target, 21 writes retain exact mapping. |
| Composite actions | [Report §7](authorization-permissions-current-state-report.md#7-composite-action-decisions) | `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Writes/Abwab/EfAbwabDoorsWriter.cs:88-435`; `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Writes/Abwab/EfAbwabTemplatesWriter.cs:9-213`; `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Writes/Abwab/EfAbwabTemplateApplyWriter.cs:11-145` | Adopted unchanged: one user-visible permission per current composite action. |
| Backend architecture | [Report §9](authorization-permissions-current-state-report.md#9-proposed-backend-authorization-direction) | `Backend/api/QuranDashboard.Api/Authentication/AuthenticationRegistration.cs:50-58`; `Backend/infrastructure/QuranDashboard.Infrastructure/Access/CachedUserRoleResolver.cs:8-52`; `Backend/tests/QuranDashboard.Tests/Smoke/SmokeCoverageParityTests.cs:41-68` | Exact-write handler, Owner bypass, request scope, fail-closed metadata, and controlled responses adopted; global active-read fallback rejected. |
| Frontend behavior | [Report §10](authorization-permissions-current-state-report.md#10-frontend-integration-direction) | `Frontend/quran-dashboard-ui/src/app/app.routes.ts:22-69`; `Frontend/quran-dashboard-ui/src/app/features/abwab/abwab.routes.ts:12-23`; `Frontend/quran-dashboard-ui/src/app/core/auth/current-user.store.ts:8-79`; current Abwab templates/pages cited in section 16 | Permission-aware write UX adopted; active-user guard for public routes rejected. |
| Testing debt | [Report §11](authorization-permissions-current-state-report.md#11-testing-debt-and-acceptance-matrix) | `docs/TESTING_DEBT.md:20-25`, `33-37`, `58-63`, `83-87`, `98-102`, `138-142` | Five mandatory rows retained; authorization personas corrected for public reads. |
| Previously open questions | [Report §13](authorization-permissions-current-state-report.md#13-questions-that-genuinely-require-product-owner-decisions) | `Backend/domain/QuranDashboard.Domain/Access/UserStatus.cs:5-10`; `Backend/infrastructure/QuranDashboard.Infrastructure/Access/UserProvisioningService.cs:35-127`; `Backend/tests/QuranDashboard.Tests/Api/Access/AccessMeEndpointTests.cs:116-145` | All five resolved: multiple configured Owners, Owner-only administration, zero-grant reactivation, append-only audit, Owner-only explicit relink. |

There are no unresolved product or architecture contradictions in this record. The current singular Owner option, Admin/Editor seeds/policies, role-oriented frontend model, absent permission/audit schema, absent `403` writer, and open Abwab writes are implementation gaps against the accepted target—not conflicting target decisions.

## 23. Final readiness verdict

`READY_FOR_IMPLEMENTATION_PLAN`

The endpoint inventory, 19-code catalogue, public-read boundary, multiple-Owner model, administration authority, status/grant lifecycle, relink rules, audit contract, data shape, Backend fail-closed posture, frontend behavior, migration invariants, and acceptance obligations are now decided. Implementation planning may begin from this record without reopening them.
