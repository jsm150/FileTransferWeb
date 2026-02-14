# Transfer 도메인 규칙

## 도메인 소유 규칙

1. Transfer 배치의 상태 전이는 `TransferBatch` 집합 루트 메서드로만 수행한다.
2. `TransferBatch`는 `Collecting` 상태에서 완료 업로드 등록과 마무리 처리를 허용한다.
3. 배치 마무리는 “예상 파일 수가 모두 도착했는지”를 선행 조건으로 강제하지 않는다.
4. 배치 마무리 결과가 전부 성공이고 결과 수가 예상 파일 수와 같으면 `Completed` 상태가 된다.
5. 배치 마무리에서 일부만 성공하거나 예상 수보다 적게 처리되면 `PartiallyCompleted` 상태가 된다.
6. 배치 마무리에서 성공 건이 하나도 없으면 `Failed` 상태가 된다.
7. 파일명 충돌 시 `UploadFileNamePolicy`가 단일 스캔 분석 결과로 `name (n).ext` 규칙의 최소 사용 가능 번호를 결정한다.
8. 같은 요청 내 중복 파일도 정책 내부 예약 상태로 즉시 점유 처리한다.
9. 파일명 정규화(`Path.GetFileName`, 공백 시 기본 이름 대체)는 도메인 정책이 담당한다.
10. 배치 단위 영속화 포트는 도메인 계층(`ITransferBatchRepository`)에 정의한다.
11. 도메인 계층에서 발생하는 예외는 `TransferDomainException`으로 통일한다.
12. Transfer 도메인은 도메인 이벤트를 발행하지 않는다.

## 계층 경계 규칙

1. tus 완료 등록 유스케이스의 애플리케이션 진입점은 `RegisterCompletedTusUploadCommandHandler` 단일 경로를 사용한다.
2. 배치 마무리 유스케이스의 애플리케이션 진입점은 `FinalizeTransferBatchCommandHandler` 단일 경로를 사용한다.
3. Application 계층은 오케스트레이션만 수행하고 비즈니스 분기를 직접 판단하지 않는다.
4. Web/Infrastructure 계층은 프로토콜 처리와 파일 I/O 같은 기술 책임만 가진다.
5. 포트 인터페이스는 도메인 계층에 위치시키고, 구현은 인프라 계층에서 제공한다.
