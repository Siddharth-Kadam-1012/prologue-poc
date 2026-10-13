# Frontend — Rules

## Detection first
Before writing any frontend code, inspect the repository for:
- Framework: React, Blazor, Razor Pages/MVC, Vue, Angular, or none.
- Package manager: npm, pnpm, yarn, or none.
- Component library: Carbon, MUI, Shadcn, Fluent, custom, or none.
- Routing: framework router, Next.js, React Router, or server-side routing.
- State management: Redux, Zustand, Context, none.
- Build tool: Vite, Webpack, esbuild, .NET build pipeline.
- Storybook: present or absent.
- Design tokens: CSS custom properties, tailwind config, theme file.

If **no frontend exists**, record the decision explicitly before creating one.
Do not silently introduce a framework.

## Component rules
- Use **only** components that already exist in the repository's design system.
- If a required component does not exist, record a **design-system gap** in
  `specs/<NNN>-<feature>/ux/component-map.yaml` and obtain approval before implementing a new component.
- Do not introduce a second component library without a documented ADR.
- Do not use inline styles or `!important` overrides on design-system components.

## Routing
- Follow existing route conventions. Do not add routes that conflict with existing ones.
- Lazy-load large feature bundles unless the project does not support code splitting.

## State management
- Use the existing state management pattern. Do not add a second store library.
- Keep server state (API data) and UI state (open/closed, form values) clearly separated.

## Forms and validation
- Validate on the client with the same rules as the server (DRY via shared schema if possible).
- Display field-level validation messages using the design system's error state.
- Disable the submit button while a request is in-flight to prevent double-submission.

## Accessibility
- All interactive elements must be reachable and operable by keyboard.
- Use semantic HTML elements. Use ARIA attributes only when semantics are insufficient.
- Color contrast must meet WCAG 2.1 AA.
- Every image must have descriptive `alt` text.
- Error messages must be associated with their input via `aria-describedby`.

## Error and loading states
Every screen must implement:
- Loading state (skeleton or spinner).
- Empty state (when the data set is empty).
- Error state (server failure, network error).
- Forbidden/unauthorized state.
- Success confirmation for write operations.

## Storybook / component gallery
- If Storybook is present, add or update a story for every new or modified component.
- Do not introduce Storybook where it does not exist without a justified ADR.
