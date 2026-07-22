// Self-contained fixture for the bounded synthetic-tree spike (FR-022, §14.1). It builds a standalone HTML
// page (no app server, no Angular) that renders a windowed/virtualized scroller over `nodeCount` GENERIC
// synthetic nodes. A node is only { index, depth, label } computed procedurally in the browser — there is
// deliberately NO domain DTO here (nothing about sections/categories/ayah/gates); the spike measures raw
// render + scroll + virtualization behavior so later tree Kits inherit a perf baseline, not a data shape.

export interface SyntheticTreeFixtureOptions {
  readonly nodeCount: number;
  readonly rowHeight?: number;
  readonly viewportHeight?: number;
  readonly overscan?: number;
}

export interface SyntheticTreeFixture {
  readonly html: string;
  readonly nodeCount: number;
}

export function buildSyntheticTreeFixture(options: SyntheticTreeFixtureOptions): SyntheticTreeFixture {
  const nodeCount = options.nodeCount;
  const rowHeight = options.rowHeight ?? 28;
  const viewportHeight = options.viewportHeight ?? 400;
  const overscan = options.overscan ?? 8;

  const html = `
    <main dir="rtl" style="font-family: system-ui, sans-serif;">
      <div
        data-testid="tree-viewport"
        data-total="${nodeCount}"
        style="height:${viewportHeight}px; overflow:auto; position:relative; border:1px solid #ccc;"
      >
        <div data-testid="tree-spacer" style="height:${nodeCount * rowHeight}px; position:relative;"></div>
      </div>
      <script>
        (function () {
          var TOTAL = ${nodeCount};
          var ROW = ${rowHeight};
          var OVERSCAN = ${overscan};
          var BRANCHING = 6; // synthetic-only: gives rows a tree-like indentation, carries no meaning
          var viewport = document.querySelector('[data-testid=tree-viewport]');
          var spacer = document.querySelector('[data-testid=tree-spacer]');

          function depthOf(index) {
            return index === 0 ? 0 : Math.floor(Math.log(index * (BRANCHING - 1) + 1) / Math.log(BRANCHING));
          }

          function render() {
            var visible = Math.ceil(viewport.clientHeight / ROW);
            var start = Math.max(0, Math.floor(viewport.scrollTop / ROW) - OVERSCAN);
            var end = Math.min(TOTAL, start + visible + OVERSCAN * 2);

            var rows = '';
            for (var i = start; i < end; i++) {
              var depth = depthOf(i);
              rows +=
                '<div data-testid="tree-row" data-index="' + i + '"' +
                ' style="position:absolute; top:' + (i * ROW) + 'px; height:' + ROW + 'px;' +
                ' inset-inline-start:0; padding-inline-start:' + (depth * 16) + 'px;">' +
                'node ' + i + '</div>';
            }
            spacer.innerHTML = rows;
          }

          viewport.addEventListener('scroll', render, { passive: true });
          render();
        })();
      </script>
    </main>
  `;

  return { html, nodeCount };
}
