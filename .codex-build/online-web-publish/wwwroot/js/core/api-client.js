import { normalizeBaseUrl } from "./formatters.js";

export class ApiError extends Error {
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

export class ApiClient {
  constructor(apiBaseUrl, token, onUnauthorized = null) {
    this.apiBaseUrl = normalizeBaseUrl(apiBaseUrl);
    this.token = token || "";
    this.onUnauthorized = onUnauthorized;
  }

  async request(path, options = {}) {
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

      const message = response.status === 401
        ? "Session expired. Sign in again."
        : await readError(response);
      throw new ApiError(message, response.status);
    }

    if (response.status === 204) {
      return null;
    }

    return response.json();
  }

  get(path) {
    return this.request(path);
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
