// API Client for PayFlow HR

const BASE_URL = "/api/v1";

export interface ProblemDetails {
  type?: string;
  title?: string;
  status?: number;
  detail?: string;
  instance?: string;
  correlationId?: string;
  errors?: string[];
}

export class ApiError extends Error {
  status: number;
  problemDetails?: ProblemDetails;

  constructor(
    message: string,
    status: number,
    problemDetails?: ProblemDetails,
  ) {
    super(message);
    this.name = "ApiError";
    this.status = status;
    this.problemDetails = problemDetails;
  }
}

export async function apiFetch<T>(
  endpoint: string,
  options: RequestInit = {},
): Promise<T> {
  const token = localStorage.getItem("payflow_token");
  const headers = new Headers(options.headers || {});

  if (!headers.has("Content-Type") && !(options.body instanceof FormData)) {
    headers.set("Content-Type", "application/json");
  }

  if (token && !headers.has("Authorization")) {
    headers.set("Authorization", `Bearer ${token}`);
  }

  // Include credentials for HttpOnly cookie support
  const config: RequestInit = {
    ...options,
    headers,
    credentials: "include",
  };

  const url = endpoint.startsWith("http") ? endpoint : `${BASE_URL}${endpoint}`;
  const response = await fetch(url, config);

  if (response.status === 401 && endpoint !== "/auth/login") {
    // If unauthorized, clear token and redirect to login
    localStorage.removeItem("payflow_token");
    localStorage.removeItem("payflow_user");
    window.dispatchEvent(new Event("payflow-session-expired"));
  }

  if (!response.ok) {
    let errorDetail = `Request failed with status ${response.status}`;
    let problemDetails: ProblemDetails | undefined;

    try {
      const errJson = await response.json();
      problemDetails = errJson;
      errorDetail =
        errJson.detail || errJson.title || errJson.message || errorDetail;
    } catch {
      // Not JSON
    }

    throw new ApiError(errorDetail, response.status, problemDetails);
  }

  if (response.status === 204) {
    return {} as T;
  }

  const contentType = response.headers.get("content-type");
  if (contentType && contentType.includes("application/json")) {
    return response.json();
  }

  return response.text() as unknown as T;
}
