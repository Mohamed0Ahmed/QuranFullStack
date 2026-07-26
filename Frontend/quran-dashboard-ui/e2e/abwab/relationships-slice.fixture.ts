// Self-contained browser fixture for the Abwab relationship slice (T021). Same pattern as
// e2e/abwab/core-slice.fixture.ts: a standalone page (no Angular bootstrap, no dev server) driven
// with page.route stubs.
//
// WHAT THIS PROVES AND WHAT IT DOES NOT. The Playwright config registers no `webServer` and no
// `baseURL` — every e2e spec in this repo owns its own target, and none of them boots the Angular
// app. So this suite proves the slice's BROWSER-LEVEL contract (real DOM, real event dispatch, real
// fetch/409 handling): explicit actions only, no drag, native keyboard focus order under RTL,
// invalidate-and-reload on success AND on conflict, the Arabic conflict message rather than a raw
// abwab.* code, an INTEGER RelationshipType on the wire, and post-mutation context preservation.
// It does NOT prove the Angular components themselves — the shipped
// AbwabRelationshipsPageComponent / AbwabRelationshipsFacade are covered by
// abwab-relationships-page.component.spec.ts and abwab-relationships.facade.spec.ts, which drive the
// real component through the real reactive-forms value accessors. This markup deliberately mirrors
// the shipped template's structure (plain <ul>/<li>, native controls, no roving tabindex) so the two
// layers cannot describe different products. Synthetic Arabic only.
export const ABWAB_API_ORIGIN = 'http://localhost';

// Mirrors ABWAB_CONFLICT_MESSAGES for the codes this slice can raise; the page renders the message,
// never the code.
export const RELATIONSHIP_CONFLICT_MESSAGES: Record<string, string> = {
  'abwab.relationship_duplicate': 'توجد علاقة فعّالة بهذا النوع بين البابين بالفعل.',
  'abwab.relationship_cycle': 'لا يمكن إنشاء هذه العلاقة لأنها تُغلق دورة بين الأعم والأخص.',
  'abwab.manual_protection': 'هذا الباب محمي يدويًا من هذا الإجراء.',
};

export interface AbwabRelationshipsFixtureOptions {
  readonly categoryId?: string;
}

export function buildAbwabRelationshipsFixture(options: AbwabRelationshipsFixtureOptions = {}): string {
  const categoryId = options.categoryId ?? 'category-a';

  return `
    <main dir="rtl" lang="ar" style="font-family: system-ui, sans-serif;">
      <h1>علاقات الباب</h1>
      <p data-testid="relationships-conflict" role="alert" style="display:none;"></p>
      <div data-testid="selected-relationship-id"></div>
      <div data-testid="relationships-fetch-count" data-count="0"></div>
      <div data-testid="relationships-status">idle</div>
      <form data-testid="relationship-add-form">
        <label for="relationship-type">نوع العلاقة</label>
        <select id="relationship-type" data-testid="relationship-type">
          <option value="0">مشابه</option>
          <option value="1">مقابل</option>
          <option value="2">أعم / أخص</option>
        </select>
        <label for="relationship-direction">اتجاه العلاقة</label>
        <select id="relationship-direction" data-testid="relationship-direction">
          <option value="routed-is-broader">هذا الباب هو الأعم</option>
          <option value="routed-is-narrower">هذا الباب هو الأخص</option>
        </select>
        <label for="relationship-endpoint">الباب الآخر</label>
        <select id="relationship-endpoint" data-testid="relationship-endpoint">
          <option value="category-b">باب ب</option>
          <option value="category-z">باب ز</option>
        </select>
        <button type="button" data-testid="relationship-add">إضافة علاقة</button>
      </form>
      <ul data-testid="relationship-list" style="list-style:none; padding:0;"></ul>
    </main>
    <script>
      (function () {
        var API = '${ABWAB_API_ORIGIN}';
        var CATEGORY_ID = '${categoryId}';
        var MESSAGES = ${JSON.stringify(RELATIONSHIP_CONFLICT_MESSAGES)};
        var TYPE_LABELS = { 0: 'مشابه', 1: 'مقابل', 2: 'أعم / أخص' };

        var relationships = [];
        var generation = 1;
        var selectedId = null;
        var fetchCount = 0;

        function statusBox() { return document.querySelector('[data-testid=relationships-status]'); }
        function conflictBox() { return document.querySelector('[data-testid=relationships-conflict]'); }
        function typeSelect() { return document.querySelector('[data-testid=relationship-type]'); }
        function endpointSelect() { return document.querySelector('[data-testid=relationship-endpoint]'); }
        function directionSelect() { return document.querySelector('[data-testid=relationship-direction]'); }

        async function loadRelationships() {
          fetchCount++;
          document.querySelector('[data-testid=relationships-fetch-count]').setAttribute('data-count', String(fetchCount));
          statusBox().textContent = 'loading';
          var response = await fetch(API + '/api/abwab/relationships/' + CATEGORY_ID + '?includeDeleted=false');
          var body = await response.json();
          relationships = body.data.relationships;
          generation = body.data.expectedTimelineGeneration.generation;
          statusBox().textContent = relationships.length === 0 ? 'empty' : 'ready';
          render();
        }

        function render() {
          var list = document.querySelector('[data-testid=relationship-list]');
          list.setAttribute('data-total', String(relationships.length));
          var html = '';
          for (var i = 0; i < relationships.length; i++) {
            var row = relationships[i];
            var isSelected = row.categoryRelationshipId === selectedId;
            var fromLabel = row.isDirectional ? 'الأعم' : 'الطرف';
            var toLabel = row.isDirectional ? 'الأخص' : 'الطرف';
            html +=
              '<li data-testid="relationship-row" data-id="' + row.categoryRelationshipId + '"' +
              (isSelected ? ' style="background:#eef;"' : '') + '>' +
              '<span data-testid="relationship-type-label">' + TYPE_LABELS[row.relationshipType] + '</span> ' +
              '<span data-testid="relationship-from">' + fromLabel + ': ' + row.from.name + '</span> ' +
              '<span data-testid="relationship-to">' + toLabel + ': ' + row.to.name + '</span> ' +
              '<button type="button" data-testid="relationship-select" data-id="' + row.categoryRelationshipId + '">تحديد</button> ' +
              '<button type="button" data-testid="relationship-edit" data-id="' + row.categoryRelationshipId + '">تعديل</button> ' +
              '<button type="button" data-testid="relationship-delete" data-id="' + row.categoryRelationshipId + '">حذف</button>' +
              '</li>';
          }
          list.innerHTML = html;

          list.querySelectorAll('[data-testid=relationship-select]').forEach(function (button) {
            button.addEventListener('click', function () { select(button.getAttribute('data-id')); });
          });
          list.querySelectorAll('[data-testid=relationship-delete]').forEach(function (button) {
            button.addEventListener('click', function () {
              mutate('DELETE', '/api/abwab/relationships/' + button.getAttribute('data-id'), {
                expectedVersion: 1,
                expectedTimelineGeneration: generation,
              });
            });
          });
          list.querySelectorAll('[data-testid=relationship-edit]').forEach(function (button) {
            button.addEventListener('click', function () {
              mutate('PUT', '/api/abwab/relationships/' + button.getAttribute('data-id'), buildBody({ expectedVersion: 1 }));
            });
          });
        }

        // The reactive-forms SelectControlValueAccessor resolves the chosen option back to its bound
        // value; this hand-written fixture does the same conversion explicitly, because the wire
        // contract is an INTEGER RelationshipType (no JsonStringEnumConverter is registered).
        function buildBody(extra) {
          var type = Number(typeSelect().value);
          var other = endpointSelect().value;
          var routedIsBroader = type !== 2 || directionSelect().value === 'routed-is-broader';
          var body = {
            relationshipType: type,
            firstCategoryId: routedIsBroader ? CATEGORY_ID : other,
            secondCategoryId: routedIsBroader ? other : CATEGORY_ID,
            expectedTimelineGeneration: generation,
          };
          for (var key in extra) { body[key] = extra[key]; }
          return body;
        }

        function select(id) {
          selectedId = id;
          document.querySelector('[data-testid=selected-relationship-id]').textContent = id;
          render();
        }

        // The cache rule: EVERY outcome — success AND conflict — invalidates and reloads from the
        // server. Nothing is applied to the rendered list ahead of server confirmation, so the
        // "rollback" is simply this re-sync.
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
          }
          await loadRelationships();
        }

        document.querySelector('[data-testid=relationship-add]').addEventListener('click', function () {
          mutate('POST', '/api/abwab/relationships', buildBody({}));
        });

        window.__abwabRelationshipsFixture = {
          select: select,
          getFetchCount: function () { return fetchCount; },
        };

        loadRelationships();
      })();
    </script>
  `;
}
