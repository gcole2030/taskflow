/implement-slice web — Next.js frontend per spec §6

Scaffold in web/: Next.js App Router, TypeScript, output:'standalone', TanStack Query.
- tasks list: status filter tabs, priority badge, overdue highlight
- create form: client mirrors server rules but ALWAYS renders API field errors inline
- status actions: buttons driven by a legalTransitions map identical to spec §3;
  illegal targets disabled
- detail page: audit timeline from /events
Testing: Vitest + React Testing Library for exactly two things — the legalTransitions
map (table-driven mirror of the backend StateMachine) and create-form rendering of a
mocked 400 problem+json. Add npm run test to CI. Wire into compose via Dockerfile.web.
