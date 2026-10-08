---
name: frontend-polish
description: Improve the visual quality and usability of existing frontend projects while preserving functionality. Use for redesigning or polishing Next.js, React, Tailwind CSS, shadcn/ui, HTML/CSS, dashboards, landing pages, SaaS apps, settings pages, forms, navigation, cards, tables, and responsive layouts. Trigger when asked to make a project look better, more premium, modern, clean, professional, consistent, responsive, accessible, or production-ready; when recreating or refining UI from a screenshot/mockup; or when reviewing an existing interface for visual and UX improvements.
---

# Frontend Polish

## Objective

Make the existing project feel deliberately designed, visually coherent, responsive, and production-ready without breaking its behavior or rewriting working architecture unnecessarily.

Prioritize the visible user experience over novelty. Improve hierarchy, spacing, typography, composition, component consistency, states, responsiveness, and interaction quality before adding decoration.

## Workflow

1. Inspect the existing UI and project conventions before changing code.
2. Identify the highest-impact visual problems.
3. Preserve functionality, routes, data flow, component contracts, and established framework choices.
4. Define a lightweight visual direction that fits the product.
5. Implement the smallest coherent set of changes that noticeably improves the interface.
6. Review the finished UI across responsive states, interactions, and accessibility.

When the user provides a screenshot or mockup, use it as a visual target while adapting it to the project's content and component system rather than copying it mechanically.

## Audit Before Editing

Evaluate these areas first:

- Visual hierarchy: Can users instantly see the primary task, supporting information, and secondary actions?
- Spacing: Are gaps intentional and consistent rather than arbitrary?
- Typography: Are size, weight, line-height, and measure creating clear hierarchy?
- Layout: Does the page have a strong grid, sensible max-width, and balanced whitespace?
- Components: Do buttons, inputs, cards, tables, tabs, badges, and dialogs feel related?
- Color: Is the palette restrained, readable, and used semantically?
- States: Are hover, focus, active, disabled, selected, loading, empty, success, and error states handled?
- Responsiveness: Does the UI recompose cleanly instead of simply shrinking?
- Accessibility: Are semantics, labels, contrast, focus visibility, and hit targets adequate?
- Density: Is the interface too sparse, too cramped, or inconsistent between sections?

Fix the highest-impact issues first. Do not spend effort polishing low-value details while fundamental hierarchy or layout problems remain.

## Visual Direction

Choose a restrained design direction that fits the product. Prefer one clear visual idea over mixing several trends.

Default qualities:

- Clean, confident, and modern rather than flashy.
- Strong typographic hierarchy.
- Generous but controlled whitespace.
- Soft structural separation using spacing, borders, subtle backgrounds, or elevation.
- One primary accent color used intentionally.
- Limited radius scale and shadow scale.
- Consistent icon size and stroke weight.
- Subtle motion that communicates state or spatial change.

Avoid generic "AI SaaS" styling such as excessive gradients, glassmorphism everywhere, random glow effects, oversized pill shapes, decorative blobs, or every section placed in a card.

For detailed design rules, read `references/design-system.md`.

## Layout Rules

- Establish a clear page container and max-width.
- Use a consistent spacing rhythm rather than arbitrary pixel values.
- Align related elements to shared edges.
- Group content by proximity before adding borders or boxes.
- Use cards only when a container communicates meaningful grouping or interaction.
- Let important content occupy more space than secondary content.
- Keep primary actions visually dominant without making every action loud.
- Prefer responsive grids using content-driven breakpoints.
- On small screens, stack, reorder, collapse, or simplify instead of compressing desktop composition.

## Typography Rules

- Use no more type styles than necessary.
- Create hierarchy through size, weight, line-height, spacing, and contrast together.
- Keep body text comfortably readable.
- Avoid overly wide text blocks.
- Use muted text for supporting information, not essential information.
- Avoid using font weight alone to distinguish interactive states.
- Preserve the project's existing font unless there is a strong reason to change it.

## Color and Surface Rules

- Reuse existing design tokens when available.
- If tokens are inconsistent, consolidate rather than adding more one-off values.
- Use accent colors for emphasis and action, not decoration everywhere.
- Ensure foreground/background combinations remain readable.
- Use borders and shadows sparingly; do not layer both heavily without purpose.
- Keep neutral surfaces visually distinct enough to establish structure.

## Component Quality

For every changed component, check:

- Default state
- Hover state
- Focus-visible state
- Active/pressed state when applicable
- Disabled state when applicable
- Loading state when applicable
- Empty state when applicable
- Error and validation state when applicable
- Mobile behavior
- Long-content behavior

Prefer reusable variants over duplicated class strings when a pattern appears repeatedly.

## shadcn/ui Guidance

When shadcn/ui is present:

- Reuse its accessible primitives rather than rebuilding them.
- Customize tokens, spacing, radii, typography, and composition so the UI does not look like untouched defaults.
- Prefer component variants over scattered overrides.
- Keep destructive, secondary, outline, ghost, and default action semantics consistent.
- Avoid adding shadcn components merely because they exist; use the simplest appropriate primitive.

## Tailwind CSS Guidance

- Prefer project tokens and theme variables over arbitrary values.
- Reduce repeated utility combinations by extracting reusable components or variants when appropriate.
- Use responsive classes intentionally; do not add breakpoints mechanically.
- Avoid dense, unreadable class lists caused by tiny one-off differences when a component abstraction would be clearer.
- Preserve existing Tailwind conventions and configuration.

## React / Next.js Guidance

- Preserve server/client boundaries unless a UI requirement genuinely requires changing them.
- Avoid introducing client state for purely presentational changes.
- Keep components composable and scoped to a clear responsibility.
- Preserve data fetching, routing, forms, and business logic during visual refactors.
- Do not add dependencies for effects that CSS or existing libraries can handle cleanly.
- When an animation library already exists, reuse it. Otherwise prefer CSS transitions for simple states.

## Motion

Use motion to improve comprehension, not to show off.

Good uses:

- Hover and press feedback
- Expand/collapse transitions
- Dialog and sheet entrance/exit
- Tab or selection transitions
- Skeleton/loading transitions
- Small spatial transitions after user actions

Avoid:

- Large entrance animations on every section
- Long easing durations
- Constant ambient motion
- Parallax that hurts readability
- Animation on essential information that delays access

Respect reduced-motion preferences.

## Accessibility

Do not trade accessibility for aesthetics.

- Use semantic HTML before ARIA.
- Keep visible keyboard focus.
- Ensure interactive controls have accessible names.
- Maintain sensible heading order.
- Preserve sufficient color contrast.
- Keep target sizes comfortable on touch devices.
- Do not rely on color alone for status or validation.
- Associate form errors with the affected fields.

## Improvement Strategy

Prefer improvements in this order:

1. Broken or confusing layout
2. Weak hierarchy
3. Inconsistent spacing and sizing
4. Typography
5. Component consistency
6. Responsive behavior
7. Interaction states
8. Accessibility issues
9. Visual refinement
10. Decorative polish

If a page already looks strong, make fewer changes. Do not introduce churn merely to prove work was done.

## Output Behavior

When editing a project, make the changes directly when tools permit. Keep explanations brief unless the user asks for a walkthrough.

After substantial visual work, summarize:

- What changed
- Why those changes improved the interface
- Any important responsive or accessibility behavior
- Any remaining high-value polish opportunities

Do not produce a long design essay before implementation unless the user explicitly asks for a design critique or plan.

## Final Review

Before finishing, run the checklist in `references/review-checklist.md`.

The result should feel cohesive at first glance and remain solid under real content, mobile widths, keyboard navigation, and common UI states.
