# Project: Unity AI Suika

수박 게임 (Suika Game) 클론을 Unity 6 + Claude Code로 만드는 프로젝트.

## 핵심 정보

- **엔진**: Unity 6 (2D, Built-in RP)
- **씬**: `Assets/watermelon.unity` (메인 씬)
- **스크립트**: `Assets/Scripts/` (런타임), `Assets/Editor/` (에디터 도구)
- **상세 설계**: [GDD.md](./GDD.md) 참고

## 폴더 구조

```
Assets/
├── watermelon.unity      # 메인 씬
├── Scripts/
│   ├── SuikaGame.cs      # 매니저: 스폰/입력/점수/게임오버
│   └── Fruit.cs          # 과일 머지 로직
└── Editor/               # 에디터 전용 (메뉴 도구 등)
```

## 코드 스타일

- C# 포매터: **CSharpier** (`.cs` 저장 시 `PostToolUse` 훅으로 자동 포맷)
  - 설치: `dotnet tool install -g csharpier`
- 네임스페이스 생략 (단일 프로젝트라 불필요)
- `public` 필드는 Inspector 노출 목적일 때만, 그 외 `[SerializeField] private`
- Awake → 참조 캐싱, Start → 게임 상태 초기화

## Unity MCP

Unity Editor의 내장 MCP를 통해 Claude가 직접 씬을 조작할 수 있습니다.

- 활성화: **Unity Editor > Project Settings > AI > Unity MCP > Approve**
- 연결이 끊기면 ("Connection revoked") 같은 위치에서 다시 승인
- 주요 도구:
  - `Unity_RunCommand`: C# 코드를 에디터에서 즉시 컴파일/실행
  - `Unity_GetConsoleLogs`: 컴파일 에러/런타임 로그 조회
  - `Unity_SceneView_Capture2DScene`: 씬 영역 스크린샷

## Run Command 작성 규칙

`Unity_RunCommand`로 코드 실행 시 (Unity MCP의 골든 템플릿):

```csharp
using UnityEngine;
using UnityEditor;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        // 1. 로직
        // 2. result.RegisterObjectCreation / Modification / DestroyObject
        // 3. result.Log(...)
    }
}
```

- 클래스명은 반드시 `CommandScript`
- 반드시 `internal`
- 객체 생성/수정/삭제는 result 메서드 사용 (Undo 지원)

## 작업 시 주의

- **새 파일 작성 금지** (`*.md` 포함) — 명시적 요청이 있을 때만
- 기존 스크립트는 CSharpier 포맷 유지
- 씬 변경 후 `EditorSceneManager.MarkSceneDirty` + `SaveScene` 호출
- UI 패키지(`UnityEngine.UI`)가 프로젝트에 없을 수 있음 → `OnGUI` 또는 TMP 사용
- 머지 중복 방지: 두 콜라이더가 동시에 OnCollisionEnter2D를 받으므로 `InstanceID` 비교로 한쪽만 처리

## 빌드/실행

- 에디터에서 `watermelon` 씬을 열고 Play
- 메뉴 도구가 있다면 `Suika/Build Scene` 등으로 씬 자동 구성

## 마일스톤

- **M1**: 플레이 가능한 프로토타입 ← **현재 단계**
- **M2**: 폴리시 (스프라이트, 사운드, uGUI/TMP)
- **M3**: 릴리즈 (메뉴, 설정, 모바일 빌드)
