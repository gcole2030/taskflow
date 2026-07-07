import type { TaskEvent } from "../types";

export function AuditTimeline({ events }: { events: TaskEvent[] }) {
  if (events.length === 0) {
    return <p>No events yet.</p>;
  }

  return (
    <ol>
      {events.map((event) => (
        <li key={event.id}>
          <strong>{event.eventType}</strong>
          {event.fromStatus && event.toStatus && (
            <>
              {" "}
              ({event.fromStatus} → {event.toStatus})
            </>
          )}
          {" — "}
          <time dateTime={event.occurredAt}>{event.occurredAt}</time>
          {Object.keys(event.metadata).length > 0 && (
            <pre>{JSON.stringify(event.metadata, null, 2)}</pre>
          )}
        </li>
      ))}
    </ol>
  );
}
