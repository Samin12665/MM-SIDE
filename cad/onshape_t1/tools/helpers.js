// Onshape-tab helpers (evaluated inside the signed-in cad.onshape.com tab).
// Carried from disk via window.name by carry.html. All calls go through the user's own session.
window.D = '495cc3804c007e87f5b21cee';
window.W = 'd35a802f9506c7c27b1f7854';
window.DW = '/d/' + D + '/w/' + W;
window.FSE = 'ba15feb590e6c7aab0d78d2b';

window.api = async (m, u, b) => {
  const x = decodeURIComponent((document.cookie.match(/XSRF-TOKEN=([^;]+)/) || [])[1] || '');
  let r;
  for (let k = 0; k < 8; k++) {
    r = await fetch('/api/v6' + u, { method: m, headers: { 'Content-Type': 'application/json', 'Accept': 'application/json', 'X-XSRF-TOKEN': x }, body: b ? JSON.stringify(b) : undefined });
    if (r.status !== 429) break;
    const ra = parseFloat(r.headers.get('Retry-After') || '0');
    await new Promise(q => setTimeout(q, Math.max(ra * 1000, 2000 * (k + 1))));
  }
  const t = await r.text();
  let j; try { j = JSON.parse(t); } catch (e) { j = t; }
  if (!r.ok) throw new Error(r.status + ' ' + u + ' ' + t.slice(0, 1500));
  return j;
};

window.fnv = (s) => { let h = 2166136261; for (let i = 0; i < s.length; i++) { h ^= s.charCodeAt(i); h = Math.imul(h, 16777619) >>> 0; } return h.toString(16); };

window.els = async () => api('GET', '/documents' + DW + '/elements');
window.elId = async (name) => { const e = (await els()).find(x => x.name === name); return e ? e.id : null; };

// write the Feature Studio and return its namespace + notices
window.setFS = async (contents) => {
  const c = await api('GET', '/featurestudios' + DW + '/e/' + FSE);
  const r = await api('POST', '/featurestudios' + DW + '/e/' + FSE, { contents, serializationVersion: c.serializationVersion, sourceMicroversion: c.sourceMicroversion, rejectMicroversionSkew: false });
  const sp = await api('GET', '/featurestudios' + DW + '/e/' + FSE + '/featurespecs');
  window.NS = sp.featureSpecs.length ? sp.featureSpecs[0].namespace : null;
  return { ns: window.NS, specs: sp.featureSpecs.map(s => s.featureType), notices: (r.notices || []).map(n => (n.message && n.message.message) || JSON.stringify(n)).slice(0, 20) };
};

window.specNs = async () => { const sp = await api('GET', '/featurestudios' + DW + '/e/' + FSE + '/featurespecs'); window.NS = sp.featureSpecs[0].namespace; return window.NS; };

window.feats = async (eid) => api('GET', '/partstudios' + DW + '/e/' + eid + '/features');
window.addFeat = async (eid, feature) => api('POST', '/partstudios' + DW + '/e/' + eid + '/features', { feature });
window.updFeat = async (eid, feature) => api('POST', '/partstudios' + DW + '/e/' + eid + '/features/featureid/' + feature.featureId, { feature });
window.delFeat = async (eid, fid) => api('DELETE', '/partstudios' + DW + '/e/' + eid + '/features/featureid/' + fid);
window.elMv = async (eid) => (await api('GET', '/documents' + DW + '/elements?elementId=' + eid))[0].microversionId;

window.pEnum = (id, enumName, value) => ({ btType: 'BTMParameterEnum-145', parameterId: id, enumName, value, namespace: window.NS });
window.pBool = (id, value) => ({ btType: 'BTMParameterBoolean-144', parameterId: id, value });
window.pQty = (id, expression) => ({ btType: 'BTMParameterQuantity-147', parameterId: id, expression, isInteger: false });
window.pStr = (id, value) => ({ btType: 'BTMParameterString-149', parameterId: id, value });

window.custom = (featureType, name, parameters) => ({ btType: 'BTMFeature-134', featureType, name, namespace: window.NS, parameters });

window.derivedFeature = (name, srcEid, srcMv, queryString) => ({ btType: 'BTMFeature-134', featureType: 'importDerived', name, namespace: '', parameters: [
  { btType: 'BTMParameterBoolean-144', value: true, parameterId: 'newUI' },
  { btType: 'BTMParameterReferencePartStudio-3302', parameterId: 'partStudio', namespace: 'e' + srcEid + '::m' + srcMv, partIdentity: { btType: 'BTPSOIdentity-2741', theId: '' }, configuration: [],
    partQuery: { btType: 'BTMParameterQueryList-148', parameterId: 'partQuery', queries: [{ btType: 'BTMIndividualQuery-138', queryString }] } },
  { btType: 'BTMParameterEnum-145', enumName: 'DerivedPlacementType', value: 'AT_ORIGIN', parameterId: 'placement' },
  { btType: 'BTMParameterBoolean-144', value: true, parameterId: 'includeMateConnectors' },
  { btType: 'BTMParameterBoolean-144', value: true, parameterId: 'includeProperties' },
  { btType: 'BTMParameterDerived-864', parameterId: 'buildFunction', namespace: '', imports: [], configuration: [] }
] });

// feature states summary for a part studio
window.states = async (eid) => { const f = await feats(eid); const out = []; for (const x of f.features) { const s = f.featureStates[x.featureId]; out.push(x.name + ':' + (s ? s.featureStatus : '?')); } return out.join(' | '); };

// evaluate a FeatureScript lambda in a part studio; returns plain JS value
window.fsv = (v) => { if (v == null) return v; const t = v.btType || ''; if (t.endsWith('BTFSValueArray')) return v.value.map(fsv); if (t.endsWith('BTFSValueMap')) { const o = {}; for (const e of v.value) o[fsv(e.key)] = fsv(e.value); return o; } if (v.value !== undefined) return v.value; return v; };
window.evalFS = async (eid, script) => { const r = await api('POST', '/partstudios' + DW + '/e/' + eid + '/featurescript', { script }); return { result: fsv(r.result), notices: (r.notices || []).map(n => (n.message && n.message.message) || '').filter(x => x).slice(0, 10) }; };

// notices of a feature (error/warning text) via the feature-level notices endpoint is not public; use this FS probe instead
window.parts = async (eid) => (await api('GET', '/parts' + DW + '/e/' + eid)).map(p => p.name + (p.partNumber ? ' [' + p.partNumber + ']' : ''));

// shaded image into an overlay (for screenshots)
window.shade = async (kind, eid, view, h = 700, w = 1000, extra = '') => {
  const base = kind === 'a' ? '/assemblies' : '/partstudios';
  const vm = view || 'isometric';
  const r = await api('GET', base + DW + '/e/' + eid + '/shadedviews?viewMatrix=' + encodeURIComponent(vm) + '&outputHeight=' + h + '&outputWidth=' + w + '&pixelSize=0&edges=show' + extra);
  let ov = document.getElementById('arvOverlay');
  if (!ov) { ov = document.createElement('div'); ov.id = 'arvOverlay'; ov.style.cssText = 'position:fixed;left:0;top:0;right:0;bottom:0;background:#fff;z-index:2147483647;overflow:auto'; document.body.appendChild(ov); }
  ov.innerHTML = '';
  for (const im of r.images) { const i = document.createElement('img'); i.src = 'data:image/png;base64,' + im; i.style.cssText = 'display:block;margin:auto;max-width:100%;max-height:100%'; ov.appendChild(i); }
  return r.images.length;
};
window.unshade = () => { const ov = document.getElementById('arvOverlay'); if (ov) ov.remove(); };

// view matrices (3x4 row-major, flattened) — camera looking from direction
window.VIEWS = {
  iso: 'isometric',
  front: 'front', top: 'top', right: 'right', left: 'left', back: 'back', bottom: 'bottom'
};

// build a 12-number view matrix from azimuth/elevation (deg): Onshape shadedviews accepts the 12-number string
window.viewAE = (az, el) => {
  const a = az * Math.PI / 180, e = el * Math.PI / 180;
  // camera direction (from target to eye)
  const d = [Math.cos(e) * Math.cos(a), Math.cos(e) * Math.sin(a), Math.sin(e)];
  const up0 = [0, 0, 1];
  // x axis of screen = up0 x d normalized; y = d x x
  let x = [up0[1] * d[2] - up0[2] * d[1], up0[2] * d[0] - up0[0] * d[2], up0[0] * d[1] - up0[1] * d[0]];
  const n = Math.hypot(...x); x = x.map(q => q / n);
  const y = [d[1] * x[2] - d[2] * x[1], d[2] * x[0] - d[0] * x[2], d[0] * x[1] - d[1] * x[0]];
  return [x[0], x[1], x[2], 0, y[0], y[1], y[2], 0, d[0], d[1], d[2], 0].map(q => q.toFixed(5)).join(',');
};
// point every custom feature in every part studio at the current Feature Studio microversion
window.refreshNs = async () => {
  const ns = await specNs(); const out = [];
  for (const e of await els()) {
    if (e.elementType !== 'PARTSTUDIO') continue;
    const f = await feats(e.id);
    for (const x of f.features) if (x.namespace && x.namespace.startsWith('e' + FSE) && x.namespace !== ns) { x.namespace = ns; await updFeat(e.id, x); out.push(e.name + '/' + x.name); }
  }
  return out.length;
};

// push the library text, refresh namespaces, verify hash
window.pushLib = async (text) => { const r = await setFS(text); return { notices: r.notices, specs: r.specs, hash: fnv(text) }; };

// master layer ids (read from 00_CAR_MASTER)
window.masterLayers = async () => { const m = await elId('00_CAR_MASTER'); const f = await feats(m); const o = {}; for (const x of f.features) if (x.featureType === 'arvMaster') o[x.name] = x.featureId; window.MASTER = m; window.MLAYER = o; return o; };

// query string deriving the given master layers
window.layerQuery = (ids) => 'query = qUnion([' + ids.map(i => 'qCreatedBy(makeId("' + i + '"), EntityType.BODY)').join(', ') + ']);';

// create (or reuse) a subsystem studio: Derived(master layers) + ARV Studio Builder
window.mkStudio = async (name, srcName, layerIds, studio, derivedName) => {
  let eid = await elId(name);
  if (!eid) eid = (await api('POST', '/partstudios' + DW, { name })).id;
  const src = await elId(srcName);
  const f = await feats(eid);
  if (!f.features.find(x => x.featureType === 'importDerived')) await addFeat(eid, derivedFeature(derivedName || ('DERIVED_' + srcName), src, await elMv(src), layerQuery(layerIds)));
  if (studio && !f.features.find(x => x.featureType === 'arvStudio')) { await specNs(); await addFeat(eid, custom('arvStudio', studio, [pEnum('studio', 'ArvStudio', studio), pBool('hardware', true), { btType: 'BTMParameterQuantity-147', parameterId: 'steer', expression: '0 deg', isInteger: false }])); }
  return { eid, states: await states(eid) };
};

window.stat = async (eid) => (await evalFS(eid, 'function(context is Context, queries) { var r = ""; var e = ""; try silent { r = getVariable(context, "arvStatus"); } try silent { e = getVariable(context, "arvError"); } return r ~ " | err: " ~ e ~ " | bodies: " ~ size(evaluateQuery(context, qEverything(EntityType.BODY))); }')).result;
window.arvErr = async (eid) => (await evalFS(eid, 'function(context is Context, queries) { var r = ""; try silent { r = getVariable(context, "arvError"); } return r; }')).result;

// feature ids are tracked locally (GET /features is quota-limited): localStorage.arvIds
window.ids = () => { try { return JSON.parse(localStorage.arvIds || '{}'); } catch (e) { return {}; } };
window.saveIds = (o) => { const c = ids(); Object.assign(c, o); localStorage.arvIds = JSON.stringify(c); return c; };
// create a subsystem studio WITHOUT reading features: Derived(src layers) + ARV Studio Builder
window.mkStudio2 = async (name, srcEid, layerIds, studio, derivedName) => {
  let eid = await elId(name);
  const rec = ids().studios || {};
  if (!eid) eid = (await api('POST', '/partstudios' + DW, { name })).id;
  const r = rec[name] || { eid };
  r.eid = eid;
  if (!r.derived) { const d = await addFeat(eid, derivedFeature(derivedName, srcEid, await elMv(srcEid), layerQuery(layerIds))); r.derived = d.feature.featureId; r.derivedState = d.featureState.featureStatus; }
  if (studio && !r.builder) { await specNs(); const b = await addFeat(eid, custom('arvStudio', studio, [pEnum('studio', 'ArvStudio', studio), pBool('hardware', true), { btType: 'BTMParameterQuantity-147', parameterId: 'steer', expression: '0 deg', isInteger: false }])); r.builder = b.feature.featureId; r.builderState = b.featureState.featureStatus; }
  rec[name] = r; saveIds({ studios: rec });
  return r;
};
'helpers ok';
