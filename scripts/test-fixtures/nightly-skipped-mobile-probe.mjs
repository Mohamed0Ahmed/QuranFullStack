import { writeControlledBrowserEvidence } from './write-controlled-browser-evidence.mjs';

writeControlledBrowserEvidence(process.env.QDB_PR_OBSERVATION_RESULT_DIR, {
  mobileStatus: 'skipped',
});
