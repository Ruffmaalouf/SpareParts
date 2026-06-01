const { normalizeBaseUrl } = require("./formatters");

class ApiError extends Error {
  constructor(message, status) {
    super(message);
    this.name = "ApiError";
    this.status = status;
  }
}

async function readError(response) {
  const text = await response.text();
  if (!text) return `${response.status} ${response.statusText}`.trim();
  try {
    const json = JSON.parse(text);
    return json.message || json.title || json.error || text;
  } catch {
    return text;
  }
}

function isFormData(value) {
  return typeof FormData !== "undefined" && value instanceof FormData;
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

class ApiClient {
  constructor(apiBaseUrl, token, onUnauthorized) {
    this.apiBaseUrl = normalizeBaseUrl(apiBaseUrl);
    this.token = token || "";
    this.onUnauthorized = onUnauthorized || null;
  }

  async requestResponse(path, options) {
    const headers = {
      Accept: "application/json",
      ...(options && options.headers ? options.headers : {})
    };

    if (options && options.body && !isFormData(options.body)) {
      headers["Content-Type"] = "application/json";
    }

    if (this.token) {
      headers.Authorization = `Bearer ${this.token}`;
    }

    const response = await fetch(`${this.apiBaseUrl}${path}`, {
      ...(options || {}),
      headers
    });

    if (!response.ok) {
      if (response.status === 401 && this.onUnauthorized) {
        this.onUnauthorized();
      }

      const message = response.status === 401
        ? "Session expired. Sign in again."
        : await readError(response);
      throw new ApiError(message, response.status);
    }

    return response;
  }

  async readResponse(response) {
    if (response.status === 204) {
      return null;
    }

    return response.json();
  }

  async request(path, options) {
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
    return this.request(path, {
      method: "POST",
      body: JSON.stringify(body)
    });
  }

  postForm(path, formData) {
    return this.request(path, {
      method: "POST",
      body: formData
    });
  }

  put(path, body) {
    return this.request(path, {
      method: "PUT",
      body: JSON.stringify(body)
    });
  }

  delete(path) {
    return this.request(path, {
      method: "DELETE"
    });
  }
}

module.exports = { ApiClient, ApiError };
