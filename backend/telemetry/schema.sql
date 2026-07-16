-- Musebase 텔레메트리 이벤트 저장소 (D1/SQLite)
-- 원본 이벤트 보존 90일(정리는 추후 크론), 개인정보 없음(익명 랜덤 client_id).
CREATE TABLE IF NOT EXISTS events (
  id          INTEGER PRIMARY KEY AUTOINCREMENT,
  received_at TEXT    NOT NULL,          -- 서버 수신 시각(ISO-8601, UTC)
  client_id   TEXT    NOT NULL,          -- 앱이 로컬 생성한 랜덤 GUID(재설정 가능)
  platform    TEXT    NOT NULL,          -- windows | android | browser | macos | ios
  app_version TEXT    NOT NULL,
  type        TEXT    NOT NULL,          -- 이벤트 종류(app_session, lyrics_search, ...)
  props       TEXT    NOT NULL DEFAULT '{}'  -- 이벤트별 속성(JSON)
);
CREATE INDEX IF NOT EXISTS idx_events_type_time ON events(type, received_at);
CREATE INDEX IF NOT EXISTS idx_events_time ON events(received_at);
