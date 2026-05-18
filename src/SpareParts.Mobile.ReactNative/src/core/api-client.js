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

class ApiClient {
  constructor(apiBaseUrl, token, onUnauthorized) {
    this.apiBaseUrl = normalizeBaseUrl(apiBaseUrl);
    this.token = token || "";
    this.onUnauthorized = onUnauthorized || null;
  }

  async request(path, options) {
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

    if (response.status === 204) {
      return null;
    }

    return response.json();
  }

  get(path) {
    return this.request(path);
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
