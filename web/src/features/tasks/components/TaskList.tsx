import Link from "next/link";
import type { Task } from "../types";
import { PriorityBadge } from "./PriorityBadge";

function isOverdue(task: Task): boolean {
  if (!task.dueDate || task.status === "DONE" || task.status === "CANCELLED") return false;
  return task.dueDate < new Date().toISOString().slice(0, 10);
}

export function TaskList({ tasks }: { tasks: Task[] }) {
  if (tasks.length === 0) {
    return <p>No tasks match these filters.</p>;
  }

  return (
    <table>
      <thead>
        <tr>
          <th>Title</th>
          <th>Status</th>
          <th>Priority</th>
          <th>Due date</th>
        </tr>
      </thead>
      <tbody>
        {tasks.map((task) => (
          <tr key={task.id} style={isOverdue(task) ? { color: "#b91c1c" } : undefined}>
            <td>
              <Link href={`/tasks/${task.id}`}>{task.title}</Link>
            </td>
            <td>{task.status}</td>
            <td>
              <PriorityBadge priority={task.priority} />
            </td>
            <td>
              {task.dueDate ?? "—"}
              {isOverdue(task) && " (overdue)"}
            </td>
          </tr>
        ))}
      </tbody>
    </table>
  );
}
