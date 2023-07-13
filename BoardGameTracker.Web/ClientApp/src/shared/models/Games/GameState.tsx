export enum GameState {
  Wanted = 0,
  Owned = 1,
  PreviouslyOwned = 2,
  NotOwned = 3,
  ForTrade = 4,
}

export const getItemStateTranslationKey = (value: string) => {
  switch (Number(value)) {
    case GameState.Wanted:
      return 'games.state.wanted';
    case GameState.Owned:
      return 'games.state.owned';
    case GameState.PreviouslyOwned:
      return 'games.state.previously-owned';
    case GameState.NotOwned:
      return 'games.state.not-owned';
    case GameState.ForTrade:
      return 'games.state.for-trade';
    default:
      return ''
  }
};