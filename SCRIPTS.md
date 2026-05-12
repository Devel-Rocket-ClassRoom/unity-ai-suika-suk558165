# 스크립트 분석 문서

수박 게임의 런타임 코드는 4개 파일로 구성됩니다.

| 파일 | 역할 | 줄 수 |
|------|------|------|
| `Assets/Scripts/SuikaGame.cs` | 게임 매니저 (스폰/입력/점수/UI/게임오버) | ~200 |
| `Assets/Scripts/Fruit.cs`     | 과일 1개의 충돌·머지·착지 로직 | ~65 |
| `Assets/Scripts/FruitData.cs` | 과일 데이터 ScriptableObject (sprite/size/score 등) | ~30 |
| `Assets/Scripts/DeadLine.cs`  | 데드라인 트리거 — 5초 닿으면 게임오버 | ~70 |

추가로 Inspector에서 연결되는 자산:
- `Assets/Prefabs/Fruit.prefab` — Fruit 프리팹 (SpriteRenderer + CircleCollider2D + Rigidbody2D + Fruit)
- `Assets/ScriptableObjects/Fruits/Fruit_00~10_*.asset` — 11개 FruitData 인스턴스

---

## 1. 한눈 요약

```
[ 플레이어 클릭 ]
        ↓
   SuikaGame.Update
        ↓
   DropFruit(x) ──→ SpawnFruit() ──→ Instantiate(fruitPrefab) → Fruit.Init(FruitData)
                                              ↓ (물리로 낙하)
                                       다른 과일/벽과 충돌
                                              ↓
                                    Fruit.OnCollisionEnter2D
                                       hasLanded = true
                                              ↓
                                    같은 레벨? merged 아님?
                                              ↓ Yes
                                    SuikaGame.MergeAt(중간점, level+1)
                                              ↓
                                    두 과일 Destroy + 새 과일 Spawn + 점수+

[ 다른 한편 ]
   DeadLine (트리거 영역)
   과일이 5초 연속 닿아있으면 → SuikaGame.TriggerGameOver()
```

핵심 아이디어:
- **모든 과일은 같은 `Fruit` 프리팹** — 레벨별 외형/크기는 `FruitData` 가 결정
- **데이터 주도** — Inspector에서 FruitData만 바꿔도 모든 게 바뀜
- **이벤트 기반 게임오버** — 매 프레임 폴링 없이 트리거 콜백으로 처리

---

## 2. `FruitData.cs` — 데이터 정의

ScriptableObject로 각 레벨의 과일을 표현. Inspector에서 끌어다 놓는 방식.

```csharp
public int level;             // 0(체리) ~ 10(수박)
public Sprite sprite;         // 과일 이미지
public Color tint;            // 스프라이트 색조 (보통 흰색)
public float size;            // 월드 단위 직경 (스케일로 사용)
public float colliderRadius;  // 스프라이트 내용에 맞춘 콜라이더 반경
public int score;             // 머지 시 가산 점수
public AudioClip mergeSfx;    // 머지 효과음 (옵션)
```

`colliderRadius`는 스프라이트 픽셀을 스캔해 자동 계산됨 (잎/줄기 같은 부속물 제외, 본체 inscribed circle).

---

## 3. `Fruit.cs` — 과일의 행동

### 3.1 필드
```csharp
public int level;             // FruitData.level 복사본
public bool merged;           // 이미 합쳐졌으면 true (중복 처리 방지)
public bool hasLanded;        // 첫 충돌 시 true (다음 미리보기 트리거)
```

### 3.2 `Init(FruitData data)` — 외형 세팅
스폰 직후 SuikaGame이 호출:
1. `SpriteRenderer.sprite` ← data.sprite
2. `SpriteRenderer.color` ← data.tint
3. `transform.localScale` ← data.size
4. `CircleCollider2D.radius` ← data.colliderRadius

### 3.3 `OnCollisionEnter2D` — 머지 판정 (핵심)

이 메서드는 Rigidbody2D + Collider2D가 다른 콜라이더와 부딪힐 때마다 Unity가 자동 호출.

**1순위: 착지 신호 (`hasLanded = true`)**
무엇에 닿든 (벽/바닥/과일) 첫 번째 충돌은 "착지"로 간주.
→ `SuikaGame.IsReadyForNextPreview()` 가 이걸 보고 다음 미리보기 등장.

**가드 절(early return) 순서:**

```csharp
1. merged == true        → 이미 합쳐졌으면 무시
2. 상대가 Fruit 아님       → 벽/바닥과의 충돌은 머지에서 무시 (착지는 위에서 처리됨)
3. 상대도 merged          → 동시 처리 방지
4. 레벨 불일치             → 같은 종류만 머지
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

## 4. `SuikaGame.cs` — 게임 매니저

### 4.1 싱글톤
```csharp
public static SuikaGame Instance;
void Awake() { Instance = this; }
```
`Fruit`와 `DeadLine`이 매니저에 접근하기 위함.

### 4.2 Inspector 노출 필드
| Header | 필드 | 의미 |
|---|---|---|
| References | `fruitPrefab` | Fruit 프리팹 |
| References | `fruits[]`   | 11개 FruitData (레벨 순) |
| Drop       | `spawnY`, `minX`, `maxX`, `dropCooldown` | 드롭 위치/쿨다운 |
| Game Over  | `gameOverY` | (현재는 DeadLine이 사용, 직접 비교 안 함) |
| Spawn Pool | `spawnableMaxLevel` | 드롭에서 나올 수 있는 최대 레벨 (기본 4) |

### 4.3 게임 상태
| 필드 | 의미 |
|------|------|
| `currentLevel` | 지금 드롭 대기 중인 과일 레벨 |
| `nextLevel`    | 그 다음에 나올 과일 레벨 |
| `previewObj`   | 상단에서 떠다니는 반투명 미리보기 |
| `lastDropped`  | 마지막으로 떨어뜨린 Fruit 참조 (다음 미리보기 등장 판정용) |
| `lastDropTime` | 마지막 드롭 시각 (쿨다운·grace 타이머용) |
| `gameOver`     | 게임 종료 플래그 |
| `score`        | 누적 점수 |

### 4.4 라이프사이클

```
Awake  → Instance = this
Start  → fruits 배열 검증, 첫 currentLevel/nextLevel 랜덤, 미리보기 표시
Update → 매 프레임:
          1. (previewObj 없으면) 다음 미리보기 등장 조건 확인 → 있으면 ShowPreview
          2. 마우스 X → 월드 X 변환 + 클램프
          3. previewObj.x = mouseX 동기화
          4. previewObj 있고 좌클릭 + 쿨다운 OK → DropFruit
OnGUI  → 점수/Next 표시, 게임오버 시 패널 + Restart 버튼
```

### 4.5 주요 메서드

#### `IsReadyForNextPreview()`
다음 미리보기가 나와도 되는지:
```csharp
if (lastDropped == null) return true;     // 머지로 사라졌으면 즉시 OK
return lastDropped.hasLanded;             // 무언가에 닿은 뒤
```

#### `DropFruit(float x)`
```csharp
1. lastDropped = SpawnFruit(spawn 위치, currentLevel)
2. currentLevel = nextLevel
3. nextLevel = 0~spawnableMaxLevel 사이 새 랜덤
4. previewObj 즉시 제거 (IsReadyForNextPreview가 통과해야 새로 표시)
```

> **왜 nextLevel은 0~4만?** 합쳐서 만들어지는 레벨 5+ 과일을 직접 스폰하면 너무 쉬워집니다. 0~4(체리~감)만 스폰되고, 사과 이상은 플레이어가 합쳐서 만들어야 합니다.

#### `SpawnFruit(Vector3 pos, int level)` — 프리팹 인스턴스화
```csharp
var data = fruits[level];
var fruit = Instantiate(fruitPrefab, pos, Quaternion.identity);
fruit.Init(data);   // 외형 + 콜라이더 세팅
return fruit;
```

#### `ShowPreview()` — 미리보기 오브젝트 생성
- 새 GameObject + SpriteRenderer (콜라이더/Rigidbody 없음)
- `tint.a = 0.6` 으로 반투명 처리
- spawnY 위치, 마우스 X 동기화는 Update에서

#### `MergeAt(Vector3 pos, int newLevel)` — `Fruit`가 호출
```csharp
SpawnFruit(pos, newLevel);
score += data.score;
if (data.mergeSfx != null)
    AudioSource.PlayClipAtPoint(data.mergeSfx, pos);
```

#### `TriggerGameOver()` — `DeadLine`이 호출
```csharp
if (gameOver) return;
gameOver = true;
previewObj?.SetActive(false);
```

### 4.6 `OnGUI()` — UI
- uGUI/TMP 패키지가 없어 IMGUI(`OnGUI`)로 점수/Next/게임오버 화면 처리
- 게임오버 시 Restart 버튼 → 현재 씬 다시 로드

---

## 5. `DeadLine.cs` — 게임오버 트리거

`GameOverLine` 게임오브젝트에 BoxCollider2D(`isTrigger=true`) + DeadLine 컴포넌트.

### 5.1 동작
1. `OnTriggerEnter2D` — 진입한 Fruit를 `contacts` HashSet에 추가
2. `OnTriggerExit2D` — 벗어난 Fruit를 제거
3. `Update`:
   - `contacts.RemoveWhere(f => f == null)` — 머지로 사라진 과일 정리
   - 드롭 직후 `dropGrace(0.5초)` 기간엔 카운트 중지 (드롭 위치가 라인 근처라서 오판 방지)
   - `contacts` 에 누군가 있으면 `currentHold += deltaTime`
   - `currentHold >= holdTime(5초)` → `SuikaGame.TriggerGameOver()`
   - 라인을 벗어나면 currentHold 즉시 0으로 리셋

### 5.2 Inspector 파라미터
- `holdTime` — 게임오버까지 닿아 있어야 하는 시간 (기본 5초)
- `dropGrace` — 드롭 직후 무시 시간 (기본 0.5초)
- `currentHold` — 디버그용 (Play 중 실시간 표시)

---

## 6. 책임 분담

| 책임 | Fruit | SuikaGame | FruitData | DeadLine |
|------|:-----:|:---------:|:---------:|:--------:|
| 충돌 감지 | ✅ | ❌ | ❌ | ✅ (트리거) |
| 머지 가능 여부 판단 | ✅ | ❌ | ❌ | ❌ |
| 외형 데이터 보관 | ❌ | ❌ | ✅ | ❌ |
| 과일 스폰 (Instantiate) | ❌ | ✅ | ❌ | ❌ |
| 점수 가산 | ❌ | ✅ | ❌ | ❌ |
| 입력 처리 | ❌ | ✅ | ❌ | ❌ |
| 게임오버 판정 | ❌ | ❌ | ❌ | ✅ |
| 게임오버 상태 변경 | ❌ | ✅ (TriggerGameOver) | ❌ | ❌ |
| UI 그리기 | ❌ | ✅ | ❌ | ❌ |
| 다음 미리보기 타이밍 | ✅ (hasLanded) | ✅ (IsReady…) | ❌ | ❌ |

각 클래스가 단일 책임을 가지며, 매니저(SuikaGame)가 조정자 역할.

---

## 7. 전체 시퀀스 다이어그램

```
플레이어        Update           SpawnFruit       Fruit            DeadLine          MergeAt
   │              │                  │              │                │                 │
   │── 마우스 ───▶│                  │              │                │                 │
   │              │ preview.x=mouseX │              │                │                 │
   │── 클릭 ─────▶│                  │              │                │                 │
   │              │── DropFruit ────▶│              │                │                 │
   │              │                  │── Instantiate(prefab) ──────▶ │                │
   │              │                  │── Init(data) ──────────────▶ │                │
   │              │                  │   sprite/scale/colliderRadius │                │
   │              │                  │                               │                │
   │              │ previewObj=null                                  │                │
   │              │                                                  │                │
   │              │  (물리 시뮬레이션 - 낙하)                          │                │
   │              │                                                  │                │
   │              │  무엇에 충돌 → OnCollisionEnter2D                  │                │
   │              │              hasLanded = true                     │                │
   │              │                                                  │                │
   │              │ IsReadyForNextPreview() → true                    │                │
   │              │ ShowPreview() → 다음 미리보기 등장                   │                │
   │              │                                                  │                │
   │              │  같은 레벨 과일과 충돌 → 머지 가드 통과              │                │
   │              │                                ── MergeAt ──────▶│                │
   │              │                                                  │── SpawnFruit ──┐
   │              │                                                  │  (level+1)     │
   │              │                                                  │── Destroy 둘   │
   │              │                                                  │                │
   │              │  과일이 데드라인 영역 트리거 진입                                    │
   │              │                                  ── contacts.Add ──▶              │
   │              │                                                                   │
   │              │  5초 후 ── TriggerGameOver() ─────────────────────────────────────│
   │              │                                                                   │
```

---

## 8. 확장 포인트

새 기능을 어디에 넣어야 할지 가이드:

| 추가하고 싶은 것 | 어디에 추가? |
|---|---|
| 머지 사운드 | `FruitData.mergeSfx` 슬롯에 AudioClip — 코드는 이미 SuikaGame.MergeAt에 있음 |
| 머지 파티클 | `SuikaGame.MergeAt` — 파티클 프리팹 Instantiate |
| 콤보 시스템 | `SuikaGame.MergeAt` + 콤보 타이머 필드 |
| Next 과일 아이콘 UI | `SuikaGame.OnGUI` — `fruits[nextLevel].sprite` 그리기 |
| 최고 점수 저장 | `SuikaGame.TriggerGameOver` 내부 PlayerPrefs |
| 일시정지 | `SuikaGame.Update` 맨 앞 + `Time.timeScale = 0` |
| 폭탄 과일 | 새 `Fruit` 서브클래스 또는 `FruitData`에 type 필드 |
| 모바일 터치 | `SuikaGame.Update`의 Input.mousePosition → Touch |
| 새 과일 추가 | `FruitData` 인스턴스 1개 더 만들고 `fruits[]` 배열에 추가 |
| 데드라인 시간 조절 | `DeadLine.holdTime` Inspector 값만 변경 |

---

## 9. 코드의 묘미

1. **데이터 주도 디자인** — `FruitData` 만 바꾸면 모든 과일 외형/크기/점수 변경
2. **프리팹 1개로 11종** — Instantiate + Init 패턴으로 메모리/생성 효율 ↑
3. **InstanceID 비교로 중복 머지 방지** — `merged` 플래그와 함께 2중 안전장치
4. **이벤트 기반 게임오버** — 매 프레임 폴링 없이 트리거 콜백으로 처리 (DeadLine)
5. **hasLanded 플래그** — 단순한 bool 하나로 "착지 후 다음 미리보기" 자연스러운 페이스
6. **콜라이더 자동 fitting** — 픽셀 스캔으로 잎/줄기 제외한 본체 inscribed circle
7. **드롭 grace 처리** — `dropGrace(0.5초)` + `holdTime(5초)` 조합으로 오판 0
