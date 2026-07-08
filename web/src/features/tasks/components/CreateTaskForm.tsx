"use client";

import { useRouter } from "next/navigation";
import { type FormEvent, useState } from "react";
import { ApiError } from "../api";
import { useCreateTask } from "../useTasks";
import type { Priority } from "../types";

export function CreateTaskForm() {
  const router = useRouter();
  const createTask = useCreateTask();
  const [title, setTitle] = useState("");
  const [description, setDescription] = useState("");
  const [priority, setPriority] = useState<Priority>("MEDIUM");
  const [dueDate, setDueDate] = useState("");
  const [fieldErrors, setFieldErrors] = useState<Record<string, string[]>>({});

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setFieldErrors({});

    try {
      const task = await createTask.mutateAsync({
        title,
        description: description || undefined,
        priority,
        dueDate: dueDate || undefined,
      });
      router.push(`/tasks/${task.id}`);
    } catch (error) {
      if (error instanceof ApiError && error.problem.errors) {
        setFieldErrors(error.problem.errors);
      }
    }
  }

  return (
    <form onSubmit={handleSubmit}>
      <div>
        <label htmlFor="title">Title</label>
        <input id="title" value={title} onChange={(e) => setTitle(e.target.value)} />
        {fieldErrors.title && <p role="alert">{fieldErrors.title.join(" ")}</p>}
      </div>

      <div>
        <label htmlFor="description">Description</label>
        <textarea id="description" value={description} onChange={(e) => setDescription(e.target.value)} />
        {fieldErrors.description && <p role="alert">{fieldErrors.description.join(" ")}</p>}
      </div>

      <div>
        <label htmlFor="priority">Priority</label>
        <select id="priority" value={priority} onChange={(e) => setPriority(e.target.value as Priority)}>
          <option value="LOW">LOW</option>
          <option value="MEDIUM">MEDIUM</option>
          <option value="HIGH">HIGH</option>
        </select>
      </div>

      <div>
        <label htmlFor="dueDate">Due date</label>
        <input id="dueDate" type="date" value={dueDate} onChange={(e) => setDueDate(e.target.value)} />
        {fieldErrors.dueDate && <p role="alert">{fieldErrors.dueDate.join(" ")}</p>}
      </div>

      <button type="submit" disabled={createTask.isPending}>
        Create
      </button>
    </form>
  );
}
