"use client";

import Link from "next/link";
import { useState } from "react";
import { StatusFilterTabs } from "@/features/tasks/components/StatusFilterTabs";
import { TaskList } from "@/features/tasks/components/TaskList";
import { useTasks } from "@/features/tasks/useTasks";
import type { TaskStatus } from "@/features/tasks/types";

export default function TaskBoardPage() {
  const [status, setStatus] = useState<TaskStatus | undefined>(undefined);
  const { data, isLoading, isError } = useTasks({ status, page: 1, pageSize: 20 });

  return (
    <main style={{ maxWidth: 960, margin: "0 auto", padding: 24 }}>
      <div style={{ display: "flex", justifyContent: "space-between", alignItems: "center" }}>
        <h1>Tasks</h1>
        <Link href="/tasks/new">New task</Link>
      </div>

      <StatusFilterTabs value={status} onChange={setStatus} />

      {isLoading && <p>Loading…</p>}
      {isError && <p role="alert">Failed to load tasks.</p>}
      {data && <TaskList tasks={data.tasks} />}
      {data && <p>{data.totalCount} total</p>}
    </main>
  );
}
