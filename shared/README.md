# shared/ — 구현 간 단일 진실 원천

이 폴더는 **언어 중립 리소스**를 담습니다. TableCloth MCP 의 .NET 구현과 Node 구현이
**동일한 내용을 소비**해 드리프트(구현 간 불일치)를 막는 것이 목적입니다.

| 파일 | 내용 | 소비 방식 |
| --- | --- | --- |
| `strings.json` | 서버 지침, 도구 title/description/파라미터 설명, 각종 note/hint, `securityNote` 등 모든 프롬프트/문자열 | .NET: 어셈블리에 임베드해 런타임 로드 / Node: 직접 import |
| `wsb-template.xml` | 생성/실행되는 `.wsb` 의 정본 템플릿(`__SITEIDS__` 치환점 1개) | 양쪽 구현이 로드 후 `__SITEIDS__` 치환 |

## 규칙

1. **프롬프트/문자열은 여기서만 수정한다.** 코드에 새 사용자 노출 문자열을 하드코딩하지 않는다.
2. `wsb-template.xml` 의 `LogonCommand` 는 본 TableCloth 리포의 정본 `no-install-spork.wsb` 와
   동일하게 유지한다. 명령 하드닝은 [#1](https://github.com/yourtablecloth/TableClothMcp/issues/1) 에서 정본과 함께 진행한다.
3. 두 구현은 **conformance 테스트**로 이 파일들과의 일치를 검증한다(SPEC.md의 "검증 전략" 참조).

## 언어별 제약 메모

- C# 의 도구 `Description`/`Title`/파라미터 설명은 **attribute 상수**라 런타임 로드가 불가하다.
  따라서 그 문자열은 코드에도 존재하며, `strings.json` 을 정본으로 두고 **conformance 테스트가 일치를 강제**한다.
  나머지(서버 지침, note/hint, `securityNote`, `.wsb` 템플릿)는 C# 도 런타임에 이 파일에서 직접 읽는다.
- Node 는 제약이 없어 `strings.json` 을 그대로 import 해 title/description 까지 전부 여기서 가져온다.
