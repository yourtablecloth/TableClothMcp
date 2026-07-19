import { XMLParser } from "fast-xml-parser";

// 공개 카탈로그(yourtablecloth.app)를 받아 파싱하고 메모리에 캐시한다(SPEC.md §6).
// .NET 의 CatalogClient 와 동일한 파싱/검색 규칙을 재현한다.

export const CATALOG_URL = "https://yourtablecloth.app/TableClothCatalog/Catalog.xml";
export const ICON_BASE_URL = "https://yourtablecloth.app/TableClothCatalog/images";
const CACHE_TTL_MS = 30 * 60 * 1000;

export interface CatalogPackage { name: string; url: string; arguments: string; }
export interface CatalogEdgeExtension { name: string; extensionId: string; crxUrl: string; }
export interface CatalogService {
  id: string; displayName: string; displayNameEn?: string; category: string; url: string;
  packages: CatalogPackage[]; edgeExtensions: CatalogEdgeExtension[];
  searchKeywords?: string; compatNotes?: string; searchHaystack: string;
}
export interface CatalogCompanion {
  id: string; displayName: string; displayNameEn?: string; url: string; arguments?: string; searchHaystack: string;
}
export interface CatalogDocument { services: CatalogService[]; companions: CatalogCompanion[]; }

const parser = new XMLParser({
  ignoreAttributes: false,
  attributeNamePrefix: "@_",
  parseAttributeValue: false,
  ignoreDeclaration: true,
  ignorePiTags: true, // <?xml-stylesheet?> 같은 처리 명령을 무시(루트 오인 방지)
  trimValues: true,
});

const attr = (node: any, name: string): string | undefined =>
  node && typeof node === "object" ? node[`@_${name}`] : undefined;

function elemText(node: any, name: string): string | undefined {
  const v = node?.[name];
  if (v == null) return undefined;
  if (typeof v === "string") return v;
  if (typeof v === "object" && typeof v["#text"] === "string") return v["#text"];
  return undefined;
}

function asArray<T>(v: T | T[] | undefined | null): T[] {
  return v == null ? [] : Array.isArray(v) ? v : [v];
}

// XElement.Descendants(name) 재현: 노드 하위 임의 깊이에서 태그명이 name 인 요소를 모은다.
function descendants(node: any, name: string): any[] {
  const out: any[] = [];
  const walk = (n: any) => {
    if (n == null || typeof n !== "object") return;
    for (const [k, v] of Object.entries(n)) {
      if (k.startsWith("@_") || k === "#text") continue;
      for (const item of Array.isArray(v) ? v : [v]) {
        if (k === name) out.push(item);
        if (item && typeof item === "object") walk(item);
      }
    }
  };
  walk(node);
  return out;
}

function parseService(s: any): CatalogService {
  const packages: CatalogPackage[] = descendants(s, "Package").map((p) => ({
    name: attr(p, "Name") ?? "",
    url: attr(p, "Url") ?? "",
    arguments: attr(p, "Arguments") ?? "",
  }));
  const edgeExtensions: CatalogEdgeExtension[] = descendants(s, "EdgeExtension").map((e) => ({
    name: attr(e, "Name") ?? "",
    extensionId: attr(e, "ExtensionId") ?? "",
    crxUrl: attr(e, "CrxUrl") ?? "",
  }));
  const id = (attr(s, "Id") ?? "").trim();
  const displayName = attr(s, "DisplayName") ?? "";
  const displayNameEn = attr(s, "en-US-DisplayName");
  const category = attr(s, "Category") ?? "Other";
  const url = attr(s, "Url") ?? "";
  const searchKeywords = elemText(s, "SearchKeywords");
  const compatNotes = elemText(s, "CompatNotes");

  const searchHaystack = [
    id, displayName, displayNameEn ?? "", category, url,
    packages.map((p) => p.name).join(" "),
    searchKeywords ?? "",
  ].join(" ").toLowerCase();

  return { id, displayName, displayNameEn, category, url, packages, edgeExtensions, searchKeywords, compatNotes, searchHaystack };
}

export function parseCatalog(xml: string): CatalogDocument {
  const parsed = parser.parse(xml);
  const rootKey = Object.keys(parsed).find((k) => !k.startsWith("?")) ?? Object.keys(parsed)[0];
  const rootEl = parsed[rootKey];
  if (!rootEl) throw new Error("Catalog.xml has no root element.");

  const companions: CatalogCompanion[] = descendants(rootEl, "Companion")
    .map((c) => {
      const id = (attr(c, "Id") ?? "").trim();
      const displayName = attr(c, "DisplayName") ?? "";
      const displayNameEn = attr(c, "en-US-DisplayName");
      const url = attr(c, "Url") ?? "";
      const args = attr(c, "Arguments");
      const searchHaystack = `${id} ${displayName} ${displayNameEn ?? ""} ${url}`.toLowerCase();
      return { id, displayName, displayNameEn, url, arguments: args, searchHaystack };
    })
    .filter((c) => c.displayName.trim().length > 0);

  const services: CatalogService[] = asArray(rootEl.InternetServices?.Service)
    .map(parseService)
    .filter((s) => s.id.trim().length > 0);

  return { services, companions };
}

let cached: CatalogDocument | undefined;
let fetchedAt = 0;
let inflight: Promise<CatalogDocument> | undefined;

export async function getCatalog(): Promise<CatalogDocument> {
  if (cached && Date.now() - fetchedAt < CACHE_TTL_MS) return cached;
  if (inflight) return inflight;
  inflight = (async () => {
    const res = await fetch(CATALOG_URL, { headers: { "User-Agent": "tablecloth-mcp/0.2" } });
    if (!res.ok) throw new Error(`catalog fetch failed: HTTP ${res.status}`);
    const xml = await res.text();
    cached = parseCatalog(xml);
    fetchedAt = Date.now();
    return cached;
  })();
  try {
    return await inflight;
  } finally {
    inflight = undefined;
  }
}

export const iconUrlFor = (serviceId: string): string => `${ICON_BASE_URL}/${serviceId}.png`;
