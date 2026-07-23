// Self-contained browser fixture for the Abwab core-slice source suite (T066). Modeled on the same
// pattern as e2e/spikes/synthetic-tree.fixture.ts and e2e/permissions/non-authoritative.spec.ts: a
// standalone page (no Angular bootstrap, no dev server) driven with page.route stubs, that mirrors
// the SHIPPED contract semantics from features/abwab/data-access (explicit actions, no drag, no
// edit-session lock, post-mutation cache invalidation + reload, RTL, keyboard nav, virtualization) —
// not a re-implementation of the Angular components themselves.
export const ABWAB_API_ORIGIN = 'http://localhost';

export interface AbwabFixtureNode {
  readonly id: string;
  readonly parentId: string | null;
  readonly order: number;
}

export interface AbwabCoreSliceFixtureOptions {
  readonly nodeCount: number;
  readonly branching?: number;
  readonly rowHeight?: number;
  readonly viewportHeight?: number;
  readonly expandAllOnLoad?: boolean;
}

export function buildAbwabCoreSliceFixture(options: AbwabCoreSliceFixtureOptions): string {
  const rowHeight = options.rowHeight ?? 32;
  const viewportHeight = options.viewportHeight ?? 400;
  const branching = options.branching ?? 6;
  const nodeCount = options.nodeCount;
  const expandAllOnLoad = options.expandAllOnLoad ?? false;

  return `
    <main dir="rtl" lang="ar" style="font-family: system-ui, sans-serif;">
      <div data-testid="conflict-message" role="alert" style="display:none;"></div>
      <div data-testid="selected-category-id"></div>
      <div data-testid="tree-fetch-count" data-count="0"></div>
      <div
        data-testid="tree-viewport"
        role="tree"
        tabindex="0"
        style="height:${viewportHeight}px; overflow:auto; position:relative; border:1px solid #ccc;"
      ></div>
    </main>
    <script>
      (function () {
        var API = '${ABWAB_API_ORIGIN}';
        var NODE_COUNT = ${nodeCount};
        var BRANCHING = ${branching};
        var ROW = ${rowHeight};
        var OVERSCAN = 8;
        var EXPAND_ALL_ON_LOAD = ${expandAllOnLoad ? 'true' : 'false'};

        var expanded = Object.create(null);
        var selectedId = null;
        var focusedIndex = 0;
        var fetchCount = 0;
        var nodes = []; // flat authoritative list [{id, parentId, order, name}]
        var treeRevision = 1;
        var timelineGeneration = 1;

        // A shallow forest, not a deep heap: the first ROOT_COUNT nodes are independent root
        // categories (so a small fixture already shows several root rows with nothing expanded);
        // the rest are distributed one level deep, round-robin, under those roots.
        function seedNodes() {
          nodes = [];
          var rootCount = Math.min(NODE_COUNT, BRANCHING);
          for (var i = 0; i < NODE_COUNT; i++) {
            var parentId = i < rootCount ? null : 'n' + (i % rootCount);
            nodes.push({ id: 'n' + i, parentId: parentId, order: i, name: 'باب ' + i });
          }
          if (EXPAND_ALL_ON_LOAD) {
            for (var j = 0; j < NODE_COUNT; j++) {
              expanded['n' + j] = true;
            }
          }
        }
        seedNodes();

        function childrenOf(parentId) {
          return nodes.filter(function (n) { return n.parentId === parentId; });
        }
        function hasChildren(id) {
          return childrenOf(id).length > 0;
        }

        function visibleNodes() {
          var result = [];
          function visit(parentId, depth) {
            var kids = childrenOf(parentId);
            for (var i = 0; i < kids.length; i++) {
              var node = kids[i];
              result.push({ node: node, depth: depth });
              if (hasChildren(node.id) && expanded[node.id]) {
                visit(node.id, depth + 1);
              }
            }
          }
          visit(null, 0);
          return result;
        }

        async function loadTree() {
          fetchCount++;
          document.querySelector('[data-testid=tree-fetch-count]').setAttribute('data-count', String(fetchCount));
          var response = await fetch(API + '/api/abwab/tree');
          var body = await response.json();
          treeRevision = body.data.treeRevision;
          timelineGeneration = body.data.expectedTimelineGeneration.generation;
          render();
        }

        function render() {
          var viewport = document.querySelector('[data-testid=tree-viewport]');
          var visible = visibleNodes();
          viewport.setAttribute('data-total', String(visible.length));

          var visibleWindow = Math.ceil(viewport.clientHeight / ROW);
          var scrollIndex = Math.floor(viewport.scrollTop / ROW);
          var start = Math.max(0, scrollIndex - OVERSCAN);
          var end = Math.min(visible.length, start + visibleWindow + OVERSCAN * 2);

          var html = '<div data-testid="tree-spacer" style="height:' + (visible.length * ROW) + 'px; position:relative;">';
          for (var i = start; i < end; i++) {
            var entry = visible[i];
            var isSelected = entry.node.id === selectedId;
            html +=
              '<div data-testid="tree-row" data-index="' + i + '" data-id="' + entry.node.id + '"' +
              ' tabindex="0" role="treeitem" aria-selected="' + (isSelected ? 'true' : 'false') + '"' +
              ' style="position:absolute; top:' + (i * ROW) + 'px; height:' + ROW + 'px; inset-inline-start:0;' +
              ' padding-inline-start:' + (entry.depth * 16) + 'px; cursor:pointer;' +
              (isSelected ? ' background:#eef;' : '') + '">' +
              entry.node.name +
              (hasChildren(entry.node.id) ? ' <button type="button" data-testid="tree-toggle" data-id="' + entry.node.id + '">' + (expanded[entry.node.id] ? '▾' : '◂') + '</button>' : '') +
              (isSelected ? ' <button type="button" data-testid="tree-move-up" data-id="' + entry.node.id + '">أعلى</button>' +
                ' <button type="button" data-testid="tree-move-down" data-id="' + entry.node.id + '">أسفل</button>' +
                ' <button type="button" data-testid="tree-edit" data-id="' + entry.node.id + '">تعديل</button>' : '') +
              '</div>';
          }
          html += '</div>';
          viewport.innerHTML = html;

          viewport.querySelectorAll('[data-testid=tree-row]').forEach(function (row) {
            row.addEventListener('click', function () { selectRow(row.getAttribute('data-id')); });
          });
          viewport.querySelectorAll('[data-testid=tree-toggle]').forEach(function (button) {
            button.addEventListener('click', function (event) {
              event.stopPropagation();
              var id = button.getAttribute('data-id');
              expanded[id] = !expanded[id];
              render();
            });
          });
          viewport.querySelectorAll('[data-testid=tree-move-up]').forEach(function (button) {
            button.addEventListener('click', function (event) { event.stopPropagation(); reorder(button.getAttribute('data-id'), -1); });
          });
          viewport.querySelectorAll('[data-testid=tree-move-down]').forEach(function (button) {
            button.addEventListener('click', function (event) { event.stopPropagation(); reorder(button.getAttribute('data-id'), 1); });
          });
          viewport.querySelectorAll('[data-testid=tree-edit]').forEach(function (button) {
            button.addEventListener('click', function (event) { event.stopPropagation(); openEditor(button.getAttribute('data-id')); });
          });

          // Explicit-action / no-drag proof: a synthetic dragstart never reorders anything (no
          // draggable attribute, no dragstart/drop listener exists on any row).
        }

        function selectRow(id) {
          selectedId = id;
          document.querySelector('[data-testid=selected-category-id]').textContent = id;
          render();
        }

        window.__abwabOpenedEditorFor = null;
        function openEditor(id) {
          // Explicit save only — opening the editor issues NO request at all (no lock/session call).
          window.__abwabOpenedEditorFor = id;
        }

        async function reorder(id, direction) {
          var response = await fetch(API + '/api/abwab/categories/reorder', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ categoryId: id, direction: direction, expectedTreeRevision: treeRevision, expectedTimelineGeneration: timelineGeneration }),
          });
          var body = await response.json();
          var conflictBox = document.querySelector('[data-testid=conflict-message]');
          if (!response.ok || !body.isSuccess) {
            conflictBox.style.display = 'block';
            conflictBox.textContent = (body.errors && body.errors[0]) || 'conflict';
          } else {
            conflictBox.style.display = 'none';
          }
          // Cache rule: EVERY mutation outcome (success or conflict) invalidates the cached tree and
          // reloads from the server — this is the rollback-to-server-truth behavior under test.
          await loadTree();
        }

        var viewportEl = document.querySelector('[data-testid=tree-viewport]');
        viewportEl.addEventListener('scroll', render, { passive: true });
        viewportEl.addEventListener('keydown', function (event) {
          var visible = visibleNodes();
          if (visible.length === 0) return;
          if (event.key === 'ArrowDown') {
            event.preventDefault();
            focusedIndex = Math.min(focusedIndex + 1, visible.length - 1);
            focusRow(focusedIndex);
          } else if (event.key === 'ArrowUp') {
            event.preventDefault();
            focusedIndex = Math.max(focusedIndex - 1, 0);
            focusRow(focusedIndex);
          } else if (event.key === 'Home') {
            event.preventDefault();
            focusedIndex = 0;
            focusRow(focusedIndex);
          } else if (event.key === 'End') {
            event.preventDefault();
            focusedIndex = visible.length - 1;
            focusRow(focusedIndex);
          } else if (event.key === 'Enter' || event.key === ' ') {
            event.preventDefault();
            selectRow(visible[focusedIndex].node.id);
          } else if (event.key === 'ArrowLeft') {
            var entry = visible[focusedIndex];
            if (hasChildren(entry.node.id) && !expanded[entry.node.id]) {
              expanded[entry.node.id] = true;
              render();
            }
          } else if (event.key === 'ArrowRight') {
            var entry2 = visible[focusedIndex];
            if (hasChildren(entry2.node.id) && expanded[entry2.node.id]) {
              expanded[entry2.node.id] = false;
              render();
            }
          }
        });

        function focusRow(index) {
          render();
          var target = viewportEl.querySelector('[data-index="' + index + '"]');
          if (target) target.focus();
        }

        window.__abwabFixture = {
          expand: function (id) { expanded[id] = true; render(); },
          select: selectRow,
          loadTree: loadTree,
          getFetchCount: function () { return fetchCount; },
        };

        loadTree();
      })();
    </script>
  `;
}
