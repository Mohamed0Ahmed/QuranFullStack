import { ABWAB_CONFLICT_MESSAGES } from '../../src/app/features/abwab/data-access/abwab-conflict';

// Self-contained browser fixture for the Abwab template slice (T053). Same pattern as
// e2e/abwab/relationships-slice.fixture.ts: a standalone page (no Angular bootstrap, no dev server)
// driven with page.route stubs.
//
// WHAT THIS PROVES AND WHAT IT DOES NOT. The Playwright config registers no `webServer` and no
// `baseURL`, so this suite proves the slice's BROWSER-LEVEL contract (real DOM, real event dispatch,
// real fetch/409 handling): explicit save with no autosave and no edit-session lock, explicit
// ordering/reparent actions with no drag, native keyboard focus order under RTL, the alias
// add/edit/remove/restore affordances, the application flow, invalidate-and-reload on success AND on
// conflict, the Arabic conflict message rather than a raw abwab.* code, and post-mutation context
// preservation. It does NOT prove the Angular components themselves —
// AbwabTemplatesPageComponent / TemplateNodeEditorComponent / TemplateApplicationPanelComponent /
// AbwabTemplatesFacade are covered by their own specs (listed below), which drive the real
// components through the real reactive-forms value accessors.
// The markup mirrors the shipped template's structure — the same data-testids, the same per-row
// action order, `node-reparent` inside the open edit form, the same alias sub-editor, and the same
// TWO post-mutation reads the facade issues (list + detail) — so the two layers cannot describe
// different products. The Arabic conflict copy is IMPORTED from the shipped
// `ABWAB_CONFLICT_MESSAGES`, never restated, so changing a message fails this suite. Synthetic
// Arabic only. The Angular layer it defers to is covered by
// src/app/features/abwab/templates/abwab-templates-page.component.spec.ts,
// src/app/features/abwab/state/abwab-templates.facade.spec.ts,
// src/app/features/abwab/data-access/abwab-templates-cache.spec.ts and
// src/app/features/abwab/data-access/abwab-templates-parity.spec.ts.
export const ABWAB_API_ORIGIN = 'http://localhost';

export { ABWAB_CONFLICT_MESSAGES };

export interface AbwabTemplatesFixtureOptions {
  readonly doorTemplateId?: string;
}

export function buildAbwabTemplatesFixture(options: AbwabTemplatesFixtureOptions = {}): string {
  const doorTemplateId = options.doorTemplateId ?? 'template-1';

  return `
    <main dir="rtl" lang="ar" style="font-family: system-ui, sans-serif;">
      <h1>قوالب الأبواب</h1>
      <p data-testid="templates-conflict" role="alert" style="display:none;"></p>
      <div data-testid="templates-fetch-count" data-count="0"></div>
      <div data-testid="templates-status">idle</div>

      <form data-testid="template-node-add-form">
        <label for="node-name">اسم العقدة</label>
        <input id="node-name" data-testid="node-name" type="text" />
        <label for="node-parent">العقدة الأصل</label>
        <select id="node-parent" data-testid="node-parent">
          <option value="">— جذر القالب —</option>
        </select>
        <button type="button" data-testid="node-add">إضافة عقدة</button>
      </form>

      <ul data-testid="template-node-list" style="list-style:none; padding:0;"></ul>

      <form data-testid="template-apply-form">
        <label for="apply-target">الباب الهدف</label>
        <select id="apply-target" data-testid="apply-target">
          <option value="category-target">باب هدف</option>
          <option value="category-protected">باب محمي</option>
        </select>
        <button type="button" data-testid="apply-begin">تطبيق القالب</button>
      </form>
      <div data-testid="apply-confirm" style="display:none;">
        <button type="button" data-testid="apply-confirm-yes">تأكيد</button>
      </div>

      <script>
        (function () {
          var API = '${ABWAB_API_ORIGIN}';
          var TEMPLATE_ID = '${doorTemplateId}';
          var MESSAGES = ${JSON.stringify(ABWAB_CONFLICT_MESSAGES)};

          var nodes = [];
          var templateRevision = 0;
          var generation = 1;
          var fetchCount = 0;
          var editingNodeId = null;
          var aliasAddingNodeId = null;
          var editingAliasId = null;
          // The shipped editor holds its forms in reactive-forms state, so a re-render after a
          // REJECTED write must not overwrite what the operator typed (§14.3).
          var drafts = {};

          function statusBox() { return document.querySelector('[data-testid=templates-status]'); }
          function conflictBox() { return document.querySelector('[data-testid=templates-conflict]'); }
          function parentSelect() { return document.querySelector('[data-testid=node-parent]'); }

          function countFetch() {
            fetchCount++;
            document.querySelector('[data-testid=templates-fetch-count]').setAttribute('data-count', String(fetchCount));
          }

          // The facade re-reads BOTH projections after every outcome: the list moves a template
          // between its active/deleted projections, and the detail carries the node tree.
          async function loadTemplate() {
            statusBox().textContent = 'loading';
            countFetch();
            await fetch(API + '/api/abwab/templates?includeDeleted=false');

            countFetch();
            var response = await fetch(API + '/api/abwab/templates/' + TEMPLATE_ID + '?includeDeleted=false');
            var body = await response.json();
            nodes = body.data.nodes;
            templateRevision = body.data.template.templateRevision;
            generation = body.data.expectedTimelineGeneration.generation;
            statusBox().textContent = nodes.length === 0 ? 'empty' : 'ready';
            render();
          }

          function escapeText(value) {
            return String(value).replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;').replace(/"/g, '&quot;');
          }

          function aliasMarkup(node) {
            var html = '<ul data-testid="node-alias-list" style="list-style:none; padding:0;">';
            var aliases = node.aliases || [];
            if (aliases.length === 0) {
              html += '<li data-testid="node-alias-empty">لا توجد مرادفات لهذه العقدة.</li>';
            }
            for (var a = 0; a < aliases.length; a++) {
              var alias = aliases[a];
              html += '<li data-testid="node-alias-row" data-id="' + alias.templateNodeSearchAliasId + '">';
              if (editingAliasId === alias.templateNodeSearchAliasId) {
                html +=
                  '<form data-testid="node-alias-edit-form">' +
                  '<label for="node-alias-edit-value">قيمة المرادف</label>' +
                  '<input id="node-alias-edit-value" data-testid="node-alias-edit-value" type="text" value="' +
                  escapeText(alias.value) + '" />' +
                  '<button type="button" data-testid="node-alias-edit-save" data-id="' + alias.templateNodeSearchAliasId + '">حفظ</button>' +
                  '<button type="button" data-testid="node-alias-edit-cancel">إلغاء</button>' +
                  '</form>';
              } else {
                html += '<span data-testid="node-alias-value">' + escapeText(alias.value) + '</span>';
                if (alias.isDeleted) {
                  html += '<span data-testid="node-alias-deleted-badge">محذوف</span>' +
                    '<button type="button" data-testid="node-alias-restore" data-id="' + alias.templateNodeSearchAliasId +
                    '" data-version="' + alias.version + '">استعادة</button>';
                } else {
                  html += '<button type="button" data-testid="node-alias-edit" data-id="' + alias.templateNodeSearchAliasId + '">تعديل</button>' +
                    '<button type="button" data-testid="node-alias-remove" data-id="' + alias.templateNodeSearchAliasId +
                    '" data-version="' + alias.version + '">إزالة</button>';
                }
              }
              html += '</li>';
            }
            html += '</ul>';

            if (aliasAddingNodeId === node.templateNodeId) {
              html +=
                '<form data-testid="node-alias-add-form">' +
                '<label for="node-alias-add-value">مرادف جديد</label>' +
                '<input id="node-alias-add-value" data-testid="node-alias-add-value" type="text" />' +
                '<button type="button" data-testid="node-alias-add-save" data-id="' + node.templateNodeId + '">حفظ المرادف</button>' +
                '<button type="button" data-testid="node-alias-add-cancel">إلغاء</button>' +
                '</form>';
            } else {
              html += '<button type="button" data-testid="node-alias-add-open" data-id="' + node.templateNodeId + '">إضافة مرادف</button>';
            }

            return '<div data-testid="node-aliases" data-node-id="' + node.templateNodeId + '">' + html + '</div>';
          }

          function rowMarkup(node, index) {
            var html =
              '<li data-testid="template-node-row" data-id="' + node.templateNodeId + '" data-order="' + node.siblingOrder + '">' +
              '<span data-testid="node-name-label">' + escapeText(node.name) + '</span>';

            if (editingNodeId === node.templateNodeId) {
              // Reparent lives INSIDE the open edit form, exactly as the shipped editor renders it.
              var options = '<option value="">— جذر القالب —</option>';
              for (var j = 0; j < nodes.length; j++) {
                if (nodes[j].templateNodeId !== node.templateNodeId) {
                  options += '<option value="' + nodes[j].templateNodeId + '">' + escapeText(nodes[j].name) + '</option>';
                }
              }
              html +=
                '<form data-testid="template-node-edit-form">' +
                '<label for="node-edit-name">اسم العقدة</label>' +
                '<input id="node-edit-name" data-testid="node-edit-name" type="text" value="' + escapeText(node.name) + '" />' +
                '<label for="node-edit-parent">العقدة الأصل</label>' +
                '<select id="node-edit-parent" data-testid="node-edit-parent">' + options + '</select>' +
                '<button type="button" data-testid="node-edit-save" data-id="' + node.templateNodeId + '">حفظ</button>' +
                '<button type="button" data-testid="node-reparent" data-id="' + node.templateNodeId + '">نقل إلى العقدة المحددة</button>' +
                '<button type="button" data-testid="node-edit-cancel">إلغاء</button>' +
                '</form>';
            } else {
              html +=
                '<div>' +
                '<button type="button" data-testid="node-edit" data-id="' + node.templateNodeId + '">تعديل</button>' +
                '<button type="button" data-testid="node-move-up" data-id="' + node.templateNodeId + '"' +
                (index === 0 ? ' disabled' : '') + '>أعلى</button>' +
                '<button type="button" data-testid="node-move-down" data-id="' + node.templateNodeId + '"' +
                (index === nodes.length - 1 ? ' disabled' : '') + '>أسفل</button>' +
                '<button type="button" data-testid="node-remove" data-id="' + node.templateNodeId + '">إزالة</button>' +
                '</div>';
            }

            return html + aliasMarkup(node) + '</li>';
          }

          function render() {
            var list = document.querySelector('[data-testid=template-node-list]');
            list.setAttribute('data-total', String(nodes.length));
            var html = '';
            for (var i = 0; i < nodes.length; i++) {
              html += rowMarkup(nodes[i], i);
            }
            list.innerHTML = html;

            var parentOptions = '<option value="">— جذر القالب —</option>';
            for (var k = 0; k < nodes.length; k++) {
              parentOptions += '<option value="' + nodes[k].templateNodeId + '">' + escapeText(nodes[k].name) + '</option>';
            }
            var previousParent = parentSelect().value;
            parentSelect().innerHTML = parentOptions;
            parentSelect().value = previousParent;

            restoreDrafts(list);
            bind(list);
          }

          function restoreDrafts(list) {
            ['node-edit-name', 'node-alias-add-value', 'node-alias-edit-value'].forEach(function (testId) {
              var input = list.querySelector('[data-testid=' + testId + ']');
              if (input && typeof drafts[testId] === 'string') {
                input.value = drafts[testId];
              }
            });
          }

          function on(root, testId, handler) {
            root.querySelectorAll('[data-testid=' + testId + ']').forEach(function (element) {
              element.addEventListener('click', function () { handler(element); });
            });
          }

          function bind(list) {
            ['node-edit-name', 'node-alias-add-value', 'node-alias-edit-value'].forEach(function (testId) {
              var input = list.querySelector('[data-testid=' + testId + ']');
              if (input) {
                input.addEventListener('input', function () { drafts[testId] = input.value; });
              }
            });

            on(list, 'node-edit', function (button) {
              editingNodeId = button.getAttribute('data-id');
              editingAliasId = null;
              render();
            });
            on(list, 'node-edit-cancel', function () { editingNodeId = null; render(); });
            on(list, 'node-move-up', function (button) { move(button.getAttribute('data-id'), -1); });
            on(list, 'node-move-down', function (button) { move(button.getAttribute('data-id'), 1); });
            on(list, 'node-remove', function (button) {
              mutate('DELETE', '/api/abwab/templates/nodes/' + button.getAttribute('data-id'), {
                expectedVersion: 1,
                expectedTemplateRevision: templateRevision,
                expectedTimelineGeneration: generation,
              });
            });
            on(list, 'node-edit-save', function (button) {
              mutate('PUT', '/api/abwab/templates/nodes/' + button.getAttribute('data-id'), {
                name: document.querySelector('[data-testid=node-edit-name]').value,
                representativeQuranExcerpt: null,
                description: null,
                expectedVersion: 1,
                expectedTimelineGeneration: generation,
              });
            });
            on(list, 'node-reparent', function (button) {
              mutate('POST', '/api/abwab/templates/nodes/' + button.getAttribute('data-id') + '/reparent', {
                newParentTemplateNodeId: document.querySelector('[data-testid=node-edit-parent]').value || null,
                expectedVersion: 1,
                expectedTemplateRevision: templateRevision,
                expectedTimelineGeneration: generation,
              });
            });

            on(list, 'node-alias-add-open', function (button) {
              aliasAddingNodeId = button.getAttribute('data-id');
              editingAliasId = null;
              render();
            });
            on(list, 'node-alias-add-cancel', function () { aliasAddingNodeId = null; render(); });
            on(list, 'node-alias-add-save', function (button) {
              mutate('POST', '/api/abwab/templates/nodes/' + button.getAttribute('data-id') + '/aliases', {
                value: document.querySelector('[data-testid=node-alias-add-value]').value,
                expectedTimelineGeneration: generation,
              });
            });
            on(list, 'node-alias-edit', function (button) {
              editingAliasId = button.getAttribute('data-id');
              aliasAddingNodeId = null;
              render();
            });
            on(list, 'node-alias-edit-cancel', function () { editingAliasId = null; render(); });
            // Each alias carries its OWN row version, never the node's.
            on(list, 'node-alias-edit-save', function (button) {
              mutate('PUT', '/api/abwab/templates/aliases/' + button.getAttribute('data-id'), {
                value: document.querySelector('[data-testid=node-alias-edit-value]').value,
                expectedVersion: aliasVersionOf(button.getAttribute('data-id')),
                expectedTimelineGeneration: generation,
              });
            });
            on(list, 'node-alias-remove', function (button) {
              mutate('DELETE', '/api/abwab/templates/aliases/' + button.getAttribute('data-id'), {
                expectedVersion: Number(button.getAttribute('data-version')),
                expectedTimelineGeneration: generation,
              });
            });
            on(list, 'node-alias-restore', function (button) {
              mutate('POST', '/api/abwab/templates/aliases/' + button.getAttribute('data-id') + '/restore', {
                expectedVersion: Number(button.getAttribute('data-version')),
                expectedTimelineGeneration: generation,
              });
            });
          }

          function aliasVersionOf(aliasId) {
            for (var i = 0; i < nodes.length; i++) {
              var aliases = nodes[i].aliases || [];
              for (var j = 0; j < aliases.length; j++) {
                if (aliases[j].templateNodeSearchAliasId === aliasId) { return aliases[j].version; }
              }
            }
            return 0;
          }

          // Ordering is an EXPLICIT action: the new sibling order is computed and posted, never
          // inferred from a drop position.
          function move(id, offset) {
            var ids = nodes.map(function (node) { return node.templateNodeId; });
            var index = ids.indexOf(id);
            var target = index + offset;
            if (index < 0 || target < 0 || target >= ids.length) { return; }
            var swapped = ids.slice();
            swapped[index] = ids[target];
            swapped[target] = ids[index];

            mutate('POST', '/api/abwab/templates/' + TEMPLATE_ID + '/nodes/reorder', {
              parentTemplateNodeId: null,
              orderedTemplateNodeIds: swapped,
              expectedTemplateRevision: templateRevision,
              expectedTimelineGeneration: generation,
            });
          }

          // The cache rule: EVERY outcome — success AND conflict — invalidates and reloads from the
          // server. Nothing is applied to the rendered tree ahead of server confirmation, and an open
          // form is closed only once the server has ACCEPTED the write.
          async function mutate(method, path, body) {
            var response = await fetch(API + path, {
              method: method,
              headers: { 'Content-Type': 'application/json' },
              body: JSON.stringify(body),
            });
            var payload = await response.json();
            if (!response.ok || !payload.isSuccess) {
              var code = (payload.errors && payload.errors[0]) || '';
              conflictBox().style.display = 'block';
              conflictBox().textContent = MESSAGES[code] || 'تعذّر تنفيذ العملية.';
            } else {
              conflictBox().style.display = 'none';
              conflictBox().textContent = '';
              editingNodeId = null;
              aliasAddingNodeId = null;
              editingAliasId = null;
              drafts = {};
            }
            await loadTemplate();
          }

          document.querySelector('[data-testid=node-add]').addEventListener('click', function () {
            mutate('POST', '/api/abwab/templates/' + TEMPLATE_ID + '/nodes', {
              parentTemplateNodeId: parentSelect().value || null,
              name: document.querySelector('[data-testid=node-name]').value,
              representativeQuranExcerpt: null,
              description: null,
              expectedTemplateRevision: templateRevision,
              expectedTimelineGeneration: generation,
            });
          });

          document.querySelector('[data-testid=apply-begin]').addEventListener('click', function () {
            document.querySelector('[data-testid=apply-confirm]').style.display = 'block';
          });

          document.querySelector('[data-testid=apply-confirm-yes]').addEventListener('click', function () {
            mutate('POST', '/api/abwab/templates/' + TEMPLATE_ID + '/apply', {
              targetCategoryId: document.querySelector('[data-testid=apply-target]').value,
              expectedTemplateRevision: templateRevision,
              expectedTreeRevision: 1,
              expectedTargetVersion: 1,
              expectedTimelineGeneration: generation,
            });
          });

          window.__abwabTemplatesFixture = {
            getFetchCount: function () { return fetchCount; },
          };

          loadTemplate();
        })();
      </script>
    </main>
  `;
}
