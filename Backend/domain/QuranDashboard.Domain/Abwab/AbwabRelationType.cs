namespace QuranDashboard.Domain.Abwab;

// Persisted verbatim as abwab_door_relations.relation_type, and named by the direction CHECK
// constraint's literal 3 (AbwabDoorRelationConfiguration) — reordering these members rewrites the
// meaning of every stored row. Starts at 1 for the same reason AbwabReorderScope does: a missing
// or unrecognized JSON enum property lands on 0, which must not be a valid type.
public enum AbwabRelationType
{
    Similarity = 1,
    Opposition = 2,
    Comprehensiveness = 3,
}
