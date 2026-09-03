# Quran Dashboard

Shared language for the Quran Dashboard's trusted Quran corpus, system-defined catalogue, and user-authored working state.

## Data classes

**Canonical Quran Data**:
Source-traceable Quran content and its derived reference datasets, including phrase-search data. It is authoritative input that ordinary application and test activity must not change.
_Avoid_: Core data, seed data, fixture data

**System Catalogue**:
Application-defined reference entries that describe stable system capabilities, such as Permissions and built-in Roles. It is reconciled independently from user scenarios and is not disposable scenario state.
_Avoid_: Mutable data, test seed

**Mutable Application State**:
Disposable working data created by application activity, including identities, grants, sessions, audit history, Abwab, and Linking state. Tests may reset it without changing Canonical Quran Data or the System Catalogue.
_Avoid_: Core data, canonical data

**Schema State**:
The database structure and its migration history, including tables, constraints, sequences, and other database objects. Ordinary application tests observe it but do not redefine it or rewind sequence counters.
_Avoid_: Application state, seed data

**Destructive Rehearsal**:
A deliberately isolated exercise of behavior that must rewrite Canonical Quran Data, the System Catalogue, or Schema State, such as an import, catalogue reconciliation, migration, recovery, index build, or schema-drift scenario.
_Avoid_: Ordinary database test, mutable scenario

**Protected State**:
Canonical Quran Data, the System Catalogue, and Schema State considered together as the database state that ordinary tests must leave unchanged.
_Avoid_: Full database state, immutable fixture

**Test Database Capability**:
A persistent, explicitly enabled test database that provides the complete Quran corpus and verified safety boundaries required by database-backed tests. It is independently provisioned from repository-owned schema and canonical data pipelines; its Protected State changes only through explicit maintenance, while its Mutable Application State is disposable.
_Avoid_: Test fixture, ambient database

**Development Database**:
The developer-owned local database used for application development and authored work. Automated tests and Test Database provisioning neither read, reset, copy, nor mutate it.
_Avoid_: Test database, disposable database

**Canonical Data Pipeline**:
The supported ordered migrations, imports, rebuilds, and generation steps that independently produce complete Canonical Quran Data from repository-authorized sources.
_Avoid_: Database copy, fixture restore

**Rehearsal Database**:
An isolated non-authoritative database supplied for a Destructive Rehearsal. Its contents are disposable and never replace Canonical Quran Data as the application's source of truth.
_Avoid_: Canonical database, test fixture
