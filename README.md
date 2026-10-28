# STORYBORN — Unity 전투 프로토타입

이미지 대신 임시 도형으로 만든 2D 횡스크롤 액션 프로토타입입니다.

## 다운로드

[Unity 프로젝트 ZIP 다운로드](https://github.com/DuungNooo/storyborn/raw/refs/heads/main/Downloads/Storyborn_Unity_Prototype_v0.1.zip)

이 파일은 **Unity 소스 프로젝트**입니다. Windows EXE는 포함하지 않습니다.

## 실행

1. ZIP을 압축 해제합니다.
2. Unity Hub에서 Unity 2022.3.62f1을 설치하고 `Storyborn` 폴더를 추가합니다.
3. `Assets/Scenes/Prototype.unity`를 열고 Play → Enter를 누릅니다.

A/D 이동 · Space 점프 · J 공격 · Shift/K 무적 대시 · E 다음 방 · Esc 일시정지 · R 재시작.

## Windows 실행 파일 만들기

Unity Hub에서 Windows Build Support를 설치한 뒤 에디터 메뉴 **Storyborn → Build Windows x64**에서 출력 폴더를 선택합니다. 생성된 EXE와 함께 Data 폴더·DLL 등 **빌드 폴더 전체**를 배포해야 합니다.

[한국어 상세 안내](README_KO.md) · [검증 기록](VALIDATION_KO.md)

이동·점프·지상/공중 대시, 근접/원거리 적, 3개 전투 구간과 보스, 강화 선택, 사망 후 재도전을 구현했습니다. 게임명·무기·적·성장 방식은 시험용 선택입니다.

런타임 코드 API 참조 컴파일과 규칙 검사 18개는 통과했습니다. 실제 Unity 에디터 플레이와 Windows 빌드는 아직 검증하지 못했습니다.
