import { PermissionCatalogueItem } from '../../../core/api/generated/models/permission-catalogue-item';
import { PERMISSION_CODES, PermissionCode, isPermissionCode } from '../../../core/auth/permission-code';

export interface AccessPermissionGroup {
  readonly key: string;
  readonly label: string;
  readonly codes: readonly PermissionCode[];
  readonly labels: ReadonlyMap<PermissionCode, string>;
}

export function buildPermissionGroups(
  catalogue: readonly PermissionCatalogueItem[],
): readonly AccessPermissionGroup[] {
  const groups = new Map<
    string,
    {
      label: string;
      groupDisplayOrder: number;
      codes: { code: PermissionCode; label: string; displayOrder: number }[];
    }
  >();
  const seenCodes = new Set<PermissionCode>();

  for (const item of catalogue) {
    if (!isPermissionCode(item.code) || seenCodes.has(item.code)) {
      continue;
    }

    seenCodes.add(item.code);
    const group = groups.get(item.groupKey) ?? {
      label: item.groupLabel,
      groupDisplayOrder: item.groupDisplayOrder,
      codes: [],
    };
    group.codes.push({ code: item.code, label: item.arabicLabel, displayOrder: item.displayOrder });
    groups.set(item.groupKey, group);
  }

  return [...groups.entries()]
    .sort(([, left], [, right]) => left.groupDisplayOrder - right.groupDisplayOrder)
    .map(([key, group]) => {
      const codes = group.codes.sort((left, right) => left.displayOrder - right.displayOrder);
      return {
        key,
        label: group.label,
        codes: codes.map(({ code }) => code),
        labels: new Map(codes.map(({ code, label }) => [code, label])),
      };
    });
}

export function setGroupSelection(
  selected: ReadonlySet<string>,
  group: AccessPermissionGroup,
  isSelected: boolean,
): ReadonlySet<PermissionCode> {
  const next = knownSelectedCodes(selected);
  for (const code of group.codes) {
    if (isSelected) {
      next.add(code);
    } else {
      next.delete(code);
    }
  }
  return next;
}

export function setIndividualSelection(
  selected: ReadonlySet<string>,
  code: string,
  isSelected: boolean,
): ReadonlySet<PermissionCode> {
  const next = knownSelectedCodes(selected);
  if (!isPermissionCode(code)) {
    return next;
  }

  if (isSelected) {
    next.add(code);
  } else {
    next.delete(code);
  }
  return next;
}

export function permissionCodesForSubmission(selected: ReadonlySet<string>): PermissionCode[] {
  return PERMISSION_CODES.filter((code) => selected.has(code));
}

export function permissionLabelFor(
  groups: readonly AccessPermissionGroup[],
  code: PermissionCode,
): string {
  return groups.find((group) => group.codes.includes(code))?.labels.get(code) ?? code;
}

function knownSelectedCodes(selected: ReadonlySet<string>): Set<PermissionCode> {
  return new Set([...selected].filter(isPermissionCode));
}
