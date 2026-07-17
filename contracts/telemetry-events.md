# 텔레메트리 이벤트 계약 (v1)

앱 → 수집 서버(`backend/telemetry`, `POST /ingest`)의 언어 중립 계약. 단일 진실은 이 문서.
Worker의 화이트리스트(`EVENT_TYPES`)·앱 계측·`TELEMETRY.md`는 이 문서와 **같은 PR에서** 동기화한다.
정책(옵트인 2단계, 익명)은 ADR-0004.

## 전송 형식

`POST https://musebase-telemetry.musebase.workers.dev/ingest` (application/json)

```json
{
  "clientId": "f3a9…(로컬 생성 랜덤 GUID, 8–64자)",
  "platform": "windows | android | browser | macos | ios",
  "appVersion": "0.10.0",
  "events": [ { "type": "<아래 표>", "props": { } } ]
}
```

- 배치 ≤ 100건, 본문 ≤ 64KB, `props` 직렬화 ≤ 4KB/건. 초과·미정의 type = 전체 400 거부.
- 시각 필드는 보내지 않는다 — 서버 수신 시각(`received_at`)만 저장(정밀 타임라인 불필요·익명성↑).
- 실패 시 앱은 로컬 큐에 보관 후 재시도(최대 보관 상한 있음), 앱 동작에는 절대 영향 없음.

## 이벤트 (① = 기본 통계 동의, ② = 품질 리포트 동의)

| type | 동의 | props | 비고 |
|---|---|---|---|
| `app_session` | ① | `uiLang`, `targetLang`, `engine`(deepl/libretranslate/none), `sources`(활성 소스 id 배열), `sourceMode`(auto/특정앱), `osVersion`(대분류, 예 "Windows 10") | 하루 1회(일일 ping 겸용) |
| `playback_source` | ① | `appId`(SMTC/MediaSession 앱 식별자) | 클라이언트가 하루 중 앱별 1회로 디바운스 |

"하루 1회"의 기준: **로컬 날짜(yyyy-MM-dd, 자정 리셋)** — 24시간 롤링 아님. 모든 플랫폼 헤드 동일.
| `lyrics_search` | ① | `winner`(채택 소스 id 또는 "none"), `perSource`({id: {hit: bool, latencyMs: int}}), `cached`(bool), `cleanedQueryUsed`(bool) | 곡 정보 없음 |
| `lyrics_not_found` | ② | `title`, `artist` | 검색 실패 곡 |
| `wrong_lyrics` | ② | `title`, `artist`, `source`(채택됐던 소스 id) | "틀린 가사" 표시 시 |
| `translation` | ① | `engine`, `cacheHitPct`(0–100 정수), `linesBucket`("1-10"/"11-50"/"51+") | 곡당 1회(번역 파이프라인 완료 시) |
| `feature_use` | ① | `feature`(mediaControls/edit/export/offset/search/karaoke…), `count`(int) | 클라이언트가 세션 단위로 집계해 전송 |
| `error` | ① | `kind`(예외 타입명), `frame`(최상위 스택프레임 1개), `fatal`(bool) | 메시지 본문·경로 금지 |

## 변경 규칙

- **props 필드 추가 = 하위 호환**(서버는 JSON 그대로 저장) — v1 유지. type 추가는 Worker
  화이트리스트와 함께. type 의미 변경/삭제는 breaking → 버전 올리고 ADR 기록.
- 새 props에 개인정보·곡 정보(①에서)·자유 텍스트를 넣지 않는다. 자유 텍스트가 필요하면
  버킷/열거형으로 바꿔 설계한다.
