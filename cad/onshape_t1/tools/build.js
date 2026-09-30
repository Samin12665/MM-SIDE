// Concatenate src/*.fs (sorted) into ARV_LIB.fs; print length + FNV-1a hash
const fs = require('fs'), path = require('path');
const dir = path.join(__dirname, '..', 'src');
const files = fs.readdirSync(dir).filter(f => f.endsWith('.fs')).sort();
let s = files.map(f => fs.readFileSync(path.join(dir, f), 'utf8')).join('\n');
fs.writeFileSync(path.join(__dirname, '..', 'ARV_LIB.fs'), s);
let h = 2166136261; for (let i = 0; i < s.length; i++) { h ^= s.charCodeAt(i); h = Math.imul(h, 16777619) >>> 0; }
console.log(files.join(','), s.length, h.toString(16));
