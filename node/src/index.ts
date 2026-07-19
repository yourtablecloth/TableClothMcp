// TableCloth MCP server (Node/TS). SPEC.md 의 계약을 따르며 shared/ 를 단일 진실 원천으로 소비한다.
// .NET(dnx) 구현의 counterpart. stdio 전송, 모든 로그는 stderr.
import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { StdioServerTransport } from "@modelcontextprotocol/sdk/server/stdio.js";
import { z } from "zod";
import { strings } from "./generated.js";
import { searchServices, getService, listCategories, listCompanions } from "./tools/catalog-tools.js";
import { generateWsb, launchSandbox } from "./tools/sandbox-tools.js";

const T = strings.tools;
const text = (obj: unknown) => ({ content: [{ type: "text" as const, text: JSON.stringify(obj) }] });
const RO = { readOnlyHint: true, openWorldHint: true };

const server = new McpServer(
  { name: "tablecloth-mcp", version: "0.2.0" },
  { instructions: strings.server.instructions },
);

server.registerTool(
  "search_services",
  {
    title: T.search_services.title,
    description: T.search_services.description,
    inputSchema: {
      query: z.string().describe(T.search_services.params.query),
      category: z.string().nullable().default(null).describe(T.search_services.params.category),
      limit: z.number().int().default(15).describe(T.search_services.params.limit),
    },
    annotations: { title: T.search_services.title, ...RO },
  },
  async ({ query, category, limit }) => text(await searchServices(query, category, limit)),
);

server.registerTool(
  "get_service",
  {
    title: T.get_service.title,
    description: T.get_service.description,
    inputSchema: { id: z.string().describe(T.get_service.params.id) },
    annotations: { title: T.get_service.title, ...RO },
  },
  async ({ id }) => text(await getService(id)),
);

server.registerTool(
  "list_categories",
  {
    title: T.list_categories.title,
    description: T.list_categories.description,
    annotations: { title: T.list_categories.title, ...RO },
  },
  async () => text(await listCategories()),
);

server.registerTool(
  "list_companions",
  {
    title: T.list_companions.title,
    description: T.list_companions.description,
    inputSchema: { query: z.string().nullable().default(null).describe(T.list_companions.params.query) },
    annotations: { title: T.list_companions.title, ...RO },
  },
  async ({ query }) => text(await listCompanions(query)),
);

server.registerTool(
  "generate_wsb",
  {
    title: T.generate_wsb.title,
    description: T.generate_wsb.description,
    inputSchema: { serviceIds: z.array(z.string()).describe(T.generate_wsb.params.serviceIds) },
    annotations: { title: T.generate_wsb.title, ...RO },
  },
  async ({ serviceIds }) => text(await generateWsb(serviceIds)),
);

server.registerTool(
  "launch_sandbox",
  {
    title: T.launch_sandbox.title,
    description: T.launch_sandbox.description,
    inputSchema: { serviceIds: z.array(z.string()).describe(T.launch_sandbox.params.serviceIds) },
    annotations: { title: T.launch_sandbox.title, readOnlyHint: false, destructiveHint: false, openWorldHint: true },
  },
  async ({ serviceIds }) => text(await launchSandbox(serviceIds)),
);

const transport = new StdioServerTransport();
await server.connect(transport);
console.error("tablecloth-mcp (node) started on stdio");
