# test — Unity 프로젝트 저장소 안내

## 프로젝트 기본 정보
- Unity version: `6000.0.70f1` (`0d9e1a373c8b`)
- 위치: `D:/00_Works/00_U-Jam/unity`

## Assets 최상위 디렉터리

### Assets/_Project
- 목적: 프로젝트의 외부 Package 원본이 아닌, 프로젝트에 맞게 가공 혹은 생성한 것들을 저장한다.
- Runtime / Editor / Test: Runtime 중심이며 `Art/Crosshairs/Editor/`에 Editor 코드가 있다. Unity Test Framework용 Test assembly는 확인되지 않았다.
- 소유자: 프로젝트 팀

### Assets/AstarPathfindingProject
- 목적: A* Pathfinding Project의 Runtime pathfinding 코드, Editor 도구, 예제 Scene과 문서를 제공한다.
- Runtime / Editor / Test: Runtime과 Editor가 분리되어 있으며 예제 Scene을 포함한다. 프로젝트 Test 위치로 사용되는 근거는 확인되지 않았다.
- 소유자: 외부 Asset 제공자; 로컬 수정 정책은 UNKNOWN
- asmdef: `AstarPathfindingProject.asmdef`, `Editor/AstarPathfindingProjectEditor.asmdef`, `PackageTools/Editor/PackageToolsEditor.asmdef`

### Assets/Hovl Studio
- 목적: Hovl Studio Magic effects pack의 VFX Prefab, Material, Texture와 Demo Scene을 제공한다.
- Runtime / Editor / Test: Runtime VFX Asset과 Demo 자료; Test assembly는 확인되지 않았다.
- 소유자: 외부 Asset 제공자; 로컬 수정 정책은 UNKNOWN
- asmdef: 확인되지 않음

### Assets/Polytope Studio
- 목적: Low-poly environment model, Prefab, Material, Demo Scene과 Welcome Screen Editor 도구를 제공한다.
- Runtime / Editor / Test: Runtime environment Asset과 `Welcome_Screen/Editor/`의 Editor 코드; Test assembly는 확인되지 않았다.
- 소유자: 외부 Asset 제공자; 로컬 수정 정책은 UNKNOWN
- asmdef: 확인되지 않음

### Assets/Settings
- 목적: URP renderer·pipeline, Volume profile과 Input System action 설정 Asset을 둔다.
- 확인된 항목: PC·Mobile renderer와 RPAsset, `InputSystem_Actions.inputactions`, Volume profile, URP global settings
- Runtime / Editor / Test: 프로젝트 설정 Asset; Test code 없음
- 소유자: 프로젝트 팀으로 추정하지 않음 — 명시된 소유자는 UNKNOWN
- asmdef: 해당 없음

### Assets/TextMesh Pro
- 목적: TextMesh Pro의 Font, Material, Shader, Sprite와 line-breaking Resource를 제공한다.
- Runtime / Editor / Test: Runtime Resource와 Shader 중심; Test assembly는 확인되지 않았다.
- 소유자: Unity TextMesh Pro 제공 자료; 프로젝트의 로컬 수정 정책은 UNKNOWN
- asmdef: 확인되지 않음

### Assets/ThirdParty
- 목적: 포함된 `readme.md`에 따라 외부 Plugin 또는 Asset Store 자료를 모으기 위한 위치다.
- 현재 내용: `readme.md`만 확인됨
- Runtime / Editor / Test: 기능 Asset 없음
- 소유자: 개별 외부 자료에 따라 결정; 현재 해당 없음
- asmdef: 없음

### Assets/VFXPACK_FIRE_WALLCOEUR
- 목적: Fire VFX Prefab, Material, Texture, Volume Asset과 Demo Scene을 제공한다.
- Runtime / Editor / Test: Runtime VFX Asset과 Demo Scene; Test assembly는 확인되지 않았다.
- 소유자: 외부 Asset 제공자; 로컬 수정 정책은 UNKNOWN
- asmdef: 확인되지 않음

## 코드와 데이터 탐색
- First-party Runtime code: `Assets/_Project/_Scripts/`
- First-party Editor code: `Assets/_Project/Art/Crosshairs/Editor/`
- 추가 Runtime 실험 스크립트: `Assets/_Project/_FPS_Test/`
- Tests: UNKNOWN — `Assets/` 아래에 `Tests`, `EditMode`, `PlayMode` 디렉터리나 Test asmdef가 확인되지 않았다. 이름에 Test가 있는 `EnemyTest.cs`와 `TestMove.cs`는 Unity Test Framework test가 아니라 `MonoBehaviour`다.
- Scenes: `Assets/_Project/Scenes/`; 외부 Asset의 Demo·Example Scene은 각 외부 Asset 디렉터리에 있다.
- Prefabs: 주로 `Assets/_Project/_Scripts/`, `Assets/_Project/Prefabs/`와 각 외부 Asset 디렉터리에 분산되어 있다.
- ScriptableObject 정의: `Assets/_Project/_Scripts/Enemy/WaveData.cs`, `Assets/_Project/_Scripts/System/Element/Element.cs`
- 확인된 ScriptableObject Asset: `Assets/_Project/_Scripts/Enemy/Wave01.asset`, `Assets/_Project/_Scripts/System/Element/*.asset`
- Third-party: `Assets/AstarPathfindingProject/`, `Assets/Hovl Studio/`, `Assets/Polytope Studio/`, `Assets/TextMesh Pro/`, `Assets/VFXPACK_FIRE_WALLCOEUR/`; 개별 수정 정책은 UNKNOWN