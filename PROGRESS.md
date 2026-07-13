# PROGRESS — LyricsX for Windows

> 세션이 끊겨도 이 파일만 읽으면 재개 가능하도록 유지한다.
> 재개 방법: 세션 리셋 후 "이어서"라고 입력.

## ▶ 다음 세션 첫 작업
- [ ] **DeepL 라이브 검증 대기(사용자)**: 트레이 → 설정…에 API 키 입력 후 외국어 곡 재생,
  번역 라인이 한국어(MT)로 바뀌는지 확인. 캐시 동작: 같은 곡 재재생 시 즉시.
- [ ] **M5 시작**: P1 — 수동 검색 창, LRC 로컬 저장/캐시(검색 자체 캐시), Windows 시작 시 자동 실행,
  패키징(Velopack 또는 zip 배포), 앱 아이콘(SystemIcons.Application 교체)
  - 성능: 가사 검색 자체의 SQLite/파일 캐시로 재생목록 반복 시 p50<300ms 달성

## 마일스톤 현황
- [x] **M0** 스파이크 — 완료 (2026-07-13). 오버레이/렌더 스택 = **WPF 확정**
- [x] **M1** Core 엔진 — **완료 (U3 제외, 2026-07-13)**
  - [x] U1: Lyrics 모델 + LRC 파서 (12 테스트)
  - [x] U2: LRCLIB + NetEase 제공자 (EAPI AES-ECB 포함, 라이브 검증)
  - [x] U4: 품질 랭킹 + LyricsSearchService 병렬 집계 (총 26 테스트, 라이브 검증: 번역 있는 NetEase q=0.990 > LRCLIB q=0.940)
  - [ ] U3: QQ/Kugou — 후순위로 이동 (위 참조)
- [x] **M2** NowPlaying + SyncScheduler + 트레이 — 완료 (2026-07-13)
  - NowPlayingService(SMTC+보간), LyricsCoordinator(스트리밍 검색+점진 교체+100ms 틱), 트레이 툴팁
  - 스모크 검증: 트랙 감지→검색→랭킹 교체 첫 결과 ~0.9s (스트리밍이 9.4s 배치 문제 완화)
- [x] **M3** 오버레이 완성 — 완료 (2026-07-13)
  - OutlinedTextElement DP화, OverlayWindow(이중언어 2단+카라오케 채움+이동 모드+위치 영속화)
  - 트레이 제어(토글/이동/오프셋±), --demo 모드, 스크린샷 검증 완료
  - 남김: 전체화면 감지(P1), 실음악 육안 검증(사용자)
- [x] **M4** 번역 계층 + 설정 창 — 완료 (2026-07-13) = **P0(MVP) 코드 완성**
  - DeeplTranslator(free/pro 자동 판별, 50개 배치), SqliteTranslationCache, LyricsTranslationService
  - 표시 체인: tr:{target}(MT) → tr(제공자). 키 없으면 제공자 번역만(강등, PRD 정책)
  - 설정 창(키/언어/폰트, 라이브 반영). 32 유닛 테스트 통과
  - 남김: DeepL 실키 라이브 검증(사용자 키 필요)
- [ ] M5 P1 (수동 검색, 검색 캐시, 자동 실행, 패키징, 아이콘 + QQ/Kugou)

## 기술 결정 기록
- 오버레이/렌더 스택 = WPF (`OutlinedTextElement` + KaraokeProgress DP, Spike.Overlay 검증)
- SMTC 타임라인은 `LastUpdatedTime` 기반 보간 필수 (Spike.Smtc 구현 예시)
- LyricsX.Core는 net8.0 순수(Windows 의존성 없음) — 테스트 용이
- NetEase EAPI: .NET 내장 AES-ECB/MD5로 CryptoSwift 대체. 검색은 2-pass 쿠키
- 랭킹 가중치: artist 0.45 / title 0.40 / duration 0.15, 번역 +0.05, tt +0.05, 반주변형 -0.3
- **성능 주의**: 라이브 검색 4후보 집계에 ~9.4s (NetEase eapi가 지배적) → NFR p50<2s는 M5 캐시 + limit 조정 + 첫 결과 우선 표시(스트리밍 UI)로 해결 예정
- .NET 8 SDK 8.0.422. 새 셸마다: `$env:Path += ';C:\Program Files\dotnet'`

## 완료 항목
- [x] PRD 확정, 저장소/솔루션 스캐폴드, M0 스파이크 3종
- [x] M1 U1/U2/U4 (26 유닛 테스트 + 라이브 스모크 `spikes/Spike.Search`)

## 미해결 이슈
- 클릭스루 실제 마우스 통과 육안 미검증 → M3에서 사용자 확인
- NetEase 검색 API가 간헐 캡차/차단 가능성 — 실패 시 LRCLIB 단독으로도 동작함(집계가 흡수)

## 참조
- PRD: `C:\Users\AN020\.claude\plans\precious-cooking-raven.md`
- 원본(macOS): `C:\Users\AN020\LyricsX`
- 엔진 포팅 참조: `external/LyricsKit`
- 운영 규칙: 5시간 세션 한도 60% 도달 추정 시 현 작업 단위 마무리 후 중단, 커밋+본 파일 갱신
