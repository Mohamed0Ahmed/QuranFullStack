using System.Text.Json;

namespace QuranDashboard.DataImporter.Import.AbwabSnapshotExport;

internal static class AbwabSnapshotTopologyValidator
{
    internal static void Validate(AbwabSnapshotDocument snapshot, ICollection<string> errors)
    {
        ValidateHierarchies(snapshot, errors);
        ValidateRelationTypes(snapshot, errors);
    }

    private static void ValidateHierarchies(AbwabSnapshotDocument snapshot, ICollection<string> errors)
    {
        var doors = snapshot.Tables["abwab_doors"];
        if (!IsParentGraphAcyclic(doors, "parent_id"))
        {
            errors.Add("The Abwab door hierarchy contains a cycle.");
        }

        var doorSections = doors.ToDictionary(
            row => row.GetProperty("id").GetInt64(),
            row => row.GetProperty("section_id").GetInt64());
        var doorSectionMismatches = doors.Count(row =>
            TryReadNullableInt64(row, "parent_id", out var parentId)
            && parentId.HasValue
            && doorSections.TryGetValue(parentId.Value, out var parentSection)
            && parentSection != row.GetProperty("section_id").GetInt64());
        if (doorSectionMismatches > 0)
        {
            errors.Add($"The door hierarchy contains {doorSectionMismatches} parent-section mismatches.");
        }

        var nodes = snapshot.Tables["abwab_template_nodes"];
        if (!IsParentGraphAcyclic(nodes, "parent_node_id"))
        {
            errors.Add("The Abwab template hierarchy contains a cycle.");
        }

        var nodeTemplates = nodes.ToDictionary(
            row => row.GetProperty("id").GetInt64(),
            row => row.GetProperty("template_id").GetInt64());
        var nodeTemplateMismatches = nodes.Count(row =>
            TryReadNullableInt64(row, "parent_node_id", out var parentId)
            && parentId.HasValue
            && nodeTemplates.TryGetValue(parentId.Value, out var parentTemplate)
            && parentTemplate != row.GetProperty("template_id").GetInt64());
        if (nodeTemplateMismatches > 0)
        {
            errors.Add($"The template hierarchy contains {nodeTemplateMismatches} parent-template mismatches.");
        }

        var activeInclusions = snapshot.Tables["abwab_door_inclusions"]
            .Where(row => row.GetProperty("deleted_at").ValueKind == JsonValueKind.Null)
            .Select(row => (
                Source: row.GetProperty("source_door_id").GetInt64(),
                Target: row.GetProperty("target_door_id").GetInt64()))
            .ToArray();
        if (!IsDirectedGraphAcyclic(activeInclusions))
        {
            errors.Add("The active Abwab door-inclusion graph contains a cycle.");
        }
    }

    private static bool IsParentGraphAcyclic(IReadOnlyList<JsonElement> rows, string parentProperty)
    {
        var edges = rows
            .Select(row => (
                Child: row.GetProperty("id").GetInt64(),
                Parent: ReadNullableInt64(row, parentProperty)))
            .Where(edge => edge.Parent.HasValue)
            .Select(edge => (Source: edge.Parent!.Value, Target: edge.Child));
        return IsDirectedGraphAcyclic(edges);
    }

    private static bool IsDirectedGraphAcyclic(IEnumerable<(long Source, long Target)> sourceEdges)
    {
        var edges = sourceEdges.Distinct().ToArray();
        var nodes = edges.SelectMany(edge => new[] { edge.Source, edge.Target }).Distinct().ToArray();
        var indegree = nodes.ToDictionary(node => node, _ => 0);
        var targetsBySource = edges
            .GroupBy(edge => edge.Source)
            .ToDictionary(group => group.Key, group => group.Select(edge => edge.Target).Distinct().ToArray());
        foreach (var edge in edges)
        {
            indegree[edge.Target]++;
        }

        var ready = new Queue<long>(indegree.Where(item => item.Value == 0).Select(item => item.Key));
        var visited = 0;
        while (ready.TryDequeue(out var node))
        {
            visited++;
            if (!targetsBySource.TryGetValue(node, out var targets))
            {
                continue;
            }

            foreach (var target in targets)
            {
                if (--indegree[target] == 0)
                {
                    ready.Enqueue(target);
                }
            }
        }

        return visited == nodes.Length;
    }

    private static void ValidateRelationTypes(AbwabSnapshotDocument snapshot, ICollection<string> errors)
    {
        var invalid = snapshot.Tables["abwab_door_relations"].Count(row =>
        {
            var doorA = row.GetProperty("door_a_id").GetInt64();
            var doorB = row.GetProperty("door_b_id").GetInt64();
            var relationType = row.GetProperty("relation_type").GetInt32();
            var broader = ReadNullableInt64(row, "broader_door_id");
            return doorA >= doorB
                || relationType is < 1 or > 3
                || (relationType == 3 && (!broader.HasValue || (broader != doorA && broader != doorB)))
                || (relationType != 3 && broader.HasValue);
        });

        if (invalid > 0)
        {
            errors.Add($"Abwab relations contain {invalid} invalid type or direction rows.");
        }
    }

    private static long? ReadNullableInt64(JsonElement row, string property) =>
        TryReadNullableInt64(row, property, out var value) ? value : null;

    private static bool TryReadNullableInt64(JsonElement row, string property, out long? value)
    {
        value = null;
        if (!row.TryGetProperty(property, out var element))
        {
            return false;
        }

        if (element.ValueKind == JsonValueKind.Null)
        {
            return true;
        }

        if (!element.TryGetInt64(out var parsed))
        {
            return false;
        }

        value = parsed;
        return true;
    }
}
