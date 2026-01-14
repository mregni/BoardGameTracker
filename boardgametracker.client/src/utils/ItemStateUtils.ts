import { GameState } from '../models';

export const getItemStateTranslationKey = (value: GameState, isLoaned: boolean): string => {
  if (isLoaned) {
    return 'game.state.on-loan';
  }

  switch (value) {
    case GameState.Wanted:
      return 'game.state.wanted';
    case GameState.Owned:
      return 'game.state.owned';
    case GameState.PreviouslyOwned:
      return 'game.state.previously-owned';
    case GameState.NotOwned:
      return 'game.state.not-owned';
    case GameState.ForTrade:
      return 'game.state.for-trade';
    default:
      return '';
  }
};

export const getItemStateTranslationKeyByString = (value: string): string => {
  return getItemStateTranslationKey(Number(value), false);
};

export const getColorFromGameState = (
  state: GameState,
  isLoaned: boolean
): 'amber' | 'orange' | 'red' | 'purple' | 'blue' | 'green' => {
  if (isLoaned) {
    return 'orange';
  }

  switch (state) {
    case GameState.Wanted:
      return 'amber';
    case GameState.Owned:
      return 'green';
    case GameState.PreviouslyOwned:
      return 'red';
    case GameState.NotOwned:
      return 'purple';
    case GameState.ForTrade:
      return 'blue';
    default:
      return 'orange';
  }
};
