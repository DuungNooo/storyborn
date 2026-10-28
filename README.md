# STORYBORN v0.2

**기존 Unity 프로젝트에 가져와 실행하는 전투 프로토타입**입니다. 이미지는 임시 도형을 사용합니다.

## 다운로드 및 실행

**[Assets 직접 복사용 ZIP 다운로드](https://github.com/DuungNooo/storyborn/raw/refs/heads/main/Downloads/Storyborn_v0.2.1_Assets.zip)**

Unity 종료 → ZIP 압축 해제 → 안의 `Assets` 내용을 기존 프로젝트의 `Assets` 안에 복사(기존 Storyborn 파일 덮어쓰기) → Unity 열기 → **Storyborn → Play Prototype** → Game 뷰 클릭.

`Assets/Assets`가 되지 않도록 폴더 내용을 합칩니다. 이 방법은 Unity 패키지 가져오기 창을 사용하지 않습니다.

v0.2 패키지를 가져오다가 `PackageImportTreeView.RecursiveComputeEnabledStateForFolders` 오류가 난 경우 Import 창을 먼저 닫고 위 ZIP 방식을 사용해 주세요. 게임 코드는 동일하며 패키지 전달 구조를 보완했습니다.

[폴더 레코드 보완 패키지 v0.2.1](https://github.com/DuungNooo/storyborn/raw/refs/heads/main/Downloads/Storyborn_v0.2.1.unitypackage)도 제공합니다. 패키지 내부의 모든 상위 폴더 레코드·메타데이터를 검사했지만 실제 Unity 가져오기 검증은 아직 하지 못했습니다.

[한국어 상세 안내](README_KO.md) · [검증 기록](VALIDATION_KO.md)

## 조작

- **우클릭 공격 / 좌클릭 무적 대시**
- **A/D 좌우 이동 / W 점프 / S 발판 하강**
- **Q 주변 충격파 / E 전방 투사체** — 시험용 스킬
- Esc 일시정지, R 재시작

**증강·강화 선택은 없습니다.** 적을 정리한 뒤 출구로 걸어가면 다음 구간으로 이동합니다.

기준 버전은 Unity 2022.3 LTS입니다. 기존 Input Manager와 새 Input System을 위한 입력 처리를 포함합니다. 런타임 코드 API 참조 컴파일과 규칙 검사 23개는 통과했지만, Unity 에디터 가져오기·실제 플레이·Windows 빌드는 미검증입니다.

이 파일은 Unity 프로젝트용 패키지이며 설치된 게임의 EXE 옆에 넣는 모드는 아닙니다. Windows EXE는 Unity에서 **Storyborn → Build Windows x64**로 생성할 수 있습니다.
