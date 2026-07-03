# 코딩 스타일 규칙

## 1. 이름

**클래스 / 인터페이스**
- PascalCase: `SessionManager`, `PacketDispatcher`
- 인터페이스는 `I` 접두사: `IPlayerGrain`, `IChannelGrain`

**메서드**
- PascalCase + 동사 시작: `SendAsync`, `GetOrCreate`, `FlushDirtyAsync`
- bool 반환은 `Is` / `Has` / `Can` 접두사: `IsAlive`, `HasSession`

**변수**
- 멤버 변수: `_camelCase` (`_sessionId`, `_sendLock`)
- 지역 변수: `camelCase` (`bytesRead`, `characterId`)
- 상수: `PascalCase` (`MaxPacketSize`, `ExpireSeconds`)

**규칙**
- 축약 금지: `mgr` → `manager`, `buf` → `buffer`, `cnt` → `count`
- 이름만 보고 역할이 보여야 함. 헷갈리면 이름이 잘못된 것

---

## 2. 주석

**달아야 할 것 — WHY**
```csharp
// bytesTransferred == 0 은 TCP 연결 종료를 의미
if (bytesTransferred == 0) { ... }

// 60초 쿨다운 — 에러 폭발 시 LLM 과호출 방지
if (elapsed.TotalSeconds < 60) return;

// SemaphoreSlim으로 송신 직렬화 — 동시 SendAsync는 스트림 오염 유발
await _sendLock.WaitAsync();
```

**달지 말아야 할 것 — WHAT**
```csharp
// 세션 제거          ← 코드가 이미 설명함
_sessions.TryRemove(sessionId, out _);

// 루프 시작          ← 불필요
while (true) { ... }
```

**기준**: 주석을 지웠을 때 다음 사람이 헷갈릴 수 있으면 → 달기. 코드 그대로 읽히면 → 지우기

---

## 3. 함수 길이

- **한 화면(약 40줄)** 안에 들어와야 함
- 넘으면 역할 단위로 분리
- 예외: 반복 루프(`RecvLoopAsync` 등)는 흐름이 선형이라 길어도 허용

```csharp
// 나쁜 예 — 한 함수가 수신 + 파싱 + 디스패치 + 에러처리 다 함
private async Task HandleClientAsync(ClientSession session) { /* 80줄 */ }

// 좋은 예 — 역할 분리
private async Task RecvLoopAsync(ClientSession session) { ... }   // 수신
private bool TryAssemblePacket(out byte[] packet) { ... }         // 파싱
private Task DispatchAsync(ClientSession session, byte[] packet) { ... } // 디스패치
```

---

## 4. 기타

- 매직 넘버 금지: `512` → `MaxPacketSize`, `3600` → `ExpireSeconds`
- `async` 메서드는 반드시 `Async` 접미사
- `try/catch`는 시스템 경계(네트워크 수신, DB 쿼리)에만. 내부 로직에서 남용 금지
