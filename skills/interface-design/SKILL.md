---
name: interface-design
description: Standards and guidelines for professional UI/UX, responsive hierarchy, visual rhythm, microcopy, component states, and accessible interface design.
---

# Interface & Frontend Design Principles

Use this skill when designing, building, or reviewing web or mobile user interfaces, dashboard widgets, views, or reusable frontend components. Ensures applications look professional, intuitive, accessible, and polished.

---

## 1. Visual Hierarchy & Information Architecture
- **Primary vs. Secondary Action:** Every screen must have only **one** dominant primary call-to-action (CTA). Secondary actions should use outlined, muted, or ghost buttons.
- **Scanning Rhythm:** Design for F-pattern (text-heavy content) or Z-pattern (landing pages / dashboards). Users scan headers, bold numbers, and badges first.
- **Progressive Disclosure:** Do not overwhelm the user with all controls at once. Show high-level summaries first; offer drill-downs, accordions, or modals for advanced details.

---

## 2. Spatial Rhythm & Grid System
- **8px Grid System:** Use multiples of 4px and 8px for margins, padding, gap, and dimensions (`4px`, `8px`, `12px`, `16px`, `24px`, `32px`, `48px`, `64px`).
- **Whitespace as a Feature:** Generous whitespace prevents cognitive fatigue. Group related elements closely (proximity principle) and separate unrelated sections with distinct spacing.
- **Container Alignment:** Maintain consistent left and right alignment edges across components.

---

## 3. Typography & Contrast
- **Type Scale:** Limit your interface to 3–4 font sizes (e.g., Title: 20–24px, Section: 16–18px, Body: 14px, Caption/Meta: 11–12px).
- **Font Weight Contrast:** Use weight (Bold / Semi-bold vs. Regular) to establish hierarchy rather than multiplying font families or sizes.
- **Accessibility (WCAG AA):** Ensure text contrast ratio is at least 4.5:1 against the background for body text and 3:1 for large headings. Never use faint gray text (`#bbb`) on white.

---

## 4. Comprehensive Component States
Every interactive component (buttons, inputs, cards, rows) must handle all states:
1. **Default:** Clean, calm, clickable affordance.
2. **Hover:** Subtle background tint, cursor pointer, or elevation change.
3. **Focus:** Clear focus ring (essential for keyboard navigation & accessibility).
4. **Active / Pressed:** Visual tactile feedback (subtle scale or darker tint).
5. **Loading / Pending:** Spinners, skeleton loaders, or disabled state with progress text.
6. **Disabled:** Reduced opacity (e.g. 50%), `cursor: not-allowed`, no hover effect.
7. **Empty State:** Helpful illustration or message explaining why it is empty and how to create the first item.
8. **Error State:** Clear red/amber accent, descriptive inline message explaining what went wrong and how to fix it.

---

## 5. Responsive & Mobile-First Best Practices
- **Touch Targets:** Minimum 44x44px for touchable buttons on mobile devices.
- **Data Tables on Mobile:** Horizontal scroll or transform wide tabular data into compact stacked cards for screens under 768px.
- **Performance:** Avoid layout shifts (CLS), specify image aspect ratios, and optimize font loading.
