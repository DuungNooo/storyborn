# v0.2 검증 기록

- 런타임 C#을 UnityEngine 모듈 API 참조(2021.3.33) 및 .NET 8에 연결해 컴파일: 오류 0, 경고 0. 기본 입력 백엔드 경로 확인.
- 실제 CombatRules 원본을 사용하는 독립 검사 23개 통과: 대시 연속 입력·제한·초기화, 공격 방향·높이·거리, 충돌 겹침, 스킬 재사용·연속 입력 거절·초기화.
- 소스에서 증강 선택 상태·보상 코드 및 전역 런타임 자동 생성을 제거했는지 확인.
- 샘플 씬의 StoryGame 컴포넌트 참조 GUID, 패키지 안의 asset/asset.meta/pathname, Assets ZIP 무결성 확인.

실제 Unity 에디터는 실행 환경에 없습니다. Unity 2022.3에서 가져오기·화면 표시·입력·전투·씬 전환·Windows 빌드는 미검증입니다. 새 Input System 분기는 입력 어댑터 소스에 포함했지만 해당 패키지와의 실제 컴파일·플레이 확인이 남아 있습니다.

로컬 점검: 패키지 Import → Storyborn/Play Prototype → Game 뷰 클릭 → WASD, 좌/우클릭, Q/E → 쿨다운 → 적 전멸 → 출구 자동 진입 → 사망/R → Esc. 기존 작업 씬에서도 게임이 자동 생성되지 않는지 확인하세요.

.NET 8이 있다면 저장소에서 `dotnet run --project Validation/Rules.csproj`로 핵심 규칙 검사를 재실행할 수 있습니다.
