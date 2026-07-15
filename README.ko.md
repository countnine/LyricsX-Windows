<div align="center">

<img src="docs/img/icon.png" width="88" alt="LyricsX 로고">

<h1>LyricsX for Windows</h1>

<b>Windows 데스크톱에 실시간 이중언어 가사를.</b>

<a href="README.md">English</a> · <b>한국어</b>

<img src="docs/img/demo.gif" width="480" alt="LyricsX 데모">

🌐 <a href="https://countnine.github.io/lyricsx-home/"><b>소개 페이지 · 데모</b></a> · ⬇️ <a href="https://github.com/countnine/LyricsX-Windows/releases/latest"><b>다운로드</b></a>

</div>

LyricsX는 재생 중인 곡의 가사를 자동으로 찾아 줄 단위(그리고 글자 단위)로 동기화하고,
**원문과 번역을 함께** 투명한 데스크톱 오버레이로 보여줍니다. macOS 앱
[LyricsX](https://github.com/ddddxxx/LyricsX)의 Windows 네이티브 재작성판입니다.

## 기능

- **자동 재생 감지** — Windows SMTC 기반. Spotify, Apple Music, YouTube Music 등 미디어 키를
  지원하는 모든 플레이어 대응.
- **다중 소스 가사 검색** — LRCLIB, NetEase, Kugou(酷狗), QQ Music(QQ音乐)을 통합 검색하고
  품질 랭킹으로 최적 가사 자동 선택.
- **글자 단위 노래방** — 인라인 타이밍이 있는 소스(Kugou/QQ/NetEase)는 글자 단위로 채움,
  없으면 줄 단위 폴백.
- **이중언어 표시** — 원문과 번역을 2단으로. 가사 소스의 번역을 우선하고, 없으면
  **DeepL 기계번역 폴백**(API 키 입력형).
- **데스크톱 오버레이** — 투명·클릭스루·항상 위. 이동/크기 조절, 페이드 인/아웃, 배경 판(선택),
  전체화면·일시정지·마우스오버 시 자동 숨김.
- **편집·내보내기** — 현재 가사를 앱에서 무손실 수정, `.lrc` 내보내기, 틀린 가사 표시로 억제.
- **가사 캐시** — 한 번 찾은 가사(번역 포함)를 SQLite에 저장해 재재생 시 즉시·오프라인 표시.
- **다국어 UI — 19개 언어** — 시스템 언어를 따르며(영어 폴백) 설정에서 선택 가능.
  [번역 기여 »](TRANSLATING.md)
- **개인정보** — DeepL API 키는 **암호화(Windows DPAPI)** 저장, UI에서 마스킹 표시.
- **자동 업데이트** — GitHub Releases 기반 Velopack 델타 업데이트.

## 다운로드 & 설치

**[Releases](https://github.com/countnine/LyricsX-Windows/releases/latest)** 에서 최신 빌드를 받으세요:

- **`LyricsX-win-Setup.exe`** — 자동 업데이트 포함 설치(권장)
- **`LyricsX-win-Portable.zip`** — 설치 없이 압축 해제 후 실행

> 아직 코드 서명을 하지 않아 첫 실행 시 Windows SmartScreen 경고가 뜰 수 있습니다 —
> *추가 정보 → 실행*을 선택하세요.

### 요구 사항

- Windows 10 버전 2004(20H1) 이상 또는 Windows 11
- .NET 설치 불필요(빌드는 self-contained)

## 사용법

1. `LyricsX.exe` 실행 → 트레이(숨김 아이콘 `^` 영역)에 녹색 **L** 아이콘 생성
2. 음악 재생 → 화면 하단에 가사 자동 표시
3. 트레이 아이콘 우클릭:
   - **가사 검색…** — 자동 매칭이 틀렸을 때 직접 검색·교체
   - **현재 가사 편집… / 내보내기(.lrc)…**
   - **오버레이 위치 이동 모드** — 드래그로 이동 후 다시 해제
   - **설정…** — 표시 언어, DeepL API 키, 번역 대상 언어, 오버레이 스타일
4. 오버레이만 확인(재생 없이): `LyricsX.exe --demo`

### DeepL 번역 (선택)

[DeepL API](https://www.deepl.com/pro-api) 무료 키(월 50만 자)를 발급받아 **설정**에 입력하면,
가사 소스에 대상 언어 번역이 없을 때 자동으로 기계번역되고 줄 단위로 캐시됩니다(같은 곡은 1회만 번역).
키는 암호화되어 저장됩니다.

## UI 번역

인터페이스는 19개 언어로 제공됩니다(영어 + 한국어 손번역, 나머지는 DeepL 시드).
누구나 GitHub에서 바로 번역을 개선할 수 있습니다 — **[TRANSLATING.md](TRANSLATING.md)**
([English](TRANSLATING.en.md)) 참고.

## 빌드

```powershell
dotnet build src/LyricsX.App          # 개발 빌드
dotnet test  tests/LyricsX.Core.Tests # 유닛 테스트
```

릴리스(Velopack + GitHub Releases) 절차는 **[RELEASING.md](RELEASING.md)** 참고.

## 구조

```
src/LyricsX.Core/   # UI 무관 엔진: LRC 파서, 제공자, 랭킹, 번역, 캐시
src/LyricsX.App/    # WPF 앱: SMTC 감지, 동기화, 오버레이, 트레이, 설정, 다국어
tools/              # mt-bootstrap.ps1 (DeepL 번역 시드)
spikes/             # 기술 검증 스파이크 (SMTC/오버레이/검색)
```

## 라이선스

원본 LyricsX(GPLv3) 파생. 가사 검색 로직은
[LyricsKit](https://github.com/MxIris-LyricsX-Project/LyricsKit) 포팅.
