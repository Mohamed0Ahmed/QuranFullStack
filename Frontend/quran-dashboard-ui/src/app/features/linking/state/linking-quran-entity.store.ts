import { Injectable } from '@angular/core';

import { LinkingResolvedAyahDto } from '../../../core/api/generated/models/linking-resolved-ayah-dto';
import {
  LinkingQuranAyahEntity,
  LinkingQuranWordEntity,
  linkingEntityKey,
} from '../models/linking-entities.models';

export class LinkingCanonicalDataConflictError extends Error {}

@Injectable({ providedIn: 'root' })
export class LinkingQuranEntityStore {
  private readonly ayahs = new Map<string, LinkingQuranAyahEntity>();
  private readonly words = new Map<string, LinkingQuranWordEntity>();
  private readonly referenceCounts = new Map<string, number>();
  private readonly entitiesByLease = new Map<string, readonly string[]>();

  insertPage(
    linkingDataRevision: number,
    ayahs: readonly LinkingResolvedAyahDto[],
    leaseId: string,
  ): void {
    if (this.entitiesByLease.has(leaseId)) {
      return;
    }
    const pageAyahs = new Map<string, LinkingQuranAyahEntity>();
    const pageWords = new Map<string, LinkingQuranWordEntity>();
    const entityKeys: string[] = [];
    for (const dto of ayahs) {
      const ayah: LinkingQuranAyahEntity = Object.freeze({
        id: dto.ayahId,
        verseKey: dto.verseKey,
        surahNumber: dto.surahNumber,
        surahNameArabic: dto.surahNameArabic,
        ayahNumber: dto.ayahNumber,
        pageFrom: dto.pageFrom,
        pageTo: dto.pageTo,
      });
      const ayahKey = this.ayahKey(linkingDataRevision, dto.ayahId);
      this.assertEntity(pageAyahs, ayahKey, ayah, sameAyah);
      pageAyahs.set(ayahKey, ayah);
      entityKeys.push(ayahKey);
      for (const wordDto of dto.words) {
        const word: LinkingQuranWordEntity = Object.freeze({
          id: wordDto.quranWordId,
          ayahId: dto.ayahId,
          wordNumber: wordDto.wordNumber,
          textUthmani: wordDto.textUthmani,
          isAyahMarker: wordDto.isAyahMarker,
        });
        const wordKey = this.wordKey(linkingDataRevision, wordDto.quranWordId);
        this.assertEntity(pageWords, wordKey, word, sameWord);
        pageWords.set(wordKey, word);
        entityKeys.push(wordKey);
      }
    }
    for (const [key, ayah] of pageAyahs) {
      this.assertEntity(this.ayahs, key, ayah, sameAyah);
    }
    for (const [key, word] of pageWords) {
      this.assertEntity(this.words, key, word, sameWord);
    }
    for (const [key, ayah] of pageAyahs) {
      this.ayahs.set(key, this.ayahs.get(key) ?? ayah);
    }
    for (const [key, word] of pageWords) {
      this.words.set(key, this.words.get(key) ?? word);
    }
    this.retainLease(leaseId, entityKeys);
  }

  retainPage(
    linkingDataRevision: number,
    ayahIds: readonly number[],
    wordIdsByAyahId: Readonly<Record<number, readonly number[]>>,
    leaseId: string,
  ): void {
    if (this.entitiesByLease.has(leaseId)) {
      return;
    }
    const keys: string[] = [];
    for (const ayahId of ayahIds) {
      const ayahKey = this.ayahKey(linkingDataRevision, ayahId);
      const ayah = this.ayahs.get(ayahKey);
      if (ayah === undefined) {
        throw new LinkingCanonicalDataConflictError('بيانات صفحة الربط غير مكتملة.');
      }
      const wordKeys = (wordIdsByAyahId[ayahId] ?? []).map((wordId) =>
        this.wordKey(linkingDataRevision, wordId));
      if (wordKeys.some((wordKey) => !this.words.has(wordKey))) {
        throw new LinkingCanonicalDataConflictError('بيانات صفحة الربط غير مكتملة.');
      }
      keys.push(ayahKey, ...wordKeys);
    }
    this.retainLease(leaseId, keys);
  }

  release(leaseId: string): void {
    const keys = this.entitiesByLease.get(leaseId);
    if (keys === undefined) {
      return;
    }
    this.entitiesByLease.delete(leaseId);
    for (const key of keys) {
      const next = (this.referenceCounts.get(key) ?? 1) - 1;
      if (next > 0) {
        this.referenceCounts.set(key, next);
        continue;
      }
      this.referenceCounts.delete(key);
      key.startsWith('a:') ? this.ayahs.delete(key) : this.words.delete(key);
    }
  }

  ayah(linkingDataRevision: number, ayahId: number): LinkingQuranAyahEntity | null {
    return this.ayahs.get(this.ayahKey(linkingDataRevision, ayahId)) ?? null;
  }

  word(linkingDataRevision: number, wordId: number): LinkingQuranWordEntity | null {
    return this.words.get(this.wordKey(linkingDataRevision, wordId)) ?? null;
  }

  private retainLease(leaseId: string, keys: readonly string[]): void {
    const uniqueKeys = [...new Set(keys)];
    this.entitiesByLease.set(leaseId, uniqueKeys);
    for (const key of uniqueKeys) {
      this.referenceCounts.set(key, (this.referenceCounts.get(key) ?? 0) + 1);
    }
  }

  private assertEntity<T>(
    entities: Map<string, T>,
    key: string,
    incoming: T,
    equals: (left: T, right: T) => boolean,
  ): void {
    const existing = entities.get(key);
    if (existing !== undefined && !equals(existing, incoming)) {
      throw new LinkingCanonicalDataConflictError('تعارضت بيانات القرآن لنفس مراجعة الربط.');
    }
  }

  private ayahKey(revision: number, ayahId: number): string {
    return `a:${linkingEntityKey(revision, ayahId)}`;
  }

  private wordKey(revision: number, wordId: number): string {
    return `w:${linkingEntityKey(revision, wordId)}`;
  }
}

function sameAyah(left: LinkingQuranAyahEntity, right: LinkingQuranAyahEntity): boolean {
  return (
    left.id === right.id &&
    left.verseKey === right.verseKey &&
    left.surahNumber === right.surahNumber &&
    left.surahNameArabic === right.surahNameArabic &&
    left.ayahNumber === right.ayahNumber &&
    left.pageFrom === right.pageFrom &&
    left.pageTo === right.pageTo
  );
}

function sameWord(left: LinkingQuranWordEntity, right: LinkingQuranWordEntity): boolean {
  return (
    left.id === right.id &&
    left.ayahId === right.ayahId &&
    left.wordNumber === right.wordNumber &&
    left.textUthmani === right.textUthmani &&
    left.isAyahMarker === right.isAyahMarker
  );
}
