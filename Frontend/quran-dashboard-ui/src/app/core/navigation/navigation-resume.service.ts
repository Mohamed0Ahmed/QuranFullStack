import { isPlatformBrowser } from '@angular/common';
import { DestroyRef, Injectable, PLATFORM_ID, inject } from '@angular/core';
import { NavigationEnd, Router, UrlTree } from '@angular/router';
import { filter } from 'rxjs';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

import { DETAIL_OVERLAY_QUERY_KEYS } from './detail-overlay/detail-overlay.models';
import { NAV_MENU } from './nav-menu';
import { NavItem } from './nav-items';

const STORAGE_KEY = 'qd.navigation.resume.v1';
const STORAGE_VERSION = 1;
const MAX_TARGETS = 24;
const MAX_URL_LENGTH = 2048;
const TRANSIENT_QUERY_KEYS = new Set<string>([
  ...Object.values(DETAIL_OVERLAY_QUERY_KEYS),
  'modal',
  'focusAyah',
]);

interface NavRegistration {
  readonly item: NavItem;
  readonly parentKeys: readonly string[];
  readonly matchPath: string;
  readonly depth: number;
}

interface StoredNavigationResumeState {
  readonly v: number;
  readonly u: Record<string, string>;
}

@Injectable({ providedIn: 'root' })
export class NavigationResumeService {
  private readonly router = inject(Router);
  private readonly platformId = inject(PLATFORM_ID);
  private readonly destroyRef = inject(DestroyRef);
  private readonly registrations = buildRegistrations(NAV_MENU);
  private readonly registrationKeys = new Set(this.registrations.map((registration) => registration.item.key));
  private readonly urls = new Map<string, string>();

  constructor() {
    this.load();
    this.router.events
      .pipe(
        filter((event): event is NavigationEnd => event instanceof NavigationEnd),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe((event) => this.remember(event.urlAfterRedirects));

    if (this.router.navigated) {
      this.remember(this.router.url);
    }
  }

  targetFor(item: NavItem): UrlTree {
    const storedUrl = this.urls.get(item.key);
    if (storedUrl !== undefined) {
      const storedTarget = this.parseStoredTarget(item.key, storedUrl);
      if (storedTarget !== null) {
        return storedTarget;
      }
      this.urls.delete(item.key);
      this.persist();
    }

    return this.router.createUrlTree([item.route], {
      queryParams: item.queryParams ?? null,
    });
  }

  private remember(url: string): void {
    const canonicalUrl = this.canonicalize(url);
    if (canonicalUrl === null || canonicalUrl.length > MAX_URL_LENGTH) {
      return;
    }

    const tree = this.router.parseUrl(canonicalUrl);
    const keys = this.matchingKeys(tree);
    if (keys.size === 0) {
      return;
    }

    for (const key of keys) {
      this.urls.delete(key);
      this.urls.set(key, canonicalUrl);
    }
    while (this.urls.size > MAX_TARGETS) {
      const oldestKey = this.urls.keys().next().value as string | undefined;
      if (oldestKey === undefined) {
        break;
      }
      this.urls.delete(oldestKey);
    }
    this.persist();
  }

  private canonicalize(url: string): string | null {
    try {
      const tree = this.router.parseUrl(url);
      const queryParams = Object.fromEntries(
        Object.entries(tree.queryParams).filter(([key]) => !TRANSIENT_QUERY_KEYS.has(key)),
      );
      return this.router.serializeUrl(new UrlTree(tree.root, queryParams, tree.fragment));
    } catch {
      return null;
    }
  }

  private parseStoredTarget(key: string, url: string): UrlTree | null {
    if (url.length === 0 || url.length > MAX_URL_LENGTH) {
      return null;
    }
    try {
      const tree = this.router.parseUrl(url);
      return this.matchingKeys(tree).has(key) ? tree : null;
    } catch {
      return null;
    }
  }

  private matchingKeys(tree: UrlTree): Set<string> {
    const currentPath = primaryPath(tree);
    const pathMatches = this.registrations.filter((registration) =>
      matchesPath(currentPath, registration.matchPath),
    );
    if (pathMatches.length === 0) {
      return new Set();
    }

    const deepest = Math.max(...pathMatches.map((registration) => registration.depth));
    const deepestMatches = pathMatches.filter((registration) => registration.depth === deepest);
    const siblingGroups = groupByParent(deepestMatches);
    const selected: NavRegistration[] = [];

    for (const siblings of siblingGroups.values()) {
      const constrained = siblings.filter(
        (registration) =>
          registration.item.queryParams !== undefined &&
          queryConstraintsMatch(tree, registration.item.queryParams),
      );
      selected.push(
        ...(constrained.length > 0
          ? constrained
          : siblings.filter((registration) => registration.item.queryParams === undefined)),
      );
    }

    const keys = new Set<string>();
    for (const registration of selected) {
      keys.add(registration.item.key);
      registration.parentKeys.forEach((key) => keys.add(key));
    }
    return keys;
  }

  private load(): void {
    if (!isPlatformBrowser(this.platformId)) {
      return;
    }
    try {
      const raw = sessionStorage.getItem(STORAGE_KEY);
      if (raw === null) {
        return;
      }
      const parsed = JSON.parse(raw) as Partial<StoredNavigationResumeState>;
      if (parsed.v !== STORAGE_VERSION || parsed.u === null || typeof parsed.u !== 'object') {
        return;
      }
      for (const [key, url] of Object.entries(parsed.u).slice(-MAX_TARGETS)) {
        if (!this.registrationKeys.has(key) || typeof url !== 'string') {
          continue;
        }
        const target = this.parseStoredTarget(key, url);
        if (target !== null) {
          this.urls.set(key, this.router.serializeUrl(target));
        }
      }
    } catch {
      this.urls.clear();
    }
  }

  private persist(): void {
    if (!isPlatformBrowser(this.platformId)) {
      return;
    }
    const state: StoredNavigationResumeState = {
      v: STORAGE_VERSION,
      u: Object.fromEntries(this.urls),
    };
    try {
      sessionStorage.setItem(STORAGE_KEY, JSON.stringify(state));
    } catch {
      return;
    }
  }
}

function buildRegistrations(
  items: readonly NavItem[],
  parentKeys: readonly string[] = [],
): NavRegistration[] {
  return items.flatMap((item) => {
    const registration: NavRegistration = {
      item,
      parentKeys,
      matchPath: normalizePath(item.resumePath ?? item.route),
      depth: pathDepth(item.resumePath ?? item.route),
    };
    return [registration, ...buildRegistrations(item.children ?? [], [...parentKeys, item.key])];
  });
}

function normalizePath(path: string): string {
  const normalized = path.split(/[?#]/, 1)[0].replace(/\/$/, '');
  return normalized === '' ? '/' : normalized;
}

function pathDepth(path: string): number {
  return normalizePath(path).split('/').filter(Boolean).length;
}

function primaryPath(tree: UrlTree): string {
  const segments = tree.root.children['primary']?.segments.map((segment) => segment.path) ?? [];
  return segments.length === 0 ? '/' : `/${segments.join('/')}`;
}

function matchesPath(currentPath: string, matchPath: string): boolean {
  return currentPath === matchPath || currentPath.startsWith(`${matchPath}/`);
}

function queryConstraintsMatch(tree: UrlTree, constraints: Record<string, string>): boolean {
  return Object.entries(constraints).every(([key, value]) => tree.queryParamMap.get(key) === value);
}

function groupByParent(registrations: readonly NavRegistration[]): Map<string, NavRegistration[]> {
  const groups = new Map<string, NavRegistration[]>();
  for (const registration of registrations) {
    const parentKey = registration.parentKeys.at(-1) ?? '';
    const siblings = groups.get(parentKey) ?? [];
    siblings.push(registration);
    groups.set(parentKey, siblings);
  }
  return groups;
}
