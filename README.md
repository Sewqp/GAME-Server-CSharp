# RPG-Server-CSharp

C# 기반 비동기 분산 게임 서버 포트폴리오

**기간**: 2026.06.24 ~ 2026.08.24

---

## 기술 스택

| 분류 | 기술 |
|---|---|
| 언어 / 플랫폼 | C# / .NET 10.0 |
| 네트워크 | TCP 소켓 (async/await) |
| 분산 처리 | Microsoft Orleans |
| DB | MySQL 8.x, Redis |
| 관리 서버 | Node.js, Express |
| 배포 | Docker, Docker Compose |
| AI 연동 | 로컬 LLM (에러 로그 분석) |

---

## 목표

- 3만명 더미 클라이언트 동시접속
- Orleans 기반 분산 서버 Docker 시연
- 모바일 게임 서버 특화 기능 구현

---

## 개발 단계

### Phase 1 — TCP 비동기 서버 기반 구축
- async/await 기반 TCP 소켓 서버
- 세션 관리, 패킷 핸들러

### Phase 2 — DB 설정
- MySQL 커넥션 풀
- Redis 캐싱, 세션 관리

### Phase 3 — 게임 서버 기능
- 채팅 (채널, 귓속말)
- 매칭 시스템
- 재접속 처리
- 하트비트

### Phase 4 — Orleans 분산 처리 + Docker
- Orleans 클러스터 구성
- Docker Compose로 서버 여러 대 시연

### Phase 5 — 스트레스 테스트
- 더미 클라이언트 3만명 동시접속 테스트
- 병목 분석 및 버그 수정

### Phase 6 — 관리 서버 + LLM 연동
- Node.js REST API
- 로컬 LLM 에러 로그 분석 파이프라인

---

## GitHub

[RPG_Game_Server_Portfolio-2026 (C++ 버전)](https://github.com/Sewqp/RPG_Game_Server_Portfolio-2026)
