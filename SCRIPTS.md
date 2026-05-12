# 스크립트 분석 문서

수박 게임의 모든 런타임 코드는 단 2개 파일로 구성됩니다.

| 파일 | 역할 | 줄 수 |
|------|------|------|
| `Assets/Scripts/SuikaGame.cs` | 게임 매니저 (스폰/입력/점수/UI/게임오버) | ~170 |
| `Assets/Scripts/Fruit.cs`     | 과일 1개의 충돌·머지 로직 | ~33 |

---

## 1. 한눈 요약

```
[ 플레이어 클릭 ]
        ↓
   SuikaGame.Update
        ↓
   DropFruit(x) ──→ SpawnFruit() ──→ 새 GameObject + Rigidbody2D + Fruit 컴포넌트
                                              ↓ (물리로 낙하)
                                       다른 과일과 충돌
                                              ↓
                                    Fruit.OnCollisionEnter2D
                                              ↓
                                    같은 레벨? merged 아님?
                                              ↓ Yes
                                    SuikaGame.MergeAt(중간점, level+1)
                                              ↓
                                    두 과일 Destroy + 새 과일 Spawn + 점수+
```

핵심 아이디어:
- **모든 과일은 같은 클래스**(`Fruit`) — 레벨만 다름
- **스폰/머지/점수는 매니저**(`SuikaGame`) 가 전담
- **씬에 프리팹 없음** — 코드로 동적 생성, 원 스프라이트도 코드로 그림 : 코드 생성보다 프리팹으로 만들어서 직접 관리하게 바꿈. 

---

## 2. `Fruit.cs` — 과일의 행동

### 2.1 필드
```csharp
public int level;     // 0(체리) ~ 10(수박)
public bool merged;   // 이미 합쳐졌으면 true (중복 처리 방지)
```

### 2.2 `OnCollisionEnter2D` — 머지 판정 (핵심)

이 메서드는 Rigidbody2D + Collider2D가 다른 콜라이더와 부딪힐 때마다 Unity가 자동 호출합니다.

**가드 절(early return) 순서:**

```csharp
1. merged == true        → 이미 합쳐졌으면 무시
2. 상대가 Fruit 아님       → 벽/바닥과의 충돌은 무시
3. 상대도 merged          → 동시에 처리되는 걸 방지
4. 레벨 불일치             → 같은 종류가 아니면 무시
5. 이미 최고 레벨(수박)   → 더 합칠 수 없음
6. InstanceID가 작은 쪽   → 한 쌍에서 한쪽만 머지를 실행
```

> **6번이 중요한 이유**: 콜라이더 A와 B가 부딪히면 양쪽 모두 `OnCollisionEnter2D`가 호출됩니다. 둘 다 머지를 실행하면 과일이 2개 생기는 버그가 납니다. **`InstanceID` 큰 쪽만 실행** 규칙으로 한 쌍당 정확히 1번만 처리.

**머지 실행부:**
```csharp
merged = true; other.merged = true;        // 양쪽 잠금
Vector3 mid = (내 위치 + 상대 위치) / 2;    // 중간 지점 계산
SuikaGame.Instance.MergeAt(mid, level + 1); // 매니저에 위임
Destroy(other.gameObject);                  // 두 과일 제거
Destroy(gameObject);
```

---

## 3. `SuikaGame.cs` — 게임 매니저

### 3.1 싱글톤
```csharp
public static SuikaGame Instance;
void Awake() { Instance = this; }
```
`Fruit`가 `SuikaGame.Instance.MergeAt(...)`로 매니저를 찾기 위함.

### 3.2 데이터 배열 (인덱스 = 레벨)
```csharp
fruitColors[11]  // 각 레벨의 색
fruitSizes[11]   // 각 레벨의 직경 (0.4 → 2.95)
```
한 줄만 바꿔도 모든 과일의 외형이 바뀌도록 데이터 주도로 설계.

### 3.3 게임 상태
| 필드 | 의미 |
|------|------|
| `currentLevel` | 지금 드롭 대기 중인 과일의 레벨 |
| `nextLevel`    | 그 다음에 나올 과일 |
| `previewObj`   | 상단에서 떠다니는 반투명 미리보기 |
| `gameOver`     | 게임 종료 플래그 |
| `score`        | 누적 점수 |
| `lastDropTime` | 쿨다운·게임오버 타이머용 |

### 3.4 라이프사이클

```
Awake  → Instance = this
Start  → 첫 currentLevel/nextLevel 랜덤 결정 + 미리보기 표시
Update → 매 프레임:
          1. 마우스 X → 월드 X 변환 + 클램프
          2. 미리보기를 마우스 X로 따라가게
          3. 좌클릭 + 쿨다운 OK → DropFruit
          4. 게임오버 체크
OnGUI  → 점수/Next 표시, 게임오버 시 패널 + Restart 버튼
```

### 3.5 주요 메서드

#### `DropFruit(float x)`
```csharp
1. SpawnFruit(spawn 위치, currentLevel) — 실제 떨어지는 과일 생성
2. currentLevel = nextLevel             — 큐 이동
3. nextLevel = 0~4 사이 새 랜덤         — 다음 과일 준비
4. ShowPreview()                        — 새 currentLevel 미리보기 갱신
```

> **왜 nextLevel은 0~4만?** 합쳐서 만들어지는 레벨 5+ 과일을 직접 스폰하면 너무 쉬워집니다. 0~4(체리~감)만 스폰되고, 사과 이상은 플레이어가 합쳐서 만들어야 합니다.

#### `SpawnFruit(Vector3 pos, int level)` — 동적 과일 생성
```csharp
1. CreateFruitVisual(level)
   ├ 새 GameObject
   ├ SpriteRenderer + 원 스프라이트(MakeCircleSprite)
   ├ color = fruitColors[level]
   └ scale = fruitSizes[level]
2. CircleCollider2D 추가 (radius 0.5)
3. Rigidbody2D 추가 (gravityScale 2.5)
4. Fruit 컴포넌트 추가 + level 세팅
5. PhysicsMaterial2D (bounciness 0.05, friction 0.4) 부여
```

#### `MergeAt(Vector3 pos, int newLevel)` — `Fruit`가 호출
```csharp
SpawnFruit(pos, newLevel);          // 한 단계 큰 과일 생성
score += (newLevel + 1) * 10;       // 점수 가산
```

#### `CheckGameOver()`
```csharp
모든 Fruit를 순회하며:
  y > 4.0  &&  속도 < 0.1  &&  마지막 드롭 후 1.5초 경과
조건 만족 → gameOver = true
```
드롭 직후 잠깐 라인을 넘는 건 자연스러우니, **정지 상태 + 시간 경과** 두 조건으로 오판을 막습니다.

#### `MakeCircleSprite()` — 원 텍스처 절차적 생성
```csharp
128x128 텍스처 픽셀별로:
  중심으로부터 거리 d
  d <= r-1     → 불투명 흰색
  d <= r       → 알파 그라데이션 (안티앨리어싱)
  d > r        → 투명
결과 Sprite 캐시 → 모든 과일이 재사용
```
이미지 에셋 없이도 깔끔한 원이 나옵니다.

### 3.6 `OnGUI()` — UI
- uGUI/TMP 패키지가 없어 IMGUI(`OnGUI`)로 점수/Next/게임오버 화면 처리
- 게임오버 시 Restart 버튼 → 현재 씬 다시 로드

---

## 4. 두 스크립트 간의 책임 분담

| 책임 | Fruit | SuikaGame |
|------|:-----:|:---------:|
| 충돌 감지 | ✅ | ❌ |
| 머지 가능 여부 판단 | ✅ | ❌ |
| 새 과일 스폰 | ❌ | ✅ |
| 점수 가산 | ❌ | ✅ |
| 입력 처리 | ❌ | ✅ |
| 게임오버 판정 | ❌ | ✅ |
| UI 그리기 | ❌ | ✅ |

> Fruit는 **자기 자신의 충돌**만 알고, 게임 전체 상태는 SuikaGame이 관리. 책임 분리가 깔끔해 새 기능(콤보, 사운드 등) 추가가 쉽습니다.

---

## 5. 전체 시퀀스 다이어그램

```
플레이어                Update                SpawnFruit             Fruit              MergeAt
   │                      │                       │                    │                   │
   │── 마우스 이동 ───────▶│                       │                    │                   │
   │                      │ previewObj.x = mouseX │                    │                   │
   │── 클릭 ──────────────▶│                       │                    │                   │
   │                      │── DropFruit ─────────▶│                    │                   │
   │                      │                       │── new GameObject ─▶│                   │
   │                      │                       │   + Rigidbody2D    │                   │
   │                      │                       │   + Collider2D     │                   │
   │                      │                       │   + Fruit(level)   │                   │
   │                      │                       │                    │                   │
   │                      │   (물리 시뮬레이션 — 낙하)                  │                   │
   │                      │                                            │                   │
   │                      │             같은 레벨 과일과 충돌            │                   │
   │                      │                       │                    │── OnCollision ───▶│
   │                      │                       │                    │   가드 통과       │
   │                      │                       │                    │── MergeAt ───────▶│
   │                      │                       │◀─── SpawnFruit ────│                   │
   │                      │                       │   (level+1)        │── Destroy 둘 다  │
   │                      │                                            │                   │
   │                      │── CheckGameOver(매 프레임) ────────────────│                   │
```

---

## 6. 확장 포인트

새 기능을 어디에 넣어야 할지 가이드:

| 추가하고 싶은 것 | 어디에 추가? |
|---|---|
| 머지 사운드 | `SuikaGame.MergeAt` 내부 |
| 머지 파티클 | 같은 위치 |
| 콤보 시스템 | `SuikaGame.MergeAt` + 콤보 타이머 필드 |
| Next 과일 아이콘 UI | `SuikaGame.OnGUI` |
| 최고 점수 저장 | `SuikaGame.CheckGameOver` 직후 PlayerPrefs |
| 일시정지 | `SuikaGame.Update` 맨 앞 + `Time.timeScale` |
| 폭탄 과일 | 새 `Fruit` 서브클래스 또는 `Fruit.level` 음수 등 특수 값 |
| 모바일 터치 | `SuikaGame.Update`의 마우스 부분을 Touch로 대체 |
| 과일 스프라이트 교체 | `SuikaGame.CreateFruitVisual`에서 `MakeCircleSprite` 대신 외부 스프라이트 로드 |

