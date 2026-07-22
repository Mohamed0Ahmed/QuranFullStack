import { test, expect } from '@playwright/test';
import { expectRtl } from './rtl';
import { expectRole, expectAccessibleName } from './a11y';
import { expectFocusRestored } from './keyboard';
import { expectVirtualizedWindow } from './virtualization';
import { expectEscapeCloses, expectFocusReturnsToTrigger } from './dialog';

// Self-check for the reusable harness. It drives inline content (no app server) so the harness's own
// helpers are proven to work before later specs (029+) hang domain tests off them. This is scaffolding
// verification, not a domain test.
const FIXTURE = `
  <main id="shell" dir="rtl">
    <button data-testid="open" type="button">فتح الحوار</button>
    <div data-testid="dialog" role="dialog" aria-label="تأكيد الحذف" hidden>
      <button data-testid="close" type="button">إغلاق</button>
    </div>
    <ul data-testid="list" data-total="3000">
      <li>1</li><li>2</li><li>3</li><li>4</li><li>5</li>
      <li>6</li><li>7</li><li>8</li><li>9</li><li>10</li>
    </ul>
  </main>
  <script>
    const open = document.querySelector('[data-testid=open]');
    const dialog = document.querySelector('[data-testid=dialog]');
    const close = document.querySelector('[data-testid=close]');
    const closeDialog = () => { dialog.hidden = true; open.focus(); };
    open.addEventListener('click', () => { dialog.hidden = false; close.focus(); });
    close.addEventListener('click', closeDialog);
    document.addEventListener('keydown', (event) => {
      if (event.key === 'Escape' && !dialog.hidden) closeDialog();
    });
  </script>
`;

test.beforeEach(async ({ page }) => {
  await page.setContent(FIXTURE);
});

test('RTL + ARIA basics resolve', async ({ page }) => {
  await expectRtl(page.locator('#shell'));
  await expectRole(page.locator('[data-testid=open]'), 'button');
  await expectAccessibleName(page.locator('[data-testid=open]'), 'فتح الحوار');
});

test('keyboard focus is restored around a dialog interaction', async ({ page }) => {
  await page.locator('[data-testid=open]').focus();
  await expectFocusRestored(page, async () => {
    await page.locator('[data-testid=open]').press('Enter');
    await page.keyboard.press('Escape');
  });
});

test('critical dialog closes on Escape and returns focus to its trigger', async ({ page }) => {
  await page.locator('[data-testid=open]').click();
  await expectEscapeCloses(page, page.locator('[data-testid=dialog]'));

  await page.locator('[data-testid=open]').click();
  await expectFocusReturnsToTrigger(page, page.locator('[data-testid=open]'), async () => {
    await page.locator('[data-testid=close]').click();
  });
});

test('virtualized list renders a bounded window', async ({ page }) => {
  const list = page.locator('[data-testid=list]');
  const total = Number(await list.getAttribute('data-total'));
  await expectVirtualizedWindow(list.locator('li'), { renderedAtMost: 50, totalAtLeast: 2000, total });
});
