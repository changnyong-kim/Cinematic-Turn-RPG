# Cinematic Turn RPG Combat System
**Timeline 기반 턴제 전투와 실시간 패링 시스템**

Unity 6.4로 제작한 Timeline / Cinemachine 중심의 턴제 RPG 전투 포트폴리오입니다.  
명령 선택형 전투에 실시간 패링 입력을 결합하고, Timeline에서 조정한 연출 시점과 실제 전투 규칙을 분리했습니다.

## 시연 영상

[YouTube Demo](https://www.youtube.com/watch?v=CdqxunqTpkw)

> **Key Focus**  
> Timeline Signal → BattleResult → Hit / Parry 연출 분기

| 항목 | 내용 |
|---|---|
| 프로젝트 형태 | 1인 개인 프로젝트 |
| 구현 범위 | 기획, 클라이언트, 전투 시스템, UI, 연출 |
| 엔진 | Unity 6.4 / URP |
| 플랫폼 | Windows PC |
| 전투 구성 | 1:1 턴제 전투 + 실시간 패링 |
| 핵심 기술 | C#, Timeline, Cinemachine, DOTween, UniTask |

## 스크린샷

### 전투 시작 / 기본 UI
![Battle Start](Docs/Images/01_BattleStart.png)

### 패링 성공 / 반격 시퀀스
![Parry Success](Docs/Images/02_ParrySuccess.png)

### 전투 플레이 흐름

스킬 사전 알림부터 패링 입력, 성공·실패 분기, 패링 불가능 공격과 스턴 상태이상까지의 실제 전투 흐름입니다.

![Combat Flow](Docs/Images/03_CombatFlow.png)

### Timeline 기반 전투 시퀀스

공격 애니메이션, 사전 알림, 패링 가능 구간, Impact Signal과 반격 시퀀스의 타이밍을 Timeline에서 조정합니다.

![Timeline Sequence](Docs/Images/04_TimelineSequence.png)

### 전투 책임 분리 구조

입력과 시퀀스 조율, 전투 규칙, UI 상태, 시네마틱 연출을 서로 다른 책임 경계로 분리했습니다.

![Battle Architecture](Docs/Images/05_BattleArchitecture.png)

## 프로젝트 개요

이 프로젝트는 턴제 RPG 전투를 기반으로 하되, 단순한 명령 선택 방식이 아니라  
공격 연출 중 특정 타이밍에 실시간 패링 입력이 가능한 구조를 목표로 제작했습니다.

전투 로직, UI 상태, Timeline 연출 제어를 각각 분리하여  
전투 규칙 변경이나 연출 추가가 한 클래스에 집중되지 않도록 구성했습니다.

## 전투 플레이 흐름

1. 스킬 데이터에 따라 공격 타입과 사전 알림을 표시합니다.
2. Timeline Signal이 패링 입력 가능 구간을 엽니다.
3. 플레이어가 해당 구간에 패링을 요청합니다.
4. Impact Signal 시점에 `BattleModel`이 실제 결과를 판정합니다.
5. 반환된 `BattleResult`에 따라 Hit 또는 Parry 연출로 분기합니다.
6. 공격 종료 후 캐릭터 위치와 카메라를 복귀시키고 다음 턴으로 전환합니다.

## 주요 구현 기능

### Timeline Signal 기반 전투 연출

공격, 피격, 패링, 반격 시퀀스를 Timeline으로 구성하고,  
Timeline Signal 시점에 실제 전투 판정과 연출 분기를 연결했습니다.

- 공격 Impact 시점에 데미지 및 패링 판정 처리
- 패링 성공 시 몬스터 공격 Timeline 중단
- 플레이어 반격 Timeline으로 전환
- Hit Stop, 카메라 연출, 피격 반응 연동

### 테이블 기반 캐릭터 / 스킬 데이터

캐릭터와 스킬 정보를 테이블 데이터 기반으로 구성했습니다.

- 캐릭터 HP / 공격력 / 프리팹 키 관리
- 스킬별 데미지 배율 관리
- 스킬별 패링 가능 여부 처리
- 스킬별 상태이상 적용

### 턴 / 상태이상 시스템

전투 상태와 턴 흐름은 BattleModel에서 관리합니다.

- PlayerTurn / MonsterTurn / Win / Lose 상태 관리
- 스턴 상태이상 처리
- 상태이상에 따른 턴 스킵
- 패링 요청 가능 구간 관리

### ViewModel 기반 전투 UI

전투 UI는 ViewModel 상태 변경을 통해 갱신되도록 구성했습니다.

- HP 표시
- 턴 텍스트 표시
- 공격 / 패링 버튼 활성화 제어
- Command UI / Turn UI Fade 처리
- UniRx 없이 경량 ObservableValue 사용

### Addressables 기반 캐릭터 생성

캐릭터 프리팹은 테이블의 PrefabKey를 기준으로 Addressables를 통해 생성합니다.

- 테이블 데이터 기반 캐릭터 선택
- Addressables InstantiateAsync 기반 생성
- AssetManager를 통한 리소스 접근 통합

### Assembly Definition 기반 코드 분리

Core, Battle, Intro 등 기능 단위로 Assembly Definition을 적용하여  
코드 의존성과 재컴파일 범위를 분리했습니다.

## 구조

```text
BattleController
 ├─ 플레이어 입력 처리
 ├─ 몬스터 행동 선택
 ├─ 스킬 실행 흐름 제어
 ├─ 턴 전환 처리
 └─ BattleModel / BattleCinematicDirector / BattleViewModel 연결

BattleModel
 ├─ 전투 상태 관리
 ├─ 데미지 계산
 ├─ 패링 판정
 ├─ 상태이상 적용
 └─ 턴 스킵 처리

BattleCinematicDirector
 ├─ Timeline 재생 제어
 ├─ Timeline Signal 처리
 ├─ 공격 / 피격 / 패링 / 반격 연출 연결
 ├─ Hit Stop 처리
 └─ 카메라 연출 제어

BattleViewModel
 ├─ HP View Data
 ├─ Turn Text State
 ├─ Skill Notice Text
 ├─ Button Interactable State
 └─ UI Visible State

UIBattleView
 ├─ ViewModel 바인딩
 ├─ 실제 UI 반영
 ├─ Button Event 전달
 └─ CanvasGroup Fade 처리
 ```

## 기술 스택

- Unity 6
- C#
- Timeline
- Cinemachine
- Addressables
- Assembly Definition
- UniTask
- DOTween
- UGUI / TextMeshPro
- Newtonsoft.Json
- JSON Table Data

## 현재 범위와 개선 방향

현재 프로젝트는 핵심 전투 경험과 구조 검증에 집중한 **1:1 전투 프로토타입**입니다.

## 개선 예정
- 몬스터 행동 선택 정책과 전투 시나리오 데이터 분리
- 공격·피격·반격 Timeline에 Cinemachine Track / Shot Clip 적용 확대
- Camera / Movement / Hit Stop / VFX 실행 책임 세분화
- 다대다 전투와 다중 타겟 구조 확장
- BattleModel 상태 전이 단위 테스트와 Timeline Signal 검증 도구 추가


## 추가 예정
- 입력 장치별 QTE / 패링 UI 분기

## 리소스 안내

게임 내 캐릭터, 애니메이션, 배경, VFX 및 사운드 일부는 Unity Asset Store와 외부 무료 리소스를 활용했습니다.  
프로젝트의 전투 시스템, 데이터 처리, UI 흐름, Timeline 연동과 시네마틱 제어 코드는 개인 작업으로 구현했습니다.