# Frontend Polish Final Review Checklist

Use this checklist after implementing meaningful UI changes.

## Visual hierarchy

- Is the primary purpose of the screen obvious within a few seconds?
- Is the main action visually clear?
- Are secondary actions quieter?
- Are headings, supporting text, and metadata clearly differentiated?

## Spacing and alignment

- Are major sections aligned to shared edges?
- Is spacing consistent for similar relationships?
- Are any sections cramped or unnecessarily empty?
- Are repeated components dimensionally consistent?

## Typography

- Are font sizes and weights intentional?
- Are line lengths readable?
- Is muted text still legible?
- Is hierarchy consistent across pages/components touched?

## Components

- Do buttons, inputs, cards, tabs, badges, menus, and dialogs feel like one system?
- Are border radius and shadow choices consistent?
- Are icons consistent in size and alignment?

## States

- Hover checked
- Focus-visible checked
- Active/selected checked
- Disabled checked when applicable
- Loading checked when applicable
- Empty checked when applicable
- Error/validation checked when applicable

## Responsive

- Small mobile width checked
- Typical mobile width checked
- Tablet/intermediate width checked
- Desktop width checked
- Large desktop does not stretch content awkwardly
- No accidental horizontal overflow
- Primary actions remain reachable

## Content resilience

- Long titles do not break layout
- Long labels do not overlap
- Empty values do not collapse structure unexpectedly
- Repeated lists/tables remain usable with realistic data

## Accessibility

- Semantic elements used where possible
- Keyboard navigation works
- Focus is visible
- Controls have accessible names
- Heading order makes sense
- Contrast is adequate
- Status is not conveyed by color alone
- Reduced motion is respected where motion exists

## Code quality

- Existing architecture preserved unless change was justified
- No unnecessary dependencies introduced
- Repeated visual patterns extracted appropriately
- Existing tokens reused where possible
- No obvious one-off hacks that make future maintenance harder

## Final question

Does the interface now look more intentional and easier to use, or merely more decorated?

If it is only more decorated, simplify.
