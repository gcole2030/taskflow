import type { Priority, ProblemDetails, Task, TaskEvent, TaskStatus } from "./types";

const API_BASE = process.env.NEXT_PUBLIC_API_BASE ?? "http://localhost:8080/api/v1";

export class ApiError extends Error {
  problem: ProblemDetails;
  status: number;

  constructor(problem: ProblemDetails, status: number) {
    super(problem.detail ?? problem.title ?? "Request failed");
    this.problem = problem;
    this.status = status;
  }
}

async function handle<T>(response: Response): Promise<T> {
  if (!response.ok) {
    const problem = (await response.json().catch(() => ({}))) as ProblemDetails;
    throw new ApiError(problem, response.status);
  }
  return response.status === 204 ? (undefined as T) : ((await response.json()) as T);
}

export interface ListTasksParams {
  status?: TaskStatus;
  priority?: Priority;
  overdue?: boolean;
  page?: number;
  pageSize?: number;
}

export interface ListTasksResult {
  tasks: Task[];
  totalCount: number;
}

export async function listTasks(params: ListTasksParams): Promise<ListTasksResult> {
  const query = new URLSearchParams();
  if (params.status) query.set("status", params.status);
  if (params.priority) query.set("priority", params.priority);
  if (params.overdue) query.set("overdue", "true");
  if (params.page) query.set("page", String(params.page));
  if (params.pageSize) query.set("pageSize", String(params.pageSize));

  const response = await fetch(`${API_BASE}/tasks?${query.toString()}`);
  const tasks = await handle<Task[]>(response);
  const totalCount = Number(response.headers.get("X-Total-Count") ?? tasks.length);
  return { tasks, totalCount };
}

export async function getTask(id: string): Promise<Task> {
  const response = await fetch(`${API_BASE}/tasks/${id}`);
  return handle<Task>(response);
}

export async function getTaskEvents(id: string): Promise<TaskEvent[]> {
  const response = await fetch(`${API_BASE}/tasks/${id}/events`);
  return handle<TaskEvent[]>(response);
}

export interface CreateTaskInput {
  title: string;
  description?: string;
  priority?: Priority;
  dueDate?: string;
}

export async function createTask(input: CreateTaskInput): Promise<Task> {
  const response = await fetch(`${API_BASE}/tasks`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(input),
  });
  return handle<Task>(response);
}

export interface PatchTaskInput {
  title?: string;
  description?: string | null;
  priority?: Priority;
  dueDate?: string | null;
}

export async function patchTask(id: string, input: PatchTaskInput): Promise<Task> {
  const response = await fetch(`${API_BASE}/tasks/${id}`, {
    method: "PATCH",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(input),
  });
  return handle<Task>(response);
}

export async function transitionTask(
  id: string,
  to: TaskStatus,
  metadata?: Record<string, unknown>,
): Promise<Task> {
  const response = await fetch(`${API_BASE}/tasks/${id}/transitions`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ to, metadata }),
  });
  return handle<Task>(response);
}
