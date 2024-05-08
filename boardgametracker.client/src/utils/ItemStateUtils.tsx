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
    default:
      return '';
  }
};

export const getItemStateTranslationKeyByString = (value: string): string => {
  return getItemStateTranslationKey(Number(value));
};
