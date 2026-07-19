// 카탈로그 조회/검색 도구(SPEC.md §5). .NET CatalogTools 와 동일한 규칙/출력 형태.
// 출력의 선택 필드는 undefined 로 두어 JSON.stringify 가 생략하게 한다(=.NET 의 WhenWritingNull).
import { getCatalog, iconUrlFor, type CatalogService } from "../catalog.js";
import { strings } from "../generated.js";

const SEP = /[ ,\t\n\r]+/;
const clamp = (n: number, lo: number, hi: number) => Math.max(lo, Math.min(hi, n));
const blank = (s: string | undefined | null) => !s || s.trim().length === 0;
// OrdinalIgnoreCase 근사(소문자화 후 UTF-16 코드유닛 비교).
const ordIgnoreCase = (a: string, b: string) => {
  const x = a.toLowerCase(), y = b.toLowerCase();
  return x < y ? -1 : x > y ? 1 : 0;
};

export async function searchServices(query: string, category: string | null | undefined, limit: number) {
  const doc = await getCatalog();
  const tokens = [...new Set((query ?? "").split(SEP).filter((t) => t.length > 0).map((t) => t.toLowerCase()))];

  let pool: CatalogService[] = doc.services;
  if (!blank(category)) pool = pool.filter((s) => s.category.toLowerCase() === category!.toLowerCase());

  const scored = pool
    .map((s) => ({ svc: s, score: tokens.length === 0 ? 1 : tokens.filter((t) => s.searchHaystack.includes(t)).length }))
    .filter((x) => x.score > 0)
    .sort((a, b) => b.score - a.score || ordIgnoreCase(a.svc.displayName, b.svc.displayName))
    .slice(0, clamp(limit, 1, 50))
    .map((x) => ({
      id: x.svc.id,
      displayName: x.svc.displayName,
      displayNameEn: x.svc.displayNameEn,
      category: x.svc.category,
      url: x.svc.url,
      requiredPackages: x.svc.packages.map((p) => p.name),
      compatWarning: blank(x.svc.compatNotes) ? undefined : x.svc.compatNotes,
    }));

  return {
    query: query ?? "",
    category: category ?? undefined,
    totalServices: doc.services.length,
    matched: scored.length,
    results: scored,
    note: strings.tools.search_services.resultNote,
  };
}

export async function getService(id: string) {
  const doc = await getCatalog();
  const s = doc.services.find((x) => x.id === id);
  if (!s) {
    return {
      error: strings.tools.get_service.errorNotFound.replace("{id}", id),
      hint: strings.tools.get_service.hintNotFound,
    };
  }
  return {
    id: s.id,
    displayName: s.displayName,
    displayNameEn: s.displayNameEn,
    category: s.category,
    url: s.url,
    iconUrl: iconUrlFor(s.id),
    packages: s.packages.map((p) => ({ name: p.name, url: p.url, arguments: p.arguments })),
    edgeExtensions: s.edgeExtensions.map((e) => ({ name: e.name, extensionId: e.extensionId, crxUrl: e.crxUrl })),
    searchKeywords: s.searchKeywords,
    compatNotes: s.compatNotes,
  };
}

export async function listCategories() {
  const doc = await getCatalog();
  const map = new Map<string, { category: string; count: number }>();
  for (const s of doc.services) {
    const key = s.category.toLowerCase();
    const e = map.get(key);
    if (e) e.count++;
    else map.set(key, { category: s.category, count: 1 });
  }
  const categories = [...map.values()].sort((a, b) => b.count - a.count); // 동점은 첫 등장 순서 유지
  return { totalServices: doc.services.length, categories };
}

export async function listCompanions(query: string | null | undefined) {
  const doc = await getCatalog();
  const q = (query ?? "").toLowerCase().trim();
  const companions = doc.companions
    .filter((c) => q.length === 0 || c.searchHaystack.includes(q))
    .map((c) => ({ id: c.id, displayName: c.displayName, displayNameEn: c.displayNameEn, url: c.url }));
  return { matched: companions.length, companions };
}
