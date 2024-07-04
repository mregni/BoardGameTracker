import { GameState } from '../models';

export const getItemStateTranslationKey = (value: GameState): string => {
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
    case GameState.OnLoan:
      return 'game.state.on-loan';
    default:
      return '';
  }
};

export const getItemStateTranslationKeyByString = (value: string): string => {
  return getItemStateTranslationKey(Number(value));
};

export const getColorFromGameState = (state: GameState): 'amber' | 'orange' | 'red' | 'purple' | 'blue' | 'green' => {
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
    case GameState.OnLoan:
    default:
      return 'orange';
  }
};
