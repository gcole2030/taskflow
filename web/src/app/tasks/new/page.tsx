import Link from "next/link";
import { CreateTaskForm } from "@/features/tasks/components/CreateTaskForm";

export default function NewTaskPage() {
  return (
    <main style={{ maxWidth: 640, margin: "0 auto", padding: 24 }}>
      <Link href="/">← Back to tasks</Link>
      <h1>New task</h1>
      <CreateTaskForm />
    </main>
  );
}
