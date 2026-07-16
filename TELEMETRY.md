# Musebase 사용 통계(텔레메트리) 안내 / Telemetry Notice

**한국어** · [English below](#english)

Musebase는 서비스 개선을 위해 **사용자가 동의한 경우에만** 익명 사용 통계를 수집합니다.

- **기본값은 꺼짐(옵트인)** — 첫 실행 때 묻고, 설정 [일반] 탭에서 언제든 바꿀 수 있습니다.
- **익명** — 앱이 로컬에서 만든 무작위 ID(GUID)만 사용합니다. 기기·계정 정보에서 파생하지
  않으며 설정에서 재설정할 수 있습니다. 서버는 IP를 저장하지 않습니다.
- **동의를 끄면 수집 자체를 하지 않습니다** ("모아두고 안 보내는" 방식이 아님).
- 수집 서버도 직접 운영합니다(Cloudflare Workers). **제3자 분석 서비스에 보내지 않습니다.**
- 최근 30일 집계는 누구나 볼 수 있습니다: https://musebase-telemetry.musebase.workers.dev/stats

## 무엇을 수집하나요?

### ① 기본 통계 (곡 정보 없음)
| 항목 | 예 | 쓰임 |
|---|---|---|
| 앱 버전·OS 버전(대분류)·UI 언어·번역 대상 언어 | `0.10.0`, `Windows 10`, `ko` | 지원 우선순위 |
| 재생 소스 앱 | `Spotify.exe` | 어떤 음원 서비스를 우선 지원할지 |
| 가사 검색 결과 | 1위 소스, 소스별 히트/실패/응답시간, 캐시 적중 | 소스 랭킹·속도 개선 |
| 번역 | 선택 엔진, 캐시 적중률 | 무료 엔진 기본값 검증 |
| 기능 사용 횟수 | 미디어 컨트롤/편집/내보내기 등 | UX 우선순위 |
| 오류 | 종류·발생 위치(스택 최상위) | 크래시 수정 |

### ② 품질 리포트 (별도 동의, 곡 제목·아티스트 포함)
| 항목 | 쓰임 |
|---|---|
| "틀린 가사"로 표시한 곡 (제목/아티스트/가사 소스) | 오매칭 수정 |
| 가사를 못 찾은 곡 (제목/아티스트) | 소스 커버리지 확대 |

### 절대 수집하지 않는 것
가사 본문, 전체 재생 이력, API 키, 파일 경로, 이메일·계정 등 개인정보, IP 저장.

## 직접 확인하기
- 이벤트 계약: [`contracts/telemetry-events.md`](contracts/telemetry-events.md)
- 수집 서버 코드: [`backend/telemetry/`](backend/telemetry/)
- 앱 쪽 구현: `Musebase.Engine.ITelemetry` 및 각 플랫폼 헤드 (전부 이 리포에 공개)
- 원본 이벤트 보존 90일, 집계는 영구.

---

## English

Musebase collects anonymous usage statistics **only if you opt in** (off by default; asked on
first run, changeable anytime in Settings). Two separate consents: **① basic stats** (features,
performance, environment — no song data) and **② quality reports** (title/artist of tracks you
mark as wrong lyrics or that fail lyrics search). The identifier is a locally generated random
GUID (resettable); the server stores no IP. When consent is off, nothing is collected at all.
We run our own collection endpoint (Cloudflare Workers) — no third-party analytics. Never
collected: lyrics text, full listening history, API keys, file paths, personal data.
30-day public aggregates: https://musebase-telemetry.musebase.workers.dev/stats
