import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import * as api from "./api";
import type { CreateTaskInput, ListTasksParams } from "./api";
import type { TaskStatus } from "./types";

export function useTasks(filters: ListTasksParams) {
  return useQuery({
    queryKey: ["tasks", filters],
    queryFn: () => api.listTasks(filters),
  });
}

export function useTask(id: string) {
  return useQuery({
    queryKey: ["task", id],
    queryFn: () => api.getTask(id),
    enabled: Boolean(id),
  });
}

export function useTaskEvents(id: string) {
  return useQuery({
    queryKey: ["task", id, "events"],
    queryFn: () => api.getTaskEvents(id),
    enabled: Boolean(id),
  });
}

export function useCreateTask() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (input: CreateTaskInput) => api.createTask(input),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ["tasks"] }),
  });
}

export function useTransitionTask(id: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ to, metadata }: { to: TaskStatus; metadata?: Record<string, unknown> }) =>
      api.transitionTask(id, to, metadata),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["tasks"] });
      queryClient.invalidateQueries({ queryKey: ["task", id] });
    },
  });
}
