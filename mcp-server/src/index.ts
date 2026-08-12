import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { StdioServerTransport } from "@modelcontextprotocol/sdk/server/stdio.js";
import { z } from "zod";

const API_BASE = process.env.SHOPEASE_API_URL ?? "http://localhost:5000/api/v1";

// In-memory token store for the session
let authToken: string | null = null;

function headers(): Record<string, string> {
  const h: Record<string, string> = { "Content-Type": "application/json" };
  if (authToken) h["Authorization"] = `Bearer ${authToken}`;
  return h;
}

async function apiFetch(path: string, options: RequestInit = {}): Promise<unknown> {
  const url = `${API_BASE}${path}`;
  const res = await fetch(url, { ...options, headers: { ...headers(), ...(options.headers as Record<string, string> ?? {}) } });
  const json = await res.json() as { success: boolean; data: unknown; message?: string };
  if (!res.ok || !json.success) {
    throw new Error(json.message ?? `HTTP ${res.status}`);
  }
  return json.data;
}

function formatTable(rows: Record<string, unknown>[]): string {
  if (!rows.length) return "(no results)";
  const keys = Object.keys(rows[0]);
  const lines = [keys.join(" | "), keys.map(k => "-".repeat(k.length)).join("-|-")];
  for (const row of rows) {
    lines.push(keys.map(k => String(row[k] ?? "")).join(" | "));
  }
  return lines.join("\n");
}

const server = new McpServer({
  name: "shopease",
  version: "1.0.0",
});

// ── Auth ──────────────────────────────────────────────────────────────────────

server.tool(
  "login",
  "Authenticate with the ShopEase API. Call this first to get an access token before using admin tools.",
  {
    email: z.string().email().describe("Admin email address"),
    password: z.string().describe("Password"),
  },
  async ({ email, password }) => {
    const data = await apiFetch("/auth/login", {
      method: "POST",
      body: JSON.stringify({ email, password }),
    }) as { accessToken: string; user: { firstName: string; lastName: string; role: string } };

    authToken = data.accessToken;
    return {
      content: [{
        type: "text",
        text: `Logged in as ${data.user.firstName} ${data.user.lastName} (${data.user.role}). Token stored for this session.`,
      }],
    };
  }
);

// ── Admin Stats ───────────────────────────────────────────────────────────────

server.tool(
  "get_admin_stats",
  "Get platform-wide stats: total users, active listings, total orders, and total revenue.",
  {},
  async () => {
    const data = await apiFetch("/admin/stats") as {
      totalUsers: number;
      activeListings: number;
      totalOrders: number;
      totalRevenue: number;
    };
    return {
      content: [{
        type: "text",
        text: [
          "## Platform Stats",
          `- Total Users:      ${data.totalUsers}`,
          `- Active Listings:  ${data.activeListings}`,
          `- Total Orders:     ${data.totalOrders}`,
          `- Total Revenue:    $${data.totalRevenue.toFixed(2)}`,
        ].join("\n"),
      }],
    };
  }
);

// ── Listings ──────────────────────────────────────────────────────────────────

server.tool(
  "get_listings",
  "Browse public listings with optional filters. No auth required.",
  {
    search: z.string().optional().describe("Search keyword"),
    status: z.enum(["Draft", "Active", "Sold", "Ended", "Removed"]).optional(),
    page: z.number().int().min(1).default(1),
    pageSize: z.number().int().min(1).max(50).default(10),
  },
  async ({ search, status, page, pageSize }) => {
    const params = new URLSearchParams({ page: String(page), pageSize: String(pageSize) });
    if (search) params.set("search", search);
    if (status) params.set("status", status);

    const data = await apiFetch(`/listings?${params}`) as {
      items: Array<{ id: string; title: string; finalPrice: number; status: string; sellerName: string; createdAt: string }>;
      totalCount: number;
    };

    const rows = data.items.map(l => ({
      id: l.id.slice(0, 8) + "…",
      title: l.title.slice(0, 40),
      price: `$${l.finalPrice}`,
      status: l.status,
      seller: l.sellerName,
    }));

    return {
      content: [{
        type: "text",
        text: `Total: ${data.totalCount}  (page ${page})\n\n${formatTable(rows)}`,
      }],
    };
  }
);

server.tool(
  "get_pending_listings",
  "List all listings pending admin review/approval.",
  {
    page: z.number().int().min(1).default(1),
    pageSize: z.number().int().min(1).max(50).default(10),
  },
  async ({ page, pageSize }) => {
    const params = new URLSearchParams({ page: String(page), pageSize: String(pageSize), visibility: "PendingReview" });
    const data = await apiFetch(`/admin/listings?${params}`) as {
      items: Array<{ id: string; title: string; sellerName: string; finalPrice: number; hasPendingVersion: boolean; createdAt: string }>;
      totalCount: number;
    };

    const rows = data.items.map(l => ({
      id: l.id.slice(0, 8) + "…",
      title: l.title.slice(0, 40),
      seller: l.sellerName,
      price: `$${l.finalPrice}`,
      pendingUpdate: l.hasPendingVersion ? "yes" : "no",
    }));

    return {
      content: [{
        type: "text",
        text: `Pending review: ${data.totalCount}\n\n${formatTable(rows)}`,
      }],
    };
  }
);

server.tool(
  "approve_listing",
  "Approve a listing (or pending update). Requires admin login.",
  {
    listingId: z.string().uuid().describe("Full listing UUID"),
    notes: z.string().optional().describe("Optional approval notes"),
  },
  async ({ listingId, notes }) => {
    await apiFetch(`/admin/listings/${listingId}/approve`, {
      method: "POST",
      body: JSON.stringify({ notes: notes ?? "" }),
    });
    return { content: [{ type: "text", text: `Listing ${listingId} approved successfully.` }] };
  }
);

server.tool(
  "reject_listing",
  "Reject a listing with a reason. Requires admin login.",
  {
    listingId: z.string().uuid().describe("Full listing UUID"),
    reason: z.string().min(1).describe("Rejection reason shown to seller"),
    notes: z.string().optional(),
  },
  async ({ listingId, reason, notes }) => {
    await apiFetch(`/admin/listings/${listingId}/reject`, {
      method: "POST",
      body: JSON.stringify({ reason, notes: notes ?? "" }),
    });
    return { content: [{ type: "text", text: `Listing ${listingId} rejected. Reason: "${reason}"` }] };
  }
);

// ── Users ─────────────────────────────────────────────────────────────────────

server.tool(
  "get_users",
  "List all users. Requires admin login.",
  {
    search: z.string().optional().describe("Search by name or email"),
    role: z.enum(["User", "Admin"]).optional(),
    status: z.enum(["Active", "Suspended"]).optional(),
    page: z.number().int().min(1).default(1),
    pageSize: z.number().int().min(1).max(50).default(10),
  },
  async ({ search, role, status, page, pageSize }) => {
    const params = new URLSearchParams({ page: String(page), pageSize: String(pageSize) });
    if (search) params.set("search", search);
    if (role) params.set("role", role);
    if (status) params.set("status", status);

    const data = await apiFetch(`/admin/users?${params}`) as {
      items: Array<{ id: string; firstName: string; lastName: string; email: string; role: string; isSuspended: boolean; createdAt: string }>;
      totalCount: number;
    };

    const rows = data.items.map(u => ({
      id: u.id.slice(0, 8) + "…",
      name: `${u.firstName} ${u.lastName}`,
      email: u.email,
      role: u.role,
      status: u.isSuspended ? "Suspended" : "Active",
    }));

    return {
      content: [{
        type: "text",
        text: `Total users: ${data.totalCount}\n\n${formatTable(rows)}`,
      }],
    };
  }
);

server.tool(
  "suspend_user",
  "Suspend a user account. Requires admin login.",
  { userId: z.string().uuid().describe("Full user UUID") },
  async ({ userId }) => {
    await apiFetch(`/admin/users/${userId}/suspend`, { method: "PATCH" });
    return { content: [{ type: "text", text: `User ${userId} suspended.` }] };
  }
);

server.tool(
  "activate_user",
  "Reactivate a suspended user account. Requires admin login.",
  { userId: z.string().uuid().describe("Full user UUID") },
  async ({ userId }) => {
    await apiFetch(`/admin/users/${userId}/activate`, { method: "PATCH" });
    return { content: [{ type: "text", text: `User ${userId} activated.` }] };
  }
);

// ── Orders ────────────────────────────────────────────────────────────────────

server.tool(
  "get_orders",
  "List all orders (admin view). Requires admin login.",
  {
    search: z.string().optional().describe("Search by order number or buyer name"),
    status: z.enum(["Pending", "Processing", "Shipped", "Delivered", "Cancelled", "Refunded"]).optional(),
    page: z.number().int().min(1).default(1),
    pageSize: z.number().int().min(1).max(50).default(10),
  },
  async ({ search, status, page, pageSize }) => {
    const params = new URLSearchParams({ page: String(page), pageSize: String(pageSize) });
    if (search) params.set("search", search);
    if (status) params.set("status", status);

    const data = await apiFetch(`/admin/orders?${params}`) as {
      items: Array<{ id: string; orderNumber: string; buyerName: string; itemCount: number; totalAmount: number; status: string; createdAt: string }>;
      totalCount: number;
    };

    const rows = data.items.map(o => ({
      orderNumber: o.orderNumber,
      buyer: o.buyerName,
      items: o.itemCount,
      total: `$${o.totalAmount.toFixed(2)}`,
      status: o.status,
      date: o.createdAt.slice(0, 10),
    }));

    return {
      content: [{
        type: "text",
        text: `Total orders: ${data.totalCount}\n\n${formatTable(rows)}`,
      }],
    };
  }
);

// ── Categories ────────────────────────────────────────────────────────────────

server.tool(
  "get_categories",
  "List all product categories. No auth required.",
  {},
  async () => {
    const data = await apiFetch("/categories") as Array<{ id: string; name: string; parentId?: string }>;
    const rows = data.map(c => ({ id: c.id.slice(0, 8) + "…", name: c.name, parent: c.parentId ? c.parentId.slice(0, 8) + "…" : "—" }));
    return { content: [{ type: "text", text: formatTable(rows) }] };
  }
);

// ── Health ────────────────────────────────────────────────────────────────────

server.tool(
  "health_check",
  "Check if the ShopEase API is up and running.",
  {},
  async () => {
    const res = await fetch(`${API_BASE.replace("/api/v1", "")}/health`);
    const text = await res.text();
    return { content: [{ type: "text", text: `API status ${res.status}: ${text}` }] };
  }
);

// ── Start ─────────────────────────────────────────────────────────────────────

const transport = new StdioServerTransport();
await server.connect(transport);
