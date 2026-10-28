# STORYBORN v0.2.2 — 기존 Unity 프로젝트에 넣는 버전

## 가져오기 창에서 NullReferenceException이 발생한 경우

`PackageImportTreeView.RecursiveComputeEnabledStateForFolders`는 Unity 패키지 가져오기 창의 폴더 처리에서 난 오류입니다. v0.2.2는 ZIP 직접 복사 방식으로 제공하며 그 가져오기 창을 사용하지 않습니다.

1. Import 창을 Cancel/닫기로 닫고 Play를 중지합니다. Unity를 종료합니다.
2. `Storyborn_v0.2.2_Assets.zip`을 일반 폴더에 압축 해제합니다.
3. 압축을 푼 폴더 **안의 Assets 내용**을 기존 프로젝트의 **Assets 안에** 복사합니다. Scripts, Scenes, Editor와 안내 파일이 해당 폴더로 합쳐지게 합니다. `Assets/Assets`로 중첩하지 않습니다. 기존 Storyborn 파일은 덮어씁니다.
4. Unity를 다시 열고 가져오기·컴파일이 끝날 때까지 기다립니다.
5. `Storyborn → Play Prototype`을 누르고 Game 뷰를 클릭합니다.

기준 에디터: **Unity 2022.3 LTS**. 기존 Input Manager와 새 Input System 설정을 모두 지원하도록 입력 어댑터를 넣었습니다. Unity 에디터에서 실제 가져오기와 플레이 검증은 아직 하지 못했습니다.

## 조작

| 입력 | 동작 |
|---|---|
| A / D | 좌우 이동 |
| W | 점프. Space도 사용 가능 |
| S | 발판에서 아래로 내려가기 |
| 마우스 좌클릭 | 근접 공격 |
| 마우스 우클릭 | 무적 대시. WASD를 누르면 그 방향으로 대시, 방향 입력이 없으면 바라보는 방향 |
| Q | 임시 스킬: 주변 충격파, 피해 3, 반경 4, 재사용 4초 |
| E | 임시 스킬: 전방 투사체, 피해 2, 재사용 2초 |
| Esc | 일시정지 / 계속 |
| R | 사망·클리어 후 재시작 |

횡스크롤 구조이므로 W/S는 자유 비행 대신 점프·발판 하강으로 동작합니다. Q/E의 구체적인 기술은 미정이라 전투 시험용으로 구현했습니다. 재사용 시간은 일시정지 중 흐르지 않으며 새 구간·새 도전에서 초기화됩니다.

## 현재 버전의 변경점

- 좌클릭 공격 / 우클릭 대시로 변경. 기존 J·Shift·K 전투 입력 제거.
- WASD 이동, W 점프, S 발판 하강. Q/E 스킬 추가 및 재사용 시간 표시.
- **증강·강화 선택·무작위 보상·강화로 능력치가 변하는 흐름을 모두 제거.**
- 적을 전멸시키고 오른쪽 출구로 걸어가면 자동으로 다음 구간으로 이동. E는 스킬 전용.
- 기존 Unity 프로젝트에 넣을 수 있는 Assets 전용 ZIP 제공.
- 샘플 씬에 게임 컴포넌트를 직접 연결. 다른 씬을 실행할 때 자동 생성하던 전역 초기화 제거.

## 전투

세 구간을 진행하며 마지막 구간의 시험용 보스를 처치하고 출구로 걸어가면 클리어합니다. 적의 주황 전조·빨간 공격·보라 회복을 관찰해 대시와 위치 선정으로 피하세요. 패링은 없습니다. 짧은 간격의 대시 2회 후 0.5초 동안 추가 대시가 제한됩니다. 사망하면 체력과 진행이 초기화되며 누적 사망 횟수만 로컬에 남습니다. 회복 보상은 없습니다.

## 파일

- `Assets/Scripts/StoryGame.cs`: 전투와 진행, 시험용 도형 렌더링·UI.
- `Assets/Scripts/Controls.cs`: 마우스·키보드 입력, 입력 백엔드 호환.
- `Assets/Scripts/CombatRules.cs`: 대시·스킬 재사용·공격·겹침 규칙.
- `Assets/Scenes/Prototype.unity`: 실행 가능한 샘플 씬.
- `Assets/Editor/PrototypeSetup.cs`: 열기·실행·검사·Windows 빌드 메뉴.

## 실행 파일과 구분

이 패키지는 **Unity 에디터 프로젝트에 가져오는 파일**입니다. 설치된 게임의 EXE 옆이나 Data 폴더에 C#·unitypackage를 넣는 방식으로 실행되지는 않습니다. 설치된 게임에 추가하는 형태를 원하면 해당 게임과 모드 로딩 방식에 맞춘 별도 작업이 필요합니다.

Windows 실행 파일은 Unity Hub에서 Windows Build Support를 설치하고 **Storyborn → Build Windows x64**로 만듭니다. EXE만 옮기지 말고 생성된 빌드 폴더 전체를 함께 배포합니다.
