# STORYBORN v0.2

**기존 Unity 프로젝트에 가져와 실행하는 전투 프로토타입**입니다. 이미지는 임시 도형을 사용합니다.

## 다운로드 및 실행

**[Unity 가져오기 패키지 다운로드](https://github.com/DuungNooo/storyborn/raw/refs/heads/main/Downloads/Storyborn_v0.2.unitypackage)**

기존 Unity 프로젝트 열기 → 다운로드한 `.unitypackage`를 Project 창에 드래그 → **Import** → 상단 **Storyborn → Play Prototype** → Game 뷰 클릭.

새 프로젝트나 설정 파일 교체는 필요하지 않습니다. 다른 작업 씬에는 게임이 자동으로 생성되지 않습니다.

[Assets 폴더 ZIP](https://github.com/DuungNooo/storyborn/raw/refs/heads/main/Downloads/Storyborn_v0.2_Assets.zip) · [한국어 상세 안내](README_KO.md) · [검증 기록](VALIDATION_KO.md)

## 조작

- **우클릭 공격 / 좌클릭 무적 대시**
- **A/D 좌우 이동 / W 점프 / S 발판 하강**
- **Q 주변 충격파 / E 전방 투사체** — 시험용 스킬
- Esc 일시정지, R 재시작

**증강·강화 선택은 없습니다.** 적을 정리한 뒤 출구로 걸어가면 다음 구간으로 이동합니다.

기준 버전은 Unity 2022.3 LTS입니다. 기존 Input Manager와 새 Input System을 위한 입력 처리를 포함합니다. 런타임 코드 API 참조 컴파일과 규칙 검사 23개는 통과했지만, Unity 에디터 가져오기·실제 플레이·Windows 빌드는 미검증입니다.

이 파일은 Unity 프로젝트용 패키지이며 설치된 게임의 EXE 옆에 넣는 모드는 아닙니다. Windows EXE는 Unity에서 **Storyborn → Build Windows x64**로 생성할 수 있습니다.
