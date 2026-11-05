const fs = require('fs');
const A = 'C:/Users/bestv/Aquarium';
let html = fs.readFileSync(A + '/index.html', 'utf8');

const ids = ['bg', 'w0', 'w1', 'w2', 'w3', 'w4', 'w5', 'w6', 'w7', 'w8'];
const map = {};
let bytes = 0;
for (const id of ids) {
  const p = id === 'bg' ? A + '/sprites/bg_forest.jpg' : A + '/sprites/' + id + '.png';
  const b = fs.readFileSync(p);
  bytes += b.length;
  map[id] = 'data:' + (id === 'bg' ? 'image/jpeg' : 'image/png') + ';base64,' + b.toString('base64');
}

const anchor = "loadSprite('bg', 'sprites/bg_forest.jpg');";
if (html.split(anchor).length - 1 !== 1) { console.log('MISS anchor'); process.exit(1); }
html = html.replace(anchor, 'var EMBED = ' + JSON.stringify(map) + ';\n' + anchor);

const srcOld = '  img.src = url;';
if (html.split(srcOld).length - 1 !== 1) { console.log('MISS img.src'); process.exit(1); }
html = html.replace(srcOld, "  img.src = (typeof EMBED !== 'undefined' && EMBED[id]) ? EMBED[id] : url;");

html = html.replace('<title>虚拟鱼缸</title>', '<title>虚拟鱼缸 · 单文件版</title>');
fs.writeFileSync(A + '/aquarium-standalone.html', html);
console.log('assets bytes=' + bytes + ' html MB=' + (fs.statSync(A + '/aquarium-standalone.html').size / 1048576).toFixed(2));
