# Musebase.Android (Phase 2 스파이크)

`.NET for Android`(net8.0-android) 헤드. **아직 `Musebase.sln`에 포함하지 않는다** —
android 워크로드가 CI/모든 개발 머신에 없어도 메인 빌드가 깨지지 않게 하기 위함.
앱이 성숙하면 sln 등록 + `ci.yml`에 `dotnet workload install android` 단계를 추가한다.

- 빌드(로컬): `dotnet workload install android`(관리자 필요할 수 있음) 후
  `dotnet build src/Musebase.Android -c Release`
- 스파이크 목표: MediaSession(NotificationListenerService)으로 `INowPlayingSource` 구현
  → 재생 곡명/아티스트/위치가 감지되는지 확인. 엔진 조립은 `LyricsEngineFactory` 재사용.
- 골든룰: `Musebase.Core`/`Musebase.Engine`/`contracts/`는 수정 금지(.claude/agents/android.md).
