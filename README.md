# LyricsX for Windows

재생 중인 곡의 가사를 자동으로 찾아 데스크톱에 동기화 표시하고, **이중언어(원문 + 번역)**로 보여주는 Windows 앱.
macOS [LyricsX](https://github.com/ddddxxx/LyricsX)의 Windows 네이티브 재작성판.

## 기능

- **자동 재생 감지** — Windows SMTC 기반. Spotify, Apple Music, YouTube Music 등 미디어 키를 지원하는 모든 플레이어 대응
- **다중 소스 가사 검색** — LRCLIB, NetEase에서 검색 후 품질 랭킹으로 최적 가사 자동 선택
- **이중언어 표시** — 가사 소스가 주는 번역 우선, 없거나 대상 언어가 다르면 **DeepL 기계번역 폴백** (API 키 입력형, 기본 한국어)
- **데스크톱 오버레이** — 투명·클릭스루·항상 위. 현재 줄 + 번역 2단 표시, 카라오케 진행 채움, 드래그로 위치 이동
- **가사 캐시** — 한 번 찾은 가사(번역 포함)는 SQLite에 저장, 재재생 시 즉시·오프라인 표시
- **트레이 제어** — 오버레이 토글, 싱크 오프셋 ±0.5초, 수동 가사 검색, 시작 시 자동 실행

## 요구 사항

- Windows 10 20H1(2004) 이상 / Windows 11
- 배포 zip은 self-contained라 .NET 설치 불필요

## 사용법

1. `LyricsX.exe` 실행 → 트레이(숨김 아이콘 영역 `^`)에 녹색 **L** 아이콘 생성
2. 음악 재생 → 가사가 자동으로 화면 하단에 표시
3. 트레이 아이콘 우클릭:
   - **가사 검색…** — 자동 매칭이 틀렸을 때 직접 검색·교체
   - **오버레이 위치 이동 모드** — 체크 후 드래그로 이동, 다시 해제
   - **설정…** — DeepL API 키 / 번역 언어(기본 KO) / 폰트 크기
4. 데모: `LyricsX.exe --demo` (재생 없이 오버레이 확인)

### DeepL 번역 설정

[DeepL API](https://www.deepl.com/pro-api) 무료 키(월 50만 자)를 발급받아 설정에 입력하면,
가사 소스에 원하는 언어 번역이 없을 때 자동으로 기계번역됩니다. 라인 단위 캐시로 같은 곡은 1회만 번역.

## 빌드

```powershell
dotnet build src\LyricsX.App          # 개발 빌드
dotnet test tests\LyricsX.Core.Tests  # 유닛 테스트
.\scripts\publish.ps1 -Version 0.1.0  # 배포 zip 생성 (artifacts\)
```

## 구조

```
src/LyricsX.Core/   # UI 무관 엔진: LRC 파서, 제공자(LRCLIB/NetEase), 랭킹, 번역, 캐시
src/LyricsX.App/    # WPF 앱: SMTC 감지, 동기화, 오버레이, 트레이, 설정
spikes/             # 기술 검증 스파이크 (SMTC/오버레이/검색)
```

## 라이선스

원본 LyricsX(GPLv3) 파생. 가사 검색 로직은 [LyricsKit](https://github.com/MxIris-LyricsX-Project/LyricsKit) 포팅.
