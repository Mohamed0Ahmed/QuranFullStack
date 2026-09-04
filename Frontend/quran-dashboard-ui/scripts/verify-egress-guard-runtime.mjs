import assert from 'node:assert/strict';
import { createConnection, createServer } from 'node:net';

const server = createServer((socket) => socket.end('ok'));
await new Promise((resolvePromise, rejectPromise) => {
  server.once('error', rejectPromise);
  server.listen(0, '127.0.0.1', resolvePromise);
});

try {
  const address = server.address();
  assert(address && typeof address !== 'string');
  await connect('127.0.0.1', address.port);
  await connect('::ffff:127.0.0.1', address.port);

  await assert.rejects(
    connect('192.0.2.1', 443),
    (error) => error?.code === 'EPERM',
    'the controlled egress guard must reject external IPv4 connections with EPERM',
  );
  await assert.rejects(
    connect('::ffff:192.0.2.1', 443),
    (error) => error?.code === 'EPERM',
    'the controlled egress guard must reject external IPv4-mapped connections with EPERM',
  );
} finally {
  await new Promise((resolvePromise) => server.close(resolvePromise));
}

console.log('Controlled E2E egress guard allows native/mapped loopback and rejects external IPv4.');

function connect(host, port) {
  return new Promise((resolvePromise, rejectPromise) => {
    const socket = createConnection({ host, port });
    const timeout = setTimeout(() => {
      socket.destroy();
      rejectPromise(new Error(`connection to ${host}:${port} did not fail closed`));
    }, 2000);
    socket.once('connect', () => {
      clearTimeout(timeout);
      socket.end();
      resolvePromise();
    });
    socket.once('error', (error) => {
      clearTimeout(timeout);
      rejectPromise(error);
    });
  });
}
