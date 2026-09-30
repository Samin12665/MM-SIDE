// Drawings + exports helpers (eval in the signed-in Onshape tab; see helpers.js for the transport notes)
window.DW = '/d/495cc3804c007e87f5b21cee/w/d35a802f9506c7c27b1f7854';
window.R0 = { documentId: '495cc3804c007e87f5b21cee', workspaceId: 'd35a802f9506c7c27b1f7854' };
window.api = window.api || (async (m, p, body) => {
  const x = decodeURIComponent((document.cookie.match(/XSRF-TOKEN=([^;]+)/) || [])[1] || '');
  for (let k = 0; k < 6; k++) {
    const r = await fetch('/api/v6' + p, { method: m, headers: { 'Content-Type': 'application/json', Accept: 'application/json', 'X-XSRF-TOKEN': x }, body: body ? JSON.stringify(body) : undefined });
    if (r.status == 429) { await new Promise(q => setTimeout(q, 5000 * (k + 1))); continue; }
    const t = await r.text(); if (!r.ok) throw new Error(r.status + ' ' + p + ' ' + t.slice(0, 300)); return t ? JSON.parse(t) : {};
  }
});
window.b64 = (buf) => { const u = new Uint8Array(buf); let s = ''; for (let i = 0; i < u.length; i += 0x8000) s += String.fromCharCode.apply(null, u.subarray(i, i + 0x8000)); return btoa(s); };
window.blob = async (eid) => (await fetch('/api/v6/blobelements' + DW + '/e/' + eid, { headers: { Accept: 'application/octet-stream' } })).arrayBuffer();

// drawing JSON modify (async; polled)
window.drwMod = async (eid, reqs) => {
  const r = await api('POST', '/drawings' + DW + '/e/' + eid + '/modify', { description: 'ARV', jsonRequests: reqs });
  let t; for (let i = 0; i < 14; i++) { t = await api('GET', '/drawings/modify/status/' + r.id); if (t.requestState != 'ACTIVE') break; await new Promise(q => setTimeout(q, 2500)); }
  return t.output || t.requestState;
};
window.noteReq = (txt, x, y, h) => ({ messageName: 'onshapeCreateAnnotations', formatVersion: '2021-01-01', annotations: [{ type: 'Onshape::Note', note: { position: { type: 'Onshape::Reference::Point', coordinate: [x, y, 0] }, contents: txt, textHeight: h || 0.12 } }] });
// default sheet is portrait 8.5 x 11 in; view positions are sheet inches. NB the API spells it "denumerator".
window.LAY = [['top', 3.0, 8.4], ['front', 3.0, 5.9], ['right', 6.6, 5.9], ['isometric', 4.25, 2.9]];
window.mkDrw2 = async (name, ref, den, title) => {
  const d = await api('POST', '/drawings' + DW + '/create', { drawingName: name });
  const views = LAY.map(([o, x, y]) => ({ viewType: 'TopLevel', position: { x, y }, scale: { scaleSource: 'Custom', numerator: 1, denumerator: den }, orientation: o, reference: ref }));
  const o = await drwMod(d.id, [{ messageName: 'onshapeCreateViews', formatVersion: '2021-01-01', views }, noteReq('SIDE ARV-1 | ' + title + ' | SCALE 1:' + den + ' | mm | third-angle | V5', 0.5, 1.0, 0.12)]);
  return { id: d.id, ok: (o.match(/"OK"/g) || []).length };
};
window.drwPdf = async (eid, name) => {
  const r = await api('POST', '/drawings' + DW + '/e/' + eid + '/translations', { formatName: 'PDF', storeInDocument: true, destinationName: name });
  let t; for (let i = 0; i < 16; i++) { t = await api('GET', '/translations/' + r.id); if (t.requestState != 'ACTIVE') break; await new Promise(q => setTimeout(q, 2500)); }
  return (t.resultElementIds || [''])[0];
};
// [key, ref, scale, title, element name]; parts get partId filled in at run time
window.DSPEC = [
  ['complete', { elementId: '46143bc7201ce54007a8b716' }, 5, 'ASM_RC_CAR_COMPLETE', 'DRW_ASM_RC_CAR_COMPLETE'],
  ['front_steering', { elementId: 'dc6ee55ad18c7ddbbd3b8621' }, 3, 'ASM_FRONT_STEERING', 'DRW_ASM_FRONT_STEERING'],
  ['drivetrain', { elementId: 'd7ccfd92058d82bf992392a8' }, 3, 'ASM_DRIVETRAIN', 'DRW_ASM_DRIVETRAIN'],
  ['chassis', { elementId: '59d30dabe365e042256056d5' }, 4, 'ASM_CHASSIS', 'DRW_ASM_CHASSIS'],
  ['body', { elementId: 'e3c249cfffe47546a403d1d3' }, 5, 'ASM_BODY', 'DRW_ASM_BODY']];
window.PSPEC = [['10_CHASSIS', 'MAIN_CHASSIS', 4, 'PA-CF'], ['20_FRONT_STEERING', 'FRONT_BULKHEAD', 2, 'PA-CF'], ['20_FRONT_STEERING', 'FRONT_KNUCKLE_L', 1, 'PA-CF'], ['62_BODY_MAIN', 'BODY_MAIN_FRONT', 4, 'ASA']];
