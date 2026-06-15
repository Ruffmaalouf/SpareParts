import { normalizeBaseUrl } from "./formatters.js";

export class ApiError extends Error {
  constructor(message, status, details = {}) {
    super(message);
    this.name = "ApiError";
    this.status = status;
    this.errorCode = details.errorCode || null;
    this.currentPlan = details.currentPlan || null;
    this.requiredPlans = details.requiredPlans || [];
    this.upgradeUrl = details.upgradeUrl || null;
  }
}

async function readError(response) {
  const text = await response.text();
  if (!text) return { message: `${response.status} ${response.statusText}`.trim() };
  try {
    const json = JSON.parse(text);
    return {
      message: json.message || json.title || json.error || text,
      errorCode: json.errorCode,
      currentPlan: json.currentPlan,
      requiredPlans: json.requiredPlans,
      upgradeUrl: json.upgradeUrl
    };
  } catch {
    return { message: text };
  }
}

function buildPagedPath(path, page, pageSize) {
  const [pathname, query = ""] = String(path || "").split("?");
  const params = new URLSearchParams(query);
  params.set("page", String(page));
  params.set("pageSize", String(pageSize));
  const nextQuery = params.toString();
  return nextQuery ? `${pathname}?${nextQuery}` : pathname;
}

function isPartsCollectionPath(path) {
  const normalized = String(path || "").split("?")[0].replace(/\/+$/, "");
  return /^\/?api\/parts$/i.test(normalized);
}

export class ApiClient {
  constructor(apiBaseUrl, token, onUnauthorized = null, onPlanLocked = null) {
    this.apiBaseUrl = normalizeBaseUrl(apiBaseUrl);
    this.token = token || "";
    this.onUnauthorized = onUnauthorized;
    this.onPlanLocked = onPlanLocked;
  }

  async requestResponse(path, options = {}) {
    const headers = new Headers(options.headers || {});
    headers.set("Accept", "application/json");
    if (options.body && !(options.body instanceof FormData) && !headers.has("Content-Type")) {
      headers.set("Content-Type", "application/json");
    }
    if (this.token) {
      headers.set("Authorization", `Bearer ${this.token}`);
    }

    const response = await fetch(`${this.apiBaseUrl}${path}`, {
      ...options,
      headers
    });

    if (!response.ok) {
      if (response.status === 401 && this.onUnauthorized) {
        this.onUnauthorized();
      }

      if (response.status === 401) {
        throw new ApiError("Session expired. Sign in again.", response.status);
      }

      const details = await readError(response);
      const error = new ApiError(details.message, response.status, details);
      if (response.status === 403 && error.errorCode && this.onPlanLocked) {
        this.onPlanLocked(error);
      }
      throw error;
    }

    return response;
  }

  async readResponse(response) {
    if (response.status === 204) {
      return null;
    }

    return response.json();
  }

  async request(path, options = {}) {
    const response = await this.requestResponse(path, options);
    return this.readResponse(response);
  }

  get(path) {
    return this.request(path);
  }

  async getAllPages(path, pageSize = 5000) {
    const rows = [];
    let page = 1;

    while (true) {
      const response = await this.requestResponse(buildPagedPath(path, page, pageSize));
      const batch = await this.readResponse(response);
      const items = Array.isArray(batch) ? batch : batch ? [batch] : [];
      rows.push(...items);

      const totalCount = Number.parseInt(response.headers.get("X-Total-Count") || "", 10);
      const effectivePageSize = Number.parseInt(response.headers.get("X-Page-Size") || "", 10) || pageSize;
      if (items.length === 0
        || items.length < effectivePageSize
        || (Number.isFinite(totalCount) && rows.length >= totalCount)) {
        return rows;
      }

      page += 1;
    }
  }

  list(path) {
    return isPartsCollectionPath(path) ? this.getAllPages(path) : this.get(path);
  }

  post(path, body) {
    return this.request(path, { method: "POST", body: JSON.stringify(body) });
  }

  put(path, body) {
    return this.request(path, { method: "PUT", body: JSON.stringify(body) });
  }

  delete(path) {
    return this.request(path, { method: "DELETE" });
  }

  postForm(path, formData) {
    return this.request(path, { method: "POST", body: formData });
  }
}
