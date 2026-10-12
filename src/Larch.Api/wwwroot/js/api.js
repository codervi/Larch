// Thin wrapper around fetch. The login is a cookie that the browser sends by itself;
// the X-Requested-With header is what the server checks on every change (CSRF guard).

export class ApiError extends Error {
  constructor(status, problem) {
    super(problem?.errors ? Object.values(problem.errors).flat()[0]
      : problem?.detail ?? problem?.title
      ?? (status === 429 ? "Too many attempts. Wait a minute and try again." : "Something went wrong. Please try again."));
    this.status = status;
  }
}

let onUnauthorized = () => {};
export function setUnauthorizedHandler(fn) { onUnauthorized = fn; }

export async function api(method, url, body) {
  const headers = { Accept: "application/json", "X-Requested-With": "fetch" };
  const options = { method, headers };
  if (body !== undefined) {
    headers["Content-Type"] = "application/json";
    options.body = JSON.stringify(body);
  }

  let response;
  try {
    response = await fetch("/api" + url, options);
  } catch {
    throw new ApiError(0, { detail: "Can't reach the server. Check your connection." });
  }

  if (response.status === 204) return null;

  const text = await response.text();
  let data = null;
  if (text) {
    try { data = JSON.parse(text); } catch { /* not JSON */ }
  }

  if (!response.ok) {
    // 401 on a normal call means the session ended (the auth calls handle their own errors)
    if (response.status === 401 && !url.startsWith("/auth/")) onUnauthorized();
    throw new ApiError(response.status, data);
  }
  return data;
}

export const get = (url) => api("GET", url);
export const post = (url, body) => api("POST", url, body ?? {});
export const put = (url, body) => api("PUT", url, body);
export const del = (url) => api("DELETE", url);
