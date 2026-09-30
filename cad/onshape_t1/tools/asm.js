// Assembly builder (runs in the Onshape tab after helpers.js). Uses the kinematic links and part-owned
// mate connectors created by the ARV Studio Builder (variable arvLinks, read via the eval API).
window.linkInfo = async (eid) => (await evalFS(eid, 'function(context is Context, queries) { return getVariable(context, "arvLinks"); }')).result;

window.linkParts = async (eid) => (await evalFS(eid, 'function(context is Context, queries) { var o = {}; for (var b in evaluateQuery(context, qBodyType(qEverything(EntityType.BODY), BodyType.SOLID))) { for (var a in getAttributes(context, { "entities" : b })) { if (a is map && a.link != undefined && a.link != "") { if (o[a.link] == undefined) o[a.link] = { "ids" : [], "primary" : "" }; o[a.link].ids = append(o[a.link].ids, transientQueriesToStrings(b)); } if (a is map && a.arvPrimary != undefined) { if (o[a.arvPrimary] == undefined) o[a.arvPrimary] = { "ids" : [], "primary" : "" }; o[a.arvPrimary].primary = transientQueriesToStrings(b); } } } return o; }')).result;
window.gatherLinks = async (studios) => {
  window.LINKS = window.LINKS || {}; window.JOINTS = window.JOINTS || [];
  for (const n of studios) {
    const eid = ELS[n];
    const info = await linkInfo(eid);
    const lp = await linkParts(eid);
    for (const [ln, rec] of Object.entries(info.links)) LINKS[ln] = Object.assign({ studio: n, eid, feature: info.feature, partIds: lp[ln].ids, primaryId: lp[ln].primary }, rec);
    JOINTS = JOINTS.filter(j => j.studio !== n);
    for (const j of info.joints) JOINTS.push(Object.assign({ studio: n, feature: info.feature }, j));
  }
  return { links: Object.keys(LINKS).length, joints: JOINTS.length };
};

window.mkAsm = async (name) => { let eid = await elId(name); if (!eid) eid = (await api('POST', '/assemblies' + DW, { name })).id; return eid; };
window.asmDef = async (eid) => api('GET', '/assemblies' + DW + '/e/' + eid);
window.insertStudio = async (aeid, srcEid) => api('POST', '/assemblies' + DW + '/e/' + aeid + '/instances', { documentId: D, elementId: srcEid, isWholePartStudio: true, isAssembly: false });
window.insertAsm = async (aeid, srcEid) => api('POST', '/assemblies' + DW + '/e/' + aeid + '/instances', { documentId: D, elementId: srcEid, isAssembly: true });

window.mcq = (path, featureId) => ({ btType: 'BTMPartStudioMateConnectorQuery-1324', path, featureId, queryData: '' });
window.mateFeature = (name, type, q1, q2, limits) => {
  const params = [
    { btType: 'BTMParameterEnum-145', enumName: 'Mate type', value: type, parameterId: 'mateType' },
    { btType: 'BTMParameterQueryWithOccurrenceList-67', parameterId: 'mateConnectorsQuery', queries: [q1, q2] }];
  if (limits) {
    params.push({ btType: 'BTMParameterBoolean-144', value: true, parameterId: 'limitsEnabled' });
    const key = type === 'SLIDER' ? 'limitZ' : 'limitAxialZ';
    params.push({ btType: 'BTMParameterNullableQuantity-807', parameterId: key + 'Min', expression: limits[0], isNull: false, nullValue: '' });
    params.push({ btType: 'BTMParameterNullableQuantity-807', parameterId: key + 'Max', expression: limits[1], isNull: false, nullValue: '' });
  }
  return { btType: 'BTMMate-64', featureType: 'mate', name, namespace: '', version: 2, parameters: params };
};
window.groupFeature = (name, paths) => ({ btType: 'BTMMateGroup-65', featureType: 'mateGroup', name, namespace: '', parameters: [
  { btType: 'BTMParameterQueryWithOccurrenceList-67', parameterId: 'occurrences', queries: paths.map(p => ({ btType: 'BTMIndividualOccurrenceQuery-626', path: p })) }] });
window.addAsmFeat = async (aeid, f) => { const r = await api('POST', '/assemblies' + DW + '/e/' + aeid + '/features', { feature: f }); return r.featureState ? r.featureState.featureStatus : '?'; };

// map elementId|partId -> [path] for top-level part instances and parts inside sub-assembly instances
window.pathMap = (def) => {
  const m = {};
  for (const i of def.rootAssembly.instances) if (i.type === 'Part') m[i.elementId + '|' + i.partId] = [i.id];
  for (const i of def.rootAssembly.instances) if (i.type === 'Assembly') {
    const sub = def.subAssemblies.find(s => s.elementId === i.elementId);
    if (sub) for (const j of sub.instances) if (j.type === 'Part') m[j.elementId + '|' + j.partId] = [i.id, j.id];
  }
  return m;
};

// studios: part studios inserted whole; subs: sub-assemblies inserted; links: link names considered;
// base: reference link; fastenOnly: links fastened even if they are the moving side of a joint
window.buildAsm = async (name, studios, subs, links, base, opts) => {
  opts = opts || {};
  const aeid = await mkAsm(name);
  let def = await asmDef(aeid);
  const haveEl = new Set(def.rootAssembly.instances.map(i => i.elementId));
  for (const s of studios) if (!haveEl.has(ELS[s])) await insertStudio(aeid, ELS[s]);
  for (const s of (subs || [])) if (!haveEl.has(ELS[s])) await insertAsm(aeid, ELS[s]);
  def = await asmDef(aeid);
  const pm = pathMap(def);
  const pth = (ln, pid) => pm[LINKS[ln].eid + '|' + pid];
  const res = [];
  const present = new Set(links);
  // group each link (only links whose parts are top-level here; sub-assembly links are already grouped inside)
  if (!opts.noGroups) for (const ln of links) {
    const l = LINKS[ln]; const paths = l.partIds.map(p => pth(ln, p)).filter(p => p && p.length === 1);
    if (paths.length > 1) res.push('G ' + ln + ' ' + await addAsmFeat(aeid, groupFeature('GROUP ' + ln, paths)));
  }
  const moving = new Set(); for (const j of JOINTS) if (present.has(j.a) && present.has(j.b)) moving.add(j.b);
  for (const ln of links) {
    if (ln === base || (moving.has(ln) && !(opts.fastenOnly || []).includes(ln))) continue;
    if (opts.skipFasten && opts.skipFasten.includes(ln)) continue;
    const a = LINKS[base], b = LINKS[ln];
    res.push('F ' + ln + ' ' + await addAsmFeat(aeid, mateFeature('FASTEN ' + ln, 'FASTENED', mcq(pth(base, a.primaryId), a.feature + '.' + a.mo), mcq(pth(ln, b.primaryId), b.feature + '.' + b.mo))));
  }
  for (const j of JOINTS) {
    if (!(present.has(j.a) && present.has(j.b))) continue;
    if ((opts.fastenOnly || []).includes(j.b)) continue;
    const A = LINKS[j.a], Bq = LINKS[j.b];
    const lim = j.name === 'STEER_SERVO' ? ['-29 deg', '29 deg'] : (j.name === 'BATTERY_SLIDE' ? ['-165 mm', '0 mm'] : null);
    res.push(j.name + ' ' + await addAsmFeat(aeid, mateFeature(j.name, j.type, mcq(pth(j.a, A.primaryId), j.feature + '.' + j.aop), mcq(pth(j.b, Bq.primaryId), j.feature + '.' + j.bop), lim)));
  }
  return { aeid, res };
};
'asm ok';

// ---- quota-safe variant: links from part descriptions "[LNK_...]", connectors from the ARV Link Connectors feature
window.PRIMARY = { LNK_CHASSIS: 'MAIN_CHASSIS', LNK_FRONT_FRAME: 'FRONT_BULKHEAD', LNK_CRANK_L: 'BELLCRANK_L', LNK_CRANK_R: 'BELLCRANK_R', LNK_DRAG_LINK: 'DRAG_LINK', LNK_SERVO_LINK: 'SERVO_LINK', LNK_SERVO_HORN: 'SERVO_HORN_25T', LNK_KNUCKLE_L: 'FRONT_KNUCKLE_L', LNK_KNUCKLE_R: 'FRONT_KNUCKLE_R', LNK_WHEEL_FL: 'RIM_FL', LNK_WHEEL_FR: 'RIM_FR', LNK_TIE_ROD_L: 'TIE_ROD_L', LNK_TIE_ROD_R: 'TIE_ROD_R', LNK_CAMBER_LINK_L: 'CAMBER_LINK_L', LNK_CAMBER_LINK_R: 'CAMBER_LINK_R', LNK_REAR_ARM_L: 'TRAILING_ARM_L', LNK_REAR_ARM_R: 'TRAILING_ARM_R', LNK_WHEEL_RL: 'RIM_RL', LNK_WHEEL_RR: 'RIM_RR', LNK_REAR_SHOCK_L: 'SHOCK_RL', LNK_REAR_SHOCK_R: 'SHOCK_RR', LNK_BATTERY_TUNNEL: 'BATTERY_TUNNEL', LNK_BATTERY: 'BATTERY_3S_5000', LNK_ELECTRONICS_TRAY: 'TRAY_UPPER_DECK', LNK_FASCIA: 'FRONT_SENSOR_FASCIA', LNK_FRONT_BUMPER: 'FRONT_BUMPER', LNK_REAR_BUMPER: 'REAR_BUMPER', LNK_BODY_MAIN: 'BODY_MAIN_REAR', LNK_CENTER_PANEL: 'CENTER_SERVICE_PANEL', LNK_BATTERY_PANEL: 'REAR_BATTERY_PANEL', LNK_CAMERA_POD: 'CAMERA_POD', LNK_FENDER_FL: 'FENDER_FL', LNK_FENDER_FR: 'FENDER_FR', LNK_FENDER_RL: 'FENDER_RL', LNK_FENDER_RR: 'FENDER_RR' };
// joint table mirrored from jointTable() in the library: [name, linkA, linkB, type]
window.JTAB = (() => { const j = [['STEER_SERVO', 'LNK_FRONT_FRAME', 'LNK_SERVO_HORN', 'REVOLUTE'], ['HORN_LINK', 'LNK_SERVO_HORN', 'LNK_SERVO_LINK', 'REVOLUTE'], ['LINK_DRAG', 'LNK_DRAG_LINK', 'LNK_SERVO_LINK', 'REVOLUTE']];
  for (const t of ['L', 'R']) j.push(['CRANK_' + t, 'LNK_FRONT_FRAME', 'LNK_CRANK_' + t, 'REVOLUTE'], ['DRAG_' + t, 'LNK_CRANK_' + t, 'LNK_DRAG_LINK', 'REVOLUTE'], ['KINGPIN_' + t, 'LNK_FRONT_FRAME', 'LNK_KNUCKLE_' + t, 'REVOLUTE'], ['WHEEL_F' + t, 'LNK_KNUCKLE_' + t, 'LNK_WHEEL_F' + t, 'REVOLUTE'], ['TIE_OUT_' + t, 'LNK_KNUCKLE_' + t, 'LNK_TIE_ROD_' + t, 'BALL'], ['TIE_IN_' + t, 'LNK_CRANK_' + t, 'LNK_TIE_ROD_' + t, 'BALL'], ['CAMBER_OUT_' + t, 'LNK_KNUCKLE_' + t, 'LNK_CAMBER_LINK_' + t, 'BALL'], ['CAMBER_IN_' + t, 'LNK_FRONT_FRAME', 'LNK_CAMBER_LINK_' + t, 'BALL'], ['WHEEL_R' + t, 'LNK_REAR_ARM_' + t, 'LNK_WHEEL_R' + t, 'REVOLUTE']);
  j.push(['BATTERY_SLIDE', 'LNK_BATTERY_TUNNEL', 'LNK_BATTERY', 'SLIDER']); return j; })();

// add the connector feature to each studio (once) and remember its feature id
window.addConnectors = async (studios) => { const rec = ids().conn || {}; const out = []; await specNs();
  for (const n of studios) { if (rec[n]) { out.push(n + ' has'); continue; } const r = await addFeat(ELS[n], custom('arvLinkConnectors', 'LINK_CONNECTORS', [])); rec[n] = r.feature.featureId; out.push(n + ' ' + r.featureState.featureStatus); }
  saveIds({ conn: rec }); return out.join(' | '); };

// links from /parts descriptions
window.gatherLinks2 = async (studios) => { const conn = ids().conn; window.LINKS = {};
  for (const n of studios) { const parts = await api('GET', '/parts' + DW + '/e/' + ELS[n]);
    for (const p of parts) { const m = (p.description || '').match(/\[(LNK_[A-Z_]+)\]$/); if (!m) continue; const ln = m[1];
      const L = LINKS[ln] || (LINKS[ln] = { studio: n, eid: ELS[n], feature: conn[n], partIds: [], primaryId: null, mo: 'mo_' + ln });
      L.partIds.push(p.partId); if (p.name === PRIMARY[ln]) L.primaryId = p.partId; } }
  window.JOINTS = JTAB.filter(j => LINKS[j[1]] && LINKS[j[2]]).map(j => ({ name: j[0], a: j[1], b: j[2], type: j[3], feature: LINKS[j[1]].feature, aop: 'mj_' + j[0] + '_a', bop: 'mj_' + j[0] + '_b' }));
  return Object.entries(LINKS).map(([k, v]) => k + ':' + v.partIds.length + (v.primaryId ? '' : '!')).join(' '); };

// ---- serial-based version (Group mates are not accepted through the REST API)
window.gatherLinks3 = async (studios) => { const conn = ids().conn; window.LINKS = {};
  for (const n of studios) { const parts = await api('GET', '/parts' + DW + '/e/' + ELS[n]);
    for (const p of parts) { const m = (p.description || '').match(/\[(LNK_[A-Z_]+) #(\d+)\]$/); if (!m) continue; const ln = m[1];
      const L = LINKS[ln] || (LINKS[ln] = { studio: n, eid: ELS[n], feature: conn[n], partIds: [], serial: {}, primaryId: null, mo: 'mo_' + ln });
      L.partIds.push(p.partId); L.serial[p.partId] = m[2]; if (p.name === PRIMARY[ln] && !L.primaryId) L.primaryId = p.partId; } }
  window.JOINTS = JTAB.filter(j => LINKS[j[1]] && LINKS[j[2]]).map(j => ({ name: j[0], a: j[1], b: j[2], type: j[3], feature: LINKS[j[1]].feature, aop: 'mj_' + j[0] + '_a', bop: 'mj_' + j[0] + '_b' }));
  return Object.entries(LINKS).map(([k, v]) => k + ':' + v.partIds.length + (v.primaryId ? '' : '!')).join(' '); };

// existing mate names in an assembly (to resume after interruptions)
window.mateNames = async (aeid) => { try { return new Set((await api('GET', '/assemblies' + DW + '/e/' + aeid + '/features')).features.map(f => f.name)); } catch (e) { return new Set(); } };

// phase 1: insert studios; phase 2 (resumable, bounded by maxCalls): link-internal fastens, link fastens, joints
window.buildAsm3 = async (name, studios, links, base, opts) => {
  opts = opts || {}; const max = opts.maxCalls || 60; let calls = 0;
  const aeid = await mkAsm(name);
  let def = await asmDef(aeid);
  const haveEl = new Set(def.rootAssembly.instances.map(i => i.elementId));
  for (const s of studios) if (!haveEl.has(ELS[s])) { await insertStudio(aeid, ELS[s]); calls++; }
  def = await asmDef(aeid); const pm = pathMap(def); const done = await mateNames(aeid);
  const pth = (ln, pid) => pm[LINKS[ln].eid + '|' + pid];
  const todo = [];
  for (const ln of links) { const L = LINKS[ln];
    for (const pid of L.partIds) if (pid !== L.primaryId) todo.push(['P ' + ln + ' #' + L.serial[pid], 'FASTENED', mcq(pth(ln, L.primaryId), L.feature + '.mo_' + ln), mcq(pth(ln, pid), L.feature + '.mp' + L.serial[pid]), null]); }
  const present = new Set(links); const moving = new Set(); for (const j of JOINTS) if (present.has(j.a) && present.has(j.b)) moving.add(j.b);
  for (const ln of links) { if (ln === base || moving.has(ln)) continue; const a = LINKS[base], b = LINKS[ln];
    todo.push(['FASTEN ' + ln, 'FASTENED', mcq(pth(base, a.primaryId), a.feature + '.mo_' + base), mcq(pth(ln, b.primaryId), b.feature + '.mo_' + ln), null]); }
  for (const j of JOINTS) { if (!(present.has(j.a) && present.has(j.b))) continue; const A = LINKS[j.a], Bq = LINKS[j.b];
    const lim = j.name === 'STEER_SERVO' ? ['-29 deg', '29 deg'] : (j.name === 'BATTERY_SLIDE' ? ['-165 mm', '0 mm'] : null);
    todo.push([j.name, j.type, mcq(pth(j.a, A.primaryId), j.feature + '.' + j.aop), mcq(pth(j.b, Bq.primaryId), j.feature + '.' + j.bop), lim]); }
  const res = []; let left = 0;
  for (const t of todo) { if (done.has(t[0])) continue; if (calls >= max) { left++; continue; } const st = await addAsmFeat(aeid, mateFeature(t[0], t[1], t[2], t[3], t[4])); calls++; if (st !== 'OK') res.push(t[0] + ':' + st); }
  return { aeid, total: todo.length, left, bad: res };
};
