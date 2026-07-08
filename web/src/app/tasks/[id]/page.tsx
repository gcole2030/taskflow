"use client";

import Link from "next/link";
import { use } from "react";
import { AuditTimeline } from "@/features/tasks/components/AuditTimeline";
import { PriorityBadge } from "@/features/tasks/components/PriorityBadge";
import { StatusActions } from "@/features/tasks/components/StatusActions";
import { useTask, useTaskEvents } from "@/features/tasks/useTasks";

export default function TaskDetailPage({ params }: { params: Promise<{ id: string }> }) {
  const { id } = use(params);
  const task = useTask(id);
  const events = useTaskEvents(id);

  if (task.isLoading) return <p style={{ padding: 24 }}>Loading…</p>;
  if (task.isError || !task.data) return <p role="alert" style={{ padding: 24 }}>Task not found.</p>;

  return (
    <main style={{ maxWidth: 640, margin: "0 auto", padding: 24 }}>
      <Link href="/">← Back to tasks</Link>
      <h1>{task.data.title}</h1>
      <p>
        <PriorityBadge priority={task.data.priority} /> · {task.data.status}
      </p>
      {task.data.description && <p>{task.data.description}</p>}
      {task.data.dueDate && <p>Due {task.data.dueDate}</p>}

      <h2>Status</h2>
      <StatusActions taskId={id} status={task.data.status} />

      <h2>Audit trail</h2>
      {events.isLoading && <p>Loading…</p>}
      {events.data && <AuditTimeline events={events.data} />}
    </main>
  );
}
