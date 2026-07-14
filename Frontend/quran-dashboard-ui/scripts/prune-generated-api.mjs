// Keeps the generated API output models-only: this project consumes payload DTO
// interfaces exclusively (locked decision — generated services are never imported),
// so everything except models/ and its index is removed after each generation.
// Runs inside `npm run generate:api` to keep check-api-contract deterministic.
import { readdirSync, rmSync } from 'node:fs';

const outputDir = 'src/app/core/api/generated';
const keep = new Set(['models', 'models.ts']);

const removed = [];
for (const entry of readdirSync(outputDir)) {
  if (!keep.has(entry)) {
    rmSync(`${outputDir}/${entry}`, { recursive: true, force: true });
    removed.push(entry);
  }
}
console.log(`Pruned generated API output to models-only (removed: ${removed.join(', ') || 'nothing'})`);
