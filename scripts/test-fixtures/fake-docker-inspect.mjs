#!/usr/bin/env node
import { appendFileSync } from 'node:fs';

const arguments_ = process.argv.slice(2);
if (process.env.QDB_FAKE_DOCKER_LOG) appendFileSync(process.env.QDB_FAKE_DOCKER_LOG, `${arguments_.join(' ')}\n`);
if (arguments_[0] === 'info') process.exit(0);
if (arguments_[0] === 'rm' || (arguments_[0] === 'network' && arguments_[1] === 'rm')) process.exit(0);
if (arguments_[0] === 'container' && arguments_[1] === 'inspect') {
  process.stderr.write(`Error response from daemon: No such container: ${arguments_[2]}\n`);
  process.exit(1);
}
if (arguments_[0] === 'network' && arguments_[1] === 'inspect') {
  process.stderr.write(`Error response from daemon: network ${arguments_[2]} not found\n`);
  process.exit(1);
}
process.exit(97);
