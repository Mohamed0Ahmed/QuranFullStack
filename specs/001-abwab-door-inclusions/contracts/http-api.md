# HTTP Contract: Abwab Door Inclusions

**Base resource**: `/api/abwab/doors/{targetDoorId}/inclusions`
**Envelope**: Existing `ApiResponse<T>` with English property names and localized Arabic messages

This is a planning-time contract. Implementation exports the authoritative OpenAPI document and
regenerates frontend models through `Backend/scripts/check-api-contract`.

## Shared Shapes

### ApiResponse

```json
{
  "isSuccess": true,
  "message": "رسالة عربية محلية",
  "data": {},
  "errors": []
}
```

- Success uses `isSuccess: true` and the endpoint-specific `data` shape.
- Controlled failure uses `isSuccess: false`, a safe localized message, and `errors`.
- Internal exception, SQL, path, source-content, and synchronization-ledger details never appear.

### DirectInclusionDoor

```json
{
  "inclusionId": 41,
  "doorId": 930007,
  "doorName": "اسم الباب",
  "isArchived": false
}
```

### DoorInclusion

```json
{
  "inclusionId": 41,
  "targetDoorId": 930001,
  "sourceDoorId": 930007,
  "sourceDoorName": "اسم الباب",
  "isSourceArchived": false
}
```

No topology or inclusion shape identifies the origin of a Quran record, ayah, selected word, or
description.

## GET `/api/abwab/doors/{doorId}/inclusions`

Returns the requested door's direct topology in both directions.

**Authorization**: Public read

### Success — `200 OK`

```json
{
  "isSuccess": true,
  "message": "تم تحميل مصادر الباب",
  "data": {
    "doorId": 930001,
    "doorVersion": 123456,
    "sources": [
      {
        "inclusionId": 41,
        "doorId": 930007,
        "doorName": "اسم الباب المصدر",
        "isArchived": false
      }
    ],
    "consumers": [
      {
        "inclusionId": 52,
        "doorId": 930099,
        "doorName": "اسم الباب المستهدف",
        "isArchived": true
      }
    ]
  },
  "errors": []
}
```

Rules:

- `sources` contains active direct edges where the requested door is the target.
- `consumers` contains active direct edges where the requested door is the source.
- Archived participating doors remain present with `isArchived: true`.
- An archived requested door may be read.
- The endpoint returns the complete direct topology; it is not a transitive expansion and has no
  product cap or pagination in V1.
- No conditional validator/ETag is added by this feature.

### Controlled outcomes

| Status | Condition |
| --- | --- |
| `400 Bad Request` | Invalid route shape |
| `404 Not Found` | Requested door does not exist |

## POST `/api/abwab/doors/{targetDoorId}/inclusions`

Adds one or multiple live sources to one aggregate target and performs initial/transitive sync in
one transaction.

**Authorization**: Exactly `[RequirePermission(AbwabPermissions.Inclusions.Create)]`

### Request

```json
{
  "expectedTargetDoorVersion": 123456,
  "sourceDoorIds": [930007, 930008]
}
```

Validation:

- `targetDoorId` must be a valid positive door ID.
- `sourceDoorIds` must be non-empty, contain valid positive IDs, and contain no repetition.
- The target cannot appear in `sourceDoorIds`.
- Target and sources must exist and be live at creation time.
- No active direct target/source pair may already exist.
- The active graph plus the complete proposed batch must remain acyclic.
- `expectedTargetDoorVersion` must match after synchronization ownership is acquired.
- The complete source list succeeds or fails as one action; there is no multi-target body.

### Success — `201 Created`

The response is returned only after all initial and transitive synchronization has completed.

```json
{
  "isSuccess": true,
  "message": "تمت إضافة أبواب المصدر بنجاح",
  "data": {
    "targetDoorId": 930001,
    "targetDoorVersion": 123457,
    "added": [
      {
        "inclusionId": 41,
        "targetDoorId": 930001,
        "sourceDoorId": 930007,
        "sourceDoorName": "اسم الباب المصدر",
        "isSourceArchived": false
      }
    ]
  },
  "errors": []
}
```

### Controlled outcomes

| Status | Condition |
| --- | --- |
| `400 Bad Request` | Invalid IDs, empty list, repeated submitted source, or self-inclusion |
| `401 Unauthorized` | Missing or invalid authentication |
| `403 Forbidden` | Active authenticated caller lacks inclusion-create permission |
| `404 Not Found` | Target or any source does not exist |
| `409 Conflict` | Archived target/source, duplicate active edge, cycle, stale target version, or source/target link state changed before lock ownership |
| `503 Service Unavailable` | Synchronization infrastructure cannot complete the transaction safely |

Every failure leaves zero edge, clone, mapping, projection, version, and downstream changes from the
attempt.

## DELETE `/api/abwab/doors/{targetDoorId}/inclusions/{inclusionId}`

Detaches one direct source from one aggregate target and removes only state owned by that edge.

**Authorization**: Exactly `[RequirePermission(AbwabPermissions.Inclusions.Delete)]`

### Request

```json
{
  "expectedTargetDoorVersion": 123457
}
```

### Success — `200 OK`

```json
{
  "isSuccess": true,
  "message": "تم فصل الباب المُضمَّن",
  "data": {
    "inclusionId": 41,
    "removedSynchronizedRecordCount": 8,
    "targetDoorVersion": 123458
  },
  "errors": []
}
```

Rules:

- The inclusion must be active and belong to `targetDoorId`.
- Remove active and overridden clones, suppressed mappings, internal contribution ownership, and
  the active edge.
- Rebuild affected target ayah/word projection and propagate resulting target deletions.
- Leave the source, target-direct records, and other inclusion edges unchanged.
- Return `200`, not `204`, because the client needs the removal summary and new target version.

### Controlled outcomes

| Status | Condition |
| --- | --- |
| `400 Bad Request` | Invalid route or body shape |
| `401 Unauthorized` | Missing or invalid authentication |
| `403 Forbidden` | Active authenticated caller lacks inclusion-delete permission |
| `404 Not Found` | Active inclusion is not found under that target |
| `409 Conflict` | Target is archived or its version is stale |
| `503 Service Unavailable` | Detach synchronization cannot complete atomically |

## Tree DTO Addition

Each existing Abwab tree door DTO gains:

```json
{
  "inclusionSourceCount": 2,
  "inclusionConsumerCount": 1
}
```

- Counts are direct active edges only.
- Archived participants remain counted while their edge is active.
- Existing `linkCount`, `selectedWordCount`, `relationCount`, child counts, hierarchy metrics, and
  their meanings do not change.

## Existing Link Contract — Unchanged

These remain the only content-record routes:

```text
GET    /api/abwab/doors/{doorId}/links/snapshot
GET    /api/abwab/doors/{doorId}/links
GET    /api/abwab/doors/{doorId}/links/{unitId}/ayahs
PATCH  /api/abwab/doors/{doorId}/links/{unitId}/words
POST   /api/abwab/doors/{doorId}/links/bulk-delete
```

- Request and response DTO shapes remain unchanged.
- Synchronized units are returned as ordinary link records.
- No origin, source-door, inclusion ID, internal contribution, sync state, override, suppression,
  or fingerprint field is added.
- Existing PATCH and bulk-delete authorization remains Owner-only.
- Backend dispatches synchronized target edits to local override and deletes to local suppression.
- Copying a synchronized record produces an ordinary direct record at the destination.

## Permission Catalogue Contract

Add the independent Arabic group `إدارة مصادر الباب`:

```text
abwab.inclusions.create
abwab.inclusions.delete
```

Do not reuse relation permissions. Owner bypass and startup catalogue synchronization remain
authoritative. Frontend permission constants are generated, never hand-edited.

## Generation Contract

Implementation order:

1. Build Backend request/response DTOs and controller behavior.
2. Run `Backend/scripts/check-api-contract` to export Swagger and regenerate retained frontend
   models.
3. Review only sanctioned generated model/spec changes; generated services are pruned.
4. Run `npm run generate:permission-codes` after permission catalogue changes.

Planning does not execute any generation command.
