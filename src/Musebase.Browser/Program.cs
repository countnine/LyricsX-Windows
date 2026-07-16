// Musebase.Browser — PlaybackViewState를 WebSocket으로 방송하고 정적 웹 디스플레이를 서빙하는
// 서버(Phase 1). 계약은 contracts/playback-view-state.md 참고. 스켈레톤 — browser 에이전트가 구현.

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapGet("/healthz", () => "ok");

app.Run();
