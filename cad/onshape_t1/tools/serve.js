// Local CORS file server so the Onshape tab can fetch the library/scripts from disk
const http = require('http'), fs = require('fs'), path = require('path');
const root = path.join(__dirname, '..');
http.createServer((req, res) => {
  const h = { 'Access-Control-Allow-Origin': '*', 'Access-Control-Allow-Private-Network': 'true', 'Access-Control-Allow-Headers': '*', 'Access-Control-Allow-Methods': 'GET,POST,OPTIONS' };
  if (req.method === 'OPTIONS') { res.writeHead(204, h); return res.end(); }
  if (req.method === 'POST') { let b = ''; req.on('data', d => b += d); req.on('end', () => { const f = path.join(root, 'out', decodeURIComponent(req.url.slice(1))); fs.mkdirSync(path.dirname(f), { recursive: true }); fs.writeFileSync(f, b); res.writeHead(200, h); res.end('ok'); }); return; }
  const f = path.join(root, decodeURIComponent(req.url.split('?')[0].slice(1)));
  fs.readFile(f, (e, d) => { if (e) { res.writeHead(404, h); return res.end('nf'); } res.writeHead(200, { ...h, 'Content-Type': 'text/plain; charset=utf-8' }); res.end(d); });
}).listen(8765, () => console.log('serving', root, 'on 8765'));
