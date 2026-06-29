-- Materialize the read-only candidate CTE into a session-local TEMP table so the
-- whole packet (checks + CSV + JSON) can be generated in one psql session.
-- TEMP tables live in pg_temp and are dropped at session end; no persistent data is modified.
CREATE TEMP TABLE seg_stem_candidates AS
