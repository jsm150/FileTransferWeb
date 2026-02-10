# Storage 도메인 규칙

## 단일 진입점

- `DirectoryListing(StoragePathPolicy policy, IReadOnlyList<string> directoryNames)` 생성자가 `ListDirectories` 유스케이스의 도메인 규칙 단일 진입점이다.
- `StoragePathPolicy`는 생성자에서 경로 검증/정규화를 완료한 정책 객체여야 한다.

## 계층 책임

- 핸들러(`Application`):
  - 포트로 데이터 조회
  - `StoragePathPolicy` 생성
  - `DirectoryListing` 생성 후 결과 반환
- 도메인(`Domain`):
  - 경로 경계 검증
  - 현재/부모 상대 경로 계산
  - 디렉터리 이름 검증
  - 이름 오름차순 정렬
  - 자식 상대 경로 계산

## 핸들러 금지 사항

- 정렬 수행 금지
- 부모/자식 경로 계산 금지
- 도메인 분기(if/switch) 금지

## 도메인 소유 규칙

1. 상대 경로만 허용한다.
2. 업로드 루트 밖 경로 접근을 차단한다.
3. 루트/하위 경로의 부모 상대 경로를 일관되게 계산한다.
4. 디렉터리 이름은 비어 있을 수 없다.
5. 디렉터리 목록은 대소문자 무시 오름차순으로 정렬한다.
