---
name: ds-check
description: "Check the current screen or component for compliance with the attached design system — tokens, components, spacing, and text styles. Use when a design system library is attached to the file and you want to verify the output is system-consistent."
---

[Design System](https://www.figma.com/make/yCwO1u8y54t0q9tSx2ebJt/Fiserv-Design-System-Components-List?t=x3lQPqzYLOh2kVIb-1)

# Design system compliance check

## When to use
- After building any screen using Make, before review
- When mixing auto-generated elements with library components
- When a design system has been updated and screens need to be audited for drift
- Before presenting to a design system owner or lead designer

## Instructions
1. **Colour tokens** — identify all colour fills (background, text, border, icon). For each, check whether it corresponds to a named token in the attached library. Flag any hardcoded hex or RGB value that is not a token, with its nearest token equivalent.
2. **Spacing** — check padding, margin, and gap values against the spacing scale in the design system. Flag any value that is not on the scale (e.g. if the scale is 4px base and a gap is 10px, flag it).
3. **Typography** — check all text against the named text styles in the library. Flag any text that uses a custom font size, weight, or line height not in the system.
4. **Components** — identify all UI elements that have a matching component in the library. Flag any element that has been rebuilt from scratch when a library component exists. Suggest the correct component name.
5. **Border radius** — check all corner radii against the design system's scale. Flag any deviation.
6. **Iconography** — check that all icons come from the library's approved icon set. Flag any icons from a different source or any custom-drawn icons.
7. Output a compliance score (number of elements checked / number passing) and a flat list of deviations sorted by severity: **Token deviation**, **Component deviation**, **Style deviation**.

## Examples
Input: A card component with a button and a label.
Output:
- Token deviation: Card background uses #FFFFFF (hardcoded). Use $color/surface/default.
- Component deviation: Button is a custom rectangle + text group. Use the Button/Primary component from the library.
- Style deviation: Label uses 11px/400 — no matching text style. Closest match: Label/Small (12px/400). Update or create a token.
- Compliance score: 6/9 elements pass (67%).

## Common edge cases
- If no design system library is attached to the file, output a note asking the user to attach one, then perform a general consistency check across the screen instead.
- If a component exists in the library but is outdated (detached, old variant), flag it as a "stale component" rather than a deviation — recommend re-linking rather than rebuilding.
- If a screen uses a third-party component library (e.g. a charting library), exclude those components from the check and note the exclusion.
- When multiple valid tokens are equally close to a hardcoded value, suggest all options and let the designer decide.
