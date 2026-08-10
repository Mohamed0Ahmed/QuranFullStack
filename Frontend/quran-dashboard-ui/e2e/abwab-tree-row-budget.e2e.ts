import { AbwabSandboxDoor, expect, test } from './fixtures/abwab';

/**
 * The tree row's name budget and depth-indent step, measured rather than asserted from a
 * remembered number. `UI_STYLE_SYSTEM.md`'s truncation entry carried a reasoned "~184px" for the
 * name beside all three badges; slice J's subgrid conversion touches exactly that layout, so the
 * number becomes a measurement taken on both sides of the change.
 *
 * Reports through `testInfo.annotations` so a run prints the table rather than only pass/fail.
 */
const VIEWPORTS = [1024, 1184, 1440];
const THEMES = ['light', 'dark'] as const;
const CHAIN_DEPTH = 7;

for (const width of VIEWPORTS) {
  for (const theme of THEMES) {
    test(`row budget at ${width}px in ${theme}`, async ({ page, abwabSandbox }, testInfo) => {
      await page.setViewportSize({ width, height: 900 });

      // A chain deep enough to characterise the budget as a function of depth: §17's "~184px"
      // was measured on some row, and the indent costs the name one step per level, so a single
      // depth-0 number would not be comparable to it.
      const chain: AbwabSandboxDoor[] = [];
      for (let depth = 0; depth < CHAIN_DEPTH; depth++) {
        chain.push(
          await abwabSandbox.createDoor({
            name: abwabSandbox.uniqueName(`budget-d${depth}`),
            parentId: depth === 0 ? null : chain[depth - 1].id,
          }),
        );
      }
      const root = chain[0];
      const deepest = chain[CHAIN_DEPTH - 1];

      await page.goto(`/abwab?section=${abwabSandbox.sectionId}`);
      await page.emulateMedia({ colorScheme: theme });

      for (const node of chain.slice(0, -1)) {
        await page.getByTestId(`abwab-tree-chevron-${node.id}`).click();
      }
      await expect(page.getByTestId(`abwab-tree-row-${deepest.id}`)).toBeVisible();

      const nameWidth = async (doorId: number): Promise<number> => {
        const row = page.getByTestId(`abwab-tree-row-${doorId}`);
        return row.locator('.abwab-tree__name').evaluate((el) => el.getBoundingClientRect().width);
      };

      // Direction-agnostic: in RTL the row's leading edge is its right edge, so the indent is
      // measured as the distance from the row's own start edge to the first control inside it.
      const indentOf = async (doorId: number): Promise<number> => {
        const row = page.getByTestId(`abwab-tree-row-${doorId}`);
        const chevron = page.getByTestId(`abwab-tree-chevron-${doorId}`);
        const [rowBox, chevronBox] = await Promise.all([row.boundingBox(), chevron.boundingBox()]);
        return rowBox!.x + rowBox!.width - (chevronBox!.x + chevronBox!.width);
      };

      const branchName = await nameWidth(root.id);
      const leafName = await nameWidth(deepest.id);
      const namesByDepth = [];
      const indents = [];
      for (const node of chain) {
        namesByDepth.push(Math.round(await nameWidth(node.id)));
        indents.push(Math.round(await indentOf(node.id)));
      }

      testInfo.annotations.push({
        type: 'row-budget',
        description: JSON.stringify({
          viewport: width,
          theme,
          branchNamePx: Math.round(branchName),
          deepestNamePx: Math.round(leafName),
          namesByDepth,
          indentPx: indents,
          indentStepPx: indents[1] - indents[0],
        }),
      });

      // The tree is RTL; a measurement taken in the wrong direction would silently report the
      // trailing gap as the indent.
      expect(await page.getByTestId('abwab-tree').evaluate((el) => getComputedStyle(el).direction)).toBe('rtl');
      expect(indents).toHaveLength(CHAIN_DEPTH);
      expect(branchName).toBeGreaterThan(0);
      expect(leafName).toBeGreaterThan(0);
    });
  }
}
