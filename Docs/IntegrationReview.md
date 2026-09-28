# 아이템 · 적 체력바 1차 통합 검토

작업 브랜치: `integration/item-healthbar`  
별도 Unity 프로젝트: `D:\00_Works\00_U-Jam\item-healthbar-integration`  
Unity 버전: `6000.0.70f1`

## 이번 단계의 범위

- `Feature/ItemSystem` (`e64b108`)을 기준으로 `us-6.01` (`26672f6`)을 병합한다.
- 아이템 30종, 중앙 이벤트, 버프·속성 시스템, 구매·장착·스킬 슬롯 연결을 유지한다.
- EnemyHealthBar/Manager, 체력바 프리팹, Kog/Tee의 피격 플래시, ShootImpactFx를 유지한다.
- `ask`, `Feature/MapDesign`, `main` 반영은 사용자의 Unity 확인 이후 진행한다. 원본 프로젝트의 작업 브랜치는 변경하지 않는다.

## 충돌 해결

- `EnemyStatus`: 아이템의 버프 배율과 실제 감소한 체력 반환을 보존하고, 체력바가 구독하는 `DamageReported`를 추가했다. 초과 피해도 실제 피해량으로 통지한다.
- `PlayerShooter`: Ray 판정 직후 `Shooting`/`ShootingResolved`를 발행하고, 총알 도착 시 `OnShotLanded`와 적 도착 알림을 발행한다. 착탄 이펙트가 아이템 효과를 중복 실행하지 않는다. 벽 착탄도 통지한다.
- `_MainScene 1.unity`: us-6.01의 체력바·이펙트 연결을 검토용으로 보존했다. 삭제된 ItemEffectExecutor를 ItemPipelineTester로 교체하고 이미 스크립트가 사라진 컴포넌트 2개를 제거했다. 기존 `_MainScene.unity`는 그대로 유지했다.
- 아이템 브랜치에 남아 있던 잘못된 `.meta` 줄바꿈, Legacy의 ElementType 이름 충돌, Item/Element 별칭을 수정했다. Item namespace는 `Ujam.Runtime.Item`이다. 중복된 Legacy 씬 GUID는 Legacy 쪽에서 변경했다.

## Unity에서 확인하기

1. Unity Hub에서 위 별도 프로젝트를 열고 `Assets/_Project/Scenes/_MainScene 1.unity`를 연다.
2. Play를 누르면 ItemPipelineTester가 구매와 같은 Inventory API로 낙뢰(ID 11), 아드레날린(ID 13)을 장착한다.
3. 적이나 바닥을 향해 우클릭한다. 적 피해·체력바·피격 플래시, 총알 도착 위치의 적/환경 이펙트를 확인한다.
4. 적 근처 바닥을 가리키고 D를 눌러 낙뢰, F를 눌러 아드레날린을 확인한다. 스킬칸과 쿨타임도 확인한다. 일반형도 현재는 즉시 확정된다.
5. 다른 아이템을 시험하려면 Play를 종료하고 ItemPipelineTester의 Item Ids를 변경한다. 액티브는 최대 두 개이며 패시브 ID도 추가할 수 있다.

## 자동 검사

2026-09-28 Unity 6000.0.70f1 배치 실행 결과: C# 컴파일 오류 0개, 55개 검사 통과, 정상 종료 코드 0.

에디터 메뉴 `Tools > Ujam > Integration > Check item and healthbar merge`로 재실행한다.

검사 대상은 30개 카탈로그/Runtime 개체 분리, 실제 피해량과 체력바 통지, 사망 중복 방지, 적/벽/허공 착탄 통지, 착탄 시 아이템 이벤트 중복 발행 방지, 적 프리팹의 피격 플래시, 검토 씬의 스크립트 및 필수 참조다.

배치 실행 진입점은 `Ujam.Editor.Integration.ItemHealthbarMergeCheck.Run`이며 실행 로그는 `Logs/IntegrationCheck.log`에 있다. 이 검사는 에디터 검사이며 실제 플레이의 시각 효과와 조작감 확인은 위 수동 검토로 진행한다.

기존 Astar 패키지의 구형 API 경고와 검토 씬의 LightingData 호환 경고는 남아 있다. 조명 데이터는 이번 통합에서 다시 굽지 않았다.

## 다음 단계

Unity 확인 후 `ask`에서는 추가 이미지, UITitleManager, ASK-Scene만 선택 반영하고 `Feature/MapDesign`은 변경 사항 전체를 반영한다. 그 통합 결과를 확인한 다음 main에 반영한다.
