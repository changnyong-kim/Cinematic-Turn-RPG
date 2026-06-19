# Cinematic Turn RPG

Unity 6 기반의 턴제 RPG 포트폴리오 프로젝트입니다.

Cinemachine, Timeline, Addressables를 활용하여 시네마틱 전투 연출과
유지보수가 용이한 클라이언트 구조 구현을 목표로 합니다.

------------------------------------------------------------------------

## 프로젝트 개요

본 프로젝트는 턴제 전투 시스템에 실시간 입력 및 연출 요소를 결합한 RPG
프로토타입입니다.

단순한 기능 구현보다 실제 게임 개발에서 사용 가능한 구조 설계에 중점을
두고 있습니다.

------------------------------------------------------------------------

## 기술 스택

-   Unity 6
-   C#
-   Addressables
-   Cinemachine
-   Timeline
-   UniTask
-   DOTween

------------------------------------------------------------------------

## 주요 기능

### Boot System

-   Addressables 초기화
-   Catalog 업데이트 확인
-   Remote Asset 다운로드 용량 체크
-   플랫폼별 초기화 Flow 구성

### Asset System

-   AssetManager 기반 에셋 관리
-   Addressable Asset 로드 및 해제 관리
-   Addressable 인스턴스 생성 및 반환 관리
-   Object Pool 시스템 구성

### Data System

-   JSON 기반 게임 테이블 로드
-   ScriptableObject 기반 로드 설정 관리
-   Addressable Key 기반 리소스 관리

### Battle System (개발 중)

-   Timeline 기반 스킬 연출 시스템
-   Cinemachine 기반 카메라 전환
-   턴제 전투 내 실시간 입력 및 상호작용 시스템

------------------------------------------------------------------------

## 프로젝트 구조

    Assets/_Game
    ├─ Scripts
    │  ├─ Core
    │  ├─ Intro
    │  ├─ Game
    │  └─ Common
    ├─ Prefabs
    ├─ Scene
    └─ Res

------------------------------------------------------------------------

## 개발 목표

-   유지보수 및 확장이 용이한 클라이언트 구조 설계
-   시네마틱 연출 기반의 턴제 전투 경험 구현
-   실무 수준의 Unity 클라이언트 프로그래밍 기술 정리 및 포트폴리오화
