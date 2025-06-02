# GPIMSWeb - Equipment Monitoring System

ASP.NET Core MVC 기반의 실시간 장비 모니터링 웹 시스템

## 주요 기능

### 사용자 관리
- 사용자 등급별 접근 권한 관리 (Admin, Maintenance, Operate)
- 사용자 생성, 수정, 삭제
- 사용자별 활동 이력 관리

### 장비 모니터링
- 실시간 장비 상태 모니터링
- 채널별 데이터 표시 (전압, 전류, 용량, 전력 등)
- CAN/LIN 데이터 모니터링
- AUX 센서 데이터 (전압, 온도, NTC)
- 실시간 데이터 차트

### 알람 시스템
- 실시간 알람 알림
- 알람 레벨별 분류
- 알람 해제 기능

### 장비 업데이트
- 원격 프로그램 업데이트
- 장비 상태별 업데이트 가능 여부 확인

### 시스템 설정
- 장비 및 채널 수 설정
- 다국어 지원 (영어, 한국어, 중국어)
- 날짜 형식 설정

## 기술 스택

- **Backend**: ASP.NET Core 7.0 MVC
- **Database**: SQL Server LocalDB
- **ORM**: Entity Framework Core
- **Real-time**: SignalR
- **Authentication**: Cookie Authentication
- **Password Hashing**: BCrypt
- **Frontend**: Bootstrap 5, jQuery
- **Icons**: Font Awesome

## 설치 및 실행

1. 프로젝트 클론 또는 다운로드
2. Visual Studio 또는 VS Code에서 프로젝트 열기
3. Package Manager Console에서 마이그레이션 실행:
   ```
   Add-Migration InitialCreate
   Update-Database
   ```
4. 프로젝트 실행 (F5 또는 dotnet run)

## 기본 계정

- **Username**: admin
- **Password**: admin123
- **Level**: Admin

## API 엔드포인트 (클라이언트 연동용)

- `POST /api/ClientData/channel` - 채널 데이터 업데이트
- `POST /api/ClientData/canlin` - CAN/LIN 데이터 업데이트
- `POST /api/ClientData/aux` - AUX 센서 데이터 업데이트
- `POST /api/ClientData/alarm` - 알람 추가

## 프로젝트 구조

```
GPIMSWeb/
├── Controllers/         # MVC 컨트롤러
├── Models/             # 데이터 모델
├── Services/           # 비즈니스 로직 서비스
├── Views/              # Razor 뷰
├── Hubs/               # SignalR 허브
├── Data/               # Entity Framework DbContext
└── wwwroot/            # 정적 파일 (CSS, JS, 이미지)
```

## 라이선스

이 프로젝트는 MIT 라이선스 하에 배포됩니다.