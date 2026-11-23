# Spec Delta

## MODIFIED Requirements

### Requirement: Distinct tray and executable imagery
The tray icon SHALL show the selected hand cropped at the wrist and update on selection; laser SHALL use lavender for the tray. The executable SHALL use the tilted cactus README artwork as its fixed icon, independently of tray style. The Stream Deck category and pointer-action icons SHALL use the same tilted cactus artwork without changing dynamic on-screen hand behavior.

#### Scenario: Change hand style
- **WHEN** another hand style is selected
- **THEN** the tray reflects that hand while the fixed application and Stream Deck branding remains the tilted cactus

#### Scenario: Select laser mode
- **WHEN** laser mode is selected
- **THEN** the tray uses lavender, the screen shows the laser and the executable retains the cactus icon

#### Scenario: Compare presentation branding
- **WHEN** the user views the README, Windows app icon and Stream Deck category or pointer action
- **THEN** they show the same tilted cactus artwork without changing on-screen pose tracking

### Requirement: Per-monitor desktop alignment
The application SHALL provide per-monitor overlays while pointer mode is active or completed annotations require passive display. Pointer, stroke and text positions SHALL align with physical desktop locations including negative coordinates and mixed DPI through monitor-local rendering conversion. Passive display SHALL omit the pointer and tap animation. A display-layout change in active or passive mode SHALL cancel pending input, clear annotations and rebuild or remove overlays for the new layout while preserving pointer activation.

#### Scenario: Draw on a secondary monitor
- **WHEN** pointing, drawing or placing text on a negative-origin or differently scaled monitor
- **THEN** pointer and annotation anchors align with their intended physical desktop positions

#### Scenario: Show passive annotations
- **WHEN** the pointer is disabled with completed annotations on multiple monitors
- **THEN** those annotations remain aligned and no hand, laser or tap animation is visible

#### Scenario: Change monitor layout
- **WHEN** Windows reports a display-layout change with active or passive overlays
- **THEN** completed and pending content is cleared, overlays match the new topology and the prior pointer-active value is preserved
