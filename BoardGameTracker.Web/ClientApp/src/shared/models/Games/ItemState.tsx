export enum ItemState {
  Wanted = 0,
  Owned = 1,
  Sold = 2,
}

export const getItemStateTranslationKey = (value: string) => {
  switch (Number(value)) {
    case ItemState.Wanted:
      return 'games.state.wanted';
    case ItemState.Owned:
      return 'games.state.owned';
    case ItemState.Sold:
      return 'games.state.sold';
    default:
      return ''
  }
};