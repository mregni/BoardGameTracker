import { useMemo, useCallback } from 'react';
import { useNavigate } from '@tanstack/react-router';

type EntityType = 'game' | 'player' | 'session' | 'location' | 'loan';

interface EntityNavigation {
  toList: () => void;
  toDetail: (entityId?: number) => void;
  toEdit: (entityId?: number) => void;
  toSessions: (entityId?: number) => void;
  toNew: () => void;
}

/**
 * Hook for entity navigation
 * Provides consistent navigation patterns for entity types (games, players, sessions, etc.)
 *
 * @param entityType - The type of entity (game, player, session, location, loan)
 * @param id - Optional default ID to use for navigation
 * @returns Navigation functions for the entity
 *
 * @example
 * ```ts
 * const nav = useEntityNavigation('game', gameId);
 * nav.toDetail(); // Navigate to /games/123
 * nav.toEdit(); // Navigate to /games/123/update
 * nav.toList(); // Navigate to /games
 * ```
 */
export const useEntityNavigation = (entityType: EntityType, id?: number): EntityNavigation => {
  const navigate = useNavigate();

  const toList = useCallback(() => {
    navigate({ to: `/${entityType}s` as '/games' });
  }, [navigate, entityType]);

  const toDetail = useCallback(
    (entityId?: number) => {
      const targetId = entityId ?? id;
      if (!targetId) {
        throw new Error(`${entityType} ID is required for navigation to detail page`);
      }
      navigate({ to: `/${entityType}s/${targetId}` as '/games/$gameId' });
    },
    [navigate, entityType, id]
  );

  const toEdit = useCallback(
    (entityId?: number) => {
      const targetId = entityId ?? id;
      if (!targetId) {
        throw new Error(`${entityType} ID is required for navigation to edit page`);
      }
      navigate({ to: `/${entityType}s/${targetId}/update` as '/games/$gameId/update' });
    },
    [navigate, entityType, id]
  );

  const toSessions = useCallback(
    (entityId?: number) => {
      const targetId = entityId ?? id;
      if (!targetId) {
        throw new Error(`${entityType} ID is required for navigation to sessions page`);
      }
      navigate({ to: `/${entityType}s/${targetId}/sessions` as '/games/$gameId/sessions' });
    },
    [navigate, entityType, id]
  );

  const toNew = useCallback(() => {
    navigate({ to: `/${entityType}s/new` as '/games/new' });
  }, [navigate, entityType]);

  return useMemo(
    () => ({ toList, toDetail, toEdit, toSessions, toNew }),
    [toList, toDetail, toEdit, toSessions, toNew]
  );
};
