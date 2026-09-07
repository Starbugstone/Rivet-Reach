# Rivet Reach working instructions

## Current project phase

The project is at the beginning: brainstorming, documentation and specifications only. The intended outcome is a complete, responsive game, but implementation has not started. Do not create game code, scaffold Unity, install dependencies or treat planned features as implemented unless the user requests that work.

## Documentation workflow

- Keep project design documents and specifications in `.doc/`.
- Keep `README.md` and this `AGENTS.md` at the repository root. Use the standard uppercase `AGENTS.md` filename for agent instructions.
- Read `README.md`, `.doc/PROJECT_PLAN.md`, `.doc/DEVELOPMENT_STRATEGY.md` and the relevant specialist documents before changing the design.
- Preserve the full-game vision while building toward it through small playable increments.
- Do not treat gameplay responsiveness, scalability and final architecture as competing goals; the project explicitly requires all three to evolve together.
- Distinguish agreed direction, proposals to validate, and unresolved questions. An assistant suggestion is not an approved design decision.
- Make useful documentation changes autonomously within the user's request. Record nonblocking design questions in `.doc/DESIGN_QUESTIONS.md`; ask directly only when an answer is needed to proceed.
- Keep each detailed rule in its authoritative document; summaries elsewhere should link to it. Update affected summaries when a rule changes.
- Keep links relative and verify them after moving or adding documents.
- Always commit and push completed project modifications to the current branch's remote after appropriate checks, unless the user explicitly requests otherwise. This is standing authorization; do not ask for confirmation again. If the push fails, report the blocker and preserve the local work; do not force-push to bypass remote changes.
- Do not claim performance, gameplay quality or implementation correctness has been proven without measurements or playtests.

## Document ownership

- `PROJECT_PLAN.md`: vision, architecture overview, scope and milestones.
- `DEVELOPMENT_STRATEGY.md`: implementation philosophy, playable incremental development and the relationship between responsiveness, performance and final architecture.
- `SIMULATION.md`: proposed timing, network boundaries, persistence and technical validation contracts.
- `GAMEPLAY.md`: player experience, responsiveness, progression and full-game completeness.
- `TRANSPORT.md`: Gate/rocket/teleporter behaviour, traversal and transport exceptions.
- `LORE.md`: hidden history, environmental storytelling and ecology.
- `DESIGN_QUESTIONS.md`: prioritized unresolved choices and evidence needed to resolve them.

## Working principles

Protect the exploration/automation balance, capability-based progression, world-aware state, logical network simulation and persistent saves. Challenge contradictions with concrete scenarios.

Build the final architecture incrementally rather than making disposable foundational prototypes. Each major implementation stage should produce something directly playable while exercising the real data model and simulation approach intended for the final game. Performance work should enable the intended scope through better representation, sleeping systems, scheduling and measured optimization before reducing core gameplay goals.

Prefer small experiments that test risky assumptions through actual gameplay where possible. Documentation work requires review and link checks, not game tests or implementation scaffolding.
