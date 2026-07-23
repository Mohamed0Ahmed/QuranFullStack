import { describe, expect, it } from 'vitest';
import { TestBed } from '@angular/core/testing';

import { CategorySnapshotDto } from '../../../core/api/generated/models';
import { CategoryEditorComponent, CategoryEditorSaveEvent, ExistingAliasEditEvent, ExistingAliasRemoveEvent } from './category-editor.component';

function category(overrides: Partial<CategorySnapshotDto> = {}): CategorySnapshotDto {
  return {
    categoryId: 'cat-1',
    name: 'باب الطهارة',
    normalizedName: 'باب الطهارة',
    description: 'وصف الباب',
    representativeQuranExcerpt: null,
    parentCategoryId: null,
    sectionId: 'section-1',
    siblingOrder: null,
    sectionOrder: 0,
    globalOrder: 0,
    ancestorIds: [],
    depth: 0,
    categoryContentRevision: 1,
    version: 3,
    aliases: [],
    protection: { hasEffectiveManualProtection: false, isActionBlocked: false, manualProtections: [] },
    ...overrides,
  };
}

function render(canEdit = true, categoryValue: CategorySnapshotDto = category()) {
  const fixture = TestBed.createComponent(CategoryEditorComponent);
  fixture.componentRef.setInput('category', categoryValue);
  fixture.componentRef.setInput('canEdit', canEdit);
  fixture.detectChanges();
  return fixture;
}

describe('CategoryEditorComponent', () => {
  it('patches the form from the current category on open (no lock call, no emission)', () => {
    const fixture = render();
    let saveCount = 0;
    fixture.componentInstance.save.subscribe(() => (saveCount += 1));

    expect(fixture.componentInstance.form.controls.name.value).toBe('باب الطهارة');
    expect(fixture.componentInstance.form.controls.description.value).toBe('وصف الباب');
    expect(saveCount).toBe(0);
  });

  it('re-patches (without locking or emitting) when the selected category input changes', () => {
    const fixture = render();
    let saveCount = 0;
    fixture.componentInstance.save.subscribe(() => (saveCount += 1));

    fixture.componentRef.setInput('category', category({ categoryId: 'cat-2', name: 'باب الصلاة', description: null }));
    fixture.detectChanges();

    expect(fixture.componentInstance.form.controls.name.value).toBe('باب الصلاة');
    expect(fixture.componentInstance.form.controls.description.value).toBe('');
    expect(saveCount).toBe(0);
  });

  it('explicit save emits the trimmed field values, converting blank optional fields to null', () => {
    const fixture = render();
    let emitted: CategoryEditorSaveEvent | undefined;
    fixture.componentInstance.save.subscribe((event) => (emitted = event));

    fixture.componentInstance.form.patchValue({ name: '  باب معدل  ', description: '   ', representativeQuranExcerpt: 'مقتطف' });
    fixture.componentInstance.submit();

    expect(emitted).toEqual({
      name: 'باب معدل',
      description: null,
      representativeQuranExcerpt: 'مقتطف',
      newAliases: [],
    });
  });

  it('does not emit save when the required name field is empty', () => {
    const fixture = render();
    let saveCount = 0;
    fixture.componentInstance.save.subscribe(() => (saveCount += 1));

    fixture.componentInstance.form.patchValue({ name: '' });
    fixture.componentInstance.submit();

    expect(saveCount).toBe(0);
    expect(fixture.componentInstance.form.controls.name.touched).toBe(true);
  });

  it('adding and removing alias fields updates the newAliases form array, and only non-empty values are submitted', () => {
    const fixture = render();
    const instance = fixture.componentInstance;
    let emitted: CategoryEditorSaveEvent | undefined;
    instance.save.subscribe((event) => (emitted = event));

    instance.addAliasField();
    instance.addAliasField();
    expect(instance.newAliasesArray.length).toBe(2);

    instance.newAliasesArray.at(0).setValue('اسم بديل');
    instance.newAliasesArray.at(1).setValue('   ');
    instance.submit();

    expect(emitted?.newAliases).toEqual(['اسم بديل']);

    instance.removeAliasField(1);
    expect(instance.newAliasesArray.length).toBe(1);
    instance.removeAliasField(0);
    expect(instance.newAliasesArray.length).toBe(0);
  });

  it('lists existing aliases and edits one using the ALIAS OWN Version as expectedVersion (blocker fix, no manufactured value)', () => {
    const withAlias = category({ aliases: [{ categorySearchAliasId: 'alias-1', value: 'اسم قديم', version: 7 }] });
    const fixture = render(true, withAlias);
    let emitted: ExistingAliasEditEvent | undefined;
    fixture.componentInstance.editAlias.subscribe((event) => (emitted = event));

    const root = fixture.nativeElement as HTMLElement;
    const input = root.querySelector<HTMLInputElement>('[data-testid=category-editor-existing-alias-input]')!;
    input.value = 'اسم جديد';
    input.dispatchEvent(new Event('input'));
    root.querySelector<HTMLButtonElement>('[data-testid=category-editor-existing-alias-save]')!.click();

    expect(emitted).toEqual({ aliasId: 'alias-1', value: 'اسم جديد', expectedVersion: 7 });
  });

  it('removes an existing alias using its OWN Version as expectedVersion', () => {
    const withAlias = category({ aliases: [{ categorySearchAliasId: 'alias-1', value: 'اسم', version: 4 }] });
    const fixture = render(true, withAlias);
    let emitted: ExistingAliasRemoveEvent | undefined;
    fixture.componentInstance.removeAlias.subscribe((event) => (emitted = event));

    (fixture.nativeElement as HTMLElement).querySelector<HTMLButtonElement>('[data-testid=category-editor-existing-alias-remove]')!.click();

    expect(emitted).toEqual({ aliasId: 'alias-1', expectedVersion: 4 });
  });

  it('renders read-only fields and no save/alias controls when canEdit is false (non-authoritative hiding)', () => {
    const fixture = render(false);
    const root = fixture.nativeElement as HTMLElement;

    expect(root.querySelector('[data-testid=category-editor-save]')).toBeNull();
    expect(root.querySelector('[data-testid=category-editor-aliases]')).toBeNull();
    expect(root.querySelector<HTMLInputElement>('[data-testid=category-editor-name]')!.readOnly).toBe(true);
  });
});
