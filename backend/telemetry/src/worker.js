// Musebase 텔레메트리 수집 Worker.
// - POST /ingest : 앱이 보내는 익명 이벤트 배치 저장 (옵트인 사용자만 전송)
// - GET  /stats  : 최근 30일 이벤트 종류별 건수 (투명성 차원에서 공개)
// - GET  /healthz
// 개인정보 없음: client_id는 앱이 만든 랜덤 GUID. IP는 저장하지 않는다.

const MAX_BODY_BYTES = 64 * 1024; // 배치 상한 64KB
const MAX_EVENTS_PER_BATCH = 100;
const PLATFORMS = new Set(["windows", "android", "browser", "macos", "ios"]);
// 앱이 정의한 이벤트만 수용(오남용·쓰레기 데이터 차단). contracts/telemetry-events.md와 동기화.
const EVENT_TYPES = new Set([
  "app_session",
  "playback_source",
  "lyrics_search",
  "lyrics_not_found",
  "wrong_lyrics",
  "translation",
  "feature_use",
  "error",
]);

function json(data, status = 200) {
  return new Response(JSON.stringify(data), {
    status,
    headers: { "content-type": "application/json; charset=utf-8" },
  });
}

async function handleIngest(request, env) {
  if (request.headers.get("content-type")?.includes("application/json") !== true)
    return json({ error: "content-type must be application/json" }, 415);

  const raw = await request.text();
  if (raw.length > MAX_BODY_BYTES) return json({ error: "body too large" }, 413);

  let body;
  try { body = JSON.parse(raw); } catch { return json({ error: "invalid json" }, 400); }

  const { clientId, platform, appVersion, events } = body ?? {};
  if (typeof clientId !== "string" || clientId.length < 8 || clientId.length > 64)
    return json({ error: "clientId" }, 400);
  if (!PLATFORMS.has(platform)) return json({ error: "platform" }, 400);
  if (typeof appVersion !== "string" || appVersion.length > 32)
    return json({ error: "appVersion" }, 400);
  if (!Array.isArray(events) || events.length === 0 || events.length > MAX_EVENTS_PER_BATCH)
    return json({ error: "events" }, 400);

  const now = new Date().toISOString();
  const stmt = env.DB.prepare(
    "INSERT INTO events (received_at, client_id, platform, app_version, type, props) VALUES (?1, ?2, ?3, ?4, ?5, ?6)"
  );
  const rows = [];
  for (const e of events) {
    if (!e || !EVENT_TYPES.has(e.type)) return json({ error: `unknown event type` }, 400);
    const props = JSON.stringify(e.props ?? {});
    if (props.length > 4096) return json({ error: "props too large" }, 400);
    rows.push(stmt.bind(now, clientId, platform, appVersion, e.type, props));
  }
  await env.DB.batch(rows);
  return json({ ok: true, stored: rows.length });
}

async function handleStats(env) {
  const since = new Date(Date.now() - 30 * 24 * 3600 * 1000).toISOString();
  const { results } = await env.DB.prepare(
    `SELECT type, COUNT(*) AS count, COUNT(DISTINCT client_id) AS clients
       FROM events WHERE received_at >= ?1 GROUP BY type ORDER BY count DESC`
  ).bind(since).all();
  return json({ since, totals: results });
}

export default {
  async fetch(request, env) {
    const url = new URL(request.url);
    if (url.pathname === "/healthz") return new Response("ok");
    if (url.pathname === "/ingest" && request.method === "POST") return handleIngest(request, env);
    if (url.pathname === "/stats" && request.method === "GET") return handleStats(env);
    return json({ error: "not found" }, 404);
  },
};
