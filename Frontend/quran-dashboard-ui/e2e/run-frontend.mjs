import { createReadStream, readFileSync, statSync } from 'node:fs';
import { createServer } from 'node:https';
import { extname, resolve, sep } from 'node:path';

const buildRoot = requiredPath('E2E_FRONTEND_BUILD');
const certificate = requiredPath('E2E_TLS_CERTIFICATE');
const privateKey = requiredPath('E2E_TLS_PRIVATE_KEY');
const indexPath = resolve(buildRoot, 'index.html');
const contentTypes = new Map([
  ['.css', 'text/css; charset=utf-8'],
  ['.html', 'text/html; charset=utf-8'],
  ['.ico', 'image/x-icon'],
  ['.js', 'text/javascript; charset=utf-8'],
  ['.json', 'application/json; charset=utf-8'],
  ['.png', 'image/png'],
  ['.svg', 'image/svg+xml'],
  ['.woff', 'font/woff'],
  ['.woff2', 'font/woff2'],
]);

const server = createServer(
  {
    cert: readFileSync(certificate),
    key: readFileSync(privateKey),
  },
  (request, response) => {
    const requestUrl = new URL(request.url ?? '/', 'https://localhost');
    let pathname;
    try {
      pathname = decodeURIComponent(requestUrl.pathname);
    } catch {
      response.writeHead(400).end();
      return;
    }

    const candidate = resolve(buildRoot, `.${pathname}`);
    const path = candidate.startsWith(`${buildRoot}${sep}`) && isFile(candidate)
      ? candidate
      : indexPath;
    response.writeHead(200, {
      'cache-control': 'no-store',
      'content-type': contentTypes.get(extname(path)) ?? 'application/octet-stream',
      'x-content-type-options': 'nosniff',
    });
    createReadStream(path).pipe(response);
  },
);

server.listen(4200, '127.0.0.1');
process.once('SIGINT', stop);
process.once('SIGTERM', stop);

function stop() {
  server.closeAllConnections();
  server.close(() => process.exit(0));
}

function requiredPath(name) {
  const value = process.env[name]?.trim();
  if (!value) {
    throw new Error(`${name} is required for prebuilt E2E application startup.`);
  }
  return resolve(value);
}

function isFile(path) {
  try {
    return statSync(path).isFile();
  } catch {
    return false;
  }
}
