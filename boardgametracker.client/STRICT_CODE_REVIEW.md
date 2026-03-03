# Strict Code Review - React Frontend

> Reviewed on: 2026-02-12 | Branch: `feature/31-shelf-of-shame`

## Table of Contents

| # | Finding | Severity | Complexity | Status |
|---|---------|----------|------------|--------|
| 1 | [Invited players not updated in EditGameNightModal](#1-invited-players-not-updated-in-editgamenightmodal) | CRITICAL | Medium | To Do |
| 2 | [Duplicate MostPlayedGame interface](#2-duplicate-mostplayedgame-interface) | HIGH | Medium | To Do |
| 3 | [Unnecessary window resize listener in BgtSelect](#3-unnecessary-window-resize-listener-in-bgtselect) | LOW | Low | To Do |
| 4 | [Loading state returns null instead of skeleton](#4-loading-state-returns-null-instead-of-skeleton) | LOW | Low | To Do |

---

## Detailed Findings

### 1. Invited players not updated in EditGameNightModal

**File:** `src/routes/game-nights/-modals/EditGameNightModal.tsx:40`

The form lets users edit invited player IDs (`invitedPlayerIds`), but the submit handler sends the **original** `gameNight.invitedPlayers` back to the API, ignoring user edits.

```typescript
// Current (line 31-41)
const handleSubmit = async (values: GameNightFormValues) => {
  await onSave({
    ...gameNight,
    title: values.title,
    notes: values.notes,
    startDate: values.startDate,
    hostId: values.hostId,
    locationId: values.locationId,
    suggestedGames: games.filter((g) => values.suggestedGameIds.includes(g.id)),
    invitedPlayers: gameNight.invitedPlayers, // BUG: ignores form edits
  });
};
```

**Fix:**
```typescript
const handleSubmit = async (values: GameNightFormValues) => {
  await onSave({
    ...gameNight,
    title: values.title,
    notes: values.notes,
    startDate: values.startDate,
    hostId: values.hostId,
    locationId: values.locationId,
    suggestedGames: games.filter((g) => values.suggestedGameIds.includes(g.id)),
    invitedPlayers: values.invitedPlayerIds.map((id) => ({
      playerId: id,
      gameNightId: gameNight.id,
      rsvpStatus:
        gameNight.invitedPlayers.find((p) => p.playerId === id)?.rsvpStatus ?? 'pending',
    })),
  });
};
```

---

### 2. Duplicate MostPlayedGame interface

**Files:**
- `src/models/Dashboard/DashboardStatistics.ts:39-46`
- `src/models/Player/PlayerStatistics.ts:9-16`

Two separate `MostPlayedGame` interfaces with different nullability:

| Field | DashboardStatistics | PlayerStatistics |
|-------|-------------------|------------------|
| image | `string \| null` | `string` |
| totalWins | `number \| null` | `number` |
| winningPercentage | `number \| null` | `number` |

**Fix:** Create a single shared interface. Match nullability to what the API actually returns for each endpoint (likely the Dashboard version is correct since aggregate stats can be null).

```typescript
// src/models/Common/MostPlayedGame.ts
export interface MostPlayedGame {
  id: number;
  title: string;
  image: string | null;
  totalWins: number | null;
  totalSessions: number;
  winningPercentage: number | null;
}
```

---

### 3. Unnecessary window resize listener in BgtSelect

**File:** `src/components/BgtForm/BgtSelect.tsx:78-86`

A resize event listener calls `event.stopImmediatePropagation()` on every window resize. If this is a workaround for Radix UI popover auto-close behavior, it should have a comment explaining why. If it's no longer needed, remove it.

```typescript
// Current - no explanation
useEffect(() => {
  const onResize = (event: Event) => {
    event.stopImmediatePropagation();
  };
  window.addEventListener('resize', onResize);
  return () => window.removeEventListener('resize', onResize);
}, []);
```

**Fix:** Add a comment explaining the workaround, or remove if no longer needed.

---

### 4. Loading state returns null instead of skeleton

**Files:** `src/routes/games/index.tsx`, `src/routes/players/index.tsx`

Several page components return `null` when loading, causing a brief blank flash before content appears.

```typescript
// Current
if (isLoading) return null;

// Better
if (isLoading) return <LoadingSkeleton />;
```

**Fix:** Replace `null` returns with a loading skeleton or spinner for better UX. Low priority since the flash is brief with prefetched data.
