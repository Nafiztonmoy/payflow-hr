# Frontend Polish Design System Reference

## Spacing

Use a small, repeatable spacing scale. Prefer values already defined by the project. When no system exists, converge toward a rhythm equivalent to 4, 8, 12, 16, 24, 32, 48, and 64 units rather than arbitrary gaps.

Use tighter spacing inside a component and larger spacing between conceptual groups.

## Radius

Use a limited radius scale. A project usually needs roughly three levels:

- Small controls and compact elements
- Standard cards, inputs, menus, and panels
- Large feature surfaces or dialogs

Avoid mixing sharp, mildly rounded, and pill-shaped components randomly.

## Shadows

Use shadows to communicate elevation, not to decorate every surface.

- Flat surfaces: no shadow or almost none
- Floating menus/dialogs: moderate shadow
- Hero cards or emphasized interactive surfaces: subtle elevation only if it helps hierarchy

## Borders

Use low-contrast borders to define structure when spacing or surface contrast alone is insufficient. Avoid double-separating a component with heavy border plus heavy shadow plus different background unless needed.

## Typography

A useful hierarchy usually includes:

- Display/hero
- Page title
- Section title
- Component title
- Body
- Secondary/meta
- Label/caption

Do not assign every level a unique font size if weight and spacing can do the job.

## Buttons

Primary button:
- One clear primary action per local context
- Strong contrast
- Clear hover and focus-visible states

Secondary button:
- Lower emphasis than primary
- Do not compete visually with the main action

Ghost/text button:
- Use for low-emphasis actions or compact toolbars
- Ensure discoverability on hover/focus

Destructive action:
- Use destructive color only for destructive intent
- Avoid making routine cancel/back actions look destructive

## Forms

- Keep labels visible for important fields.
- Place help text near the relevant field.
- Keep validation messages specific and local.
- Use consistent control heights.
- Make grouped fields read as a unit.
- Avoid placeholder-only labeling for essential information.

## Cards

A card should represent a coherent unit of content or interaction. Avoid wrapping every section in a card.

Use one or more of:
- Surface contrast
- Border
- Radius
- Internal spacing

Do not maximize all four by default.

## Tables

- Prioritize scanability.
- Align numeric values consistently.
- Keep headers visually quieter than primary row content when appropriate.
- Use row hover only if rows are interactive or hover meaningfully aids tracking.
- Handle narrow screens with column prioritization, horizontal scrolling, or alternate compact layouts.

## Navigation

- Make the current location obvious.
- Keep icon and label alignment consistent.
- Avoid excessive navigation depth.
- On mobile, preserve priority rather than exposing every desktop item at once.

## Empty States

Good empty states explain:
1. What is missing
2. Why it matters, if not obvious
3. What the user can do next

Do not over-illustrate simple empty states.

## Loading

Prefer skeletons for predictable content structure and compact spinners for localized actions. Prevent major layout shift when content appears.

## Error States

State what failed, preserve user input where possible, and provide a useful recovery action.

## Dark Mode

When dark mode exists:
- Do not merely invert colors.
- Reduce excessive contrast between large surfaces.
- Keep borders visible but restrained.
- Check muted text carefully.
- Avoid pure black backgrounds everywhere unless the product calls for it.

## Responsive Composition

Desktop-to-mobile adaptation should consider:
- Navigation collapse
- Grid reduction
- Action prioritization
- Table strategy
- Form grouping
- Dialog/sheet behavior
- Typography scaling
- Removal of nonessential decoration

Use breakpoints based on where the composition stops working, not because a framework exposes a breakpoint.
